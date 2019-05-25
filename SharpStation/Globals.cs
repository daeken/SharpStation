namespace SharpStation {
	public class Globals {
		public static uint Timestamp;
		
		public static IRenderer Renderer = new OpenGLRasterizer();

		public static readonly EventSystem Events = new EventSystem();
		//public static readonly BaseCpu Cpu = new Interpreter();
		public static readonly BaseCpu Cpu = new Recompiler();
		public static readonly Cop0 CP0 = new Cop0();
		public static readonly CoreGpu Gpu = new CoreGpu();
		public static readonly CoreCdrom Cdrom = new CoreCdrom();
		public static readonly CoreDma Dma = new CoreDma();
		public static readonly CoreIrq Irq = new CoreIrq();
		public static readonly CoreMemory Memory = new CoreMemory();

		public static void StartSystem() => Renderer.KickOff(() => {
			while(true)
				Cpu.RunOneFrame();
		});
	}
}