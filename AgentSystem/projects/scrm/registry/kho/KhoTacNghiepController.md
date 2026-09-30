---
controller: ThuKhoTacNghiepController
module: kho
service: OrderLogistic
man_hinh_ui: "Kho > Thủ kho tác nghiệp (màn tác nghiệp đơn hàng logistic cho thủ kho) — list đơn theo trạng thái giao hàng + thao tác đăng đơn / hủy đăng / cập nhật trạng thái / quét mã đổi trạng thái / xuất Excel."
muc_dich: "Trung tâm tác nghiệp kho cho đơn hàng logistic: đăng đơn lên đối tác giao vận (pattern Insert hàng đợi → Process), hủy đăng đơn + hoàn kho, hủy chờ callback đối tác, cập nhật trạng thái giao hàng theo đơn, quét mã đơn đổi trạng thái, yêu cầu giao lại / duyệt hoàn, chuyển đơn giữa chi nhánh, blacklist + ghi chú nội bộ, xuất Excel 4 kiểu."
alias: ["thủ kho tác nghiệp", "kho tác nghiệp", "ThuKhoTacNghiep", "đăng đơn kho", "tác nghiệp đơn hàng logistic", "quét mã đổi trạng thái", "hủy chờ callback", "yêu cầu giao lại"]
auth: public
state_machine: [DonMoi(1000), ChoVanDon(1), HuyDangDon(5), DaDang(20), DaLayHang(21), DangGiaoHang(30), DaGiaoHang(31), GiaoHangMotPhan, DaThanhToan, HuyDon(999)]
deprecated: false
entities: [DonHangLogistic, DonHangDangDon, DonHangHuyDangDon, DonHangLogisticLogs, DonHangLogisticMessage, ContactPhoneBlacklist]
stored_procs: [PRC_SCRM_OrderLogistic_DonHangDangDon_GetForDangDon]
depends:
  - "IThuKhoTacNghiepService (KhoTacNghiepService.cs — service lớn ~20k dòng, dùng chung cho ThuKhoTacNghiep + KeToanTacNghiep + DonHangSale)"
  - "ICauHinhService (cấu hình đơn vị: IsSaleDangDon, ChoPhepKhoDangDon, ChoPhepKeToanDangDon, SoPhutChoCallbackDangDon, IdChiNhanhTong...)"
  - "ICommonService (DangHuyDon_V2 hoàn kho, IsChiNhanhTong)"
  - "ICommonElasticSearchService (ESIndexDonHangLogistic)"
  - "_khoTacNghiepStoreService (SP bốc đơn đăng có lock)"
cross_module_in:
  - "DeliveryConnect (callback trạng thái giao vận từ đối tác → cập nhật GiaoHangTrangThaiMa; private blacklist)"
cross_module_out:
  - "DeliveryConnect (đăng đơn → gửi lệnh tạo/hủy vận đơn sang đối tác giao vận)"
  - "WarehouseProduct (trừ/cộng SoLuongChoXuat khi đăng/hủy đăng/hoàn kho — DangHuyDon_V2)"
  - "Contact/Customer (blacklist số điện thoại; cập nhật nhãn khách hàng; care đơn)"
  - "Loyalty/NghiepVuPending (đổi trạng thái giao hàng → tạo nghiệp vụ pending)"
---

# ThuKhoTacNghiepController — Thủ kho tác nghiệp đơn hàng logistic

> **Lưu ý quan trọng:** Tên file là `KhoTacNghiepController.cs` nhưng class thật là `ThuKhoTacNghiepController`, route `api/ThuKhoTacNghiep`, TienIch `ThuKhoTacNghiepController` / `MaTienIchSCRM.TI_KhoLogistic`. Controller `partial` nhưng chỉ 1 file. Dùng chung `IThuKhoTacNghiepService` (KhoTacNghiepService.cs) với `KeToanTacNghiepController` và `DonHangSaleController` — 3 controller chỉ khác bộ permission key + role.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm đơn theo điều kiện | "Tìm kiếm" | POST `api/ThuKhoTacNghiep/TimTheoDieuKien` | `ThuKhoTacNghiep_TimTheoDieuKien` | public | màn list | List đơn logistic theo bộ lọc (kho/chi nhánh/trạng thái giao hàng/ngày) | EF | - |
| 2 | Tìm theo trạng thái giao vận | tab trạng thái | POST `api/ThuKhoTacNghiep/TimTheo_TrangThaiGiaoHang` | `ThuKhoTacNghiep_TimTheo_TrangThaiGiaoHang` | public | - | List đơn theo nhóm trạng thái giao hàng | EF | - |
| 3 | Xem chi tiết đơn | "Xem" | POST `api/ThuKhoTacNghiep/OrderLogistic_ThuKhoTacNghiepViewDetail` | `ThuKhoTacNghiep_OrderLogistic_ThuKhoTacNghiepViewDetail` | public | - | Trả chi tiết đơn logistic | EF | - |
| 4 | Đẩy đơn vào hàng đợi đăng | "Đăng đơn" | POST `api/ThuKhoTacNghiep/InsertDonHangDangDon` | `ThuKhoTacNghiep_InsertDonHangDangDon` | public | đơn chưa DaDang | Validate per-đơn (đúng đơn vị, có ĐVVC, có kho, chưa đăng) → ghi `DonHangDangDon` ChuaXuly. Gate quyền theo cấu hình kho/kế toán/sale | EF | KHÔNG đăng ngay; chỉ enqueue. Quyền đăng phụ thuộc cấu hình `ChoPhepKhoDangDon`/`ChoPhepKeToanDangDon`/`IsSaleDangDon` |
| 5 | Process đăng đơn | (poll nền) | POST `api/ThuKhoTacNghiep/ProcessDonHangDangDon` | `ThuKhoTacNghiep_ProcessDonHangDangDon` | public | có bản ghi chờ | Bốc 1 bản ghi bằng SP (lock chống bốc trùng) → validate lại (chốt đơn, có kho, chưa DaDang, chưa đối soát) → gửi lệnh ĐTGV → DaDang(20) + trừ kho | SP + EF | SP `_DonHangDangDon_GetForDangDon` để 2 user không bốc cùng đơn |
| 6 | Hủy đăng đơn (queue) | "Hủy đăng đơn" | POST `InsertDonHangHuyDangDon` / `ProcessDonHangHuyDangDon` | `ThuKhoTacNghiep_InsertDonHangHuyDangDon` / `..ProcessDonHangHuyDangDon` | public | đơn DaDang, chưa đối soát, KHÔNG phải TMĐT | Queue hủy đăng → process: gửi lệnh hủy ĐTGV + hoàn kho → đơn về chưa đăng | EF | Đơn TMĐT (Shopee/Tiktok) KHÔNG cho hủy đăng (KhoTacNghiepService.cs:4154) |
| 7 | Hủy chờ callback đối tác + hoàn kho | "Hủy chờ callback" | POST `api/ThuKhoTacNghiep/RollbackChoPartnerDangDon` | `ThuKhoTacNghiep_RollbackChoPartnerDangDon` | public | đơn in-flight (IsProcessed=true) quá ngưỡng phút | Đơn đang chờ callback đối tác quá `SoPhutChoCallbackDangDon` (mặc định 3') → hoàn kho (DangHuyDon_V2) + reset IsProcessed=false → đơn về chưa đăng | EF | Chặn nếu đã DaDang (callback đã về); chặn nếu chưa đủ ngưỡng phút |
| 8 | Cập nhật trạng thái giao hàng theo đơn | "Cập nhật trạng thái" | POST `api/ThuKhoTacNghiep/CapNhatTrangThaiGiaoHangTheoDon` | `ThuKhoTacNghiep_CapNhatTrangThaiGiaoHangTheoDon` | public | - | Đổi GiaoHangTrangThaiMa của đơn (thủ công) | EF | - |
| 9 | Quét mã đơn đổi trạng thái | "Quét mã" → nhập mã → đổi | POST `LayThongTinTheoMaDonHang` + `ThayDoiTrangThaiDonHang` | `ThuKhoTacNghiep_LayThongTinTheoMaDonHang` / `..ThayDoiTrangThaiDonHang` | public | - | Quét mã đơn/mã vận đơn → lấy thông tin → đổi trạng thái giao hàng | EF | - |
| 10 | Admin đăng/hủy đăng đơn | (admin) | POST `AdminDangDon` / `AdminHuyDangDon` | `ThuKhoTacNghiep_AdminDangDon` / `..AdminHuyDangDon` | public | admin | Đăng/hủy đăng đơn bỏ qua một số gate (cho admin/CSKH) | EF | AdminHuyDangDon chỉ DV-AD-TONG/HT-CSKH/DV-KHO-KIEM-TRUNG |
| 11 | Đổi mã đơn | "Đổi mã đơn" | POST `api/ThuKhoTacNghiep/ThayDoiMaDon` | `ThuKhoTacNghiep_ThayDoiMaDon` | public | - | Sinh lại mã đơn | EF | - |
| 12 | Care đơn / nhãn KH | "Care" | POST `UpdateTrangThaiCareDon` / `CapNhat_NhanKhachHang_OrderLogistic` | `ThuKhoTacNghiep_UpdateTrangThaiCareDon` / `[SBAuthorize()]` (mở) | public/mixed | - | Cập nhật care đơn / nhãn khách hàng | EF | CapNhat_NhanKhachHang dùng `[SBAuthorize()]` rỗng — chỉ cần đăng nhập |
| 13 | Yêu cầu giao lại / duyệt hoàn | "Yêu cầu giao lại" / "Duyệt hoàn" | POST `GiaoVan_YeuCauGiaoLai` / `GiaoVan_DuyetHoan` | `[SBAuthorize()]` (mở) | public | - | Gửi yêu cầu giao lại / duyệt đơn hoàn sang đối tác | EF | Quyền rỗng — cần xác nhận có chủ ý |
| 14 | Chuyển đơn đến chi nhánh tạo đơn / chi nhánh tổng | (kho tổng) | POST `ChuyenDenChiNhanhTaoDon/{id}` / `ChuyenDenChiNhanhTong` | `[SBAuthorize()]` (mở) | public | dùng kho tổng | Điều chuyển đơn về chi nhánh tạo đơn hoặc chi nhánh tổng | EF | Quyền rỗng |
| 15 | Cập nhật đơn hoàn đã nhận | "Đã nhận hoàn" | POST `api/ThuKhoTacNghiep/CapNhatTrangThaiDonHangDaNhan` | `ThuKhoTacNghiep_CapNhatTrangThaiDonHangDaNhan` | public | đơn hoàn | Set list đơn thành "Đã nhận đơn hoàn" | EF | - |
| 16 | Lấy đơn theo lượt nhặt | (màn lượt nhặt) | POST `api/ThuKhoTacNghiep/GetDanhSachDonHangTheoLuotNhat` | `[SBAuthorize]` (mở) | public | - | List đơn theo lượt nhặt hàng | EF | - |
| 17 | Cập nhật trạng thái phát hành hóa đơn | (nút) | POST `api/ThuKhoTacNghiep/CapNhatTrangThaiPhatHanhHoaDon` | `[SBAuthorize]` (mở) | public | - | Cập nhật trạng thái phát hành hóa đơn cho đơn | EF | Permission key bị comment, chỉ còn `[SBAuthorize]` rỗng (KhoTacNghiepController.cs:614-616) |
| 18 | Xuất Excel tác nghiệp (4 kiểu) | "Export To Excel" (kiểu 1-4) | POST `ExportExcelKhoTacNghiepKieu1..4` | `[SBAuthorize()]` (mở) | public | - | Xuất Excel danh sách tác nghiệp; lỗi → 503 nếu `Service_Unavailable`, ngược lại 500 `EXPORT_ERROR`. Đều gọi nhánh `_V2` của service | EF | Cả 4 endpoint export đều quyền rỗng |
| 19 | Cập nhật ngày muốn giao hàng | "Đổi ngày giao" | POST `api/ThuKhoTacNghiep/UpdateNgayMuonNhanHang` | `ThuKhoTacNghiep_UpdateNgayMuonNhanHang` | public | - | Đổi ngày khách muốn nhận hàng | EF | - |

## Endpoint nội bộ (private / service-to-service)
- [internal] `InsertOrUpdateBlackListPrivate` / `DeleteBlackListPrivate` (`[PrivateSBAuthorize]`) — service khác (Contact) gọi sang để đồng bộ blacklist số điện thoại theo đơn logistic.
- Nhóm ghi chú nội bộ (`SaveMessage`, `GetListMessageByIdDonHangLogistic`, `GetListMessageByPhone`) + blacklist công khai + `UpdateGhiChuKeToan` — thao tác phụ trợ, không phải hành động chính.
- `DangKyNhanTrangThaiPushsale` (`[SBAuthorize]` rỗng) — đăng ký nhận trạng thái từ Pushsale (đối tác đẩy callback trạng thái về).

## Quy tắc nghiệp vụ (gating đăng đơn)
1. **Quyền đăng đơn** = admin (DV-AD-*, HT-CSKH, DV-KHO-KIEM-TRUNG, IsSuperUser) **HOẶC** thỏa cấu hình đơn vị (KhoTacNghiepService.cs:5300-5340):
   - sale + `IsSaleDangDon=true` → OK
   - role kho + `ChoPhepKhoDangDon=true` (mặc định true) → OK
   - role kế toán + `ChoPhepKeToanDangDon=true` (mặc định true) → OK
   - không thỏa → "Đơn vị chưa được cấu hình đăng đơn".
2. **Hủy chờ callback** (action 7): chỉ khi `IsProcessed=true`, `GiaoHangTrangThaiMa < DaDang`, và `ProcessedDate` đã quá `SoPhutChoCallbackDangDon` phút (mặc định 3') — tránh hủy đơn vừa mới gửi đối tác (KhoTacNghiepService.cs:4118-4123).
3. **Hủy đăng đơn**: chặn đơn TMĐT (Shopee/Tiktok), chặn đơn đã đối soát, chặn đơn chưa DaDang / chưa có NgayDangDon (KhoTacNghiepService.cs:4154-4172).

## Quan hệ
- **Gọi sang:** DeliveryConnect (tạo/hủy vận đơn ĐTGV), WarehouseProduct (`DangHuyDon_V2` hoàn kho/trừ kho chờ xuất), Contact (blacklist, nhãn KH), Loyalty (qua đổi trạng thái → NghiepVuPending).
- **Dữ liệu (entities):** `DonHangLogistic` (đơn chính + GiaoHangTrangThaiMa, IsProcessed, ProcessedDate, InvNo...), `DonHangDangDon`/`DonHangHuyDangDon` (hàng đợi), `DonHangLogisticLogs`, `DonHangLogisticMessage`, `ContactPhoneBlacklist`.
- **Ripple / đồng bộ:** đăng đơn → vận đơn ĐTGV + trừ tồn chờ xuất + ES reindex đơn; hủy đăng/hủy chờ callback → hủy vận đơn + hoàn kho.

## Luồng xuyên module (đăng đơn end-to-end — pattern Insert→Process có lock)
1. User chọn đơn → `InsertDonHangDangDon`: validate per-đơn → ghi bản ghi `DonHangDangDon` (ChuaXuly). KHÔNG gọi đối tác.
2. FE poll `ProcessDonHangDangDon`: SP `..._DonHangDangDon_GetForDangDon` bốc 1 bản ghi + **lock** (chống 2 user bốc cùng đơn) → validate lại đơn (chốt đơn, có kho, chưa DaDang, chưa đối soát) → gửi lệnh tạo vận đơn sang đối tác giao vận → set DaDang(20) + trừ `SoLuongChoXuat`.
3. Đối tác callback (DeliveryConnect) → cập nhật `GiaoHangTrangThaiMa` (DaLayHang/DangGiaoHang/DaGiaoHang...).
4. Hủy chờ callback (`RollbackChoPartnerDangDon`): đơn kẹt in-flight quá ngưỡng → `DangHuyDon_V2` hoàn kho + reset IsProcessed → đơn về chưa đăng, cho đăng lại.

## Lỗi đã biết / lưu ý kỹ thuật
- **Tên file ≠ tên class**: `KhoTacNghiepController.cs` chứa class `ThuKhoTacNghiepController` (route `api/ThuKhoTacNghiep`) — dễ nhầm khi tìm file.
- Nhiều endpoint nhạy cảm dùng `[SBAuthorize()]` / `[SBAuthorize]` rỗng (4 export, GiaoVan_YeuCauGiaoLai, GiaoVan_DuyetHoan, ChuyenDenChiNhanh*, CapNhat_NhanKhachHang, CapNhatTrangThaiPhatHanhHoaDon, DangKyNhanTrangThaiPushsale, GetDanhSachDonHangTheoLuotNhat) — chỉ cần đăng nhập là gọi được. Cần xác nhận có chủ ý.
- Permission key `ThuKhoTacNghiep_CapNhatTrangThaiPhatHanhHoaDon` đã bị comment (KhoTacNghiepController.cs:614) → endpoint cập nhật phát hành hóa đơn hiện không gắn quyền riêng.
- Service `KhoTacNghiepService.cs` ~20.000 dòng — bug-prone, mỗi sửa đổi cần trace riêng nhánh đăng đơn / hủy đăng / đổi trạng thái.
- `RollbackChoPartnerDangDon` dựa vào `DangHuyDon_V2` (commonService) idempotent để hoàn kho — nếu hoàn kho không idempotent sẽ cộng kho 2 lần khi gọi lặp (KhoTacNghiepService.cs:4126).
