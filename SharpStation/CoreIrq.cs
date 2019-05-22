using static SharpStation.Globals;

namespace SharpStation {
	public enum IrqType {
		VBlank = 0, 
		GPU    = 1, 
		CD     = 2, 
		DMA    = 3, 
		Timer0 = 4, 
		Timer1 = 5, 
		Timer2 = 6, 
		Sio    = 7, 
		Spu    = 9, 
		Pio    = 10
	}
	
	public class CoreIrq {
		static uint Status;
		[Port(0x1F801070)]
		static ushort Status2 {
			get => (ushort) StatusPort;
			set => StatusPort = value;
		}
		[Port(0x1F801070)]
		static uint StatusPort {
			get => Status;
			set => Status &= (value & 3) * 8;
		}
		
		[Port(0x1F801074)]
		static ushort Mask2 {
			get => (ushort) Mask;
			set => Mask = value;
		}
		[Port(0x1F801074)] static uint Mask;

		uint Asserted;

		void Recalc() => Cpu.AssertIrq(0, (Status & Mask) != 0);

		public void Assert(IrqType type, bool status) {
			$"Assert {type} {status} -- {Mask:X} {Asserted:X} {Status:X}".Debug();
			var oldAsserted = Asserted;

			var whichMask = 1U << (int) type;
			Asserted &= ~whichMask;

			if(status) {
				Asserted |= whichMask;
				Status |= (oldAsserted ^ Asserted) & Asserted;
			}
			$"Post {type} {status} -- {Mask:X} {Asserted:X} {Status:X}".Debug();

			Recalc();
		}
	}
}