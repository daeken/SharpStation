using PrettyPrinter;

#pragma warning disable 169
namespace SharpStation {
	public struct Timer {
		readonly int Index;

		uint currentValue, counterMode, targetValue;
		
		public uint CurrentValue {
			get {
				$"Timer {Index} get current value ({currentValue:X08})".Debug();
				return currentValue;
			}
			set {
				$"Timer {Index} set current value ({value:X08})".Debug();
				currentValue = value;
			}
		}
		public uint CounterMode {
			get {
				$"Timer {Index} get counter mode ({counterMode:X08})".Debug();
				return counterMode;
			}
			set {
				$"Timer {Index} set counter mode ({value:X08})".Debug();
				counterMode = value;
			}
		}
		public uint TargetValue {
			get {
				$"Timer {Index} get target value ({targetValue:X08})".Debug();
				return targetValue;
			}
			set {
				$"Timer {Index} set target value ({value:X08})".Debug();
				targetValue = value;
			}
		}

		public Timer(int index) {
			Index = index;
			currentValue = counterMode = targetValue = 0;
		}
	}
	
	public class Timing {
		static readonly Timer[] Timers = { new Timer(0), new Timer(1), new Timer(2) };
		
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