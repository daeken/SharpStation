using System;
using System.Linq;
using PrettyPrinter;
using static System.Console;

namespace SharpStation {
	public partial class Interpreter : Cpu {
		void DoLds() {
			if(LdWhich != 0) Gpr[LdWhich] = LdValue;
			ReadAbsorb[LdWhich] = LdAbsorb;
			ReadFudge = LdWhich;
			ReadAbsorbWhich |= LdWhich != 35 ? LdWhich & 0x1F : 0;
			LdWhich = 35;
		}

		public void DoLoad(uint reg, ref uint value) {
			if(LdWhich == reg) {
				ReadFudge = 0;
				value = LdValue;
			} else
				DoLds();
		}

		public void DeferSet(uint reg, uint value) {
			LdWhich = reg;
			LdValue = value;
		}
		
		string TtyBuf = "";

		public override void RunFrom(uint pc) {
			while(true) {
				var insn = Memory.Load32(pc);
				//WriteLine($"{pc:X}:  {Disassemble(pc, insn)}");

				BranchTo = NoBranch;
				RunOne(pc, insn);
			
				pc += 4;

				if(BranchTo != NoBranch && DeferBranch == NoBranch) {
					DeferBranch = BranchTo;
				} else if(DeferBranch != NoBranch) {
					pc = DeferBranch;
					DeferBranch = NoBranch;
				}
			}
		}

		void RunOne(uint pc, uint inst) {
			if(pc == 0x2C94 && Gpr[4] == 1) {
				TtyBuf += string.Join("", Enumerable.Range(0, (int) Gpr[6]).Select(i => (char) Memory.Load8((uint) (Gpr[5] + i))));
				if(TtyBuf.Contains('\n')) {
					var lines = TtyBuf.Split('\n');
					TtyBuf = lines.Last();
					foreach(var line in lines.SkipLast(1)) {
						WriteLine($"TTY: {line}");
						if(line.Contains("VSync"))
							Environment.Exit(0);
					}
				}
			}

			var res = Interpret(pc, inst);
			if(!res) throw new Exception($"Unknown instruction @ {pc:X}");
			if(Gpr[0] != 0) throw new Exception($"R0 != 0 ?! {pc:X}");
			//Console.WriteLine($"Interpret: {res}");
		}
	}
}