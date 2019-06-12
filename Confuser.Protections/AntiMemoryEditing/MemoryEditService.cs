using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Confuser.Protections.AntiMemoryEditing {
	//This is probably not needed, but it looks nice :)
	internal interface IMemoryEditService {
		void AddToList(FieldDef d);

		IEnumerable<FieldDef> GetFields();

		TypeDef GetWrapperType(ModuleDef m);
		void SetWrapperType(ModuleDef m, TypeDef t);

		IMethod GetReadMethod(ModuleDef mod);
		IMethod GetWriteMethod(ModuleDef mod);
		void SetReadMethod(ModuleDef mod, IMethod m);
		void SetWriteMethod(ModuleDef mod, IMethod m);
	}

	internal class MemoryEditService : IMemoryEditService {
		private readonly List<FieldDef> _fields = new List<FieldDef>();
		private readonly Dictionary<ModuleDef, IMethod> _readMethods = new Dictionary<ModuleDef, IMethod>();
		private readonly Dictionary<ModuleDef, IMethod> _writeMethods = new Dictionary<ModuleDef, IMethod>();
		private readonly Dictionary<ModuleDef, TypeDef> _wrapperTypes = new Dictionary<ModuleDef, TypeDef>();

		public void AddToList(FieldDef d) => _fields.Add(d);
		public IEnumerable<FieldDef> GetFields() => _fields;

		public TypeDef GetWrapperType(ModuleDef mod) => _wrapperTypes.ContainsKey(mod) ? _wrapperTypes[mod] : null;
		public void SetWrapperType(ModuleDef mod, TypeDef t) => _wrapperTypes[mod] = t;

		public IMethod GetReadMethod(ModuleDef mod) => _readMethods[mod];
		public IMethod GetWriteMethod(ModuleDef mod) => _writeMethods[mod];
		public void SetReadMethod(ModuleDef mod, IMethod m) => _readMethods[mod] = m;
		public void SetWriteMethod(ModuleDef mod, IMethod m) => _writeMethods[mod] = m;
	}
}
