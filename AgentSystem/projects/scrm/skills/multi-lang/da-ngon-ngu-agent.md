# Agent: Đa ngôn ngữ (Multi-language)

Agent chuyên xử lý đa ngôn ngữ cho SCRM frontend (Angular 15 + ng-zorro).

## Khi nào dùng

User yêu cầu: "làm đa ngôn ngữ", "dịch module X", "translate", hoặc tương đương.

## Cách chạy

1. Xác nhận module/folder cần xử lý
2. Scan file HTML + TS trong folder
3. Chia batch, spawn agent song song (mỗi batch ~10-15 components)
4. QA sau khi xong
5. Gen SQL keys (KHÔNG commit SQL vào git)

## Rules — HTML

### Text giữa tags
```html
<!-- BEFORE -->
>Vietnamese text</tag>
<!-- AFTER -->
>{{ 'key' | tsb : 'Vietnamese text' }}</tag>
```

Chỉ wrap phần TEXT, không đụng `{{ expression }}` hoặc child tags.
Mixed content: `Tổng: {{ total }}` → `{{ 'key' | tsb : 'Tổng' }}: {{ total }}`

### Attribute values
```html
<!-- BEFORE -->
placeholder="Vietnamese text"
nzErrorTip="Không được bỏ trống!"
nzTitle="Tiêu đề"
nzTooltipTitle="Sửa"
nzLabel="Hoạt động"

<!-- AFTER -->
[placeholder]="'key' | tsb : 'Vietnamese text'"
[nzErrorTip]="'key' | tsb : 'Không được bỏ trống!'"
[nzTitle]="'key' | tsb : 'Tiêu đề'"
[nzTooltipTitle]="'key' | tsb : 'Sửa'"
[nzLabel]="'key' | tsb : 'Hoạt động'"
```

### Placeholder — quy tắc QUAN TRỌNG
| Element | Attribute | Ví dụ |
|---------|-----------|-------|
| `<input>`, `<textarea>` (kể cả có `nz-input`) | `[placeholder]` (lowercase) | `[placeholder]="'key' \| tsb : 'text'"` |
| `<nz-date-picker>`, `<nz-range-picker>`, `<nz-select>`, `<nz-cascader>`, `<nz-time-picker>` | `[nzPlaceHolder]` | `[nzPlaceHolder]="'key' \| tsb : 'text'"` |
| `<app-input-number>`, `<app-input-number-v2>` (custom component) | `[PlaceHolder]` (viết hoa P, H) | `[PlaceHolder]="'key' \| tsb : 'text'"` |
| `<ng-select>` (third-party) | `[placeholder]` (lowercase) | `[placeholder]="'key' \| tsb : 'text'"` |

- KHÔNG dùng `[nzPlaceHolder]` trên `<input>` → lỗi NG8002
- KHÔNG dùng `[PlaceHolder]` trên `<input>`, `<textarea>`, `<ng-select>` → lỗi NG8002
- `[PlaceHolder]` viết hoa CHỈ dùng cho `<app-input-number>`

### nz-option
```html
<nz-option [nzLabel]="'key' | tsb : 'Vietnamese'" nzValue="...">
```

### Binding đã có [] nhưng value là string Vietnamese
```html
<!-- BEFORE -->
[nzTitle]="'Vietnamese raw string'"
<!-- AFTER -->
[nzTitle]="'key' | tsb : 'Vietnamese raw string'"
```

## Rules — TypeScript

### Notification/message
```typescript
// BEFORE
this._notification.showMessage('success', 'Cập nhật thành công');
// AFTER
this._notification.showMessage('success', this.tsb.t('msg-cap-nhat-thanh-cong', 'Cập nhật thành công'));
```

### GridColumn header
```typescript
header: this.tsb.t('table-ten-nhan-vien', 'Tên nhân viên'),
```

### Modal/Confirm dialog
```typescript
nzTitle: this.tsb.t('title-them-moi', 'Thêm mới'),
nzContent: this.tsb.t('msg-ban-co-chac-muon-xoa', 'Bạn có chắc muốn xóa?'),
nzOkText: this.tsb.t('btn-dong-y', 'Đồng ý'),
nzCancelText: this.tsb.t('btn-khong', 'Không'),
```

### Import + Inject Pattern

**REQUIRED: Dùng `inject()` pattern (Angular 14+)**

```typescript
// Import
import { Component, ..., inject } from '@angular/core';
import { TranslateSBService } from 'src/app/shared/services/translate.sb.service';

// ✅ CORRECT: Class-level injection
export class MyComponent {
  private tsb = inject(TranslateSBService);
  
  constructor(
    private service1: Service1,
    private service2: Service2
    // KHÔNG có: private tsb: TranslateSBService
  ) {}
}
```

**Why inject() not constructor injection:**
- Constructor injection chạy TRƯỚC class property initializers hoàn thành
- Nên `this.tsb` undefined khi init dropdown/label arrays ở class level
- `inject()` hoạt động trong Angular injection context → service available ngay

**3-Step Fix Pattern (áp dụng khi add/refactor):**
1. Thêm `inject` vào import từ `@angular/core`
2. Thêm `private tsb = inject(TranslateSBService);` ở class level (ngay sau `export class`)
3. Xóa `private tsb: TranslateSBService` từ constructor params

**Example:**
```typescript
// BEFORE (❌ lỗi — tsb undefined ở dropdowns)
export class ConfigComponent {
  constructor(private tsb: TranslateSBService) {}
  
  ddlStatus = [
    { name: this.tsb.t('option-dang-xu-ly', 'Đang xử lý'), id: '1' }  // ❌ undefined
  ];
}

// AFTER (✅ đúng — tsb có sẵn)
export class ConfigComponent {
  private tsb = inject(TranslateSBService);
  
  ddlStatus = [
    { name: this.tsb.t('option-dang-xu-ly', 'Đang xử lý'), id: '1' }  // ✅ works
  ];
  
  constructor() {}
}
```

**Edge case — Constructor parameter comments:**
- Nếu file có `// elementRef: ElementRef,` (comment parameter), xóa nó khi xóa `private tsb`
- Đảm bảo closing `)` của constructor không bị lẫn vào comment

**IMPORTANT — Verify no duplicates:**
- ✅ Scan sau khi inject: KHÔNG được có cả `private tsb = inject()` và `private tsb: TranslateSBService` trong cùng 1 file
- Nếu có duplicate → xóa dòng `private tsb: TranslateSBService` từ constructor params
- Script có thể miss nếu param không có dấu `,` ở cuối hoặc là param cuối cùng
- QA check: `grep -n "private tsb" file.ts` phải chỉ có 1 dòng (inject version)

## Key naming convention

| Prefix | Dùng cho |
|--------|----------|
| `btn-` | Button, action text |
| `title-` | Page/dialog/card titles |
| `table-` | Table column headers |
| `input-` | Placeholders |
| `label-` | Form labels, field labels |
| `msg-` | Notification, confirm, validation messages |
| `tab-` | Tab labels |
| `tooltip-` | Tooltip text |
| `option-` | Dropdown option labels |
| `help-` | Help text, description |

### Slugify
- Lowercase, bỏ dấu tiếng Việt (à→a, đ→d)
- Khoảng trắng → hyphen, max 50 ký tự
- VD: "Tìm kiếm" → `btn-tim-kiem`

## Reusable keys (đã có trong DB, KHÔNG tạo SQL mới)

| Key | Value |
|-----|-------|
| `btn-search` | Tìm kiếm |
| `btn-add` | Thêm mới |
| `btn-export-excel` | Export To Excel |
| `btn-sua` | Sửa |
| `btn-xoa` | Xóa |
| `btn-dong` | Đóng |
| `btn-luu` | Lưu |
| `btn-huy` | Hủy |
| `btn-cap-nhat` | Cập nhật |
| `btn-xac-nhan` | Xác nhận |
| `btn-dong-y` | Đồng ý |
| `btn-reset` | Làm mới |
| `title-xac-nhan` | Xác nhận |
| `title-them-moi` | Thêm mới |
| `title-chinh-sua` | Chỉnh sửa |
| `title-xoa` | Xóa |
| `msg-cap-nhat-thanh-cong` | Cập nhật thành công |
| `msg-them-moi-thanh-cong` | Thêm mới thành công |
| `msg-xoa-thanh-cong` | Xóa thành công |
| `msg-ban-co-chac-chan-muon-xoa` | Bạn có chắc chắn muốn xóa? |
| `msg-vui-long-chon` | Vui lòng chọn |
| `msg-khong-duoc-bo-trong` | Không được bỏ trống! |
| `label-trang-thai` | Trạng thái |
| `label-ngay-tao` | Ngày tạo |
| `label-nguoi-tao` | Người tạo |
| `label-ghi-chu` | Ghi chú |
| `label-ten` | Tên |
| `label-ma` | Mã |
| `label-mo-ta` | Mô tả |
| `label-stt` | STT |
| `label-hanh-dong` | Hành động |
| `label-tat-ca` | Tất cả |
| `table-stt` | STT |
| `table-hanh-dong` | Hành động |

## KHÔNG được làm

1. **CHỈ thay đổi liên quan đến ngôn ngữ** — không sửa BẤT KỲ thứ gì khác (không format lại code, không refactor, không clean up, không sửa typo code, không thêm/xóa dòng trống)
2. **KHÔNG xóa comment code cũ** (`<!-- -->`, `//`) — giữ nguyên 100%
3. **KHÔNG thay đổi logic** — chỉ wrap text display
4. **KHÔNG wrap text dùng làm LOGIC** — Nếu 1 chuỗi tiếng Việt dùng để so sánh (`if (x === "Tiền mặt")`, `.includes("Hàng chờ")`, `filter(i => i.tenPhuongThuc === "Chuyển khoản")`), assign vào field data để so sánh sau, hoặc dùng làm enum value → KHÔNG wrap. Wrap sẽ phá logic.
   - Nhận biết: search trong file xem chuỗi đó có xuất hiện trong `===`, `!==`, `.includes(`, `.indexOf(`, `.filter(`, `.find(`, `switch/case`, so sánh với biến khác hay không
   - Nếu có → để nguyên, không dịch
5. **KHÔNG đụng**: `.join()`, `.indexOf()`, `.split()`, `.includes()`, path `assets/`, tiền tệ VNĐ/VND/₫, import path, templateUrl, styleUrls, selector, CSS class, nzValue binding, ngModel binding, console.log, variable name, enum, regex
4. **KHÔNG phá syntax HTML** — giữ `>` đóng tag khi thay attribute
5. **KHÔNG commit SQL vào git** — chỉ gen file local, chạy thủ công
6. **KHÔNG dùng escaped quotes** `\"` trong binding — nếu fallback text chứa `'`, bỏ dấu `'` hoặc dùng cách khác

## QA checklist (chạy sau khi xong)

```bash
# 1. Missing > sau tsb attribute
find src/app/pages/MODULE -name "*.html" -exec awk 'prev ~ /tsb.*"$/ && /^\s*</ {print FILENAME ":" NR-1} {prev=$0}' {} \;

# 2. [nzPlaceHolder] trên <input>
grep -rn '\[nzPlaceHolder\]' src/app/pages/MODULE --include="*.html" -B3 | grep "<input "

# 3. this.tsb.t() thiếu inject
for f in $(grep -rl "this\.tsb\.t(" src/app/pages/MODULE --include="*.ts"); do
  grep -q "private tsb" "$f" || echo "MISSING: $f"
done

# 4. Escaped quotes
grep -rn '\\\"' src/app/pages/MODULE --include="*.html" | grep "tsb"

# 5. Logic thay đổi
git diff -- "*.ts" | awk '/^-\s+(on|load|get|show|apply|save)[A-Z]/{print}'

# 6. Comment bị xóa
git diff -- "*.html" | grep "^-.*<!--"

# 7. TS files có Vietnamese string chưa wrap tsb (BẮT BUỘC - dễ bị miss nhất)
for f in $(find src/app/pages/MODULE -name "*.ts" ! -name "*.spec.ts" ! -name "*.module.ts" ! -name "*.service.ts"); do
  not_tsb=$(grep "[àáảãạăằắẳẵặâầấẩẫậèéẻẽẹêềếểễệìíỉĩịòóỏõọôồốổỗộơờớởỡợùúủũụưừứửữựỳýỷỹỵđ]" "$f" 2>/dev/null | grep -cv "tsb\|//\|import\|\*")
  [ "$not_tsb" -ge 3 ] && echo "MISSING: $f ($not_tsb lines)"
done

# 8. Key conflict — cùng key nhưng khác value (BẮT BUỘC fix trước commit)
# HTML
grep -roh "'[a-z0-9-]*' | tsb : '[^']*'" src/app/pages/MODULE --include="*.html" | sed "s/' | tsb : '/\t/;s/^'//;s/'$//" | sort -t$'\t' -k1,1 | awk -F'\t' 'prev==$1 && prevv!=$2 {print "CONFLICT: key=" $1 " val1=" prevv " val2=" $2} {prev=$1; prevv=$2}'
# TS
grep -roh "tsb\.t('[a-z0-9-]*', '[^']*')" src/app/pages/MODULE --include="*.ts" | sed "s/tsb\.t('//;s/', '/\t/;s/')//" | sort -t$'\t' -k1,1 | awk -F'\t' 'prev==$1 && prevv!=$2 {print "CONFLICT: key=" $1 " val1=" prevv " val2=" $2} {prev=$1; prevv=$2}'

# 8. Sai import path (phải là src/app/shared/services/translate.sb.service)
grep -rn "import.*TranslateSBService" src/app/pages/MODULE --include="*.ts" | grep -v "src/app/shared/services/translate.sb.service"

# 9. [placeholder] trên app-input-number (phải dùng [PlaceHolder] viết hoa)
grep -rn '\[placeholder\]' src/app/pages/MODULE --include="*.html" | grep "app-input-number"
```

### Quy tắc key-value
- **1 key = 1 value duy nhất** trên toàn bộ codebase
- Cùng text hiển thị nhưng khác context → tạo key khác (VD: `btn-dong` = Đóng, `btn-thoat` = Thoát)
- Không dùng dấu `!` `...` `:` cuối value trừ khi cần thiết — thống nhất 1 kiểu
- Viết hoa/thường phải nhất quán (VD: "Ngày tạo" không "Ngày Tạo")
- Typo trong value phải fix ngay, không để 2 value khác nhau cho cùng key

## DB

- Server staging: 10.0.161.13
- DB: SCRM_Core
- Table: NgonNguTranslate
- Columns: Id (NEWID()), Lang, KeyLang, ValueLang, NguoiTaoUserName='04', NgayTao=GETDATE(), IsDelete=0

```sql
IF NOT EXISTS (SELECT 1 FROM SCRM_Core.dbo.NgonNguTranslate WHERE KeyLang = 'key' AND Lang = 'vi')
  INSERT INTO SCRM_Core.dbo.NgonNguTranslate (Id, Lang, KeyLang, ValueLang, NguoiTaoUserName, NgayTao, IsDelete)
  VALUES (NEWID(), 'vi', 'key', N'Value', '04', GETDATE(), 0);
```
