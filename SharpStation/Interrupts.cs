#pragma warning disable 169
namespace SharpStation {
	public class Interrupts {
		[Port(0x1F801070)] static uint Status;
		[Port(0x1F801074)] static uint Mask;
	}
}