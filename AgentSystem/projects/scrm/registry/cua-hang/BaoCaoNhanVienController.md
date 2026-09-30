---
controller: BaoCaoNhanVienController
module: cua-hang
service: Order
man_hinh_ui: "Báo cáo / Nhân viên — màn 'Báo cáo doanh số nhân viên' (route `bao-cao-doanh-so-nhan-vien`, component `pages/Report/nhanvien/bao-cao-doanh-so/bao-cao-doanh-so.component.ts`; chi tiết: `bao-cao-chi-tiet`). Lưu ý FE gọi qua `services/branch/baocao` (nhóm báo cáo Branch)."
muc_dich: "Báo cáo doanh số theo nhân viên (tổng hợp + chi tiết) — nằm trong service Order nhưng thuộc nhóm tiện ích báo cáo Branch (TI_Branch)."
alias: ["báo cáo doanh số nhân viên", "doanh số nhân viên", "báo cáo nhân viên", "BC nhân viên"]
auth: public
state_machine: []
deprecated: false
entities: [DonHang]
stored_procs: []
depends: [Order (dữ liệu đơn hàng/doanh số), nhóm tiện ích TI_Branch]
cross_module_in: [FE bao-cao-doanh-so.component (Report) gọi qua BaoCaoService (services/branch/baocao)]
cross_module_out: []
---

# BaoCaoNhanVienController — Báo cáo doanh số nhân viên

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Báo cáo doanh số nhân viên (tổng hợp) | "Xem báo cáo" / grid tổng hợp | POST `api/BaoCaoNhanVien/BaoCaoDoanhSoNhanVien` | `BC_BaoCaoDoanhSoNhanVien` | public | màn list | Tổng hợp doanh số theo nhân viên theo bộ lọc (`BaoCaoDoanhSoNhanVienDto`: tên NV, từ/đến ngày, kiểu ngày…) của CurrentUser | EF (service) | - |
| 2 | Báo cáo doanh số nhân viên — chi tiết | "Xem chi tiết" (drill-down) | POST `api/BaoCaoNhanVien/BaoCaoDoanhSoNhanVien_ChiTiet` | `BC_BaoCaoDoanhSoNhanVienChiTiet` | public | từ dòng tổng hợp → chi tiết | Chi tiết doanh số 1 nhân viên (danh sách đơn/giao dịch) theo cùng bộ lọc `BaoCaoDoanhSoNhanVienDto` | EF (service) | - |

## Quan hệ
- **Gọi sang:** truy vấn dữ liệu doanh số trong service Order (qua `IBaoCaoNhanVienService`).
- **Dữ liệu (entities):** `DonHang` (doanh số tổng hợp/chi tiết theo nhân viên).
- **Ripple / đồng bộ:** không (chỉ đọc/báo cáo).

## Lỗi đã biết / lưu ý kỹ thuật
- **Tiện ích TI_Branch dù nằm service Order**: controller gắn `[TienIch("BCNhanVien", "Báo cáo nhân viên", MaTienIchSCRM.TI_Branch)]` → gate theo nhóm báo cáo Branch; FE cũng gọi qua `services/branch/baocao` (`BaoCaoService`). Khi map permission/tiện ích, xếp vào nhóm báo cáo Branch.
- **Nguồn dữ liệu**: service `IBaoCaoNhanVienService.BaoCaoDoanhSoNhanVien[_ChiTiet]` — không xác minh được trong batch này là EF hay stored proc → ghi "EF (service)"; nếu cần chính xác, đọc `Order.Services.Generated` impl.
- Namespace controller có typo `Orrder.API.Controllers.BaoCao` (2 chữ r) — không ảnh hưởng route (`api/BaoCaoNhanVien`).
- Hai endpoint khác nhau ở danh sách role: `BaoCaoDoanhSoNhanVien` có thêm `DV-KT, DV-KT-TONG`; bản `_ChiTiet` không có (role list chỉ là metadata mô tả của `[SBAuthorize]`, key mới là cái query).
- Code thật: BE `Order/Order.API/Controllers/BaoCao/BaoCaoNhanVienController.cs`; FE component `pages/Report/nhanvien/bao-cao-doanh-so/`, service `services/branch/baocao`.
