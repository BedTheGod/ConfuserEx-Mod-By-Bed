using Confuser.Core;
using Confuser.Core.Helpers;
using Confuser.Core.Services;
using Confuser.Renamer;
using Confuser.Runtime.AntiMemoryEditing;
using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Confuser.Protections.AntiMemoryEditing {
	internal class MemoryEditInjectPhase : ProtectionPhase {
		public override ProtectionTargets Targets => ProtectionTargets.Fields;
		public override string Name => "Memory obfuscation type injection";

		public MemoryEditInjectPhase(ConfuserComponent parent) : base(parent) { }

		protected override void Execute(ConfuserContext context, ProtectionParameters parameters) {
			//we only want to do this if any of the targets are in this module
			if (!parameters.Targets.Any(a => a is FieldDef fd && fd.Module == context.CurrentModule))
				return;

			var m = context.CurrentModule;

			//get services
			var service = context.Registry.GetService<IMemoryEditService>();
			var marker = context.Registry.GetService<IMarkerService>();
			var name = context.Registry.GetService<INameService>();

			//import type
			var obfType = GetRuntimeType(typeof(ObfuscatedValue<>));
			var newType = new TypeDefUser(obfType.Namespace, obfType.Name, new Importer(m).Import(typeof(object)));
			newType.GenericParameters.Add(new GenericParamUser(0, GenericParamAttributes.NonVariant, "T"));
			m.Types.Add(newType);
			var injected = InjectHelper.Inject(obfType, newType, m);
			service.SetWrapperType(m, newType);

			//find read/write methods
			var methods = newType.FindMethods("op_Implicit").ToArray();
			service.SetReadMethod(m, methods[0]);
			service.SetWriteMethod(m, methods[1]);

			//mark type for renaming
			name.MarkHelper(newType, marker, Parent);

			//workaround for issue below
			foreach (IDnlibDef def in injected)
				marker.Mark(def, Parent);

			//TODO: this breaks it. Why?
			//foreach (MethodDef method in newType.Methods)
			//    name.MarkHelper(method, marker, Parent);
			//foreach (FieldDef field in newType.Fields)
			//    name.MarkHelper(field, marker, Parent);
			//foreach (PropertyDef property in newType.Properties)
			//    name.MarkHelper(property, marker, Parent);
		}

		private static readonly Lazy<ModuleDefMD> RuntimeModule = new Lazy<ModuleDefMD>(() => ModuleDefMD.Load(typeof(ObfuscatedValue<>).Module));

		public static TypeDef GetRuntimeType(Type t) => RuntimeModule.Value.Find(t.FullName, true);
	}
}
