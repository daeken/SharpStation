using PrettyPrinter;

#pragma warning disable 169
namespace SharpStation {
	public struct Timer {
		public uint CurrentValue;
		public uint CounterMode;
		public uint TargetValue;
	}
	
	public class Timing {
		static readonly Timer[] Timers = new Timer[3];
		
		[Port(0x1F801130)] static uint NotTimer3CurrentValue; // TODO: This appears to be wrong but I have no idea what this is

		[Port(0x1F801100, 3, 0x10)]
		static uint GetCurrentValue(int timer) => Timers[timer].CurrentValue;
		[Port(0x1F801100, 3, 0x10)]
		static void SetCurrentValue(int timer, uint value) => Timers[timer].CurrentValue = value;

		[Port(0x1F801104, 3, 0x10)]
		static uint GetCounterMode(int timer) => Timers[timer].CounterMode;
		[Port(0x1F801104, 3, 0x10)]
		static void SetCounterMode(int timer, uint value) => Timers[timer].CounterMode = value;

		[Port(0x1F801108, 3, 0x10)]
		static uint GetCounterTargetValue(int timer) => Timers[timer].TargetValue;
		[Port(0x1F801108, 3, 0x10)]
		static void SetCounterTargetValue(int timer, uint value) => Timers[timer].TargetValue = value;
	}
}