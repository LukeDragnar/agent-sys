# Examples - Usage Scenarios

## Scenario 1: User says "bổ sung multi-lang"

```
User: "bổ sung multi-lang"
  ↓
Claude:
  1. Đọc projects/scrm/skills/multi-lang/baseline.md
     → last_baseline_commit: abc123def456
     → last_execution_date: 2026-04-20 14:30
  
  2. Dùng AskUserQuestion:
     "Mode nào?"
       - Baseline (lần cuối: 2026-04-20) ← recommended
       - All (toàn bộ FE)
       - Specific file
       - Other: ...
     
     "Scope nào?"
       - FE only ← default
       - BE + FE
  
  3. User chọn "Baseline" + "FE only"
  
  4. Claude:
     - git log abc123def456...release_product
     - git diff abc123def456...release_product src/app/pages/**
     - Spawn multi-lang-agent
  
  5. Agent done → Update baseline.md
     last_execution_date: 2026-04-24 10:00
     last_baseline_commit: xyz789abc123
     files_processed: 12 files
     keys_created: 45 keys
```

---

## Scenario 2: User says "làm multi-lang all"

```
User: "làm multi-lang all"
  ↓
Claude:
  1. AskUserQuestion:
     "Scope nào?"
       - FE only ← recommended
       - BE (C# files)
       - Both
  
  2. User chọn "FE only"
  
  3. Claude:
     - Scan src/app/pages/** (ignore baseline)
     - Spawn agent (batch 10-15 files)
  
  4. Agent:
     - Wrap ALL files
     - Report: 89 files processed, 234 keys created
     - Generate SQL
  
  5. Update baseline.md (scan all = new baseline)
```

---

## Scenario 3: User says "làm multi-lang file src/app/pages/sale/sale.component.ts"

```
User: "làm multi-lang file src/app/pages/sale/sale.component.ts"
  ↓
Claude:
  1. AskUserQuestion:
     "File này:"
       - File mới (chưa có multi-lang) ← recommended
       - File cũ (bổ sung missing)
       - Other: ...
  
  2. User chọn "File mới"
  
  3. Claude:
     - Scan file đó
     - Spawn agent (1 file only)
  
  4. Agent:
     - Wrap file
     - Report: 1 file, 8 keys created
     - Add keys to keys-log.md
     - Do NOT update baseline.md
```

---

## Scenario 4: User says "bổ sung multi-lang file order.component.html"

```
User: "bổ sung multi-lang file order.component.html"
  ↓
Claude:
  1. AskUserQuestion:
     "File này:"
       - File cũ, bổ sung missing translations ← recommended
       - File mới (full wrap)
  
  2. User chọn "File cũ, bổ sung"
  
  3. Claude:
     - Smart scan: tìm chỉ Vietnamese text CHƯA wrap
     - Spawn agent
  
  4. Agent:
     - Wrap missing parts only
     - Report: 1 file, 3 keys added
     - Update keys-log.md (add new keys)
     - Do NOT update baseline.md
```

---

## Scenario 5: User says "làm đa ngôn ngữ"

```
User: "làm đa ngôn ngữ"
  ↓
Claude:
  1. AskUserQuestion:
     "Bạn muốn:"
       - Bổ sung từ lần cuối (Baseline) ← recommended
       - Wrap toàn bộ (All)
       - Wrap 1 file cụ thể
       - Bổ sung 1 file cụ thể
       - Other: ...
  
  2. User chọn "Baseline"
  
  3. Claude: → Flow tương tự Scenario 1
```

---

## Output Examples

### Fix Summary
```
✓ Multi-lang Processing Complete

Mode: Baseline (since 2026-04-20)
Scope: FE only (Angular)

Results:
  Files processed: 12
  New keys created: 45
  Files skipped (no change): 5
  
SQL generated: projects/scrm/skills/multi-lang/trace/2026-04-24-keys.sql
Trace log: projects/scrm/skills/multi-lang/trace/2026-04-24.md

Updated baseline: 2026-04-24 10:30
```

### Trace Log Example
```markdown
# Multi-lang Fixes - 2026-04-24

## Summary
- Mode: Baseline
- Files processed: 12
- New keys: 45
- Execution time: 10:30

## Files

### 1. src/app/pages/sale/sale.component.ts
- Wrapped: 3 texts
- Keys: sale-title, sale-search-placeholder, msg-no-data
- Issues: none

### 2. src/app/pages/sale/sale.component.html
- Wrapped: 8 attributes
- Keys: action-edit, action-delete, status-active, status-inactive, ...
- Issues: 1 placeholder attribute mismatch (fixed)

### 3. src/app/pages/order/order.component.ts
- Wrapped: 5 messages
- Keys: msg-create-success, msg-update-error, error-validation-required, ...
- Issues: none

...
```

### SQL Output Example
```sql
-- Multi-lang Keys - 2026-04-24
-- Total: 45 new keys

INSERT INTO dbo.TranslationKey (Key, VietnameseValue, CreatedDate)
VALUES 
  ('sale-title', 'Quản lý bán hàng', GETDATE()),
  ('sale-search-placeholder', 'Tìm kiếm đơn hàng', GETDATE()),
  ('action-edit', 'Sửa', GETDATE()),
  ('action-delete', 'Xóa', GETDATE()),
  ('status-active', 'Hoạt động', GETDATE()),
  ('status-inactive', 'Không hoạt động', GETDATE()),
  ('msg-create-success', 'Tạo thành công', GETDATE()),
  ('msg-update-error', 'Cập nhật thất bại', GETDATE()),
  ('error-validation-required', 'Trường bắt buộc', GETDATE()),
  ...
;
```

---

## Checklist - Before Agent Spawns

Claude should verify:
☐ Baseline.md exists and readable  
☐ User confirms mode choice  
☐ Scope is clear (FE/BE/Both)  
☐ If file-specific: path is valid  
☐ Git history accessible (if baseline mode)  

If any check fails → ask user to clarify before spawning.
