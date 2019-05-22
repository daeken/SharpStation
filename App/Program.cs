using SharpStation;

namespace App {
	class Program {
		static void Main(string[] args) {
			//var cpu = new Interpreter();
			var cpu = new Recompiler();
			cpu.Run();
		}
	}
}