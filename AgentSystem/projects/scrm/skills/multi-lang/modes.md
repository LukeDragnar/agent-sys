# Multi-lang Modes - Chi Tiết

## Mode 1: Baseline (Default)

**Trigger:** "bổ sung multi-lang" / "bổ sung đa ngôn ngữ"

**What it does:**
```
1. Đọc projects/scrm/skills/multi-lang/baseline.md
   → lấy last_baseline_commit, last_execution_date
2. git log {last_baseline_commit}...release_product
   → list tất cả commits kể từ lần cuối
3. git diff {last_baseline_commit}...release_product src/app/pages/**
   → list all changed files
4. Scan files đó → tìm Vietnamese text chưa wrap
5. Wrap + generate SQL + spawn review-agent
6. Update baseline.md (date, commit, counts)
```

**Input:** None (tự động lấy baseline)

**Output:**
- X files processed
- Y keys created
- SQL INSERT statements
- Trace: projects/scrm/skills/multi-lang/trace/{date}.md

**When to use:** 
- Default choice
- Khi user chỉ nói "bổ sung multi-lang"

---

## Mode 2: All

**Trigger:** "làm multi-lang all" / "wrap all" / "multi-lang toàn bộ"

**What it does:**
```
1. Ignore baseline (scan từ đầu)
2. Scan ALL files in src/app/pages/** (FE)
   + Optionally: BE C# files
3. Tìm ALL Vietnamese text (wrap hoặc không wrap)
4. Wrap tất cả
5. Generate SQL + review
6. Update baseline.md (do lần này scan all)
```

**Input:** 
- Scope: FE only / BE only / Both? (user choose)

**Output:**
- All X files in scope processed
- All Y keys (including already wrapped)
- SQL INSERT
- Trace

**When to use:**
- Clean up toàn bộ project
- After major refactor
- Baseline quá cũ/unstable

---

## Mode 3: New File

**Trigger:** "làm multi-lang file X" / "wrap file Y" / "translate file Z"

**What it does:**
```
1. User provides: file path (e.g., src/app/pages/sale/sale.component.ts)
2. Scan file → tìm Vietnamese text
3. Wrap toàn bộ (assume file mới, chưa có multi-lang)
4. Generate SQL + review
5. Update keys-log.md (thêm keys mới vào global log)
6. Do NOT update baseline (chỉ update 1 file, không scan commits)
```

**Input:**
- File path: src/app/pages/.../...component.ts or .html

**Output:**
- 1 file wrapped
- N keys created
- SQL INSERT
- Trace (log file path + keys)

**When to use:**
- User viết file mới → cần multi-lang
- Refactor/move file → cần wrap lại
- Specific file missing translations

---

## Mode 4: Update File

**Trigger:** "bổ sung multi-lang file X" / "update file Y" / "thêm translation file Z"

**What it does:**
```
1. User provides: file path
2. Scan file → tìm Vietnamese text CHƯA wrap
3. Wrap chỉ những phần thiếu (smart detect)
4. Generate SQL (chỉ new keys) + review
5. Update keys-log.md
6. Do NOT update baseline (single file update)
```

**Input:**
- File path
- (Optional) List specific keys to update

**Output:**
- 1 file updated
- M keys added (chỉ missing ones)
- SQL INSERT
- Trace

**When to use:**
- File cũ → bổ sung multi-lang
- File có vài hardcoded text → thêm wrap
- QA phát hiện missing translations

---

## Decision Tree

```
User nói multi-lang?
  ├─ Nếu nói "bổ sung" (không có "file" + không "all")
  │  → Mode 1: Baseline (default)
  │
  ├─ Nếu nói "all" hoặc "toàn bộ"
  │  → Mode 2: All (ask: FE/BE/Both?)
  │
  ├─ Nếu nói "làm" + file path
  │  → Mode 3: New File
  │
  └─ Nếu nói "bổ sung" + file path
     → Mode 4: Update File
```

---

## Scope (FE / BE / Both)

**Default: FE only** (Angular components)
```
src/app/pages/**/*.ts
src/app/pages/**/*.html
```

**If user ask BE:**
```
{WORKING_BE}/src/**/*.cs
(Search for hardcoded Vietnamese strings)
```

**Both:**
- Scan FE + BE tương tự
- Generate separate SQL per project
