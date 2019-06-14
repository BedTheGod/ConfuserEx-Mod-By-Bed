using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Confuser.Runtime {
	internal static class Calli {
		public static IntPtr ResolveToken(long token) {
			return typeof(Calli).Module.ResolveMethod((int)token).MethodHandle.GetFunctionPointer();
		}
	}
}
