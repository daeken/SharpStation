using System;
using PrettyPrinter;
using static SharpStation.Globals;

namespace SharpStation {
	public partial class Cop2 : ICoprocessor {
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

		enum ControlMatrix {
			Rotation, 
			Light, 
			Color, 
			Invalid
		}

		enum ControlVector {
			Translation, 
			BackgroundColor, 
			FarColor, 
			Zero
		}

		struct Config {
			public readonly ControlMatrix Matrix;
			public readonly ControlVector VectorAdd;
			public readonly byte Shift, VectorMul;
			public readonly bool ClampNegative;

			public Config(uint command) {
				Matrix = (ControlMatrix) ((command >> 17) & 3);
				VectorAdd = (ControlVector) ((command >> 13) & 3);
				Shift = (byte) (command.HasBit(19) ? 12 : 0);
				VectorMul = (byte) ((command >> 15) & 3);
				ClampNegative = command.HasBit(10);
			}
		}

		uint XyToUint((int, int) xy) => (ushort) xy.Item1 | ((uint) (ushort) xy.Item2 << 16);
		uint RgbxToUint((byte R, byte G, byte B, byte X) v) => (uint) v.R | ((uint) v.G << 8) | ((uint) v.B << 16) | ((uint) v.X << 24);

		public uint this[uint register] {
			get {
				switch(register) {
					case 06: return RgbxToUint(Rgb);
					case 07: return Otz;
					
					case 08: return (uint) Ir[0];
					case 09: return (uint) Ir[1];
					case 10: return (uint) Ir[2];
					case 11: return (uint) Ir[3];
					
					case 12: return XyToUint(XyFifo[0]);
					case 13: return XyToUint(XyFifo[1]);
					case 14: return XyToUint(XyFifo[2]);
					case 15: return XyToUint(XyFifo[3]);
					
					case 20: return RgbxToUint(RgbFifo[0]);
					case 21: return RgbxToUint(RgbFifo[1]);
					case 22: return RgbxToUint(RgbFifo[2]);

					case 24: return (uint) Mac[0];
					case 25: return (uint) Mac[1];
					case 26: return (uint) Mac[2];
					case 27: return (uint) Mac[3];
					default: throw new NotImplementedException($"Cop2 reg {register}");
				}
			}
			set {
				(byte, byte, byte, byte) ToRgbx(uint v) => ((byte) v, (byte) (v >> 8), (byte) (v >> 16), (byte) (v >> 24));
				
				switch(register) {
					case  0: { var v = (int) value; V[0][0] = (short) v; V[0][1] = (short) (v >> 16); break; }
					case  1: V[0][2] = (short) (int) value; break;
					case  2: { var v = (int) value; V[1][0] = (short) v; V[1][1] = (short) (v >> 16); break; }
					case  3: V[1][2] = (short) (int) value; break;
					case  4: { var v = (int) value; V[2][0] = (short) v; V[2][1] = (short) (v >> 16); break; }
					case  5: V[2][2] = (short) (int) value; break;
					
					case  6: Rgb = ToRgbx(value); break;
					
					case  8: Ir[0] = (short) value; break;
					case  9: Ir[1] = (short) value; break;
					case 10: Ir[2] = (short) value; break;
					case 11: Ir[3] = (short) value; break;
					
					default: throw new NotImplementedException($"Setting cop2 reg {register} -- 0x{value:X}");
				}
			}
		}

		void AssignMatrix(ControlMatrix matrixType, uint value, int a, int b, int c = -1, int d = -1) {
			var ivalue = (int) value;
			var matrix = Matrices[(int) matrixType];
			matrix[a, b] = (short) ivalue;
			if(c != -1)
				matrix[c, d] = (short) (ivalue >> 16);
		}

		public void Copcreg(uint reg, uint value) {
			switch(reg) {
				case  0: AssignMatrix(ControlMatrix.Rotation, value, 0, 0, 0, 1); break;
				case  1: AssignMatrix(ControlMatrix.Rotation, value, 0, 2, 1, 0); break;
				case  2: AssignMatrix(ControlMatrix.Rotation, value, 1, 1, 1, 2); break;
				case  3: AssignMatrix(ControlMatrix.Rotation, value, 2, 0, 2, 1); break;
				case  4: AssignMatrix(ControlMatrix.Rotation, value, 2, 2); break;

				case  5: ControlVectors[(int) ControlVector.Translation][0] = (int) value; break;
				case  6: ControlVectors[(int) ControlVector.Translation][1] = (int) value; break;
				case  7: ControlVectors[(int) ControlVector.Translation][2] = (int) value; break;

				case  8: AssignMatrix(ControlMatrix.Light, value, 0, 0, 0, 1); break;
				case  9: AssignMatrix(ControlMatrix.Light, value, 0, 2, 1, 0); break;
				case 10: AssignMatrix(ControlMatrix.Light, value, 1, 1, 1, 2); break;
				case 11: AssignMatrix(ControlMatrix.Light, value, 2, 0, 2, 1); break;
				case 12: AssignMatrix(ControlMatrix.Light, value, 2, 2); break;

				case 13: ControlVectors[(int) ControlVector.BackgroundColor][0] = (int) value; break;
				case 14: ControlVectors[(int) ControlVector.BackgroundColor][1] = (int) value; break;
				case 15: ControlVectors[(int) ControlVector.BackgroundColor][2] = (int) value; break;

				case 16: AssignMatrix(ControlMatrix.Color, value, 0, 0, 0, 1); break;
				case 17: AssignMatrix(ControlMatrix.Color, value, 0, 2, 1, 0); break;
				case 18: AssignMatrix(ControlMatrix.Color, value, 1, 1, 1, 2); break;
				case 19: AssignMatrix(ControlMatrix.Color, value, 2, 0, 2, 1); break;
				case 20: AssignMatrix(ControlMatrix.Color, value, 2, 2); break;

				case 21: ControlVectors[(int) ControlVector.FarColor][0] = (int) value; break;
				case 22: ControlVectors[(int) ControlVector.FarColor][1] = (int) value; break;
				case 23: ControlVectors[(int) ControlVector.FarColor][2] = (int) value; break;

				case 24: OfX = (int) value; break;
				case 25: OfY = (int) value; break;
				case 26: H = (ushort) value; break;
				case 27: Dqa = (short) value; break;
				case 28: Dqb = (int) value; break;
				case 29: Zsf3 = (short) value; break;
				case 30: Zsf4 = (short) value; break;
				case 31: Flags = value & 0x7FFFF00 | ((value & 0x7F87E000) != 0).ToBit(31); break;
				default: throw new NotImplementedException($"Setting cop2 control reg {reg} -- 0x{value:X}");
			}
		}

		public uint Copcreg(uint reg) {
			switch(reg) {
				case 31: return Flags;
				default: throw new NotImplementedException($"Getting cop2 control reg {reg}");
			}
		}

		public void Call(uint func, uint inst) {
			var opcode = inst & 0x3F;
			var config = new Config(inst);
			switch(opcode) {
				case 0x06: // NClip
					NClip();
					break;
				case 0x13: // NCDS
					Ncds(config);
					break;
				case 0x2D: // AVSZ3
					AvsZ3();
					break;
				case 0x30: // RTPT
					Rtpt(config);
					break;
				default:
					throw new NotImplementedException($"Cop2 call {opcode:X}");
			}
		}
	}
}