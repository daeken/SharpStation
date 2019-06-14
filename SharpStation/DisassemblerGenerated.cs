/* Autogenerated from insts.td. DO NOT EDIT */
// ReSharper disable RedundantUsingDirective
// ReSharper disable SwitchStatementMissingSomeCases
// ReSharper disable SuggestVarOrType_BuiltInTypes
// ReSharper disable ArrangeRedundantParentheses
// ReSharper disable RedundantCast
// ReSharper disable ConditionIsAlwaysTrueOrFalse
// ReSharper disable HeuristicUnreachableCode
// ReSharper disable ConvertIfStatementToConditionalTernaryExpression
#pragma warning disable 162
using System;

namespace SharpStation {
	public partial class BaseCpu {
		internal string Disassemble(uint pc, uint inst) {
			switch((inst) >> ((int) 0x1a)) {
				case 0x0: {
					switch((inst) & (0x3f)) {
						case 0x0: {
							var rt = ((inst) >> ((int) 0x10)) & (0x1f);
							var rd = ((inst) >> ((int) 0xb)) & (0x1f);
							var shamt = ((inst) >> ((int) 0x6)) & (0x1f);
							return($"sll %{rd}, %{rt}, 0x{shamt:X}");
							break;
						}
						case 0x2: {
							var rt = ((inst) >> ((int) 0x10)) & (0x1f);
							var rd = ((inst) >> ((int) 0xb)) & (0x1f);
							var shamt = ((inst) >> ((int) 0x6)) & (0x1f);
							return($"srl %{rd}, %{rt}, 0x{shamt:X}");
							break;
						}
						case 0x3: {
							var rt = ((inst) >> ((int) 0x10)) & (0x1f);
							var rd = ((inst) >> ((int) 0xb)) & (0x1f);
							var shamt = ((inst) >> ((int) 0x6)) & (0x1f);
							return($"sra %{rd}, %{rt}, 0x{shamt:X}");
							break;
						}
						case 0x4: {
							var rs = ((inst) >> ((int) 0x15)) & (0x1f);
							var rt = ((inst) >> ((int) 0x10)) & (0x1f);
							var rd = ((inst) >> ((int) 0xb)) & (0x1f);
							return($"sllv %{rd}, %{rt}, %{rs}");
							break;
						}
						case 0x6: {
							var rs = ((inst) >> ((int) 0x15)) & (0x1f);
							var rt = ((inst) >> ((int) 0x10)) & (0x1f);
							var rd = ((inst) >> ((int) 0xb)) & (0x1f);
							return($"srlv %{rd}, %{rt}, 0x{rs:X}");
							break;
						}
						case 0x7: {
							var rs = ((inst) >> ((int) 0x15)) & (0x1f);
							var rt = ((inst) >> ((int) 0x10)) & (0x1f);
							var rd = ((inst) >> ((int) 0xb)) & (0x1f);
							return($"srav %{rd}, %{rt}, 0x{rs:X}");
							break;
						}
						case 0x8: {
							var rs = ((inst) >> ((int) 0x15)) & (0x1f);
							return($"jr %{rs}");
							break;
						}
						case 0x9: {
							var rs = ((inst) >> ((int) 0x15)) & (0x1f);
							var rd = ((inst) >> ((int) 0xb)) & (0x1f);
							return($"jalr %{rd}, %{rs}");
							break;
						}
						case 0xc: {
							var code = ((inst) >> ((int) 0x6)) & (0xfffff);
							return($"syscall 0x{code:X}");
							break;
						}
						case 0xd: {
							var code = ((inst) >> ((int) 0x6)) & (0xfffff);
							return($"break 0x{code:X}");
							break;
						}
						case 0x10: {
							var rd = ((inst) >> ((int) 0xb)) & (0x1f);
							return($"mfhi %{rd}");
							break;
						}
						case 0x11: {
							var rs = ((inst) >> ((int) 0x15)) & (0x1f);
							return($"mthi %{rs}");
							break;
						}
						case 0x12: {
							var rd = ((inst) >> ((int) 0xb)) & (0x1f);
							return($"mflo %{rd}");
							break;
						}
						case 0x13: {
							var rs = ((inst) >> ((int) 0x15)) & (0x1f);
							return($"mtlo %{rs}");
							break;
						}
						case 0x18: {
							var rs = ((inst) >> ((int) 0x15)) & (0x1f);
							var rt = ((inst) >> ((int) 0x10)) & (0x1f);
							ulong _t = (ulong) (((long) ((int) ((Gpr)[rs]))) * ((long) ((int) ((Gpr)[rt]))));
							return($"mult %{rs}, %{rt}");
							break;
						}
						case 0x19: {
							var rs = ((inst) >> ((int) 0x15)) & (0x1f);
							var rt = ((inst) >> ((int) 0x10)) & (0x1f);
							ulong _t = ((ulong) ((Gpr)[rs])) * ((ulong) ((Gpr)[rt]));
							return($"multu %{rs}, %{rt}");
							break;
						}
						case 0x1a: {
							var rs = ((inst) >> ((int) 0x15)) & (0x1f);
							var rt = ((inst) >> ((int) 0x10)) & (0x1f);
							
							return($"div %{rs}, %{rt}");
							break;
						}
						case 0x1b: {
							var rs = ((inst) >> ((int) 0x15)) & (0x1f);
							var rt = ((inst) >> ((int) 0x10)) & (0x1f);
							
							return($"divu %{rs}, %{rt}");
							break;
						}
						case 0x20: {
							var rs = ((inst) >> ((int) 0x15)) & (0x1f);
							var rt = ((inst) >> ((int) 0x10)) & (0x1f);
							var rd = ((inst) >> ((int) 0xb)) & (0x1f);
							return($"add %{rd}, %{rs}, %{rt}");
							break;
						}
						case 0x21: {
							var rs = ((inst) >> ((int) 0x15)) & (0x1f);
							var rt = ((inst) >> ((int) 0x10)) & (0x1f);
							var rd = ((inst) >> ((int) 0xb)) & (0x1f);
							return($"addu %{rd}, %{rs}, %{rt}");
							break;
						}
						case 0x22: {
							var rs = ((inst) >> ((int) 0x15)) & (0x1f);
							var rt = ((inst) >> ((int) 0x10)) & (0x1f);
							var rd = ((inst) >> ((int) 0xb)) & (0x1f);
							return($"sub %{rd}, %{rs}, %{rt}");
							break;
						}
						case 0x23: {
							var rs = ((inst) >> ((int) 0x15)) & (0x1f);
							var rt = ((inst) >> ((int) 0x10)) & (0x1f);
							var rd = ((inst) >> ((int) 0xb)) & (0x1f);
							return($"subu %{rd}, %{rs}, %{rt}");
							break;
						}
						case 0x24: {
							var rs = ((inst) >> ((int) 0x15)) & (0x1f);
							var rt = ((inst) >> ((int) 0x10)) & (0x1f);
							var rd = ((inst) >> ((int) 0xb)) & (0x1f);
							return($"and %{rd}, %{rs}, %{rt}");
							break;
						}
						case 0x25: {
							var rs = ((inst) >> ((int) 0x15)) & (0x1f);
							var rt = ((inst) >> ((int) 0x10)) & (0x1f);
							var rd = ((inst) >> ((int) 0xb)) & (0x1f);
							return($"or %{rd}, %{rs}, %{rt}");
							break;
						}
						case 0x26: {
							var rs = ((inst) >> ((int) 0x15)) & (0x1f);
							var rt = ((inst) >> ((int) 0x10)) & (0x1f);
							var rd = ((inst) >> ((int) 0xb)) & (0x1f);
							return($"xor %{rd}, %{rs}, %{rt}");
							break;
						}
						case 0x27: {
							var rs = ((inst) >> ((int) 0x15)) & (0x1f);
							var rt = ((inst) >> ((int) 0x10)) & (0x1f);
							var rd = ((inst) >> ((int) 0xb)) & (0x1f);
							return($"nor %{rd}, %{rs}, %{rt}");
							break;
						}
						case 0x2a: {
							var rs = ((inst) >> ((int) 0x15)) & (0x1f);
							var rt = ((inst) >> ((int) 0x10)) & (0x1f);
							var rd = ((inst) >> ((int) 0xb)) & (0x1f);
							return($"slt %{rd}, %{rs}, %{rt}");
							break;
						}
						case 0x2b: {
							var rs = ((inst) >> ((int) 0x15)) & (0x1f);
							var rt = ((inst) >> ((int) 0x10)) & (0x1f);
							var rd = ((inst) >> ((int) 0xb)) & (0x1f);
							return($"sltu %{rd}, %{rs}, %{rt}");
							break;
						}
					}
					break;
				}
				case 0x1: {
					switch(((inst) >> ((int) 0x10)) & (0x1f)) {
						case 0x0: {
							var rs = ((inst) >> ((int) 0x15)) & (0x1f);
							var imm = (inst) & (0xffff);
							var target = (uint) (((pc) + (0x4)) + ((SignExt(0x10, imm)) << ((int) 0x2)));
							
							return($"bltz %{rs}, 0x{target:X}");
							break;
						}
						case 0x1: {
							var rs = ((inst) >> ((int) 0x15)) & (0x1f);
							var imm = (inst) & (0xffff);
							var target = (uint) (((pc) + (0x4)) + ((SignExt(0x10, imm)) << ((int) 0x2)));
							
							return($"bgez %{rs}, 0x{target:X}");
							break;
						}
						case 0x2: {
							var rs = ((inst) >> ((int) 0x15)) & (0x1f);
							var imm = (inst) & (0xffff);
							var target = (uint) (((pc) + (0x4)) + ((SignExt(0x10, imm)) << ((int) 0x2)));
							
							return($"bltz %{rs}, 0x{target:X}");
							break;
						}
						case 0x3: {
							var rs = ((inst) >> ((int) 0x15)) & (0x1f);
							var imm = (inst) & (0xffff);
							var target = (uint) (((pc) + (0x4)) + ((SignExt(0x10, imm)) << ((int) 0x2)));
							
							return($"bgez %{rs}, 0x{target:X}");
							break;
						}
						case 0x4: {
							var rs = ((inst) >> ((int) 0x15)) & (0x1f);
							var imm = (inst) & (0xffff);
							var target = (uint) (((pc) + (0x4)) + ((SignExt(0x10, imm)) << ((int) 0x2)));
							
							return($"bltz %{rs}, 0x{target:X}");
							break;
						}
						case 0x5: {
							var rs = ((inst) >> ((int) 0x15)) & (0x1f);
							var imm = (inst) & (0xffff);
							var target = (uint) (((pc) + (0x4)) + ((SignExt(0x10, imm)) << ((int) 0x2)));
							
							return($"bgez %{rs}, 0x{target:X}");
							break;
						}
						case 0x6: {
							var rs = ((inst) >> ((int) 0x15)) & (0x1f);
							var imm = (inst) & (0xffff);
							var target = (uint) (((pc) + (0x4)) + ((SignExt(0x10, imm)) << ((int) 0x2)));
							
							return($"bltz %{rs}, 0x{target:X}");
							break;
						}
						case 0x7: {
							var rs = ((inst) >> ((int) 0x15)) & (0x1f);
							var imm = (inst) & (0xffff);
							var target = (uint) (((pc) + (0x4)) + ((SignExt(0x10, imm)) << ((int) 0x2)));
							
							return($"bgez %{rs}, 0x{target:X}");
							break;
						}
						case 0x8: {
							var rs = ((inst) >> ((int) 0x15)) & (0x1f);
							var imm = (inst) & (0xffff);
							var target = (uint) (((pc) + (0x4)) + ((SignExt(0x10, imm)) << ((int) 0x2)));
							
							return($"bltz %{rs}, 0x{target:X}");
							break;
						}
						case 0x9: {
							var rs = ((inst) >> ((int) 0x15)) & (0x1f);
							var imm = (inst) & (0xffff);
							var target = (uint) (((pc) + (0x4)) + ((SignExt(0x10, imm)) << ((int) 0x2)));
							
							return($"bgez %{rs}, 0x{target:X}");
							break;
						}
						case 0xa: {
							var rs = ((inst) >> ((int) 0x15)) & (0x1f);
							var imm = (inst) & (0xffff);
							var target = (uint) (((pc) + (0x4)) + ((SignExt(0x10, imm)) << ((int) 0x2)));
							
							return($"bltz %{rs}, 0x{target:X}");
							break;
						}
						case 0xb: {
							var rs = ((inst) >> ((int) 0x15)) & (0x1f);
							var imm = (inst) & (0xffff);
							var target = (uint) (((pc) + (0x4)) + ((SignExt(0x10, imm)) << ((int) 0x2)));
							
							return($"bgez %{rs}, 0x{target:X}");
							break;
						}
						case 0xc: {
							var rs = ((inst) >> ((int) 0x15)) & (0x1f);
							var imm = (inst) & (0xffff);
							var target = (uint) (((pc) + (0x4)) + ((SignExt(0x10, imm)) << ((int) 0x2)));
							
							return($"bltz %{rs}, 0x{target:X}");
							break;
						}
						case 0xd: {
							var rs = ((inst) >> ((int) 0x15)) & (0x1f);
							var imm = (inst) & (0xffff);
							var target = (uint) (((pc) + (0x4)) + ((SignExt(0x10, imm)) << ((int) 0x2)));
							
							return($"bgez %{rs}, 0x{target:X}");
							break;
						}
						case 0xe: {
							var rs = ((inst) >> ((int) 0x15)) & (0x1f);
							var imm = (inst) & (0xffff);
							var target = (uint) (((pc) + (0x4)) + ((SignExt(0x10, imm)) << ((int) 0x2)));
							
							return($"bltz %{rs}, 0x{target:X}");
							break;
						}
						case 0xf: {
							var rs = ((inst) >> ((int) 0x15)) & (0x1f);
							var imm = (inst) & (0xffff);
							var target = (uint) (((pc) + (0x4)) + ((SignExt(0x10, imm)) << ((int) 0x2)));
							
							return($"bgez %{rs}, 0x{target:X}");
							break;
						}
						case 0x10: {
							var rs = ((inst) >> ((int) 0x15)) & (0x1f);
							var imm = (inst) & (0xffff);
							var target = (uint) (((pc) + (0x4)) + ((SignExt(0x10, imm)) << ((int) 0x2)));
							
							return($"bltzal %{rs}, 0x{target:X}");
							break;
						}
						case 0x11: {
							var rs = ((inst) >> ((int) 0x15)) & (0x1f);
							var imm = (inst) & (0xffff);
							var target = (uint) (((pc) + (0x4)) + ((SignExt(0x10, imm)) << ((int) 0x2)));
							
							return($"bgezal %{rs}, 0x{target:X}");
							break;
						}
						case 0x12: {
							var rs = ((inst) >> ((int) 0x15)) & (0x1f);
							var imm = (inst) & (0xffff);
							var target = (uint) (((pc) + (0x4)) + ((SignExt(0x10, imm)) << ((int) 0x2)));
							
							return($"bltzal %{rs}, 0x{target:X}");
							break;
						}
						case 0x13: {
							var rs = ((inst) >> ((int) 0x15)) & (0x1f);
							var imm = (inst) & (0xffff);
							var target = (uint) (((pc) + (0x4)) + ((SignExt(0x10, imm)) << ((int) 0x2)));
							
							return($"bgezal %{rs}, 0x{target:X}");
							break;
						}
						case 0x14: {
							var rs = ((inst) >> ((int) 0x15)) & (0x1f);
							var imm = (inst) & (0xffff);
							var target = (uint) (((pc) + (0x4)) + ((SignExt(0x10, imm)) << ((int) 0x2)));
							
							return($"bltzal %{rs}, 0x{target:X}");
							break;
						}
						case 0x15: {
							var rs = ((inst) >> ((int) 0x15)) & (0x1f);
							var imm = (inst) & (0xffff);
							var target = (uint) (((pc) + (0x4)) + ((SignExt(0x10, imm)) << ((int) 0x2)));
							
							return($"bgezal %{rs}, 0x{target:X}");
							break;
						}
						case 0x16: {
							var rs = ((inst) >> ((int) 0x15)) & (0x1f);
							var imm = (inst) & (0xffff);
							var target = (uint) (((pc) + (0x4)) + ((SignExt(0x10, imm)) << ((int) 0x2)));
							
							return($"bltzal %{rs}, 0x{target:X}");
							break;
						}
						case 0x17: {
							var rs = ((inst) >> ((int) 0x15)) & (0x1f);
							var imm = (inst) & (0xffff);
							var target = (uint) (((pc) + (0x4)) + ((SignExt(0x10, imm)) << ((int) 0x2)));
							
							return($"bgezal %{rs}, 0x{target:X}");
							break;
						}
						case 0x18: {
							var rs = ((inst) >> ((int) 0x15)) & (0x1f);
							var imm = (inst) & (0xffff);
							var target = (uint) (((pc) + (0x4)) + ((SignExt(0x10, imm)) << ((int) 0x2)));
							
							return($"bltzal %{rs}, 0x{target:X}");
							break;
						}
						case 0x19: {
							var rs = ((inst) >> ((int) 0x15)) & (0x1f);
							var imm = (inst) & (0xffff);
							var target = (uint) (((pc) + (0x4)) + ((SignExt(0x10, imm)) << ((int) 0x2)));
							
							return($"bgezal %{rs}, 0x{target:X}");
							break;
						}
						case 0x1a: {
							var rs = ((inst) >> ((int) 0x15)) & (0x1f);
							var imm = (inst) & (0xffff);
							var target = (uint) (((pc) + (0x4)) + ((SignExt(0x10, imm)) << ((int) 0x2)));
							
							return($"bltzal %{rs}, 0x{target:X}");
							break;
						}
						case 0x1b: {
							var rs = ((inst) >> ((int) 0x15)) & (0x1f);
							var imm = (inst) & (0xffff);
							var target = (uint) (((pc) + (0x4)) + ((SignExt(0x10, imm)) << ((int) 0x2)));
							
							return($"bgezal %{rs}, 0x{target:X}");
							break;
						}
						case 0x1c: {
							var rs = ((inst) >> ((int) 0x15)) & (0x1f);
							var imm = (inst) & (0xffff);
							var target = (uint) (((pc) + (0x4)) + ((SignExt(0x10, imm)) << ((int) 0x2)));
							
							return($"bltzal %{rs}, 0x{target:X}");
							break;
						}
						case 0x1d: {
							var rs = ((inst) >> ((int) 0x15)) & (0x1f);
							var imm = (inst) & (0xffff);
							var target = (uint) (((pc) + (0x4)) + ((SignExt(0x10, imm)) << ((int) 0x2)));
							
							return($"bgezal %{rs}, 0x{target:X}");
							break;
						}
						case 0x1e: {
							var rs = ((inst) >> ((int) 0x15)) & (0x1f);
							var imm = (inst) & (0xffff);
							var target = (uint) (((pc) + (0x4)) + ((SignExt(0x10, imm)) << ((int) 0x2)));
							
							return($"bltzal %{rs}, 0x{target:X}");
							break;
						}
						case 0x1f: {
							var rs = ((inst) >> ((int) 0x15)) & (0x1f);
							var imm = (inst) & (0xffff);
							var target = (uint) (((pc) + (0x4)) + ((SignExt(0x10, imm)) << ((int) 0x2)));
							
							return($"bgezal %{rs}, 0x{target:X}");
							break;
						}
					}
					break;
				}
				case 0x2: {
					var imm = (inst) & (0x3ffffff);
					var target = (((pc) + (0x4)) & (0xf0000000)) + ((imm) << ((int) 0x2));
					return($"j 0x{target:X}");
					break;
				}
				case 0x3: {
					var imm = (inst) & (0x3ffffff);
					var target = (((pc) + (0x4)) & (0xf0000000)) + ((imm) << ((int) 0x2));
					return($"jal 0x{target:X}");
					break;
				}
				case 0x4: {
					var rs = ((inst) >> ((int) 0x15)) & (0x1f);
					var rt = ((inst) >> ((int) 0x10)) & (0x1f);
					var imm = (inst) & (0xffff);
					var target = (uint) (((pc) + (0x4)) + ((SignExt(0x10, imm)) << ((int) 0x2)));
					
					return($"beq %{rs}, %{rt}, 0x{target:X}");
					break;
				}
				case 0x5: {
					var rs = ((inst) >> ((int) 0x15)) & (0x1f);
					var rt = ((inst) >> ((int) 0x10)) & (0x1f);
					var imm = (inst) & (0xffff);
					var target = (uint) (((pc) + (0x4)) + ((SignExt(0x10, imm)) << ((int) 0x2)));
					
					return($"bne %{rs}, %{rt}, 0x{target:X}");
					break;
				}
				case 0x6: {
					switch(((inst) >> ((int) 0x10)) & (0x1f)) {
						case 0x0: {
							var rs = ((inst) >> ((int) 0x15)) & (0x1f);
							var imm = (inst) & (0xffff);
							var target = (uint) (((pc) + (0x4)) + ((SignExt(0x10, imm)) << ((int) 0x2)));
							
							return($"blez %{rs}, 0x{target:X}");
							break;
						}
					}
					break;
				}
				case 0x7: {
					switch(((inst) >> ((int) 0x10)) & (0x1f)) {
						case 0x0: {
							var rs = ((inst) >> ((int) 0x15)) & (0x1f);
							var imm = (inst) & (0xffff);
							var target = (uint) (((pc) + (0x4)) + ((SignExt(0x10, imm)) << ((int) 0x2)));
							
							return($"bgtz %{rs}, 0x{target:X}");
							break;
						}
					}
					break;
				}
				case 0x8: {
					var rs = ((inst) >> ((int) 0x15)) & (0x1f);
					var rt = ((inst) >> ((int) 0x10)) & (0x1f);
					var imm = (inst) & (0xffff);
					var eimm = (uint) (SignExt(0x10, imm));
					
					return($"addi %{rt}, %{rs}, 0x{eimm:X}");
					break;
				}
				case 0x9: {
					var rs = ((inst) >> ((int) 0x15)) & (0x1f);
					var rt = ((inst) >> ((int) 0x10)) & (0x1f);
					var imm = (inst) & (0xffff);
					var eimm = (uint) (SignExt(0x10, imm));
					return($"addiu %{rt}, %{rs}, 0x{eimm:X}");
					break;
				}
				case 0xa: {
					var rs = ((inst) >> ((int) 0x15)) & (0x1f);
					var rt = ((inst) >> ((int) 0x10)) & (0x1f);
					var imm = (inst) & (0xffff);
					var eimm = (uint) (SignExt(0x10, imm));
					return($"slti %{rt}, %{rs}, 0x{eimm:X}");
					break;
				}
				case 0xb: {
					var rs = ((inst) >> ((int) 0x15)) & (0x1f);
					var rt = ((inst) >> ((int) 0x10)) & (0x1f);
					var imm = (inst) & (0xffff);
					var eimm = (uint) (SignExt(0x10, imm));
					return($"sltiu %{rt}, %{rs}, 0x{eimm:X}");
					break;
				}
				case 0xc: {
					var rs = ((inst) >> ((int) 0x15)) & (0x1f);
					var rt = ((inst) >> ((int) 0x10)) & (0x1f);
					var imm = (inst) & (0xffff);
					var eimm = imm;
					return($"andi %{rt}, %{rs}, 0x{eimm:X}");
					break;
				}
				case 0xd: {
					var rs = ((inst) >> ((int) 0x15)) & (0x1f);
					var rt = ((inst) >> ((int) 0x10)) & (0x1f);
					var imm = (inst) & (0xffff);
					var eimm = imm;
					return($"ori %{rt}, %{rs}, 0x{eimm:X}");
					break;
				}
				case 0xe: {
					var rs = ((inst) >> ((int) 0x15)) & (0x1f);
					var rt = ((inst) >> ((int) 0x10)) & (0x1f);
					var imm = (inst) & (0xffff);
					var eimm = imm;
					return($"xori %{rt}, %{rs}, 0x{eimm:X}");
					break;
				}
				case 0xf: {
					var rt = ((inst) >> ((int) 0x10)) & (0x1f);
					var imm = (inst) & (0xffff);
					return($"lui %{rt}, 0x{imm:X}");
					break;
				}
				case 0x10: {
					switch(((inst) >> ((int) 0x15)) & (0x1f)) {
						case 0x0: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var rt = ((inst) >> ((int) 0x10)) & (0x1f);
							var rd = ((inst) >> ((int) 0xb)) & (0x1f);
							return($"mfc0x{cop:X} %{rt}, 0x{rd:X}");
							break;
						}
						case 0x2: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var rt = ((inst) >> ((int) 0x10)) & (0x1f);
							var rd = ((inst) >> ((int) 0xb)) & (0x1f);
							return($"cfc0x{cop:X} %{rt}, 0x{rd:X}");
							break;
						}
						case 0x4: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var rt = ((inst) >> ((int) 0x10)) & (0x1f);
							var rd = ((inst) >> ((int) 0xb)) & (0x1f);
							return($"mtc0x{cop:X} %{rt}, 0x{rd:X}");
							break;
						}
						case 0x6: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var rt = ((inst) >> ((int) 0x10)) & (0x1f);
							var rd = ((inst) >> ((int) 0xb)) & (0x1f);
							return($"ctc0x{cop:X} %{rt}, 0x{rd:X}");
							break;
						}
						case 0x10: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x11: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x12: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x13: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x14: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x15: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x16: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x17: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x18: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x19: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x1a: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x1b: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x1c: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x1d: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x1e: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x1f: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
					}
					break;
				}
				case 0x11: {
					switch(((inst) >> ((int) 0x15)) & (0x1f)) {
						case 0x0: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var rt = ((inst) >> ((int) 0x10)) & (0x1f);
							var rd = ((inst) >> ((int) 0xb)) & (0x1f);
							return($"mfc0x{cop:X} %{rt}, 0x{rd:X}");
							break;
						}
						case 0x2: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var rt = ((inst) >> ((int) 0x10)) & (0x1f);
							var rd = ((inst) >> ((int) 0xb)) & (0x1f);
							return($"cfc0x{cop:X} %{rt}, 0x{rd:X}");
							break;
						}
						case 0x4: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var rt = ((inst) >> ((int) 0x10)) & (0x1f);
							var rd = ((inst) >> ((int) 0xb)) & (0x1f);
							return($"mtc0x{cop:X} %{rt}, 0x{rd:X}");
							break;
						}
						case 0x6: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var rt = ((inst) >> ((int) 0x10)) & (0x1f);
							var rd = ((inst) >> ((int) 0xb)) & (0x1f);
							return($"ctc0x{cop:X} %{rt}, 0x{rd:X}");
							break;
						}
						case 0x10: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x11: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x12: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x13: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x14: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x15: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x16: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x17: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x18: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x19: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x1a: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x1b: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x1c: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x1d: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x1e: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x1f: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
					}
					break;
				}
				case 0x12: {
					switch(((inst) >> ((int) 0x15)) & (0x1f)) {
						case 0x0: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var rt = ((inst) >> ((int) 0x10)) & (0x1f);
							var rd = ((inst) >> ((int) 0xb)) & (0x1f);
							return($"mfc0x{cop:X} %{rt}, 0x{rd:X}");
							break;
						}
						case 0x2: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var rt = ((inst) >> ((int) 0x10)) & (0x1f);
							var rd = ((inst) >> ((int) 0xb)) & (0x1f);
							return($"cfc0x{cop:X} %{rt}, 0x{rd:X}");
							break;
						}
						case 0x4: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var rt = ((inst) >> ((int) 0x10)) & (0x1f);
							var rd = ((inst) >> ((int) 0xb)) & (0x1f);
							return($"mtc0x{cop:X} %{rt}, 0x{rd:X}");
							break;
						}
						case 0x6: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var rt = ((inst) >> ((int) 0x10)) & (0x1f);
							var rd = ((inst) >> ((int) 0xb)) & (0x1f);
							return($"ctc0x{cop:X} %{rt}, 0x{rd:X}");
							break;
						}
						case 0x10: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x11: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x12: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x13: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x14: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x15: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x16: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x17: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x18: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x19: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x1a: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x1b: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x1c: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x1d: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x1e: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x1f: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
					}
					break;
				}
				case 0x13: {
					switch(((inst) >> ((int) 0x15)) & (0x1f)) {
						case 0x0: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var rt = ((inst) >> ((int) 0x10)) & (0x1f);
							var rd = ((inst) >> ((int) 0xb)) & (0x1f);
							return($"mfc0x{cop:X} %{rt}, 0x{rd:X}");
							break;
						}
						case 0x2: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var rt = ((inst) >> ((int) 0x10)) & (0x1f);
							var rd = ((inst) >> ((int) 0xb)) & (0x1f);
							return($"cfc0x{cop:X} %{rt}, 0x{rd:X}");
							break;
						}
						case 0x4: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var rt = ((inst) >> ((int) 0x10)) & (0x1f);
							var rd = ((inst) >> ((int) 0xb)) & (0x1f);
							return($"mtc0x{cop:X} %{rt}, 0x{rd:X}");
							break;
						}
						case 0x6: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var rt = ((inst) >> ((int) 0x10)) & (0x1f);
							var rd = ((inst) >> ((int) 0xb)) & (0x1f);
							return($"ctc0x{cop:X} %{rt}, 0x{rd:X}");
							break;
						}
						case 0x10: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x11: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x12: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x13: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x14: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x15: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x16: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x17: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x18: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x19: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x1a: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x1b: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x1c: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x1d: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x1e: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
						case 0x1f: {
							var cop = ((inst) >> ((int) 0x1a)) & (0x3);
							var cofun = (inst) & (0x1ffffff);
							return($"cop0x{cop:X} 0x{cofun:X}");
							break;
						}
					}
					break;
				}
				case 0x20: {
					var rs = ((inst) >> ((int) 0x15)) & (0x1f);
					var rt = ((inst) >> ((int) 0x10)) & (0x1f);
					var imm = (inst) & (0xffff);
					var offset = (uint) (SignExt(0x10, imm));
					return($"lb %{rt}, 0x{offset:X}(%{rs})");
					break;
				}
				case 0x21: {
					var rs = ((inst) >> ((int) 0x15)) & (0x1f);
					var rt = ((inst) >> ((int) 0x10)) & (0x1f);
					var imm = (inst) & (0xffff);
					var offset = (uint) (SignExt(0x10, imm));
					var addr = ((Gpr)[rs]) + (offset);
					return($"lh %{rt}, 0x{offset:X}(%{rs})");
					break;
				}
				case 0x22: {
					var rs = ((inst) >> ((int) 0x15)) & (0x1f);
					var rt = ((inst) >> ((int) 0x10)) & (0x1f);
					var imm = (inst) & (0xffff);
					var simm = (uint) (SignExt(0x10, imm));
					var offset = ((Gpr)[rs]) + (simm);
					var bottom = (offset) & (0x3);
					var moffset = (offset) & (0xfffffffc);
					
					return($"lwl %{rt}, 0x{offset:X}(%{rs})");
					break;
				}
				case 0x23: {
					var rs = ((inst) >> ((int) 0x15)) & (0x1f);
					var rt = ((inst) >> ((int) 0x10)) & (0x1f);
					var imm = (inst) & (0xffff);
					var offset = (uint) (SignExt(0x10, imm));
					var addr = ((Gpr)[rs]) + (offset);
					return($"lw %{rt}, 0x{offset:X}(%{rs})");
					break;
				}
				case 0x24: {
					var rs = ((inst) >> ((int) 0x15)) & (0x1f);
					var rt = ((inst) >> ((int) 0x10)) & (0x1f);
					var imm = (inst) & (0xffff);
					var offset = (uint) (SignExt(0x10, imm));
					return($"lbu %{rt}, 0x{offset:X}(%{rs})");
					break;
				}
				case 0x25: {
					var rs = ((inst) >> ((int) 0x15)) & (0x1f);
					var rt = ((inst) >> ((int) 0x10)) & (0x1f);
					var imm = (inst) & (0xffff);
					var offset = (uint) (SignExt(0x10, imm));
					var addr = ((Gpr)[rs]) + (offset);
					return($"lhu %{rt}, 0x{offset:X}(%{rs})");
					break;
				}
				case 0x26: {
					var rs = ((inst) >> ((int) 0x15)) & (0x1f);
					var rt = ((inst) >> ((int) 0x10)) & (0x1f);
					var imm = (inst) & (0xffff);
					var simm = (uint) (SignExt(0x10, imm));
					var offset = ((Gpr)[rs]) + (simm);
					var bottom = (offset) & (0x3);
					
					return($"lwr %{rt}, 0x{offset:X}(%{rs})");
					break;
				}
				case 0x28: {
					var rs = ((inst) >> ((int) 0x15)) & (0x1f);
					var rt = ((inst) >> ((int) 0x10)) & (0x1f);
					var imm = (inst) & (0xffff);
					var offset = (uint) (SignExt(0x10, imm));
					return($"sb %{rt}, 0x{offset:X}(%{rs})");
					break;
				}
				case 0x29: {
					var rs = ((inst) >> ((int) 0x15)) & (0x1f);
					var rt = ((inst) >> ((int) 0x10)) & (0x1f);
					var imm = (inst) & (0xffff);
					var offset = (uint) (SignExt(0x10, imm));
					var addr = ((Gpr)[rs]) + (offset);
					return($"sh %{rt}, 0x{offset:X}(%{rs})");
					break;
				}
				case 0x2a: {
					var rs = ((inst) >> ((int) 0x15)) & (0x1f);
					var rt = ((inst) >> ((int) 0x10)) & (0x1f);
					var imm = (inst) & (0xffff);
					var simm = (uint) (SignExt(0x10, imm));
					var offset = ((Gpr)[rs]) + (simm);
					var bottom = (offset) & (0x3);
					var moffset = (offset) & (0xfffffffc);
					
					return($"swl %{rt}, 0x{offset:X}(%{rs})");
					break;
				}
				case 0x2b: {
					var rs = ((inst) >> ((int) 0x15)) & (0x1f);
					var rt = ((inst) >> ((int) 0x10)) & (0x1f);
					var imm = (inst) & (0xffff);
					var offset = (uint) (SignExt(0x10, imm));
					var addr = ((Gpr)[rs]) + (offset);
					return($"sw %{rt}, 0x{offset:X}(%{rs})");
					break;
				}
				case 0x2e: {
					var rs = ((inst) >> ((int) 0x15)) & (0x1f);
					var rt = ((inst) >> ((int) 0x10)) & (0x1f);
					var imm = (inst) & (0xffff);
					var simm = (uint) (SignExt(0x10, imm));
					var offset = ((Gpr)[rs]) + (simm);
					var bottom = (offset) & (0x3);
					
					return($"swr %{rt}, 0x{offset:X}(%{rs})");
					break;
				}
				case 0x32: {
					var rs = ((inst) >> ((int) 0x15)) & (0x1f);
					var rt = ((inst) >> ((int) 0x10)) & (0x1f);
					var imm = (inst) & (0xffff);
					var offset = (uint) (SignExt(0x10, imm));
					var addr = ((Gpr)[rs]) + (offset);
					return($"lwc2 %{rt}, 0x{offset:X}(%{rs})");
					break;
				}
				case 0x3a: {
					var rs = ((inst) >> ((int) 0x15)) & (0x1f);
					var rt = ((inst) >> ((int) 0x10)) & (0x1f);
					var imm = (inst) & (0xffff);
					var offset = (uint) (SignExt(0x10, imm));
					var addr = ((Gpr)[rs]) + (offset);
					return($"swc2 %{rt}, 0x{offset:X}(%{rs})");
					break;
				}
			}
			return $"Unknown instruction at {pc:X8}: {inst:X8}";
		}
	}
}
