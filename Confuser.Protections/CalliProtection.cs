using Confuser.Core;
using Confuser.Core.Helpers;
using Confuser.Core.Services;
using Confuser.Renamer;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Confuser.Protections {
	[BeforeProtection("Ki.Constants", "Ki.AntiDebug", "Ki.AntiDump")]
	public class CalliProtection : Protection {
		public override ProtectionPreset Preset => ProtectionPreset.Maximum;

		public override string Name => "Calli Protection";

		public override string Description => "Replaces Calls with Calli";

		public override string Author => "ElektroKill";

		public override string Id => "calli";

		public override string FullId => "ElektroKill.Calli";

		protected override void Initialize(ConfuserContext context) { }

		protected override void PopulatePipeline(ProtectionPipeline pipeline) {
			pipeline.InsertPreStage(PipelineStage.ProcessModule, new InjectPhase(this));
			pipeline.InsertPreStage(PipelineStage.ProcessModule, new CalliPhase(this));
		}

		private static MethodDef calliMethod;
		private static CalliMode mode;

		private class InjectPhase : ProtectionPhase {
			public InjectPhase(CalliProtection parent) : base(parent) { }

			public override ProtectionTargets Targets => ProtectionTargets.Modules;

			public override string Name => "Calli Helper Injection";

			protected override void Execute(ConfuserContext context, ProtectionParameters parameters) {
				var rt = context.Registry.GetService<IRuntimeService>();
				var marker = context.Registry.GetService<IMarkerService>();
				var name = context.Registry.GetService<INameService>();

				foreach (ModuleDef module in parameters.Targets.OfType<ModuleDef>()) {
					mode = parameters.GetParameter(context, module, "mode", CalliMode.Normal);
					if (mode == CalliMode.Normal) {
						TypeDef typeDef = rt.GetRuntimeType("Confuser.Runtime.Calli");
						IEnumerable<IDnlibDef> members = InjectHelper.Inject(typeDef, module.GlobalType, module);
						calliMethod = (MethodDef)members.Single(method => method.Name == "ResolveToken");
						foreach (IDnlibDef member in members)
							name.MarkHelper(member, marker, (Protection)Parent);
						context.CurrentModuleWriterOptions.MetadataOptions.Flags |= MetadataFlags.PreserveAllMethodRids;
					}
				}
			}
		}

		private class CalliPhase : ProtectionPhase {
			public CalliPhase(CalliProtection parent) : base(parent) { }

			public override ProtectionTargets Targets => ProtectionTargets.Types;

			public override string Name => "Call to Calli Replacer";

			protected override void Execute(ConfuserContext context, ProtectionParameters parameters) {
				foreach (TypeDef type in parameters.Targets.OfType<TypeDef>()) {
					if (type.InGlobalModuleType()) continue;
					foreach (MethodDef method in type.Methods) {
						if (method.InGlobalModuleType()) continue;
						if (method.FullName.Contains("My.")) continue;

						switch (mode) {
							case CalliMode.Normal: {
									if (method.Equals(calliMethod)) continue;
									if (!method.HasBody) continue;
									if (!method.Body.HasInstructions) continue;
									for (int i = 0; i < method.Body.Instructions.Count; i++) {
										if (method.Body.Instructions[i].OpCode == OpCodes.Call || method.Body.Instructions[i].OpCode == OpCodes.Callvirt) {
											try {
												MemberRef membertocalli = (MemberRef)method.Body.Instructions[i].Operand;
												method.Body.Instructions[i].OpCode = OpCodes.Calli;
												method.Body.Instructions[i].Operand = membertocalli.MethodSig;
												
												method.Body.Instructions.Insert(i, Instruction.Create(OpCodes.Call, calliMethod));
												method.Body.Instructions.Insert(i, Instruction.Create(OpCodes.Ldc_I8, (long)membertocalli.MDToken.ToInt32()));
											}
											catch { }
										}
									}
									break;
								}
							case CalliMode.Ldftn: {
									if (!method.HasBody) continue;
									if (!method.Body.HasInstructions) continue;
									for (int i = 0; i < method.Body.Instructions.Count; i++) {
										if (method.Body.Instructions[i].OpCode == OpCodes.Call || method.Body.Instructions[i].OpCode == OpCodes.Callvirt) {
											try {
												MemberRef membertocalli = (MemberRef)method.Body.Instructions[i].Operand;
												method.Body.Instructions[i].OpCode = OpCodes.Calli;
												method.Body.Instructions[i].Operand = membertocalli.MethodSig;
												method.Body.Instructions.Insert(i, Instruction.Create(OpCodes.Ldftn, membertocalli));
											}
											catch { }
										}
									}
									break;
								}
						}
					}
				}
			}
		}

		enum CalliMode {
			Normal,
			Ldftn
		}
	}
}
