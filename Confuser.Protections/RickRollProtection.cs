using Confuser.Core;
using Confuser.Renamer;
using dnlib.DotNet;
using System.Linq;

namespace Confuser.Protections {
	[AfterProtection("Ki.Rename", "Ki.AntiTamper", "Ki.Constants", "Ki.ControlFlow", "Ki.AntiDebug", "Ki.AntiDump", "Ki.RefProxy", "ElektroKill.ModuleProperties")]
	public class RickRollProtection : Protection {
		public override ProtectionPreset Preset => ProtectionPreset.Normal;

		public override string Name => "RickRoll Protection";

		public override string Description => "Makes .NET Reflector useless.\nIncompatible with alot of other protections.";

		public override string Author => "Ki (yck1509)";

		public override string Id => "rickroll";

		public override string FullId => "Ki.RickRoll";

		protected override void Initialize(ConfuserContext context) { }

		protected override void PopulatePipeline(ProtectionPipeline pipeline) {
			pipeline.InsertPreStage(PipelineStage.WriteModule, new RickRollPhase(this));
		}

		class RickRollPhase : ProtectionPhase {
			public RickRollPhase(RickRollProtection parent) : base(parent) { }

			public override ProtectionTargets Targets => ProtectionTargets.Modules;

			public override string Name => "RickRolling";

			protected override void Execute(ConfuserContext context, ProtectionParameters parameters) {
				foreach (ModuleDef module in parameters.Targets.OfType<ModuleDef>().WithProgress(context.Logger))
					RickRoller.CommenceRickroll(context, module);
			}
		}
	}
}
