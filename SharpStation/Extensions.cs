using System;
using System.Reflection;
using PrettyPrinter;

namespace SharpStation {
	public static class Extensions {
		public static T Debug<T>(this T value) {
			if(value is string str)
				Console.WriteLine(str);
			else
				value.Print();
			return value;
		}
		
		public static DelegateT CreateDelegate<DelegateT>(this MethodInfo mi) =>
			(DelegateT) (object) Delegate.CreateDelegate(typeof(DelegateT), mi);

		public static int CountLeadingZeros(this uint v) {
			var c = 0;
			for(var i = 0x80000000U; i != 0; i >>= 1, ++c) {
				if((v & i) != 0)
					return c;
			}
			return 0;
		}

		public static bool HasBit(this byte v, int bit) => (v & (1U << bit)) != 0;
		public static bool HasBit(this ushort v, int bit) => (v & (1U << bit)) != 0;
		public static bool HasBit(this uint v, int bit) => (v & (1U << bit)) != 0;
		public static uint ToBit(this bool v, int bit) => v ? 1U << bit : 0;
		
		public static int SignExt(this uint imm, int size) {
			unchecked {
				switch(size) {
					case 8: return (sbyte) (byte) imm;
					case 16: return (short) (ushort) imm;
					case 32: return (int) imm;
					case int _ when (imm & (1 << (size - 1))) != 0: return (int) imm - (1 << size);
					default: return (int) imm;
				}
			}
		}

		public static int FromBcd(this byte v) => (v >> 4) * 10 + (v & 0xF);
		
		public static (int, int, int) ToMsf(this uint v) =>
			((int) v / (60 * 75), (int) v / 75 % 60, (int) v % 75);
	}
}