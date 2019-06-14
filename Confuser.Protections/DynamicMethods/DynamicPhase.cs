
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SRE = System.Reflection.Emit;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using Perplex.Core;

namespace Perplex.NF.Protections.DynamicMethods
{
    public class DynamicPhase : ProtectionPhase
    {
        public override ProtectionTargets Targets => ProtectionTargets.Methods;

        public DynamicContext Context;
        public MethodDef Method;
        public CilBody NewBody;

        public override void Execute(PerplexContext context, IList<IDnlibDef> targets)
        {
            Context = context.Get<DynamicContext>(DynamicMethodsProtection.ContextKey);
            Console.WriteLine(OpCodes.Throw.OperandType);
            foreach (var method in targets.OfType<MethodDef>())
            {
                if (!method.HasBody || method.DeclaringType.IsGlobalModuleType || method.IsVirtual || method.IsConstructor) continue;
                Method = method;

                Serialize();
                InitializeLocals();
                InitializeBranches();
                InitializeExceptionHandlers();

                var instructions = NewBody.Instructions;

                foreach (var instruction in method.Body.Instructions)
                {
                    TryTransform(instruction);

                    var parameters = new List<Type>();
                    parameters.Add(typeof(SRE.OpCode));

                    string name = "Emit";
                    var reflectionOpCode = OpCodeToRef(instruction.OpCode.Name.Replace(".", "_"));

                    instructions.Add(OpCodes.Ldloc.ToInstruction(Context.ILGenerator));
                    instructions.Add(OpCodes.Ldsfld.ToInstruction(reflectionOpCode));
                    switch (instruction.OpCode.OperandType)
                    {
                        case OperandType.InlineBrTarget:
                            var lbl = Context.BranchRefs[instruction];
                            parameters.Add(typeof(SRE.Label));
                            instructions.Add(OpCodes.Ldloc.ToInstruction(lbl));
                            break;
                        case OperandType.InlineVar:
                            if (instruction.Operand is Local lcl)
                            {
                                var local = Context.LocalRefs[lcl];
                                parameters.Add(typeof(SRE.LocalBuilder));
                                instructions.Add(OpCodes.Ldloc.ToInstruction(local));
                            }
                            else
                            {
                                parameters.Add(typeof(int));
                                instructions.Add(Instruction.CreateLdcI4(instruction.GetParameterIndex()));
                            }
                            break;
                        case OperandType.InlineI:
                            parameters.Add(typeof(int));
                            instructions.Add(OpCodes.Ldc_I4.ToInstruction((int)instruction.Operand));
                            break;
                        case OperandType.InlineR:
                            parameters.Add(typeof(double));
                            instructions.Add(OpCodes.Ldc_R8.ToInstruction((double)instruction.Operand));
                            break;
                        case OperandType.ShortInlineR:
                            parameters.Add(typeof(float));
                            instructions.Add(OpCodes.Ldc_R4.ToInstruction((float)instruction.Operand));
                            break;
                        case OperandType.InlineI8:
                            parameters.Add(typeof(long));
                            instructions.Add(OpCodes.Ldc_I8.ToInstruction((long)instruction.Operand));
                            break;
                        case OperandType.InlineString:
                            parameters.Add(typeof(string));
                            instructions.Add(OpCodes.Ldstr.ToInstruction((string)instruction.Operand));
                            break;
                        case OperandType.InlineTok:
                        case OperandType.InlineType:
                            parameters.Add(typeof(Type));
                            EmitTypeof((ITypeDefOrRef)instruction.Operand);
                            instructions.Add(OpCodes.Call.ToInstruction(Context.GetRefImport<IMethod>("GetTypeFromHandle")));
                            break;
                        case OperandType.InlineField:
                            parameters.Add(typeof(FieldInfo));
                            var mRef = (IField)instruction.Operand;
                            EmitTypeof(mRef.DeclaringType);
                            instructions.Add(OpCodes.Ldstr.ToInstruction(Context.Random.ShuffleString(mRef.Name)));
                            instructions.Add(OpCodes.Ldc_I4.ToInstruction(0x3D));
                            instructions.Add(OpCodes.Callvirt.ToInstruction(Context.GetRefImport<MemberRef>("GetField")));
                            break;
                        case OperandType.InlineMethod:
                            bool addLdnull = false;
                            var m = (IMethod)instruction.Operand;
                            MethodDef get = null;
                            if (m.Name == ".ctor" && instruction.OpCode != OpCodes.Call)
                            {
                                parameters.Add(typeof(ConstructorInfo));
                                get = Context.GetRefImport<MethodDef>("GetConstructor");
                            }
                            else
                            {
                                if (instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt)
                                {
                                    parameters.AddRange(new Type[] { typeof(MethodInfo), typeof(Type[]) });
                                    name = "EmitCall";
                                    addLdnull = true;
                                }
                                else
                                {
                                    parameters.Add(typeof(MethodInfo));
                                }

                                get = Context.GetRefImport<MethodDef>("GetMethod");
                                instructions.Add(OpCodes.Ldstr.ToInstruction(m.Name));
                            }

                            EmitTypeof(m.DeclaringType);
                            EmitTypeArray(m.MethodSig.Params);
                            instructions.Add(OpCodes.Call.ToInstruction(get));
                            if (addLdnull)
                                instructions.Add(OpCodes.Ldnull.ToInstruction());
                            break;

                    }

                    instructions.Add(OpCodes.Callvirt.ToInstruction(context.Module.Import(typeof(SRE.ILGenerator).GetMethod(name, parameters.ToArray()))));
                                      
                }


                Deserialize();
            }
        }


        private void Serialize()
        {
            Method.Body.SimplifyMacros(Method.Parameters);
            NewBody = new CilBody();

            Context.ILGenerator = new Local(Context.GetTypeImport<TypeSig>("ILGenerator"));
            NewBody.Variables.Add(Context.ILGenerator);
            Context.MethodInfo = new Local(Context.GetTypeImport<TypeSig>("MethodInfo"));
            NewBody.Variables.Add(Context.MethodInfo);
            Context.DynamicMethod = new Local(Context.GetTypeImport<TypeSig>("DynamicMethod"));
            NewBody.Variables.Add(Context.DynamicMethod);

            var instructions = NewBody.Instructions;
            var name = Guid.NewGuid().ToString();
            //Name of dynamic method
            instructions.Add(OpCodes.Ldstr.ToInstruction(name));
            //MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.Static
            instructions.Add(OpCodes.Ldc_I4.ToInstruction(0x16));
            //CallingConventions.Standard
            instructions.Add(OpCodes.Ldc_I4_1.ToInstruction());
            //Return Type
            EmitTypeof(Method.ReturnType.ToTypeDefOrRef());
            EmitTypeArray(GetSigs(Method.Parameters));
            //Owner Type
            EmitTypeof(Method.DeclaringType);

            instructions.Add(OpCodes.Ldc_I4_0.ToInstruction());
            //Dynamic method constructor call
            instructions.Add(OpCodes.Newobj.ToInstruction(Context.GetRefImport<MemberRef>(".ctor")));
            instructions.Add(OpCodes.Stloc.ToInstruction(Context.DynamicMethod));
            instructions.Add(OpCodes.Ldloc.ToInstruction(Context.DynamicMethod));
            instructions.Add(OpCodes.Call.ToInstruction(Context.GetRefImport<MethodDef>("GetCreatedMethodInfo")));
            instructions.Add(OpCodes.Stloc.ToInstruction(Context.MethodInfo));
            instructions.Add(OpCodes.Ldloc.ToInstruction(Context.MethodInfo));
            instructions.Add(OpCodes.Ldnull.ToInstruction());
            instructions.Add(OpCodes.Ceq.ToInstruction());
            Context.Branch = OpCodes.Brfalse.ToInstruction(NewBody.Instructions.Last());
            instructions.Add(Context.Branch);
            instructions.Add(OpCodes.Ldloc.ToInstruction(Context.DynamicMethod));
            instructions.Add(OpCodes.Callvirt.ToInstruction(Context.GetRefImport<MemberRef>("GetILGenerator")));
            instructions.Add(OpCodes.Stloc.ToInstruction(Context.ILGenerator));
        }


        private void TryTransform(Instruction instruction)
        {
            var instructions = NewBody.Instructions;
            if (Context.IsBranchTarget(instruction, out var label))
            {
                instructions.Add(OpCodes.Ldloc.ToInstruction(Context.ILGenerator));
                instructions.Add(OpCodes.Ldloc.ToInstruction(label));
                instructions.Add(OpCodes.Callvirt.ToInstruction(Context.GetRefImport<MemberRef>("MarkLabel")));
            }

            if (Context.IsExceptionStart(instruction))
            {
                instructions.Add(OpCodes.Ldloc.ToInstruction(Context.ILGenerator));
                instructions.Add(OpCodes.Callvirt.ToInstruction(Context.GetRefImport<MemberRef>("BeginExceptionBlock")));
                instructions.Add(OpCodes.Pop.ToInstruction());
            }
            else if (Context.IsHandlerStart(instruction, out var beginMethod, out var ex))
            {
                instructions.Add(OpCodes.Ldloc.ToInstruction(Context.ILGenerator));
                if (ex.HandlerType == ExceptionHandlerType.Catch)
                    EmitTypeof(ex.CatchType);
                instructions.Add(OpCodes.Callvirt.ToInstruction(beginMethod));
            }
            else if (Context.IsExceptionEnd(instruction))
            {
                instructions.Add(OpCodes.Ldloc.ToInstruction(Context.ILGenerator));
                instructions.Add(OpCodes.Callvirt.ToInstruction(Context.GetRefImport<MemberRef>("EndExceptionBlock")));
            }

        }


        private void InitializeExceptionHandlers()
        {
            if (!Method.Body.HasExceptionHandlers)
                return;

            foreach (var ex in Method.Body.ExceptionHandlers)
                Context.ExceptionHandlers.Add(ex);

        }
        private void InitializeLocals()
        {
            if (!Method.Body.HasVariables)
                return;

            var instructions = NewBody.Instructions;

            foreach (var local in Method.Body.Variables)
            {
                var localBuilder = new Local(Context.GetTypeImport<TypeSig>("LocalBuilder"));
                NewBody.Variables.Add(localBuilder);
                instructions.Add(OpCodes.Ldloc.ToInstruction(Context.ILGenerator));
                EmitTypeof(local.Type.ToTypeDefOrRef());
                instructions.Add(OpCodes.Ldc_I4_0.ToInstruction());
                instructions.Add(OpCodes.Callvirt.ToInstruction(Context.GetRefImport<MemberRef>("DeclareLocal")));
                instructions.Add(OpCodes.Stloc.ToInstruction(localBuilder));
                Context.LocalRefs.Add(local, localBuilder);
            }


        }

        private void InitializeBranches()
        {
            var instructions = NewBody.Instructions;

            var refs = new Dictionary<Instruction, Local>();
            foreach (var instruction in Method.Body.Instructions.Where(x => x.OpCode.OperandType == OperandType.InlineBrTarget))
            {
                Local lcl = null;
                if (Context.IsBranchTarget((Instruction)instruction.Operand, out var label))
                    lcl = label;
                else
                {
                    lcl = new Local(Context.GetTypeImport<TypeSig>("Label"));
                    NewBody.Variables.Add(lcl);
                    instructions.Add(OpCodes.Ldloc.ToInstruction(Context.ILGenerator));
                    instructions.Add(OpCodes.Callvirt.ToInstruction(Context.GetRefImport<MemberRef>("DefineLabel")));
                    instructions.Add(OpCodes.Stloc.ToInstruction(lcl));
                }

                Context.BranchRefs.Add(instruction, lcl);
            }

        }

        private void Deserialize()
        {
            var instructions = NewBody.Instructions;
            instructions.Add(OpCodes.Ldloc.ToInstruction(Context.DynamicMethod));
            instructions.Add(OpCodes.Callvirt.ToInstruction(Context.GetRefImport<MemberRef>("GetBaseDefinition")));
            instructions.Add(OpCodes.Stloc.ToInstruction(Context.MethodInfo));
            instructions.Add(OpCodes.Ldloc.ToInstruction(Context.DynamicMethod));
            instructions.Add(OpCodes.Ldloc.ToInstruction(Context.MethodInfo));
            instructions.Add(OpCodes.Call.ToInstruction(Context.GetRefImport<MethodDef>("SetMethodInfo")));
            var branchTarget = OpCodes.Ldloc.ToInstruction(Context.MethodInfo);
            Context.Branch.Operand = branchTarget;
            instructions.Add(branchTarget);

            instructions.Add(OpCodes.Ldnull.ToInstruction());
            var count = Method.MethodSig.Params.Count;

            EmitArray(Context.GetTypeImport<TypeSig>("Object"), GetSigs(Method.Parameters),
            (index, s, instrs) =>
            {
                instrs.Add(OpCodes.Dup.ToInstruction());
                instrs.Add(OpCodes.Ldc_I4.ToInstruction(index));
                instrs.Add(OpCodes.Ldarg.ToInstruction(Method.Parameters.ElementAt(index)));
                instrs.Add(OpCodes.Box.ToInstruction(Method.Parameters[index].Type.ToTypeDefOrRef()));
                instrs.Add(OpCodes.Stelem_Ref.ToInstruction());
            }
            );

            instructions.Add(OpCodes.Callvirt.ToInstruction(Context.GetRefImport<MemberRef>("Invoke")));

            if (Method.HasReturnType)
                instructions.Add(OpCodes.Unbox_Any.ToInstruction(Method.ReturnType.ToTypeDefOrRef()));
            else
                instructions.Add(OpCodes.Pop.ToInstruction());


            instructions.Add(OpCodes.Ret.ToInstruction());



            Method.FreeMethodBody();
            Method.Body = NewBody;
            NewBody.OptimizeMacros();
        }
        private MemberRef OpCodeToRef(string opName)
        {
            return Context.Module.Import(typeof(SRE.OpCodes).GetField(opName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Static));
        }


        private void EmitArray(TypeSig type, IList<TypeSig> sigs, Action<int, IList<TypeSig>, IList<Instruction>> emit)
        {
            var instructions = NewBody.Instructions;
            var count = sigs.Count;
            instructions.Add(OpCodes.Ldc_I4.ToInstruction(count));
            instructions.Add(OpCodes.Newarr.ToInstruction(type.ToTypeDefOrRef()));
            for (int i = 0; i < sigs.Count; i++)
                emit(i, sigs, instructions);
        }

        private void EmitTypeof(ITypeDefOrRef type)
        {
            NewBody.Instructions.Add(OpCodes.Ldtoken.ToInstruction(type));
            NewBody.Instructions.Add(OpCodes.Call.ToInstruction(Context.GetRefImport<MemberRef>("GetTypeFromHandle")));
        }


        private void EmitTypeArray(IList<TypeSig> sigs)
        {
            EmitArray(Context.GetTypeImport<TypeSig>("Type"), sigs,
            (index, s, instrs) =>
            {
                instrs.Add(OpCodes.Dup.ToInstruction());
                instrs.Add(OpCodes.Ldc_I4.ToInstruction(index));
                EmitTypeof(s[index].ToTypeDefOrRef());
                instrs.Add(OpCodes.Stelem_Ref.ToInstruction());
            }
            );
        }

        private List<TypeSig> GetSigs(ParameterList list)
        {
            var sigs = new List<TypeSig>();
            foreach (var parameter in list)
                sigs.Add(parameter.Type);

            return sigs;
        }

    }
}
