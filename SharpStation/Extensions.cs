using System;
using System.Reflection;

namespace SharpStation {
	public static class Extensions {
		public static DelegateT CreateDelegate<DelegateT>(this MethodInfo mi) =>
			(DelegateT) (object) Delegate.CreateDelegate(typeof(DelegateT), mi);
	}
}