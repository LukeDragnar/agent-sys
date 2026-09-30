---
controller: DMGhiChuGiaoHangController
module: kho
service: OrderLogistic
man_hinh_ui: "Đơn hàng logistic > Danh mục > Ghi chú giao hàng: list theo điều kiện, thêm/sửa/xóa ghi chú, đặt ghi chú mặc định (IsDefault). Ghi chú dùng prefill khi đăng đơn giao vận."
muc_dich: "CRUD danh mục Ghi chú giao hàng (template ghi chú gắn khi đăng đơn vận chuyển) theo đơn vị + chi nhánh, có đặt mặc định và log lịch sử tác động; cache theo chi nhánh được clear sau mỗi thay đổi."
alias: ["ghi chú giao hàng", "danh mục ghi chú", "template ghi chú đơn", "ghi chú mặc định giao hàng", "DM ghi chú"]
auth: public
state_machine: []
deprecated: false
entities: [DMGhiChuGiaoHang]
stored_procs: []
depends:
  - "IDMGhiChuGiaoHangService (OrderLogistic.Service/Generated)"
  - "ICommonCacheService (GhiChuGiaoHang_RemoveByIdChiNhanh — clear cache sau CUD)"
  - "ILogActivityHelper (ghi LichSuTacDong: ADD/UPDATE/DELETE)"
cross_module_in: []
cross_module_out:
  - "Log/LichSuTacDong (mỗi thao tác ADD/UPDATE/DELETE ghi EventLichSuTacDong, TableId=SCRM_ORDERLOGISTIC_DMGhiChuGiaoHang)"
---

# DMGhiChuGiaoHangController — Danh mục ghi chú giao hàng

> CRUD danh mục đơn giản (scope đơn vị + chi nhánh), có log lịch sử tác động và clear cache. Mọi endpoint có permission key riêng.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Thêm mới ghi chú | "Thêm mới" | POST `api/DMGhiChuGiaoHang/ThemMoi` | `DMGhiChuGiaoHang_ThemMoi` | public | form hợp lệ | Set IdDonVi/IdChiNhanh từ user, validate ModelState, cắt Ten/NoiDung ≤255, insert → log ADD → clear cache chi nhánh | EF | Cắt chuỗi >255 âm thầm (không báo user) |
| 2 | Cập nhật thông tin | "Lưu" | POST `api/DMGhiChuGiaoHang/CapNhatThongTin` | `DMGhiChuGiaoHang_CapNhatThongTin` | public | có Id | Load theo Id; null → "không tìm thấy"; DeepClone bản cũ, cắt Ten/NoiDung ≤255, update Ten+NoiDung → log UPDATE (old/new) → clear cache | EF | Chỉ cập nhật Ten + NoiDung (không cập IsDefault ở đây) |
| 3 | Đặt ghi chú mặc định | toggle "Mặc định" | POST `api/DMGhiChuGiaoHang/CapNhatIsDefault` | `DMGhiChuGiaoHang_CapNhatIsDefault` | public | có Id | Set IdDonVi/IdChiNhanh → `CapNhatIsDefaultGhiChu` (gán mặc định trong phạm vi chi nhánh) | EF | Logic "chỉ 1 mặc định" nằm trong service, không ở controller |
| 4 | Xóa ghi chú | "Xóa" | POST `api/DMGhiChuGiaoHang/Xoa/{id:Guid}` | `DMGhiChuGiaoHang_Xoa` | public | có Id | Load theo Id; null → "không tìm thấy"; DeleteAsync → log DELETE → clear cache | EF | - |
| 5 | Chi tiết theo Id | mở row | GET `api/DMGhiChuGiaoHang/TimTheoId/{id:Guid}` | `DMGhiChuGiaoHang_TimTheoId` | public | có Id | Trả 1 bản ghi theo Id | EF | - |
| 6 | Tìm theo điều kiện | grid + filter | POST `api/DMGhiChuGiaoHang/TimTheoDieuKien` | `DMGhiChuGiaoHang_TimTheoDieuKien` | public | màn list | List + phân trang ghi chú theo filter, scope user | EF | - |

## Quan hệ
- **Gọi sang:** `IDMGhiChuGiaoHangService` (CRUD), `ILogActivityHelper.LogActivity` (lịch sử tác động), `ICommonCacheService.GhiChuGiaoHang_RemoveByIdChiNhanh` (clear cache sau CUD).
- **Dữ liệu (entities):** `DMGhiChuGiaoHang` (Ten, NoiDung ≤255, IdDonVi, IdChiNhanh, IsDefault).
- **Ripple / đồng bộ:** mỗi ADD/UPDATE/DELETE ghi `EventLichSuTacDong` (TableId `SCRM_ORDERLOGISTIC_DMGhiChuGiaoHang`) + xóa cache ghi chú theo chi nhánh. Danh mục này được tiêu thụ khi đăng đơn giao vận (prefill ghi chú).

## Lỗi đã biết / lưu ý kỹ thuật
- `Ten`/`NoiDung` bị cắt cứng ≤255 ký tự ở controller (`:43-44, :76-77`) không cảnh báo user.
- `CapNhatThongTin` chỉ cập nhật Ten + NoiDung; muốn đổi mặc định phải gọi `CapNhatIsDefault` riêng.
- Cache `GhiChuGiaoHang_RemoveByIdChiNhanh` phải được clear sau mọi CUD — nếu thêm endpoint sửa data mới mà quên clear → dữ liệu cũ còn trong cache.
