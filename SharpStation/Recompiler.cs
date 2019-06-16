using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
	public class BlockPage {
		public readonly uint Base;
		public readonly SortedList<uint, Block> Blocks = new SortedList<uint, Block>();
		public readonly byte[] Occupancy = new byte[32];

		public BlockPage(uint @base) => Base = @base;

		public void Add(Block block) {
			Blocks.Add(block.Addr, block);
			Occupy(block);
		}

		public void Remove(Block block) {
			Blocks.Remove(block.Addr);
			var end = block.End & 0x0FFFFFFF;
			// TODO: This can be a lot more performant; most of the time, blocks are going to cover whole occupancy zones
			for(var i = block.Addr & 0x0FFFFFFF; i < end; i += 4)
				Occupancy[(i & 0x3FF) >> 5] ^= (byte) (1 << ((int) (i >> 2) & 7));
			foreach(var oblock in Blocks.Values)
				Occupy(oblock);
		}

		void Occupy(Block block) {
			var end = block.End & 0x0FFFFFFF;
			// TODO: This can be a lot more performant; most of the time, blocks are going to cover whole occupancy zones
			for(var i = block.Addr & 0x0FFFFFFF; i < end; i += 4)
				Occupancy[(i & 0x3FF) >> 5] |= (byte) (1 << ((int) (i >> 2) & 7));
		}

		public bool Occupied(uint addr) => (Occupancy[(addr & 0x3FF) >> 5] & (1 << ((int) (addr >> 2) & 7))) != 0;
	}
	
	public class Block {
		public readonly uint Addr;
		public uint End;
		Action<Recompiler>? _Func;

		public Action<Recompiler>? Func {
			get => _Func;
			set {
				if(_Func == value) return;
				var spage = Addr & 0x0FFFFC00;
				var npages = (((End & 0x0FFFFFFF) - spage) >> 10) + 1;
				var rc = (Recompiler) Cpu;
				
				if(_Func != null)
					for(var i = 0U; i < npages; ++i)
						rc.GetBlockPage(Addr + (i << 10)).Remove(this);
				if(value != null)
					for(var i = 0U; i < npages; ++i)
						rc.GetBlockPage(Addr + (i << 10)).Add(this);
				_Func = value;
			}
		}

		public Block(uint addr) => Addr = End = addr;
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

			public Value DivUn(Value b) => new Value(() => EmitThen(() => b.EmitThen(() => Ilg.UnsignedDivide())));
			public Value ModUn(Value b) => new Value(() => EmitThen(() => b.EmitThen(() => Ilg.UnsignedRemainder())));

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
			public Value LtUn(Value b) => Comp(this, b, l => Ilg.UnsignedBranchIfLess(l));

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
				get => reg == 0
					? new SettableValue(() => {
						Ilg.LoadConstant(0);
						Ilg.Convert<uint>();
					}, _ => {})
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

		protected override void Run() {
			if(BranchToBlock != null && BranchToBlock.Addr == Pc) {
				var func = LastBlock = BranchToBlock.Func;
				Pc = LastBlockAddr = BranchToBlock.Addr;
				BranchToBlock = null;
				Pc = Intercept(Pc);
				if(func == null)
					Pc = RecompileBlock(Pc, BranchToBlock);
				else {
					func(this);
					Pc = BranchTo;
				}
			} else {
				Pc = Intercept(Pc);
				Pc = RecompileBlock(Pc);
			}
		}

		readonly BlockPage[] BlockPages = Enumerable.Range(0, 0x40000).Select(i => new BlockPage((uint) i << 10)).ToArray();
		readonly Dictionary<uint, Block> BlockCache = new Dictionary<uint, Block>();

		Action<Recompiler>? LastBlock;
		uint LastBlockAddr = ~0U;

		Dictionary<string, (FieldBuilder, Block)> CurBlockRefs = new Dictionary<string, (FieldBuilder, Block)>();
		Dictionary<uint, Label> BlockInstLabels = new Dictionary<uint, Label>();
		public Block? BranchToBlock;
		Action? BranchToLabel;
		Local? BranchToLabelLocal;

		uint RecompileBlock(uint pc, Block? block = null) {
			if(pc == LastBlockAddr && LastBlock != null) {
				BranchToBlock = null;
				LastBlock(this);
				return BranchTo;
			}

			block ??= GetBlock(pc);
			if(block.Func != null) {
				LastBlock = block.Func;
				LastBlockAddr = pc;
				BranchToBlock = null;
				LastBlock(this);
				return BranchTo;
			}

			if(DebugMemory)
				$"Recompiling block at {pc:X8}".Debug();
			CurBlockRefs = new Dictionary<string, (FieldBuilder, Block)>();
			BlockInstLabels = new Dictionary<uint, Label>();

			var ab = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString()), AssemblyBuilderAccess.Run);
			var mb = ab.DefineDynamicModule("Block");
			Tb = mb.DefineType("Block");
			var mname = $"Block_{pc:X8}";
			Ilg = Emit<Action<Recompiler>>.BuildMethod(Tb, mname, MethodAttributes.Static | MethodAttributes.Public, CallingConventions.Standard);

			BranchToLabel = null;
			BranchToLabelLocal = Ilg.DeclareLocal<bool>();
			
			var opc = pc;
			var branched = false;
			var no_delay = false;
			var need_load = true;
			var did_delay = false;
			while(!did_delay) {
				var insn = ICache[(pc & 0xFFC) >> 2].Data;
				var timestep = 0U;
				// TODO: Move all of the cache->timing logic into runtime to maximize accuracy
				if(ICache[(pc & 0xFFC) >> 2].TV != pc) {
					if(pc >= 0xA0000000 || !BIU.HasBit(11)) {
						insn = Memory.Load32(pc);
						timestep += 4;
					} else {
						var ICI = ((Span<ICacheEntry>) ICache).Slice((int) ((pc & 0xFF0) >> 2), 4);
						ICI[0].TV = (pc & ~0xFU) | 0x00U | 0x02U;
						ICI[1].TV = (pc & ~0xFU) | 0x04U | 0x02U;
						ICI[2].TV = (pc & ~0xFU) | 0x08U | 0x02U;
						ICI[3].TV = (pc & ~0xFU) | 0x0CU | 0x02U;

						timestep += 3;

						switch(pc & 0xC) {
							case 0x0:
								timestep++;
								ICI[0].TV &= ~0x2U;
								ICI[0].Data = Memory.Load32(pc);
								goto case 0x4;
							case 0x4:
								timestep++;
								ICI[1].TV &= ~0x2U;
								ICI[1].Data = Memory.Load32((pc & ~0xFU) | 0x4U);
								goto case 0x8;
							case 0x8:
								timestep++;
								ICI[2].TV &= ~0x2U;
								ICI[2].Data = Memory.Load32((pc & ~0xFU) | 0x8U);
								goto case 0xC;
							case 0xC:
								timestep++;
								ICI[3].TV &= ~0x2U;
								ICI[3].Data = Memory.Load32((pc & ~0xFU) | 0xCU);
								break;
						}

						insn = ICache[(pc & 0xFFCU) >> 2].Data;
					}
				}

				var rinsn = Memory.Load32(pc);
				if(insn != rinsn) {
					$"Instruction mismatch! Cache fuckup?".Debug();
					insn = rinsn;
					for(var i = 0; i < ICache.Length; ++i)
						ICache[i].TV = ICache[i].Data = 0;
				}
				
				if(timestep != 0)
					TimestampInc(timestep);

				if(DebugMemory)
					WriteLine($"{pc:X}:  {Disassemble(pc, insn)}");

				if(branched)
					did_delay = true;
				
				var has_load = false;
				Ilg.MarkLabel(BlockInstLabels[pc] = Ilg.DefineLabel());
				
				this[nameof(Pc)] = MakeValue(pc);
				
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
			BranchToBlock = null;
			func(this);
			return BranchTo;
		}

		public void DebugMtMfLoHi(int i, int b, int r) {
			var name = i switch { 0 => "MFHI", 1 => "MTHI", 2 => "MFLO", 3 => "MTLO", _ => throw new NotSupportedException() };
			$"{(b == 0 ? "Before" : "After ")} {name} ${r}".Debug();
			RegisterDebug();
		}

		public override void Invalidate(uint addr) {
			addr &= 0x0FFFFFFC;
			var page = GetBlockPage(addr);
			if(!page.Occupied(addr)) return;
			var toClear = page.Blocks.Values.Where(block => block.Func != null && (block.Addr & 0x0FFFFFFF) <= addr && (block.End & 0x0FFFFFFF) > addr).ToList();
			foreach(var block in toClear)
				block.Func = null;
		}

		Block GetBlock(uint addr) {
			if(BlockCache.TryGetValue(addr, out var block)) return block;
			block = BlockCache[addr] = new Block(addr);
			return block;
		}

		public BlockPage GetBlockPage(uint addr) => BlockPages[(addr & 0x0FFFFFFFU) >> 10];

		void BranchLink(Value target, uint pc) => Call(nameof(BranchLinkTo), target, pc);
		void BranchLink(uint target, uint pc) => Call(nameof(BranchLinkTo), target, pc);
		public void BranchLinkTo(uint target, uint pc) {
			//$"Calling {target:X8} from {pc:X8}".Debug();
		}

		void Branch(Label label) => Ilg.Branch(label);
		
		void Branch(Value target) {
			CpuRef.Emit();
			target.Emit();
			Ilg.StoreField(typeof(Recompiler).GetField(nameof(BranchTo)));
		}
		void Branch(uint target) {
			// TODO: Add counter to prevent endless loops without checking interrupts
			/*if(BlockStart <= target && target <= CurPc) {
				Ilg.LoadConstant(true);
				Ilg.StoreLocal(BranchToLabelLocal);
				BranchToLabel = () => {
					Ilg.LoadLocal(BranchToLabelLocal);
					Ilg.BranchIfTrue(BlockInstLabels[target]);
				};
				return;
			}*/
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
			CpuRef.Emit();
			Ilg.LoadConstant(target);
			Ilg.StoreField(typeof(Recompiler).GetField(nameof(BranchTo)));
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