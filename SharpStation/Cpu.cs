using System;
using PrettyPrinter;
using static System.Console;

namespace SharpStation {
	public interface ICoprocessor {
		uint this[uint register] { get; set; }
		void Call(uint func, uint inst);
	}
	
	public abstract partial class Cpu {
		public readonly Memory Memory;
		
		public readonly uint[] Gpr = new uint[36];
		public uint Lo, Hi;
		public uint LdWhich, LdValue, LdAbsorb;
		public const uint NoBranch = ~0U;
		public uint BranchTo = NoBranch, DeferBranch = NoBranch;

		public readonly ICoprocessor[] Coprocessors = new ICoprocessor[4];
		
		public readonly uint[] ReadAbsorb = new uint[36];
		public uint ReadAbsorbWhich, ReadFudge;

		public bool IsolateCache;
		
		protected Cpu() {
			Memory = new Memory(this);
			Coprocessors[0] = new Cop0(this);
		}

		public abstract void Run(uint pc);

		public void Alignment(uint addr, int size, bool store, uint pc) {
			if((size == 16 && (addr & 1) != 0) || (size == 32 && (addr & 3) != 0)) {
				//Interrupt(Exception(store ? EXCEPTION_ADES : EXCEPTION_ADEL, pc, pc, 0xFF, 0));
			}
		}

		public void Syscall(int code, uint pc, uint inst) {
			$"Syscall {code} at {pc:X8}".Print();
		}

		public void Break(int code, uint pc, uint inst) {
		}

		public uint ReadCopreg(uint cop, uint reg) {
			//WriteLine($"Read cop{cop}r{reg}");
			if(Coprocessors[cop] == null)
				throw new Exception($"Read from null coprocessor {cop}");
			return Coprocessors[cop][reg];
		}
		
		public void WriteCopreg(uint cop, uint reg, uint value) {
			//WriteLine($"Write cop{cop}r{reg} <- 0x{value:X}");
			if(Coprocessors[cop] == null)
				throw new Exception($"Write to null coprocessor {cop}");
			Coprocessors[cop][reg] = value;
		}

		public uint ReadCopcreg(uint cop, uint reg) {
			return 0;
		}
		
		public void WriteCopcreg(uint cop, uint reg, uint value) {
		}

		public void Copfun(uint cop, uint cofun, uint inst) {
			WriteLine($"Call cop{cop} function {cofun}");
			if(Coprocessors[cop] == null)
				throw new Exception($"Call to null coprocessor {cop}");
			Coprocessors[cop].Call(cofun, inst);
		}

		public void AbsorbMuldivDelay() {
		}
		
		public void MulDelay(uint a, uint b, bool isSigned) {
			/*if(isSigned)
				cpu->muldiv_ts_done = gtimestamp + cpu->MULT_Tab24[MDFN_lzcount32((a ^ ((int32_t) b >> 31)) | 0x400)];
			else
				cpu->muldiv_ts_done = gtimestamp + cpu->MULT_Tab24[MDFN_lzcount32(a | 0x400)];*/
		}

		public void DivDelay() {
		}

		public void Overflow(uint a, uint b, int dir, uint pc, uint instr) {
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
			//WriteLine($"Load {size/8} bytes from {addr:X8}");
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
			
			//WriteLine($"Store {size/8} bytes to {addr:X8} <- {value:X}");
			switch(size) {
				case 8: Memory.Store8(addr, (byte) value); break;
				case 16: Memory.Store16(addr, (ushort) value); break;
				case 32: Memory.Store32(addr, value); break;
			}
		}
	}
}