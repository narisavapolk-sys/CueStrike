#!/usr/bin/env bash
# =============================================================================
# CueStrike Unity compile gate — batchmode compile check
#
# Exit codes:
#   0 = compile clean (no "error CS" in log)
#   1 = compile errors found (or Unity exited abnormally)
#   2 = Unity Editor not found
#
# Usage:
#   tools/compile_check.sh
#   UNITY_PATH="/path/to/Unity.exe" tools/compile_check.sh
#
# Log: compile_gate.log (already gitignored via *.log)
# =============================================================================
set -u

PROJECT_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
LOG_FILE="${COMPILE_GATE_LOG:-$PROJECT_ROOT/compile_gate.log}"

# ---- 1. Locate Unity Editor -------------------------------------------------
UNITY_PATH="${UNITY_PATH:-}"
if [ -z "$UNITY_PATH" ]; then
  HUB_DIR="/c/Program Files/Unity/Hub/Editor"
  if [ -d "$HUB_DIR" ]; then
    if [ -x "$HUB_DIR/6000.4.4f1/Editor/Unity.exe" ]; then
      UNITY_PATH="$HUB_DIR/6000.4.4f1/Editor/Unity.exe"
    else
      UNITY_PATH="$(ls -d "$HUB_DIR"/*/Editor/Unity.exe 2>/dev/null | sort -V | tail -1)"
    fi
  fi
fi

if [ -z "$UNITY_PATH" ] || [ ! -f "$UNITY_PATH" ]; then
  echo "❌ [compile-gate] ไม่พบ Unity Editor — ตั้งตัวแปร UNITY_PATH หรือติดตั้งผ่าน Unity Hub" >&2
  exit 2
fi

echo "🔍 [compile-gate] Unity: $UNITY_PATH"
echo "🔍 [compile-gate] Project: $PROJECT_ROOT"

# ---- 2. Convert paths for Unity (Windows style) -----------------------------
win_project="$(cygpath -w "$PROJECT_ROOT" 2>/dev/null || echo "$PROJECT_ROOT")"
win_log="$(cygpath -w "$LOG_FILE" 2>/dev/null || echo "$LOG_FILE")"

# ---- 3. Run batchmode compile ----------------------------------------------
rm -f "$LOG_FILE"
"$UNITY_PATH" -batchmode -quit -nographics \
  -projectPath "$win_project" \
  -logFile "$win_log" \
  >/dev/null 2>&1
UNITY_EXIT=$?

# ---- 4. Parse result ---------------------------------------------------------
ERRORS="$(grep -c "error CS" "$LOG_FILE" 2>/dev/null || true)"
ERRORS="${ERRORS:-0}"

if [ "${ERRORS:-0}" -gt 0 ]; then
  echo "❌ [compile-gate] พบ ${ERRORS} compile error(s):" >&2
  grep "error CS" "$LOG_FILE" | head -10 >&2
  echo "   ดู log เต็ม: $LOG_FILE" >&2
  exit 1
fi

if [ "$UNITY_EXIT" -ne 0 ]; then
  echo "⚠️  [compile-gate] Unity ออกจากด้วย code ${UNITY_EXIT} (ไม่พบ 'error CS' — ดู log: $LOG_FILE)" >&2
  exit 1
fi

echo "✅ [compile-gate] 0 errors — compile ผ่าน"
exit 0
