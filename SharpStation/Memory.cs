using System;
using System.IO;
using System.Runtime.InteropServices;
using static System.Console;

namespace SharpStation {
	interface IMemory {
		uint Size { get; }
		byte Load8(uint addr);
		ushort Load16(uint addr);
		uint Load32(uint addr);
		void Store8(uint addr, byte value);
		void Store16(uint addr, ushort value);
		void Store32(uint addr, uint value);
	}

	public abstract unsafe class BackedMemory : IMemory {
		protected readonly byte* Backing;
		public abstract uint Size { get; } 

		protected BackedMemory(byte* backing) => Backing = backing;
		
		public byte Load8(uint addr) => Backing[addr];
		public void Store8(uint addr, byte value) => Backing[addr] = value;
		public ushort Load16(uint addr) => *((ushort*) (Backing + addr));
		public void Store16(uint addr, ushort value) => *((ushort*) (Backing + addr)) = value;
		public uint Load32(uint addr) => *((uint*) (Backing + addr));
		public void Store32(uint addr, uint value) => *((uint*) (Backing + addr)) = value;
	}
	
	public unsafe class Ram : BackedMemory {
		const int _Size = 2 * 1024 * 1024;
		public override uint Size => _Size;
		
		public Ram() : base((byte*) Marshal.AllocHGlobal(_Size)) {}
	}

	public unsafe class Scratchpad : BackedMemory {
		const int _Size = 1024;
		public override uint Size => _Size;
		
		public Scratchpad() : base((byte*) Marshal.AllocHGlobal(_Size)) {}
	}

	public unsafe class Bios : BackedMemory {
		const int _Size = 512 * 1024;
		public override uint Size => _Size;

		public Bios() : base((byte*) Marshal.AllocHGlobal(_Size)) {
			using(var fp = File.OpenRead("SCPH1001.bin")) {
				var bytes = new byte[_Size];
				fp.Read(bytes, 0, _Size);
				Marshal.Copy(bytes, 0, (IntPtr) Backing, _Size);
			}
		}
	}

	public class IoPorts : IMemory {
		public uint Size => 8 * 1024;

		readonly Cpu Cpu;

		public IoPorts(Cpu cpu) => Cpu = cpu;
		
		public byte Load8(uint addr) => throw new NotImplementedException();
		public void Store8(uint addr, byte value) {
			WriteLine($"Storing to 8-bit IO port {addr:X8} <- {value:X2}");
			if(addr == 0x1F802041)
				WriteLine($"BIOS boot status {value:X2}");
		}

		public ushort Load16(uint addr) => throw new NotImplementedException();
		public void Store16(uint addr, ushort value) {
			WriteLine($"Storing to 16-bit IO port {addr:X8} <- {value:X4}");
		}

		public uint Load32(uint addr) => throw new NotImplementedException();

		public void Store32(uint addr, uint value) {
			WriteLine($"Storing to 32-bit IO port {addr:X8} <- {value:X8}");
		}
	}

	public class Blackhole : IMemory {
		public uint Size => 0xFFFFFFFF;
		
		public byte Load8(uint addr) => 0;
		public ushort Load16(uint addr) => 0;
		public uint Load32(uint addr) => 0;

		public void Store8(uint addr, byte value) {}
		public void Store16(uint addr, ushort value) {}
		public void Store32(uint addr, uint value) {}
	}
	
	public class Memory {
		public readonly Cpu Cpu;
		readonly Ram Ram = new Ram();
		readonly Scratchpad Scratchpad = new Scratchpad();
		readonly IoPorts IoPorts;
		readonly Bios Bios = new Bios();
		readonly Blackhole Blackhole = new Blackhole();

		public Memory(Cpu cpu) {
			Cpu = cpu;
			IoPorts = new IoPorts(Cpu);
		}

		T FindMemory<T>(uint vaddr, Func<IMemory, uint, T> func) {
			var rvaddr = vaddr;
			if(rvaddr >= 0x80000000U && rvaddr < 0xA0000000U)
				rvaddr -= 0x80000000U;
			else if(rvaddr >= 0xA0000000U && rvaddr < 0xFFFE0000U)
				rvaddr -= 0xA0000000U;
			
			if(rvaddr < Ram.Size)
				return func(Ram, rvaddr);
			else if(rvaddr >= 0x1F000000 && rvaddr < 0x1F800000)
				return func(Blackhole, rvaddr);
			else if(rvaddr >= 0x1F800000 && rvaddr < 0x1F800400)
				return func(Scratchpad, rvaddr - 0x1F800000);
			else if(rvaddr >= 0x1F801000 && rvaddr < 0x1F803000)
				return func(IoPorts, rvaddr);
			else if(rvaddr >= 0x1FC00000U && rvaddr < 0x1FC80000U)
				return func(Bios, rvaddr - 0x1FC00000);
			else if(rvaddr >= 0xFFFE0000 && rvaddr < 0xFFFE0200)
				return func(IoPorts, rvaddr - 0xFFFE0000 + 0x1F801000);
			throw new NotImplementedException();
		}
		
		void FindMemory(uint vaddr, Action<IMemory, uint> func) {
			FindMemory<uint>(vaddr, (i, a) => {
				func(i, a);
				return 0;
			});
		}

		Memory LogLoad(uint addr, int size) {
			//WriteLine($"Load {size} bytes from {addr:X}");
			return this;
		}
		Memory LogStore(uint addr, uint value, int size) {
			//WriteLine($"Store {size} bytes ({value:X}) to {addr:X}");
			return this;
		}

		public byte Load8(uint addr) => LogLoad(addr, 1).FindMemory(addr, (i, a) => i.Load8(a));
		public void Store8(uint addr, byte value) => LogStore(addr, value, 1).FindMemory(addr, (i, a) => i.Store8(a, value));
		public ushort Load16(uint addr) => LogLoad(addr, 2).FindMemory(addr, (i, a) => i.Load16(a));
		public void Store16(uint addr, ushort value) => LogStore(addr, value, 2).FindMemory(addr, (i, a) => i.Store16(a, value));
		public uint Load32(uint addr) => LogLoad(addr, 4).FindMemory(addr, (i, a) => i.Load32(a));
		public void Store32(uint addr, uint value) => LogStore(addr, value, 4).FindMemory(addr, (i, a) => i.Store32(a, value));
	}
}