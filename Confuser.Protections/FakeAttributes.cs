using System;
using Confuser.Core;
using Confuser.Core.Services;
using dnlib.DotNet;

namespace Confuser.Protections.FakeAttributes
{
	// Token: 0x0200007B RID: 123
	internal class FakeAttributes : Protection
	{
		// Token: 0x17000067 RID: 103
		// (get) Token: 0x0600020A RID: 522 RVA: 0x000126E2 File Offset: 0x000108E2
		public override string Name
		{
			get
			{
				return "Fake Attributes";
			}
		}

		// Token: 0x17000068 RID: 104
		// (get) Token: 0x0600020B RID: 523 RVA: 0x000126E9 File Offset: 0x000108E9
		public override string Description
		{
			get
			{
				return "Add fake attributes of others obfuscators to create fake detections on de4dot !";
			}
		}

		// Token: 0x17000069 RID: 105
		// (get) Token: 0x0600020C RID: 524 RVA: 0x000126F0 File Offset: 0x000108F0
		public override string Id
		{
			get
			{
				return "fake attributes";
			}
		}

		// Token: 0x1700006A RID: 106
		// (get) Token: 0x0600020D RID: 525 RVA: 0x000126F7 File Offset: 0x000108F7
		public override string FullId
		{
			get
			{
				return "Ki.FakeAttributes";
			}
		}

		// Token: 0x1700006B RID: 107
		// (get) Token: 0x0600020E RID: 526 RVA: 0x000126FE File Offset: 0x000108FE
		public override ProtectionPreset Preset
		{
			get
			{
				return ProtectionPreset.Maximum;
			}
		}

		// Token: 0x0600020F RID: 527 RVA: 0x00012701 File Offset: 0x00010901
		protected override void Initialize(ConfuserContext context)
		{
		}

		// Token: 0x06000210 RID: 528 RVA: 0x00012703 File Offset: 0x00010903
		protected override void PopulatePipeline(ProtectionPipeline pipeline)
		{
			pipeline.InsertPreStage(PipelineStage.ProcessModule, new FakeAttributes.FakeAttributesPhase(this));
		}

		// Token: 0x04000180 RID: 384
		public const string _Id = "fake atributes";

		// Token: 0x04000181 RID: 385
		public const string _FullId = "Ki.FakeAttributes";

		// Token: 0x0200007C RID: 124
		private class FakeAttributesPhase : ProtectionPhase
		{
			// Token: 0x1700006C RID: 108
			// (get) Token: 0x06000212 RID: 530 RVA: 0x0001271A File Offset: 0x0001091A
			public override ProtectionTargets Targets
			{
				get
				{
					return ProtectionTargets.Modules;
				}
			}

			// Token: 0x1700006D RID: 109
			// (get) Token: 0x06000213 RID: 531 RVA: 0x0001271E File Offset: 0x0001091E
			public override string Name
			{
				get
				{
					return "Fake Attributes";
				}
			}

			// Token: 0x06000214 RID: 532 RVA: 0x00012725 File Offset: 0x00010925
			public FakeAttributesPhase(FakeAttributes parent) : base(parent)
			{
			}

			// Token: 0x06000215 RID: 533 RVA: 0x00012730 File Offset: 0x00010930
			protected override void Execute(ConfuserContext context, ProtectionParameters parameters)
			{
				IMarkerService service = context.Registry.GetService<IMarkerService>();
				foreach (ModuleDefMD current in context.Modules)
				{

					TypeRef typeRef = current.CorLibTypes.GetTypeRef("System", "Attribute");
                    TypeRef typeRef6 = current.CorLibTypes.GetTypeRef("System", "Attribute");
                    TypeRef typeRef7 = current.CorLibTypes.GetTypeRef("System", "Attribute");
                    TypeDefUser typeDefUser6 = new TypeDefUser("", "ETERALDEATH-PRIVATE-PROTECTOR", typeRef6);
                    current.Types.Add(typeDefUser6);
                    service.Mark(typeDefUser6, null);
                    TypeDefUser typeDefUser7 = new TypeDefUser("", "EternalDeath-Private-Protector", typeRef7);
                    current.Types.Add(typeDefUser7);
                    service.Mark(typeDefUser7, null);
                    TypeDefUser typeDefUser = new TypeDefUser("", "E[.][.]t[.]e[.]r[.]n[.]a[.]l[.]D[.]e[.]a[.]t[.]h", typeRef);
					current.Types.Add(typeDefUser);
					service.Mark(typeDefUser, null);
					TypeRef typeRef2 = current.CorLibTypes.GetTypeRef("System", "Attribute");
					TypeDefUser typeDefUser2 = new TypeDefUser("", "E-t-e-r-n-a-l-D-e-a-t-h", typeRef2);
					current.Types.Add(typeDefUser2);
					service.Mark(typeDefUser2, null);
					TypeRef typeRef3 = current.CorLibTypes.GetTypeRef("System", "Attribute");
					TypeDefUser typeDefUser3 = new TypeDefUser("", "EternalDeath", typeRef3);
					current.Types.Add(typeDefUser3);
					service.Mark(typeDefUser3, null);
					TypeRef typeRef4 = current.CorLibTypes.GetTypeRef("System", "Attribute");
					TypeDefUser typeDefUser4 = new TypeDefUser("", "EternalDeath-Protector", typeRef4);
					current.Types.Add(typeDefUser4);
					service.Mark(typeDefUser4, null);
					TypeRef typeRef5 = current.CorLibTypes.GetTypeRef("System", "Attribute");
					TypeDefUser typeDefUser5 = new TypeDefUser("", "ObfuscatedByEternalDeath", typeRef5);
					current.Types.Add(typeDefUser5);
					service.Mark(typeDefUser5, null);

				}
			}
		}
	}
}
