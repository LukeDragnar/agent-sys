---
controller: NguonGioiThieuController
module: marketing
service: Promotion
man_hinh_ui: "Marketing > Nguồn giới thiệu (CRUD nguồn giới thiệu khách hàng — cộng tác viên/người giới thiệu)"
muc_dich: "CRUD nguồn giới thiệu khách hàng (người/kênh giới thiệu để tính hoa hồng), tìm kiếm, và cung cấp thông tin nguồn cho service khác (đơn hàng) qua API private."
alias: ["nguồn giới thiệu", "NguonGioiThieu", "cộng tác viên", "người giới thiệu", "CTV"]
auth: mixed
state_machine: []
deprecated: false
entities: [NguonGioiThieu, CauHinhHoaHongGioiThieu]
stored_procs: []
depends:
  - "Core/TienIch (TienIchAttribute TI_HoaHongGioiThieu)"
cross_module_in:
  - "Order/OrderProcess (TimTheoId_Private / GetByIds — lấy thông tin nguồn khi gắn vào đơn để tính hoa hồng)"
cross_module_out:
  - "HoaHongGioiThieu (nguồn này là gốc cấu hình & phát sinh hoa hồng khi thanh toán)"
---
> ⚠️ Lưu ý: class này nằm trong file `NguonKhachHangController.cs` (tên file ≠ tên class). Tên class & route là `NguonGioiThieu`.

# NguonGioiThieu — Quản lý nguồn giới thiệu

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|
| 1 | Danh sách (grid) | Danh sách / Lọc | POST `TimTheoDieuKien` | `NguonGioiThieu_TimTheoDieuKien` (AD-TONG/CN/MCN) | public | Phân trang nguồn giới thiệu theo điều kiện + CurrentUser. | EF | — |
| 2 | Tìm theo keyword | Tìm kiếm | POST `SearchByKeyword` | `[SBAuthorize()]` (chỉ login) | public | Autocomplete/search nhanh theo keyword. | EF | quyền key thật bị comment, để `[SBAuthorize]` rỗng |
| 3 | Thêm mới | Lưu | POST `ThemMoi` | `NguonGioiThieu_ThemMoi` (AD-TONG/CN/MCN) | public | `Create(payload)`. | EF | — |
| 4 | Cập nhật | Lưu | POST `CapNhat` | `NguonGioiThieu_CapNhat` | public | `Update(payload)`. | EF | — |
| 5 | Xóa | Xóa | GET `Xoa/{id}` | `NguonGioiThieu_Xoa` | public | `Delete(id)`. | EF | GET cho hành động xóa |
| 6 | Chi tiết theo Id | Mở edit | GET `TimTheoId/{id}` | `NguonGioiThieu_TimTheoId` | public | `GetById(id)`. | EF | — |
| 7 | Check tồn tại theo Id | — | GET `CheckById/{id}` | `NguonGioiThieu_CheckById` (+QLDV) | public | Kiểm tra nguồn có hợp lệ (trước khi gắn vào đơn). | EF | — |

## Endpoint nội bộ (private / service-to-service)
- `[PrivateSBAuthorize]` — Order/đơn hàng gọi lấy nguồn để tính hoa hồng:
  - GET `TimTheoId_Private/{id}` → `GetByIdAsync(id)` (không qua CurrentUser).
  - POST `GetByIds` (list Guid) → `GetByIds(ids, CurrentUser)`.

## Quan hệ
- **Gọi sang:** không gọi sang service ngoài ở controller.
- **Dữ liệu (entities):** `NguonGioiThieu` (master nguồn), liên kết `CauHinhHoaHongGioiThieu` (cấu hình hoa hồng theo nguồn).
- **Ripple/đồng bộ:** khi đơn có `IdNguonGioiThieu` + có cấu hình → PromotionUpdateWithBillService sinh `HoaHongGioiThieu` lúc thanh toán.

## Lỗi đã biết / lưu ý kỹ thuật
- File `NguonKhachHangController.cs` chứa class `NguonGioiThieuController` (tên file lệch tên class) → dễ nhầm khi tìm file.
- `NguonGioiThieuController.cs:35` — `SearchByKeyword` để `[SBAuthorize]` rỗng (quyền key bị comment) → mọi user login tìm được; rò danh sách nguồn.
- `NguonGioiThieuController.cs:72` — `Xoa` dùng HTTP GET (không idempotent-safe).
- Nhiều method check `!ModelState.IsValid` cho action chỉ nhận `Guid id` từ route (Xoa/TimTheoId/CheckById) → check thừa, ModelState luôn valid.
