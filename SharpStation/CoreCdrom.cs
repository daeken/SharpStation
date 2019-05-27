using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MoreLinq.Extensions;
using static SharpStation.Globals;
#pragma warning disable 169

namespace SharpStation {
	[AttributeUsage(AttributeTargets.Method)]
	class CdCommand : Attribute {
		public readonly byte Command;
		public CdCommand(byte command) => Command = command;
	}
	
	public class CoreCdrom {
		readonly (int Count, MethodInfo Func)[] Commands = new (int, MethodInfo)[0x100];
		CurCommand? CurCommand;
		
		uint Index;

		readonly Queue<byte> ParameterFifo = new Queue<byte>();
		readonly Queue<byte> ResponseFifo = new Queue<byte>();

		uint InterruptEnable, InterruptFlag;
		byte RequestRegister;
		byte VolumeLeftToLeft, VolumeLeftToRight, VolumeRightToRight, VolumeRightToLeft;
		byte VolumeApplyChanges;
		byte DataFifo8;

		int? Command;

		public CoreCdrom() {
			typeof(CoreCdrom).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
				.Select(x => (Method: x, Attr: x.GetCustomAttribute<CdCommand>())).Where(x => x.Attr != null)
				.ForEach(x => Commands[x.Attr.Command] = (x.Method.GetParameters().Length, x.Method));
		}

		[Port(0x1F801800, debug: true)]
		byte IndexStatus {
			get => (byte) (
				Index |
				(ParameterFifo.Count == 0 ? 1U << 3 : 0) |
				(1U << 4) | 
				(ResponseFifo.Count != 0 ? 1U << 5 : 0) | 
				(ResponseFifo.Count != 0 ? 1U << 6 : 0)
			);
			set => Index = value & 3U;
		}

		[Port(0x1F801801, debug: true)] byte Multiplex1 {
			get => ResponseFifo.Dequeue();
			set {
				switch(Index) {
					case 0: Command = value; TryStartCommand(); break;
					case 3: VolumeRightToRight = value; break;
					default: throw new NotImplementedException($"Unknown set to 0x1F801801 index {Index}: {value:X2}");
				}
			}
		}

		[Port(0x1F801802, debug: true)]
		byte Multiplex2 {
			set {
				switch(Index) {
					case 0:
						if(CurCommand == null)
							ParameterFifo.Enqueue(value);
						else {
							CurCommand.Add(value);
							if(CurCommand.Off == CurCommand.Fifo.Length) {
								CurCommand.Func.Invoke(this, CurCommand.Fifo);
								CurCommand = null;
								Command = null;
							}
						}
						break;
					case 1:
						InterruptEnable = value & 0x1FU;
						if((InterruptEnable & InterruptFlag) != 0)
							Events.Add(Timestamp + 100000, () => Irq.Assert(IrqType.CD, true));
						break;
					case 2:  VolumeLeftToLeft = value; break;
					default: VolumeRightToLeft = value; break;
				}
			}
		}

		[Port(0x1F801803, debug: true)]
		byte Multiplex3 {
			get => (byte) (((Index & 1) == 0 ? InterruptEnable : InterruptFlag) | 0xE0);
			set {
				switch(Index) {
					case 0:  RequestRegister = value; break;
					case 1:
						IrqAck(value & 0x1FU);
						if(value.HasBit(6))
							ParameterFifo.Clear();
						break;
					case 2:  VolumeLeftToRight = value; break;
					default: VolumeApplyChanges = value; break;
				}
			}
		}

		void IrqAck(uint value) {
			InterruptFlag &= ~value;
			Irq.Assert(IrqType.CD, false);
		}

		void TryStartCommand() {
			$"Trying to start command .... ?".Debug();
			if(Command == null || InterruptFlag != 0 || ResponseFifo.Count != 0) return;
			$"Foo?".Debug();
			CurCommand = null;
			var (count, func) = Commands[Command.Value];
			if(func == null) throw new NotImplementedException($"CD-ROM command {Command:X2}h not implemented");
			if(count == 0) func.Invoke(this, null);
			else {
				CurCommand = new CurCommand(func, count);
				for(var i = 0; i < count; ++i)
					if(ParameterFifo.TryDequeue(out var data))
						CurCommand.Add(data);
				if(CurCommand.Fifo.Length == count) {
					func.Invoke(this, CurCommand.Fifo);
					CurCommand = null;
					Command = null;
				}
			}
		}

		[CdCommand(0x01)]
		void Stat() {
			ResponseFifo.Enqueue(0x10);
			Events.Add(Timestamp + 1000, () => {
				InterruptFlag = 3;
				Irq.Assert(IrqType.CD, true);
			});
		}

		[CdCommand(0x02)]
		void SetLoc(byte m, byte s, byte f) {
			ResponseFifo.Enqueue(0x10);
			Events.Add(Timestamp + 1000, () => {
				InterruptFlag = 3;
				Irq.Assert(IrqType.CD, true);
			});
		}

		[CdCommand(0x06)]
		void ReadN() {
			ResponseFifo.Enqueue(0x10);
			Events.Add(Timestamp + 1000, () => {
				InterruptFlag = 3;
				Irq.Assert(IrqType.CD, true);
				Events.Add(Timestamp + 1000, () => {
					ResponseFifo.Enqueue(0x10);
					InterruptFlag = 1;
					Irq.Assert(IrqType.CD, true);
				});
			});
		}

		[CdCommand(0x09)]
		void Pause() {
			ResponseFifo.Enqueue(0x10);
			Events.Add(Timestamp + 100000, () => {
				InterruptFlag = 3;
				Irq.Assert(IrqType.CD, true);
				Events.Add(Timestamp + 100000, () => {
					ResponseFifo.Enqueue(0x10);
					InterruptFlag = 2;
					Irq.Assert(IrqType.CD, true);
					Cpu.DebugMemory = true;
				});
			});
		}

		[CdCommand(0x0A)]
		void Init() {
			ResponseFifo.Enqueue(0x10);
			Events.Add(Timestamp + 1000, () => {
				InterruptFlag = 3;
				Irq.Assert(IrqType.CD, true);
				Events.Add(Timestamp + 1000, () => {
					ResponseFifo.Enqueue(0x10);
					InterruptFlag = 2;
					Irq.Assert(IrqType.CD, true);
				});
			});
		}

		[CdCommand(0x0E)]
		void SetMode(byte mode) {
			ResponseFifo.Enqueue(0x10);
			Events.Add(Timestamp + 1000, () => {
				InterruptFlag = 3;
				Irq.Assert(IrqType.CD, true);
			});
		}

		[CdCommand(0x15)]
		void SeekL() {
			ResponseFifo.Enqueue(0x10);
			Events.Add(Timestamp + 1000, () => {
				InterruptFlag = 3;
				Irq.Assert(IrqType.CD, true);
				Events.Add(Timestamp + 1000, () => {
					ResponseFifo.Enqueue(0x10);
					InterruptFlag = 2;
					Irq.Assert(IrqType.CD, true);
				});
			});
		}

		[CdCommand(0x19)]
		void Test(byte subCommand) {
			if(subCommand != 0x20) throw new NotImplementedException($"Unknown CD test subcommand {subCommand:X2}");
			ResponseFifo.Enqueue(0x98);
			ResponseFifo.Enqueue(0x06);
			ResponseFifo.Enqueue(0x10);
			ResponseFifo.Enqueue(0xc3);
			Events.Add(Timestamp + 1000, () => {
				InterruptFlag = 3;
				Irq.Assert(IrqType.CD, true);
			});
		}
	}
}