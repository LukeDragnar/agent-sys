---
name: ngzorro-disable-attribute
description: ng-zorro nz-button dùng [disabled] native HTML attribute, KHÔNG có [nzDisabled]. Verify ng-zorro API spec trước khi dùng directive, đừng đoán.
metadata:
  type: feedback
---

# ng-zorro `[disabled]` vs `[nzDisabled]` — phân biệt

**Rule:** Trên `<button nz-button>` (ng-zorro), KHÔNG có `[nzDisabled]`. Phải dùng `[disabled]` (native HTML attribute).

**Why:**
- `[nzDisabled]` là input của 1 số directive ng-zorro chuyên biệt (`nz-checkbox`, `nz-radio`, `nz-input-group`, …) chứ KHÔNG phải universal.
- `nz-button` là HTML `<button>` enhanced — dùng native `disabled`.
- Đoán sai → compile error NG8002 "Can't bind to 'nzDisabled' since it isn't a known property of 'button'".

**Counter-example đã gặp (session 2026-05-29):**
```html
<!-- ❌ SAI — compile fail -->
<button nz-button [nzDisabled]="!isHangVeKho(item)" ...>

<!-- ✅ ĐÚNG -->
<button nz-button [disabled]="!isHangVeKho(item)" ...>
```

**How to apply:**

| Component | Attribute disable |
|-----------|-------------------|
| `<button nz-button>` | `[disabled]` ✅ native |
| `<input nz-input>` | `[disabled]` ✅ native |
| `<nz-checkbox>` | `[nzDisabled]` ✅ directive input |
| `<nz-radio>` | `[nzDisabled]` ✅ |
| `<nz-select>` | `[nzDisabled]` ✅ |
| `<nz-switch>` | `[nzDisabled]` ✅ |
| `<nz-input-group>` | `[nzDisabled]` ✅ |
| `<nz-date-picker>` | `[nzDisabled]` ✅ |

**Quy tắc nhanh:** Nếu element là HTML native (`button` / `input` / `textarea`) thì dùng `[disabled]`. Nếu là Angular component (tag bắt đầu bằng `nz-...`) thì dùng `[nzDisabled]`.

**Verify trước khi code:** Đọc ng-zorro docs (https://ng.ant.design/components/{tên}/en) hoặc grep code hiện có pattern tương tự — đừng đoán mặc định "tất cả ng-zorro dùng nzXxx".

**Liên quan:** [[verify-ngzorro-api-before-use]] — rule chung verify ng-zorro API trước khi dùng (chưa viết, gặp case khác thì viết).
