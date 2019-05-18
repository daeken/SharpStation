#pragma warning disable 169
namespace SharpStation {
	public class Dma {
		[Port(0x1F8010F0)] static uint Control;
		[Port(0x1F8010F4)] static uint Interrupt;
	}
}