#!/usr/bin/env bash
# infra.sh <từ khóa> — tra nhanh SỔ HẠ TẦNG (memory/local/infra-registry.md)
# Không tham số → liệt kê các mục. Có từ khóa → grep (không phân biệt hoa thường), in kèm số dòng.
set -u
HERE="$(cd "$(dirname "$0")" && pwd)"
REG="$HERE/../../memory/local/infra-registry.md"

if [ ! -f "$REG" ]; then
  echo "❌ Chưa có sổ hạ tầng: $REG"
  echo "   → Tạo/quét bằng skill 'quan-ly-thong-tin-project' (mục VERIFY/QUÉT LẠI)."
  exit 1
fi

if [ "$#" -eq 0 ]; then
  echo "📖 SỔ HẠ TẦNG — các mục (dùng: infra.sh <từ khóa>):"
  grep -nE "^#{1,3} " "$REG"
  exit 0
fi

KW="$*"
echo "🔎 '$KW' trong sổ hạ tầng:"
if grep -inE "$KW" "$REG"; then :; else
  echo "   (không thấy — thử từ khóa khác, vd IP '10.0.63', tên service 'Invoice', 'Redis', 'Portainer')"
fi
