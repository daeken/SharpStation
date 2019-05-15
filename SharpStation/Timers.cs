#pragma warning disable 169
namespace SharpStation {
	public static class Timers {
		[Port(0x1F801100)] static ushort Timer0CurrentValue;
		[Port(0x1F801104)] static ushort Timer0CounterMode;
		[Port(0x1F801108)] static ushort Timer0CounterTargetValue;
		
		[Port(0x1F801110)] static ushort Timer1CurrentValue;
		[Port(0x1F801114)] static ushort Timer1CounterMode;
		[Port(0x1F801118)] static ushort Timer1CounterTargetValue;

		[Port(0x1F801120)] static ushort Timer2CurrentValue;
		[Port(0x1F801124)] static ushort Timer2CounterMode;
		[Port(0x1F801128)] static ushort Timer2CounterTargetValue;

		[Port(0x1F801130)] static uint Timer3; // TODO: Figure out if this is actually right
	}
}