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
	}
}