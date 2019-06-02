using System;
using System.Linq;
using Confuser.Core;
using dnlib.DotNet;

using Microsoft.VisualBasic.CompilerServices;
using Microsoft.VisualBasic;
using System.Collections.Generic;
using dnlib.DotNet.Emit;
using System.Security.Cryptography;
using Confuser.Protections.Compress;
using dnlib.DotNet.Writer;
using dnlib.DotNet.MD;

namespace Confuser.Protections
{
    // Token: 0x02000002 RID: 2
    internal class NamePhaseProc : Protection
    {
        // Token: 0x06000006 RID: 6 RVA: 0x000020EF File Offset: 0x000002EF
        protected override void Initialize(ConfuserContext context)
        {
        }

        // Token: 0x06000007 RID: 7 RVA: 0x000020F1 File Offset: 0x000002F1
        protected override void PopulatePipeline(ProtectionPipeline pipeline)
        {
            pipeline.InsertPreStage(PipelineStage.WriteModule, new NamePhaseProc.NamePhase(this));
        }

        // Token: 0x17000002 RID: 2
        public override string Description
        {
            // Token: 0x06000002 RID: 2 RVA: 0x000020D7 File Offset: 0x000002D7
            get
            {
                return "Renames the module and assembly.";
            }
        }
      
        // Token: 0x17000004 RID: 4
        public override string FullId
        {
            // Token: 0x06000004 RID: 4 RVA: 0x000020E5 File Offset: 0x000002E5
            get
            {
                return "Ki.RenameModule";
            }
        }

        // Token: 0x17000003 RID: 3
        public override string Id
        {
            // Token: 0x06000003 RID: 3 RVA: 0x000020DE File Offset: 0x000002DE
            get
            {
                return "Rename Module";
            }
        }

        // Token: 0x17000001 RID: 1
        public override string Name
        {
            // Token: 0x06000001 RID: 1 RVA: 0x000020D0 File Offset: 0x000002D0
            get
            {
                return "Rename Module";
            }
        }

        // Token: 0x17000005 RID: 5
        public override ProtectionPreset Preset
        {
            // Token: 0x06000005 RID: 5 RVA: 0x000020EC File Offset: 0x000002EC
            get
            {
                return ProtectionPreset.Minimum;
            }
        }

        // Token: 0x04000002 RID: 2
        public const string _FullId = "Ki.RenameModule";

        // Token: 0x04000001 RID: 1
        public const string _Id = "Rename";
        public static string Random(int len)
        {
            string str = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRST123456789他是说汉语的Ⓟⓡⓞⓣⓔⓒⓣｱ尺Ծｲ乇ζｲ123456789αβγδεζηθικλμνξοπρστυφχψω卍卍卍卍卍卍卍";
            char[] chArray = new char[(len - 1) + 1];
            int num = len - 1;
            for (int i = 0; i <= num; i++)
            {
                chArray[i] = str[(int)Math.Round((double)Conversion.Int((float)(VBMath.Rnd() * str.Length)))];
            }
            return new string(chArray);
        }
        // Token: 0x02000003 RID: 3
        private class NamePhase : ProtectionPhase
        {
            // Token: 0x06000009 RID: 9 RVA: 0x00002108 File Offset: 0x00000308
            public NamePhase(NamePhaseProc parent) : base(parent)
            {
            }
            Random cheekycunt = new Random();
            // Token: 0x0600000C RID: 12 RVA: 0x0000211C File Offset: 0x0000031C
            protected override void Execute(ConfuserContext context, ProtectionParameters parameters)
            {
                foreach (ModuleDefMD module in parameters.Targets.OfType<ModuleDef>())
                {
                    module.Name = "EternalDeath-Protector-Private2019 - " + cheekycunt.Next(100000, 1000000000).ToString() + cheekycunt.Next(100000, 1000000000).ToString() + cheekycunt.Next(100000, 1000000000).ToString();
                    module.Assembly.Name = "вє∂ѕ ρяσтє¢тσя - " + cheekycunt.Next(100000, 1000000000).ToString() + cheekycunt.Next(100000, 1000000000).ToString() + cheekycunt.Next(100000, 1000000000).ToString();
                    
                }
            }

            // Token: 0x17000007 RID: 7
            public override string Name
            {
                // Token: 0x0600000B RID: 11 RVA: 0x00002115 File Offset: 0x00000315
                get
                {
                    return "Renaming";
                }
            }

            // Token: 0x17000006 RID: 6
            public override ProtectionTargets Targets
            {
                // Token: 0x0600000A RID: 10 RVA: 0x00002111 File Offset: 0x00000311
                get
                {
                    return ProtectionTargets.Modules;
                }
            }
        }
    }
}
