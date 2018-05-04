class DagOp;

def add : DagOp;
def sub : DagOp;
def and : DagOp;
def or : DagOp;
def nor : DagOp;
def xor : DagOp;
def mul : DagOp; // word * word -> word
def mul64 : DagOp; // word * word -> dword
def umul64 : DagOp; // word * word -> dword
def div : DagOp; // word / word -> word
def mod : DagOp; // word % word -> word
def shl : DagOp;
def shra : DagOp;
def shrl : DagOp;
def signext : DagOp;
def zeroext : DagOp;
def cast : DagOp;

def eq : DagOp;
def ge : DagOp;
def gt : DagOp;
def le : DagOp;
def lt : DagOp;
def neq : DagOp;

def bool2uint : DagOp;
def true : DagOp;
def false : DagOp;

def branch : DagOp;
def branch_default : DagOp;
def copreg : DagOp;
def copcreg : DagOp;
def gpr : DagOp;
def hi : DagOp;
def lo : DagOp;
def check_overflow : DagOp;
def check_store_alignment : DagOp;
def check_load_alignment : DagOp;
def pcd : DagOp;
def raise : DagOp;
def load : DagOp;
def store : DagOp;

def block : DagOp;
def if : DagOp;
def let : DagOp;
def rlet: DagOp;
def set : DagOp;
def defer_set : DagOp;
def when : DagOp;
def unsigned : DagOp;
def signed : DagOp;

// Special traps
def break : DagOp;
def syscall : DagOp;
def copfun : DagOp;

def do_load : DagOp;
def div_delay : DagOp;
def mul_delay : DagOp;
def absorb_muldiv_delay : DagOp;

class Exception;
def ArithmeticOverflow : Exception;

class BaseInst<bits<6> op, string disasm, dag eval> {
	bits<6> Opcode = op;

	string Disasm = disasm;
	dag Eval = eval;
}

class IType<bits<6> op, string disasm, dag eval> : BaseInst<op, disasm, eval> {
	// 31-26: op
	// 25-21: rs
	// 20-16: rt
	// 15-0: imm
}

class RIType<bits<6> op, bits<5> funct, string disasm, dag eval> : BaseInst<op, disasm, eval> {
	// 31-26: op
	// 25-21: rs
	// 20-16: funct
	// 15-0: imm

	bits<5> Function = funct;
}

class JType<bits<6> op, string disasm, dag eval> : BaseInst<op, disasm, eval> {
	// 31-26: op
	// 25-0: imm
}

class RType<bits<6> funct, string disasm, dag eval> : BaseInst<0b000000, disasm, eval> {
	// 31-26: op
	// 25-21: rs
	// 20-16: rt
	// 15-11: rd
	// 10-6: shamt
	// 5-0: funct

	bits<6> Function = funct;
}

class SType<bits<6> funct, string disasm, dag eval> : BaseInst<0b000000, disasm, eval> {
	// 31-26: op
	// 25-6: code
	// 5-0: funct

	bits<6> Function = funct;
}

class CFType<bits<6> op, bits<5> funct, string disasm, dag eval> : BaseInst<op, disasm, eval> {
	// 31-26: op
	// 27-26: cop
	// 25-21: funct
	// 20-16: rt
	// 15-11: rd
	// 24-0: cofun

	bits<5> Function = funct;
}

multiclass CFDef<bits<6> op, bits<5> funct, string disasm, dag eval> {
	def : CFType<!add(op, 0b000000), funct, disasm, eval>;
	def : CFType<!add(op, 0b000001), funct, disasm, eval>;
	def : CFType<!add(op, 0b000010), funct, disasm, eval>;
	def : CFType<!add(op, 0b000011), funct, disasm, eval>;
}

def ADD : RType<0b100000, "add %$rd, %$rs, %$rt",
	(block
		(check_overflow (add (gpr $rs), (gpr $rt))),
		(set (gpr $rd), (add (gpr $rs), (gpr $rt))))
>;

def ADDI : IType<0b001000, "addi %$rt, %$rs, $eimm",
	(let $eimm, (unsigned (signext 16, $imm)),
		(block
			(check_overflow (add (gpr $rs), $eimm)),
			(set (gpr $rt), (add (gpr $rs), $eimm))))
>;

def ADDIU : IType<0b001001, "addiu %$rt, %$rs, $eimm",
	(let $eimm, (unsigned (signext 16, $imm)),
		(set (gpr $rt), (add (gpr $rs), $eimm)))
>;

def ADDU : RType<0b100001, "addu %$rd, %$rs, %$rt",
	(set (gpr $rd), (add (gpr $rs), (gpr $rt)))
>;

def AND : RType<0b100100, "and %$rd, %$rs, %$rt",
	(set (gpr $rd), (and (gpr $rs), (gpr $rt)))
>;

def ANDI : IType<0b001100, "andi %$rt, %$rs, $eimm",
	(let $eimm, (zeroext 16, $imm),
		(set (gpr $rt), (and (gpr $rs), $eimm)))
>;

def BEQ : IType<0b000100, "beq %$rs, %$rt, $target",
	(let $target, (unsigned (add (pcd), (shl (signext 16, $imm), 2))),
		(if (eq (gpr $rs), (gpr $rt)),
			(branch $target),
			(branch_default)))
>;

// This is used to fill in the middle 3 bits of funct
// Since the PSX cpu seems to ignore them
multiclass BHack<bits<6> op, bits<5> funct, string disasm, dag eval> {
	def : RIType<op, funct, disasm, eval>;
	def : RIType<op, !add(funct, 0b00010), disasm, eval>;
	def : RIType<op, !add(funct, 0b00100), disasm, eval>;
	def : RIType<op, !add(funct, 0b00110), disasm, eval>;
	def : RIType<op, !add(funct, 0b01000), disasm, eval>;
	def : RIType<op, !add(funct, 0b01010), disasm, eval>;
	def : RIType<op, !add(funct, 0b01100), disasm, eval>;
	def : RIType<op, !add(funct, 0b01110), disasm, eval>;
}

defm BGEZ : BHack<0b000001, 0b00001, "bgez %$rs, $target",
	(let $target, (unsigned (add (pcd), (shl (signext 16, $imm), 2))),
		(if (ge (signed (gpr $rs)), 0),
			(branch $target),
			(branch_default)))
>;

defm BGEZAL : BHack<0b000001, 0b10001, "bgezal %$rs, $target",
	(block
		(set (gpr 31), (add (pcd), 4)),
		(let $target, (unsigned (add (pcd), (shl (signext 16, $imm), 2))),
			(if (ge (signed (gpr $rs)), 0),
				(branch $target),
				(branch_default))))
>;

// This isn't really RIType, but that lets us constrain rt
def BGTZ : RIType<0b000111, 0b00000, "bgtz %$rs, $target",
	(let $target, (unsigned (add (pcd), (shl (signext 16, $imm), 2))),
		(if (gt (signed (gpr $rs)), 0),
			(branch $target),
			(branch_default)))
>;

// This isn't really RIType, but that lets us constrain rt
def BLEZ : RIType<0b000110, 0b00000, "blez %$rs, $target",
	(let $target, (unsigned (add (pcd), (shl (signext 16, $imm), 2))),
		(if (le (signed (gpr $rs)), 0),
			(branch $target),
			(branch_default)))
>;

defm BLTZ : BHack<0b000001, 0b00000, "bltz %$rs, $target",
	(let $target, (unsigned (add (pcd), (shl (signext 16, $imm), 2))),
		(if (lt (signed (gpr $rs)), 0),
			(branch $target),
			(branch_default)))
>;

defm BLTZAL : BHack<0b000001, 0b10000, "bltzal %$rs, $target",
	(block
		(set (gpr 31), (add (pcd), 4)),
		(let $target, (unsigned (add (pcd), (shl (signext 16, $imm), 2))),
			(if (lt (signed (gpr $rs)), 0),
				(branch $target),
				(branch_default))))
>;

def BNE : IType<0b000101, "bne %$rs, %$rt, $target",
	(let $target, (unsigned (add (pcd), (shl (signext 16, $imm), 2))),
		(if (neq (gpr $rs), (gpr $rt)),
			(branch $target),
			(branch_default)))
>;

def BREAK : SType<0b001101, "break $code",
	(break $code, $pc, $inst)
>;

defm CFCz : CFDef<0b010000, 0b00010, "cfc$cop %$rt, $rd",
	(set (gpr $rt), (copcreg $cop, $rd))
>;

// HACK! This should be special cased.
multiclass COPzType<string disasm, dag eval> {
	defm : CFDef<0b010000, 0b10000, disasm, eval>;
	defm : CFDef<0b010000, 0b10001, disasm, eval>;
	defm : CFDef<0b010000, 0b10010, disasm, eval>;
	defm : CFDef<0b010000, 0b10011, disasm, eval>;
	defm : CFDef<0b010000, 0b10100, disasm, eval>;
	defm : CFDef<0b010000, 0b10101, disasm, eval>;
	defm : CFDef<0b010000, 0b10110, disasm, eval>;
	defm : CFDef<0b010000, 0b10111, disasm, eval>;

	defm : CFDef<0b010000, 0b11000, disasm, eval>;
	defm : CFDef<0b010000, 0b11001, disasm, eval>;
	defm : CFDef<0b010000, 0b11010, disasm, eval>;
	defm : CFDef<0b010000, 0b11011, disasm, eval>;
	defm : CFDef<0b010000, 0b11100, disasm, eval>;
	defm : CFDef<0b010000, 0b11101, disasm, eval>;
	defm : CFDef<0b010000, 0b11110, disasm, eval>;
	defm : CFDef<0b010000, 0b11111, disasm, eval>;
}
defm COPz : COPzType<"cop$cop $cofun",
	(copfun $cop, $cofun, $inst)
>;

defm CTCz : CFDef<0b010000, 0b00110, "ctc$cop %$rt, $rd",
	(set (copcreg $cop, $rd), (gpr $rt))
>;

def DIV : RType<0b011010, "div %$rs, %$rt",
	(if (eq (gpr $rt), 0),
		(block
			(if (neq (and (gpr $rs), 0x80000000), 0),
				(set (lo), 1),
				(set (lo), 0xFFFFFFFF)),
			(set (hi), (gpr $rs))),
		(if (and (eq (gpr $rs), 0x80000000), (eq (gpr $rt), 0xFFFFFFFF)),
			(block
				(set (lo), 0x80000000),
				(set (hi), 0)),
			(block
				(set (lo), (unsigned (div (signed (gpr $rs)), (signed (gpr $rt))))),
				(set (hi), (unsigned (mod (signed (gpr $rs)), (signed (gpr $rt))))),
				(div_delay))))
>;

def DIVU : RType<0b011011, "divu %$rs, %$rt",
	(if (eq (gpr $rt), 0),
		(block
			(set (lo), 0xFFFFFFFF),
			(set (hi), (gpr $rs))),
		(block
			(set (lo), (div (gpr $rs), (gpr $rt))),
			(set (hi), (mod (gpr $rs), (gpr $rt))),
			(div_delay)))
>;


def J : JType<0b000010, "j $target",
	(let $target, (add (and (pcd), 0xF0000000), (zeroext 28, (shl $imm, 2))),
		(branch $target))
>;

def JAL : JType<0b000011, "jal $target",
	(block
		(set (gpr 31), (add (pcd), 4)),
		(let $target, (add (and (pcd), 0xF0000000), (zeroext 28, (shl $imm, 2))),
			(branch $target)))
>;

def JALR : RType<0b001001, "jalr %$rd, %$rs",
	(block
		(set (gpr $rd), (add (pcd), 4)),
		(check_load_alignment (gpr $rs), 32),
		(branch (gpr $rs)))
>;

def JR : RType<0b001000, "jr %$rs",
	(block
		(check_load_alignment (gpr $rs), 32),
		(branch (gpr $rs)))
>;

def LB : IType<0b100000, "lb %$rt, $offset(%$rs)",
	(let $offset, (unsigned (signext 16, $imm)),
		(defer_set (gpr $rt), (unsigned (signext 8, (load 8, (add (gpr $rs), $offset))))))
>;

def LBU : IType<0b100100, "lbu %$rt, $offset(%$rs)",
	(let $offset, (unsigned (signext 16, $imm)),
		(defer_set (gpr $rt), (zeroext 8, (load 8, (add (gpr $rs), $offset)))))
>;

def LH : IType<0b100001, "lh %$rt, $offset(%$rs)",
	(let $offset, (unsigned (signext 16, $imm)),
		(rlet $addr, (add (gpr $rs), $offset),
			(check_load_alignment $addr, 16),
			(defer_set (gpr $rt), (unsigned (signext 16, (load 16, $addr))))))
>;

def LHU : IType<0b100101, "lhu %$rt, $offset(%$rs)",
	(let $offset, (unsigned (signext 16, $imm)),
		(rlet $addr, (add (gpr $rs), $offset),
			(check_load_alignment $addr, 16),
			(defer_set (gpr $rt), (zeroext 16, (load 16, $addr)))))
>;

def LUI : IType<0b001111, "lui %$rt, $imm",
	(set (gpr $rt), (shl $imm, 16))
>;

def LW : IType<0b100011, "lw %$rt, $offset(%$rs)",
	(let $offset, (unsigned (signext 16, $imm)),
		(rlet $addr, (add (gpr $rs), $offset),
			(check_load_alignment $addr, 32),
			(defer_set (gpr $rt), (load 32, $addr))))
>;

def LWL : IType<0b100010, "lwl %$rt, $offset(%$rs)",
	(block
		(do_load $rt),
		(let $simm, (unsigned (signext 16, $imm)),
			(rlet $offset, (add (gpr $rs), $simm),
				(rlet $bottom, (and $offset, 0x3),
					(rlet $moffset, (and $offset, 0xFFFFFFFC),
						(if (eq $bottom, 0),
							(defer_set (gpr $rt), (or (and (gpr $rt), 0x00FFFFFF), (shl (load 8, $moffset), 24))),
							(if (eq $bottom, 1),
								(defer_set (gpr $rt), (or (and (gpr $rt), 0x0000FFFF), (shl (load 16, $moffset), 16))),
								(if (eq $bottom, 2),
									(defer_set (gpr $rt), (or (and (gpr $rt), 0x000000FF), (shl (load 24, $moffset), 8))),
									(defer_set (gpr $rt), (load 32, $moffset))))))))))
>;
def LWR : IType<0b100110, "lwr %$rt, $offset(%$rs)",
	(block
		(do_load $rt),
		(let $simm, (unsigned (signext 16, $imm)),
			(rlet $offset, (add (gpr $rs), $simm),
				(rlet $bottom, (and $offset, 0x3),
					(if (eq $bottom, 0),
						(defer_set (gpr $rt), (load 32, $offset)),
						(if (eq $bottom, 1),
							(defer_set (gpr $rt), (or (and (gpr $rt), 0xFF000000), (load 24, $offset))),
							(if (eq $bottom, 2),
								(defer_set (gpr $rt), (or (and (gpr $rt), 0xFFFF0000), (load 16, $offset))),
								(defer_set (gpr $rt), (or (and (gpr $rt), 0xFFFFFF00), (load 8, $offset))))))))))
>;

def LWC2: IType<0b110010, "lwc2 %$rt, $offset(%$rs)",
	(let $offset, (unsigned (signext 16, $imm)),
		(rlet $addr, (add (gpr $rs), $offset),
			(check_load_alignment $addr, 32),
			(set (copreg 2, $rt), (load 32, (add (gpr $rs), $offset)))))
>;

defm MFCz : CFDef<0b010000, 0b00000, "mfc$cop %$rt, $rd",
	(defer_set (gpr $rt), (copreg $cop, $rd))
>;

def MFHI : RType<0b010000, "mfhi %$rd",
	(block
		(set (gpr $rd), (hi)),
		(absorb_muldiv_delay))
>;

def MFLO : RType<0b010010, "mflo %$rd",
	(block
		(set (gpr $rd), (lo)),
		(absorb_muldiv_delay))
>;

defm MTCz : CFDef<0b010000, 0b00100, "mtc$cop %$rt, $rd",
	(set (copreg $cop, $rd), (gpr $rt))
>;

def MTHI : RType<0b010001, "mthi %$rd",
	(set (hi), (gpr $rd))
>;

def MTLO : RType<0b010011, "mtlo %$rd",
	(set (lo), (gpr $rd))
>;

def MULT : RType<0b011000, "mult %$rs, %$rt",
	(rlet $_t, (unsigned (mul64 (gpr $rs), (gpr $rt)), 64),
		(set (lo), (cast 32, $_t)),
		(set (hi), (cast 32, (shrl $_t, 32))),
		(mul_delay (gpr $rs), (gpr $rt), true))
>;

def MULTU : RType<0b011001, "multu %$rs, %$rt",
	(rlet $_t, (umul64 (gpr $rs), (gpr $rt)),
		(set (lo), (cast 32, $_t)),
		(set (hi), (cast 32, (shrl $_t, 32))),
		(mul_delay (gpr $rs), (gpr $rt), false))
>;

def NOR : RType<0b100111, "nor %$rd, %$rs, %$rt",
	(set (gpr $rd), (nor (gpr $rs), (gpr $rt)))
>;

def OR : RType<0b100101, "or %$rd, %$rs, %$rt",
	(set (gpr $rd), (or (gpr $rs), (gpr $rt)))
>;

def ORI : IType<0b001101, "ori %$rt, %$rs, $eimm",
	(let $eimm, (zeroext 16, $imm),
		(set (gpr $rt), (or (gpr $rs), $eimm)))
>;

def SB : IType<0b101000, "sb %$rt, $offset(%$rs)",
	(let $offset, (unsigned (signext 16, $imm)),
		(store 8, (add (gpr $rs), $offset), (gpr $rt)))
>;

def SH : IType<0b101001, "sh %$rt, $offset(%$rs)",
	(let $offset, (unsigned (signext 16, $imm)),
		(rlet $addr, (add (gpr $rs), $offset),
			(check_store_alignment $addr, 16),
			(store 16, $addr, (gpr $rt))))
>;

def SLL : RType<0b000000, "sll %$rd, %$rt, $shamt",
	(set (gpr $rd), (shl (gpr $rt), $shamt))
>;

def SLLV : RType<0b000100, "sllv %$rd, %$rt, %$rs",
	(set (gpr $rd), (shl (gpr $rt), (gpr $rs)))
>;

def SLT : RType<0b101010, "slt %$rd, %$rs, %$rt",
	(set (gpr $rd), (bool2uint (lt (signed (gpr $rs)), (signed (gpr $rt)))))
>;

def SLTI : IType<0b001010, "slti %$rt, %$rs, $eimm",
	(let $eimm, (unsigned (signext 16, $imm)),
		(set (gpr $rt), (bool2uint (lt (signed (gpr $rs)), (signed $eimm)))))
>;

def SLTIU : IType<0b001011, "sltiu %$rt, %$rs, $eimm",
	(let $eimm, (unsigned (signext 16, $imm)),
		(set (gpr $rt), (bool2uint (lt (gpr $rs), $eimm))))
>;

def SLTU : RType<0b101011, "sltu %$rd, %$rs, %$rt",
	(set (gpr $rd), (bool2uint (lt (gpr $rs), (gpr $rt))))
>;

def SRA : RType<0b000011, "sra %$rd, %$rt, $shamt",
	(set (gpr $rd), (unsigned (shra (gpr $rt), $shamt)))
>;

def SRAV : RType<0b000111, "srav %$rd, %$rt, $rs",
	(set (gpr $rd), (unsigned (shra (gpr $rt), (gpr $rs))))
>;

def SRL : RType<0b000010, "srl %$rd, %$rt, $shamt",
	(set (gpr $rd), (shrl (gpr $rt), $shamt))
>;

def SRLV : RType<0b000110, "srlv %$rd, %$rt, $rs",
	(set (gpr $rd), (shrl (gpr $rt), (gpr $rs)))
>;

def SUB : RType<0b100010, "sub %$rd, %$rs, %$rt",
	(block
		(check_overflow (sub (gpr $rs), (gpr $rt))),
		(set (gpr $rd), (sub (gpr $rs), (gpr $rt))))
>;

def SUBU : RType<0b100011, "subu %$rd, %$rs, %$rt",
	(set (gpr $rd), (sub (gpr $rs), (gpr $rt)))
>;

def SW : IType<0b101011, "sw %$rt, $offset(%$rs)",
	(let $offset, (unsigned (signext 16, $imm)),
		(rlet $addr, (add (gpr $rs), $offset),
			(check_store_alignment $addr, 32),
			(store 32, $addr, (gpr $rt))))
>;

def SWC2: IType<0b111010, "swc2 %$rt, $offset(%$rs)",
	(let $offset, (unsigned (signext 16, $imm)),
		(rlet $addr, (add (gpr $rs), $offset),
			(check_store_alignment $addr, 32),
			(store 32, $addr, (copreg 2, $rt))))
>;

def SWL : IType<0b101010, "swl %$rt, $offset(%$rs)",
	(let $simm, (unsigned (signext 16, $imm)),
		(rlet $offset, (add (gpr $rs), $simm),
			(rlet $bottom, (and $offset, 0x3),
				(rlet $moffset, (and $offset, 0xFFFFFFFC),
					(if (eq $bottom, 0),
						(store 8, $moffset, (shrl (gpr $rt), 24)),
						(if (eq $bottom, 1),
							(store 16, $moffset, (shrl (gpr $rt), 16)),
							(if (eq $bottom, 2),
								(store 24, $moffset, (shrl (gpr $rt), 8)),
								(store 32, $moffset, (gpr $rt)))))))))
>;
def SWR : IType<0b101110, "swr %$rt, $offset(%$rs)",
	(let $simm, (unsigned (signext 16, $imm)),
		(rlet $offset, (add (gpr $rs), $simm),
			(rlet $bottom, (and $offset, 0x3),
				(if (eq $bottom, 0),
					(store 32, $offset, (gpr $rt)),
					(if (eq $bottom, 1),
						(store 24, $offset, (gpr $rt)),
						(if (eq $bottom, 2),
							(store 16, $offset, (gpr $rt)),
							(store 8, $offset, (gpr $rt))))))))
>;

def SYSCALL : SType<0b001100, "syscall $code",
	(syscall $code, $pc, $inst)
>;

def XOR : RType<0b100110, "xor %$rd, %$rs, %$rt",
	(set (gpr $rd), (xor (gpr $rs), (gpr $rt)))
>;

def XORI : IType<0b001110, "xori %$rt, %$rs, $eimm",
	(let $eimm, (zeroext 16, $imm),
		(set (gpr $rt), (xor (gpr $rs), $eimm)))
>;