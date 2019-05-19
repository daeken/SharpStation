using PrettyPrinter;

#pragma warning disable 169
namespace SharpStation {
	public class Dma {
		static readonly Dma Instance = new Dma();
		
		[Port(0x1F8010F4)] uint Interrupt;

		[Port(0x1F8010F0)] uint Control {
			get => 0;
			set { }
		}

		[Port(0x1F801080, 7, 0x10)]
		uint GetBase(int channel) => 0U;
		[Port(0x1F801080, 7, 0x10)]
		void SetBase(int channel, uint value) {
			$"Base for DMA channel {channel} set to {value:X}".Print();
		}

		[Port(0x1F801084, 7, 0x10)]
		uint GetBlockControl(int channel) => 0U;
		[Port(0x1F801084, 7, 0x10)]
		void SetBlockControl(int channel, uint value) {
			$"Block control for DMA channel {channel} set to {value:X}".Print();
		}

		[Port(0x1F801088, 7, 0x10)]
		uint GetChannelControl(int channel) => 0U;
		[Port(0x1F801088, 7, 0x10)]
		void SetChannelControl(int channel, uint value) {
			$"Channel control for DMA channel {channel} set to {value:X}".Print();
		}
	}
}