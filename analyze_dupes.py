import re, os

editor_dir = r'Assets\CueStrike\Editor'
menu_items = {}

for root, dirs, files in os.walk(editor_dir):
    for f in files:
        if f.endswith('.cs'):
            fp = os.path.join(root, f)
            with open(fp, encoding='utf-8', errors='ignore') as fh:
                content = fh.read()
            for m in re.finditer(r'\[MenuItem\(("[^"]+")', content):
                path = m.group(1)
                rel = os.path.relpath(fp, editor_dir)
                menu_items.setdefault(path, []).append(rel)

print("=== ALL MENUITEMS ===")
for path, files in sorted(menu_items.items()):
    uniq = list(set(files))
    print(f'  {path}')
    for f in uniq:
        print(f'    [{files.count(f)}x] {f}')

print("\n=== DUPLICATES ===")
found = 0
for path, files in sorted(menu_items.items()):
    uniq = list(set(files))
    if len(uniq) > 1 or len(files) > len(uniq):
        found += 1
        print(f'ISSUE: {path}')
        for f in set(files):
            cnt = files.count(f)
            print(f'  [{cnt}x] {f}')
if found == 0:
    print('No duplicates')
else:
    print(f'{found} issues')