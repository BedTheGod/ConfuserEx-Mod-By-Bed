using Confuser.Core;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Confuser.Protections.AntiMemoryEditing {
	public class MemoryEditApplyPhase : ProtectionPhase {
		public override ProtectionTargets Targets => ProtectionTargets.Fields;
		public override string Name => "Apply memory protection";

		public MemoryEditApplyPhase(ConfuserComponent parent) : base(parent) { }

		protected override void Execute(ConfuserContext context, ProtectionParameters parameters) {
			var m = context.CurrentModule;

			//get type
			var service = context.Registry.GetService<IMemoryEditService>();
			var wrapperType = service.GetWrapperType(m);

			//if we didn't inject a wrapper type, don't run anything else
			if (wrapperType == null) return;

			//get methods
			var methodRead = service.GetReadMethod(m);
			var methodWrite = service.GetWriteMethod(m);

			//wrap all fields in this type
			foreach (FieldDef field in service.GetFields())
				field.FieldType = new GenericInstSig((ClassOrValueTypeSig)wrapperType.ToTypeSig(), field.FieldType.ToTypeDefOrRefSig());

			//run through all methods, patching r/w
			context.Logger.Debug("Looping through all types to patch access to obfuscated values");
			foreach (MethodDef t in context.CurrentModule.GetTypes().SelectMany(a => a.Methods).WithProgress(context.Logger)) {
				if (!t.HasBody || !t.Body.HasInstructions) continue;

				for (int i = 0; i < t.Body.Instructions.Count; i++) {
					Instruction instr = t.Body.Instructions[i];

					if (instr.Operand == null) continue;
					if (!(instr.Operand is FieldDef fd) || !(fd.FieldType is GenericInstSig sig)) continue;
					if (!new SigComparer().Equals(sig.GenericType.TypeDef, wrapperType)) continue;

					switch (instr.OpCode.Code) {
						case Code.Ldfld:
						case Code.Ldsfld: {
								//loading field: add a call after it
								t.Body.Instructions.Insert(i + 1, new Instruction(OpCodes.Call, FindReadMethod(m, sig, methodRead)));
								i++;
								break;
							}
						case Code.Stfld:
						case Code.Stsfld: {
								//storing field: add a call before it
								t.Body.Instructions.Insert(i, new Instruction(OpCodes.Call, FindReadMethod(m, sig, methodWrite)));
								i++;
							}
							break;
					}
				}
			}
		}

		private static MemberRefUser FindReadMethod(ModuleDef m, GenericInstSig sig, IMethod method) {
			return new MemberRefUser(m, method.Name, method.MethodSig) { Class = new TypeSpecUser(sig) };
		}
	}
}
