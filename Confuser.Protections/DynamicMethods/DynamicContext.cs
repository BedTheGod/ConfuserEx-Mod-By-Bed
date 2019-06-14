using System;
using System.Reflection;
using System.Reflection.Emit;
using SR = System.Reflection;
using DN = dnlib.DotNet.Emit;
using dnlib.DotNet;
using System.Collections.Generic;
using dnlib.DotNet.Emit;
using System.Linq;
using Perplex.Core;
using Perplex.Core.Services;

namespace Perplex.NF.Protections.DynamicMethods
{
    public class DynamicContext : ImportContext
    {
        public Dictionary<Instruction, Local> BranchRefs;
        public Dictionary<Local, Local> LocalRefs;
        public List<DN.ExceptionHandler> ExceptionHandlers;
        public IRandomService Random;
        public Instruction Branch;
        public Local ILGenerator;
        public Local DynamicMethod;
        public Local MethodInfo;
        public DynamicContext(ModuleDef module) : base(module)
        {
            LocalRefs = new Dictionary<Local, Local>();
            BranchRefs = new Dictionary<Instruction, Local>();
            ExceptionHandlers = new List<DN.ExceptionHandler>();
        }

        public override void Initialize()
        {
            AddTypeImport(Module.ImportAsTypeSig(typeof(ILGenerator)));
            AddTypeImport(Module.ImportAsTypeSig(typeof(DynamicMethod)));
            AddTypeImport(Module.ImportAsTypeSig(typeof(LocalBuilder)));
            AddTypeImport(Module.ImportAsTypeSig(typeof(Type)));
            AddTypeImport(Module.ImportAsTypeSig(typeof(MethodInfo)));
            AddTypeImport(Module.ImportAsTypeSig(typeof(Label)));
            AddTypeImport(Module.ImportAsTypeSig(typeof(object)));
            AddRefImport(Module.Import(typeof(Type).GetMethod("GetTypeFromHandle", new Type[] { typeof(RuntimeTypeHandle) })));
            AddRefImport(Module.Import(typeof(DynamicMethod).GetConstructor(new Type[] { typeof(string), typeof(SR.MethodAttributes), typeof(CallingConventions), typeof(Type), typeof(Type[]), typeof(Type), typeof(bool) })));
            AddRefImport(Module.Import(typeof(DynamicMethod).GetMethod("GetILGenerator", new Type[0])));
            AddRefImport(Module.Import(typeof(ILGenerator).GetMethod("DeclareLocal", new Type[] { typeof(Type), typeof(bool) })));
            AddRefImport(Module.Import(typeof(Type).GetMethod("get_Module")));
            AddRefImport(Module.Import(typeof(MethodBase).GetMethod("Invoke", new Type[] { typeof(object), typeof(object[]) })));
            AddRefImport(Module.Import(typeof(ILGenerator).GetMethod("DefineLabel")));
            AddRefImport(Module.Import(typeof(ILGenerator).GetMethod("MarkLabel", new Type[] { typeof(Label) })));
            AddRefImport(Module.Import(typeof(MethodInfo).GetMethod("GetBaseDefinition")));
            AddRefImport(Module.Import(typeof(Type).GetMethod("GetField", new Type[] { typeof(string), typeof(BindingFlags)})));
            AddRefImport(Module.Import(typeof(ILGenerator).GetMethod("BeginExceptionBlock")));
            AddRefImport(Module.Import(typeof(ILGenerator).GetMethod("BeginCatchBlock", new Type[] { typeof(Type) })));
            AddRefImport(Module.Import(typeof(ILGenerator).GetMethod("BeginExceptFilterBlock")));
            AddRefImport(Module.Import(typeof(ILGenerator).GetMethod("BeginFinallyBlock")));
            AddRefImport(Module.Import(typeof(ILGenerator).GetMethod("BeginFaultBlock")));
            AddRefImport(Module.Import(typeof(ILGenerator).GetMethod("EndExceptionBlock")));

        }


        public bool IsBranchTarget(Instruction instruction, out Local label)
        {
            label = BranchRefs.FirstOrDefault(x => ((Instruction)x.Key.Operand) == instruction).Value;

            return label != null;
        }

        public bool IsExceptionStart(Instruction instruction)
        {
            return ExceptionHandlers.FirstOrDefault(x => x.TryStart == instruction) != null;
        }


        public bool IsExceptionEnd(Instruction instruction)
        {
            return ExceptionHandlers.FirstOrDefault(x => x.HandlerEnd == instruction) != null;
        }

        public bool IsHandlerStart(Instruction instruction, out MemberRef beginMethod, out DN.ExceptionHandler ex)
        {
            ex = ExceptionHandlers.FirstOrDefault(x => x.HandlerStart == instruction);
            beginMethod = null;
            if (ex == null)
                return false;

            string handlerName = (ex.HandlerType == ExceptionHandlerType.Filter) ? FormatName("ExceptFilter") : FormatName(ex.HandlerType.ToString());
            beginMethod = GetRefImport<MemberRef>(handlerName);
            return true;
        }


        private string FormatName(string handlerName)
        {
            return "Begin" + handlerName + "Block";
        }

     
    }
}
