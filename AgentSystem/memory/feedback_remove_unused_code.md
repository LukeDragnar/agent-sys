---
name: Xóa code không dùng
description: Khi inject/khai báo gì mà không dùng thì phải xóa ngay, không để thừa
type: feedback
---

Khi thêm field, inject, using mà sau đó refactor không còn dùng nữa → phải xóa ngay trong cùng session.

**Why:** User yêu cầu code sạch, không chấp nhận code thừa dù chỉ là 1 field hay 1 using không dùng.

**How to apply:** Sau mỗi lần refactor/chuyển logic, grep lại tất cả field/using đã thêm trong session — nếu không còn reference nào ngoài khai báo + gán thì xóa ngay. Đặc biệt chú ý khi chuyển logic từ service A sang service B: dọn sạch service A.
