---
name: feedback-cot-moi-xep-cuoi-list
description: Thêm cột mới vào grid/list SCRM → luôn đặt CUỐI mảng cột (trước cột THAO TÁC cố định ghim phải), không chèn vào giữa
metadata:
  type: feedback
---

Khi thêm **cột mới** vào bất kỳ grid/list nào của SCRM FE → **luôn xếp ở CUỐI** danh sách cột, **không chèn vào giữa** các cột đang có.

Cột **THAO TÁC** (thao tác/actions) là ngoại lệ mặc định: nó **cố định cuối cùng** (hardcode ngoài mảng `gridColumns`, dùng `[nzRight]` ghim phải) → không cần nhắc, cột mới xếp **ngay trước** nó.

**Why:** User đã dặn nhiều lần; chèn cột mới vào giữa làm xáo trộn thứ tự quen thuộc của list. Đây là quy ước mặc định — user không muốn phải nhắc lại mỗi lần.

**How to apply:** Định nghĩa cột mới → thêm vào **cuối** mảng `gridColumns` (vd `ticket-ho-tro.component.ts`), sau tất cả cột hiện có. THAO TÁC vẫn tự cuối. Không hỏi lại "để đâu" nếu chỉ là 1 cột mới thông thường.

Liên quan: [[feedback-report-touched-paths]].
