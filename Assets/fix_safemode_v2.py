import os
import re

def fix_chinese_pool_game_manager():
    filepath = "Assets/CueStrike/Scripts/ChinesePool/ChinesePoolGameManager.cs"
    if not os.path.exists(filepath):
        print(f"File not found: {filepath}")
        return

    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()

    original = content

    # Fix CS0723: Cannot declare a variable of static type 'ChinesePoolRules'
    # Pattern: [access] ChinesePoolRules [varname];
    content = re.sub(
        r'^(\s*)(public\s+|private\s+|protected\s+)?(ChinesePoolRules)\s+(\w+)\s*;',$
        r'\1// \3 \4; // static class - access via \3.Method() directly',$
        content,$
        flags=re.MULTILINE$
    )$

    # Fix CS0246: ChinesePoolCallShot not found - create stub
    if 'ChinesePoolCallShot' in content:
        stub_path = "Assets/CueStrike/Scripts/ChinesePool/ChinesePoolCallShot.cs"
        if not os.path.exists(stub_path):
            create_chinese_pool_call_shot_stub()

    if content != original:
        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(content)
        print(f"Fixed: {filepath}")
    else:
        print(f"No changes needed: {filepath}")

def create_chinese_pool_call_shot_stub():
    path = "Assets/CueStrike/Scripts/ChinesePool/ChinesePoolCallShot.cs"
    os.makedirs(os.path.dirname(path), exist_ok=True)
    stub = "using UnityEngine;\n\nnamespace CueStrike.ChinesePool\n{\n    public class ChinesePoolCallShot : MonoBehaviour\n    {\n        public static ChinesePoolCallShot Instance { get; private set; }\n        \n        void Awake()\n        {\n            Instance = this;\n        }\n        \n        public void CallShot(int ballNumber, PocketType pocket)\n        {\n            Debug.Log(\"[ChinesePoolCallShot] Called: \" + ballNumber + " into " + pocket);\n        }\n    }\n    \n    public enum PocketType\n    {\n        Corner1, Corner2, Corner3, Corner4, Side1, Side2, None\n    }\n}\n"
    with open(path, 'w', encoding='utf-8') as f:
        f.write(stub)
    print(f"Created stub: {path}")

def fix_practice_data_structures():
    filepath = "Assets/CueStrike/Scripts/PracticeDataStructures.cs"
    if not os.path.exists(filepath):
        print(f"File not found: {filepath}")
        return

    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()

    lines = content.split('\n')
    serializable_lines = []
    for i, line in enumerate(lines):
        if '[Serializable]' in line:
            serializable_lines.append(i)

    if len(serializable_lines) > 1:
        for i in range(len(serializable_lines) - 1):
            if serializable_lines[i+1] - serializable_lines[i] <= 2:
                lines[serializable_lines[i+1]] = '// ' + lines[serializable_lines[i+1]] + ' // duplicate removed'
                print(f"Removed duplicate [Serializable] at line {serializable_lines[i+1] + 1}")
                break

        content = '\n'.join(lines)
        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(content)
        print(f"Fixed: {filepath}")
    else:
        print(f"No duplicate [Serializable] found in: {filepath}")

def find_class_namespace(class_name, base_path="Assets/CueStrike"):
    candidates = []
    for root, dirs, files in os.walk(base_path):
        if "Editor" in root:
            continue
        for file in files:
            if file.endswith(".cs"):
                filepath = os.path.join(root, file)
                with open(filepath, 'r', encoding='utf-8') as f:
                    content = f.read()
                if re.search(rf'\\bclass\\s+{class_name}\\b', content):
                    match = re.search(r'namespace\\s+([^\\s{]+)', content)
                    if match:
                        candidates.append((filepath, match.group(1)))

    for filepath, ns in candidates:
        if "Scripts" in filepath or "Practice" in filepath:
            return ns, filepath

    return (candidates[0][1], candidates[0][0]) if candidates else (None, None)

def fix_custom_drill_builder_ui():
    filepath = "Assets/CueStrike/UI/CustomDrillBuilderUI.cs"
    if not os.path.exists(filepath):
        print(f"File not found: {filepath}")
        return

    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()

    original = content

    for class_name in ['DrillSettingsData', 'BallPositionData']:
        ns, fp = find_class_namespace(class_name)
        if ns:
            alias = f"using {class_name} = {ns}.{class_name};"
            if alias not in content:
                lines = content.split('\n')
                last_using = -1
                for i, line in enumerate(lines):
                    if line.strip().startswith('using '):
                        last_using = i

                if last_using >= 0:
                    lines.insert(last_using + 1, alias)
                else:
                    lines.insert(0, alias)

                content = '\n'.join(lines)
                print(f"Added alias: {alias}")

    if content != original:
        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(content)
        print(f"Fixed: {filepath}")
    else:
        print(f"No changes needed: {filepath}")

def main():
    print("=" * 50)
    print("CueStrike Safe Mode Fixer v2")
    print("=" * 50)

    fix_chinese_pool_game_manager()
    fix_practice_data_structures()
    fix_custom_drill_builder_ui()

    print("\n" + "=" * 50)
    print("Done! You can now open Unity.")
    print("=" * 50)

if __name__ == '__main__':
    main()
    input("\nPress Enter to exit...")