using SharpStation;
using static SharpStation.Globals;

namespace App {
	static class Program {
		static void Main(string[] args) => StartSystem(new CueImage(args[0]));
	}
}