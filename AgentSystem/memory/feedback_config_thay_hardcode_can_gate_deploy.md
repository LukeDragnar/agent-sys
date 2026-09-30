---
name: feedback_config_thay_hardcode_can_gate_deploy
description: Refactor bỏ hardcode → đọc ConfigSystem là đổi hành vi phụ thuộc dữ liệu runtime; phải gate deploy (config có mặt ở MỌI môi trường) + cẩn thận nơi throw
metadata:
  type: feedback
---

Khi refactor **bỏ hằng hardcode (GUID/ID/flag) → đọc từ ConfigSystem** (vd `SANDBOX_GUIDELINE_DON_VI_ID` thay 2 GUID đơn vị/chi nhánh ticket): code từ "chạy được mọi lúc" thành "phụ thuộc 1 dòng dữ liệu runtime". Đây là đổi hành vi, KHÔNG phải cleanup vô hại.

**Why (bài học thật từ task ticket 2026-07):**
- GUID hardcode cũ `b8f8d798-…` KHÔNG tồn tại trong bảng DonVi → nhánh "user thuộc đơn vị hệ thống" LUÔN false. Bỏ hardcode → đọc config → **mọi user thuộc đơn vị đó bỗng đổi hành vi** (thành "loại 2", quyền cross-tenant, đường lấy IdDonVi khác). Ripple rộng hơn tưởng.
- Config lưu Redis cache 24h (`GetValueByCode` → `LayTatCaCauHinhHeThong`): sửa thẳng DB KHÔNG có tác dụng tới 24h; phải sửa qua màn Quản lý cấu hình (nó tự `LuuRedisCauHinhHeThong`).
- Config từng dính giá trị rác (`...D8531Exxx`) làm `Guid.TryParse` fail âm thầm → parse-fail = mất quyền, không lỗi rõ.

**How to apply:**
1. **Gate deploy BẮT BUỘC**: trước khi merge product, xác nhận key config đã có + là giá trị HỢP LỆ ở **mọi môi trường sẽ deploy** (staging XONG ≠ product xong). Ghi vào mô tả MR mục "Điều kiện triển khai". Không tự kiểm được prod (không có connection string) → nêu rõ cho user tự nhìn: đăng nhập tài khoản có đặc quyền xem tính năng có bật không.
2. **Nơi throw khi thiếu config**: phân biệt luồng.
   - Web/API (user đứng trước màn) → throw OK, user thấy lỗi.
   - **Check quyền** → KHÔNG throw (thiếu config chỉ mất quyền, không được sập cả API).
   - **Webhook/consumer** → TUYỆT ĐỐI cẩn thận: nếu endpoint bật `DiscardFaultedMessages()` thì exception = message bị VỨT sau retry, KHÔNG vào `_error` queue, đối tác đã nhận 200 nên không gửi lại ⇒ **MẤT TRẮNG data**. Phải ghi bản ghi (IdDonVi null) + log thay vì throw. Kiểm `MassTransitExtensions`/`UseMessageRetry`/`DiscardFaultedMessages` trước khi quyết throw.
3. **Luôn log trước khi throw** ở nhánh thiếu-config (`LogHelper.LogError`) — nếu không, lỗi vận hành chết im lặng.
4. **Gộp nhiều bản sao logic về 1 nguồn**: nếu thấy cùng một phép kiểm (vd "là SB team") copy-paste ở nhiều service/controller, refactor thành 1 hàm nguồn-sự-thật. Task ticket từng có FE hỏi hàm A (so hardcode) còn BE gác hàm B (đọc config) → lệch nhau: BE cho xem mà FE giấu tab.

Liên quan: [[feedback_di_checklist_when_adding_service]] · [[feedback_db]]
