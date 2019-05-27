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
		uint _Mask, Asserted, ToAssert, Status;
		
		[Port(0x1F801070)]
		ushort Status2 {
			get => (ushort) StatusPort;
			set => StatusPort = value;
		}
		[Port(0x1F801070)]
		uint StatusPort {
			get => Status;
			set => Status &= (value & 3) * 8;
		}
		
		[Port(0x1F801074)]
		ushort Mask2 {
			get => (ushort) Mask;
			set => Mask = value;
		}

		[Port(0x1F801074)]
		uint Mask {
			get => _Mask;
			set {
				_Mask = value;
				if((_Mask & ToAssert) != 0) {
					"Flag turned on while IRQ flagged!".Debug();
					Cpu.AssertIrq(true);
					ToAssert &= ~_Mask;
				}
			}
		}

		public void Assert(IrqType type, bool status) {
			var oldAsserted = Asserted;

			var whichMask = 1U << (int) type;
			if(status)
				$"IRQ type {type} attempting to assert.  Masked: {Asserted & whichMask}".Debug();
			Asserted &= ~whichMask;

			if(status) {
				Asserted |= whichMask;
				Status |= (oldAsserted ^ Asserted) & Asserted;
				if((Mask & whichMask) == 0) {
					$"Couldn't assert irq {type} due to mask".Debug();
					ToAssert |= whichMask;
				}
			}

			Cpu.AssertIrq((Status & Mask) != 0);
		}
	}
}