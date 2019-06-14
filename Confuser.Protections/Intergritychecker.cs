using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Confuser.Core;
using Confuser.Core.Helpers;
using Confuser.Core.Services;
using Confuser.Renamer;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;

namespace Confuser.Protections
{
    [BeforeProtection("Ki.AntiTamper")]
    internal class IntegrityChecker : Protection
    {
        public const string _Id = "Integrity";
        public const string _FullId = "Ki.Integrity";

        public override string Name
        {
            get { return "Integrity Protection"; }
        }

        public override string Description
        {
            get { return "This protection hashs the module to preventing file modifications."; }
        }

        public override string Id
        {
            get { return _Id; }
        }

        public override string FullId
        {
            get { return _FullId; }
        }

        public override ProtectionPreset Preset
        {
            get { return ProtectionPreset.None; }
        }

        protected override void Initialize(ConfuserContext context)
        {
            //
        }

        protected override void PopulatePipeline(ProtectionPipeline pipeline)
        {
            pipeline.InsertPreStage(PipelineStage.OptimizeMethods, new IntegrityPhase(this));
            pipeline.InsertPreStage(PipelineStage.EndModule, new HashPhase(this));
        }

        class IntegrityPhase : ProtectionPhase
        {
            public IntegrityPhase(IntegrityChecker parent)
                : base(parent) { }

            public override ProtectionTargets Targets
            {
                get { return ProtectionTargets.Modules; }
            }

            public override string Name
            {
                get { return ""; }
            }

            protected override void Execute(ConfuserContext context, ProtectionParameters parameters)
            {
                TypeDef rtType = context.Registry.GetService<IRuntimeService>().GetRuntimeType("Confuser.Runtime.Integrity");

                var marker = context.Registry.GetService<IMarkerService>();
                var name = context.Registry.GetService<INameService>();

                foreach (ModuleDef module in parameters.Targets.OfType<ModuleDef>())
                {
                    IEnumerable<IDnlibDef> members = InjectHelper.Inject(rtType, module.GlobalType, module);

                    MethodDef cctor = module.GlobalType.FindStaticConstructor();
                    var init = (MethodDef)members.Single(method => method.Name == "Initialize");
                    cctor.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, init));

                    foreach (IDnlibDef member in members)
                        name.MarkHelper(member, marker, (Protection)Parent);
                }
            }
        }
    }

    internal class HashPhase : ProtectionPhase
    {
        public HashPhase(ConfuserComponent parent) : base(parent)
        {
        }

        public override ProtectionTargets Targets => ProtectionTargets.Modules;

        public override string Name => "Hash Phase";

        protected override void Execute(ConfuserContext context, ProtectionParameters parameters)
        {
            context.CurrentModuleWriterListener.OnWriterEvent += CurrentModuleWriterListener_OnWriterEvent;
        }

        private void CurrentModuleWriterListener_OnWriterEvent(object sender, ModuleWriterListenerEventArgs e)
        {
            var writer = (ModuleWriterBase)sender;
            if (e.WriterEvent == dnlib.DotNet.Writer.ModuleWriterEvent.End)
            {
                HashFile(writer);
            }
        }

        internal byte[] ReadFully(Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }

        internal string MD5(byte[] metin)
        {
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            byte[] btr = metin;
            btr = md5.ComputeHash(btr);
            StringBuilder sb = new StringBuilder();

            foreach (byte ba in btr)
            {
                sb.Append(ba.ToString("x2").ToLower());
            }
            return sb.ToString();
        }

        private void HashFile(ModuleWriterBase writer)
        {
            var st = new StreamReader(writer.DestinationStream);
            var a = new BinaryReader(st.BaseStream);
            a.BaseStream.Position = 0;
            var data = a.ReadBytes((int)(st.BaseStream.Length - 32));

            var md5 = MD5(data);

            var enc = Encoding.ASCII.GetBytes(md5);

            writer.DestinationStream.Position = writer.DestinationStream.Length - enc.Length;
            writer.DestinationStream.Write(enc, 0, enc.Length);
        }
    }
}
