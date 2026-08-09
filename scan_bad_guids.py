import os
import re

bad = []
for dp, dn, fn in os.walk('Assets'):
    for f in fn:
        if f.endswith('.meta'):
            p = os.path.join(dp, f)
            try:
                with open(p, encoding='utf-8', errors='replace') as fh:
                    t = fh.read()
            except Exception:
                continue
            m = re.search(r'^guid:\s*(\S+)', t, re.M)
            if m and not re.fullmatch(r'[0-9a-fA-F]{32}', m.group(1)):
                bad.append((p, m.group(1)))

print('BAD GUID COUNT:', len(bad))
for p, g in bad:
    print(g, '=>', p)