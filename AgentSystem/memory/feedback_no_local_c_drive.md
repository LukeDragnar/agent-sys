---
name: KHÔNG lưu files ở ổ C:\ — Chỉ dùng repo paths
description: Không bao giờ lưu session files, plans, logs vào C:\ drive. Chỉ lưu vào repo root (memory/local/)
type: feedback
---

## Rule: KHÔNG lưu gì ở ổ C:\ — Chỉ dùng repo root paths

**Ban hành:** 2026-05-05

**Quy tắc:**
- ❌ KHÔNG dùng `.claude/plans/` (ở C:\Users\..., local machine)
- ❌ KHÔNG dùng `.claude/` local paths bất kỳ
- ✅ **DỰY dùng `memory/local/`** (folder trong repo root)
- ✅ Tất cả session files → repo root only (không hardcode absolute paths)

**Why:** 
- Ổ C:\ là local machine → không shared với team
- Repo root là shared, persistent, backed up → mỗi dev clone về đều có
- Plans/traces của member phải accessible cho team (hoặc member session sau)
- KHÔNG hardcode paths (mỗi dev clone vào folder khác nhau)

**How to apply:**
Khi user yêu cầu:
- "Lên kế hoạch" → `memory/local/{task}.md` (repo)
- "Trace case" → `memory/local/trace/{task}.md` (repo)
- "Log usage" → `memory/local/tools-usage-log.md` (repo)
- KHÔNG bao giờ suggest `.claude/` paths

**Checklist trước khi report task done:**
- ☐ Tất cả files lưu vào repo root (không ở C:\ drive)
- ☐ KHÔNG suggest `.claude/plans/` hoặc C:\ paths
- ☐ Session files trong `memory/local/` (gitignored nhưng persistent)
