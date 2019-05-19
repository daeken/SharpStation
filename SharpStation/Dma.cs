#pragma warning disable 169
namespace SharpStation {
	public class Dma {
		static readonly Dma Instance = new Dma();
		
		[Port(0x1F801080)] uint Channel0Base { get => GetBase(0); set => SetBase(0, value); }
		[Port(0x1F801090)] uint Channel1Base { get => GetBase(1); set => SetBase(1, value); }
		[Port(0x1F8010A0)] uint Channel2Base { get => GetBase(2); set => SetBase(2, value); }
		[Port(0x1F8010B0)] uint Channel3Base { get => GetBase(3); set => SetBase(3, value); }
		[Port(0x1F8010C0)] uint Channel4Base { get => GetBase(4); set => SetBase(4, value); }
		[Port(0x1F8010D0)] uint Channel5Base { get => GetBase(5); set => SetBase(5, value); }
		[Port(0x1F8010E0)] uint Channel6Base { get => GetBase(6); set => SetBase(6, value); }

		[Port(0x1F801084)] uint Channel0BlockControl { get => GetBlockControl(0); set => SetBlockControl(0, value); }
		[Port(0x1F801094)] uint Channel1BlockControl { get => GetBlockControl(1); set => SetBlockControl(1, value); }
		[Port(0x1F8010A4)] uint Channel2BlockControl { get => GetBlockControl(2); set => SetBlockControl(2, value); }
		[Port(0x1F8010B4)] uint Channel3BlockControl { get => GetBlockControl(3); set => SetBlockControl(3, value); }
		[Port(0x1F8010C4)] uint Channel4BlockControl { get => GetBlockControl(4); set => SetBlockControl(4, value); }
		[Port(0x1F8010D4)] uint Channel5BlockControl { get => GetBlockControl(5); set => SetBlockControl(5, value); }
		[Port(0x1F8010E4)] uint Channel6BlockControl { get => GetBlockControl(6); set => SetBlockControl(6, value); }

		[Port(0x1F801088)] uint Channel0ChannelControl { get => GetChannelControl(0); set => SetChannelControl(0, value); }
		[Port(0x1F801098)] uint Channel1ChannelControl { get => GetChannelControl(1); set => SetChannelControl(1, value); }
		[Port(0x1F8010A8)] uint Channel2ChannelControl { get => GetChannelControl(2); set => SetChannelControl(2, value); }
		[Port(0x1F8010B8)] uint Channel3ChannelControl { get => GetChannelControl(3); set => SetChannelControl(3, value); }
		[Port(0x1F8010C8)] uint Channel4ChannelControl { get => GetChannelControl(4); set => SetChannelControl(4, value); }
		[Port(0x1F8010D8)] uint Channel5ChannelControl { get => GetChannelControl(5); set => SetChannelControl(5, value); }
		[Port(0x1F8010E8)] uint Channel6ChannelControl { get => GetChannelControl(6); set => SetChannelControl(6, value); }

		[Port(0x1F8010F4)] uint Interrupt;

		[Port(0x1F8010F0)] uint Control {
			get => 0;
			set { }
		}

		uint GetBase(int channel) => 0U;
		void SetBase(int channel, uint value) {}

		uint GetBlockControl(int channel) => 0U;
		void SetBlockControl(int channel, uint value) {}

		uint GetChannelControl(int channel) => 0U;
		void SetChannelControl(int channel, uint value) {}
	}
}