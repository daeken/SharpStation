using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using MoreLinq.Extensions;
using PrettyPrinter;
using static SharpStation.Globals;
#pragma warning disable 169

// TODO: Understand wtf all of this is doing. This is 80% from Rustation.

namespace SharpStation {
	[AttributeUsage(AttributeTargets.Method)]
	class CdCommand : Attribute {
		public readonly byte Command;
		public CdCommand(byte command) => Command = command;
	}
	
	public class CoreCdrom : ISyncable {
		enum CdIrq {
			SectorReady = 1, 
			AsyncOk = 2, 
			Ok = 3, 
			Error = 5
		}
		
		enum SubCpuState {
			Idle, 
			CommandPending, 
			ParamPush, 
			Execution, 
			RxFlush, 
			RxPush, 
			BusyDelay, 
			IrqDelay, 
			AsyncRxPush
		}
		
		class SubCpu {
			public readonly Queue<byte> ParameterFifo = new Queue<byte>();
			public readonly Queue<byte> ResponseFifo = new Queue<byte>();
			
			public SubCpuState State = SubCpuState.Idle;
			public CdIrq CurIrq = CdIrq.Ok;

			public ulong Timer;
			public (ulong Delay, Func<ulong> Func)? AsyncResponse;

			public bool Busy {
				get {
					switch(State) {
						case SubCpuState.Idle:
						case SubCpuState.IrqDelay:
						case SubCpuState.AsyncRxPush:
							return false;
					}
					return true;
				}
			}

			public void StartCommand(ulong delay) {
				if(State != SubCpuState.Idle) throw new Exception($"New command to subcpu while in non-idle state {State}");
				
				State = SubCpuState.CommandPending;
				Timer = delay;
				ParameterFifo.Clear();
				ResponseFifo.Clear();

				CurIrq = CdIrq.Ok;
			}
		}
		
		readonly Syncer Syncer;
		readonly SubCpu Sub = new SubCpu();
		readonly Random Rng = new Random();
		
		readonly (int Count, MethodInfo Func)[] Commands = new (int, MethodInfo)[0x100];
		
		uint Index;

		readonly Queue<byte> ParameterFifo = new Queue<byte>();
		readonly Queue<byte> ResponseFifo = new Queue<byte>();
		public readonly Queue<byte> DataFifo = new Queue<byte>();

		uint InterruptEnable, InterruptFlag;
		byte RequestRegister;
		byte VolumeLeftToLeft, VolumeLeftToRight, VolumeRightToRight, VolumeRightToLeft;
		byte VolumeApplyChanges;

		bool ReadWholeSector = true, ReadPending, DoubleSpeed;

		int? Command;
		ulong? ReadingDelay;
		bool HaveError;
		uint ReadPosition;
		uint? SeekTarget;

		public CoreCdrom() {
			Syncer = new Syncer(this);
			typeof(CoreCdrom).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
				.Select(x => (Method: x, Attr: x.GetCustomAttribute<CdCommand>())).Where(x => x.Attr != null)
				.ForEach(x => Commands[x.Attr.Command] = (x.Method.GetParameters().Length, x.Method));
		}

		[Port(0x1F801800, debug: true)]
		byte IndexStatus {
			get => (byte) (
				Index |
				(ParameterFifo.Count == 0).ToBit(3) |
				(1U << 4) | 
				(ResponseFifo.Count != 0).ToBit(5) | 
				(DataFifo.Count != 0).ToBit(6) | 
				(Sub.State != SubCpuState.Idle).ToBit(7)
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
			get => throw new NotImplementedException();
			set {
				switch(Index) {
					case 0:
						ParameterFifo.Enqueue(value);
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

		public void Sync(ulong remaining) {
			while(remaining > 0) {
				var elapsed = remaining;
				if(Sub.State != SubCpuState.Idle) {
					if(Sub.Timer > remaining)
						Sub.Timer -= remaining;
					else {
						elapsed = Sub.Timer;
						StepSubCpu();
					}
				}

				if(Sub.AsyncResponse is var (delay, func)) {
					if(delay > elapsed)
						Sub.AsyncResponse = (delay - elapsed, func);
					else {
						Sub.AsyncResponse = (0, func);
						ProcessAsync();
					}
				}

				if(ReadingDelay != null) {
					if(ReadingDelay > elapsed) ReadingDelay -= elapsed;
					else {
						var remainder = elapsed - ReadingDelay.Value;
						ReadSector();
						NotifyRead();
						ReadingDelay = CyclesPerSector - remainder;
					}
				}
				
				remaining -= elapsed;
			}
			
			UpdateEvent();
		}

		void UpdateEvent() {
			var nextUpdate = 0xFFFFFFFFFFFFFFFF;
			if(Sub.State != SubCpuState.Idle)
				nextUpdate = Timestamp + Sub.Timer;
			else if(InterruptFlag == 0 && Sub.AsyncResponse is var (delay, _))
				nextUpdate = Timestamp + delay;
			if(ReadingDelay != null && nextUpdate > Timestamp + ReadingDelay)
				nextUpdate = Timestamp + ReadingDelay.Value;
			Syncer.NextTimestamp = nextUpdate;
		}

		void ReadSector() {
			if(ReadPending)
				throw new Exception("ReadSector while read pending");
			
			var cdata = new byte[2352];
			$"Reading sector from {ReadPosition.ToMsf().ToPrettyString()}".Debug();
			Cd.ReadSector(cdata, ReadPosition.ToMsf());
			if(ReadWholeSector)
				for(var i = 12; i < 2352; ++i)
					DataFifo.Enqueue(cdata[i]);
			else
				for(var i = 24; i < 24 + 2048; ++i)
					DataFifo.Enqueue(cdata[i]);
			if(ReadWholeSector)
				$"Bytes at 0x20: {cdata.Skip(12 + 0x20).Take(8).ToArray().ToPrettyString()}".Debug();
			else
				$"PYTES at 0x20: {cdata.Skip(24 + 0x20).Take(8).ToArray().ToPrettyString()}".Debug();
			ReadPosition++;
			ReadPending = true;
		}

		void NotifyRead() {
			if(!ReadPending || InterruptFlag != 0 || Sub.State != SubCpuState.Idle) return;
			
			Sub.CurIrq = CdIrq.SectorReady;
			Sub.ResponseFifo.Clear();
			Sub.ResponseFifo.Enqueue(DriveStatus);

			Sub.State = SubCpuState.AsyncRxPush;
			Sub.Timer = 1_800;
			ReadPending = false;
			
			UpdateEvent();
		}

		void ProcessAsync() {
			if(!(Sub.AsyncResponse is (0, var action)) || InterruptFlag != 0 || Sub.State != SubCpuState.Idle) return;

			Sub.AsyncResponse = null;
			Sub.ResponseFifo.Clear();
			Sub.CurIrq = CdIrq.AsyncOk;
			
			Sub.Timer = action();
			Sub.State = SubCpuState.AsyncRxPush;
			
			UpdateEvent();
		}

		void StepSubCpu() {
			switch(Sub.State) {
				case SubCpuState.Idle: break;
				case SubCpuState.CommandPending:
				case SubCpuState.ParamPush:
					if(ParameterFifo.Count == 0) {
						if(Command == null) throw new NotSupportedException();
						var (count, func) = Commands[Command.Value];
						if(func == null) throw new NotImplementedException($"CD-ROM command {Command:X2}h not implemented");
						$"Running command {func.Name}".Debug();
						if(count != Sub.ParameterFifo.Count) throw new Exception($"Parameter count mismatch in CD-ROM command {Command:X2} -- got {Sub.ParameterFifo.Count}, expected {count}");
						if(count == 0) func.Invoke(this, null);
						else {
							var cmd = new List<object>();
							for(var i = 0; i < count; ++i)
								if(Sub.ParameterFifo.TryDequeue(out var data))
									cmd.Add(data);
							func.Invoke(this, cmd.ToArray());
						}

						Sub.Timer = 2_000;
						Sub.State = SubCpuState.Execution;
					} else {
						Sub.ParameterFifo.Enqueue(ParameterFifo.Dequeue());
						Sub.Timer = 1_800;
						Sub.State = SubCpuState.ParamPush;
					}
					break;
				case SubCpuState.Execution:
					ResponseFifo.Clear();
					Sub.Timer = 3_500;
					Sub.State = SubCpuState.RxFlush;
					break;
				case SubCpuState.RxFlush:
				case SubCpuState.RxPush:
					ResponseFifo.Enqueue(Sub.ResponseFifo.Dequeue());

					if(Sub.ResponseFifo.Count == 0) {
						Sub.Timer = 3_300;
						Sub.State = SubCpuState.BusyDelay;
					} else {
						Sub.Timer = 1_500;
						Sub.State = SubCpuState.RxPush;
					}
					break;
				case SubCpuState.BusyDelay:
					Sub.Timer = 2_000;
					Sub.State = SubCpuState.IrqDelay;
					break;
				case SubCpuState.IrqDelay:
					Command = null;
					TriggerIrq(Sub.CurIrq);
					Sub.State = SubCpuState.Idle;
					break;
				case SubCpuState.AsyncRxPush:
					ResponseFifo.Enqueue(Sub.ResponseFifo.Dequeue());

					if(Sub.ResponseFifo.Count == 0) {
						Sub.Timer = 2_000;
						Sub.State = SubCpuState.IrqDelay;
					} else
						Sub.Timer = 1_500;
					break;
			}
		}

		void TriggerIrq(CdIrq irq) {
			Debug.Assert(InterruptFlag == 0);
			InterruptFlag = (uint) irq;
			if((InterruptFlag & InterruptEnable) != 0)
				Irq.Assert(IrqType.CD, true);
		}

		void IrqAck(uint value) {
			InterruptFlag &= ~value;
			Irq.Assert(IrqType.CD, false);
			TryStartCommand();
			ProcessAsync();
			NotifyRead();
			UpdateEvent();
		}

		void TryStartCommand() {
			if(Command == null || InterruptFlag != 0 || Sub.State != SubCpuState.Idle) return;

			var variation = 3_000;//Rng.Next(6_000);
			Sub.StartCommand(9_400 + (ulong) variation);
			UpdateEvent();
		}

		ulong CyclesPerSector => BaseCpu.FreqHz / 75 / (DoubleSpeed ? 2U : 1);

		byte DriveStatus => (byte) (true.ToBit(1) | (ReadingDelay != null).ToBit(5));

		[CdCommand(0x01)]
		void Stat() {
			Sub.ResponseFifo.Enqueue(DriveStatus);
		}

		void Seek() {
			if(SeekTarget == null) return;
			ReadPosition = SeekTarget.Value;
			SeekTarget = null;
		}

		[CdCommand(0x02)]
		void SetLoc(byte m, byte s, byte f) {
			Sub.ResponseFifo.Enqueue(DriveStatus);
			SeekTarget = (uint) (m.FromBcd() * 60 * 75 + s.FromBcd() * 75 + f.FromBcd());
		}

		[CdCommand(0x06)]
		void ReadN() {
			Sub.ResponseFifo.Enqueue(DriveStatus);
			
			Seek();

			ReadingDelay = CyclesPerSector;
		}

		[CdCommand(0x09)]
		void Pause() {
			Sub.ResponseFifo.Enqueue(DriveStatus);
			Sub.AsyncResponse = (ReadingDelay == null ? 9_000UL : 1_000_000, () => {
				Sub.ResponseFifo.Enqueue(DriveStatus);
				return 1_700;
			});
			ReadingDelay = null;
		}

		[CdCommand(0x0A)]
		void Init() {
			ReadingDelay = null;
			ReadPending = false;
			Sub.ResponseFifo.Enqueue(DriveStatus);
			Sub.AsyncResponse = (900/*_000*/, () => {
				Sub.ResponseFifo.Enqueue(DriveStatus);
				return 1_700;
			});
		}

		[CdCommand(0x0E)]
		void SetMode(byte mode) {
			ReadWholeSector = mode.HasBit(5);
			DoubleSpeed = mode.HasBit(7);
			Sub.ResponseFifo.Enqueue(DriveStatus);
		}

		[CdCommand(0x15)]
		void SeekL() {
			Seek();
			Sub.ResponseFifo.Enqueue(DriveStatus);
			Sub.AsyncResponse = (1_000_000, () => {
				Sub.ResponseFifo.Enqueue(DriveStatus);
				return 1_700;
			});
		}

		[CdCommand(0x19)]
		void Test(byte subCommand) {
			if(subCommand != 0x20) throw new NotImplementedException($"Unknown CD test subcommand {subCommand:X2}");
			Sub.ResponseFifo.Enqueue(0x98);
			Sub.ResponseFifo.Enqueue(0x06);
			Sub.ResponseFifo.Enqueue(0x10);
			Sub.ResponseFifo.Enqueue(0xc3);
		}

		[CdCommand(0x1A)]
		void GetId() {
			Sub.ResponseFifo.Enqueue(DriveStatus);
			Sub.AsyncResponse = (15_000, () => {
				Sub.ResponseFifo.Enqueue(DriveStatus);
				Sub.ResponseFifo.Enqueue(0x00);
				Sub.ResponseFifo.Enqueue(0x20);
				Sub.ResponseFifo.Enqueue(0x00);
				Sub.ResponseFifo.Enqueue((byte) 'S');
				Sub.ResponseFifo.Enqueue((byte) 'C');
				Sub.ResponseFifo.Enqueue((byte) 'E');
				Sub.ResponseFifo.Enqueue((byte) 'A');
				return 3_100;
			});
		}

		[CdCommand(0x1E)]
		void ReadToc() {
			Sub.ResponseFifo.Enqueue(DriveStatus);
			Sub.AsyncResponse = (15_000, () => { // 16_000_000
				Sub.ResponseFifo.Enqueue(DriveStatus);
				return 1_700;
			});
		}
	}
}