#!/bin/bash
# CodeMap CLI — query source map from terminal
# Usage:
#   ./codemap.sh search <keyword>
#   ./codemap.sh symbol <name> [type]
#   ./codemap.sh endpoint [route] [method]
#   ./codemap.sh deps <class-name>
#   ./codemap.sh stats
#   ./codemap.sh scan <rootPath>          Re-scan project
#   ./codemap.sh projects                 List all projects
#   ./codemap.sh use <project-name>       Switch project
#
# Setup cho member mới:
#   1. Backend đã chạy tại http://10.0.161.21:50888 (không cần run local)
#   2. Tạo project trên web: http://10.0.161.21:50888/swagger
#   3. Scan: ./codemap.sh scan "E:\SandboxVN\SCRMCode"
#   4. Dùng: ./codemap.sh search OrderService

API="${CODEMAP_API:-http://10.0.161.21:50888}"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
CACHE_DIR="$SCRIPT_DIR/.codemap-cache"
CACHE_TOKEN="$CACHE_DIR/token"
# Project mapping file (committed to git, shared across team)
PROJECT_MAP="$SCRIPT_DIR/codemap-projects.json"
# Per-terminal session cache
SHELL_PID=$(ps -o ppid= $$ 2>/dev/null | tr -d ' ' || echo $$)
CACHE_PROJECT="$CACHE_DIR/project_$SHELL_PID"

mkdir -p "$CACHE_DIR"

# ── JSON helpers dùng node (không cần python) ──
json_get() {
  # json_get <json_string> <field>
  echo "$1" | node -e "process.stdin.resume();let d='';process.stdin.on('data',c=>d+=c);process.stdin.on('end',()=>{try{const o=JSON.parse(d);console.log(o['$2']||'');}catch(e){}})"
}

json_parse() {
  node -e "$1"
}

# ── Auth (cache token) ──
get_token() {
  if [ -f "$CACHE_TOKEN" ]; then
    local age=$(( $(date +%s) - $(date -r "$CACHE_TOKEN" +%s 2>/dev/null || echo 0) ))
    if [ "$age" -lt 3600 ]; then
      cat "$CACHE_TOKEN"
      return
    fi
  fi

  local user="${CODEMAP_USER:-admin}"
  local pass="${CODEMAP_PASS:-Admin@123}"
  local resp=$(curl -s "$API/api/auth/login" -X POST -H "Content-Type: application/json" \
    -d "{\"username\":\"$user\",\"password\":\"$pass\"}")
  local token=$(echo "$resp" | node -e "let d='';process.stdin.on('data',c=>d+=c);process.stdin.on('end',()=>{try{console.log(JSON.parse(d).token||'');}catch(e){console.log('')}})")

  if [ -n "$token" ] && [ "$token" != "" ]; then
    echo "$token" > "$CACHE_TOKEN"
    echo "$token"
  else
    echo "ERROR: Login failed. Is backend running at $API?" >&2
    exit 1
  fi
}

# ── Project ID (auto-detect or cached) ──
get_project_id() {
  # 1. From env
  if [ -n "$CODEMAP_PROJECT_ID" ]; then
    echo "$CODEMAP_PROJECT_ID"
    return
  fi

  # 2. From per-terminal cache
  if [ -f "$CACHE_PROJECT" ]; then
    cat "$CACHE_PROJECT"
    return
  fi

  # 3. From codemap-projects.json (shared, git-committed)
  if [ -f "$PROJECT_MAP" ]; then
    local count=$(node -e "try{const d=JSON.parse(require('fs').readFileSync(process.argv[1],'utf8'));console.log(Object.keys(d).length)}catch(e){console.log(0)}" "$PROJECT_MAP" 2>/dev/null)
    if [ "$count" = "1" ]; then
      local pid=$(node -e "try{const d=JSON.parse(require('fs').readFileSync(process.argv[1],'utf8'));console.log(Object.values(d)[0])}catch(e){}" "$PROJECT_MAP" 2>/dev/null)
      echo "$pid" > "$CACHE_PROJECT"
      echo "$pid"
      return
    fi
  fi

  # 4. Ask user to pick
  local token=$(get_token)
  local projects=$(curl -s "$API/api/projects" -H "Authorization: Bearer $token")
  local count=$(echo "$projects" | node -e "let d='';process.stdin.on('data',c=>d+=c);process.stdin.on('end',()=>{try{console.log(JSON.parse(d).length)}catch(e){console.log(0)}})")

  if [ "$count" = "0" ] || [ -z "$count" ]; then
    echo "ERROR: No projects found. Create one first on web UI." >&2
    exit 1
  fi

  echo "" >&2
  echo "=== Chon project ===" >&2
  echo "$projects" | node -e "
let d='';process.stdin.on('data',c=>d+=c);process.stdin.on('end',()=>{
  const list=JSON.parse(d);
  list.forEach((p,i)=>{
    const s=p.lastScannedAt?p.lastScannedAt.slice(0,16):'chua scan';
    console.error((i+1)+'. '+p.name.padEnd(20)+' (scanned: '+s+')');
  });
})" 2>&1 >&2

  read -p "Nhap so: " choice </dev/tty
  local result=$(echo "$projects" | node -e "
let d='';process.stdin.on('data',c=>d+=c);process.stdin.on('end',()=>{
  const list=JSON.parse(d);
  const i=parseInt('${choice}')-1;
  if(i>=0&&i<list.length) console.log(list[i].id+'|'+list[i].name);
  else console.log('');
})")

  local pid=$(echo "$result" | cut -d'|' -f1)
  local pname=$(echo "$result" | cut -d'|' -f2)

  if [ -n "$pid" ]; then
    echo "$pid" > "$CACHE_PROJECT"
    # Save to shared project map
    node -e "
const fs=require('fs');
const f='$PROJECT_MAP';
const d=fs.existsSync(f)?JSON.parse(fs.readFileSync(f,'utf8')):{};
d['$pname'.toLowerCase()]='$pid';
fs.writeFileSync(f,JSON.stringify(d,null,2));
" 2>/dev/null
    echo "-> $pname" >&2
    echo "$pid"
  else
    echo "ERROR: Invalid choice." >&2
    exit 1
  fi
}

# ── Check backend alive ──
check_backend() {
  local status=$(curl -s -o /dev/null -w "%{http_code}" --connect-timeout 3 "$API/swagger/index.html" 2>/dev/null)
  if [ "$status" != "200" ]; then
    echo "ERROR: Backend không phản hồi tại $API" >&2
    exit 1
  fi
}

# ── Pretty print JSON ──
pretty() {
  node -e "let d='';process.stdin.on('data',c=>d+=c);process.stdin.on('end',()=>{try{console.log(JSON.stringify(JSON.parse(d),null,2))}catch(e){process.stdout.write(d)}})"
}

check_backend
TOKEN=$(get_token)
PROJECT_ID=$(get_project_id)

fetch() {
  curl -s "$API$1" -H "Authorization: Bearer $TOKEN"
}

case "$1" in
  search|s)
    fetch "/api/codemap/$PROJECT_ID/search?q=$2&limit=${3:-10}" | pretty
    ;;
  symbol|sym)
    URL="/api/codemap/$PROJECT_ID/symbols?name=$2"
    [ -n "$3" ] && URL="$URL&type=$3"
    fetch "$URL" | pretty
    ;;
  endpoint|ep)
    URL="/api/codemap/$PROJECT_ID/endpoints?"
    [ -n "$2" ] && URL="${URL}route=$2&"
    [ -n "$3" ] && URL="${URL}method=$3"
    fetch "$URL" | pretty
    ;;
  deps|d)
    fetch "/api/codemap/$PROJECT_ID/dependencies?symbol=$2&depth=${3:-2}" | pretty
    ;;
  stats)
    fetch "/api/codemap/$PROJECT_ID/stats" | pretty
    ;;
  scan)
    if [ -z "$2" ]; then echo "Usage: ./codemap.sh scan <rootPath>"; exit 1; fi
    echo "Full scan $2 (xoa data cu, scan lai tu dau)..."
    curl -s "$API/api/codemap/$PROJECT_ID/scan" -X POST \
      -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
      -d "{\"rootPath\":\"$2\"}" | pretty
    ;;
  update|u)
    if [ -z "$2" ]; then echo "Usage: ./codemap.sh update <rootPath>"; exit 1; fi
    echo "Incremental scan $2 (chi scan files thay doi)..."
    curl -s "$API/api/codemap/$PROJECT_ID/scan/incremental" -X POST \
      -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
      -d "{\"rootPath\":\"$2\"}" | pretty
    ;;
  add)
    if [ -z "$2" ]; then echo "Usage: ./codemap.sh add <rootPath>"; exit 1; fi
    echo "Adding path $2 (them vao data hien tai, khong xoa)..."
    curl -s "$API/api/codemap/$PROJECT_ID/scan/add-path" -X POST \
      -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
      -d "{\"rootPath\":\"$2\"}" | pretty
    ;;
  projects|ls)
    fetch "/api/projects" | node -e "
let d='';process.stdin.on('data',c=>d+=c);process.stdin.on('end',()=>{
  JSON.parse(d).forEach(p=>{
    const s=p.lastScannedAt?p.lastScannedAt.slice(0,16):'never';
    console.log(p.id+'  '+p.name.padEnd(20)+' scanned: '+s);
  });
})"
    ;;
  use)
    if [ -z "$2" ]; then echo "Usage: ./codemap.sh use <project-name>"; exit 1; fi
    result=$(fetch "/api/projects" | node -e "
let d='';process.stdin.on('data',c=>d+=c);process.stdin.on('end',()=>{
  const match=JSON.parse(d).find(p=>p.name.toLowerCase().includes('$2'.toLowerCase()));
  if(match) console.log(match.id+'|'+match.name);
})")
    local pid=$(echo "$result" | cut -d'|' -f1)
    local pname=$(echo "$result" | cut -d'|' -f2)
    if [ -n "$pid" ]; then
      echo "$pid" > "$CACHE_PROJECT"
      node -e "
const fs=require('fs');
const f='$PROJECT_MAP';
const d=fs.existsSync(f)?JSON.parse(fs.readFileSync(f,'utf8')):{};
d['$2'.toLowerCase()]='$pid';
fs.writeFileSync(f,JSON.stringify(d,null,2));
" 2>/dev/null
      echo "Switched to project: $pname ($pid)"
    else
      echo "Project '$2' not found"
    fi
    ;;
  *)
    echo "CodeMap CLI"
    echo ""
    echo "Query:"
    echo "  ./codemap.sh search <keyword> [limit]   Search symbols"
    echo "  ./codemap.sh symbol <name> [type]        Find class/method detail"
    echo "  ./codemap.sh endpoint [route] [method]   Find API endpoints"
    echo "  ./codemap.sh deps <ClassName> [depth]    Dependency tree"
    echo "  ./codemap.sh stats                       Project stats"
    echo ""
    echo "Admin:"
    echo "  ./codemap.sh scan <rootPath>             Full scan (xoa cu, scan lai)"
    echo "  ./codemap.sh update <rootPath>           Incremental (chi files thay doi)"
    echo "  ./codemap.sh add <rootPath>              Them path"
    echo "  ./codemap.sh projects                    List projects"
    echo "  ./codemap.sh use <project-name>          Switch active project"
    echo ""
    echo "Env vars:"
    echo "  CODEMAP_API          API URL (default: http://10.0.161.21:50888)"
    echo "  CODEMAP_USER         Login username (default: admin)"
    echo "  CODEMAP_PASS         Login password (default: Admin@123)"
    echo "  CODEMAP_PROJECT_ID   Override project ID"
    ;;
esac
