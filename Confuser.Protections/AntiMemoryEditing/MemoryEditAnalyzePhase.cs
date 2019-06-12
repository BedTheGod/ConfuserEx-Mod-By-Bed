using Confuser.Core;
using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Confuser.Protections.AntiMemoryEditing {
	public class MemoryEditAnalyzePhase : ProtectionPhase {
		public override ProtectionTargets Targets => ProtectionTargets.Fields;
		public override string Name => "Memory Protection analysis";

		public MemoryEditAnalyzePhase(ConfuserComponent parent) : base(parent) { }

		protected override void Execute(ConfuserContext context, ProtectionParameters parameters) {
			var service = context.Registry.GetService<IMemoryEditService>();

			foreach (IDnlibDef def in parameters.Targets.WithProgress(context.Logger)) {
				if (def is FieldDef d) {
					if (CorrectType(d)) {
						service.AddToList(d);
						context.Logger.DebugFormat("Added {0} to list", d.Name);
					}
					else
						context.Logger.WarnFormat("{0} was marked for memory protection, but type '{1}' is not supported!", d, d.FieldType);
				}
				else
					context.Logger.WarnFormat("{0} (of type {1}) was marked for memory protection, but this is not a field!", def, def.GetType());
			}
		}

		private static bool CorrectType(FieldDef f) {
			switch (f.FieldType.FullName) {
				case "System.Byte":
				case "System.SByte":
				case "System.Int16":
				case "System.UInt16":
				case "System.Int32":
				case "System.UInt32":
				case "System.Int64":
				case "System.UInt64":

				case "System.Single":
				case "System.Double":
				case "System.Decimal":

				case "System.String":
					return true;
				default:
					return false;
			}
		}
	}
}
