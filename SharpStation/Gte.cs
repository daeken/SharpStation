using System;
using System.Linq;

// Cribbed nearly directly from Rustation

namespace SharpStation {
	public partial class Cop2 {
		int OfX, OfY, Dqb;
		ushort H, Otz;
		short Dqa, Zsf3, Zsf4;
		uint Flags, Lzcs, Reg23;
		byte Lzcr = 32;

		readonly short[][,] Matrices = Enumerable.Range(0, 3).Select(_ => new short[3, 3]).ToArray();
		readonly int[][] ControlVectors = Enumerable.Range(0, 4).Select(_ => new int[3]).ToArray();

		readonly short[][] V = Enumerable.Range(0, 4).Select(_ => new short[3]).ToArray();
		readonly int[] Mac = new int[4];
		(byte, byte, byte, byte) Rgb;
		readonly short[] Ir = new short[4];
		readonly (short, short)[] XyFifo = new (short, short)[4];
		readonly short[] ZFifo = new short[4];
		readonly (byte, byte, byte, byte)[] RgbFifo = new (byte, byte, byte, byte)[3];

		void SetFlag(int bit) => Flags |= 1U << bit;
		T SetFlag<T>(int bit, T value) { SetFlag(bit); return value; }

		long Truncate44(int flag, long val) {
			if(val > 0x7ffffffffff) SetFlag(30 - flag);
			else if(val < -0x80000000000) SetFlag(27 - flag);

			return (val << (64 - 44)) >> (64 - 44);
		}

		short Saturate11(int flag, int val) {
			if(val < -0x400) return SetFlag(14 - flag, (short) -0x400);
			if(val > 0x3ff) return SetFlag(14 - flag, (short) 0x3FF);
			return (short) val;
		}

		short Saturate16(Config config, byte flag, int val) {
			var min = config.ClampNegative ? 0 : short.MinValue;

			if(val > short.MaxValue) {
				SetFlag(24 - flag);
				return short.MaxValue;
			}
			if(val >= min) return (short) val;
			
			SetFlag(24 - flag);
			return (short) min;
		}

		ushort ToOtz(long avg) {
			var value = avg >> 12;
			if(value < 0) return SetFlag(18, (ushort) 0);
			if(value > 0xFFFF) return SetFlag(18, (ushort) 0xFFFF);
			return (ushort) value;
		}

		void CheckMacOverflow(long val) {
			if(val < -0x80000000) SetFlag(15);
			else if(val > 0x7fffffff) SetFlag(16);
		}

		void NClip() {
			var (x0, y0) = XyFifo[0];
			var (x1, y1) = XyFifo[1];
			var (x2, y2) = XyFifo[2];

			var a = x0 * (y1 - y2);
			var b = x1 * (y2 - y0);
			var c = x2 * (y0 - y1);

			var sum = (long) a + b + c;
			CheckMacOverflow(sum);
			Mac[0] = (int) sum;
		}

		void MacToIr(Config config) {
			Ir[1] = Saturate16(config, 0, Mac[1]);
			Ir[2] = Saturate16(config, 1, Mac[2]);
			Ir[3] = Saturate16(config, 2, Mac[3]);
		}

		void MultiplyMatrixByVector(Config config, ControlMatrix matrix, int vectorIndex, ControlVector vector) {
			if(matrix == ControlMatrix.Invalid) throw new NotSupportedException();
			if(vector == ControlVector.FarColor) throw new NotSupportedException();

			var mat = (int) matrix;
			var crv = (int) vector;

			for(var r = 0; r < 3; ++r) {
				var res = (long) ControlVectors[crv][r] << 12;

				for(var c = 0; c < 3; ++c) {
					var v = (int) V[vectorIndex][c];
					var m = (int) Matrices[mat][r, c];

					var product = v * m;
					res = Truncate44(r, res + product);
				}

				Mac[r + 1] = (int) (res >> config.Shift);
			}

			MacToIr(config);
		}

		void Ncd(Config config, int vectorIndex) {
			MultiplyMatrixByVector(config, ControlMatrix.Light, vectorIndex, ControlVector.Zero);
			
			V[3][0] = Ir[1];
			V[3][1] = Ir[2];
			V[3][2] = Ir[3];

			MultiplyMatrixByVector(config, ControlMatrix.Color, 3, ControlVector.BackgroundColor);
		}

		void Ncds(Config config) => Ncd(config, 0);

		void MacToRgbFifo() {
			byte MacToColor(int mac, int which) {
				var c = mac >> 4;
				if(c < 0) return SetFlag(21 - which, (byte) 0);
				if(c > 0xFF) return SetFlag(21 - which, (byte) 0xFF);
				return (byte) c;
			}

			var r = MacToColor(Mac[1], 0);
			var g = MacToColor(Mac[2], 0);
			var b = MacToColor(Mac[3], 0);

			var (_, _, _, x) = Rgb;

			RgbFifo[0] = RgbFifo[1];
			RgbFifo[1] = RgbFifo[2];
			RgbFifo[2] = RgbFifo[3];
			RgbFifo[3] = (r, g, b, x);
		}

		void Dcpl(Config config) {
			var (r, g, b, _) = Rgb;
			var col = new[] { r, g, b };

			for(var i = 0; i < 3; ++i) {
				var fc = (long) ControlVectors[(int) ControlVector.FarColor][i] << 12;
				var ir = (int) Ir[i + 1];
				var ccol = col[i] << 4;

				var shading = (long) (ccol * ir);
				var tmp = (int) (Truncate44(i, fc - shading) >> config.Shift);
				
				var res = Saturate16(new Config(0), (byte) i, tmp);
				Mac[i + 1] = (int) (Truncate44(i, shading + (long) Ir[0] * res) >> config.Shift);
			}
			
			MacToIr(config);
			MacToRgbFifo();
		}

		void AvsZ3() {
			var sum = (uint) ZFifo[1] + (uint) ZFifo[2] + (uint) ZFifo[3];
			var avg = Zsf3 * sum;
			
			CheckMacOverflow(avg);

			Mac[0] = (int) avg;
			Otz = ToOtz(avg);
		}

		void Rtpt(Config config) {
			Rtp(config, 0);
			Rtp(config, 1);
			DepthQueuing(Rtp(config, 2));
		}

		uint Rtp(Config config, int vectorIndex) {
			var zShifted = 0;

			var rm = (int) ControlMatrix.Rotation;
			var tr = (int) ControlVector.Translation;

			for(var r = 0; r < 3; ++r) {
				var res = (long) ControlVectors[tr][r] << 12;

				for(var c = 0; c < 3; ++c) {
					var v = (int) V[vectorIndex][c];
					var m = (int) Matrices[rm][r, c];
					res = Truncate44(c, res + v * m);
				}

				Mac[r + 1] = (int) (res >> config.Shift);
				zShifted = (int) (res >> 12);
			}

			Ir[1] = Saturate16(config, 0, Mac[1]);
			Ir[2] = Saturate16(config, 1, Mac[2]);
			
			if(zShifted > short.MaxValue || zShifted < short.MinValue) SetFlag(22);
			
			var min = config.ClampNegative ? 0 : short.MinValue;
			var val = Mac[3];
			if(val < min) Ir[3] = (short) min;
			else if(val > short.MaxValue) Ir[3] = short.MaxValue;
			else Ir[3] = (short) val;

			var zSaturated = zShifted;
			if(zShifted < 0) {
				SetFlag(18);
				zSaturated = 0;
			} else if(zShifted > ushort.MaxValue) {
				SetFlag(18);
				zSaturated = ushort.MaxValue;
			}

			ZFifo[0] = ZFifo[1];
			ZFifo[1] = ZFifo[2];
			ZFifo[2] = ZFifo[3];
			ZFifo[3] = (short) zSaturated;

			var factor = (long) (zSaturated > H / 2 ? Divider.Divide(H, (ushort) zSaturated) : SetFlag(17, 0x1ffffU));

			var screenX = Ir[1] * factor + OfX;
			var screenY = Ir[2] * factor + OfY;
			
			CheckMacOverflow(screenX);
			CheckMacOverflow(screenY);

			XyFifo[3] = (Saturate11(0, (int) (screenX >> 16)), Saturate11(1, (int) (screenY >> 16)));
			XyFifo[0] = XyFifo[1];
			XyFifo[1] = XyFifo[2];
			XyFifo[2] = XyFifo[3];

			return (uint) factor;
		}

		void DepthQueuing(uint projectionFactor) {
			var factor = (long) projectionFactor;
			var dqa = (long) Dqa;
			var dqb = (long) Dqb;

			var depth = dqb + dqa * factor;
			CheckMacOverflow(depth);
			Mac[0] = (int) depth;

			depth >>= 12;

			if(depth < 0) Ir[0] = (short) SetFlag(12, 0);
			else if(depth > 4096) Ir[0] = (short) SetFlag(12, 4096);
			else Ir[0] = (short) depth;
		}
	}
}