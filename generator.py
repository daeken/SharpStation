import json, os.path, re, struct, sys
from tblgen import interpret, Dag, TableGenBits

def dag2expr(dag):
	def clean(value):
		if isinstance(value, tuple) and len(value) == 2 and value[0] == 'defref':
			return value[1]
		return value

	def sep((name, value)):
		if name is None:
			return clean(value)
		return name

	if isinstance(dag, Dag):
		return [dag2expr(sep(elem)) for elem in dag.elements]
	else:
		return dag


if not os.path.exists('insts.td.cache') or os.path.getmtime('insts.td') > os.path.getmtime('insts.td.cache'):
	print 'Rebuilding instruction cache'
	insts = interpret('insts.td').deriving('BaseInst')
	ops = []
	for name, (bases, data) in insts:
		ops.append((name, bases[1], data['Opcode'][1], data['Function'][1] if 'Function' in data else None,
		            data['Disasm'][1], dag2expr(data['Eval'][1])))
	with file('insts.td.cache', 'w') as fp:
		json.dump(ops, fp)
else:
	ops = json.load(file('insts.td.cache'))

toplevel = {}

for name, type, op, funct, dasm, dag in ops:
	if funct is None:
		assert op not in toplevel
		toplevel[op] = name, type, dasm, dag
	else:
		if op not in toplevel:
			toplevel[op] = [type, {}]
		toplevel[op][1][funct] = name, type, dasm, dag


def generate(gfunc):
	switch = []
	for op, body in toplevel.items():
		if isinstance(body, list):
			type, body = body
			subswitch = []
			for funct, sub in body.items():
				subswitch.append(('case', funct, gfunc(sub)))
			if type == 'CFType':
				when = ('&', ('>>', 'inst', 21), 0x1F)
			elif type == 'RIType':
				when = ('&', ('>>', 'inst', 16), 0x1F)
			else:
				when = ('&', 'inst', 0x3F)
			switch.append(('case', op, ('switch', when, subswitch)))
		else:
			switch.append(('case', op, gfunc(body)))
	return ('switch', ('>>', 'inst', 26), switch)


def indent(str, single=True, count=1):
	if single and '\n' not in str:
		return ' %s ' % str
	else:
		return '\n%s\n' % '\n'.join('\t' * count + x for x in str.split('\n'))


def output(expr, top=True):
	if isinstance(expr, list):
		return '\n'.join(output(x, top=top) for x in expr)
	elif isinstance(expr, int) or isinstance(expr, long):
		if expr >= 0:
			return '0x%x' % expr
		else:
			return '-0x%x' % -expr
	elif isinstance(expr, str) or isinstance(expr, unicode):
		expr = expr if expr.startswith('$"') else expr.replace('$', '')
		return expr

	op = expr[0]
	if op == 'switch':
		return 'switch(%s) {%s}' % (output(expr[1]), indent(output(expr[2])))
	elif op == 'case':
		return 'case %s: {%s\tbreak;\n}' % (output(expr[1]), indent(output(expr[2]), single=False))
	elif op in ('+', '-', '*', '/', '%', '<<', '>>', '>>', '&', '|', '^', '==', '!=', '<', '<=', '>', '>='):
		return '(%s) %s (%s)' % (output(expr[1], top=False), op, ('(int) ' if op in ('<<', '>>') else '') + output(expr[2], top=False))
	elif op == '!':
		return '!(%s)' % output(expr[1], top=False)
	elif op == '=':
		lval = output(expr[1], top=False)
		rval = output(expr[2], top=False)
		type = ''
		if lval != 'branched' and lval != 'no_delay' and lval != 'BranchTo' and lval != 'has_load' and '(' not in lval and lval not in ('Hi', 'Lo'):
			if '*' in rval:
				type = 'ulong '
			else:
				type = 'var '
		return '%s%s %s %s;' % (type, lval, op, rval)
	elif op == 'if':
		if len(expr) != 4:
			return ''
		_if, _else = indent(output(expr[2]), single=False), indent(output(expr[3]), single=False)
		if _if.strip() == '' and _else.strip() == '':
			return ''
		return 'if(%s) {%s} else {%s}' % (output(expr[1], top=False), _if, _else)
	elif op == 'when':
		return 'if(%s) {%s}' % (output(expr[1], top=False), indent(output(expr[2])))
	elif op == 'comment':
		return '/*%s*/' % indent(output(expr[1]))
	elif op == 'str':
		return `str(expr[1])`
	elif op == 'index':
		return '(%s)[%s]' % (output(expr[1], top=False), output(expr[2], top=False))
	elif op == 'emit':
		return '\n'.join(flatten(_emitter(expr[1])))
	elif op in gops:
		return output(gops[op](*expr[1:]), top=top)
	elif op == 'zeroext':
		return output(expr[2], top=top)
	elif op == 'signext':
		return 'SignExt(%s)%s' % (', '.join(output(x, top=False) for x in expr[1:]), ';' if top else '')
	elif op == 'cast':
		return '(%s) (%s)' % ('uint' if expr[1] == 32 else 'ulong', output(expr[2], top=False))
	elif op == 'signed':
		if len(expr) == 3:
			return '(%s) (%s)' % ('int' if expr[2] == 32 else 'long', output(expr[1], top=False))
		else:
			return '(int) (%s)' % output(expr[1], top=False)
	elif op == 'unsigned':
		if len(expr) == 3:
			return '(%s) (%s)' % ('uint' if expr[2] == 32 else 'ulong', output(expr[1], top=False))
		else:
			return '(uint) (%s)' % output(expr[1], top=False)
	elif op == '[]':
		return '(%s)[%s]' % (output(expr[1], top=False), output(expr[2], top=False))
	elif op == '?:':
		return '(%s) ? (%s) : (%s)' % (output(expr[1], top=False), output(expr[2], top=False), output(expr[3], top=False))
	elif op == '??':
		return '(%s) ?? (%s)' % (output(expr[1], top=False), output(expr[2], top=False))
	else:
		return '%s(%s)%s' % (op, ', '.join(output(x, top=False) for x in expr[1:]), ';' if top else '')


def flatten(x):
	if isinstance(x, list) or isinstance(x, tuple):
		return reduce(lambda a, b: a + b, map(flatten, x))
	else:
		return [x]


gops = {
	'add': lambda a, b: ('+', a, b),
	'sub': lambda a, b: ('-', a, b),
	'and': lambda a, b: ('&', a, b),
	'or': lambda a, b: ('|', a, b),
	'nor': lambda a, b: ('~', ('|', a, b)),
	'xor': lambda a, b: ('^', a, b),
	'mul': lambda a, b: ('*', a, b),
	'mul64': lambda a, b: ('*', ('signed', ('signed', a, 32), 64), ('signed', ('signed', b, 32), 64)),
	'umul64': lambda a, b: ('*', ('cast', 64, a), ('cast', 64, b)),
	'div': lambda a, b: ('/', a, b),
	'mod': lambda a, b: ('%', a, b),
	'shl': lambda a, b: ('<<', a, b),
	'shra': lambda a, b: ('>>', ('signed', a), ('signed', b)),
	'shrl': lambda a, b: ('>>', a, b),

	'eq': lambda a, b: ('==', a, b),
	'ge': lambda a, b: ('>=', a, b),
	'gt': lambda a, b: ('>', a, b),
	'le': lambda a, b: ('<=', a, b),
	'lt': lambda a, b: ('<', a, b),
	'neq': lambda a, b: ('!=', a, b),
}

eops = {
	'add': lambda a, b: ('call', 'Add', a, b),
	'sub': lambda a, b: ('call', 'Sub', a, b),
	'and': lambda a, b: ('call', 'And', a, b),
	'or': lambda a, b: ('call', 'Or', a, b),
	'nor': lambda a, b: ('call', 'Not', ('call', 'Or', a, b)),
	'xor': lambda a, b: ('call', 'Xor', a, b),
	'mul': lambda a, b: ('call', 'Mul', a, b),
	'mul64': lambda a, b: ('call', 'Mul64', ('cast-signed', 64, ('cast-signed', 32, a)), ('cast-signed', 64, ('cast-signed', 32, b))),
	'umul64': lambda a, b: ('call', 'UMul64', ('cast', 64, a), ('cast', 64, b)),
	'div': lambda a, b: ('call', 'Div', a, b),
	'mod': lambda a, b: ('call', 'Mod', a, b),
	'shl': lambda a, b: ('call', 'Shl', a, b),
	'shra': lambda a, b: ('call', 'SShr', a, b),
	'shrl': lambda a, b: ('call', 'UShr', a, b),

	'eq': lambda a, b: ('call', 'Eq', a, b),
	'ge': lambda a, b: ('call', 'Ge', a, b),
	'gt': lambda a, b: ('call', 'Gt', a, b),
	'le': lambda a, b: ('call', 'Le', a, b),
	'lt': lambda a, b: ('call', 'Lt', a, b),
	'neq': lambda a, b: ('call', 'Ne', a, b),
}


def cleansexp(sexp):
	if isinstance(sexp, list):
		return [cleansexp(x) for x in sexp if x != []]
	elif isinstance(sexp, tuple):
		return tuple([cleansexp(x) for x in sexp if x != []])
	else:
		return sexp


def find_deps(dag):
	if isinstance(dag, str) or isinstance(dag, unicode):
		return set([dag])
	elif not isinstance(dag, list):
		return set()

	return reduce(lambda a, b: a | b, map(find_deps, dag[1:])) if len(dag) != 1 else set()


def decoder(code, vars, type, dag):
	def decl(name, val):
		if name in deps:
			vars.append(name)
			code.append(('=', name, val))

	deps = find_deps(dag)
	if type == 'IType' or type == 'RIType':
		decl('$rs', ('&', ('>>', 'inst', 21), 0x1F))
		decl('$rt', ('&', ('>>', 'inst', 16), 0x1F))
		decl('$imm', ('&', 'inst', 0xFFFF))
	elif type == 'JType':
		decl('$imm', ('&', 'inst', 0x3FFFFFF))
	elif type == 'RType':
		decl('$rs', ('&', ('>>', 'inst', 21), 0x1F))
		decl('$rt', ('&', ('>>', 'inst', 16), 0x1F))
		decl('$rd', ('&', ('>>', 'inst', 11), 0x1F))
		decl('$shamt', ('&', ('>>', 'inst', 6), 0x1F))
	elif type == 'SType':
		decl('$code', ('&', ('>>', 'inst', 6), 0x0FFFFF))
	elif type == 'CFType':
		decl('$cop', ('&', ('>>', 'inst', 26), 3))
		decl('$rt', ('&', ('>>', 'inst', 16), 0x1F))
		decl('$rd', ('&', ('>>', 'inst', 11), 0x1F))
		decl('$cofun', ('&', 'inst', 0x01FFFFFF))
	else:
		print 'Unknown instruction type:', type
		assert False


debug = False


def dlog(dag, code, pos):
	if dag[0] == 'gpr':
		name = ('regname', dag[1])
	elif dag[0] == 'copreg':
		name = '+', ('+', ('+', ('str', 'cop'), dag[1]), ('str', ' reg ')), dag[2]
	elif dag[0] == 'copcreg':
		name = '+', ('+', ('+', ('str', 'cop'), dag[1]), ('str', ' control reg ')), dag[2]
	elif dag[0] in ('hi', 'lo', 'pc'):
		name = dag[0]
	elif dag[0] == 'store':
		name = '>>', dag[1], 0
	else:
		print 'Unknown dag to dlog:', dag

	return ('phex32', name, ('str', pos + ':'), code, ('str', 'uint:'), ('>>', code, 0))


temp_i = 0


def tempname():
	global temp_i
	temp_i += 1
	return 'temp_%i' % temp_i


def _emitter(sexp, storing=False, locals=None):
	def emitter(sexp, _storing=False, _locals=None):
		return _emitter(sexp, storing=storing or _storing, locals=locals if _locals is None else _locals)

	def to_val(val):
		if val.startswith('jit_') or val.startswith('call_') or val.split('(')[0] in (
		'RGPR', 'WGPR', 'RPC', 'WPC', 'RHI', 'WHI', 'RLO', 'WLO') or val.endswith('Ref'):
			return val
		elif '(' in val or val.startswith('temp_') or val in locals:
			return val
		return 'MakeValue<uint>(%s)' % val

	locals = [] if locals is None else locals
	if isinstance(sexp, list):
		if len(sexp) == 1:
			sexp = sexp[0]
		else:
			if isinstance(sexp[0], list):
				return list(map(emitter, sexp))
			_, lvalue, rvalue = sexp[0]
			lvalue = emitter(lvalue)
			vdef = ['var %s = %s;' % (lvalue, emitter(rvalue))]
			for elem in sexp[1:]:
				sub = emitter(elem, _locals=locals + [lvalue])
				if isinstance(sub, list):
					vdef += sub
				else:
					vdef += [sub]
			return vdef

	if isinstance(sexp, str) or isinstance(sexp, unicode):
		return sexp.replace('$', '')
	elif isinstance(sexp, int):
		if sexp >= 0:
			return '0x%x' % sexp
		else:
			return '-0x%x' % -sexp
	op = sexp[0]
	if op == '=':
		lvalue = sexp[1]
		if isinstance(lvalue, list) and len(lvalue) == 1:
			lvalue = lvalue[0]
		if lvalue[0] == 'reg':
			return 'Gprs[%s] = %s;' % (emitter(lvalue[1]), to_val(emitter(sexp[2])))
		elif lvalue[0] == 'pc':
			return 'WPC(%s);' % to_val(emitter(sexp[2]))
		elif lvalue[0] == 'hi':
			return 'HiRef = %s;' % to_val(emitter(sexp[2]))
		elif lvalue[0] == 'lo':
			return 'LoRef = %s;' % to_val(emitter(sexp[2]))
		elif lvalue[0] == 'copreg':
			return 'WriteCopreg(%s, %s, %s);' % (
			emitter(lvalue[1]), emitter(lvalue[2]), to_val(emitter(sexp[2])))
		elif lvalue[0] == 'copcreg':
			return 'WriteCopcreg(%s, %s, %s);' % (
			emitter(lvalue[1]), emitter(lvalue[2]), to_val(emitter(sexp[2])))
		else:
			print 'Unknown lvalue', lvalue
			raise False
	elif op == 'defer_set':
		return 'DeferSet(%s, %s);' % (emitter(sexp[1][1]), to_val(emitter(sexp[2])))
	elif op == 'reg':
		return 'RGPR(%s)' % emitter(sexp[1])
	elif op == 'pc':
		return 'RPC()'
	elif op == 'hi':
		return 'HiRef'
	elif op == 'lo':
		return 'LoRef'
	elif op == 'copreg':
		return 'GenReadCopreg(%s, %s)' % (emitter(sexp[1]), emitter(sexp[2]))
	elif op == 'copcreg':
		return 'GenReadCopcreg(%s, %s)' % (emitter(sexp[1]), emitter(sexp[2]))
	elif op == 'branch':
		#if (isinstance(sexp[1], str) or isinstance(sexp[1], unicode)) and not sexp[1].startswith('temp_'):
		#	return 'if(!branched) BranchBlock(GetBlockReference(%s));' % emitter(sexp[1])
		#else:
		return 'if(!branched) Branch(%s);' % emitter(sexp[1])
	elif op == 'branch_default':
		#return 'if(!branched) BranchBlock(GetBlockReference(pc + 8));'
		return 'if(!branched) Branch(pc + 8);'
	elif op == 'Syscall':
		return 'Syscall(%s, %s, %s);' % (emitter(sexp[1]), emitter(sexp[2]), emitter(sexp[3]))
	elif op == 'Break':
		return 'Break(%s, %s, %s);' % (emitter(sexp[1]), emitter(sexp[2]), emitter(sexp[3]))
	elif op == 'copfun':
		return 'GenCopfun(%s, %s, %s);' % (emitter(sexp[1]), emitter(sexp[2]), emitter(sexp[3]))
	elif op == 'emit':
		return emitter(sexp[1], _storing=storing)
	elif op == 'store':
		return 'Store(%i, %s, %s, pc);' % (
		sexp[1], to_val(emitter(sexp[2])), to_val(emitter(sexp[3])))
	elif op == 'load':
		return 'Load(%i, %s, pc)' % (sexp[1], to_val(emitter(sexp[2])))
	elif op == 'signed':
		return 'Signed(%s)' % (to_val(emitter(sexp[1])))
	elif op == 'unsigned':
		return 'Unsigned(%s)' % (to_val(emitter(sexp[1])))
	elif op == 'cast':
		if sexp[1] == 32:
			type = 'ToU32'
		elif sexp[1] == 64:
			type = 'ToU64'
		return '%s(%s)' % (type, to_val(emitter(sexp[2])))
	elif op == 'cast-signed':
		if sexp[1] == 32:
			type = 'ToI32'
		elif sexp[1] == 64:
			type = 'ToI64'
		return '%s(%s)' % (type, to_val(emitter(sexp[2])))
	elif op == 'if':
		temp = tempname()
		end = tempname()
		return [
			'Label %s = Ilg.DefineLabel(), %s = Ilg.DefineLabel();' % (temp, end),
			'BranchIf(%s, %s);' % (to_val(emitter(sexp[1])), temp),
			emitter(sexp[3]),
			'Branch(%s);' % end,
			'Label(%s);' % temp,
			emitter(sexp[2]),
			'Label(%s);' % end
		]
	elif op == 'when':
		temp = tempname()
		return [
			'var %s = Ilg.DefineLabel();' % temp,
			'BranchIfNot(%s, %s);' % (to_val(emitter(sexp[1])), temp),
			emitter(sexp[2]),
			'Label(%s);' % temp
		]
	elif op == 'overflow':
		if sexp[1][0] == 'add':
			return 'Overflow(%s, %s, 1, pc, inst);' % (
			to_val(emitter(sexp[1][1])), to_val(emitter(sexp[1][2])))
		else:
			return 'Overflow(%s, %s, -1, pc, inst);' % (
			to_val(emitter(sexp[1][1])), to_val(emitter(sexp[1][2])))
	elif op == 'Alignment':
		return 'Alignment(%s, %i, %s, pc);' % (to_val(emitter(sexp[1])), sexp[2], sexp[3])
	elif op == 'zeroext':
		return emitter(sexp[2])
	elif op == 'signext':
		return 'SignExt(%i, %s)' % (sexp[1], emitter(sexp[2]))
	elif op == 'mul_delay':
		return 'MulDelay(%s, %s, %s);' % (
		to_val(emitter(sexp[1])), to_val(emitter(sexp[2])), emitter(sexp[3]))
	elif op == 'div_delay':
		return 'GenDivDelay();'
	elif op == 'ReadAbsorb':
		_else, _end = tempname(), tempname()
		ra, raw = tempname(), tempname()
		return [
			'Label %s = Ilg.DefineLabel(), %s = Ilg.DefineLabel();' % (_else, _end),
			'Value %s = ReadAbsorbWhichRef, %s = RRA(%s);' % (raw, ra, raw),
			'BranchIf(%s, %s);' % (emitter(('eq', ra, 'MakeValue<uint>(0)')), _else),
			'WRA(%s, Sub(%s, MakeValue<uint>(1)));' % (raw, ra),
			'Branch(%s);' % _end,
			'Label(%s);' % _else,
			'TimestampInc(1);',
			'Label(%s);' % _end
		]
	elif op == 'do_load':
		reg = to_val(emitter(sexp[1]))
		return emitter(('if',
		                ('eq', 'LDWhichRef', reg),
		                [
			                ['ReadFudgeRef = MakeValue<uint>(0);'],
			                ['Store(%s, LDValueRef);' % sexp[2]]
		                ],
		                [('DO_LDS',)]
		                ))
	elif op == 'DO_LDS':
		return 'DoLds();'
	elif op == 'check_irq':
		return 'CheckIrq(pc);'
	elif op in eops:
		return emitter(eops[op](*sexp[1:]))
	elif op == 'call':
		return '%s(%s)' % (sexp[1], ', '.join([to_val(emitter(x)) for x in sexp[2:]]))
	elif op == 'bool2uint':
		#return 'BoolToUint(%s)' % emitter(sexp[1])
		return emitter(sexp[1])
	else:
		print 'Unknown', sexp
	sys.exit(1)


def find(dag, name, cb):
	if not isinstance(dag, list):
		return False, dag

	if dag[0] == name:
		return True, cb(dag)
	else:
		any = False
		out = []
		for x in dag:
			f, v = find(x, name, cb)
			any = f or any
			out.append(v)
		return any, out


def findDepres(dag):
	dep, res = set(), set()
	if not isinstance(dag, list):
		return dep, res

	if dag[0] == 'set' or dag[0] == 'defer_set':
		if dag[1][0] == 'gpr':
			res.add(dag[1][1])
		sdep, sres = findDepres(dag[2])
		dep.update(sdep)
		res.update(sres)
	elif dag[0] == 'gpr':
		dep.add(dag[1])
	else:
		for sdep, sres in map(findDepres, dag):
			dep.update(sdep)
			res.update(sres)
	return dep, res


def genCommon(iname, type, dag, decomp):
	code = [('comment', iname)]
	# code += [('emit', ('=', ('pc', ), '$pc'))]
	if decomp:
		# code += [('emit', ('check_irq', ))] # per-instruction irq checking
		code += [('emit', ('ReadAbsorb',))]
	vars = []
	decoder(code, vars, type, dag)

	dep, res = findDepres(dag)
	if len(dep) != 0 or len(res) != 0:
		tdep = dep.difference(res)
		if decomp:
			code += [('DEP', x) for x in tdep] + [('RES', x) for x in res]
		else:
			code += [('when', ('!=', x, 0), [('=', ('[]', 'ReadAbsorb', x), 0)]) for x in tdep.union(res)]

	lregs = {}
	for reg in dep:
		name = tempname()
		if decomp:
			code += [('=', name, ('[]', 'Gprs', reg))]
		else:
			code += [('=', name, ('[]', 'Gpr', reg))]
		lregs[reg] = name

	def cb(subdag):
		return subdag + [lregs[subdag[1]]]

	found, dag = find(dag, 'do_load', cb)
	if not found:
		if decomp:
			code += [('when', 'need_load', ('DoLds', ))]
		else:
			code += [('DoLds', )]
	#code += [('Console.WriteLine', '"' + iname + '"')]

	return dag, code, vars, lregs


def genInterp((iname, type, dasm, dag)):
	dag, code, vars, lregs = genCommon(iname, type, dag, decomp=False)

	def subgen(dag):
		if isinstance(dag, str) or isinstance(dag, unicode):
			return dag
		elif isinstance(dag, int) or isinstance(dag, long):
			return dag
		elif not isinstance(dag, list):
			print 'Fail', dag
			assert False
		op = dag[0]
		if op in ('let', 'rlet'):
			if dag[1] not in vars:
				vars.append(dag[1])
			return [('=', dag[1], subgen(dag[2]))] + subgen(['block'] + dag[3:])
		elif op == 'set':
			left = dag[1]
			leftjs = subgen(left)
			if left[0] == 'copreg' or left[0] == 'copcreg':
				return [('Write' + left[0].title(), subgen(left[1]), subgen(left[2]), subgen(dag[2]))]
			ret = [('=', leftjs, subgen(dag[2]))]
			if left[0] == 'gpr':
				ret = [('when', ('neq', left[1], 0), ret)]
			return ret
		elif op == 'defer_set':
			left = dag[1]
			assert left[0] == 'gpr'
			return [('DeferSet', left[1], subgen(dag[2]))]
		elif op == 'if':
			return [('if', subgen(dag[1]), subgen(dag[2]), subgen(dag[3]))]
		elif op == 'when':
			return [('when', subgen(dag[1]), subgen(dag[2]))]
		elif op in gops:
			return tuple(map(subgen, dag))
		elif op == 'signext':
			return ('SignExt', dag[1], subgen(dag[2]))
		elif op == 'zeroext':
			return ('zeroext', dag[1], subgen(dag[2]))
		elif op == 'pc':
			return ['pc']
		elif op in ('hi', 'lo'):
			return [op.title()]
		elif op == 'pcd':
			return [('add', '$pc', 4)]  # Return the delay slot position
		elif op == 'gpr':
			name = subgen(dag[1])
			if name in lregs:
				return lregs[name]
			return ('[]', 'Gpr', subgen(dag[1]))
		elif op == 'copreg':
			return ('ReadCopreg', subgen(dag[1]), subgen(dag[2]))
		elif op == 'copcreg':
			return ('ReadCopcreg', subgen(dag[1]), subgen(dag[2]))
		elif op == 'block':
			return list(map(subgen, dag[1:]))
		elif op == 'unsigned':
			if len(dag) == 2:
				return ('unsigned', subgen(dag[1]))
			else:
				return ('unsigned', subgen(dag[1]), subgen(dag[2]))
		elif op == 'signed':
			if len(dag) == 2:
				return ('signed', subgen(dag[1]))
			else:
				return ('signed', subgen(dag[1]), subgen(dag[2]))
		elif op == 'bool2uint':
			return ('?:', subgen(dag[1]), '1U', '0U')
		elif op == 'check_overflow':
			if dag[1][0] == 'add':
				return [('Overflow', subgen(dag[1][1]), subgen(dag[1][2]), 1, 'pc', 'inst')]
			else:
				return [('Overflow', subgen(dag[1][1]), subgen(dag[1][2]), -1, 'pc', 'inst')]
		elif op == 'check_store_alignment':
			return [('Alignment', subgen(dag[1]), dag[2], 'true', 'pc')]
		elif op == 'check_load_alignment':
			return [('Alignment', subgen(dag[1]), dag[2], 'false', 'pc')]
		elif op == 'break':
			return [('Break', ('signed', dag[1]), subgen(dag[2]), subgen(dag[3]))]
		elif op == 'syscall':
			return [('Syscall', ('signed', subgen(dag[1])), subgen(dag[2]), subgen(dag[3]))]
		elif op == 'branch':
			return [('=', 'BranchTo', subgen(dag[1]))]
		elif op == 'branch_default':
			return [('=', 'BranchTo', ('unsigned', ('+', 'pc', 8)))]
		elif op == 'load':
			return [('LoadMemory', dag[1], subgen(dag[2]), 'pc')]
		elif op == 'store':
			return [('StoreMemory', dag[1], subgen(dag[2]), subgen(dag[3]), 'pc')]
		elif op == 'copfun':
			return [('Copfun', subgen(dag[1]), subgen(dag[2]), subgen(dag[3]))]
		elif op == 'cast':
			return [('cast', dag[1], subgen(dag[2]))]
		elif op == 'mul_delay':
			return [('MulDelay', subgen(dag[1]), subgen(dag[2]), subgen(dag[3]))]
		elif op == 'div_delay':
			return [('DivDelay',)]
		elif op == 'absorb_muldiv_delay':
			return [('AbsorbMuldivDelay',)]
		elif op == 'do_load':
			return [('DoLoad', subgen(dag[1]), ('ref', dag[2]))]
		else:
			print 'Unknown op:', op
			return []

	code += cleansexp(subgen(dag))
	code.append(('return', 'true'))

	return code

def genDisasm((iname, type, dasm, dag)):
	code = []
	vars = []
	decoder(code, vars, type, dag)

	def subgen(dag):
		if isinstance(dag, str) or isinstance(dag, unicode):
			return dag
		elif isinstance(dag, int) or isinstance(dag, long):
			return dag
		elif not isinstance(dag, list):
			print 'Fail', dag
			assert False
		op = dag[0]
		if op in ('let', 'rlet'):
			if dag[1] not in vars:
				vars.append(dag[1])
			return [('=', dag[1], subgen(dag[2]))] + subgen(['block'] + dag[3:])
		elif op == 'if':
			return [('if', subgen(dag[1]), subgen(dag[2]), subgen(dag[3]))]
		elif op == 'when':
			return [('when', subgen(dag[1]), subgen(dag[2]))]
		elif op in gops:
			return tuple(map(subgen, dag))
		elif op == 'signext':
			return ('SignExt', dag[1], subgen(dag[2]))
		elif op == 'zeroext':
			return ('zeroext', dag[1], subgen(dag[2]))
		elif op == 'pc':
			return ['pc']
		elif op in ('hi', 'lo'):
			return [op.title()]
		elif op == 'pcd':
			return [('add', '$pc', 4)]  # Return the delay slot position
		elif op == 'gpr':
			name = subgen(dag[1])
			return ('[]', 'Gpr', subgen(dag[1]))
		elif op == 'block':
			return list(map(subgen, dag[1:]))
		elif op == 'unsigned':
			if len(dag) == 2:
				return ('unsigned', subgen(dag[1]))
			else:
				return ('unsigned', subgen(dag[1]), subgen(dag[2]))
		elif op == 'signed':
			if len(dag) == 2:
				return ('signed', subgen(dag[1]))
			else:
				return ('signed', subgen(dag[1]), subgen(dag[2]))
		elif op == 'cast':
			return [('cast', dag[1], subgen(dag[2]))]
		else:
			return []

	code += cleansexp(subgen(dag))

	def cb(match):
		reg, name = match.group(1), match.group(2)
		if reg == '%':
			return '%%{%s}' % name
		else:
			return '0x{%s:X}' % name
	d = re.sub(r'(\%?)\$([a-zA-Z0-9]+)', cb, dasm)
	return code + [('return', '$"' + d + '"')]

def genRecomp((iname, type, dasm, dag)):
	dag, code, vars, lregs = genCommon(iname, type, dag, decomp=True)

	has_branch = [False]
	no_delay = [False]
	has_load = [False]

	def subgen(dag, in_set=False):
		if isinstance(dag, str) or isinstance(dag, unicode):
			return dag
		elif isinstance(dag, int) or isinstance(dag, long):
			return dag
		elif not isinstance(dag, list):
			print 'Fail', dag
			assert False
		op = dag[0]
		if op in ('let', 'rlet'):
			if dag[1] not in vars:
				vars.append(dag[1])
			ret = [('=', dag[1], subgen(dag[2]))] + subgen(['block'] + dag[3:])
			if op == 'rlet':
				return [('emit', ret)]
			else:
				return ret
		elif op == 'set':
			left = dag[1]
			leftjs = subgen(left)
			ret = [('emit', ('=', leftjs, subgen(dag[2])))]
			return ret
		elif op == 'defer_set':
			has_load[0] = True
			left = dag[1]
			leftjs = subgen(left, in_set=True)
			ret = [('emit', ('defer_set', leftjs, subgen(dag[2])))]
			return ret
		# XXX: Conditionals should detect if they can happen at decompile-time
		elif op == 'if':
			return [('emit', ('if', subgen(dag[1]), subgen(dag[2]), subgen(dag[3])))]
		elif op == 'when':
			return [('emit', ('when', subgen(dag[1]), subgen(dag[2])))]
		elif op in gops:
			return tuple(map(subgen, dag))
		elif op in ('signext', 'zeroext'):
			return [(op, dag[1], subgen(dag[2]))]
		elif op == 'pc':
			return ['$pc']
		elif op in ('hi', 'lo'):
			return [(op,)]
		elif op == 'pcd':
			return [('add', '$pc', 4)]  # Return the delay slot position
		elif op == 'gpr':
			name = subgen(dag[1])
			if name in lregs and not in_set:
				return lregs[name]
			return ('reg', subgen(dag[1]))
		elif op == 'copreg':
			return ('copreg', subgen(dag[1]), subgen(dag[2]))
		elif op == 'copcreg':
			return ('copcreg', subgen(dag[1]), subgen(dag[2]))
		elif op == 'block':
			return list(map(subgen, dag[1:]))
		elif op == 'unsigned':
			if len(dag) == 2:
				return ('unsigned', subgen(dag[1]))
			else:
				return ('unsigned', subgen(dag[1]), subgen(dag[2]))
		elif op == 'signed':
			return ('signed', subgen(dag[1]))
		elif op == 'bool2uint':
			return ('bool2uint', subgen(dag[1]))
		elif op == 'check_overflow':
			return [('emit', ('overflow', subgen(dag[1])))]
		elif op == 'check_store_alignment':
			return [('emit', ('Alignment', subgen(dag[1]), dag[2], 'true'))]
		elif op == 'check_load_alignment':
			return [('emit', ('Alignment', subgen(dag[1]), dag[2], 'false'))]
		elif op == 'raise':
			return [('emit', ('raise', dag[1]))]
		elif op == 'break':
			has_branch[0] = True
			no_delay[0] = True
			return [('emit', ('Break', dag[1], subgen(dag[2]), subgen(dag[3])))]
		elif op == 'syscall':
			has_branch[0] = True
			no_delay[0] = True
			return [('emit', ('Syscall', subgen(dag[1]), subgen(dag[2]), subgen(dag[3])))]
		elif op == 'branch':
			has_branch[0] = True
			return [('emit', ('branch', subgen(dag[1]), 'true'))]
		elif op == 'branch_default':
			has_branch[0] = True
			return [('emit', ('branch_default',))]
		elif op == 'load':
			return [('load', dag[1], subgen(dag[2]))]
		elif op == 'store':
			return [('emit', ('store', dag[1], subgen(dag[2]), subgen(dag[3])))]
		elif op == 'copfun':
			return [('emit', ('copfun', subgen(dag[1]), subgen(dag[2]), subgen(dag[3])))]
		elif op == 'cast':
			return [('cast', dag[1], subgen(dag[2]))]
		elif op == 'mul_delay':
			return [('emit', ('mul_delay', subgen(dag[1]), subgen(dag[2]), subgen(dag[3])))]
		elif op == 'div_delay':
			return [('emit', ('div_delay',))]
		elif op == 'absorb_muldiv_delay':
			return [('GenAbsorbMuldivDelay', )]
		elif op == 'do_load':
			return [('emit', ('do_load', subgen(dag[1]), dag[2]))]
		else:
			print 'Unknown op:', op
			return []

	code += cleansexp(subgen(dag))
	if has_branch[0]:
		code.append(('=', 'branched', 'true'))
	if no_delay[0]:
		code.append(('=', 'no_delay', 'true'))
	if has_load[0]:
		code.append(('=', 'has_load', 'true'))
	code.append(('return', 'true'))

	return code


def build():
	print 'Rebuilding from tables'
	with file('SharpStation/InterpreterGenerated.cs', 'w') as fp:
		print >>fp, '/* Autogenerated from insts.td. DO NOT EDIT */'
		data = file('GeneratorStubs/InterpStub.cs', 'r').read().decode('utf-8').lstrip(u'\ufeff')
		data = data.replace('\n\t\t\t\t/*<<GENERATED>>*/\n', indent(output(generate(genInterp)), count=4))
		print >>fp, data.encode('utf-8')
	with file('SharpStation/DisassemblerGenerated.cs', 'w') as fp:
		print >>fp, '/* Autogenerated from insts.td. DO NOT EDIT */'
		data = file('GeneratorStubs/DisasmStub.cs', 'r').read().decode('utf-8').lstrip(u'\ufeff')
		data = data.replace('\n\t\t\t/*<<GENERATED>>*/\n', indent(output(generate(genDisasm)), count=3))
		print >>fp, data.encode('utf-8')
	with file('SharpStation/RecompilerGenerated.cs', 'w') as fp:
		print >>fp, '/* Autogenerated from insts.td. DO NOT EDIT */'
		data = file('GeneratorStubs/RecompilerStub.cs', 'r').read().decode('utf-8').lstrip(u'\ufeff')
		data = data.replace('\n\t\t\t/*<<GENERATED>>*/\n', indent(output(generate(genRecomp)), count=4))
		print >>fp, data.encode('utf-8')


if __name__ == '__main__':
	build()