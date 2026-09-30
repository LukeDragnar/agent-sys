---
name: feedback-i18n-html-attr-to-binding-keep-gt
description: Khi convert static HTML attribute (vd `placeholder="x"`) sang property binding (`[placeholder]="..."`) trong Angular template, phải giữ nguyên ký tự `>` đóng tag. Bug recurring khi dùng Edit tool replace 1 attribute mà attribute đó đứng cuối dòng — `>` bị drop kéo theo lỗi NG5002 "Opening tag not terminated"
metadata:
  type: feedback
---

Khi wrap i18n trong Angular HTML, lúc convert một attribute static `attr="value"` sang property binding `[attr]="'key' | tsb : 'value'"`, phải kiểm tra:

1. **Nếu attribute đó nằm cuối dòng và dòng kết thúc bằng `>`** → giữ nguyên `>` ở cuối dòng sau khi sửa.
2. **Nếu attribute nằm giữa dòng** → không bị ảnh hưởng.

**Why:** Khi dùng Edit tool replace pattern `attr="value">` thành `[attr]="..."`  (quên copy `>`), Angular parser báo NG5002 "Opening tag not terminated" + "Unexpected closing tag" cho element con. Đã mắc lỗi này 2 lần trong session i18n:
- `<ng-select ... placeholder="Chọn Tk nợ">` → mất `>` → `ng-template` con bị parse như attribute
- `<nz-input-group ... nzAddOnAfter="ngày">` (4 lần) → mất `>` → `nz-input-number` con bị parse sai

**How to apply:**
- Khi sửa attribute cuối dòng, dùng `old_string` bao gồm cả `>` và (nếu cần) ký tự đầu dòng kế tiếp để bám context. Ví dụ:
  ```
  old_string: '...nzAddOnAfter="ngày">\n        <nz-input-number'
  new_string: '...[nzAddOnAfter]="\'addon-ngay\' | tsb : \'ngày\'">\n        <nz-input-number'
  ```
- Hoặc sửa nhỏ hơn — chỉ replace `attr="value"` thành `[attr]="..."`, không động đến `>`.
- Sau khi edit xong file HTML có nhiều attribute conversion, scan lại bằng grep pattern `attr=".+"\s*$` để chắc không sót `>`.

Related: [[feedback-i18n-no-getter-for-array]], [[feedback-i18n-loanword-translation]]
