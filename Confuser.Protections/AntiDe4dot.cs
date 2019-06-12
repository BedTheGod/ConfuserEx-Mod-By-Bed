using Confuser.Core;
using Confuser.Core.Services;
using Confuser.Renamer;
using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Confuser.Protections
{
    public class AntiDe4DotProtection : Protection
    {
        public override ProtectionPreset Preset => ProtectionPreset.Minimum;

        public override string Name => "Anti De4Dot";

        public override string Description => "Prevents usage of De4Dot.";

        public override string Id => "anti de4dot";

        public override string FullId => "Ki.AntiDe4Dot";

        protected override void Initialize(ConfuserContext context) { }

        protected override void PopulatePipeline(ProtectionPipeline pipeline)
        {
            pipeline.InsertPreStage(PipelineStage.WriteModule, new AntiDe4DotPhase(this));
        }

        private class AntiDe4DotPhase : ProtectionPhase
        {
            public AntiDe4DotPhase(AntiDe4DotProtection parent) : base(parent) { }

            public override ProtectionTargets Targets => ProtectionTargets.Modules;

            public override string Name => "Anti De4Dot";

            protected override void Execute(ConfuserContext context, ProtectionParameters parameters)
            {
                var marker = context.Registry.GetService<IMarkerService>();
                var name = context.Registry.GetService<INameService>();
                foreach (ModuleDef module in parameters.Targets.OfType<ModuleDef>().WithProgress(context.Logger))
                {
                    InterfaceImpl interfaceM = new InterfaceImplUser(module.GlobalType);

                    TypeDef typeDef1 = new TypeDefUser("", name.RandomName(), module.CorLibTypes.GetTypeRef("System", "Attribute"));
                    InterfaceImpl interface1 = new InterfaceImplUser(typeDef1);
                    module.Types.Add(typeDef1);
                    typeDef1.Interfaces.Add(interface1);
                    typeDef1.Interfaces.Add(interfaceM);
                    marker.Mark(typeDef1, Parent);
                    name.SetCanRename(typeDef1, false);

                    for(int i = 0; i < new Random().Next(4, 15); i++)
                    {
                        TypeDef typeDef2 = new TypeDefUser("", name.RandomName(), module.CorLibTypes.GetTypeRef("System", "Attribute"));
                        InterfaceImpl interface2 = new InterfaceImplUser(typeDef2);
                        module.Types.Add(typeDef2);
                        typeDef2.Interfaces.Add(interface2);
                        typeDef2.Interfaces.Add(interfaceM);
                        typeDef2.Interfaces.Add(interface1);
                        marker.Mark(typeDef2, Parent);
                        name.SetCanRename(typeDef2, false);
                    }               
                }
            }
        }
    }
}
