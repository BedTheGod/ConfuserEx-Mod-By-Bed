using dnlib.DotNet;
using System.Collections.Generic;
using System.Linq;

namespace Perplex.Core
{
    public abstract class ImportContext
    {
        private List<IMemberRef> importedRefs = new List<IMemberRef>();
        private List<IType> importedTypes  = new List<IType>();

        public ModuleDef Module { get; private set; }
        public ImportContext(ModuleDef module)
        {
            Module = module;
        }
        public abstract void Initialize();

        public void AddRefImport(IMemberRef mRef)
        {
            importedRefs.Add(mRef);
        }
        public void AddTypeImport(IType type)
        {
            importedTypes.Add(type);
        }
        public T GetRefImport<T>(string name)
        {
            return (T)importedRefs.FirstOrDefault(x => x.Name == name);
        }

        public T GetTypeImport<T>(string name)
        {
            return (T)importedTypes.FirstOrDefault(x => x.Name == name);
        }

    }
}
