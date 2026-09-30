---
controller: NhanVienTienTipController
module: nhan-su
service: Branch
man_hinh_ui: "Nhân sự > Tiền tip nhân viên (danh sách tiền tip theo đơn vị) + tổng tiền tip theo NV; tip phát sinh theo khách hàng"
muc_dich: "Quản lý tiền tip nhân viên: tra cứu tip theo đơn vị, theo khách hàng, tổng tip theo nhân viên. CRUD bản ghi tip kế thừa từ SBBaseController."
alias: [NhanVienTienTip, "tiền tip", "tiền típ", "tip nhân viên", "tiền tip NV"]
auth: public
state_machine: []
deprecated: false
entities: [NhanVienTienTip]
stored_procs: []
depends: [SBBaseController<NhanVienTienTip,NhanVienTienTipDto>]
cross_module_in: []
cross_module_out: []
---

# NhanVienTienTip — Tiền tip nhân viên

> Kế thừa `SBBaseController<NhanVienTienTip, NhanVienTienTipDto>` → CRUD bản ghi tip dùng endpoint **base** (`TimTheoId/LayTatCa/ThemMoi/CapNhatThongTin/Xoa`), controller-level `[SBAuthorize]` của base = login-only (không key). 3 action tra cứu/tổng hợp dưới đây có key phân quyền. (Action `InsertMultipleTienTipNhanVien` đã bị comment, không hoạt động.)

## Hành động
| # | Hành động | Nhãn UI/nút | Endpoint | Quyền (key) | Auth | Hành vi đúng (expected) | Nguồn | Known-issue |
|---|---|---|---|---|---|---|---|---|
| 1 | Tất cả tiền tip theo đơn vị | Danh sách | GET `GetAllTienTipByDonVi` | `NhanVienTienTip_GetAllTienTipByDonVi` | public | Trả toàn bộ tip của đơn vị hiện tại (theo `CurrentUser`). | EF | — |
| 2 | Tiền tip theo khách hàng | Xem chi tiết | GET `GetTienTipNhanVien?idKhachHang` | `NhanVienTienTip_GetTienTipNhanVien` | public | Tip gắn theo khách hàng (idKhachHang nullable). | EF | — |
| 3 | Tổng tiền tip theo NV | (tổng) | GET `GetTongTienTipByNhanVien?idNhanVien` | `NhanVienTienTip_GetTongTienTipByNhanVien` | public | Tổng tiền tip của 1 NV (idNhanVien nullable, theo `CurrentUser`). | EF | — |
| 4 | CRUD base (kế thừa) | Thêm/Sửa/Xóa tip | GET `TimTheoId/{id}`, `LayTatCa`, POST `ThemMoi`, `CapNhatThongTin`, `Xoa/{id}` | (base `[SBAuthorize]`, login-only) | mixed | Thêm/sửa/xóa bản ghi tip qua endpoint base. | EF | Base endpoint login-only, KHÔNG key — đường ghi tip thực tế |

## Quan hệ
- **Gọi sang:** không (EF nội bộ Branch).
- **Dữ liệu (entities):** `NhanVienTienTip` (tip gắn `IdNhanVien`, có thể gắn `IdKhachHang`, số tiền).
- **Ripple/đồng bộ:** tổng tip là đầu vào bảng lương / tổng hợp thu nhập NV.

## Lỗi đã biết / lưu ý kỹ thuật
- `NhanVienTienTipController.cs:14` — CRUD ghi tip đi qua endpoint **base SBBaseController** login-only, KHÔNG có key phân quyền (chỉ 3 action đọc/tổng hợp mới có key). Audit quyền cần soi cả endpoint base.
- `NhanVienTienTipController.cs:24-28` — `InsertMultipleTienTipNhanVien` đã bị comment (bulk insert tip không khả dụng).
