import os

fixed = []
errors = []

for dp, dn, fn in os.walk('Assets'):
    for f in fn:
        if not f.endswith('.meta'):
            continue
        p = os.path.join(dp, f)
        try:
            with open(p, 'r', encoding='utf-8', errors='replace', newline='') as fh:
                text = fh.read()
            has_crlf = '\r\n' in text
            lines = text.splitlines()
            new_lines = []
            changed = False
            for line in lines:
                cleaned = line.rstrip(' \t')
                if cleaned != line:
                    changed = True
                new_lines.append(cleaned)
            if changed:
                nl = '\r\n' if has_crlf else '\n'
                with open(p, 'w', encoding='utf-8', newline='') as fh:
                    fh.write(nl.join(new_lines) + nl)
                fixed.append(p)
        except Exception as e:
            errors.append((p, str(e)))

print('FIXED COUNT:', len(fixed))
for p in fixed:
    print('fixed =>', p)
if errors:
    print('ERRORS:')
    for p, e in errors:
        print(p, '=>', e)