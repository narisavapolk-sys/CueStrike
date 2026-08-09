# Normcore SDK Installation Guide
> **Project:** CueStrike VR
> **Goal:** Enable Multiplayer

---

## Step 1: Install Normcore Package

### Option A: Package Manager (Recommended)
1. Open Unity → Window → Package Manager
2. Click `+` → Add package from git URL
3. Enter: `https://github.com/normalvr/normal.unity.git`
4. Wait for installation

### Option B: .unitypackage
1. Download from https://normcore.io
2. Assets → Import Package → Custom Package
3. Select Normcore.unitypackage
4. Import all

---

## Step 2: Add Scripting Define

1. Edit → Project Settings → Player
2. Scroll to "Scripting Define Symbols"
3. Add: `CUESTRIKE_NORMCORE`
4. Click Apply
5. Unity will recompile

---

## Step 3: Uncomment Guarded Code

Find all files with `#if CUESTRIKE_NORMCORE`:
- `CueStrikeNormcoreManager.cs`
- `CueStrikeBallSync.cs`
- `CueStrikeGameSync.cs`

Remove the `#if` / `#endif` guards (keep the code inside).

---

## Step 4: Configure Normcore App Key

1. Go to https://normcore.io → Create Account
2. Create new app → Get App Key
3. In Unity: Assets → Create → Normal → Normcore App Settings
4. Paste App Key into the asset
5. Assign to `CueStrikeNormcoreManager`

---

## Step 5: Test Connection

1. Enter Play Mode
2. Check Console for Normcore connection logs
3. Open second Unity instance (or build)
4. Verify both instances see each other

---

## Troubleshooting

| Problem | Solution |
|---------|----------|
| `CS0246: Normcore type not found` | Package not installed correctly → reinstall |
| `App Key invalid` | Check key in Normcore dashboard |
| `Cannot connect` | Check firewall / internet connection |
| Compile errors after uncomment | Check if all Normcore types are available |

---

## Files to Modify

| File | Action |
|------|--------|
| `CueStrikeNormcoreManager.cs` | Remove `#if CUESTRIKE_NORMCORE` guards |
| `CueStrikeBallSync.cs` | Remove `#if CUESTRIKE_NORMCORE` guards |
| `CueStrikeGameSync.cs` | Remove `#if CUESTRIKE_NORMCORE` guards |
| Project Settings | Add `CUESTRIKE_NORMCORE` define |