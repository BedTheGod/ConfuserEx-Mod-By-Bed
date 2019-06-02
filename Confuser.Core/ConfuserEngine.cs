using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Confuser.Core.Services;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using Microsoft.Win32;

namespace Confuser.Core
{
	/// <summary>
	///     The processing engine of ConfuserEx.
	/// </summary>
	// Token: 0x02000035 RID: 53
	public static class ConfuserEngine
	{
		// Token: 0x06000108 RID: 264 RVA: 0x00008780 File Offset: 0x00006980
		static ConfuserEngine()
		{
			Assembly assembly = typeof(ConfuserEngine).Assembly;
			AssemblyProductAttribute nameAttr = (AssemblyProductAttribute)assembly.GetCustomAttributes(typeof(AssemblyProductAttribute), false)[0];
			AssemblyInformationalVersionAttribute verAttr = (AssemblyInformationalVersionAttribute)assembly.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)[0];
			AssemblyCopyrightAttribute cpAttr = (AssemblyCopyrightAttribute)assembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false)[0];
			ConfuserEngine.Version = string.Format("{0} {1}", nameAttr.Product, verAttr.InformationalVersion);
			ConfuserEngine.Copyright = cpAttr.Copyright;

            AppDomain.CurrentDomain.AssemblyResolve += (sender, e) => {
                try
                {
                    var asmName = new AssemblyName(e.Name);
                    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                        if (asm.GetName().Name == asmName.Name)
                            return asm;
                    return null;
                }
                catch
                {
                    return null;
                }
            };
        }

		/// <summary>
		///     Runs the engine with the specified parameters.
		/// </summary>
		/// <param name="parameters">The parameters.</param>
		/// <param name="token">The token used for cancellation.</param>
		/// <returns>Task to run the engine.</returns>
		/// <exception cref="T:System.ArgumentNullException">
		///     <paramref name="parameters" />.Project is <c>null</c>.
		/// </exception>
		// Token: 0x06000109 RID: 265 RVA: 0x00008830 File Offset: 0x00006A30
		public static Task Run(ConfuserParameters parameters, CancellationToken? token = null)
		{
			if (parameters.Project == null)
			{
				throw new ArgumentNullException("parameters");
			}
			if (!token.HasValue)
			{
				token = new CancellationToken?(new CancellationTokenSource().Token);
			}
			return Task.Factory.StartNew(delegate
			{
				ConfuserEngine.RunInternal(parameters, token.Value);
			}, token.Value);
		}

		/// <summary>
		///     Runs the engine.
		/// </summary>
		/// <param name="parameters">The parameters.</param>
		/// <param name="token">The cancellation token.</param>
		// Token: 0x0600010A RID: 266 RVA: 0x000088B4 File Offset: 0x00006AB4
		private static void RunInternal(ConfuserParameters parameters, CancellationToken token)
		{
			ConfuserContext context = new ConfuserContext();
			context.Logger = parameters.GetLogger();
			context.Project = parameters.Project;
			context.PackerInitiated = parameters.PackerInitiated;
			context.token = token;
			ConfuserEngine.PrintInfo(context);
			bool ok = false;
			try
			{
				AssemblyResolver asmResolver = new AssemblyResolver();
				asmResolver.EnableTypeDefCache = true;
				asmResolver.DefaultModuleContext = new ModuleContext(asmResolver);
				context.Resolver = asmResolver;
				context.BaseDirectory = Path.Combine(Environment.CurrentDirectory, parameters.Project.BaseDirectory.TrimEnd(new char[]
				{
					Path.DirectorySeparatorChar
				}) + Path.DirectorySeparatorChar);
				context.OutputDirectory = Path.Combine(parameters.Project.BaseDirectory, parameters.Project.OutputDirectory.TrimEnd(new char[]
				{
					Path.DirectorySeparatorChar
				}) + Path.DirectorySeparatorChar);
				foreach (string probePath in parameters.Project.ProbePaths)
				{
					asmResolver.PostSearchPaths.Insert(0, Path.Combine(context.BaseDirectory, probePath));
				}
				context.CheckCancellation();
				Marker marker = parameters.GetMarker();
				context.Logger.Debug("Discovering plugins...");
				IList<Protection> prots;
				IList<Packer> packers;
				IList<ConfuserComponent> components;
				parameters.GetPluginDiscovery().GetPlugins(context, out prots, out packers, out components);
				context.Logger.InfoFormat("Discovered {0} protections, {1} packers.", new object[]
				{
					prots.Count,
					packers.Count
				});
				context.CheckCancellation();
				context.Logger.Debug("Resolving component dependency...");
				try
				{
					DependencyResolver resolver = new DependencyResolver(prots);
					prots = resolver.SortDependency();
				}
				catch (CircularDependencyException ex)
				{
					context.Logger.ErrorException("", ex);
					throw new ConfuserException(ex);
				}
				components.Insert(0, new CoreComponent(parameters, marker));
				foreach (Protection prot in prots)
				{
					components.Add(prot);
				}
				foreach (Packer packer in packers)
				{
					components.Add(packer);
				}
				context.CheckCancellation();
				context.Logger.Info("Loading input modules...");
				marker.Initalize(prots, packers);
				MarkerResult markings = marker.MarkProject(parameters.Project, context);
				context.Modules = markings.Modules.ToList<ModuleDefMD>().AsReadOnly();
				foreach (ModuleDefMD module in context.Modules)
				{
					module.EnableTypeDefFindCache = true;
				}
				context.OutputModules = Enumerable.Repeat<byte[]>(null, markings.Modules.Count).ToArray<byte[]>();
				context.OutputSymbols = Enumerable.Repeat<byte[]>(null, markings.Modules.Count).ToArray<byte[]>();
				context.OutputPaths = Enumerable.Repeat<string>(null, markings.Modules.Count).ToArray<string>();
				context.Packer = markings.Packer;
				context.ExternalModules = markings.ExternalModules;
				context.CheckCancellation();
				context.Logger.Info("Initializing...");
				foreach (ConfuserComponent comp in components)
				{
					try
					{
						comp.Initialize(context);
					}
					catch (Exception ex2)
					{
						context.Logger.ErrorException("Error occured during initialization of '" + comp.Name + "'.", ex2);
						throw new ConfuserException(ex2);
					}
					context.CheckCancellation();
				}
				context.CheckCancellation();
				context.Logger.Debug("Building pipeline...");
				ProtectionPipeline pipeline = new ProtectionPipeline();
				context.Pipeline = pipeline;
				foreach (ConfuserComponent comp2 in components)
				{
					comp2.PopulatePipeline(pipeline);
				}
				context.CheckCancellation();
				ConfuserEngine.RunPipeline(pipeline, context);
				ok = true;
			}
			catch (AssemblyResolveException ex3)
			{
				context.Logger.ErrorException("Failed to resolve an assembly, check if all dependencies are present in the correct version.", ex3);
				ConfuserEngine.PrintEnvironmentInfo(context);
			}
			catch (TypeResolveException ex4)
			{
				context.Logger.ErrorException("Failed to resolve a type, check if all dependencies are present in the correct version.", ex4);
				ConfuserEngine.PrintEnvironmentInfo(context);
			}
			catch (MemberRefResolveException ex5)
			{
				context.Logger.ErrorException("Failed to resolve a member, check if all dependencies are present in the correct version.", ex5);
				ConfuserEngine.PrintEnvironmentInfo(context);
			}
			catch (IOException ex6)
			{
				context.Logger.ErrorException("An IO error occurred, check if all input/output locations are readable/writable.", ex6);
			}
			catch (OperationCanceledException)
			{
				context.Logger.Error("Operation cancelled.");
			}
			catch (ConfuserException)
			{
			}
			catch (Exception ex7)
			{
				context.Logger.ErrorException("Unknown error occurred.", ex7);
			}
			finally
			{
				if (context.Resolver != null)
				{
					context.Resolver.Clear();
				}
				context.Logger.Finish(ok);
			}
		}

		/// <summary>
		///     Runs the protection pipeline.
		/// </summary>
		/// <param name="pipeline">The protection pipeline.</param>
		/// <param name="context">The context.</param>
		// Token: 0x0600010B RID: 267 RVA: 0x00009030 File Offset: 0x00007230
		private static void RunPipeline(ProtectionPipeline pipeline, ConfuserContext context)
		{
			Func<IList<IDnlibDef>> getAllDefs = () => context.Modules.SelectMany((ModuleDefMD module) => module.FindDefinitions()).ToList<IDnlibDef>();
			Func<ModuleDef, IList<IDnlibDef>> getModuleDefs = (ModuleDef module) => module.FindDefinitions().ToList<IDnlibDef>();
			context.CurrentModuleIndex = -1;
			pipeline.ExecuteStage(PipelineStage.Inspection, new Action<ConfuserContext>(ConfuserEngine.Inspection), () => getAllDefs(), context);
			ModuleWriterOptionsBase[] options = new ModuleWriterOptionsBase[context.Modules.Count];
			ModuleWriterListener[] listeners = new ModuleWriterListener[context.Modules.Count];
			for (int i = 0; i < context.Modules.Count; i++)
			{
				context.CurrentModuleIndex = i;
				context.CurrentModuleWriterOptions = null;
				context.CurrentModuleWriterListener = null;
				pipeline.ExecuteStage(PipelineStage.BeginModule, new Action<ConfuserContext>(ConfuserEngine.BeginModule), () => getModuleDefs(context.CurrentModule), context);
				pipeline.ExecuteStage(PipelineStage.ProcessModule, new Action<ConfuserContext>(ConfuserEngine.ProcessModule), () => getModuleDefs(context.CurrentModule), context);
				pipeline.ExecuteStage(PipelineStage.OptimizeMethods, new Action<ConfuserContext>(ConfuserEngine.OptimizeMethods), () => getModuleDefs(context.CurrentModule), context);
				pipeline.ExecuteStage(PipelineStage.EndModule, new Action<ConfuserContext>(ConfuserEngine.EndModule), () => getModuleDefs(context.CurrentModule), context);
				options[i] = context.CurrentModuleWriterOptions;
				listeners[i] = context.CurrentModuleWriterListener;
			}
			for (int j = 0; j < context.Modules.Count; j++)
			{
				context.CurrentModuleIndex = j;
				context.CurrentModuleWriterOptions = options[j];
				context.CurrentModuleWriterListener = listeners[j];
				pipeline.ExecuteStage(PipelineStage.WriteModule, new Action<ConfuserContext>(ConfuserEngine.WriteModule), () => getModuleDefs(context.CurrentModule), context);
				context.OutputModules[j] = context.CurrentModuleOutput;
				context.OutputSymbols[j] = context.CurrentModuleSymbol;
				context.CurrentModuleWriterOptions = null;
				context.CurrentModuleWriterListener = null;
				context.CurrentModuleOutput = null;
				context.CurrentModuleSymbol = null;
			}
			context.CurrentModuleIndex = -1;
			pipeline.ExecuteStage(PipelineStage.Debug, new Action<ConfuserContext>(ConfuserEngine.Debug), () => getAllDefs(), context);
			pipeline.ExecuteStage(PipelineStage.Pack, new Action<ConfuserContext>(ConfuserEngine.Pack), () => getAllDefs(), context);
			pipeline.ExecuteStage(PipelineStage.SaveModules, new Action<ConfuserContext>(ConfuserEngine.SaveModules), () => getAllDefs(), context);
			if (!context.PackerInitiated)
			{
				context.Logger.Info("Done.");
			}
		}

		// Token: 0x0600010C RID: 268 RVA: 0x000093F8 File Offset: 0x000075F8
		private static void Inspection(ConfuserContext context)
		{
			context.Logger.Info("Resolving dependencies...");
			foreach (Tuple<AssemblyRef, ModuleDefMD> dependency in context.Modules.SelectMany((ModuleDefMD module) => from asmRef in module.GetAssemblyRefs()
			select Tuple.Create<AssemblyRef, ModuleDefMD>(asmRef, module)))
			{
				try
				{
					context.Resolver.ResolveThrow(dependency.Item1, dependency.Item2);
				}
				catch (AssemblyResolveException ex)
				{
					context.Logger.ErrorException("Failed to resolve dependency of '" + dependency.Item2.Name + "'.", ex);
					throw new ConfuserException(ex);
				}
			}
			context.Logger.Debug("Checking Strong Name...");
			foreach (ModuleDefMD module4 in context.Modules)
			{
				StrongNameKey snKey = context.Annotations.Get<StrongNameKey>(module4, Marker.SNKey, null);
				if (snKey == null && module4.IsStrongNameSigned)
				{
					context.Logger.WarnFormat("[{0}] SN Key is not provided for a signed module, the output may not be working.", new object[]
					{
						module4.Name
					});
				}
				else if (snKey != null && !module4.IsStrongNameSigned)
				{
					context.Logger.WarnFormat("[{0}] SN Key is provided for an unsigned module, the output may not be working.", new object[]
					{
						module4.Name
					});
				}
				else if (snKey != null && module4.IsStrongNameSigned && !module4.Assembly.PublicKey.Data.SequenceEqual(snKey.PublicKey))
				{
					context.Logger.WarnFormat("[{0}] Provided SN Key and signed module's public key do not match, the output may not be working.", new object[]
					{
						module4.Name
					});
				}
			}
			IMarkerService marker = context.Registry.GetService<IMarkerService>();
			context.Logger.Debug("Creating global .cctors...");
			foreach (ModuleDefMD module2 in context.Modules)
			{
				TypeDef modType = module2.GlobalType;
				if (modType == null)
				{
					modType = new TypeDefUser("", "<Bed>", null);
					modType.Attributes = dnlib.DotNet.TypeAttributes.NotPublic;
					module2.Types.Add(modType);
					marker.Mark(modType, null);
				}
				MethodDef cctor = modType.FindOrCreateStaticConstructor();
				if (!marker.IsMarked(cctor))
				{
					marker.Mark(cctor, null);
				}
			}
			context.Logger.Debug("Watermarking...");
			foreach (ModuleDefMD module3 in context.Modules)
			{
				TypeRef attrRef = module3.CorLibTypes.GetTypeRef("System", "Attribute");
				TypeDefUser attrType = new TypeDefUser("", "EternalDeath-Protector-Private2019", attrRef);
				module3.Types.Add(attrType);
				marker.Mark(attrType, null);
				MethodDefUser ctor = new MethodDefUser(".ctor", MethodSig.CreateInstance(module3.CorLibTypes.Void, module3.CorLibTypes.String), dnlib.DotNet.MethodImplAttributes.IL, dnlib.DotNet.MethodAttributes.FamANDAssem | dnlib.DotNet.MethodAttributes.Family | dnlib.DotNet.MethodAttributes.HideBySig | dnlib.DotNet.MethodAttributes.SpecialName | dnlib.DotNet.MethodAttributes.RTSpecialName);
				ctor.Body = new CilBody();
				ctor.Body.MaxStack = 1;
				ctor.Body.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
				ctor.Body.Instructions.Add(OpCodes.Call.ToInstruction(new MemberRefUser(module3, ".ctor", MethodSig.CreateInstance(module3.CorLibTypes.Void), attrRef)));
				ctor.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
				attrType.Methods.Add(ctor);
				marker.Mark(ctor, null);
				CustomAttribute attr = new CustomAttribute(ctor);
				attr.ConstructorArguments.Add(new CAArgument(module3.CorLibTypes.String, ConfuserEngine.Version));
				module3.CustomAttributes.Add(attr);
			}
		}

		// Token: 0x0600010D RID: 269 RVA: 0x000098A4 File Offset: 0x00007AA4
		private static void BeginModule(ConfuserContext context)
		{
			context.Logger.InfoFormat("Processing module '{0}'...", new object[]
			{
				context.CurrentModule.Name
			});
			context.CurrentModuleWriterListener = new ModuleWriterListener();
			context.CurrentModuleWriterListener.OnWriterEvent += delegate(object sender, ModuleWriterListenerEventArgs e)
			{
				context.CheckCancellation();
			};
			context.CurrentModuleWriterOptions = new ModuleWriterOptions(context.CurrentModule, context.CurrentModuleWriterListener);
			if (!context.CurrentModule.IsILOnly)
			{
				context.RequestNative();
			}
			StrongNameKey snKey = context.Annotations.Get<StrongNameKey>(context.CurrentModule, Marker.SNKey, null);
			context.CurrentModuleWriterOptions.InitializeStrongNameSigning(context.CurrentModule, snKey);
			foreach (TypeDef type in context.CurrentModule.GetTypes())
			{
				foreach (MethodDef method in type.Methods)
				{
					if (method.Body != null)
					{
						method.Body.Instructions.SimplifyMacros(method.Body.Variables, method.Parameters);
					}
				}
			}
		}

		// Token: 0x0600010E RID: 270 RVA: 0x00009A4C File Offset: 0x00007C4C
		private static void ProcessModule(ConfuserContext context)
		{
		}

		// Token: 0x0600010F RID: 271 RVA: 0x00009A50 File Offset: 0x00007C50
		private static void OptimizeMethods(ConfuserContext context)
		{
			foreach (TypeDef type in context.CurrentModule.GetTypes())
			{
				foreach (MethodDef method in type.Methods)
				{
					if (method.Body != null)
					{
						method.Body.Instructions.OptimizeMacros();
					}
				}
			}
		}

		// Token: 0x06000110 RID: 272 RVA: 0x00009AE8 File Offset: 0x00007CE8
		private static void EndModule(ConfuserContext context)
		{
			string output = context.Modules[context.CurrentModuleIndex].Location;
			if (output != null)
			{
				if (!Path.IsPathRooted(output))
				{
					output = Path.Combine(Environment.CurrentDirectory, output);
				}
				output = Utils.GetRelativePath(output, context.BaseDirectory);
			}
			else
			{
				output = context.CurrentModule.Name;
			}
			context.OutputPaths[context.CurrentModuleIndex] = output;
		}

		// Token: 0x06000111 RID: 273 RVA: 0x00009B58 File Offset: 0x00007D58
		private static void WriteModule(ConfuserContext context)
		{
			context.Logger.InfoFormat("Writing module '{0}'...", new object[]
			{
				context.CurrentModule.Name
			});
			MemoryStream pdb = null;
			MemoryStream output = new MemoryStream();
			if (context.CurrentModule.PdbState != null)
			{
				pdb = new MemoryStream();
				context.CurrentModuleWriterOptions.WritePdb = true;
				context.CurrentModuleWriterOptions.PdbFileName = Path.ChangeExtension(Path.GetFileName(context.OutputPaths[context.CurrentModuleIndex]), "pdb");
				context.CurrentModuleWriterOptions.PdbStream = pdb;
			}
            //Anti Dnspy
            context.CurrentModuleWriterOptions.MetaDataLogger = DummyLogger.NoThrowInstance;
            if (context.CurrentModuleWriterOptions is ModuleWriterOptions)
			{
				context.CurrentModule.Write(output, (ModuleWriterOptions)context.CurrentModuleWriterOptions);
			}
			else
			{
				context.CurrentModule.NativeWrite(output, (NativeModuleWriterOptions)context.CurrentModuleWriterOptions);
			}
			context.CurrentModuleOutput = output.ToArray();
			if (context.CurrentModule.PdbState != null)
			{
				context.CurrentModuleSymbol = pdb.ToArray();
			}
		}

		// Token: 0x06000112 RID: 274 RVA: 0x00009C4C File Offset: 0x00007E4C
		private static void Debug(ConfuserContext context)
		{
			context.Logger.Info("Finalizing...");
			for (int i = 0; i < context.OutputModules.Count; i++)
			{
				if (context.OutputSymbols[i] != null)
				{
					string path = Path.GetFullPath(Path.Combine(context.OutputDirectory, context.OutputPaths[i]));
					string dir = Path.GetDirectoryName(path);
					if (!Directory.Exists(dir))
					{
						Directory.CreateDirectory(dir);
					}
					File.WriteAllBytes(Path.ChangeExtension(path, "pdb"), context.OutputSymbols[i]);
				}
			}
		}

		// Token: 0x06000113 RID: 275 RVA: 0x00009CDC File Offset: 0x00007EDC
		private static void Pack(ConfuserContext context)
		{
			if (context.Packer != null)
			{
				context.Logger.Info("Packing...");
				context.Packer.Pack(context, new ProtectionParameters(context.Packer, context.Modules.OfType<IDnlibDef>().ToList<IDnlibDef>()));
			}
		}

		// Token: 0x06000114 RID: 276 RVA: 0x00009D28 File Offset: 0x00007F28
		private static void SaveModules(ConfuserContext context)
		{
			context.Resolver.Clear();
			for (int i = 0; i < context.OutputModules.Count; i++)
			{
				string path = Path.GetFullPath(Path.Combine(context.OutputDirectory, context.OutputPaths[i]));
				string dir = Path.GetDirectoryName(path);
				if (!Directory.Exists(dir))
				{
					Directory.CreateDirectory(dir);
				}
				context.Logger.DebugFormat("Saving to '{0}'...", new object[]
				{
					path
				});
				File.WriteAllBytes(path, context.OutputModules[i]);
              
            }
		}

		/// <summary>
		///     Prints the copyright stuff and environment information.
		/// </summary>
		/// <param name="context">The working context.</param>
		// Token: 0x06000115 RID: 277 RVA: 0x00009DB8 File Offset: 0x00007FB8
		private static void PrintInfo(ConfuserContext context)
		{
			if (context.PackerInitiated)
			{
				context.Logger.Info("Protecting packer stub...");
				return;
			}
			context.Logger.InfoFormat("{0} {1}", new object[]
			{
				ConfuserEngine.Version,
				ConfuserEngine.Copyright
			});
			Type mono = Type.GetType("Mono.Runtime");
			context.Logger.InfoFormat("Running on {0}, {1}, {2} bits", new object[]
			{
				Environment.OSVersion,
				(mono == null) ? (".NET Framework v" + Environment.Version) : mono.GetMethod("GetDisplayName", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null),
				IntPtr.Size * 8
			});
		}

		// Token: 0x06000116 RID: 278 RVA: 0x0000A410 File Offset: 0x00008610
		private static IEnumerable<string> GetFrameworkVersions()
		{
			using (RegistryKey registryKey = RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, "").OpenSubKey("SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\"))
			{
				try
				{
					string[] subKeyNames = registryKey.GetSubKeyNames();
					for (int i = 0; i < subKeyNames.Length; i++)
					{
						string text = subKeyNames[i];
						if (text.StartsWith("v"))
						{
							RegistryKey registryKey2 = registryKey.OpenSubKey(text);
							string text2 = (string)registryKey2.GetValue("Version", "");
							string a = registryKey2.GetValue("SP", "").ToString();
							string a2 = registryKey2.GetValue("Install", "").ToString();
							if (a2 == "" || (a != "" && a2 == "1"))
							{
								yield return text + "  " + text2;
							}
							if (!(text2 != ""))
							{
								try
								{
									string[] subKeyNames2 = registryKey2.GetSubKeyNames();
									for (int j = 0; j < subKeyNames2.Length; j++)
									{
										string text3 = subKeyNames2[j];
										RegistryKey registryKey3 = registryKey2.OpenSubKey(text3);
										text2 = (string)registryKey3.GetValue("Version", "");
										if (text2 != "")
										{
											a = registryKey3.GetValue("SP", "").ToString();
										}
										a2 = registryKey3.GetValue("Install", "").ToString();
										if (a2 == "")
										{
											yield return text + "  " + text2;
										}
										else if (a2 == "1")
										{
											yield return "  " + text3 + "  " + text2;
										}
									}
								}
								finally
								{
								}
							}
						}
					}
				}
				finally
				{
				}
			}
			using (RegistryKey registryKey4 = RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, "").OpenSubKey("SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v4\\Full\\"))
			{
				if (registryKey4.GetValue("Release") == null)
				{
					yield break;
				}
				int num = (int)registryKey4.GetValue("Release");
				yield return "v4.5 " + num;
			}
			yield break;
		}

		/// <summary>
		///     Prints the environment information when error occurred.
		/// </summary>
		/// <param name="context">The working context.</param>
		// Token: 0x06000117 RID: 279 RVA: 0x0000A430 File Offset: 0x00008630
		private static void PrintEnvironmentInfo(ConfuserContext context)
		{
			if (context.PackerInitiated)
			{
				return;
			}
			context.Logger.Error("---BEGIN DEBUG INFO---");
			context.Logger.Error("Installed Framework Versions:");
			foreach (string ver in ConfuserEngine.GetFrameworkVersions())
			{
				context.Logger.ErrorFormat("    {0}", new object[]
				{
					ver.Trim()
				});
			}
			context.Logger.Error("");
			if (context.Resolver != null)
			{
				context.Logger.Error("Cached assemblies:");
				foreach (AssemblyDef asm in context.Resolver.GetCachedAssemblies())
				{
					if (string.IsNullOrEmpty(asm.ManifestModule.Location))
					{
						context.Logger.ErrorFormat("    {0}", new object[]
						{
							asm.FullName
						});
					}
					else
					{
						context.Logger.ErrorFormat("    {0} ({1})", new object[]
						{
							asm.FullName,
							asm.ManifestModule.Location
						});
					}
					foreach (AssemblyRef reference in asm.Modules.OfType<ModuleDefMD>().SelectMany((ModuleDefMD m) => m.GetAssemblyRefs()))
					{
						context.Logger.ErrorFormat("        {0}", new object[]
						{
							reference.FullName
						});
					}
				}
			}
			context.Logger.Error("---END DEBUG INFO---");
		}

		/// <summary>
		///     The version of ConfuserEx.
		/// </summary>
		// Token: 0x0400010D RID: 269
		public static readonly string Version;

		// Token: 0x0400010E RID: 270
		private static readonly string Copyright;
	}
}
