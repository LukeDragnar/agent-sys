# Luồng — Cấu hình AI

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc một đơn vị (DonViId).
- Token AI: xem cần HT-AD/DV-AD-TONG/HT-CSKH; thêm/sửa/xóa cần DV-AD-TONG.
- Prompt AI (SandBox): chỉ cần đăng nhập (permission key còn ở comment — xem dev.md); thao tác scope cứng theo đơn vị của người dùng.
- Tính năng AI của đơn vị đã được bật (gói/tiện ích AI) — cần product xác nhận.

## Luồng dùng (Token AI)
1. Mở **Quản lý AI → Danh sách Token AI** (`/quan-ly-ai/danh-sach`).
2. Bấm **Thêm mới** → chọn **Loại AI**, nhập **Token**, tích **Mặc định** nếu cần → Lưu (`ThemMoi`, gắn `CurrentUser.DonViId`).
3. **Sửa** (`CapNhatThongTin`) / **Xóa** (`Xoa/{id}`) token. Bấm **Tải lại** để refresh danh sách (`TimTheoDieuKien`).

## Luồng dùng (Prompt AI)
1. Mở **Cấu hình AI (SandBox) → Quản lý Prompt** (`/sandboxai/ai-prompt`).
2. (Shop mới) bấm **Khởi tạo mẫu** (`KhoiTaoMau`): nếu đơn vị chưa có prompt → clone toàn bộ bộ mẫu (`IdDonVi == Guid.Empty`) sang đơn vị hiện tại; nếu đã có → báo "Đơn vị đã khởi tạo mẫu", không tạo.
3. Bấm **Thêm mới** (`ThemMoi`): nhập mã, tên, prompt, mô tả → Lưu, hệ thống ép `IdDonVi = CurrentUser.DonViId`.
4. **Sửa** (`CapNhatThongTin`) / **Xóa** (`Xoa/{id}`): chỉ tác động bản ghi thuộc đơn vị mình (khác đơn vị → NotFound).
5. Tìm theo tên/mã (`TimTheoDieuKien`, lọc cứng `IdDonVi`); **Xuất Excel**.

## Luồng hệ thống (lúc AI chạy)
1. Tính năng AI / bot chat cần trả lời.
2. Hệ thống lấy **token** của đơn vị (token mặc định) để gọi model + lấy **prompt** phù hợp.
3. Model sinh câu trả lời theo prompt → trả về cho khách/nhân viên.

## Case biên
- **Xóa token đang dùng / token mặc định sai** → AI/bot ngừng trả lời (mất khóa gọi model).
- **Sửa/xóa prompt của đơn vị khác** → NotFound (scope cứng theo đơn vị). Riêng **SuperUser** xóa được prompt của đơn vị khác (xem dev.md).
- **Bấm Khởi tạo mẫu khi đơn vị đã có prompt** → không tạo, báo "đã khởi tạo mẫu" (idempotent theo "đã có bất kỳ bản ghi nào", không theo từng mã → nếu mới có 1 prompt cũng coi là đã khởi tạo).
- **Sort prompt từ FE không có tác dụng** → backend luôn sort theo NgayTao ASC (lỗi nhánh if/else giống nhau — xem dev.md).
- **Token Xóa không re-check đơn vị** → chỉ theo id (lưu ý rủi ro — xem dev.md).
- **Permission key prompt còn ở comment** → mọi user đăng nhập đều CRUD được trong phạm vi đơn vị mình (chưa siết theo role admin).

## Ví dụ cụ thể có số
Shop C vừa bật AI.
1. Admin (DV-AD-TONG) vào Token AI → Thêm mới: Loại AI = "ChatGPT", Token = `sk-xxxx...`, tích **Mặc định** → Lưu. Danh sách hiện 1 token, nhãn "Mặc định".
2. Admin sang Quản lý Prompt → bấm **Khởi tạo mẫu** → hệ thống clone 8 prompt mẫu sang shop C. Lưới hiện 8 dòng.
3. Admin sửa prompt mã `GREETING` cho đúng tên shop → Lưu.
4. Hôm sau admin bấm lại **Khởi tạo mẫu** → báo "Đơn vị đã khởi tạo mẫu", không thêm dòng nào (vẫn 8).
5. Một nhân viên thường (chỉ login) cũng vào sửa được prompt của shop C (do quyền còn lỏng) → admin lưu ý kiểm soát.

## Hậu điều kiện
- Thêm/Sửa/Xóa token → cập nhật bảng TokenAI của đơn vị; ảnh hưởng ngay khả năng gọi model.
- Thêm/Sửa/Xóa prompt → cập nhật AIDonViPrompt của đơn vị; tính năng AI đọc dùng.
- Khởi tạo mẫu → bulk insert bộ prompt mẫu cho đơn vị (chạy 1 lần khi đơn vị trống prompt).
