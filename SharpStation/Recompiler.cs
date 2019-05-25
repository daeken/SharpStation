using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using static SharpStation.Globals;
#if DEBUG
using Sigil;
using Label = Sigil.Label;
#else
using SigilLite;
using Label = SigilLite.Label;
#endif

using static System.Console;

namespace SharpStation {
	public class Block {
		public readonly uint Addr;
		public uint End;
		public Action<Recompiler>? Func;

		public Block(uint addr) => Addr = addr;
	}

	public partial class Recompiler : BaseCpu {
		public class Value {
			readonly Action Generate;

			public Value(Action generate) => Generate = generate;

			public void Emit() => Generate();

			public void EmitThen(Action next) {
				Generate();
				next();
			}
			
			public static Value operator +(Value a, Value b) => new Value(() => a.EmitThen(() => b.EmitThen(() => Ilg.Add())));
			public static Value operator -(Value a, Value b) => new Value(() => a.EmitThen(() => b.EmitThen(() => Ilg.Subtract())));
			public static Value operator *(Value a, Value b) => new Value(() => a.EmitThen(() => b.EmitThen(() => Ilg.Multiply())));
			public static Value operator /(Value a, Value b) => new Value(() => a.EmitThen(() => b.EmitThen(() => Ilg.Divide())));
			public static Value operator %(Value a, Value b) => new Value(() => a.EmitThen(() => b.EmitThen(() => Ilg.Remainder())));
			
			public static Value operator &(Value a, Value b) => new Value(() => a.EmitThen(() => b.EmitThen(() => Ilg.And())));
			public static Value operator |(Value a, Value b) => new Value(() => a.EmitThen(() => b.EmitThen(() => Ilg.Or())));
			public static Value operator ^(Value a, Value b) => new Value(() => a.EmitThen(() => b.EmitThen(() => Ilg.Xor())));
			public static Value operator ~(Value v) => new Value(() => v.EmitThen(() => Ilg.Not()));

			static Value Comp(Value a, Value b, Action<Label> op) => new Value(() => {
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
			public static Value operator ==(Value a, Value b) => Comp(a, b, l => Ilg.BranchIfEqual(l));
			public static Value operator !=(Value a, Value b) => Comp(a, b, l => Ilg.UnsignedBranchIfNotEqual(l));
			public static Value operator <(Value a, Value b) => Comp(a, b, l => Ilg.BranchIfLess(l));
			public static Value operator >(Value a, Value b) => Comp(a, b, l => Ilg.BranchIfGreater(l));
			public static Value operator <=(Value a, Value b) => Comp(a, b, l => Ilg.BranchIfLessOrEqual(l));
			public static Value operator >=(Value a, Value b) => Comp(a, b, l => Ilg.BranchIfGreaterOrEqual(l));

			public override bool Equals(object obj) => throw new NotImplementedException();
			public override int GetHashCode() => throw new NotImplementedException();
		}

		public class SettableValue : Value {
			readonly Action<Value> GenerateSetter;

			public SettableValue(Action generateGetter, Action<Value> generateSetter) : base(generateGetter) => GenerateSetter = generateSetter;

			public void Set(Value value) => GenerateSetter(value);
		}
		
		struct StaticRef<T> {
			static readonly Type Type = typeof(T);
		
			public Value this[string fieldName] {
				get => new Value(() => Ilg.LoadField(Type.GetField(fieldName)));
				set => value.EmitThen(() => Ilg.StoreField(Type.GetField(fieldName)));
			}
		}

		struct RGprs {
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
		StaticRef<Globals> GlobalRef;

		Value ReadAbsorbRef { get => this[nameof(ReadAbsorb)]; set => this[nameof(ReadAbsorb)] = value; }
		Value ReadAbsorbWhichRef { get => this[nameof(ReadAbsorbWhich)]; set => this[nameof(ReadAbsorbWhich)] = value; }
		Value LDWhichRef { get => this[nameof(LdWhich)]; set => this[nameof(LdWhich)] = value; }
		Value LDAbsorbRef { get => this[nameof(LdAbsorb)]; set => this[nameof(LdAbsorb)] = value; }
		Value LDValueRef { get => this[nameof(LdValue)]; set => this[nameof(LdValue)] = value; }
		Value ReadFudgeRef { get => this[nameof(ReadFudge)]; set => this[nameof(ReadFudge)] = value; }
		Value HiRef { get => this[nameof(Hi)]; set => this[nameof(Hi)] = value; }
		Value LoRef { get => this[nameof(Lo)]; set => this[nameof(Lo)] = value; }

		void InterceptBlock(uint pc) {
			switch(pc) {
				case 0x2C94 when Gpr[4] == 1:
					TtyBuf += string.Join("", Enumerable.Range(0, (int) Gpr[6]).Select(i => (char) Memory.Load8((uint) (Gpr[5] + i))));
					if(TtyBuf.Contains('\n')) {
						var lines = TtyBuf.Split('\n');
						TtyBuf = lines.Last();
						foreach(var line in lines.SkipLast(1))
							WriteLine($"TTY: {line}");
					}
					break;
			}
		}

		protected override void Run() {
			//$"Running block {Pc:X}".Debug();
			if(BranchToBlock != null) {
				var func = LastBlock = BranchToBlock.Func;
				Pc = LastBlockAddr = BranchToBlock.Addr;
				BranchToBlock = null;
				InterceptBlock(Pc);
				if(func == null)
					Pc = RecompileBlock(Pc);
				else {
					func(this);
					Pc = BranchTo;
				}
			} else {
				InterceptBlock(Pc);
				Pc = RecompileBlock(Pc);
			}
		}

		readonly Dictionary<uint, Block> BlockCache = new Dictionary<uint, Block>();

		Action<Recompiler>? LastBlock;
		uint LastBlockAddr = ~0U;

		Dictionary<string, (FieldBuilder, Block)> CurBlockRefs = new Dictionary<string, (FieldBuilder, Block)>();
		Dictionary<uint, Label> BlockInstLabels = new Dictionary<uint, Label>();
		public Block? BranchToBlock;
		Action? BranchToLabel;
		Local? BranchToLabelLocal;
		uint BlockStart, CurPc;

		string TtyBuf = "";
		uint RecompileBlock(uint pc, Block? block = null) {
			//$"Running block at {pc:X8}".Debug();
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
			BlockInstLabels = new Dictionary<uint, Label>();

			var ab = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString()), AssemblyBuilderAccess.Run);
			var mb = ab.DefineDynamicModule("Block");
			Tb = mb.DefineType("Block");
			var mname = $"Block_{pc:X8}";
			Ilg = Emit<Action<Recompiler>>.BuildMethod(Tb, mname, MethodAttributes.Static | MethodAttributes.Public, CallingConventions.Standard);

			BranchToLabel = null;
			BranchToLabelLocal = Ilg.DeclareLocal<bool>();
			
			var opc = BlockStart = pc;
			var branched = false;
			var no_delay = false;
			var need_load = true;
			var did_delay = false;
			while(!did_delay) {
				var insn = Memory.Load32(pc);
				//WriteLine($"{pc:X}:  {Disassemble(pc, insn)}");

				if(branched)
					did_delay = true;
				
				CurPc = pc;
				var has_load = false;
				Ilg.MarkLabel(BlockInstLabels[pc] = Ilg.DefineLabel());
				if(!Recompile(pc, insn, ref branched, ref no_delay, ref has_load, need_load))
					throw new NotSupportedException($"Unknown instruction at 0x{pc:X8}");
				
				need_load = has_load;

				if(branched && no_delay) {
					did_delay = true;
					BranchToLabel?.Invoke();
					BranchToLabel = null;
				} else if(did_delay && BranchToLabel != null) {
					BranchToLabel();
					BranchToLabel = null;
				}

				pc += 4;
			}

			try { Ilg.Return(); } catch (SigilVerificationException) { }

			Ilg.CreateMethod();
			var type = Tb.CreateType();
			foreach(var br in CurBlockRefs)
				type.GetField(br.Key).SetValue(null, br.Value.Item2);
			var func = type.GetMethod(mname).CreateDelegate<Action<Recompiler>>();

			block.End = pc;
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
			if(BlockStart <= target && target <= CurPc) {
				Ilg.LoadConstant(true);
				Ilg.StoreLocal(BranchToLabelLocal);
				BranchToLabel = () => {
					Ilg.LoadLocal(BranchToLabelLocal);
					Ilg.BranchIfTrue(BlockInstLabels[target]);
				};
				return;
			}
			Ilg.LoadConstant(false);
			Ilg.StoreLocal(BranchToLabelLocal);

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

		static Value MakeValue<T>(T value) =>
			value is uint i
				? new Value(() => {
						Ilg.LoadConstant(i);
						Ilg.Convert<uint>();
					})
				: throw new NotImplementedException($"Unknown type to MakeValue: {typeof(T)}");

		static void LoadConstant(object c) {
			switch(c) {
				case bool v: Ilg.LoadConstant(v); break;
				case byte v: Ilg.LoadConstant(v); break;
				case sbyte v: Ilg.LoadConstant(v); break;
				case ushort v: Ilg.LoadConstant(v); break;
				case short v: Ilg.LoadConstant(v); break;
				case uint v: Ilg.LoadConstant(v); break;
				case int v: Ilg.LoadConstant(v); break;
				default: throw new NotImplementedException($"Unknown type for object LoadConstant: {c.GetType()}");
			}
		}

		static Value Call(string methodName, params object[] args) {
			var mi = typeof(BaseCpu).GetMethod(methodName,
				BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			if(mi == null)
				mi = typeof(Recompiler).GetMethod(methodName,
					BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			var value = new Value(() => {
				if(!mi.IsStatic)
					CpuRef.Emit();
				foreach(var a in args)
					if(a is Value v) v.Emit();
					else LoadConstant(a);
				Ilg.Call(mi);
			});
			if(mi.ReturnType == typeof(void))
				value.Emit();
			return value;
		}

		Value Load(int bits, Value ptr, uint pc) => Call(nameof(LoadMemory), bits, ptr, pc);
		void Store(int bits, Value ptr, Value value, uint pc) => Call(nameof(StoreMemory), bits, ptr, value, pc);

		static void Store(Value ptr, Value value) {
			if(ptr is SettableValue sv) sv.Set(value);
			else throw new NotSupportedException("Assignment to non-settable value");
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
			
			ReadFudgeRef = LDWhichRef;

			ReadAbsorbWhichRef |=
				Ternary(LDWhichRef != MakeValue(35U),
					LDWhichRef & MakeValue(0x1FU),
					MakeValue(0U)
				);
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

		void TimestampInc(uint inc) => GlobalRef[nameof(Timestamp)] += MakeValue(inc);
		void GenAbsorbMuldivDelay() => Call(nameof(AbsorbMuldivDelay));
		void MulDelay(Value a, Value b, bool signed) => Call(nameof(MulDelay), a, b, signed);
		void GenDivDelay() => Call(nameof(DivDelay));

		void Alignment(Value value, int bits, bool store, uint pc) => Call(nameof(Alignment), value, bits, store, pc);
		void Overflow(Value a, Value b, int dir, uint pc, uint inst) => Call(nameof(Overflow), a, b, dir, pc, inst);

		void Syscall(uint code, uint pc, uint inst) {
			Call(nameof(Syscall), (int) code, pc, inst);
			Branch(pc + 4);
		}

		void Break(uint code, uint pc, uint inst) {
			Call(nameof(Break), (int) code, pc, inst);
			Branch(pc + 4);
		}

		Value GenReadCopreg(uint cop, uint reg) => Call(nameof(ReadCopreg), cop, reg);
		Value GenReadCopcreg(uint cop, uint reg) => Call(nameof(ReadCopcreg), cop, reg);

		void WriteCopreg(uint cop, uint reg, Value value) => Call(nameof(WriteCopreg), cop, reg, value);
		void WriteCopcreg(uint cop, uint reg, Value value) => Call(nameof(WriteCopcreg), cop, reg, value);
		void GenCopfun(uint cop, uint cofun, uint inst) => Call(nameof(Copfun), cop, cofun, inst);

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
		public static Value Shl(Value a, Value b) => new Value(() => a.EmitThen(() => b.EmitThen(() => Ilg.ShiftLeft())));
		public static Value SShr(Value a, Value b) => new Value(() => a.EmitThen(() => b.EmitThen(() => Ilg.ShiftRight())));
		public static Value UShr(Value a, Value b) => new Value(() => a.EmitThen(() => b.EmitThen(() => Ilg.UnsignedShiftRight())));

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