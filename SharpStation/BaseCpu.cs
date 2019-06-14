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

	public struct ICacheEntry {
		public uint TV, Data;
	}

	public abstract partial class BaseCpu {
		public const uint FreqHz = 33_868_500;
		
		public readonly uint[] Gpr = new uint[36];
		public uint Lo, Hi;
		public uint Pc = 0xBFC00000U;
		public uint LdWhich, LdValue, LdAbsorb;
		public readonly uint[] ReadAbsorb = new uint[36];
		public uint ReadAbsorbWhich, ReadFudge, BIU;
		public bool IsolateCache;
		public readonly ICacheEntry[] ICache = new ICacheEntry[1024];

		public const uint NoBranch = ~0U;
		public uint BranchTo = NoBranch, DeferBranch = NoBranch;

		public uint IPCache;

		public bool Running, Halted;

		ulong MuldivTsDone;

		public void RunOneFrame() {
			Running = true;
			
			while(Timestamp < Events.NextTimestamp || Events.RunEvents())
				try {
					if(Pc == 0xBFC0D850)
						Pc = Gpr[31];
					if(DebugMemory)
						$"Running block at {Pc:X8}".Debug();
					
					if(IPCache != 0) {
						if(Halted) {
						} else if((CP0.StatusRegister & 1) != 0) {
							DispatchException(new CpuException(ExceptionType.INT, Pc, Pc, 0xFF, 0));
							continue;
						}
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

		public void AssertIrq(bool asserted) {
			const uint mask = 1U << 10;
			CP0.Cause &= ~mask;
			if(asserted)
				CP0.Cause |= mask;

			RecalcIPCache();
		}

		protected abstract void Run();
		public abstract void Invalidate(uint addr);

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
			if(cop == 2)
				return CP2[reg];
			throw new NotSupportedException($"Read from unknown coprocessor {cop}");
		}

		public void WriteCopreg(uint cop, uint reg, uint value) {
			//WriteLine($"Write cop{cop}r{reg} <- 0x{value:X}");
			if(cop == 0)
				CP0[reg] = value;
			else if(cop == 2)
				CP2[reg] = value;
			else
				throw new NotSupportedException($"Write to unknown coprocessor {cop}");
		}

		public uint ReadCopcreg(uint cop, uint reg) {
			if(cop == 0)
				return CP0.Copcreg(reg);
			if(cop == 2)
				return CP2.Copcreg(reg);
			throw new NotSupportedException($"CRead from unknown coprocessor {cop}");
		}

		public void WriteCopcreg(uint cop, uint reg, uint value) {
			if(cop == 0)
				CP0.Copcreg(reg, value);
			else if(cop == 2)
				CP2.Copcreg(reg, value);
			else
				throw new NotSupportedException($"CWrite to unknown coprocessor {cop}");
		}

		public void Copfun(uint cop, uint cofun, uint inst) {
			//WriteLine($"Call cop{cop} function {cofun}");
			if(cop == 0)
				CP0.Call(cofun, inst);
			else if(cop == 2)
				CP2.Call(cofun, inst);
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

		public static int SignExt(int size, uint imm) => imm.SignExt(size);

		public bool DebugMemory;

		public uint LoadMemory(int size, uint addr, uint pc) {
			if(DebugMemory)
				$"Load {size/8} bytes from {addr:X8} -- {Pc:X8}".Debug();
			if(addr == 0x80083C58) return 0;
			switch(size) {
				case 8: return Memory.Load8(addr);
				case 16: return Memory.Load16(addr);
				case 24: return Memory.Load32(addr) & 0xFFFFFF;
				case 32: return Memory.Load32(addr);
			}
			return 0;
		}

		public void StoreMemory(int size, uint addr, uint value, uint pc) {
			if(IsolateCache)
				return;
			
			if(DebugMemory)
				$"Store {size/8} bytes to {addr:X8} <- {value:X} -- {Pc:X8}".Debug();
			switch(size) {
				case 8: Memory.Store8(addr, (byte) value); break;
				case 16: Memory.Store16(addr, (ushort) value); break;
				case 24:
					Memory.Store16(addr, (ushort) value);
					Memory.Store8(addr + 2, (byte) (value >> 16));
					break;
				case 32: Memory.Store32(addr, value); break;
			}
		}

		public void RegisterDebug() {
			$"$0  {Gpr[ 0]:X8}    $1  {Gpr[ 1]:X8}    $2  {Gpr[ 2]:X8}    $3  {Gpr[ 3]:X8}".Debug();
			$"$4  {Gpr[ 4]:X8}    $5  {Gpr[ 5]:X8}    $6  {Gpr[ 6]:X8}    $7  {Gpr[ 7]:X8}".Debug();
			$"$8  {Gpr[ 8]:X8}    $9  {Gpr[ 9]:X8}    $10 {Gpr[10]:X8}    $11 {Gpr[11]:X8}".Debug();
			$"$12 {Gpr[12]:X8}    $13 {Gpr[13]:X8}    $14 {Gpr[14]:X8}    $15 {Gpr[15]:X8}".Debug();
			
			$"$16 {Gpr[16]:X8}    $17 {Gpr[17]:X8}    $18 {Gpr[18]:X8}    $19 {Gpr[19]:X8}".Debug();
			$"$20 {Gpr[20]:X8}    $21 {Gpr[21]:X8}    $22 {Gpr[22]:X8}    $23 {Gpr[23]:X8}".Debug();
			$"$24 {Gpr[24]:X8}    $25 {Gpr[25]:X8}    $26 {Gpr[26]:X8}    $27 {Gpr[27]:X8}".Debug();
			$"$28 {Gpr[28]:X8}    $29 {Gpr[29]:X8}    $30 {Gpr[30]:X8}    $31 {Gpr[31]:X8}".Debug();
			
			$"LO  {Lo:X8}    HI  {Hi:X8}    PC  {Pc:X8}".Debug();
		}
	}
}