using System;

namespace SharpStation {
	public class Cop0 : ICoprocessor {
		readonly Cpu Cpu;
		public uint StatusRegister = (1 << 22) | (1 << 21), Cause, EPC, TargetAddress;

		public Cop0(Cpu cpu) => Cpu = cpu;
		
		public uint this[uint register] {
			get {
				switch(register) {
					case 6:
						return TargetAddress;
					case 12:
						return StatusRegister;
					case 13:
						return Cause;
					case 14:
						return EPC;
					case 15: // Processor ID
						return 2;
					case uint _ when register >= 16: return 0;
					default:
						throw new NotImplementedException();
				}
			}
			set {
				switch(register) {
					case 3: case 5: case 6: case 7: case 9: case 11:
						if(value != 0)
							throw new NotImplementedException();
						break;
					case 12:
						StatusRegister = value;
						Cpu.IsolateCache = ((value >> 16) & 1) == 1;
						break;
					case 13:
						Cause &= ~0x300U;
						Cause |= value & 0x300U;
						break;
					default:
						throw new NotImplementedException();
				}
			}
		}

		public void Call(uint func, uint inst) {
			switch(func) {
				case 16: // RFE
					var mode = StatusRegister & 0x3FU;
					StatusRegister &= ~0xFU;
					StatusRegister |= mode >> 2;
					break;
				default:
					throw new NotImplementedException();
			}
		}
	}
}