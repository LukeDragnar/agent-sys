# Agent System

Nền tảng AI assistant dùng chung cho cả team — viết code, review, commit, phân tích nghiệp vụ với context chuẩn của từng dự án.

> Web UI có trang **Tổng quan** chi tiết hơn: `http://localhost:4201/overview`

---

## 🗺️ Bản đồ kho tri thức (cho AI + team)

> Thiết kế chuẩn: [docs/cau-truc-kho-tri-thuc.md](docs/cau-truc-kho-tri-thuc.md) · Quy ước: [CONVENTIONS.md](CONVENTIONS.md) · Brain của AI: [CLAUDE.md](CLAUDE.md)

```
AgentSystem/
├── rules/            ← quy tắc TOÀN CÔNG TY: code · git · build · db · quy-trinh-theo-level
├── agents/           ← VAI TRÒ AI: code · review · trace · git · duc-ket (đúc kết → nuôi kho)
├── templates/        ← khuôn thêm mới: feature · agent
├── feedback/         ← TÍN HIỆU NGOÀI (bug khách/ticket) → pattern → kho  [vòng lặp nuôi kho]
├── memory/           ← TRÍ NHỚ tích lũy (1-fact-1-file + MEMORY.md index)
├── docs/             ← tài liệu thiết kế (cấu trúc kho, codemap)
└── projects/<mảng>/  ← TRI THỨC TỪNG MẢNG (scrm, ps_delivery, ecommerce_connect...)
    ├── architecture · domain · patterns · qa-code · qa-spec   (mô tả + quy tắc)
    ├── registry/     ← 🧠 KNOWLEDGE-GRAPH: mỗi controller 1 node (endpoint/hành vi đúng/quan hệ/lỗi)
    ├── features/     ← cẩm nang tính năng (summarize/faq/flow/dev) cho người + chatbot
    └── skills/       ← recipe tác vụ tái sử dụng (paging, export, upload...)
```

**3 lớp tri thức của 1 mảng:** `registry/` (backbone máy-đọc) → `features/` (prose cho người/chatbot) → `skills/` (recipe how-to). Xem [projects/scrm/registry/README.md](projects/scrm/registry/README.md).

---

## Yêu cầu

| Tool | Version | Ghi chú |
|------|---------|---------|
| Claude Code CLI | mới nhất | `npm install -g @anthropic-ai/claude-code` |
| Node.js | 18+ | Cho MCP server |
| .NET SDK | 8.0 | Chỉ cần nếu chạy backend local |

## Quick Start (member mới)

### 1. Clone & install

```bash
git clone <repo-url>
cd AgentSystem
cd mcp && npm install && cd ..
```

### 2. Cấu hình paths local

```bash
cp projects/local-paths.example.json projects/local-paths.json
```

Sửa `projects/local-paths.json` theo máy của bạn:

```json
{
  "scrm":          "D:/PROJECT/Git/scrm",
  "scrm_frontend": "D:/PROJECT/Git/scrm_frontend",
  "ps_delivery":   "D:/PROJECT/Git/ps_delivery"
}
```

> File này đã gitignore — mỗi người giữ paths riêng.

### 3. Tạo tài khoản & cấu hình MCP

```bash
node scripts/setup-user.js
```

Script hỏi: **username** (đề xuất từ git identity), **password**, **setup key** (hỏi lead). Sau khi chạy xong, `.mcp.json` được tạo tự động.

### 4. Mở Claude Code

```bash
claude
```

MCP kết nối tự động. Mọi conversation tự lưu vào DB.

### 5. Kiểm tra

Vào web app → Sessions → kiểm tra session của bạn xuất hiện sau lần đầu chat.

---

## Chạy Backend (local, tùy chọn)

Backend chạy chung trên server, member không cần chạy local trừ khi phát triển AgentSystem.

```bash
# Chạy backend
dotnet run --project backend/AgentService/AgentService.csproj

# Chạy frontend
cd frontend && npm install && npm start
```

- Backend: `http://localhost:5137` (Swagger UI)
- Frontend: `http://localhost:4201`

---

## CodeMap — Source Code Analyzer

CodeMap dùng Roslyn scan toàn bộ source code → lưu vào DB → Claude Code query trực tiếp thay vì grep. **Tiết kiệm ~90% tokens**.

### Cách thức hoạt động

```
Source code (.cs files)
    │
    │  Roslyn parse (KHÔNG dùng AI, $0)
    ▼
SQL Server (CodeSymbols, CodeApiEndpoints, CodeDependencies)
    │
    ├── REST API  → Web UI (xem trên browser)
    ├── CLI       → codemap.sh (query từ terminal)
    └── MCP       → Claude Code (tự động query khi cần)
```

1. **Scan** — Roslyn đọc từng file `.cs`, parse AST, trích xuất class/method/property/enum values/endpoint/dependency
2. **Lưu DB** — Kết quả lưu vào SQL Server với indexes, mỗi project tách biệt bằng `ProjectId`
3. **Query** — Claude Code hoặc Web UI gọi REST API → query DB → trả kết quả chính xác

> Toàn bộ scan + query **không dùng AI**, không tốn token. Chỉ là compiler API + SQL.

### Ví dụ thực tế

**Câu hỏi: "Tạo phiếu xuất có mấy loại?"**

| | Không có Map | Có Map |
|---|---|---|
| Bước 1 | `grep "PhieuXuat" -r` → 100+ matches | `codemap.sh search PhanLoaiXuatEnum` → 17 values + description |
| Bước 2 | Đọc 3-5 file sai | *(không cần)* |
| Bước 3-6 | grep thêm 3-4 lần | *(không cần)* |
| **Tool calls** | 12-15 | **1** |
| **Tokens** | ~13,500 | **~300** |
| **Tiết kiệm** | — | **97.8%** |

**Câu hỏi: "Fix bug OrderService"**

| | Không có Map | Có Map |
|---|---|---|
| Tìm file + methods + deps | grep 8-12 lần, đọc sai 2-3 file | `codemap.sh symbol OrderService Class` → 1 lần |
| Tìm endpoint | grep [HttpPost] + [Route] | `codemap.sh endpoint order POST` → 1 lần |
| **Tokens** | ~10,000 | **~1,200** |
| **Tiết kiệm** | — | **88%** |

**Chi phí ước tính (20 câu/ngày):**

| | Grep | CodeMap |
|---|---|---|
| Tokens/ngày | 200,000 | 16,000 |
| Chi phí/tháng | $66 | $5.28 |
| **Tiết kiệm/tháng** | — | **$60.72** |

### Dùng trên CLI

```bash
# Xem danh sách projects
./codemap.sh projects

# Chọn project (hỏi 1 lần, cache theo terminal)
./codemap.sh use SCRM

# Tìm class/method/property
./codemap.sh search OrderService

# Xem chi tiết class (members, dependencies)
./codemap.sh symbol OrderService Class

# Tìm API endpoint
./codemap.sh endpoint order POST

# Xem dependency tree
./codemap.sh deps OrderService

# Xem stats project
./codemap.sh stats

# Re-scan khi code thay đổi lớn
./codemap.sh scan "D:\PROJECT\Git\scrm"
```

### Dùng trên Web

Mở `http://localhost:4201/codemap`:

| Tab | Chức năng |
|-----|-----------|
| **Overview** | Stats: số class, method, endpoint, dependencies |
| **Search** | Gõ keyword → kết quả realtime, click xem chi tiết |
| **Endpoints** | Bảng tất cả API routes, filter theo route/method |
| **Explorer** | 3 panel: Tree (trái) + Detail (giữa) + Graph (phải) |
| **Dependencies** | Nhập class name → xem depends on + used by |

### Chạy song song nhiều project

Mỗi terminal cache project riêng:

```bash
# Terminal 1
./codemap.sh use SCRM
./codemap.sh search OrderService    # → query SCRM

# Terminal 2
./codemap.sh use ps_delivery
./codemap.sh search WebhookService  # → query ps_delivery
```

Hoặc dùng env var:

```bash
CODEMAP_PROJECT_ID="abc..." ./codemap.sh search OrderService
```

### Env vars

| Var | Default | Mô tả |
|-----|---------|--------|
| `CODEMAP_API` | `http://localhost:5137` | API URL |
| `CODEMAP_USER` | `admin` | Login username |
| `CODEMAP_PASS` | `Admin@123` | Login password |
| `CODEMAP_PROJECT_ID` | auto-detect | Override project ID |

---

## Agents & Slash Commands

| Command | Mô tả |
|---------|--------|
| `/code-agent` | Viết và sửa code — mở terminal trong folder project |
| `/git-agent` | Commit, branch, push — Claude tạo message chuẩn |
| `/review-agent` | Review code theo checklist `qa-code.md` |
| BA Agent (mặc định) | Chat trên web — phân tích nghiệp vụ, tạo spec |

---

## Cấu trúc repo

```
AgentSystem/
├── backend/
│   ├── AgentService/          # API (ASP.NET Core)
│   ├── AgentCore/             # Business logic + CodeMap (Roslyn)
│   └── AgentInfrastructure/   # Data access, EF Core, Entities
├── frontend/                  # Angular 17 Web UI
├── mcp/                       # MCP Server (Node.js)
├── projects/                  # Project context (architecture, patterns, qa)
├── memory/                    # Shared memory (team dùng chung)
├── rules/                     # Rules cho agents
├── agents/                    # Agent definitions
├── docs/                      # Documentation
│   ├── CODEMAP_GUIDE.md       # Hướng dẫn CodeMap chi tiết
│   └── CODEMAP_BENCHMARK.md   # So sánh hiệu suất
├── codemap.sh                 # CodeMap CLI
└── CLAUDE.md                  # Instructions cho Claude Code
```

---

## Troubleshooting

| Lỗi | Xử lý |
|-----|--------|
| MCP không kết nối | Kiểm tra `AGENT_API_URL` trong `.mcp.json` và backend đang chạy |
| 401 Unauthorized | Sai username/password trong `.mcp.json` |
| `codemap.sh` báo login failed | Backend chưa chạy hoặc sai `CODEMAP_API` |
| Scan chậm | Bình thường cho lần đầu (~2 phút cho 5K files). Data lưu DB, không cần scan lại |
| Session không hiện trên web | Kiểm tra backend logs |
| Context project không load | Kiểm tra `projects/local-paths.json` |
