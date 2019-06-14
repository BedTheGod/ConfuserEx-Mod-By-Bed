using System.Collections.Generic;
using dnlib.DotNet;
using Perplex.Core;
using Perplex.Core.Helpers;
using Perplex.Core.Services;

namespace Perplex.NF.Protections.DynamicMethods
{
    public class InjectPhase : ProtectionPhase
    {
        public override ProtectionTargets Targets => ProtectionTargets.None;

        public override void Execute(PerplexContext context, IList<IDnlibDef> targets)
        {
            IRuntimeService runtimeService = context.Registry.GetService<IRuntimeService>();
            var members = RuntimeInjector.Inject(runtimeService.GetRuntimeType("DynamicMethods"), context.Module.GlobalType, context.Module);
            var ctx = new DynamicContext(context.Module);
            foreach(var member in members)
            {
                if (member is MethodDef mDef)
                    ctx.AddRefImport(mDef);
            }
            ctx.Initialize();
            ctx.Random = context.Registry.GetService<IRandomService>();
            context.Pin(DynamicMethodsProtection.ContextKey, ctx);
        }
    }
}
