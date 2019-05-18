// ReSharper disable UnusedMember.Global
using PrettyPrinter;

namespace SharpStation {
	public class Gpu {
		[Port(0x1F801810)]
		static uint GpuReadAndGp0Send {
			get {
				"GPU read!".Print();
				return 0;
			}
			set => $"GPU GP0 send 0x{value:X8}".Print();
		}
		
		[Port(0x1F801814)]
		static uint GpuStatAndGp1Send {
			get {
				"GPU Stat!".Print();
				return 0;
			}
			set => $"GPU GP1 send 0x{value:X8}".Print();
		}
	}
}