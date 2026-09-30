# Step-by-Step: Làm Multi-lang Cho 1 File

Hướng dẫn chi tiết wrap Vietnamese text thành multi-lang keys.

⚠️ **CRITICAL RULE: Minimal Changes Only**
- Chỉ wrap Vietnamese text
- KHÔNG refactor, cleanup, hoặc reformat code khác
- Khi `git diff`, chỉ thấy multi-lang changes
- Mục đích: diff sạch sẽ, dễ review

---

## 📋 Checklist Trước Khi Bắt Đầu

- ☐ Biết file path cần xử lý (vd: `src/app/pages/sale/sale.component.ts`)
- ☐ Có 2 files: `.ts` + `.html` hay chỉ `.ts` / `.html`?
- ☐ File này có sử dụng `TranslateSBService` chưa?

---

## Step 1: Scan File → Collect Vietnamese Text

### 1.1 HTML File (`.component.html`)

**Tìm tất cả Vietnamese text:**

```html
<!-- BEFORE -->
<button>Sửa</button>                    ← Text giữa tags
<input placeholder="Nhập tên">         ← Attribute value
<nz-select nzPlaceHolder="Chọn...">   ← nz-component attribute
<span [title]="'Xem chi tiết'">        ← Binding với raw string
nzErrorTip="Không được bỏ trống!"       ← nz-component prop
nzTitle="Tiêu đề"                      ← nz-component prop
```

**Script helper (search):**
```bash
# Grep Vietnamese text patterns
grep -E "(>.*[À-ỿ]|placeholder=.*[À-ỿ]|nz[A-Z].*=.*[À-ỿ])" file.html
```

### 1.2 TypeScript File (`.component.ts`)

**Tìm tất cả Vietnamese strings:**

```typescript
// BEFORE
this._notification.success('Cập nhật thành công');
this._notification.error('Không tìm thấy dữ liệu');
const statusList = [
  { label: 'Chờ duyệt', value: 1 },
  { label: 'Đã duyệt', value: 2 }
];
throw new Error('Lỗi xảy ra');
```

**Script helper:**
```bash
# Grep Vietnamese strings
grep -E "('[^']*[À-ỿ]|\"[^\"]*[À-ỿ])" file.ts
```

---

## Step 2: Check Existing Keys (MANDATORY — Before Creating New)

### 2.1 Step 1: Check keys-log.md

```bash
# Search in projects/scrm/skills/multi-lang/keys-log.md
grep -i "Sửa" keys-log.md
# Result: - `action-edit`: Sửa ✓ FOUND → REUSE → Done!
```

If found → **Use existing key, SKIP to wrapping (Step 4)**

### 2.2 Step 2: NOT in keys-log.md? → PHẢI Query DB

**⚠️ CRITICAL:** Không được skip bước này!

```sql
-- Query DB for existing key
SELECT [Key], [VietnameseValue] 
FROM dbo.TranslationKey 
WHERE [VietnameseValue] = 'Sửa'

-- Possible results:
-- 1) FOUND: action-edit | Sửa
--    → Use existing key, update keys-log.md
-- 2) NOT FOUND: (empty)
--    → Safe to create new key, proceed to Step 3
```

**⚠️ If you skip this and INSERT new key:**
- Text "Sửa" sẽ có 2 keys trong DB → DUPLICATE → sai quy tắc
- Other parts of code dùng key cũ, your code dùng key mới → inconsistency

**Decision Tree:**
```
Found in keys-log.md?
  YES → Use that key ✓
  NO  → Query DB?
        FOUND → Use DB key, update keys-log.md ✓
        NOT FOUND → Create new key (Step 3) ✓
```

---

## Step 3: Create Key Names (Only If NOT Existing)

### Naming Convention: `{module}-{entity}-{property}`

**Rules:**
- kebab-case (lowercase, dash separated)
- Reuse existing keys (check `keys-log.md`)
- Avoid duplicates with same text

**Examples:**

| Vietnamese | Key | Module | Entity | Property |
|------------|-----|--------|--------|----------|
| "Sửa" | `action-edit` | - | Action | Edit |
| "Xóa" | `action-delete` | - | Action | Delete |
| "Chờ duyệt" | `order-status-pending` | Order | Status | Pending |
| "Đã duyệt" | `order-status-approved` | Order | Status | Approved |
| "Nhập tên khách" | `customer-name-placeholder` | Customer | Name | Placeholder |
| "Cập nhật thành công" | `msg-update-success` | - | Message | Success |
| "Không tìm thấy" | `error-not-found` | - | Error | Not Found |
| "Không được bỏ trống" | `field-required` | - | Field | Required |
| "Chọn chi nhánh" | `branch-placeholder` | - | Branch | Placeholder |

---

## Step 4: Check/Setup TranslateSBService

### 4.1 Check if Already Imported

```typescript
// Run grep
grep "TranslateSBService" file.ts
```

### 3.2A If NOT Imported → Add Import

```typescript
import { Component, OnInit, inject } from '@angular/core';
import { TranslateSBService } from 'src/app/shared/services/translate.sb.service';  ← ADD THIS

@Component({
  selector: 'app-sale',
  templateUrl: './sale.component.html',
  styleUrls: ['./sale.component.scss']
})
export class SaleComponent implements OnInit {
  // ADD THIS
  private tsb = inject(TranslateSBService);
  
  constructor(...) { ... }
}
```

**⚠️ IMPORTANT:** 
- Use `inject()` NOT constructor injection
- Put `private tsb = inject(TranslateSBService);` at class level (after `export class`)
- Verify no duplicate: `grep "private tsb" file.ts` → should return 1 line only

### 3.2B If Already Imported

✅ No change needed, continue to Step 4

---

## Step 4: Wrap HTML Text

### 4.1 Text Between Tags

**BEFORE:**
```html
<button>Sửa</button>
<span class="status">Đã duyệt</span>
<h3>Chi tiết đơn hàng</h3>
```

**AFTER:**
```html
<button>{{ 'action-edit' | tsb : 'Sửa' }}</button>
<span class="status">{{ 'order-status-approved' | tsb : 'Đã duyệt' }}</span>
<h3>{{ 'order-details-title' | tsb : 'Chi tiết đơn hàng' }}</h3>
```

**Rule:** Chỉ wrap TEXT, không touch `{{ expression }}` hoặc child tags

### 4.2 Placeholder Attributes

**⚠️ CRITICAL - Casing Rules:**

#### `<input>` / `<textarea>` → `[placeholder]` (lowercase)
```html
<!-- BEFORE -->
<input placeholder="Nhập tên khách hàng">

<!-- AFTER -->
<input [placeholder]="'customer-name-placeholder' | tsb : 'Nhập tên khách hàng'">
```

#### `<nz-select>`, `<nz-date-picker>`, etc. → `[nzPlaceHolder]` (camelCase)
```html
<!-- BEFORE -->
<nz-select nzPlaceHolder="Chọn chi nhánh">

<!-- AFTER -->
<nz-select [nzPlaceHolder]="'branch-placeholder' | tsb : 'Chọn chi nhánh'">
```

#### `<app-input-number>` custom → `[PlaceHolder]` (PascalCase)
```html
<!-- BEFORE -->
<app-input-number [PlaceHolder]="'Nhập số lượng'">

<!-- AFTER -->
<app-input-number [PlaceHolder]="'quantity-placeholder' | tsb : 'Nhập số lượng'">
```

### 4.3 Other Attributes

```html
<!-- nzErrorTip -->
[nzErrorTip]="'field-required' | tsb : 'Không được bỏ trống!'"

<!-- nzTitle -->
[nzTitle]="'order-details-title' | tsb : 'Chi tiết đơn hàng'"

<!-- nzTooltipTitle -->
[nzTooltipTitle]="'action-edit' | tsb : 'Sửa'"

<!-- nzLabel (on nz-option) -->
<nz-option [nzLabel]="'order-status-pending' | tsb : 'Chờ duyệt'" [nzValue]="1">
```

### 4.4 Mixed Content

```html
<!-- BEFORE -->
Tổng tiền: {{ total }}

<!-- AFTER -->
{{ 'order-total-label' | tsb : 'Tổng tiền' }}: {{ total }}
```

---

## Step 5: Wrap TypeScript Strings

### 5.1 Notifications/Messages

**BEFORE:**
```typescript
this._notification.success('Cập nhật thành công');
this._notification.error('Cập nhật thất bại');
this._notification.warning('Chưa chọn dòng nào');
```

**AFTER:**
```typescript
this._notification.success(this.tsb.t('msg-update-success', 'Cập nhật thành công'));
this._notification.error(this.tsb.t('msg-update-error', 'Cập nhật thất bại'));
this._notification.warning(this.tsb.t('msg-no-selection', 'Chưa chọn dòng nào'));
```

### 5.2 Dropdowns/Options Lists

**BEFORE:**
```typescript
const statusList = [
  { label: 'Chờ duyệt', value: 1 },
  { label: 'Đã duyệt', value: 2 },
  { label: 'Bị từ chối', value: 3 }
];
```

**AFTER:**
```typescript
const statusList = [
  { label: this.tsb.t('order-status-pending', 'Chờ duyệt'), value: 1 },
  { label: this.tsb.t('order-status-approved', 'Đã duyệt'), value: 2 },
  { label: this.tsb.t('order-status-rejected', 'Bị từ chối'), value: 3 }
];
```

### 5.3 Error Messages

**BEFORE:**
```typescript
throw new Error('Không tìm thấy dữ liệu');
if (!data) {
  throw new Error('ID không hợp lệ');
}
```

**AFTER:**
```typescript
throw new Error(this.tsb.t('error-not-found', 'Không tìm thấy dữ liệu'));
if (!data) {
  throw new Error(this.tsb.t('error-invalid-id', 'ID không hợp lệ'));
}
```

### 5.4 Dynamic Properties

**BEFORE:**
```typescript
title: string = 'Quản lý bán hàng';
subtitle: string = 'Danh sách các đơn hàng đã tạo';
```

**AFTER:**
```typescript
title: string = this.tsb.t('sale-page-title', 'Quản lý bán hàng');
subtitle: string = this.tsb.t('sale-page-subtitle', 'Danh sách các đơn hàng đã tạo');
```

---

## Step 6: Verify & QA

### 6.1 Syntax Check

☐ No orphan `{{ }}`, `[`, `]` in HTML  
☐ No unclosed parenthesis in TS  
☐ Placeholder attributes use correct casing  

### 6.2 Key Name Check

☐ All keys follow naming convention  
☐ No duplicate keys with same text  
☐ Reused existing keys (if applicable)  

### 6.3 Compile Check

```bash
# Test compile (if in working directory)
ng build --configuration development
# or
npm run build
```

Should have NO syntax errors related to:
- `NG2339: Property does not exist`
- `NG8002: Invalid attribute` (placeholder casing issue)
- Template errors

### 6.4 Verify No Hardcoded Vietnamese Remains

```bash
grep -E "[À-ỿ]" file.html
grep -E "('[^']*[À-ỿ]|\"[^\"]*[À-ỿ])" file.ts
```

Should return **0 results** (or only in comments)

---

## Step 7: Generate SQL Insert Script

Collect all NEW keys (không trong DB) và tạo script INSERT safe:

### Format: IF NOT EXISTS (Recommended)

```sql
-- Multi-lang Keys - 2026-04-24
-- Total: 15 new keys
-- Only INSERT if key does not exist

IF NOT EXISTS (SELECT 1 FROM dbo.TranslationKey WHERE Key = 'action-edit')
  INSERT INTO dbo.TranslationKey (Key, VietnameseValue, CreatedDate)
  VALUES ('action-edit', 'Sửa', GETDATE());

IF NOT EXISTS (SELECT 1 FROM dbo.TranslationKey WHERE Key = 'order-status-pending')
  INSERT INTO dbo.TranslationKey (Key, VietnameseValue, CreatedDate)
  VALUES ('order-status-pending', 'Chờ duyệt', GETDATE());

IF NOT EXISTS (SELECT 1 FROM dbo.TranslationKey WHERE Key = 'order-status-approved')
  INSERT INTO dbo.TranslationKey (Key, VietnameseValue, CreatedDate)
  VALUES ('order-status-approved', 'Đã duyệt', GETDATE());

...
```

### OR use MERGE (Cleaner)

```sql
MERGE INTO dbo.TranslationKey AS target
USING (VALUES 
  ('action-edit', N'Sửa'),
  ('order-status-pending', N'Chờ duyệt'),
  ('order-status-approved', N'Đã duyệt'),
  ...
) AS source (Key, VietnameseValue)
ON target.Key = source.Key
WHEN NOT MATCHED THEN
  INSERT (Key, VietnameseValue, CreatedDate)
  VALUES (source.Key, source.VietnameseValue, GETDATE());
```

### Save to:
```
projects/scrm/skills/multi-lang/sql/{YYYY-MM-DD}.sql
Example: projects/scrm/skills/multi-lang/sql/2026-04-24.sql
```

**Rules:**
- One file per day → `2026-04-24.sql`, `2026-04-25.sql`, etc.
- Only NEW keys (not existing ones in DB)
- Safe to re-run (idempotent with IF NOT EXISTS / MERGE)
- User runs manually (NOT auto-execute)

---

## Step 8: Log Trace

Create file: `projects/scrm/skills/multi-lang/trace/{YYYY-MM-DD}.md`

```markdown
## File: src/app/pages/sale/sale.component.ts

- Wrapped: 8 texts (notifications, messages, dropdowns)
- Keys created: msg-update-success, msg-update-error, order-status-pending, ...
- HTML: src/app/pages/sale/sale.component.html
  - Wrapped: 12 attributes + 3 texts
  - Keys: action-edit, action-delete, customer-name-placeholder, ...
- Issues: None

**Total:** 1 file, 20 keys
```

---

## Complete Example

### BEFORE

**sale.component.ts:**
```typescript
import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-sale',
  templateUrl: './sale.component.html',
  styleUrls: ['./sale.component.scss']
})
export class SaleComponent implements OnInit {
  statusList = [
    { label: 'Chờ duyệt', value: 1 },
    { label: 'Đã duyệt', value: 2 }
  ];

  constructor(private notif: NzNotificationService) {}

  save() {
    this.notif.success('Cập nhật thành công');
  }
}
```

**sale.component.html:**
```html
<div class="header">
  <h2>Quản lý bán hàng</h2>
  <input placeholder="Tìm kiếm đơn hàng">
</div>

<div class="content">
  <button>Sửa</button>
  <button>Xóa</button>
  
  <nz-select nzPlaceHolder="Chọn trạng thái">
    <nz-option nzLabel="Chờ duyệt" [nzValue]="1"></nz-option>
    <nz-option nzLabel="Đã duyệt" [nzValue]="2"></nz-option>
  </nz-select>
</div>
```

### AFTER

**sale.component.ts:**
```typescript
import { Component, OnInit, inject } from '@angular/core';
import { TranslateSBService } from 'src/app/shared/services/translate.sb.service';

@Component({
  selector: 'app-sale',
  templateUrl: './sale.component.html',
  styleUrls: ['./sale.component.scss']
})
export class SaleComponent implements OnInit {
  private tsb = inject(TranslateSBService);

  statusList = [
    { label: this.tsb.t('order-status-pending', 'Chờ duyệt'), value: 1 },
    { label: this.tsb.t('order-status-approved', 'Đã duyệt'), value: 2 }
  ];

  constructor(private notif: NzNotificationService) {}

  save() {
    this.notif.success(this.tsb.t('msg-update-success', 'Cập nhật thành công'));
  }
}
```

**sale.component.html:**
```html
<div class="header">
  <h2>{{ 'sale-page-title' | tsb : 'Quản lý bán hàng' }}</h2>
  <input [placeholder]="'order-search-placeholder' | tsb : 'Tìm kiếm đơn hàng'">
</div>

<div class="content">
  <button>{{ 'action-edit' | tsb : 'Sửa' }}</button>
  <button>{{ 'action-delete' | tsb : 'Xóa' }}</button>
  
  <nz-select [nzPlaceHolder]="'status-placeholder' | tsb : 'Chọn trạng thái'">
    <nz-option [nzLabel]="'order-status-pending' | tsb : 'Chờ duyệt'" [nzValue]="1"></nz-option>
    <nz-option [nzLabel]="'order-status-approved' | tsb : 'Đã duyệt'" [nzValue]="2"></nz-option>
  </nz-select>
</div>
```

---

## Summary Checklist

✅ Scan HTML: text + attributes  
✅ Scan TS: notifications, dropdowns, errors, strings  
✅ Create key names (naming convention)  
✅ Import TranslateSBService (if not exist)  
✅ Wrap HTML text: `{{ 'key' | tsb : 'text' }}`  
✅ Wrap HTML attributes: correct casing!  
✅ Wrap TS strings: `this.tsb.t('key', 'text')`  
✅ No hardcoded Vietnamese remains  
✅ Compile check: no syntax errors  
✅ Generate SQL + log trace  

**Done!** File is fully multi-lang ready.
