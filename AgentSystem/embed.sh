#!/bin/bash
# Embedding CLI — semantic code search, complement CodeMap
# Dùng khi BIẾT Ý NGHĨA nhưng KHÔNG BIẾT TÊN class/method.
# Với tên cụ thể, dùng ./codemap.sh thay thế (nhanh + chính xác hơn).
#
# Usage:
#   ./embed.sh search "<query tiếng Việt/English>" [limit]
#   ./embed.sh index <rootPath>              Full re-index
#   ./embed.sh update <rootPath>             Incremental index
#   ./embed.sh progress                      Index progress
#   ./embed.sh clear                         Clear all embeddings
#
# Ví dụ:
#   ./embed.sh search "chỗ xử lý thanh toán đơn hàng"
#   ./embed.sh search "logic retry RabbitMQ" 5
#   ./embed.sh index "E:/SandboxVN/SCRMCode"

API="${CODEMAP_API:-http://10.0.161.21:50888}"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
CACHE_DIR="$SCRIPT_DIR/.codemap-cache"
CACHE_TOKEN="$CACHE_DIR/token"
PROJECT_MAP="$SCRIPT_DIR/codemap-projects.json"
SHELL_PID=$(ps -o ppid= $$ 2>/dev/null | tr -d ' ' || echo $$)
CACHE_PROJECT="$CACHE_DIR/project_$SHELL_PID"

mkdir -p "$CACHE_DIR"

# Tái dùng logic auth + project của codemap.sh
get_token() {
  if [ -f "$CACHE_TOKEN" ]; then
    local age=$(( $(date +%s) - $(date -r "$CACHE_TOKEN" +%s 2>/dev/null || echo 0) ))
    if [ "$age" -lt 3600 ]; then cat "$CACHE_TOKEN"; return; fi
  fi
  local user="${CODEMAP_USER:-admin}"
  local pass="${CODEMAP_PASS:-Admin@123}"
  local resp=$(curl -s "$API/api/auth/login" -X POST -H "Content-Type: application/json" \
    -d "{\"username\":\"$user\",\"password\":\"$pass\"}")
  local token=$(echo "$resp" | node -e "let d='';process.stdin.on('data',c=>d+=c);process.stdin.on('end',()=>{try{console.log(JSON.parse(d).token||'')}catch(e){console.log('')}})")
  if [ -n "$token" ]; then echo "$token" > "$CACHE_TOKEN"; echo "$token"
  else echo "ERROR: Login failed at $API" >&2; exit 1; fi
}

get_project_id() {
  [ -n "$CODEMAP_PROJECT_ID" ] && { echo "$CODEMAP_PROJECT_ID"; return; }
  [ -f "$CACHE_PROJECT" ] && { cat "$CACHE_PROJECT"; return; }
  if [ -f "$PROJECT_MAP" ]; then
    local pid=$(node -e "try{const d=JSON.parse(require('fs').readFileSync(process.argv[1],'utf8'));const v=Object.values(d);if(v.length===1)console.log(v[0])}catch(e){}" "$PROJECT_MAP" 2>/dev/null)
    [ -n "$pid" ] && { echo "$pid"; return; }
  fi
  echo "ERROR: No project selected. Run ./codemap.sh use <name> first." >&2
  exit 1
}

check_backend() {
  local status=$(curl -s -o /dev/null -w "%{http_code}" --connect-timeout 3 "$API/swagger/index.html" 2>/dev/null)
  if [ "$status" != "200" ]; then echo "ERROR: Backend không phản hồi tại $API" >&2; exit 1; fi
}

pretty() {
  node -e "let d='';process.stdin.on('data',c=>d+=c);process.stdin.on('end',()=>{try{console.log(JSON.stringify(JSON.parse(d),null,2))}catch(e){process.stdout.write(d)}})"
}

check_backend
TOKEN=$(get_token)
PROJECT_ID=$(get_project_id)

fetch() { curl -s "$API$1" -H "Authorization: Bearer $TOKEN"; }

case "$1" in
  search|s)
    if [ -z "$2" ]; then echo "Usage: ./embed.sh search \"<query>\" [limit]"; exit 1; fi
    QUERY=$(node -e "console.log(encodeURIComponent(process.argv[1]))" "$2")
    fetch "/api/embedding/$PROJECT_ID/search?q=$QUERY&limit=${3:-10}" | pretty
    ;;
  index)
    if [ -z "$2" ]; then echo "Usage: ./embed.sh index <rootPath>"; exit 1; fi
    echo "Full re-index $2 (xóa embeddings cũ, làm lại từ đầu)..."
    curl -s "$API/api/embedding/$PROJECT_ID/index" -X POST \
      -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
      -d "{\"rootPath\":\"$2\"}" | pretty
    ;;
  update|u)
    if [ -z "$2" ]; then echo "Usage: ./embed.sh update <rootPath>"; exit 1; fi
    echo "Incremental index $2 (chỉ embed chunks mới)..."
    curl -s "$API/api/embedding/$PROJECT_ID/index/incremental" -X POST \
      -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
      -d "{\"rootPath\":\"$2\"}" | pretty
    ;;
  progress)
    fetch "/api/embedding/$PROJECT_ID/progress" | pretty
    ;;
  clear)
    read -p "Xóa toàn bộ embeddings của project? (y/N) " c
    [ "$c" = "y" ] && curl -s -X DELETE "$API/api/embedding/$PROJECT_ID" -H "Authorization: Bearer $TOKEN" | pretty
    ;;
  *)
    cat <<EOF
Embedding CLI — semantic code search (bổ sung CodeMap)

Query:
  ./embed.sh search "<query>" [limit]      Semantic search (tiếng Việt/Anh)

Admin:
  ./embed.sh index <rootPath>              Full re-index
  ./embed.sh update <rootPath>             Incremental
  ./embed.sh progress                      Xem tiến độ index
  ./embed.sh clear                         Clear embeddings

Khi nào dùng embed vs codemap?
  - Biết tên class/method/endpoint    → ./codemap.sh (chính xác, nhanh)
  - Biết ý nghĩa, không biết tên      → ./embed.sh   (semantic)

Env vars:
  CODEMAP_API, CODEMAP_USER, CODEMAP_PASS, CODEMAP_PROJECT_ID (tái dùng từ codemap.sh)
EOF
    ;;
esac
