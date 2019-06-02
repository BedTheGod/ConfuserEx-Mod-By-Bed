using System;
using System.Collections.Generic;
using System.Linq;
using Confuser.Core;
using Confuser.Core.Helpers;
using Confuser.Core.Services;
using Confuser.Renamer;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace Confuser.Protections
{
    // Token: 0x02000074 RID: 116
  
   
    internal class EraseHeadersProtection : Protection
    {
        // Token: 0x060001E9 RID: 489 RVA: 0x00011C8C File Offset: 0x0000FE8C
        protected override void Initialize(ConfuserContext context)
        {
        }

        // Token: 0x060001EA RID: 490 RVA: 0x00011C8E File Offset: 0x0000FE8E
        protected override void PopulatePipeline(ProtectionPipeline pipeline)
        {
           
            pipeline.InsertPreStage(PipelineStage.EndModule, new EraseHeadersProtection.ErasePhase(this));
           
            

        }
       public static bool first;
        // Token: 0x17000053 RID: 83
        public override string Description
        {
            // Token: 0x060001E5 RID: 485 RVA: 0x00011C74 File Offset: 0x0000FE74
            get
            {
                return "Eternaldeath erease headers";
            }
        }

        // Token: 0x17000055 RID: 85
        public override string FullId
        {
            // Token: 0x060001E7 RID: 487 RVA: 0x00011C82 File Offset: 0x0000FE82
            get
            {
                return "Ki.EraseHeaders";
            }
        }

        // Token: 0x17000054 RID: 84
        public override string Id
        {
            // Token: 0x060001E6 RID: 486 RVA: 0x00011C7B File Offset: 0x0000FE7B
            get
            {
                return "erase headers";
            }
        }

        // Token: 0x17000052 RID: 82
        public override string Name
        {
            // Token: 0x060001E4 RID: 484 RVA: 0x00011C6D File Offset: 0x0000FE6D
            get
            {
                return "erase headers Protection";
            }
        }

        // Token: 0x17000056 RID: 86
        public override ProtectionPreset Preset
        {
            // Token: 0x060001E8 RID: 488 RVA: 0x00011C89 File Offset: 0x0000FE89
            get
            {
                return ProtectionPreset.Normal;
            }
        }

        // Token: 0x04000174 RID: 372
        public const string _FullId = "Ki.EraseHeaders";

        // Token: 0x04000173 RID: 371
        public const string _Id = "erase headers";

        // Token: 0x02000075 RID: 117
        private class ErasePhase : ProtectionPhase
        {
            // Token: 0x060001EC RID: 492 RVA: 0x00011CA5 File Offset: 0x0000FEA5
            public ErasePhase(EraseHeadersProtection parent) : base(parent)
            {
            }

            // Token: 0x060001EF RID: 495 RVA: 0x00011CCC File Offset: 0x0000FECC
            protected override void Execute(ConfuserContext context, ProtectionParameters parameters)
            {
                TypeDef rtType = context.Registry.GetService<IRuntimeService>().GetRuntimeType("Confuser.Runtime.EraseHeaders");
                IMarkerService marker = context.Registry.GetService<IMarkerService>();
                INameService name = context.Registry.GetService<INameService>();
                foreach (ModuleDef module in parameters.Targets.OfType<ModuleDef>())
                {
                    IEnumerable<IDnlibDef> members = InjectHelper.Inject(rtType, module.GlobalType, module);
                    MethodDef cctor = module.GlobalType.FindStaticConstructor();
  
                    MethodDef init = (MethodDef)members.Single((IDnlibDef method) => method.Name == "Initialize");
                    cctor.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, init));
                    
     

                    foreach (IDnlibDef member in members)
                    {
                        name.MarkHelper(member, marker, (Protection)base.Parent);
                    }
                }
            }

            // Token: 0x17000058 RID: 88
            public override string Name
            {
                // Token: 0x060001EE RID: 494 RVA: 0x00011CB2 File Offset: 0x0000FEB2
                get
                {
                    return "Erasing Headers";
                }
            }

            // Token: 0x17000057 RID: 87
            public override ProtectionTargets Targets
            {
                // Token: 0x060001ED RID: 493 RVA: 0x00011CAE File Offset: 0x0000FEAE
                get
                {
                    return ProtectionTargets.Modules;
                }
            }
        }
    }
}
