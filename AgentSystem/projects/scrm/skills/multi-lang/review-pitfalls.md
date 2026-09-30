---
name: feedback-multilang-review-key-collision
description: Khi review thay đổi đa ngôn ngữ — quét key collision và ưu tiên không vỡ logic/text cũ
metadata:
  type: feedback
---

Khi review branch đa ngôn ngữ (wrap text → `tsb.t()` / `| tsb` pipe), 2 ưu tiên user nhấn mạnh: **(1) không ảnh hưởng logic cũ, (2) không mất/sai text value cũ**.

**Why:** Wrap đa ngôn ngữ tưởng vô hại nhưng dễ phát sinh 2 lỗi câm: key collision và vỡ thứ tự khởi tạo field.

**How to apply:**

### 1. Quét key collision (script bắt buộc)

Extract mọi cặp `key → text` từ diff (cả `'key' | tsb : 'text'` lẫn `tsb.t('key', 'text')`), group theo key:

```bash
# HTML
git diff <base>..<branch> | grep -E "^\+" | grep -oE "'[a-z0-9_-]+' \| tsb : '[^']*'" | sort -u > /tmp/html.txt
# TS
git diff <base>..<branch> | grep -E "^\+" | grep -oE "tsb\.t\('[a-z0-9_-]+', '[^']*'\)" | sort -u > /tmp/ts.txt
# Normalize + find dup
cat /tmp/html.txt /tmp/ts.txt | sed -E "s/.*'([a-z0-9_-]+)'.*'([^']*)'.*/\1|\2/" | sort -u | awk -F'|' '{print $1}' | sort | uniq -d
```

Loại lỗi cần FAIL build:
- **Cùng key, 2 text fallback khác nhau** → DB chỉ lưu 1 value, 1 phía sai sau khi dịch. Nguy hiểm nhất khi 1 phía có `{0}` placeholder, phía kia không.
- **Khác biệt câm hay gặp** (đã thấy trong nhánh `nghiatd/FixLangCustomer`):
  - Dấu `:` trailing: `'Nghỉ'` vs `'Nghỉ:'`
  - Hoa/thường: `'Phút'` vs `'phút:'`, `'Sản phẩm / Dịch vụ'` vs `'Sản phẩm / dịch vụ'`
  - Space khác: `'Sản phẩm / Dịch vụ'` vs `'Sản phẩm/Dịch vụ'`
- **Cùng text, nhiều key** → cảnh báo nhẹ (duplication, dịch trùng).

### 2. Tách dấu câu trailing khỏi text fallback

Khi text gốc có `:` cuối → tách ra HTML thay vì nhét vào fallback:

```html
<!-- ❌ SAI -->
<span>{{ 'lbl-nghi' | tsb : 'Nghỉ:' }}</span>
<!-- ✅ ĐÚNG -->
<span>{{ 'lbl-nghi' | tsb : 'Nghỉ' }}:</span>
```

### 3. Prefix nhất quán trong cùng PR

Trộn `lbl-` + `label-` (cùng nghĩa "label") hoặc `tbl-` + `table-` trong cùng PR = code smell. Chọn 1, giữ nguyên cho cả PR.

### 4. Field init order — `inject()` đặt là field đầu tiên là ĐỦ

Với `useDefineForClassFields: true` (default khi target ES2022+):
- Class field initializers chạy **theo thứ tự khai báo**, mỗi field commit `this.[field]` ngay
- Angular ≥14: `inject()` là injection context hợp lệ trong field initializer

→ Pattern an toàn nhất, **giữ literal init**, không cần empty + constructor fill:
```ts
public tsb = inject(TranslateSBService);  // ← field đầu tiên
lstFoo = [{ ten: this.tsb.t('key', 'Foo'), value: 1 }];  // ← OK
```

**Chỉ fail khi** dùng constructor param thay cho `inject()`: `constructor(private tsb: TranslateSBService)` → field init dùng `this.tsb` sẽ thấy undefined (constructor params assign SAU field init).

**KHÔNG khuyến nghị** pattern `lstFoo = []` + fill trong constructor — gây noise diff + behavior shift (reference→copy, race với `@Input` setter). Đã verify production code [cong-viec-tab-filter.component.ts](D:\PROJECT\Git\_SourceForCode\Group1\scrm_frontend\src\app\pages\work\danh-sach-cong-viec\cong-viec-tab-filter\cong-viec-tab-filter.component.ts) chạy ổn pattern literal init.

### 5. Chỉ wrap phần hiển thị

- `id`/`nzValue`/`value`/giá trị nghiệp vụ phải giữ nguyên (`{value: 1}` không đổi sang `{value: '1'}`)
- Template literal `` `..${x}..` `` → giữ template literal hoặc tách: `tsb.t('key', 'text') + ' ' + x`
- Đơn vị nghiệp vụ Việt (`điểm`, `phút`, `giờ`) → wrap; ký hiệu thuần (`đ`, `VNĐ`, `%`, `$`, `°C`) → KHÔNG wrap

### 6. Reference → copy behavior shift

Khi đổi `listX = sharedConst` → `listX = []` + `listX = sharedConst.map(...)`:
- Cảnh báo nếu code có `listX.push/splice/sort` (mutation behavior khác)
- Cảnh báo nếu const có consumer khác (shape change của const ảnh hưởng)

### 7. Dynamic concat — preserve whitespace

`'NHÂN VIÊN: ' + tenNV` → `tsb.t('NHÂN VIÊN') + ': ' + tenNV` (tách `: `). Verify output VN **giống hệt** text gốc.

Liên quan: [[projects/scrm/skills/multi-lang/agent.md]] (rules đã ghi vào "Common Pitfalls").
