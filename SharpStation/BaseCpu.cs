using System;
using System.Diagnostics;
using static SharpStation.Globals;

namespace SharpStation {
	public interface ICoprocessor {
		uint this[uint register] { get; set; }
		void Call(uint func, uint inst);
	}

	public enum ExceptionType {
		INT  = 0, // Interrupt
		ADEL = 4, // Address error, Data load or Instruction fetch
		ADES = 5, // Address error, Data store
		          // The address errors occur when attempting to read
		          // outside of KUseg in user mode and when the address
		          // is misaligned. (See also: BadVaddr register)
		IBE  = 6, // Bus error on Instruction fetch
		DBE  = 7, // Bus error on Data load/store
		Syscall , // Generated unconditionally by syscall instruction
		Break   , // Breakpoint - break instruction
		RI      , // Reserved instruction
		CPU     , // Coprocessor Unusable
		OV      , // Arithmetic overflow
	}

	public class CpuException : Exception {
		public readonly ExceptionType Type;
		public readonly uint PC, NP, NPM, Inst;
		
		public CpuException(ExceptionType type, uint pc, uint np, uint npm, uint inst) {
			Type = type;
			PC = pc;
			NP = np;
			NPM = npm;
			Inst = inst;
		}
	}

	public abstract partial class BaseCpu {
		public readonly uint[] Gpr = new uint[36];
		public uint Lo, Hi;
		public uint Pc = 0xBFC00000U;
		public uint LdWhich, LdValue, LdAbsorb;
		public readonly uint[] ReadAbsorb = new uint[36];
		public uint ReadAbsorbWhich, ReadFudge;
		public bool IsolateCache;

		public const uint NoBranch = ~0U;
		public uint BranchTo = NoBranch, DeferBranch = NoBranch;

		public uint IPCache;

		public bool Running, Halted;

		uint MuldivTsDone;

		public void RunOneFrame() {
			Running = true;
			
			while(Timestamp < Events.NextTimestamp || Events.RunEvents())
				try {
					//$"Foo? {Pc:X}".Debug();
					if(IPCache != 0) {
						//"IPCache != 0!".Debug();
						if(Halted) {
						} else if((CP0.StatusRegister & 1) != 0)
							DispatchException(new CpuException(ExceptionType.INT, Pc, Pc, 0xFF, 0));
					}
					Run();
				} catch (CpuException ce) {
					DispatchException(ce);
				}
		}

		void DispatchException(CpuException exc) {
			var afterBranchInst = (exc.NPM & 0x1) == 0;
			var branchTaken = (exc.NPM & 0x3) == 0;
			var handler = 0x80000080;

			if((CP0.StatusRegister & (1 << 22)) != 0) // BEV
				handler = 0xBFC00180;

			CP0.EPC = exc.PC;
			if(afterBranchInst) {
				CP0.EPC -= 4;
				CP0.TargetAddress = (exc.PC & (exc.NPM | 3)) + exc.NP;
			}

			// "Push" IEc and KUc(so that the new IEc and KUc are 0)
			CP0.StatusRegister = (CP0.StatusRegister & ~0x3FU) | ((CP0.StatusRegister << 2) & 0x3FU);
			
			// Setup cause register
			CP0.Cause &= 0x0000FF00;
			CP0.Cause |= (uint) exc.Type << 2;

			// If EPC was adjusted -= 4 because we are after a branch instruction, set bit 31.
			CP0.Cause |= (afterBranchInst ? 1U : 0) << 31;
			CP0.Cause |= (branchTaken ? 1U : 0) << 30;
			CP0.Cause |= (exc.Inst << 2) & (3 << 28); // CE
			
			RecalcIPCache();

			Pc = handler;
		}

		public void RecalcIPCache() {
			IPCache = (CP0.StatusRegister & CP0.Cause & 0xFF00) != 0 && (CP0.StatusRegister & 1) != 0 || Halted
				? 0x80U
				: 0;
		}

		public void AssertIrq(int which, bool asserted) {
			$"Asserting {which} {asserted}".Debug();
			Debug.Assert(which <= 5);
			
			var mask = 1U << (10 + which);
			CP0.Cause &= ~mask;

			if(asserted)
				CP0.Cause |= mask;

			RecalcIPCache();
		}

		protected abstract void Run();

		public void Alignment(uint addr, int size, bool store, uint pc) {
			if(size == 16 && (addr & 1) != 0 || size == 32 && (addr & 3) != 0)
				throw new CpuException(store ? ExceptionType.ADES : ExceptionType.ADEL, pc, pc, 0xFF, 0);
		}

		public void Syscall(int code, uint pc, uint inst) =>
			throw new CpuException(ExceptionType.Syscall, pc, pc + 4, 0xFF, inst);

		public void Break(int code, uint pc, uint inst) =>
			throw new CpuException(ExceptionType.Break, pc, pc + 4, 0xFF, inst);

		public uint ReadCopreg(uint cop, uint reg) {
			//WriteLine($"Read cop{cop}r{reg}");
			if(cop == 0)
				return CP0[reg];
			throw new NotSupportedException($"Read from unknown coprocessor {cop}");
		}

		public void WriteCopreg(uint cop, uint reg, uint value) {
			//WriteLine($"Write cop{cop}r{reg} <- 0x{value:X}");
			if(cop == 0)
				CP0[reg] = value;
			else
				throw new NotSupportedException($"Write to unknown coprocessor {cop}");
		}

		public uint ReadCopcreg(uint cop, uint reg) {
			return 0;
		}

		public void WriteCopcreg(uint cop, uint reg, uint value) { }

		public void Copfun(uint cop, uint cofun, uint inst) {
			//WriteLine($"Call cop{cop} function {cofun}");
			if(cop == 0)
				CP0.Call(cofun, inst);
			else
				throw new NotSupportedException($"Call to unknown coprocessor {cop}");
		}

		public void AbsorbMuldivDelay() {
			if(Timestamp >= MuldivTsDone) return;
			if(Timestamp == MuldivTsDone - 1)
				MuldivTsDone--;
			else
				do {
					if(ReadAbsorb[ReadAbsorbWhich] != 0)
						ReadAbsorb[ReadAbsorbWhich]--;
					Timestamp++;
				} while(Timestamp < MuldivTsDone);
		}

		static readonly uint[] MultTab = {
			14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 10, 10, 10, 10, 10, 10, 10, 10, 10, 7, 7, 7
		};
		
		public void MulDelay(uint a, uint b, bool isSigned) {
			if(isSigned)
				MuldivTsDone = Timestamp + MultTab[((a ^ (uint) ((int) b >> 31)) | 0x400).CountLeadingZeros()];
			else
				MuldivTsDone = Timestamp + MultTab[(a | 0x400).CountLeadingZeros()];
		}

		public void DivDelay() => MuldivTsDone = Timestamp + 37;

		public void Overflow(uint a, uint b, int dir, uint pc, uint inst) {
			if(dir == 1) {
				var r = unchecked(a + b);
				if((~(a ^ b) & (a ^ r) & 0x80000000) != 0)
					throw new CpuException(ExceptionType.OV, pc, pc, 0xFF, inst);
			} else {
				var r = unchecked(a - b);
				if(((a ^ b) & (a ^ r) & 0x80000000) != 0)
					throw new CpuException(ExceptionType.OV, pc, pc, 0xFF, inst);
			}
		}

		public int SignExt(int size, uint imm) {
			unchecked {
				switch(size) {
					case 8: return (sbyte) (byte) imm;
					case 16: return (short) (ushort) imm;
					case 32: return (int) imm;
					case int _ when (imm & (1 << (size - 1))) != 0: return (int) imm - (1 << size);
					default: return (int) imm;
				}
			}
		}

		public uint LoadMemory(int size, uint addr, uint pc) {
			//$"Load {size/8} bytes from {addr:X8} -- {Pc:X8}".Debug();
			switch(size) {
				case 8: return Memory.Load8(addr);
				case 16: return Memory.Load16(addr);
				case 32: return Memory.Load32(addr);
			}
			return 0;
		}

		public void StoreMemory(int size, uint addr, uint value, uint pc) {
			if(IsolateCache)
				return;
			
			//$"Store {size/8} bytes to {addr:X8} <- {value:X} -- {Pc:X8}".Debug();
			switch(size) {
				case 8: Memory.Store8(addr, (byte) value); break;
				case 16: Memory.Store16(addr, (ushort) value); break;
				case 32: Memory.Store32(addr, value); break;
			}
		}
	}
}