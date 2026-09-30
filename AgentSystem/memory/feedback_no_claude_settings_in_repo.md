---
name: Không commit .claude/settings.local.json
description: File Claude Code settings local (personal dev) phải gitignore, không push repo
type: feedback
---

File `.claude/settings.local.json` (và tương tự `.claude/settings.json` nếu có) là **Claude Code local settings** của dev cá nhân — KHÔNG commit vào repo chung. `.gitignore` repo SCRM đã có pattern `.claude/` nhưng file có thể được track từ trước nếu dev khác đã commit rồi.

**Why:** User confirm rule này 2026-04-15 — file settings của Claude Code không thuộc về shared repo (mỗi dev có preferences riêng, tool configs riêng), push lên gây conflict cho team khác.

**How to apply:**
- Khi git status show `.claude/settings.local.json` modified/new → **KHÔNG stage**, KHÔNG commit
- Nếu file đã tracked (từ commit cũ) → `git rm --cached .claude/settings.local.json` + commit + push để untrack
- Verify `.gitignore` có `.claude/` pattern (đã có trong SCRM)
- Nếu thấy file này trong branch mới cherry-pick hoặc diff MR → revert ngay
- Rule áp dụng cho TẤT CẢ repo (SCRM và các repo khác)
