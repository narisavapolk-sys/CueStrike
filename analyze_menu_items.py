import re, os, glob

editor_dir = r'Assets\CueStrike\Editor'
menu_items = {}

for f in glob.glob(os.path.join(editor_dir, '*.cs')):
    with open(f, encoding='utf-8') as fh:
        content = fh.read()
    for m in re.finditer(r'\[MenuItem\(\"([^\"]+)\"', content):
        path = m.group(1)
        if path not in menu_items:
            menu_items[path] = []
        menu_items[path].append(os.path.basename(f))

found = 0
for path, files in sorted(menu_items.items()):
    if len(files) > 1:
        found += 1
        print(f'DUPLICATE ({len(files)}x): "{path}"')
        for f in files:
            print(f'  <- {f}')
if found == 0:
    print('No duplicate MenuItem paths found')
else:
    print(f'{found} duplicate(s) found')