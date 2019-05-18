using System;
using System.Collections.Generic;
using PrettyPrinter;
using Sigil;
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

		public static Emit<Action<Recompiler>> Ilg;
		
		static readonly Value CpuRef = new Value(() => Ilg.LoadArgument(0));
		static readonly Value GprRef = new Value(() => CpuRef.EmitThen(() => Ilg.LoadField(typeof(Recompiler).GetField(nameof(Gpr)))));
		
		static RGprs Gprs;

		Value this[string fieldName] {
			get => new Value(() => CpuRef.EmitThen(() => Ilg.LoadField(typeof(Recompiler).GetField(fieldName))));
			set => CpuRef.EmitThen(() => value.EmitThen(() => Ilg.StoreField(typeof(Recompiler).GetField(fieldName))));
		}

		Value ReadAbsorbWhichRef { get => this[nameof(ReadAbsorbWhich)]; set => this[nameof(ReadAbsorbWhich)] = value; }
		Value LDWhichRef { get => this[nameof(LdWhich)]; set => this[nameof(LdWhich)] = value; }
		Value LDAbsorbRef { get => this[nameof(LdAbsorb)]; set => this[nameof(LdAbsorb)] = value; }
		Value LDValueRef { get => this[nameof(LdValue)]; set => this[nameof(LdValue)] = value; }
		Value ReadFudgeRef { get => this[nameof(ReadFudge)]; set => this[nameof(ReadFudge)] = value; }
		Value HiRef { get => this[nameof(Hi)]; set => this[nameof(Hi)] = value; }
		Value LoRef { get => this[nameof(Lo)]; set => this[nameof(Lo)] = value; }

		public override void Run(uint pc) {
			while(true) {
				pc = RecompileBlock(pc);
			}
		}

		readonly Dictionary<uint, Action<Recompiler>> BlockCache = new Dictionary<uint, Action<Recompiler>>();

		Action<Recompiler> LastBlock;
		uint LastBlockAddr = ~0U;

		uint RecompileBlock(uint pc) {
			$"Running block at {pc:X8}".Print();
			if(pc == LastBlockAddr) {
				LastBlock(this);
				return BranchTo;
			}
			if(BlockCache.ContainsKey(pc)) {
				LastBlock = BlockCache[pc];
				LastBlockAddr = pc;
				LastBlock(this);
				return BranchTo;
			}
			
			var opc = pc;
			Ilg = Emit<Action<Recompiler>>.NewDynamicMethod($"Block_{pc:X8}");
			var branched = false;
			var no_delay = false;
			var need_load = true;
			var did_delay = false;
			while(!did_delay) {
				var insn = Memory.Load32(pc);
				WriteLine($"{pc:X}:  {Disassemble(pc, insn)}");

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
			var func = Ilg.CreateDelegate();
			LastBlock = BlockCache[opc] = func;
			LastBlockAddr = opc;
			func(this);
			return BranchTo;
		}

		void Branch(Label label) => Ilg.Branch(label);
		
		void Branch(Value target) {
			CpuRef.Emit();
			target.Emit();
			Ilg.StoreField(typeof(Recompiler).GetField(nameof(BranchTo)));
		}
		void Branch(uint target) {
			CpuRef.Emit();
			Ilg.LoadConstant(target);
			Ilg.StoreField(typeof(Recompiler).GetField(nameof(BranchTo)));
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
			LDAbsorbRef.Emit();
			LDWhichRef.Emit();
			Ilg.StoreElement<uint>();

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
			LDWhichRef = MakeValue(reg);
			LDValueRef = value;
		}

		Value RRA(Value idx) => new Value(() => {
			CpuRef.Emit();
			Ilg.LoadField(typeof(Recompiler).GetField(nameof(ReadAbsorb)));
			idx.Emit();
			Ilg.LoadElement<uint>();
		});
		void WRA(Value idx, Value val) {
			CpuRef.Emit();
			Ilg.LoadField(typeof(Recompiler).GetField(nameof(ReadAbsorb)));
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
		
		void Syscall(uint code, uint pc, uint inst) {}
		void Break(uint code, uint pc, uint inst) {}

		Value GenReadCopreg(uint cop, uint reg) => null;
		Value GenReadCopcreg(uint cop, uint reg) => null;
		void WriteCopreg(uint cop, uint reg, Value value) {}
		void WriteCopcreg(uint cop, uint reg, Value value) {}
		void GenCopfun(uint cop, uint cofun, uint inst) {}

		Value Add(Value a, Value b) => new Value(() => a.EmitThen(() => b.EmitThen(() => Ilg.Add())));
		Value Sub(Value a, Value b) => new Value(() => a.EmitThen(() => b.EmitThen(() => Ilg.Subtract())));
		Value Mul(Value a, Value b) => new Value(() => a.EmitThen(() => b.EmitThen(() => Ilg.Multiply())));
		Value Mul64(Value a, Value b) => null;
		Value UMul64(Value a, Value b) => null;
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

		Value ToI32(Value v) => null;
		Value ToI64(Value v) => null;
		Value ToU32(Value v) => null;
		Value ToU64(Value v) => null;
		
		Value Signed(Value v) => null;
		Value Unsigned(Value v) => null;

		Value SignExt(int size, Value v) => null;
	}
}