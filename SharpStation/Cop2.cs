using System;
using static SharpStation.Globals;

namespace SharpStation {
	public class Cop2 : ICoprocessor {
		enum Reg {
			BPC   = 3,  // PC breakpoint address
			BDA   = 5,  // Data load/store breakpoint address
			TAR   = 6,  // Target address
			DCIC  = 7,  // Cache control
			BDAM  = 9,  // Data load/store address mask
			BPCM  = 11, // PC breakpoint address mask
			SR    = 12, 
			CAUSE = 13, 
			EPC   = 14, 
			PRID  = 15  // Product ID
		}
		
		public uint this[uint register] {
			get => throw new NotImplementedException();
			set => throw new NotImplementedException();
		}

		public void Copcreg(uint reg, uint value) {
			$"Writing cop2 reg {reg:X} -- 0x{value:X}".Debug();
		}

		public uint Copcreg(uint reg) {
			$"Reading cop2 reg {reg:X}".Debug();
			return 0;
		}

		public void Call(uint func, uint inst) => throw new NotImplementedException();
	}
}