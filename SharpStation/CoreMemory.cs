﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.VisualBasic.Devices;
using MoreLinq;
using static System.Console;

namespace SharpStation {
	interface IMemory {
		int Size { get; }
		byte Load8(uint addr);
		ushort Load16(uint addr);
		uint Load32(uint addr);
		void Store8(uint addr, byte value);
		void Store16(uint addr, ushort value);
		void Store32(uint addr, uint value);
	}

	public abstract unsafe class BackedMemory : IMemory {
		protected readonly byte* Backing;
		public abstract int Size { get; } 

		protected BackedMemory(byte* backing) => Backing = backing;
		protected BackedMemory() => Backing = (byte*) Marshal.AllocHGlobal(Size);
		
		public byte Load8(uint addr) => Backing[addr];
		public void Store8(uint addr, byte value) => Backing[addr] = value;
		public ushort Load16(uint addr) => *((ushort*) (Backing + addr));
		public void Store16(uint addr, ushort value) => *((ushort*) (Backing + addr)) = value;
		public uint Load32(uint addr) => *((uint*) (Backing + addr));
		public void Store32(uint addr, uint value) => *((uint*) (Backing + addr)) = value;
	}
	
	public class Ram : BackedMemory {
		public override int Size => 2 * 1024 * 1024;
	}

	public class Scratchpad : BackedMemory {
		public override int Size => 1024;
	}

	public unsafe class Bios : BackedMemory {
		public override int Size => 512 * 1024;

		public Bios() {
			using(var fp = File.OpenRead("SCPH1001.bin")) {
				var bytes = new byte[Size];
				fp.Read(bytes, 0, Size);
				Marshal.Copy(bytes, 0, (IntPtr) Backing, Size);
			}
		}
	}

	public class Port<T> : IEnumerable where T : struct {
		public uint Addr { get; }
		public string? Name { get; }

		public Func<T>? _Load;
		public Action<T>? _Store;

		int BitSize => typeof(T).Name switch {
			"Byte" => 8, 
			"UInt16" => 16, 
			"UInt32" => 32, 
			_ => throw new NotImplementedException($"Unknown type for Port bitsize: {typeof(T).Name}")
		};

		public Port(uint addr, string? name = null) {
			Addr = addr;
			Name = name;
		}

		public void Add(Func<T>? load) => _Load = load;
		
		public void Add(Action<T>? store) => _Store = store;

		public T Load() => _Load?.Invoke() ?? throw new NotImplementedException($"No load{BitSize} for 0x{Addr:X8} ({Name})");

		public void Store(T value) {
			if(_Store == null) throw new NotImplementedException($"No store{BitSize} for 0x{Addr:X8} ({Name})");
			_Store(value);
		}

		public IEnumerator GetEnumerator() => throw new NotImplementedException();
	}

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = true)]
	public class PortAttribute : Attribute {
		public readonly uint Addr, Stride;
		public readonly int Count;
		public PortAttribute(uint addr) => Addr = addr;
		public PortAttribute(uint addr, int count, uint stride) {
			Addr = addr;
			Count = count;
			Debug.Assert(count != 0);
			Stride = stride;
			Debug.Assert(stride != 0);
		}
	}
	
	public class IoPorts : IMemory {
		public int Size => 8 * 1024;

		readonly Dictionary<uint, Port<byte>> Ports8 = new Dictionary<uint, Port<byte>>();
		readonly Dictionary<uint, Port<ushort>> Ports16 = new Dictionary<uint, Port<ushort>>();
		readonly Dictionary<uint, Port<uint>> Ports32 = new Dictionary<uint, Port<uint>>();

		[Port(0x1F802041)] static void POST(byte value) => WriteLine($"BIOS boot status: {value:X2}");

		void Add(Port<byte> port) {
			if(Ports8.ContainsKey(port.Addr)) {
				var cport = Ports8[port.Addr];
				if(cport._Load == null && port._Load != null)
					cport._Load = port._Load;
				else if(cport._Store == null && port._Store != null)
					cport._Store = port._Store;
				else
					throw new Exception($"Port {port.Name} assigned to already-occupied address 0x{port.Addr:X8}");
				return;
			}
			Ports8[port.Addr] = port;
		}
		void Add(Port<ushort> port) {
			if(Ports16.ContainsKey(port.Addr)) {
				var cport = Ports16[port.Addr];
				if(cport._Load == null && port._Load != null)
					cport._Load = port._Load;
				else if(cport._Store == null && port._Store != null)
					cport._Store = port._Store;
				else
					throw new Exception($"Port {port.Name} assigned to already-occupied address 0x{port.Addr:X8}");
				return;
			}
			Ports16[port.Addr] = port;
		}
		void Add(Port<uint> port) {
			if(Ports32.ContainsKey(port.Addr)) {
				var cport = Ports32[port.Addr];
				if(cport._Load == null && port._Load != null)
					cport._Load = port._Load;
				else if(cport._Store == null && port._Store != null)
					cport._Store = port._Store;
				else
					throw new Exception($"Port {port.Name} assigned to already-occupied address 0x{port.Addr:X8}");
				return;
			}
			Ports32[port.Addr] = port;
		}
		
		public IoPorts() {
			Port<T> MapProperty<T>(object? instance, uint addr, string name, PropertyInfo pi) where T : struct {
				var port = new Port<T>(addr, name);
				if(pi.GetMethod != null) port.Add(() => (T) pi.GetValue(instance));
				if(pi.SetMethod != null) port.Add(v => pi.SetValue(instance, v));
				return port;
			}
			
			AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes())
				.SelectMany(x => x.GetMembers(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
				.Where(x => x.GetCustomAttributes(typeof(PortAttribute)).Count() != 0)
				.ForEach(x => {
					var attr = x.GetCustomAttribute<PortAttribute>();
					var addr = attr.Addr;
					var name = $"{x.DeclaringType.Name}.{x.Name}";
					var instance = x.DeclaringType.GetField("Instance",
						BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(null);
					instance ??=
						typeof(Globals)
							.GetFields(BindingFlags.Public | BindingFlags.Static)
							.FirstOrDefault(y => y.FieldType == x.DeclaringType)?.GetValue(null);
					if(attr.Count == 0 && x is FieldInfo fi) {
						if(!fi.IsStatic && instance == null)
							throw new Exception($"Port {name} is not static and no instance available");
						if(fi.FieldType == typeof(byte))
							Add(new Port<byte>(addr, name)
								{ () => (byte) fi.GetValue(instance), v => fi.SetValue(instance, v) });
						else if(fi.FieldType == typeof(ushort))
							Add(new Port<ushort>(addr, name)
								{ () => (ushort) fi.GetValue(instance), v => fi.SetValue(instance, v) });
						else if(fi.FieldType == typeof(uint))
							Add(new Port<uint>(addr, name)
								{ () => (uint) fi.GetValue(instance), v => fi.SetValue(instance, v) });
						else
							throw new NotImplementedException($"Field {x.DeclaringType.Name}.{x} not a supported type");
					} else if(x is FieldInfo f) {
						if(!f.IsStatic && instance == null)
							throw new Exception($"Port {name} is not static and no instance available");
						if(f.FieldType == typeof(byte[])) {
							var arr = new byte[attr.Count];
							f.SetValue(instance, arr);
							Enumerable.Range(0, attr.Count).ForEach(i => Add(
								new Port<byte>(addr + (uint) (attr.Stride * i), name) { () => arr[i], v => arr[i] = v }));
						} else if(f.FieldType == typeof(ushort[])) {
							var arr = new ushort[attr.Count];
							f.SetValue(instance, arr);
							Enumerable.Range(0, attr.Count).ForEach(i => Add(
								new Port<ushort>(addr + (uint) (attr.Stride * i), name) { () => arr[i], v => arr[i] = v }));
						} else if(f.FieldType == typeof(uint[])) {
							var arr = new uint[attr.Count];
							f.SetValue(instance, arr);
							Enumerable.Range(0, attr.Count).ForEach(i => Add(
								new Port<uint>(addr + (uint) (attr.Stride * i), name) { () => arr[i], v => arr[i] = v }));
						} else
							throw new NotImplementedException($"Field {x.DeclaringType.Name}.{x} not a supported type");
					} else if(x is PropertyInfo pi) {
						if((pi.GetMethod != null && !pi.GetMethod.IsStatic || pi.SetMethod != null && !pi.SetMethod.IsStatic) && instance == null)
							throw new Exception($"Port {name} is not static and no instance available");
						if(attr.Count != 0)
							throw new Exception($"Port {name} is multi-port but not a field");
						if(pi.PropertyType == typeof(byte)) Add(MapProperty<byte>(instance, addr, name, pi));
						else if(pi.PropertyType == typeof(ushort)) Add(MapProperty<ushort>(instance, addr, name, pi));
						else if(pi.PropertyType == typeof(uint)) Add(MapProperty<uint>(instance, addr, name, pi));
						else throw new NotImplementedException($"Property {x.DeclaringType.Name}.{x} not a supported type");
					} else if(attr.Count != 0 && x is MethodInfo mi) {
						if(!mi.IsStatic && instance == null)
							throw new Exception($"Port {name} is not static and no instance available");
						if(mi.ReturnType == typeof(void)) {
							var t = mi.GetParameters()[1].ParameterType;
							if(t == typeof(byte)) Enumerable.Range(0, attr.Count).ForEach(
								i => Add(new Port<byte>(addr + (uint) (attr.Stride * i), name)
									{ v => mi.Invoke(instance, new object[] { i, v }) }));
							else if(t == typeof(ushort)) Enumerable.Range(0, attr.Count).ForEach(i => 
								Add(new Port<ushort>(addr + (uint) (attr.Stride * i), name)
									{ v => mi.Invoke(instance, new object[] { i, v }) }));
							else if(t == typeof(uint)) Enumerable.Range(0, attr.Count).ForEach(i => 
								Add(new Port<uint>(addr + (uint) (attr.Stride * i), name)
									{ v => mi.Invoke(instance, new object[] { i, v }) }));
							else throw new NotImplementedException($"Method {x.DeclaringType.Name}.{x} not a supported type");
						} else {
							var t = mi.ReturnType;
							if(t == typeof(byte)) Enumerable.Range(0, attr.Count).ForEach(i => 
								Add(new Port<byte>(addr + (uint) (attr.Stride * i), name)
									{ () => (byte) mi.Invoke(instance, new object[] { i }) }));
							else if(t == typeof(ushort)) Enumerable.Range(0, attr.Count).ForEach(i => 
								Add(new Port<ushort>(addr + (uint) (attr.Stride * i), name)
									{ () => (ushort) mi.Invoke(instance, new object[] { i }) }));
							else if(t == typeof(uint)) Enumerable.Range(0, attr.Count).ForEach(i => 
								Add(new Port<uint>(addr + (uint) (attr.Stride * i), name)
									{ () => (uint) mi.Invoke(instance, new object[] { i }) }));
							else throw new NotImplementedException($"Method {x.DeclaringType.Name}.{x} not a supported type");
						}
					} else if(x is MethodInfo m) {
						if(!m.IsStatic && instance == null)
							throw new Exception($"Port {name} is not static and no instance available");
						if(m.ReturnType == typeof(void)) {
							var t = m.GetParameters()[0].ParameterType;
							if(t == typeof(byte)) Add(new Port<byte>(addr, name) { v => m.Invoke(instance, new[] { (object) v }) });
							else if(t == typeof(ushort)) Add(new Port<ushort>(addr, name) { v => m.Invoke(instance, new[] { (object) v }) });
							else if(t == typeof(uint)) Add(new Port<uint>(addr, name) { v => m.Invoke(instance, new[] { (object) v }) });
							else throw new NotImplementedException($"Method {x.DeclaringType.Name}.{x} not a supported type");
						} else {
							var t = m.ReturnType;
							if(t == typeof(byte)) Add(new Port<byte>(addr, name) { () => (byte) m.Invoke(instance, null) });
							else if(t == typeof(ushort)) Add(new Port<ushort>(addr, name) { () => (ushort) m.Invoke(instance, null) });
							else if(t == typeof(uint)) Add(new Port<uint>(addr, name) { () => (uint) m.Invoke(instance, null) });
							else throw new NotImplementedException($"Method {x.DeclaringType.Name}.{x} not a supported type");
						}
					}
				});

			Ports32.Values.Where(port => !Ports16.ContainsKey(port.Addr)).ForEach(port =>
				Add(new Port<ushort>(port.Addr, port.Name) {
					port._Load != null ? () => (ushort) port.Load() : (Func<ushort>?) null,
					port._Store != null ? value => port.Store(value) : (Action<ushort>?) null
				}));
		}

		public byte Load8(uint addr) => Ports8.ContainsKey(addr) ? Ports8[addr].Load()
			: throw new NotImplementedException($"Unknown port for load8: {addr:X8}");
		public void Store8(uint addr, byte value) {
			if(!Ports8.ContainsKey(addr)) throw new NotImplementedException($"Unknown port for store8: {addr:X8} (0x{value:X2})");
			Ports8[addr].Store(value);
		}

		public ushort Load16(uint addr) => Ports16.ContainsKey(addr) ? Ports16[addr].Load()
			: throw new NotImplementedException($"Unknown port for load16: {addr:X8}");
		public void Store16(uint addr, ushort value) {
			if(!Ports16.ContainsKey(addr)) throw new NotImplementedException($"Unknown port for store16: {addr:X8} (0x{value:X4})");
			Ports16[addr].Store(value);
		}

		public uint Load32(uint addr) => Ports32.ContainsKey(addr) ? Ports32[addr].Load()
			: throw new NotImplementedException($"Unknown port for load32: {addr:X8}");
		public void Store32(uint addr, uint value) {
			if(!Ports32.ContainsKey(addr)) throw new NotImplementedException($"Unknown port for store32: {addr:X8} (0x{value:X8})");
			Ports32[addr].Store(value);
		}
	}

	public class Blackhole : IMemory {
		public int Size => 0x7FFFFFFF;
		
		public byte Load8(uint addr) => 0;
		public ushort Load16(uint addr) => 0;
		public uint Load32(uint addr) => 0;

		public void Store8(uint addr, byte value) {}
		public void Store16(uint addr, ushort value) {}
		public void Store32(uint addr, uint value) {}
	}
	
	public class CoreMemory {
		readonly Ram Ram = new Ram();
		readonly Scratchpad Scratchpad = new Scratchpad();
		readonly IoPorts IoPorts = new IoPorts();
		readonly Bios Bios = new Bios();
		readonly Blackhole Blackhole = new Blackhole();

		T FindMemory<T>(uint vaddr, Func<IMemory, uint, T> func) {
			var rvaddr = vaddr;
			if(rvaddr >= 0x80000000U && rvaddr < 0xA0000000U)
				rvaddr -= 0x80000000U;
			else if(rvaddr >= 0xA0000000U && rvaddr < 0xFFFE0000U)
				rvaddr -= 0xA0000000U;
			
			if(rvaddr < Ram.Size) return func(Ram, rvaddr);
			if(rvaddr >= 0x1F000000 && rvaddr < 0x1F800000) return func(Blackhole, rvaddr);
			if(rvaddr >= 0x1F800000 && rvaddr < 0x1F800400) return func(Scratchpad, rvaddr - 0x1F800000);
			if(rvaddr >= 0x1F801000 && rvaddr < 0x1F803000) return func(IoPorts, rvaddr);
			if(rvaddr >= 0x1FC00000U && rvaddr < 0x1FC80000U) return func(Bios, rvaddr - 0x1FC00000);
			if(rvaddr >= 0xFFFE0000 && rvaddr < 0xFFFE0200) return func(IoPorts, rvaddr - 0xFFFE0000 + 0x1F801000);
			throw new NotImplementedException($"Unknown memory region for addr {rvaddr:X08}h");
		}
		
		void FindMemory(uint vaddr, Action<IMemory, uint> func) {
			FindMemory<uint>(vaddr, (i, a) => {
				func(i, a);
				return 0;
			});
		}

		CoreMemory LogLoad(uint addr, int size) {
			//WriteLine($"Load {size} bytes from {addr:X}");
			return this;
		}
		CoreMemory LogStore(uint addr, uint value, int size) {
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