namespace SharpStation {
	public static class Globals {
		public static readonly BaseCpu Cpu = new Interpreter();
		//public static readonly BaseCpu Cpu = new Recompiler();
		public static readonly Cop0 CP0 = new Cop0();
		public static readonly CoreGpu Gpu = new CoreGpu();
		public static readonly CoreDma Dma = new CoreDma();
		public static readonly CoreMemory Memory = new CoreMemory();
		public static readonly CoreIrq Irq = new CoreIrq();

		public static void StartSystem() => Cpu.Run();
	}
}