import os
import re

bad_guid = []
trailing_ws = []
null_byte = []
errors = []

for dp, dn, fn in os.walk('Assets'):
    for f in fn:
        if not f.endswith('.meta'):
            continue
        p = os.path.join(dp, f)
        try:
            with open(p, encoding='utf-8', errors='replace') as fh:
                t = fh.read()
        except Exception as e:
            errors.append((p, str(e)))
            continue

        m = re.search(r'^guid:\s*(\S+)', t, re.M)
        if m and not re.fullmatch(r'[0-9a-fA-F]{32}', m.group(1)):
            bad_guid.append((p, m.group(1)))

        if any(line.endswith((' ', '\t')) for line in t.splitlines()):
            trailing_ws.append(p)

        with open(p, 'rb') as fh:
            raw = fh.read()
        if b'\x00' in raw:
            null_byte.append(p)

print('BAD GUID COUNT:', len(bad_guid))
for p, g in bad_guid:
    print(' GUID:', g, '=>', p)

print('TRAILING WS COUNT:', len(trailing_ws))
for p in trailing_ws:
    print(' WS:', p)

print('NULL BYTE COUNT:', len(null_byte))
for p in null_byte:
    print(' NULL:', p)

print('ERRORS:', len(errors))
for p, e in errors:
    print(' ERR:', p, e)