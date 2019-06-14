curGroup = None
curTest = None

tests = {}

for line in file('log.txt'):
	if not line.startswith('Print:'):
		continue
	line = line[7:].strip()
	if 'Running' in line and line.endswith('test'):
		cur = line.split('Running ', 1)[1].split(' test', 1)[0]
		if curGroup is None:
			curGroup = cur.split('CPU ')[-1]
			tests[curGroup] = {}
		else:
			curTest = cur
			tests[curGroup][curTest] = dict(v=0, x=0)
	elif line == 'Done' or 'too many errors' in line:
		if curTest is None:
			curGroup = None
		else:
			curTest = None
	elif 'value error' in line:
		tests[curGroup][curTest]['v'] += 1
	elif 'exception error' in line:
		tests[curGroup][curTest]['x'] += 1

with file('tests.html', 'w') as fp:
	print >>fp, '''
<style>
.failure {
	color: red;
	font-weight: bold;
}
.success {
}
table {
	border-collapse: collapse;
	margin-top: 0;
}
h3 {
	margin-bottom: 2px;
}
td {
	padding: 3px;
	padding-left: 5px;
	padding-right: 5px;
}
.group {
	float: left;
	padding-right: 50px;
}
</style>
'''
	for group, tests in sorted(tests.items(), key=lambda x: x[0]):
		print >>fp, '<div class="group">'
		print >>fp, '<h3>%s<br>(%i/%i)</h3>' % (group, sum(0 if errors['v'] or errors['x'] else 1 for errors in tests.values()), len(tests))
		print >>fp, '<table border=1>'
		print >>fp, '<tr><th></th><th>V</th><th>X</th></tr>'
		for name, errors in sorted(tests.items(), key=lambda x: (0 if x[1]['v'] or x[1]['x'] else 1, x[0])):
			print >>fp, '<tr class="%s"><td>%s</td><td>%i</td><td>%i</td></tr>' % ('failure' if errors['v'] or errors['x'] else 'success', name, errors['v'], errors['x'])
		print >>fp, '</table>'
		print >>fp, '</div>'
