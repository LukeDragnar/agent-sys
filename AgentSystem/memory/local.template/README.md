# memory/local/ — Session-Specific Files (Per Member)

Folder này chứa **session-specific files** của mỗi member.

**Important:** Folder này là **gitignored** — files ở đây KHÔNG được commit lên git.

---

## 📁 Structure

```
memory/local/                    ⛔ gitignore — KHÔNG push (Write/mkdir tự tạo khi ghi)
├── paths.md                     (your working paths)
├── {feature-name}.md            (plan/kế hoạch — GỒM cả khối QA: review+trace)
├── {feature-name}-progress.md   (progress report)
├── tools-usage-log.md           (log tool usage)
├── trace/                       (trace report — trace-agent ghi vào đây)
│   └── {feature}-trace.md
├── sql/                         (SQL gen ra — KHÔNG commit, dev tự chạy)
└── tmp/                         (code-gen / scratch — xóa sau task)
```
> **MỌI thứ sinh khi task → đặt dưới `memory/local/` (1 ô)**: plan/progress trực tiếp; `trace/`, `sql/`, `tmp/` là subfolder. Tất cả ignore. `scripts/` chỉ chứa script reusable đã commit.

---

## Files to Create Per Session

### 1. `paths.md` — Your Working Paths (Setup once)

Template: `memory/local.template/paths.md.template`

```markdown
# Working Paths — {Your Name}

WORKING_BE = D:\PROJECT\Git\_SourceForCode\Group3\scrm
WORKING_FE = D:\PROJECT\Git\_SourceForCode\Group3\scrm_frontend
```

Why: Paths khác nhau per member, không hardcode vào git.

---

### 2. `{feature-name}.md` — Session Plan (Per task)

**Template: `memory/local.template/plan.md.template`** — copy → `memory/local/{feature-name}.md`. Đủ khối **BẮT BUỘC**: Nguồn-đã-NẮM (codemap/registry/feature) · Scope · Acceptance/Edge · **QA (review+trace)** · Confirm.

> Task đụng logic: lập plan xong → **TRÌNH + chờ user duyệt** rồi mới code. KHÔNG tự cắt scope.

Auto-cleanup: Delete after task done (or keep 1-2 sessions if useful).

---

### 3. `{feature-name}-progress.md` — Progress Tracking (Optional)

```
memory/local/multi-lang-batch-3-progress.md
├── Completed: 26/119 files
├── Current: File 27 (baocao-ketoan)
└── Next: File 28 (bssid)
```

Auto-cleanup: Delete after session done.

---

### 4. `tools-usage-log.md` — Tools Usage History (Optional)

Template: `memory/local.template/tools-usage-log.md.template`

Track which tools you used:
```
| 2026-05-05 | rg "OrderService" | 12 matches | Search BE |
| 2026-05-05 | codemap symbol OrderService | 1.2s | Deps |
```

---

### 5. `trace/` — Debug Traces (Per task)

When you trace a case:
```
memory/local/trace/chi-tieu-batch-trace.md
├── Case: Customer A, 5 orders, 3 status changes
├── Result: ✅ Correct delta = +500k
```

---

## 🚫 What NOT to Store Here

- ❌ Code files (belongs in source folder)
- ❌ Team-wide rules / kinh nghiệm chung → `memory/` **ROOT** (file `.md`, committed) — KHÔNG để trong `local/`
- ✅ **SQL gen → `local/sql/`** · **code-gen/artifact → `local/tmp/`** (GIỜ để TRONG `local/`, không còn `./scripts/sql` hay `.tmp/`). `scripts/` chỉ chứa script **reusable đã commit**.

---

## Lifecycle

| Phase | Action |
|-------|--------|
| Session start | Create paths.md (first time only) |
| Task planning | Create {feature-name}.md |
| During work | Update progress, trace |
| Session end | Keep useful files, delete ephemeral ones |
| Next session | Read old .md for reference, create new ones |

---

## ✅ Benefits

| Benefit | Example |
|---------|---------|
| No path conflicts | Each dev has own paths in local/ |
| Clean repo | No ephemeral files in git |
| Reference history | Keep useful traces for future sessions |
| Per-session isolation | Plan/progress don't interfere across sessions |

---

**Created:** 2026-05-05
