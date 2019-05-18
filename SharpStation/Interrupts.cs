#pragma warning disable 169
namespace SharpStation {
	public class Interrupts {
		[Port(0x1F801070)] static ushort Status;
		[Port(0x1F801070)]
		static uint Status4 {
			get => Status;
			set => Status = (ushort) value;
		}
		[Port(0x1F801074)] static ushort Mask;
		[Port(0x1F801074)]
		static uint Mask4 {
			get => Mask;
			set => Mask = (ushort) value;
		}
	}
}