using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using PrettyPrinter;

#if DEBUG
using Sigil;
using Label = Sigil.Label;
#else
using SigilLite;
using Label = SigilLite.Label;
#endif

using static System.Console;

namespace SharpStation {
	public class Value {
		readonly Action Generate;

		public Value(Action generate) => Generate = generate;

		public void Emit() => Generate();

		public void EmitThen(Action next) {
			Generate();
			next();
		}
	}

	public class SettableValue : Value {
		readonly Action<Value> GenerateSetter;

		public SettableValue(Action generateGetter, Action<Value> generateSetter) : base(generateGetter) => GenerateSetter = generateSetter;

		public void Set(Value value) => GenerateSetter(value);
	}

	public class Block {
		public readonly uint Addr;
		public Action<Recompiler> Func;

		public Block(uint addr) => Addr = addr;
	}

	public partial class Recompiler : Cpu {
		public struct RGprs {
			public Value this[uint reg] {
				get => reg == 0 ? MakeValue(0U)
					: new SettableValue(() => {
						GprRef.Emit();
						Ilg.LoadConstant(reg);
						Ilg.LoadElement<uint>();
					}, sv => {
						GprRef.Emit();
						Ilg.LoadConstant(reg);
						sv.Emit();
						Ilg.StoreElement<uint>();
					});
				set {
					if(reg == 0) return;
					GprRef.Emit();
					Ilg.LoadConstant(reg);
					value.Emit();
					Ilg.StoreElement<uint>();
				}
			}
		}

		static TypeBuilder Tb;
		static Emit<Action<Recompiler>> Ilg;
		
		static readonly Value CpuRef = new Value(() => Ilg.LoadArgument(0));
		static readonly Value GprRef = new Value(() => CpuRef.EmitThen(() => Ilg.LoadField(typeof(Recompiler).GetField(nameof(Gpr)))));
		
		static RGprs Gprs;

		Value this[string fieldName] {
			get => new Value(() => CpuRef.EmitThen(() => Ilg.LoadField(typeof(Recompiler).GetField(fieldName))));
			set => CpuRef.EmitThen(() => value.EmitThen(() => Ilg.StoreField(typeof(Recompiler).GetField(fieldName))));
		}

		Value ReadAbsorbRef { get => this[nameof(ReadAbsorb)]; set => this[nameof(ReadAbsorb)] = value; }
		Value ReadAbsorbWhichRef { get => this[nameof(ReadAbsorbWhich)]; set => this[nameof(ReadAbsorbWhich)] = value; }
		Value LDWhichRef { get => this[nameof(LdWhich)]; set => this[nameof(LdWhich)] = value; }
		Value LDAbsorbRef { get => this[nameof(LdAbsorb)]; set => this[nameof(LdAbsorb)] = value; }
		Value LDValueRef { get => this[nameof(LdValue)]; set => this[nameof(LdValue)] = value; }
		Value ReadFudgeRef { get => this[nameof(ReadFudge)]; set => this[nameof(ReadFudge)] = value; }
		Value HiRef { get => this[nameof(Hi)]; set => this[nameof(Hi)] = value; }
		Value LoRef { get => this[nameof(Lo)]; set => this[nameof(Lo)] = value; }

		void InterceptBlock(uint pc) {
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
		}
		
		public override void Run(uint pc) {
			while(true) {
				if(BranchToBlock != null) {
					var func = LastBlock = BranchToBlock.Func;
					pc = LastBlockAddr = BranchToBlock.Addr;
					BranchToBlock = null;
					InterceptBlock(pc);
					if(func == null)
						pc = RecompileBlock(pc);
					else {
						func(this);
						pc = BranchTo;
					}
				} else {
					InterceptBlock(pc);
					pc = RecompileBlock(pc);
				}
			}
		}

		readonly Dictionary<uint, Block> BlockCache = new Dictionary<uint, Block>();

		Action<Recompiler> LastBlock;
		uint LastBlockAddr = ~0U;

		Dictionary<string, (FieldBuilder, Block)> CurBlockRefs;
		public Block BranchToBlock;

		string TtyBuf = "";
		uint RecompileBlock(uint pc, Block block = null) {
			//$"Running block at {pc:X8}".Print();
			if(pc == LastBlockAddr && LastBlock != null) {
				LastBlock(this);
				return BranchTo;
			}

			block ??= GetBlock(pc);
			if(block.Func != null) {
				LastBlock = block.Func;
				LastBlockAddr = pc;
				LastBlock(this);
				return BranchTo;
			}
			
			CurBlockRefs = new Dictionary<string, (FieldBuilder, Block)>();

			var ab = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString()), AssemblyBuilderAccess.Run);
			var mb = ab.DefineDynamicModule("Block");
			Tb = mb.DefineType("Block");
			var mname = $"Block_{pc:X8}";
			Ilg = Emit<Action<Recompiler>>.BuildMethod(Tb, mname, MethodAttributes.Static | MethodAttributes.Public, CallingConventions.Standard);
			
			var opc = pc;
			var branched = false;
			var no_delay = false;
			var need_load = true;
			var did_delay = false;
			while(!did_delay) {
				var insn = Memory.Load32(pc);
				//WriteLine($"{pc:X}:  {Disassemble(pc, insn)}");

				if(branched)
					did_delay = true;
				
				var has_load = false;
				if(!Recompile(pc, insn, ref branched, ref no_delay, ref has_load, need_load))
					throw new NotSupportedException($"Unknown instruction at 0x{pc:X8}");
				
				need_load = has_load;

				if(branched && no_delay)
					did_delay = true;

				pc += 4;
			}
			Ilg.Return();
			Ilg.CreateMethod();
			var type = Tb.CreateType();
			foreach(var (fn, (_, b)) in CurBlockRefs)
				type.GetField(fn).SetValue(null, b);
			var func = type.GetMethod(mname).CreateDelegate<Action<Recompiler>>();

			block.Func = LastBlock = func;
			LastBlockAddr = opc;
			func(this);
			return BranchTo;
		}

		Block GetBlock(uint addr) =>
			BlockCache.TryGetValue(addr, out var block) ? block : BlockCache[addr] = new Block(addr);

		void Branch(Label label) => Ilg.Branch(label);
		
		void Branch(Value target) {
			CpuRef.Emit();
			target.Emit();
			Ilg.StoreField(typeof(Recompiler).GetField(nameof(BranchTo)));
		}
		void Branch(uint target) {
			var fname = $"_{target:X8}";
			var block = GetBlock(target);
			if(CurBlockRefs.TryGetValue(fname, out var br)) {
				CpuRef.Emit();
				Ilg.LoadField(br.Item1);
				Ilg.StoreField(typeof(Recompiler).GetField(nameof(BranchToBlock)));
			} else {
				var fb = Tb.DefineField(fname, typeof(Block), FieldAttributes.Public | FieldAttributes.Static);
				CurBlockRefs[fname] = (fb, block);
				CpuRef.Emit();
				Ilg.LoadField(fb);
				Ilg.StoreField(typeof(Recompiler).GetField(nameof(BranchToBlock)));
			}
		}

		void BranchIf(Value condition, Label label) => condition.EmitThen(() => Ilg.BranchIfTrue(label));
		void Label(Label label) => Ilg.MarkLabel(label);
		
		public static Value MakeValue<T>(T value) =>
			value is uint i
				? new Value(() => {
						Ilg.LoadConstant(i);
						Ilg.Convert<uint>();
					})
				: throw new NotImplementedException($"Unknown type to MakeValue: {typeof(T)}");

		Value Load(int bits, Value ptr, uint pc) => new Value(() => {
			CpuRef.Emit();
			Ilg.LoadConstant(bits);
			ptr.Emit();
			Ilg.LoadConstant(pc);
			Ilg.Call(typeof(Recompiler).GetMethod("LoadMemory"));
		});

		void Store(Value ptr, Value value) {
			if(ptr is SettableValue sv)
				sv.Set(value);
			else
				throw new NotSupportedException("Assignment to non-settable value");
		}

		void Store(int bits, Value ptr, Value value, uint pc) {
			CpuRef.Emit();
			Ilg.LoadConstant(bits);
			ptr.Emit();
			value.Emit();
			Ilg.LoadConstant(pc);
			Ilg.Call(typeof(Recompiler).GetMethod("StoreMemory"));
		}

		void DoLds() {
			GprRef.Emit();
			LDWhichRef.Emit();
			LDValueRef.Emit();
			Ilg.StoreElement<uint>();

			ReadAbsorbRef.Emit();
			LDWhichRef.Emit();
			LDAbsorbRef.Emit();
			Ilg.StoreElement<uint>();

			/*var which = Ilg.DeclareLocal(typeof(uint));
			LDWhichRef.Emit();
			Ilg.StoreLocal(which);
			var value = Ilg.DeclareLocal(typeof(uint));
			LDValueRef.Emit();
			Ilg.StoreLocal(value);
			Ilg.WriteLine("DoLds {0} {1:X}", which, value);*/

			ReadFudgeRef = LDWhichRef;

			ReadAbsorbWhichRef = Or(ReadAbsorbWhichRef, 
				Ternary(Ne(LDWhichRef, MakeValue(35U)), 
					And(LDWhichRef, MakeValue(0x1FU)), 
					MakeValue(0U)
				));
			LDWhichRef = MakeValue(35U);
		}
		
		Value Ternary(Value cond, Value a, Value b) => new Value(() => {
			Label _if = Ilg.DefineLabel(), end = Ilg.DefineLabel();
			cond.Emit();
			Ilg.BranchIfTrue(_if);
			b.Emit();
			Ilg.Branch(end);
			Ilg.MarkLabel(_if);
			a.Emit();
			Ilg.MarkLabel(end);
		});
		
		void DEP(uint reg) {
			if(reg != 0)
				WRA(MakeValue(reg), MakeValue(0U));
		}
		void RES(uint reg) {
			if(reg != 0)
				WRA(MakeValue(reg), MakeValue(0U));
		}

		void DeferSet(uint reg, Value value) {
			if(reg == 0)
				value.EmitThen(() => Ilg.StoreLocal(Ilg.DeclareLocal<uint>()));
			else {
				LDWhichRef = MakeValue(reg);
				LDValueRef = value;
			}
		}

		Value RRA(Value idx) => new Value(() => {
			ReadAbsorbRef.Emit();
			idx.Emit();
			Ilg.LoadElement<uint>();
		});
		void WRA(Value idx, Value val) {
			ReadAbsorbRef.Emit();
			idx.Emit();
			val.Emit();
			Ilg.StoreElement<uint>();
		}
		
		void TimestampInc(int inc) {}
		void GenAbsorbMuldivDelay() {}
		void MulDelay(Value a, Value b, bool signed) {}
		void GenDivDelay() {}
		
		void Alignment(Value value, int bits, bool store, uint pc) {}
		void Overflow(Value a, Value b, int dir, uint pc, uint inst) {}

		void Syscall(uint code, uint pc, uint inst) =>
			CpuRef.EmitThen(() => {
				Ilg.LoadConstant((int) code);
				Ilg.LoadConstant(pc);
				Ilg.LoadConstant(inst);
				Ilg.Call(typeof(Recompiler).GetMethod(nameof(Syscall)));
				Branch(pc + 4);
			});
		void Break(uint code, uint pc, uint inst) =>
			CpuRef.EmitThen(() => {
				Ilg.LoadConstant((int) code);
				Ilg.LoadConstant(pc);
				Ilg.LoadConstant(inst);
				Ilg.Call(typeof(Recompiler).GetMethod(nameof(Break)));
				Branch(pc + 4);
			});

		Value GenReadCopreg(uint cop, uint reg) => new Value(() => CpuRef.EmitThen(() => {
			Ilg.LoadConstant(cop);
			Ilg.LoadConstant(reg);
			Ilg.Call(typeof(Recompiler).GetMethod(nameof(ReadCopreg)));
		}));
		Value GenReadCopcreg(uint cop, uint reg) => new Value(() => CpuRef.EmitThen(() => {
			Ilg.LoadConstant(cop);
			Ilg.LoadConstant(reg);
			Ilg.Call(typeof(Recompiler).GetMethod(nameof(ReadCopcreg)));
		}));

		void WriteCopreg(uint cop, uint reg, Value value) {
			CpuRef.Emit();
			Ilg.LoadConstant(cop);
			Ilg.LoadConstant(reg);
			value.Emit();
			Ilg.Call(typeof(Recompiler).GetMethod(nameof(WriteCopreg)));
		}
		void WriteCopcreg(uint cop, uint reg, Value value) {
			CpuRef.Emit();
			Ilg.LoadConstant(cop);
			Ilg.LoadConstant(reg);
			value.Emit();
			Ilg.Call(typeof(Recompiler).GetMethod(nameof(WriteCopcreg)));
		}
		void GenCopfun(uint cop, uint cofun, uint inst) {
			CpuRef.Emit();
			Ilg.LoadConstant(cop);
			Ilg.LoadConstant(cofun);
			Ilg.LoadConstant(inst);
			Ilg.Call(typeof(Recompiler).GetMethod(nameof(Copfun)));
		}

		Value Add(Value a, Value b) => new Value(() => a.EmitThen(() => b.EmitThen(() => Ilg.Add())));
		Value Sub(Value a, Value b) => new Value(() => a.EmitThen(() => b.EmitThen(() => Ilg.Subtract())));
		Value Mul(Value a, Value b) => new Value(() => a.EmitThen(() => b.EmitThen(() => Ilg.Multiply())));
		Value Mul64(Value a, Value b) {
			var tempLocal = Ilg.DeclareLocal<long>();
			a.Emit();
			b.Emit();
			Ilg.Multiply();
			Ilg.StoreLocal(tempLocal);
			return new Value(() => Ilg.LoadLocal(tempLocal));
		}
		Value UMul64(Value a, Value b) {
			var tempLocal = Ilg.DeclareLocal<ulong>();
			a.Emit();
			b.Emit();
			Ilg.Multiply();
			Ilg.StoreLocal(tempLocal);
			return new Value(() => Ilg.LoadLocal(tempLocal));
		}
		Value Div(Value a, Value b) => new Value(() => a.EmitThen(() => b.EmitThen(() => Ilg.Divide())));
		Value Mod(Value a, Value b) => new Value(() => a.EmitThen(() => b.EmitThen(() => Ilg.Remainder())));
		
		Value Shl(Value a, Value b) => new Value(() => a.EmitThen(() => b.EmitThen(() => Ilg.ShiftLeft())));
		Value SShr(Value a, Value b) => new Value(() => a.EmitThen(() => b.EmitThen(() => Ilg.ShiftRight())));
		Value UShr(Value a, Value b) => new Value(() => a.EmitThen(() => b.EmitThen(() => Ilg.UnsignedShiftRight())));
		Value And(Value a, Value b) => new Value(() => a.EmitThen(() => b.EmitThen(() => Ilg.And())));
		Value Or(Value a, Value b) => new Value(() => a.EmitThen(() => b.EmitThen(() => Ilg.Or())));
		Value Xor(Value a, Value b) => new Value(() => a.EmitThen(() => b.EmitThen(() => Ilg.Xor())));
		Value Not(Value v) => new Value(() => v.EmitThen(() => Ilg.Not()));

		Value Comp(Value a, Value b, Action<Label> op) => new Value(() => {
			Label _if = Ilg.DefineLabel(), end = Ilg.DefineLabel();
			a.Emit();
			b.Emit();
			op(_if);
			Ilg.LoadConstant(0);
			Ilg.Branch(end);
			Ilg.MarkLabel(_if);
			Ilg.LoadConstant(1);
			Ilg.MarkLabel(end);
		});
		Value Eq(Value a, Value b) => Comp(a, b, l => Ilg.BranchIfEqual(l));
		Value Ne(Value a, Value b) => Comp(a, b, l => Ilg.UnsignedBranchIfNotEqual(l));
		Value Lt(Value a, Value b) => Comp(a, b, l => Ilg.BranchIfLess(l));
		Value Gt(Value a, Value b) => Comp(a, b, l => Ilg.BranchIfGreater(l));
		Value Le(Value a, Value b) => Comp(a, b, l => Ilg.BranchIfLessOrEqual(l));
		Value Ge(Value a, Value b) => Comp(a, b, l => Ilg.BranchIfGreaterOrEqual(l));

		Value ToI32(Value v) => new Value(() => v.EmitThen(() => Ilg.Convert<int>()));
		Value ToI64(Value v) => new Value(() => v.EmitThen(() => Ilg.Convert<long>()));
		Value ToU32(Value v) => new Value(() => v.EmitThen(() => Ilg.Convert<uint>()));
		Value ToU64(Value v) => new Value(() => v.EmitThen(() => Ilg.Convert<ulong>()));
		
		Value Signed(Value v) => new Value(() => v.EmitThen(() => Ilg.Convert<int>()));
		Value Unsigned(Value v) => new Value(() => v.EmitThen(() => Ilg.Convert<uint>()));

		Value SignExt(int size, Value v) {
			switch(size) {
				case 8:
					return new Value(() => {
						v.Emit();
						Ilg.Convert<byte>();
						Ilg.Convert<sbyte>();
						Ilg.Convert<int>();
					});
				case 16:
					return new Value(() => {
						v.Emit();
						Ilg.Convert<ushort>();
						Ilg.Convert<short>();
						Ilg.Convert<int>();
					});
				case 32: return v;
				default:
					var topmask = 1U << (size - 1);
					var sub = 1 << size;
					return Ternary(
						new Value(() => {
							v.Emit();
							Ilg.Duplicate();
							Ilg.LoadConstant(topmask);
							Ilg.And();
							Ilg.LoadConstant(0U);
							Ilg.CompareEqual();
						}), 
						new Value(() => {}), 
						new Value(() => {
							Ilg.Convert<int>();
							Ilg.LoadConstant(sub);
							Ilg.Subtract();
						}));
			}
		}
	}
}