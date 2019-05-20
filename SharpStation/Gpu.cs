// ReSharper disable UnusedMember.Global

using System;
using System.ComponentModel.Design.Serialization;
using System.Linq;
using System.Reflection;
using MoreLinq;
using PrettyPrinter;
#pragma warning disable 414

namespace SharpStation {
	[AttributeUsage(AttributeTargets.Method)]
	class Gp0Command : Attribute {
		public readonly byte Command;
		public Gp0Command(byte command) => Command = command;
	}
	
	[AttributeUsage(AttributeTargets.Method)]
	class Gp1Command : Attribute {
		public readonly byte Command;
		public Gp1Command(byte command) => Command = command;
	}

	class CurCommand {
		public readonly MethodInfo Func;
		public readonly object[] Fifo;
		public int Off;

		public CurCommand(MethodInfo func, int count) {
			Func = func;
			Fifo = new object[count];
		}

		public void Add(uint value) => Fifo[Off++] = value;
	}
	
	public class Gpu {
		static readonly Gpu Instance = new Gpu();
		
		readonly (int Count, MethodInfo Func)[] Gp0Commands = new (int, MethodInfo)[0x100];
		readonly (int Count, MethodInfo Func)[] Gp1Commands = new (int, MethodInfo)[0x100];

		CurCommand? CurGp0, CurGp1;

		Gpu() {
			typeof(Gpu).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
				.Select(x => (Method: x, Attr: x.GetCustomAttribute<Gp0Command>())).Where(x => x.Attr != null)
				.ForEach(x => Gp0Commands[x.Attr.Command] = (x.Method.GetParameters().Length, x.Method));
			typeof(Gpu).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
				.Select(x => (Method: x, Attr: x.GetCustomAttribute<Gp1Command>())).Where(x => x.Attr != null)
				.ForEach(x => Gp1Commands[x.Attr.Command] = (x.Method.GetParameters().Length, x.Method));
		}

		[Port(0x1F801810)] uint Read = 0xd0;
		[Port(0x1F801814)] uint Stat;

		[Port(0x1F801810)]
		void Gp0Incoming(uint value) {
			if(CurGp0 == null) {
				var cmd = (byte) (value >> 24);
				var (count, func) = Gp0Commands[cmd];
				if(func == null) throw new NotImplementedException($"GP0 command {cmd:X02}h not implemented");
				switch(count) {
					case 0: func.Invoke(this, null); break;
					case 1: func.Invoke(this, new[] { (object) value }); break;
					default:
						CurGp0 = new CurCommand(func, count);
						CurGp0.Add(value);
						break;
				}
			} else {
				CurGp0.Add(value);
				if(CurGp0.Off == CurGp0.Fifo.Length) {
					CurGp0.Func.Invoke(this, CurGp0.Fifo);
					CurGp0 = null;
				}
			}
		}

		[Port(0x1F801814)]
		void Gp1Incoming(uint value) {
			if(CurGp1 == null) {
				var cmd = (byte) (value >> 24);
				var (count, func) = Gp1Commands[cmd];
				if(func == null) throw new NotImplementedException($"GP1 command {cmd:X02}h not implemented");
				switch(count) {
					case 0: func.Invoke(this, null); break;
					case 1: func.Invoke(this, new[] { (object) value }); break;
					default:
						CurGp1 = new CurCommand(func, count);
						CurGp1.Add(value & 0x00FFFFFF);
						break;
				}
			} else {
				CurGp1.Add(value);
				if(CurGp1.Off == CurGp1.Fifo.Length) {
					CurGp1.Func.Invoke(this, CurGp1.Fifo);
					CurGp1 = null;
				}
			}
		}
		
		[Gp0Command(0x00)] void Nop() {}

		[Gp0Command(0xE1)]
		void SetDrawMode(uint value) {
			$"Setting draw mode to {value:X06}".Print();
			Stat = 0xFFFFFFFF;
		}

		[Gp1Command(0x00)]
		void Reset() => "GPU reset!".Print();

		[Gp1Command(0x04)]
		void DmaDirectionDataRequest(uint value) => $"DMA direction {value & 3}".Print();

		[Gp1Command(0x08)]
		void DisplayMode(uint value) => $"Display mode {value:X06}".Print();
	}
}