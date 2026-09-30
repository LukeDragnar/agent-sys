#!/usr/bin/env bash
# group-status.sh — Bản đồ RẢNH/BẬN các folder dev _SourceForCode/Group*
# Dùng: bash scripts/group-status.sh   (chạy ở đâu cũng được — BASE cố định bên dưới)
# Output: mỗi Group -> repo | branch | số file chưa commit | commit cuối; kết luận rảnh/bận.
# RẢNH = mọi repo clean (0 dirty). BẬN = có repo còn code chưa commit (WIP).

BASE="${GROUP_BASE:-D:/PROJECT/Git/_SourceForCode}"

for G in "$BASE"/Group*/; do
  [ -d "$G" ] || continue
  GN=$(basename "$G")
  busy=0; repos=""
  out=""
  for D in "$G"*/; do
    [ -d "$D/.git" ] || continue
    RN=$(basename "$D")
    BR=$(git -C "$D" rev-parse --abbrev-ref HEAD 2>/dev/null)
    N=$(git -C "$D" status --short 2>/dev/null | wc -l | tr -d ' ')
    LAST=$(git -C "$D" log -1 --pretty=format:"%ad %s" --date=format:"%d/%m" 2>/dev/null | cut -c1-50)
    [ "$N" -gt 0 ] && busy=1
    repos="$repos $RN"
    flag=""; [ "$N" -gt 0 ] && flag=" ⚠️WIP"
    out="$out  $(printf '%-16s [%-32s] dirty=%s%s\n      %s\n' "$RN" "$BR" "$N" "$flag" "$LAST")\n"
  done
  [ -z "$repos" ] && continue
  if [ "$busy" -eq 1 ]; then STATE="🔴 BẬN (còn WIP chưa commit)"; else STATE="🟢 RẢNH (clean)"; fi
  echo "══════════ $GN — $STATE ══════════"
  echo "  path: $G"
  echo "  repo:$repos"
  printf "$out"
  echo ""
done
echo "Ghi chú: 🟢 mọi repo clean = sẵn sàng task mới · 🔴 có repo dirty = đang WIP, đừng dọn/đụng."
