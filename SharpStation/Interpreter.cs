using System;
using System.Linq;

namespace SharpStation {
	public partial class Interpreter : Cpu {
		void DoLds() {
			Gpr[LdWhich] = LdValue;
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

		string TtyBuf = "";

		protected override void RunOne(uint pc, uint inst) {
			if(pc == 0x2C94 && Gpr[4] == 1) {
				TtyBuf += string.Join("", Enumerable.Range(0, (int) Gpr[6]).Select(i => (char) Memory.Load8((uint) (Gpr[5] + i))));
				if(TtyBuf.Contains('\n')) {
					var lines = TtyBuf.Split('\n');
					TtyBuf = lines.Last();
					foreach(var line in lines.SkipLast(1))
						Console.WriteLine($"TTY: {line}");
				}
			}
			
			var res = Interpret(pc, inst);
			//Console.WriteLine($"Interpret: {res}");
		}
	}
}