# Tools — Shared Development Tools for Team

Folder này chứa các **shared tools** cho toàn team dùng chung khi làm việc với SCRM codebase.

---

## 📚 Tools

### 1️⃣ Ripgrep — Fast Pattern Search

**Folder:** `ripgrep/`

Ripgrep là primary search tool cho codebase (10-100x nhanh hơn grep).

```bash
cd AgentSystem/
rg --config-path tools/ripgrep/.ripgreprc "pattern" --type ts
rg --config-path tools/ripgrep/.ripgreprc "this\.tsb\.t\(" --type ts -A 2
rg --config-path tools/ripgrep/.ripgreprc --files -g "*translate*"
```

**Docs:** `projects/scrm/RIPGREP.md`, `projects/scrm/RIPGREP_GUIDE.md`

---

### 2️⃣ CodeMap — Source Code Index & Query

**Folder:** `codemap/`

CodeMap scans C# backend code + creates searchable source map.

```bash
# From AgentSystem root
bash tools/codemap/codemap.sh search OrderService
bash tools/codemap/codemap.sh symbol ChiTieuService Class
bash tools/codemap/codemap.sh deps OrderService
bash tools/codemap/codemap.sh endpoint order GET
```

**Files:**
- `codemap.sh` — CLI tool
- `codemap-projects.json` — Project mapping (shared, committed)
- `.codemap-cache/` — Local cache (gitignored)

**Backend:** Running at `http://10.0.161.21:50888` (team shared instance)

---

### 3️⃣ MCP Server — AI Integration

**Folder:** `mcp/`

MCP (Model Context Protocol) server for Claude to query codemap + ripgrep programmatically.

**Setup:**
```bash
cd tools/mcp
npm install
node server.js
```

**Config:** `.mcp.json` — MCP server config

---

## 🚀 Quick Start

### For Developers

1. **Search code fast:**
   ```bash
   rg --config-path tools/ripgrep/.ripgreprc "FindInvoiceByNumber"
   ```

2. **Query symbol + dependencies:**
   ```bash
   bash tools/codemap/codemap.sh symbol OrderService
   bash tools/codemap/codemap.sh deps OrderService
   ```

3. **Find API endpoint:**
   ```bash
   bash tools/codemap/codemap.sh endpoint "order" POST
   ```

### For Team Setup (First Time)

1. **Setup CodeMap backend** (one time, team shared):
   ```
   Backend: http://10.0.161.21:50888
   Already running for SCRM (check Swagger UI)
   ```

2. **Add new project to CodeMap:**
   - Edit `tools/codemap/codemap-projects.json`
   - Add project entry with path + name
   - Run: `bash tools/codemap/codemap.sh scan "/path/to/project"`

3. **Setup MCP (optional, for Claude integration):**
   ```bash
   cd tools/mcp && npm install && node server.js
   ```

---

## 📖 References

| Tool | Docs | Location |
|------|------|----------|
| Ripgrep | RIPGREP.md, RIPGREP_GUIDE.md | projects/scrm/ |
| CodeMap | CLAUDE.md section | / |
| MCP | TODO: Build docs | tools/mcp/ |

---

## 🗂️ Folder Structure

```
tools/
├── README.md (this file)
├── ripgrep/
│   └── .ripgreprc
├── codemap/
│   ├── codemap.sh
│   ├── codemap-projects.json
│   └── .codemap-cache/ (gitignored)
└── mcp/
    ├── server.js
    ├── package.json
    ├── .mcp.json
    └── node_modules/ (gitignored)
```

---

## ⚠️ Common Issues

**CodeMap returns no results?**
- Check if backend is running: `curl -s http://10.0.161.21:50888/swagger`
- If down, contact team (not running locally)
- Fallback: use `rg` (ripgrep) for pattern search

**Ripgrep pattern not matching?**
- Use PCRE2 regex: `rg "pattern" --type ts`
- Escape special chars: `rg "this\.method\(" --type ts`

**MCP server won't start?**
- Check Node version: `node --version` (need 18+)
- Clear cache: `rm -rf tools/mcp/node_modules && npm install`

---

**Last updated:** 2026-05-05
