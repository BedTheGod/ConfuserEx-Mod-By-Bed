using Crystalizer.Helpers;
using Crystalizer.Helpers.Generator;

using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;

using System;
using SR = System.Reflection;
using System.Collections.Generic;

namespace Crystalizer.Protections
{
    class ILtoVIL
    {
        public MethodDef m;
        public IList<Instruction> mIns;


        public ILtoVIL(MethodDef ToVirtualize)
        {
            m = ToVirtualize;
        }




        public void AddIL(Instruction IL)
        {
            m.Body.Instructions.Add(OpCodes.Dup.ToInstruction());


            Importer importer = new Importer(m.Module);
            var OpcodeImported = importer.ImportAsTypeSig(typeof(System.Reflection.Emit.OpCode));



            if (IL.OpCode == OpCodes.Ldstr)
            {
                var Opcode = new MemberRefUser(m.Module, "Ldstr",
                                MethodSig.CreateStatic(m.Module.CorLibTypes.Void, m.Module.CorLibTypes.Object),
                                m.Module.CorLibTypes.GetTypeRef("System.Reflection.Emit", "OpCodes"));


                m.Body.Instructions.Add(OpCodes.Ldstr.ToInstruction(Opcode));
            }

            if (IL.OpCode == OpCodes.Call)
            {
                var Opcode = new MemberRefUser(m.Module, "Call",
                                MethodSig.CreateStatic(m.Module.CorLibTypes.Void, m.Module.CorLibTypes.Object),
                                m.Module.CorLibTypes.GetTypeRef("System.Reflection.Emit", "OpCodes"));


                m.Body.Instructions.Add(OpCodes.Call.ToInstruction(Opcode));
            }














            MemberRef EmitMember = new MemberRefUser(m.Module, "Emit",
            MethodSig.CreateStatic(m.Module.CorLibTypes.Void, OpcodeImported),
            new TypeRefUser(m.Module, "System.Reflection.Emit", "ILGenerator", m.Module.CorLibTypes.AssemblyRef));



            m.Body.Instructions.Add(OpCodes.Callvirt.ToInstruction(EmitMember));




        }

        public void Convert()
        {
            mIns = m.Body.Instructions;

            m.Body.Instructions.Clear();
            m.Body.Variables.Clear();
            m.Body.ExceptionHandlers.Clear();


            //var DynamicMethodCCtor = typeof(System.Reflection.Emit.DynamicMethod).GetConstructor(new[] { typeof(string), typeof(Type), typeof(Type) });

            Importer importer = new Importer(m.Module);
            var TypeImported = importer.ImportAsTypeSig(typeof(System.Type));
            var TypeSImported = importer.ImportAsTypeSig(typeof(System.Type[]));


            MemberRef DynamicMethodCCtor = new MemberRefUser(m.Module, ".ctor",
            MethodSig.CreateStatic(m.Module.CorLibTypes.Void, m.Module.CorLibTypes.String, TypeImported, TypeSImported),
            new TypeRefUser(m.Module, "System.Reflection.Emit", "DynamicMethod", m.Module.CorLibTypes.AssemblyRef));


            m.Body.Instructions.Add(OpCodes.Ldstr.ToInstruction("Method"));
            m.Body.Instructions.Add(OpCodes.Ldnull.ToInstruction());
            m.Body.Instructions.Add(OpCodes.Ldnull.ToInstruction());
            m.Body.Instructions.Add(OpCodes.Newobj.ToInstruction(DynamicMethodCCtor));
            m.Body.Instructions.Add(OpCodes.Dup.ToInstruction());

            MemberRef GetILGeneratorMember = new MemberRefUser(m.Module, "GetILGenerator", MethodSig.CreateStatic(m.Module.CorLibTypes.Void), new TypeRefUser(m.Module, "System.Reflection.Emit", "DynamicMethod", m.Module.CorLibTypes.AssemblyRef));



            m.Body.Instructions.Add(OpCodes.Callvirt.ToInstruction(GetILGeneratorMember));





            m.Body.Instructions.Add(OpCodes.Ldnull.ToInstruction());
            m.Body.Instructions.Add(OpCodes.Ldnull.ToInstruction());

            MemberRef InitMember = new MemberRefUser(m.Module, "Invoke",
                        MethodSig.CreateStatic(m.Module.CorLibTypes.Void, m.Module.CorLibTypes.Object, m.Module.CorLibTypes.Object),
                        new TypeRefUser(m.Module, "System.Reflection", "MethodBase", m.Module.CorLibTypes.AssemblyRef));



            m.Body.Instructions.Add(OpCodes.Callvirt.ToInstruction(InitMember));
            m.Body.Instructions.Add(OpCodes.Pop.ToInstruction());
            m.Body.Instructions.Add(OpCodes.Ret.ToInstruction());








        }



    }
}
