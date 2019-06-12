using System;
using System.Collections.Generic;
using System.Text;

namespace Confuser.Runtime.AntiMemoryEditing {
	public class ObfuscatedValue<T> {
		private static readonly Random Rand = new Random();

		private readonly int _salt;
		private T _obf;

		public ObfuscatedValue(T val) {
			_salt = Rand.Next(int.MinValue, int.MaxValue);
			_obf = Obfuscate(val, _salt);
		}

		//allow normal read/write
		public static implicit operator T(ObfuscatedValue<T> value) => Deobfuscate(value._obf, value._salt);
		public static implicit operator ObfuscatedValue<T>(T value) => new ObfuscatedValue<T>(value);

		public override string ToString() => Deobfuscate(_obf, _salt).ToString();

		private static T Deobfuscate(T currentvalue, int salt) => Obfuscate(currentvalue, salt);
		private static T Obfuscate(T currentvalue, int salt) {
			Type type = currentvalue.GetType();

			switch (currentvalue) {
				case string s: return (T)(object)XorS(s, salt);

				case sbyte i: return (T)(object)(i ^ salt);
				case byte i: return (T)(object)(i ^ salt);
				case short i: return (T)(object)(i ^ salt);
				case ushort i: return (T)(object)(i ^ salt);
				case int i: return (T)(object)(i ^ salt);
				case uint i: return (T)(object)(i ^ salt);
				case long i: return (T)(object)(i ^ (salt + ((long)salt << 32)));
				case ulong i: return (T)(object)(i ^ ((ulong)salt + ((ulong)salt << 32)));

				case float f: return (T)(object)XorF(f, salt);
				case double d: return (T)(object)XorD(d, salt + ((long)salt << 32));
				case decimal m: return (T)(object)XorM(m, salt);
				default:
					if (type.BaseType == typeof(Enum))
						return (T)(object)((int)Convert.ChangeType(currentvalue, typeof(int)) ^ salt);  //this assumes that the enum is 4 bytes in size
					else
						throw new NotSupportedException();
			}
		}

		private static unsafe float XorF(float f, int salt) {
			int raw = *((int*)&f) ^ salt;
			return *((float*)&raw);
		}

		private static unsafe double XorD(double d, long salt) {
			long raw = *((long*)&d) ^ salt;
			return *((double*)&raw);
		}

		private static decimal XorM(decimal m, int salt) {
			int[] bits = decimal.GetBits(m);

			bits[0] ^= salt;    //xor lo
			bits[1] ^= salt;    //xor mid
			bits[2] ^= salt;    //xor hi

			const int signMask = unchecked((int)0x8000_0000);
			const int scaleMask = 0x001F_0000;

			bits[3] ^= salt & (signMask | scaleMask);    //xor flags

			return new decimal(bits);
		}

		private static string XorS(string str, int salt) {
			var sb = new StringBuilder(str.Length);
			foreach (char c in str)
				sb.Append((char)(c ^ salt));
			return sb.ToString();
		}
	}
}
