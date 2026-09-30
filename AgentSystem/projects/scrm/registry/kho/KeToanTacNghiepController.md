---
controller: KeToanTacNghiepController
module: kho
service: OrderLogistic
man_hinh_ui: "Kế toán > Kế toán tác nghiệp (màn tác nghiệp đơn hàng logistic cho kế toán) — list đơn theo điều kiện + thao tác đăng đơn / care / ghi chú kế toán / xuất Excel; là bản 'khẩu độ kế toán' của Thủ kho tác nghiệp."
muc_dich: "Tác nghiệp đơn hàng logistic dành cho role kế toán: tìm/xem đơn, đăng đơn (Insert→Process hàng đợi), hủy đăng đơn, admin đăng/hủy đăng, cập nhật trạng thái giao hàng, ghi chú kế toán + tin nhắn nội bộ + blacklist, xuất Excel 4 kiểu, đổi ngày muốn giao. Dùng chung service với Thủ kho tác nghiệp."
alias: ["kế toán tác nghiệp", "KeToanTacNghiep", "tác nghiệp kế toán đơn hàng", "ghi chú kế toán", "đăng đơn kế toán"]
auth: public
state_machine: [DonMoi(1000), ChoVanDon(1), DaDang(20), DangGiaoHang(30), DaGiaoHang(31), DaThanhToan, HuyDangDon(5), HuyDon(999)]
deprecated: false
entities: [DonHangLogistic, DonHangDangDon, DonHangHuyDangDon, DonHangLogisticMessage, ContactPhoneBlacklist]
stored_procs: [PRC_SCRM_OrderLogistic_DonHangDangDon_GetForDangDon]
depends:
  - "IThuKhoTacNghiepService (KhoTacNghiepService.cs — dùng chung với ThuKhoTacNghiep + DonHangSale)"
  - "IKeToanTacNghiepService (KeToanTacNghiepService.cs — riêng cho TimTheoDieuKien + ViewDetail + Export _V2)"
cross_module_in: []
cross_module_out:
  - "DeliveryConnect (đăng/hủy đăng đơn → gửi lệnh ĐTGV)"
  - "WarehouseProduct (trừ/cộng kho khi đăng/hủy đăng)"
  - "Contact/Customer (blacklist, care đơn, ghi chú kế toán)"
---

# KeToanTacNghiepController — Kế toán tác nghiệp đơn hàng logistic

> Controller `partial` (1 file). TienIch `KeToanTacNghiepController` / `MaTienIchSCRM.TI_KeToanLogistic`. Dùng **2 service**: `IThuKhoTacNghiepService` (chung với ThuKhoTacNghiep/DonHangSale — đa số endpoint tác nghiệp) và `IKeToanTacNghiepService` (riêng — TimTheoDieuKien, ViewDetail, Export _V2). Khẩu độ permission `KeToanTacNghiep_*` + role kế toán (DV-KT, DV-KT-TONG, HT-CSKH).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm đơn theo điều kiện | "Tìm kiếm" | POST `api/KeToanTacNghiep/TimTheoDieuKien` | `KeToanTacNghiep_TimTheoDieuKien` | public | màn list | List đơn logistic theo bộ lọc kế toán (dùng `IKeToanTacNghiepService`) | EF | - |
| 2 | Tìm theo trạng thái giao hàng | tab trạng thái | POST `api/KeToanTacNghiep/TimTheo_TrangThaiGiaoHang` | `KeToanTacNghiep_TimTheo_TrangThaiGiaoHang` | public | - | List đơn theo nhóm trạng thái | EF | - |
| 3 | Xem chi tiết | "Xem" | POST `api/KeToanTacNghiep/OrderLogistic_KeToanTacNghiepViewDetail` | `KeToanTacNghiep_OrderLogistic_KeToanTacNghiepViewDetail` | public | - | Trả chi tiết đơn (dùng `IKeToanTacNghiepService`) | EF | - |
| 4 | Tra cứu đơn theo SĐT + lịch sử | (popup tra cứu) | POST `GetOrderLogisticByPhone` / `GetOrderLogisticHistoryByPhone` | `KeToanTacNghiep_GetOrderLogisticByPhone` / `..GetOrderLogisticHistoryByPhone` | public | - | Lấy đơn + lịch sử đơn theo số điện thoại | EF | - |
| 5 | Đăng đơn (queue Insert→Process) | "Đăng đơn" | POST `InsertDonHangDangDon` / `ProcessDonHangDangDon` | `KeToanTacNghiep_InsertDonHangDangDon` / `..ProcessDonHangDangDon` | public | đơn chưa DaDang | Enqueue `DonHangDangDon` → process bốc SP có lock → gửi ĐTGV + trừ kho. Quyền đăng phụ thuộc `ChoPhepKeToanDangDon` (mặc định true) | SP + EF | Pattern giống ThuKhoTacNghiep (cùng service) |
| 6 | Hủy đăng đơn (queue) | "Hủy đăng đơn" | POST `InsertDonHangHuyDangDon` / `ProcessDonHangHuyDangDon` | `KeToanTacNghiep_InsertDonHangHuyDangDon` / `..ProcessDonHangHuyDangDon` | public | đơn DaDang | Queue hủy đăng → process: hủy vận đơn ĐTGV + hoàn kho | EF | Đơn TMĐT chặn hủy đăng (service chung) |
| 7 | Admin đăng/hủy đăng đơn | (admin) | POST `AdminDangDon` / `AdminHuyDangDon` | `KeToanTacNghiep_AdminDangDon` / `..AdminHuyDangDon` | public | admin | Đăng/hủy đăng bỏ qua gate (admin/CSKH); AdminHuyDangDon chỉ DV-AD-TONG/HT-CSKH | EF | - |
| 8 | Cập nhật trạng thái giao hàng theo đơn | "Cập nhật trạng thái" | POST `api/KeToanTacNghiep/CapNhatTrangThaiGiaoHangTheoDon` | `KeToanTacNghiep_CapNhatTrangThaiGiaoHangTheoDon` | public | - | Đổi GiaoHangTrangThaiMa thủ công | EF | - |
| 9 | Đổi mã đơn | "Đổi mã đơn" | POST `api/KeToanTacNghiep/ThayDoiMaDon` | `KeToanTacNghiep_ThayDoiMaDon` | public | - | Sinh lại mã đơn | EF | - |
| 10 | Ghi chú kế toán | "Ghi chú KT" | POST `api/KeToanTacNghiep/UpdateGhiChuKeToan` | `KeToanTacNghiep_UpdateGhiChuKeToan` | public | - | Cập nhật ghi chú kế toán cho đơn | EF | - |
| 11 | Tin nhắn nội bộ | (panel chat) | POST `SaveMessage` / `GetListMessageByIdDonHangLogistic` / `GetListMessageByPhone` | `KeToanTacNghiep_SaveMessage` / `..GetListMessageByIdDonHangLogistic` / `..GetListMessageByPhone` | public | - | Lưu/lấy ghi chú nội bộ theo đơn hoặc SĐT | EF | - |
| 12 | Blacklist SĐT | "Thêm/Xóa blacklist" | POST `InsertOrUpdateBlackList` / `DeleteBlackList` | `KeToanTacNghiep_InsertOrUpdateBlackList` / `..DeleteBlackList` | public | - | Thêm/xóa số điện thoại blacklist | EF | - |
| 13 | Care đơn | "Care" | POST `api/KeToanTacNghiep/UpdateTrangThaiCareDon` | `KeToanTacNghiep_UpdateTrangThaiCareDon` | public | - | Cập nhật trạng thái care đơn | EF | - |
| 14 | Xuất Excel tác nghiệp (4 kiểu) | "Export To Excel" (kiểu 1-4) | POST `ExportExcelKeToanTacNghiepKieu1..4` | `[SBAuthorize()]` (mở) | public | - | Xuất Excel; lỗi → 503 nếu `Service_Unavailable`, ngược lại 500 `EXPORT_ERROR`. Gọi nhánh `_V2` của `IKeToanTacNghiepService` | EF | Cả 4 endpoint export quyền rỗng; kiểu 1-2 trả file `KeToanTacNghiep.xlsx` (trùng tên), kiểu 3-4 có hậu tố Kieu3/Kieu4 |
| 15 | Đổi ngày muốn nhận hàng | "Đổi ngày giao" | POST `api/KeToanTacNghiep/UpdateNgayMuonNhanHang` | `KeToanTacNghiep_UpdateNgayMuonNhanHang` | public | - | Cập nhật ngày khách muốn nhận hàng | EF | - |
| 16 | Đếm/list/xóa đơn đăng + hủy đăng | badge/grid/xóa | POST `GetDonHangDangDonTotal`, `GetDonHangDangDonByUser`, `DeleteDonHangDangDonByStatus`, `DangDonSearch` + nhánh `*HuyDangDon*` | `KeToanTacNghiep_*` tương ứng | public | - | Phụ trợ hàng đợi đăng/hủy đăng (tương tự ThuKhoTacNghiep) | EF | - |

## Quan hệ
- **Gọi sang:** DeliveryConnect (vận đơn ĐTGV qua đăng/hủy đăng), WarehouseProduct (trừ/cộng kho), Contact (blacklist, care đơn). Logic đăng đơn = `IThuKhoTacNghiepService` (chung); query/export = `IKeToanTacNghiepService` (riêng).
- **Dữ liệu (entities):** `DonHangLogistic`, `DonHangDangDon`/`DonHangHuyDangDon`, `DonHangLogisticMessage`, `ContactPhoneBlacklist`.
- **Ripple / đồng bộ:** đăng đơn → vận đơn ĐTGV + trừ kho; hủy đăng → hủy vận đơn + hoàn kho.

## Lỗi đã biết / lưu ý kỹ thuật
- 4 endpoint export đều `[SBAuthorize()]` rỗng (KeToanTacNghiepController.cs:284,327,367,407) — chỉ cần đăng nhập là xuất được.
- Export kiểu 1 và kiểu 2 đều trả file tên `KeToanTacNghiep.xlsx` (KeToanTacNghiepController.cs:316,359) — FE/người dùng dễ nhầm 2 file; chỉ kiểu 3/4 có hậu tố.
- Endpoint `OrderLogistic_KeToanTacNghiepCount`, `AdminDangDon` (bản 2), `TimTheoId` bị comment chết (KeToanTacNghiepController.cs:49-55,114-128).
- Đăng đơn dùng CHUNG `IThuKhoTacNghiepService` với ThuKhoTacNghiep → mọi gate/known-issue luồng đăng đơn xem node `KhoTacNghiepController.md` (class ThuKhoTacNghiepController).
