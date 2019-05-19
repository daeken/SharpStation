#pragma warning disable 169
namespace SharpStation {
	public class Timers {
		static readonly Timers Instance = new Timers();
		
		[Port(0x1F801130)] uint Timer3CurrentValue; // TODO: Figure out if this is actually right

		[Port(0x1F801100, 3, 0x10)]
		uint GetCurrentValue(int timer) => 0;
		[Port(0x1F801100, 3, 0x10)]
		void SetCurrentValue(int timer, uint value) {}

		[Port(0x1F801104, 3, 0x10)]
		uint GetCounterMode(int timer) => 0;
		[Port(0x1F801104, 3, 0x10)]
		void SetCounterMode(int timer, uint value) {}

		[Port(0x1F801108, 3, 0x10)]
		uint GetCounterTargetValue(int timer) => 0;
		[Port(0x1F801108, 3, 0x10)]
		void SetCounterTargetValue(int timer, uint value) {}
	}
}