---
name: Multi-lang Baseline - SCRM
description: Last execution info for multi-lang automation
type: reference
---

# Multi-lang Baseline - SCRM

Baseline info cho lần thực hiện multi-lang cuối cùng.

## Last Execution

| Field | Value |
|-------|-------|
| Date | 2026-05-13 |
| Time | now |
| Mode | Update File (don-hang-hoan: cha + detail) |
| Scope | FE only |
| Status | Completed ✓ |

## Last Baseline Commit

```
Commit: pending
Message: Multi-lang: Wrap Vietnamese in don-hang-hoan FE (list + detail)
Date: 2026-05-13
Branch: feature/agent-embedding-and-scrm-docs
Files:
  - don-hang-hoan.component.html
  - don-hang-hoan.component.ts
  - don-hang-hoan-detail/don-hang-hoan-detail.component.html
  - don-hang-hoan-detail/don-hang-hoan-detail.component.ts
```

## Statistics

| Metric | Count |
|--------|-------|
| Files processed | 4 (2 HTML + 2 TS) |
| New keys created | 58 |
| Reused keys | 2 (error-title, undetermined) |
| Files skipped | 0 |
| Issues found | 0 |
| Total DB records (new) | 58 |

## Previous Execution

| Field | Value |
|-------|-------|
| Date | 2026-04-24 |
| Time | 16:15 |
| Mode | New File (6 components) |
| Scope | FE only |
| Status | Completed ✓ |
| Files | 12 (6 TS + 6 HTML) |
| New keys | 29 |
| Components | omicall-view-dial, table-resize-not-save, table-resize, th-directive, th-resizer-directive, table-setting |

## Usage

Khi user yêu cầu multi-lang:
1. Claude đọc file này
2. Lấy `last_baseline_commit` → compare với release_product HEAD
3. Scan files thay đổi → wrap
4. Update file này khi done

## SQL Files Generated

Each execution generates INSERT script(s):
- Location: `projects/scrm/skills/multi-lang/sql/2026-05-13.sql` (latest)
- Content: INSERT statements for new keys only (safe, IF NOT EXISTS)
- User runs manually (NOT auto-executed)

Generated:
- `sql/2026-05-13.sql` → 58 new keys (don-hang-hoan list + detail)
- `sql/2026-04-24-batch-3-components.sql` → 29 new keys (prev)
