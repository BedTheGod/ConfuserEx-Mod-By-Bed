using Confuser.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Confuser.Protections.AntiMemoryEditing {
	public class MemoryEditProtection : Protection {
		public override string Name => "Anti Memory Editing";
		public override string Description => "Prevent memory editing on selected variables.";
		public override string Author => "HoLLy";
		public override string Id => "memory protection";
		public override string FullId => "HoLLy.MemoryProtection";
		public override ProtectionPreset Preset => ProtectionPreset.None;

		protected override void Initialize(ConfuserContext context) {
			context.Registry.RegisterService(Id, typeof(IMemoryEditService), new MemoryEditService());
		}

		protected override void PopulatePipeline(ProtectionPipeline pipeline) {
			//find all types that need to be modified
			pipeline.InsertPostStage(PipelineStage.Inspection, new MemoryEditAnalyzePhase(this));

			//insert type
			pipeline.InsertPostStage(PipelineStage.BeginModule, new MemoryEditInjectPhase(this));

			//change type and apply IL changes
			pipeline.InsertPreStage(PipelineStage.ProcessModule, new MemoryEditApplyPhase(this));
		}
	}
}
