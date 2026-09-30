#!/bin/bash
# ─────────────────────────────────────────────────────────────────
# Tải ONNX embedding model về backend/AgentService/Models/
#
# Model: paraphrase-multilingual-MiniLM-L12-v2 (ONNX quantized, ~120MB)
# Hỗ trợ tiếng Việt + tiếng Anh, dim = 384
#
# Chạy 1 lần sau khi clone repo. Sau đó build Docker sẽ tự copy vào image.
#
# Usage:
#   bash scripts/download-embedding-model.sh
# ─────────────────────────────────────────────────────────────────

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
DEST="$ROOT/backend/AgentService/Models"

mkdir -p "$DEST"

# HuggingFace mirror của sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2 (ONNX)
BASE="https://huggingface.co/sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2/resolve/main"
ONNX_BASE="https://huggingface.co/Xenova/paraphrase-multilingual-MiniLM-L12-v2/resolve/main"

echo "→ Downloading model.onnx (~120MB) ..."
curl -L -o "$DEST/model.onnx" "$ONNX_BASE/onnx/model_quantized.onnx"

echo "→ Downloading vocab.txt ..."
curl -L -o "$DEST/vocab.txt" "$BASE/vocab.txt"

echo "→ Downloading tokenizer.json (optional, for reference) ..."
curl -L -o "$DEST/tokenizer.json" "$BASE/tokenizer.json" || true

echo ""
echo "Done. Files saved to: $DEST"
ls -lh "$DEST"
echo ""
echo "Build + deploy AgentService, model sẽ được copy vào container."
