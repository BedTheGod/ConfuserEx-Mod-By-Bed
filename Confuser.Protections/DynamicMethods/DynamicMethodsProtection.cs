using Perplex.Core;

namespace Perplex.NF.Protections.DynamicMethods
{
    public class DynamicMethodsProtection : Protection
    {
        public static object ContextKey = new object();
        public override void Initialize()
        {
            //
        }

        public override void Prepare(Pipeline pipeline)
        {
            pipeline.Add(new InjectPhase());
            pipeline.Add(new DynamicPhase());
        }
    }
}
