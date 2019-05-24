// ReSharper disable UnusedMember.Global

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MoreLinq;
using PrettyPrinter;
using static SharpStation.Globals;
#pragma warning disable 414

namespace SharpStation {
	[AttributeUsage(AttributeTargets.Method)]
	class Gp0Command : Attribute {
		public readonly byte Command;
		public Gp0Command(byte command) => Command = command;
	}
	
	[AttributeUsage(AttributeTargets.Method)]
	class Gp1Command : Attribute {
		public readonly byte Command;
		public Gp1Command(byte command) => Command = command;
	}

	class CurCommand : IEnumerable {
		public readonly MethodInfo Func;
		public readonly object[] Fifo;
		public int Off;

		public CurCommand(MethodInfo func, int count) {
			Func = func;
			Fifo = new object[count];
		}

		public void Add(uint value) => Fifo[Off++] = value;

		IEnumerator IEnumerable.GetEnumerator() => Fifo.GetEnumerator();
	}
	
	public class CoreGpu {
		readonly (int Count, MethodInfo Func)[] Gp0Commands = new (int, MethodInfo)[0x100];
		readonly (int Count, MethodInfo Func)[] Gp1Commands = new (int, MethodInfo)[0x100];

		CurCommand? CurGp0, CurGp1;

		public CoreGpu() {
			typeof(CoreGpu).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
				.Select(x => (Method: x, Attr: x.GetCustomAttribute<Gp0Command>())).Where(x => x.Attr != null)
				.ForEach(x => Gp0Commands[x.Attr.Command] = (x.Method.GetParameters().Length, x.Method));
			typeof(CoreGpu).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
				.Select(x => (Method: x, Attr: x.GetCustomAttribute<Gp1Command>())).Where(x => x.Attr != null)
				.ForEach(x => Gp1Commands[x.Attr.Command] = (x.Method.GetParameters().Length, x.Method));
		}

		[Port(0x1F801810)] uint Read = 0xd0;
		[Port(0x1F801814)] uint Stat = 0x1C000000;

		[Port(0x1F801810)]
		public void Gp0Incoming(uint value) {
			if(CurGp0 == null) {
				var cmd = (byte) (value >> 24);
				//$"GP0({cmd:X2}h) started".Debug();
				var (count, func) = Gp0Commands[cmd];
				if(func == null) throw new NotImplementedException($"GP0 command {cmd:X02}h not implemented");
				switch(count) {
					case 0: func.Invoke(this, null); break;
					case 1: func.Invoke(this, new[] { (object) value }); break;
					default:
						CurGp0 = new CurCommand(func, count);
						CurGp0.Add(value);
						break;
				}
			} else {
				CurGp0.Add(value);
				if(CurGp0.Off == CurGp0.Fifo.Length) {
					var oc = CurGp0;
					CurGp0 = null;
					var p = oc.Func.GetParameters();
					var arr = oc.Fifo;
					if(p[^1].ParameterType.IsArray)
						arr = arr.Take(p.Length - 1).Concat(new[] { arr.Skip(p.Length - 1).ToArray() }).ToArray();
					oc.Func.Invoke(this, arr);
				}
			}
		}

		[Port(0x1F801814)]
		void Gp1Incoming(uint value) {
			if(CurGp1 == null) {
				var cmd = (byte) (value >> 24);
				//$"GP1({cmd:X2}h) started".Debug();
				var (count, func) = Gp1Commands[cmd];
				if(func == null) throw new NotImplementedException($"GP1 command {cmd:X02}h not implemented");
				switch(count) {
					case 0: func.Invoke(this, null); break;
					case 1: func.Invoke(this, new[] { (object) value }); break;
					default:
						CurGp1 = new CurCommand(func, count);
						CurGp1.Add(value & 0x00FFFFFF);
						break;
				}
			} else {
				CurGp1.Add(value);
				if(CurGp1.Off == CurGp1.Fifo.Length) {
					CurGp1.Func.Invoke(this, CurGp1.Fifo);
					CurGp1 = null;
				}
			}
		}

		uint LastTimestamp, LineClockCounter = 3412 - 200, DotClockCounter;
		ulong ClockCounter;

		uint DisplayMode;

		const ulong ClockRatio = 103896; // NTSC clock

		readonly uint[] DotClockRatios = {10, 8, 5, 4, 7};

		public void Update() {
			var sysClocks = Timestamp - LastTimestamp;
			if(sysClocks == 0) goto end;

			ClockCounter += sysClocks * ClockRatio;
			var gpuClocks = (uint) (ClockCounter >> 16);
			ClockCounter -= gpuClocks << 16;

			while(ClockCounter > 0) {
				var chunkClocks = gpuClocks;
				if(chunkClocks > LineClockCounter)
					chunkClocks = LineClockCounter;
				gpuClocks -= chunkClocks;
				LineClockCounter -= chunkClocks;

				DotClockCounter += chunkClocks;
				var dotClocks = DotClockCounter / DotClockRatios[DisplayMode & 3];
				DotClockCounter -= dotClocks * DotClockRatios[DisplayMode & 3];
			}
			
			end:
			LastTimestamp = Timestamp;
		}
		
		[Gp0Command(0x00)] void Nop() {}
		[Gp0Command(0x01)] void ClearCache() {}

		[Gp0Command(0x28)]
		void MonochromeOpaqueQuad(uint cmd, uint v1, uint v2, uint v3, uint v4) =>
			$"MonochromeOpaqueQuad {cmd:X8} {v1:X8} {v2:X8} {v3:X8} {v4:X8}".Debug();

		[Gp0Command(0x2C)]
		void TexturedOpaqueBlendedQuad(uint color, uint v1, uint t1, uint v2, uint t2, uint v3, uint t3, uint v4, uint t4) {
			color &= 0xFFFFFF;
			$"TexturedOpaqueBlendedQuad {color:X6} {v1:X8} {t1:X8} {v2:X8} {t2:X8} {v3:X8} {t3:X8} {v4:X8} {t4:X8}".Debug();
			
		}

		[Gp0Command(0x30)]
		void ShadedOpaqueTri(uint c1, uint v1, uint c2, uint v2, uint c3, uint v3) {
			c1 &= 0xFFFFFF;
			$"ShadedOpaqueTri {c1:X6} {v1:X8} {c2:X6} {v2:X8} {c2:X6} {v2:X8}".Debug();
		}

		[Gp0Command(0x38)]
		void ShadedOpaqueQuad(uint c1, uint v1, uint c2, uint v2, uint c3, uint v3, uint c4, uint v4) {
			c1 &= 0xFFFFFF;
			$"ShadedOpaqueQuad {c1:X6} {v1:X8} {c2:X6} {v2:X8} {c2:X6} {v2:X8}".Debug();
		}

		[Gp0Command(0xA0)]
		void _CopyRectCpuToVram(uint cmd, uint dest, uint size) {
			var xd = dest & 0xFFFF;
			var yd = dest >> 16;
			var width = size & 0xFFFF;
			var height = size >> 16;
			var count = (int) (width * height);
			if(count % 2 == 1) count++;
			CurGp0 = new CurCommand(typeof(CoreGpu).GetMethod(nameof(CopyRectCpuToVram), BindingFlags.NonPublic | BindingFlags.Instance), 5 + count / 2) {
				cmd, xd, yd, width, height
			};
		}
		void CopyRectCpuToVram(uint cmd, uint x, uint y, uint w, uint h, params object[] data) {
			$"Copy rect! {x} . {y} -- {w} x {h}".Debug();
		}

		[Gp0Command(0xE1)]
		void SetDrawMode(uint value) {
			$"Setting draw mode to {value:X06}".Debug();
			//Stat = 0xFFFFFFFF;
		}
		
		[Gp0Command(0xE2)] void SetTextureWindow(uint value) {}
		[Gp0Command(0xE3)] void SetDrawingAreaTopLeft(uint value) {}
		[Gp0Command(0xE4)] void SetDrawingAreaBottomRight(uint value) {}
		[Gp0Command(0xE5)] void SetDrawingOffset(uint value) {}
		[Gp0Command(0xE6)] void SetMaskBit(uint value) {}

		[Gp1Command(0x00)]
		void Reset() => "GPU reset!".Debug();

		[Gp1Command(0x01)]
		void ResetCmdBuffer() => "GPU reset command buffer".Debug();

		[Gp1Command(0x02)]
		void AckInterrupt() => "ACK interrupt".Debug();

		[Gp1Command(0x03)]
		void DisplayEnable(uint value) => $"Turn GPU {((value & 1) == 0 ? "on" : "off")}".Debug();

		[Gp1Command(0x04)]
		void DmaDirectionDataRequest(uint value) => $"DMA direction {value & 3}".Debug();

		[Gp1Command(0x05)]
		void StartDisplayArea(uint value) => $"Start display area {value & 0x3FF} {(value >> 10) & 0x1FF}".Debug();

		[Gp1Command(0x06)]
		void HorizontalDisplayRange(uint value) => $"Horizontal display range {value & 0xFFF} {(value >> 12) & 0xFFF}".Debug();

		[Gp1Command(0x07)]
		void VerticalDisplayRange(uint value) => $"Vertical display range {value & 0x3FF} {(value >> 10) & 0x3FF}".Debug();

		[Gp1Command(0x08)]
		void SetDisplayMode(uint value) => DisplayMode = value & 0xFF;
	}
}