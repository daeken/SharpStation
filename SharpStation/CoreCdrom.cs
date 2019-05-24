using System;

namespace SharpStation {
	public class CoreCdrom {
		[Port(0x1F801800)]
		byte IndexStatus {
			get => throw new NotImplementedException();
			set {
				"CD-ROM access!".Debug();
				Environment.Exit(0);
			}
		}
	}
}