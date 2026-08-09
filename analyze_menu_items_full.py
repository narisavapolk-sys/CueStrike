import re, os, glob

# Scan ALL .cs files under Assets (not just Editor)
menu_items = {}

for f in glob.glob(r'Assets/**/*.cs', recursive=True):
    try:
        with open(f, encoding='utf-8-sig') as fh:
            content = fh.read()
    except Exception:
        continue
    for m in re.finditer(r'\[(?:UnityEditor\.)?MenuItem\("([^"]+)"', content):
        path = m.group(1)
        if path not in menu_items:
            menu_items[path] = []
        menu_items[path].append(f)

found = 0
for path, files in sorted(menu_items.items()):
    if len(files) > 1:
        found += 1
        print(f'DUPLICATE ({len(files)}x): "{path}"')
        for f in files:
            print(f'  <- {f}')
if found == 0:
    print('No duplicate MenuItem paths found in entire project')
else:
    print(f'{found} duplicate(s) found')