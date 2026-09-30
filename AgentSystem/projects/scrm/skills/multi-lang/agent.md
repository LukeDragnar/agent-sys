# Agent: SCRM Multi-lang

Agent xử lý wrapping Vietnamese text thành multi-lang keys.

## Critical Rules

### Rule 1: Minimal Changes Only

**MUST follow:** Chỉ wrap Vietnamese text, KHÔNG refactor/cleanup/reformat code khác.

**Why:** Khi user `git diff`, sẽ chỉ thấy multi-lang changes, không noise từ refactor.

### Rule 2: TUYỆT ĐỐI Không Tác Động Vào Logic

**MUST:** KHÔNG thay đổi logic code, flow, hay behavior.

**Examples of FORBIDDEN:**
```typescript
// ❌ SAI: Thay đổi variable name
- const statusList = [...]
+ const statuses = [...]  // ← LOGIC CHANGE

// ❌ SAI: Thay đổi return value
- return data;
+ return data ?? [];  // ← LOGIC CHANGE

// ❌ SAI: Thay đổi condition
- if (status === 'pending')
+ if (status === 'pending' || status === 'waiting')  // ← LOGIC CHANGE

// ❌ SAI: Thêm new method/field
+ private formatDate() { ... }  // ← LOGIC ADDITION

// ❌ SAI: Reorder statements (có thể thay đổi execution order)
- doA();
- doB();
+ doB();  // ← LOGIC CHANGE
+ doA();
```

**Chỉ được phép:**
```typescript
// ✅ Wrap text (LOGIC UNCHANGED)
- { label: 'Pending', value: 1 }
+ { label: this.tsb.t('status-pending', 'Pending'), value: 1 }

// ✅ Add import (LOGIC UNCHANGED)
+ import { TranslateSBService } from '...';

// ✅ Add inject (LOGIC UNCHANGED)
+ private tsb = inject(TranslateSBService);
```

**Examples:**
```typescript
// ✅ ĐÚNG: Minimal change
- this._notification.success('Cập nhật thành công');
+ this._notification.success(this.tsb.t('msg-update-success', 'Cập nhật thành công'));

// ❌ SAI: Refactoring (extra changes)
- this._notification.success('Cập nhật thành công');
+ this._notificationService.showSuccess(this.tsb.t('msg-update-success', 'Cập nhật thành công'));
  ↑ Thay đổi hàm gọi → noise trong diff

// ❌ SAI: Cleanup (extra changes)
- const statusList = [
-   { label: 'Chờ duyệt', value: 1 },
-   { label: 'Đã duyệt', value: 2 }
- ];
+ const statusList = [{ label: this.tsb.t('status-pending', 'Chờ duyệt'), value: 1 }, { label: this.tsb.t('status-approved', 'Đã duyệt'), value: 2 }];
  ↑ Reformat lại → noise trong diff
```

---

## Before Execute

Agent MUST đọc trước:
1. `projects/scrm/patterns.md` → naming convention cho keys
2. `memory/pattern_da_ngon_ngu_inject.md` → inject() pattern
3. `memory/reference_translation_api.md` → API endpoints + DB access để check key
4. `projects/scrm/skills/multi-lang/baseline.md` → understand baseline
5. `projects/scrm/skills/multi-lang/keys-log.md` → avoid duplicate keys

## HTML Rules

### Text giữa tags
```html
<!-- BEFORE -->
<span>Đã duyệt</span>

<!-- AFTER -->
<span>{{ 'order-status-approved' | tsb : 'Đã duyệt' }}</span>
```

Rules:
- Chỉ wrap TEXT, không đụng `{{ expression }}` hoặc child tags
- Mixed: `Tổng: {{ total }}` → `{{ 'key' | tsb : 'Tổng' }}: {{ total }}`
- Key naming: `kebab-case` từ text (e.g., "Đã duyệt" → "order-status-approved")

### Attribute values
```html
<!-- BEFORE -->
<input placeholder="Nhập tên khách hàng">
<nz-select nzPlaceHolder="Chọn chi nhánh">
<app-input-number [PlaceHolder]="'Nhập số lượng'">

<!-- AFTER -->
<input [placeholder]="'customer-name-placeholder' | tsb : 'Nhập tên khách hàng'">
<nz-select [nzPlaceHolder]="'branch-placeholder' | tsb : 'Chọn chi nhánh'">
<app-input-number [PlaceHolder]="'quantity-placeholder' | tsb : 'Nhập số lượng'">
```

**IMPORTANT - Placeholder casing:**
| Element | Attribute | Case |
|---------|-----------|------|
| `<input>`, `<textarea>` | `[placeholder]` | lowercase |
| `<nz-date-picker>`, `<nz-select>`, `<nz-cascader>` | `[nzPlaceHolder]` | camelCase (nz prefix) |
| `<app-input-number>`, `<app-input-number-v2>` | `[PlaceHolder]` | PascalCase (uppercase P, H) |
| `<ng-select>` | `[placeholder]` | lowercase |

### nz-option
```html
<nz-option [nzLabel]="'order-status-pending' | tsb : 'Chờ duyệt'" nzValue="pending">
```

### Error messages, titles, tooltips
```html
nzErrorTip="Không được bỏ trống!"
→ [nzErrorTip]="'field-required' | tsb : 'Không được bỏ trống!'"

nzTitle="Thông tin chi tiết"
→ [nzTitle]="'detail-title' | tsb : 'Thông tin chi tiết'"

nzTooltipTitle="Sửa"
→ [nzTooltipTitle]="'action-edit' | tsb : 'Sửa'"
```

---

## TypeScript Rules

### Inject pattern (MUST)
```typescript
import { Component, OnInit, inject } from '@angular/core';
import { TranslateSBService } from 'src/app/shared/services/translate.sb.service';

export class MyComponent implements OnInit {
  private tsb = inject(TranslateSBService);  // ← Class level, NOT constructor
  
  statusOptions = [
    { label: this.tsb.t('order-status-pending', 'Chờ duyệt'), value: 'pending' }
  ];
}
```

### Notifications, messages
```typescript
// BEFORE
this._notification.success('Cập nhật thành công');

// AFTER
this._notification.success(this.tsb.t('msg-update-success', 'Cập nhật thành công'));
```

### Error messages
```typescript
// BEFORE
throw new Error('Không tìm thấy dữ liệu');

// AFTER
throw new Error(this.tsb.t('error-not-found', 'Không tìm thấy dữ liệu'));
```

### Dynamic dropdowns / enums
```typescript
// BEFORE
const statusList = [
  { name: 'Chờ duyệt', id: 1 },
  { name: 'Đã duyệt', id: 2 }
];

// AFTER
const statusList = [
  { name: this.tsb.t('order-status-pending', 'Chờ duyệt'), id: 1 },
  { name: this.tsb.t('order-status-approved', 'Đã duyệt'), id: 2 }
];
```

---

## Key Naming Convention

**Format:** `{module}-{entity}-{property}` (kebab-case)

**Examples:**
- `order-status-pending` (Order module, Status entity, Pending state)
- `customer-name-placeholder` (Customer module, Name field, Placeholder)
- `msg-update-success` (Message, Update action, Success)
- `error-validation-required` (Error, Validation, Required field)
- `action-edit` (Action, Edit)
- `action-delete` (Action, Delete)

**Guidelines:**
- **MUST Reuse existing keys** if Vietnamese text already in DB
- Avoid duplicate keys with same text
- Keep key short but descriptive
- Use `msg-` prefix for toast/notification messages
- Use `error-` prefix for error messages
- Use `action-` prefix for buttons/actions

---

## Rule: Reuse Existing Keys (Critical) — PHẢI Check DB

**MANDATORY process before creating new key:**

```
Vietnamese text: "Sửa"
  ↓
Step 1: Check keys-log.md (local memory)
  SELECT "Sửa" in projects/scrm/skills/multi-lang/keys-log.md
  → Found: 'action-edit' ✓
  → REUSE → Done
  
OR if NOT in keys-log.md:
  ↓
Step 2: PHẢI Query DB (non-negotiable!)
  SELECT [Key] FROM dbo.TranslationKey 
  WHERE [VietnameseValue] = 'Sửa'
  
  → IF FOUND in DB: Use existing key (update keys-log.md)
  → IF NOT FOUND in DB: Proceed to Step 3
  
  ↓
Step 3: Create NEW key (only if completely new)
  - Create key: 'action-edit'
  - INSERT INTO dbo.TranslationKey (Key, VietnameseValue)
  - Add to SQL batch
  - Update keys-log.md
```

**⚠️ CRITICAL:** 
- **PHẢI check DB trước INSERT**
- Nếu không check mà cứ INSERT → duplicate key trong DB → sai quy tắc
- Nếu text đó đã có key trong DB, PHẢI dùng key cũ

**Reason:**
- Consistency: same text = same key everywhere
- No duplication: prevent duplicate keys in DB
- Easier maintenance: 1 translation covers all places

**Checklist - Before Creating New Key:**
☐ Check keys-log.md (`grep 'Vietnamese text'`)  
☐ NOT found → Query DB (see below)
☐ Still NOT found in DB → Only then create new key  
☐ Generate INSERT for new key only  
☐ Update keys-log.md after commit

---

## How to Check Key in DB

**Complete reference:** `memory/reference_translation_api.md`

**Quick summary:**

### Step 1: Check keys-log.md (LOCAL)
```bash
grep 'Sửa' projects/scrm/skills/multi-lang/keys-log.md
# Found → REUSE that key
```

### Step 2: Query Database (if not in keys-log.md)
```sql
SELECT [KeyLang] FROM dbo.NgonNguTranslate 
WHERE [Lang] = 'vi' AND [ValueLang] = N'Sửa'

-- Connection: 10.0.161.13 SCRM_Core (READ ONLY)
-- Result:
--   - Found (e.g., 'action-edit') → REUSE existing key
--   - Empty → SAFE TO CREATE NEW KEY
```

### Step 3: Create SQL Insert (only if not found in DB)
Use safe `IF NOT EXISTS` pattern:
```sql
IF NOT EXISTS (SELECT 1 FROM dbo.NgonNguTranslate 
  WHERE [Lang] = 'vi' AND [KeyLang] = 'action-edit')
  INSERT INTO dbo.NgonNguTranslate (Id, Lang, KeyLang, ValueLang)
  VALUES (NEWID(), 'vi', 'action-edit', 'Sửa');
```

**See also:**
- Database schema: `memory/reference_translation_api.md` → Database Schema
- API endpoints: `memory/reference_translation_api.md` → LangController Endpoints
- Example workflow: `memory/reference_translation_api.md` → Agent Workflow

---

## Checklist - After Each File

☐ No hardcoded Vietnamese text remains  
☐ All `{{ 'key' | tsb : 'text' }}` follow rules (no orphan tags)  
☐ Placeholder attributes use correct casing (input vs nz-select vs custom)  
☐ `this.tsb.t('key', 'text')` in TS files  
☐ `this.tsb = inject(TranslateSBService)` at class level (NOT in constructor)  
☐ No duplicate keys in file  
☐ Keys follow naming convention  
☐ No syntax errors (check template compilation)  
☐ **Key collision check**: cùng 1 key không được map 2 text fallback khác nhau (kể cả khác `:`, hoa/thường, dấu cách) — xem [Common Pitfalls](#common-pitfalls--learned-from-code-review)
☐ **Trailing punctuation tách khỏi key**: text fallback KHÔNG chứa `:`/`,`/`.` ở cuối — tách ra ngoài HTML
☐ **Prefix nhất quán** trong cùng PR: chọn 1 trong (`lbl-` HOẶC `label-`), (`tbl-` HOẶC `table-`), (`tlt-` HOẶC `title-`) — không trộn  

---

## Auto-Review Requirements

After wrapping, agent MUST:
1. Check syntax (no orphan `{{ }}`, `[`, `]`)
2. Verify key names follow convention
3. Scan for remaining hardcoded Vietnamese (may miss some patterns)
4. **Run key-collision script**: extract mọi `'key' | tsb : 'text'` + `tsb.t('key', 'text')`, group theo key, FAIL nếu cùng key có >1 text fallback distinct
5. Report: X files, Y new keys, Z potential issues, **W collision pairs**

If issues found → fix immediately, don't wait for user report.

---

## Common Pitfalls — Learned from Code Review

Bài học từ review branch `nghiatd/FixLangCustomer` (2026-05-27). MUST tránh khi wrap text mới.

### Pitfall 1: Key collision do dấu câu trailing

**❌ SAI:**
```html
<!-- File A -->
<span>{{ 'lbl-nghi' | tsb : 'Nghỉ' }}</span>

<!-- File B -->
<span>{{ 'lbl-nghi' | tsb : 'Nghỉ:' }}</span>   <!-- ← cùng key, khác text -->
```
**Hậu quả:** DB chỉ lưu 1 value cho `lbl-nghi`. Sau khi user/translator dịch, 1 trong 2 chỗ sẽ thiếu hoặc thừa `:`.

**✅ ĐÚNG — Option A: tách `:` ra ngoài**
```html
<span>{{ 'lbl-nghi' | tsb : 'Nghỉ' }}:</span>
```
**✅ ĐÚNG — Option B: tách thành 2 key riêng nếu thật sự là 2 ngữ cảnh khác**
```html
<span>{{ 'lbl-nghi-heading' | tsb : 'Nghỉ' }}</span>
<span>{{ 'lbl-nghi-suffix' | tsb : 'Nghỉ:' }}</span>
```

Áp dụng tương tự cho: dấu cách trailing, capitalization khác (`Phút` vs `phút`, `Dịch vụ` vs `dịch vụ`), space khác (`Sản phẩm / Dịch vụ` vs `Sản phẩm/Dịch vụ`).

### Pitfall 2: Trộn prefix cho cùng loại

**❌ SAI:** trong cùng 1 PR dùng cả `lbl-` + `label-`, hoặc `tbl-` + `table-`, hoặc `tlt-` + `title-`.

**✅ ĐÚNG:** Chọn 1 prefix mỗi loại và dùng nhất quán **trong cùng PR**:
- Label: `label-` (recommend, theo baseline cũ)
- Table header: `table-` (recommend)
- Modal title: `title-` (recommend)
- Button: `btn-`
- Message/notification: `msg-`
- Error: `error-`
- Action: `action-`
- Option (dropdown): `option-`
- Placeholder: `plh-` hoặc `placeholder-` (chọn 1)
- Tooltip: `tooltip-`

> Nếu thấy baseline trộn lẫn rồi → giữ nguyên prefix file đang sửa, không tự "thống nhất" trong scope nhỏ (gây desync với DB).

### Pitfall 3: Thay đổi ngầm reference → copy

**❌ Trước:** `listKieuLoai: Array<any> = listKieuLoaiDMKhachHang;` (share reference với const)
**Sau:** `listKieuLoai: Array<any> = [];` + `ngOnInit: this.listKieuLoai = const.map(item => ({...item, ten: tsb.t(item.key, item.ten)}))`

→ Tạo behavior shift: trước mutate ảnh hưởng const + mọi instance; sau mỗi instance copy riêng.

**Rule:** Khi đổi từ reference → copy do wrap text, agent PHẢI:
1. Grep tất cả nơi mutate `listKieuLoai.push/splice/sort/reverse` → nếu có → cảnh báo user
2. Grep tất cả import của const đó → nếu có consumer khác → cảnh báo

### Pitfall 4: Field init order với `inject(TranslateSBService)`

**✅ Pattern chuẩn — giữ literal init, KHÔNG cần đổi sang empty + constructor fill:**
```ts
export class FooComponent {
  public tsb = inject(TranslateSBService);  // ← BẮT BUỘC là field đầu tiên
  
  lstLoaiThanhToan: any[] = [
    { ten: this.tsb.t('opt-tien-mat', 'Tiền mặt'), value: 1 },
    { ten: this.tsb.t('opt-chuyen-khoan', 'Chuyển khoản'), value: 2 },
  ];  // ← OK, this.tsb đã có
}
```

**Vì sao OK:**
1. `tsconfig` target ES2022+ → `useDefineForClassFields: true` (Angular default)
2. Class field initializers chạy **theo thứ tự khai báo**, mỗi field commit `this.[field]` ngay → field sau access được field trước
3. Angular ≥14: `inject()` là injection context hợp lệ trong class field initializer
4. Đã verify trong production code: [cong-viec-tab-filter.component.ts](file:///D:/PROJECT/Git/_SourceForCode/Group1/scrm_frontend/src/app/pages/work/danh-sach-cong-viec/cong-viec-tab-filter/cong-viec-tab-filter.component.ts) đang chạy pattern này

**❌ Pattern duy nhất bị fail** — `tsb` đến từ constructor param (legacy DI):
```ts
constructor(private tsb: TranslateSBService) {}  // ← constructor param
lstFoo = [{ ten: this.tsb.t(...) }];             // ← CRASH: tsb undefined
// Lý do: constructor params assign SAU field init khi useDefineForClassFields=true
```
→ Với `inject()` thì KHÔNG vướng vấn đề này.

**Rule cho agent:**
1. Luôn đặt `public tsb = inject(TranslateSBService)` là class field **đầu tiên** của class
2. Field array bên dưới giữ **literal init** với `this.tsb.t(...)` — KHÔNG cần chuyển sang empty + constructor fill (tạo noise diff thừa + behavior shift)
3. KHÔNG dùng pattern `constructor(private tsb: TranslateSBService)` cho TranslateSBService

> Nếu thấy code cũ dùng pattern empty + constructor fill (như branch `nghiatd/FixLangCustomer`) → không sửa lại trong scope wrap text, để bulk refactor sau.

### Pitfall 5: Const shape change

Nếu wrap text trong const yêu cầu thêm field (vd `key: string`):
1. **Update interface** cùng commit (nếu const có TypeScript interface)
2. **Grep mọi consumer** của const → kiểm tra không ai dựa vào shape cũ
3. **Comment-out items vẫn phải pass interface** hoặc xóa hẳn để khỏi gây nhầm

### Pitfall 6: Dynamic title concat — preserve whitespace

**❌ Risk:** `'NHÂN VIÊN: ' + tenNV` → `tsb.t('NHÂN VIÊN') + tenNV` (mất `: `)
**✅ Đúng:** `tsb.t('NHÂN VIÊN') + ': ' + tenNV` — tách dấu câu/space ra string literal

**Checklist khi wrap concat:**
- Đếm space/dấu câu trong text gốc
- Verify output `tsb.t(...) + literal + var` cho ra **chuỗi giống hệt** text gốc khi VN
- Nếu fallback chứa space/`:` cuối → tách ra literal

### Pitfall 7: Không wrap symbol đơn vị

KHÔNG wrap: `đ`, `VNĐ`, `VND`, `%`, `$`, `°C`, ký hiệu thuần — không cần dịch, gây noise key.

**OK wrap:** `điểm`, `phút`, `giờ`, `ngày` — đơn vị tiếng Việt cần dịch.

---

---

## SQL Generation (Safe Insert)

Generate INSERT script for all NEW keys (only if not exist):

```sql
-- Multi-lang Keys - 2026-04-24
-- Total: 45 new keys
-- Table: SCRM_Core.dbo.NgonNguTranslate
-- Only INSERT if key does not exist

IF NOT EXISTS (SELECT 1 FROM dbo.NgonNguTranslate WHERE Lang = 'vi' AND KeyLang = 'order-status-pending')
  INSERT INTO dbo.NgonNguTranslate (Id, Lang, KeyLang, ValueLang)
  VALUES (NEWID(), 'vi', 'order-status-pending', N'Chờ duyệt');

IF NOT EXISTS (SELECT 1 FROM dbo.NgonNguTranslate WHERE Lang = 'vi' AND KeyLang = 'order-status-approved')
  INSERT INTO dbo.NgonNguTranslate (Id, Lang, KeyLang, ValueLang)
  VALUES (NEWID(), 'vi', 'order-status-approved', N'Đã duyệt');

...
```

**OR use MERGE for cleaner syntax:**
```sql
MERGE INTO dbo.NgonNguTranslate AS target
USING (VALUES 
  ('vi', 'order-status-pending', N'Chờ duyệt'),
  ('vi', 'order-status-approved', N'Đã duyệt'),
  ...
) AS source (Lang, KeyLang, ValueLang)
ON target.Lang = source.Lang AND target.KeyLang = source.KeyLang
WHEN NOT MATCHED THEN
  INSERT (Id, Lang, KeyLang, ValueLang)
  VALUES (NEWID(), source.Lang, source.KeyLang, source.ValueLang);
```

**Save to:** `projects/scrm/skills/multi-lang/sql/{YYYY-MM-DD}.sql`

Example: `sql/2026-04-24.sql`

**User runs manually (NOT auto-execute).**

**Rules:**
- One file per day of execution
- Checks IF NOT EXISTS before INSERT
- Safe for re-running (idempotent)
- Contains only NEW keys (not existing ones)

---

## Trace Logging

Every file fixed → log entry in `projects/scrm/skills/multi-lang/trace/{date}.md`:
```markdown
## File: src/app/pages/order/order.component.ts
- Wrapped: 5 texts
- Keys created: order-status-pending, order-status-approved, ...
- Issues: none
```

Final report: Total X files, Y keys, Z blockers
