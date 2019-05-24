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

		public static bool HasBit(this uint v, int bit) => (v & (1U << bit)) != 0;
		public static uint ToBit(this bool v, int bit) => v ? 1U << bit : 0;
	}
}