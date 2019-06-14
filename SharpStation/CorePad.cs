using System;
using PrettyPrinter;
using static SharpStation.Globals;
#pragma warning disable 414

// TODO: Understand wtf all of this is doing. This is pretty much 100% from Rustation.

#pragma warning disable 169
namespace SharpStation {
	public class Pad {
		public bool Active = true;
		public byte Seq;
		
		public (byte Response, bool Dsr) SendCommand(byte cmd) {
			if(!Active) return (0xFF, false);
			Active = false;
			Seq++;
			return (0xFF, false); // Disconnected
		}

		public void Select() {
			Active = true;
			Seq = 0;
		}
	}

	public abstract class BusState {
		public static readonly BusIdle Idle = new BusIdle();
		public static Transfer Transfer(byte response, bool dsr, uint remainingCycles) => new Transfer(response, dsr, remainingCycles);
		public static readonly Dsr Dsr = new Dsr();
	}
	public class BusIdle : BusState {}
	public class Transfer : BusState {
		public readonly byte Response;
		public readonly bool DSR;
		public readonly uint RemainingCycles;
		public Transfer(byte response, bool dsr, uint remainingCycles) {
			Response = response;
			DSR = dsr;
			RemainingCycles = remainingCycles;
		}
	}
	public class Dsr : BusState {}
	
	public class CorePad {
		BusState State = BusState.Idle;
		bool RxNotEmpty, RxEnable, TxEnable, Select, Dsr, DsrIt, Interrupt;
		byte Response, Target;
		readonly Pad Pad1 = new Pad(), Pad2 = new Pad();
		uint Unknown;
		
		[Port(0x1F801040)]
		byte Data {
			get {
				var v = Response;
				RxNotEmpty = false;
				Response = 0xFF;
				return v;
			}
			set {
				if(!TxEnable) throw new NotSupportedException("Write to gamepad when Tx disabled");
				if(State != BusState.Idle) throw new NotSupportedException("Write to gamepad while bus not idle");

				var (response, dsr) = Select ? (Target == 0 ? Pad1 : Pad2).SendCommand(value) : ((byte) 0xFF, false);
				var txDuration = 8U * Baud;
				State = BusState.Transfer(response, dsr, txDuration);
				Events.Add(Timestamp + txDuration, Sync);
			} 
		}

		[Port(0x1F801044)] ushort Stat => (ushort) (
			5 |
			RxNotEmpty.ToBit(1) | 
			Dsr.ToBit(7) | 
			Interrupt.ToBit(9));
			
		[Port(0x1F801048)] ushort Mode;

		[Port(0x1F80104A)]
		ushort Control {
			get => (ushort) (
				Unknown | 
				TxEnable.ToBit(0) | 
				Select.ToBit(1) | 
				RxEnable.ToBit(2) | 
				DsrIt.ToBit(12) | 
				((uint) Target << 13)
			);
			set {
				if(value.HasBit(6)) {
					Baud = 0;
					Mode = 0;
					Select = false;
					Target = 0;
					Unknown = 0;
					Interrupt = false;
					RxNotEmpty = false;
					State = BusState.Idle;
					Dsr = false;
				} else {
					if(value.HasBit(4)) {
						Interrupt = false;
						if(Dsr && DsrIt) {
							Interrupt = true;
							Irq.Assert(IrqType.Sio, true);
						} else
							Irq.Assert(IrqType.Sio, false);
					}

					var oldSelect = Select;
					Unknown = value & 0x28U;
					TxEnable = value.HasBit(0);
					Select = value.HasBit(1);
					RxEnable = value.HasBit(2);
					DsrIt = value.HasBit(12);
					Target = (byte) (value.HasBit(13) ? 1 : 0);

					if(!oldSelect && Select)
						Pad1.Select();
				}
				Events.Add(Timestamp + 1000000, () => Irq.Assert(IrqType.Sio, true));
			}
		}
		[Port(0x1F80104E)] ushort Baud;

		void Sync() {
			//$"CorePad sync {State.ToPrettyString()}".Debug();
			switch(State) {
				case BusIdle _: break;
				case Transfer transfer:
					Response = transfer.Response;
					RxNotEmpty = true;
					Dsr = transfer.DSR;

					if(Dsr) {
						if(DsrIt) {
							if(!Interrupt)
								Irq.Assert(IrqType.Sio, true);
							Interrupt = true;
						}
						
						State = BusState.Dsr;
						Events.Add(Timestamp + 10, Sync);
					} else
						State = BusState.Idle;
					break;
				case Dsr dsr:
					Dsr = false;
					State = BusState.Idle;
					break;
			}
		}
	}
}