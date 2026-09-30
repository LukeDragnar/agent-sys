# Agent System — Roadmap

## Tổng quan hệ thống

Agent System là nền tảng AI assistant dùng chung cho cả team, gồm các module:

```
┌─────────────────────────────────────────────────────────────────────┐
│                        AGENT SYSTEM                                 │
│                                                                     │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────────────┐   │
│  │ BA Agent │  │Code Agent│  │Git Agent │  │ Review Agent     │   │
│  │ (web UI) │  │ (CLI)    │  │ (CLI)    │  │ (CLI)            │   │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘  └────┬─────────────┘   │
│       └──────────────┴──────────────┴──────────────┘               │
│                              │                                      │
│  ┌───────────────────────────┴───────────────────────────────────┐  │
│  │                     AgentService (API)                         │  │
│  │  Sessions │ Specs │ Usage │ Projects │ CodeMap │ Orchestrator  │  │
│  └───────────────────────────┬───────────────────────────────────┘  │
│                              │                                      │
│  ┌─────────┐  ┌──────────┐  │  ┌──────────┐  ┌────────────────┐   │
│  │ SQL     │  │ Claude   │  │  │ MCP      │  │ Project Docs   │   │
│  │ Server  │  │ API      │  │  │ Server   │  │ (patterns, qa) │   │
│  └─────────┘  └──────────┘  │  └──────────┘  └────────────────┘   │
│                              │                                      │
│  ┌───────────────────────────┴───────────────────────────────────┐  │
│  │                     Frontend (Angular 17)                      │  │
│  │  Chat │ Specs │ CodeMap │ Logs │ Admin │ Overview │ Projects   │  │
│  └───────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Modules hiện có

| Module | Mô tả | Status |
|--------|--------|--------|
| **BA Agent** | Chat trên web, phân tích nghiệp vụ, tạo spec | DONE |
| **Code Agent** | Slash command `/code-agent`, viết/sửa code theo context project | DONE |
| **Git Agent** | Slash command `/git-agent`, commit/push chuẩn convention | DONE |
| **Review Agent** | Slash command `/review-agent`, review theo qa-code.md | DONE |
| **Orchestrator** | Sinh code tự động từ spec (BA→DEV→QA→FIX loop) | DONE |
| **Session & History** | Lưu conversation, xem lại trên web | DONE |
| **Spec Management** | Tạo/quản lý spec từ conversation, version tracking | DONE |
| **Usage Tracking** | Token usage per user, weekly limit | DONE |
| **Project Context** | Auto-load architecture/patterns/qa per project | DONE |
| **CodeMap** | Roslyn scan source → query symbols/endpoints/deps | DONE |
| **CodeMap Explorer** | Web UI: Tree + Detail + Graph (D3.js) | DONE |
| **CodeMap CLI** | `codemap.sh` query từ terminal | DONE |
| **Shared Memory** | Team memory trong git, rules cho Claude | DONE |

---

## Roadmap — Features sắp tới

### Priority 1 — Giảm token + tăng tốc

| # | Feature | Mô tả | Effort | Impact |
|---|---------|--------|--------|--------|
| 1.1 | **Impact Analysis** | `codemap.sh impact <ClassName>` → full chain: entity → services → controllers → FE components. Trả lời "sửa X ảnh hưởng gì?" bằng 1 lệnh | M | Giảm ~80% token cho câu hỏi impact |
| 1.2 | **Incremental Scan Watcher** | File watcher tự scan khi code thay đổi (dotnet watch hoặc git hook), không cần chạy thủ công | S | Dữ liệu luôn mới, không cần nhớ re-scan |
| 1.3 | **Smart Context Loader** | Trước khi Claude trả lời, tự query CodeMap lấy context liên quan → inject vào prompt. Claude không cần grep nữa | M | Giảm ~90% token cho mọi câu hỏi |

### Priority 2 — Tăng chất lượng code

| # | Feature | Mô tả | Effort | Impact |
|---|---------|--------|--------|--------|
| 2.1 | **PR Review Auto-Checklist** | Input: git diff files → Output: checklist tự động dựa trên qa-code.md + CodeMap (thiếu DI? cần migration? FE cần update?) | M | Bắt lỗi convention trước khi review |
| 2.2 | **Duplicate Detector** | Scan tìm classes/methods trùng signature ở nhiều modules, suggest merge/extract shared | M | Giảm tech debt |
| 2.3 | **Convention Enforcer** | Pre-commit hook check naming, structure, pattern theo patterns.md. Block nếu vi phạm | S | Thống nhất 100% convention |

### Priority 3 — Tăng tốc phát triển

| # | Feature | Mô tả | Effort | Impact |
|---|---------|--------|--------|--------|
| 3.1 | **Snippet Templates** | `codemap.sh gen entity/controller/service <Name>` → generate code chuẩn convention từ patterns.md | M | Junior không cần nhớ convention |
| 3.2 | **API Contract Dashboard** | So sánh BE endpoints vs FE service calls → tìm API orphan, API missing, breaking changes | M | Giảm bug khi sửa API |
| 3.3 | **Migration Script Generator** | Detect entity field changes giữa 2 lần scan → auto-gen ALTER TABLE script | S | Không quên migration |

### Priority 4 — Quản lý & tracking

| # | Feature | Mô tả | Effort | Impact |
|---|---------|--------|--------|--------|
| 4.1 | **Token Budget Dashboard** | Log token per conversation, leaderboard, trend, alert member dùng grep thay vì CodeMap | M | Giảm chi phí toàn team |
| 4.2 | **Docs Management (Phase 2)** | Migrate project docs sang DB, Web UI edit, MCP realtime, collaborative | L | Docs luôn up-to-date |
| 4.3 | **Activity Feed** | Ai đang làm gì, sửa module nào, conflict potential detection | S | Giảm conflict, tăng visibility |

### Priority 5 — Mở rộng

| # | Feature | Mô tả | Effort | Impact |
|---|---------|--------|--------|--------|
| 5.1 | **Multi-project Scan** | Scan tất cả projects (ps_delivery, ecommerce_connect...) không chỉ SCRM | S | CodeMap cho toàn bộ hệ thống |
| 5.2 | **Cross-project Dependency** | Detect dependency giữa SCRM ↔ ps_delivery ↔ ecommerce_connect qua RabbitMQ messages | L | Hiểu toàn bộ microservice graph |
| 5.3 | **AI Code Review** | Claude auto-review mỗi PR dựa trên qa-code.md + CodeMap context, comment trên GitLab | L | Review 24/7, không chờ người |

---

## Effort Legend

| Label | Thời gian ước tính |
|-------|-------------------|
| **S** (Small) | 1-2 ngày |
| **M** (Medium) | 3-5 ngày |
| **L** (Large) | 1-2 tuần |

---

## Đã hoàn thành

| Feature | Ngày | Ghi chú |
|---------|------|---------|
| CodeMap Roslyn Scanner | 2026-04-07 | Phase 1: scan C# + TS, lưu SQL Server |
| CodeMap REST API | 2026-04-07 | 8 endpoints: scan, symbols, endpoints, deps, search, stats, tree, graph |
| CodeMap MCP Tools | 2026-04-07 | 5 tools trong mcp/server.js |
| CodeMap Web UI | 2026-04-07 | 6 tabs: Overview, Search, Endpoints, Explorer, Dependencies |
| CodeMap CLI | 2026-04-07 | codemap.sh: search, symbol, endpoint, deps, stats, scan, update, add |
| ExtraInfo (enum values, attributes) | 2026-04-08 | Enum values + [Description] + attributes + const fields |
| Incremental Scan | 2026-04-08 | Chỉ scan files thay đổi, không xóa hết |
| Add Path (FE support) | 2026-04-08 | Thêm Angular FE vào cùng project BE |
| Module Graph | 2026-04-08 | D3.js force graph, drill-down modules → classes → symbol |
| Graph Improvements | 2026-04-08 | Auto fit-to-view, edge toggle, hover highlight, breadcrumb |
| Benchmark Report | 2026-04-08 | So sánh token savings: 92% giảm trung bình |
