using System;
using System.Threading;

namespace SharpStation {
	public class CoreCdrom {
		[Port(0x1F801800)]
		byte IndexStatus {
			get => throw new NotImplementedException();
			set {
				"CD-ROM access!".Debug();
				while(true)
					Thread.Sleep(500);
			}
		}
	}
}