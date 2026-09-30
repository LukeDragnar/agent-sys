---
controller: DonViKhoChiTietSyncController
module: kho
service: WarehouseProduct
man_hinh_ui: "Quản trị (super-admin) > Quản lý sync đơn vị kho chi tiết (route quanly-sync-donvi-khochitiet) — list đơn vị đang bật sync KhoChiTiet V2, nút Resync/Xóa index theo đơn vị; + tab cấu hình super-admin (cauhinh-sync-donvi-khochitiet). KHÔNG phải màn cho user đơn vị thường."
muc_dich: "Quản trị hệ thống cấu hình + vận hành ETL đồng bộ 'kho chi tiết V2' (KhoChiTietLichSu) cho từng đơn vị: thêm/cập nhật danh sách đơn vị sync, bật/tắt phương thức sync, đẩy job resync lại lịch sử phiếu nhập/xuất/chuyển qua RabbitMQ, hoặc xóa index. Là cửa điều khiển batch reindex cho tính năng tồn kho chi tiết."
alias: ["sync đơn vị kho chi tiết", "DonViKhoChiTietSync", "reindex kho chi tiết", "resync kho", "kho chi tiết V2", "đồng bộ lịch sử kho", "quản lý sync đơn vị kho"]
auth: internal
state_machine: []
deprecated: false
entities: [DonViKhoChiTietSync, KhoChiTiet, KhoChiTietLichSu]
stored_procs: []
depends:
  - "DonViKhoChiTietSyncService (IDonViKhoChiTietSyncService — CRUD bảng DonViKhoChiTietSync + GetAll/GetDonViByIds)"
  - "KhoChiTietLichSuService (IKhoChiTietLichSuService — SyncKhoChiTietLichSu, chỉ FixBug dùng)"
  - "IPublishEndpoint (MassTransit) — publish EventSyncDonViKhoChiTiet / EventDeleteSyncDonViKhoChiTiet"
  - "CauHinhService (GetAllCauHinhSuperAdmin — đọc JsonSetting_SuperAdmin.Allows_Use_KhoChiTietSync)"
  - "Core DB (đọc DonViKhoModel qua CoreConnectStr để lấy đơn vị còn hạn dùng)"
cross_module_in: []
cross_module_out:
  - "WarehouseProductConsumer.API (EventSyncDonViKhoChiTietConsumer → ProcessSyncPhieuNhap/Xuat/Chuyen qua KhoChiTietLichSuService — reindex KhoChiTietLichSu)"
  - "EventDeleteSyncDonViKhoChiTiet consumer (xóa index kho chi tiết của đơn vị)"
---

# DonViKhoChiTietSyncController — Quản trị đồng bộ kho chi tiết V2 (reindex theo đơn vị)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Quét + đồng bộ danh sách đơn vị sync | (tác vụ nền / nút super-admin "AddOrUpdate đơn vị") | POST `api/DonViKhoChiTietSync/AddOrUpdateDonVi` | `DonViKhoChiTietSync_AddOrUpdateDonVi` (HT-AD, HT-CSKH) | internal | super-admin | Quét all đơn vị Core còn hạn dùng (NgayHetHanSuDung ≥ hôm nay-1 tháng) → BulkInsertOrUpdate vào `DonViKhoChiTietSync`, xóa đơn vị không còn hợp lệ. Cờ `V2KhoChiTietSuDung` lấy NGHỊCH ĐẢO của `Allows_Use_KhoChiTietSync` trong cấu hình super-admin | EF (Bulk) + Core DB | `AddOrUpdateDonvi` luôn trả `ResultWithData(true)` kể cả khi transaction rollback (nuốt lỗi, xem service) |
| 2 | Tìm/list đơn vị sync theo điều kiện | "Tìm kiếm" → grid | POST `api/DonViKhoChiTietSync/TimTheoDieuKien` | `DonViKhoChiTietSync_TimTheoDieuKien` (HT-AD, HT-CSKH) | internal | màn list | Lọc theo KeyWord (Ten), IsSuDungV2KhoChiTietSuDung, trạng thái phiếu Nhập/Xuất/Chuyển + phân trang | EF | - |
| 3 | Bật/tắt phương thức sync | switch "Sử dụng" / apply-for-all | POST `api/DonViKhoChiTietSync/UpdateUseKhoChiTietSync` | `DonViKhoChiTietSync_UpdateUseKhoChiTietSync` (HT-AD, HT-CSKH) | internal | per-row hoặc toàn bộ | Set `V2KhoChiTietSuDung` cho list đơn vị (`ListIdDonVi`) hoặc tất cả nếu `IsApplyForAll=true` | EF | - |
| 4 | Resync lịch sử kho (cũ) | nút "Resync" | POST `api/DonViKhoChiTietSync/ResyncKhoChiTiet` | `DonViKhoChiTietSync_ResyncKhoChiTiet` (HT-AD, HT-CSKH) | internal | có chọn đơn vị hoặc bỏ trống = all | Publish `EventSyncDonViKhoChiTiet` (TuNgay/DenNgay) cho từng đơn vị; bỏ trống `DanhSachDonVi` → đẩy cho TẤT CẢ đơn vị | EF + RabbitMQ | Bỏ trống danh sách = resync ALL đơn vị → tải nặng, cần thận trọng |
| 5 | Resync lịch sử kho theo đơn vị (V2) | nút "Resync theo đơn vị" | POST `api/DonViKhoChiTietSync/ResyncKhoChiTietAllByDonVi` | `DonViKhoChiTietSync_ResyncKhoChiTietAllByDonVi` (HT-AD, HT-CSKH) | internal | chọn đơn vị | Publish `EventSyncDonViKhoChiTiet` kèm cờ `IsSyncPhieuNhap/Xuat/Chuyen` (chọn loại phiếu cần reindex) theo từng đơn vị | EF + RabbitMQ | - |
| 6 | Xóa index kho chi tiết theo đơn vị | nút "Xóa index" | POST `api/DonViKhoChiTietSync/DeleteKhoChiTietSyncAllByDonVi` | `DonViKhoChiTietSync_DeleteKhoChiTietSyncAllByDonVi` (HT-AD) | internal | chọn đơn vị | Publish `EventDeleteSyncDonViKhoChiTiet` cho từng đơn vị → consumer xóa dữ liệu index kho chi tiết | EF + RabbitMQ | Chỉ HT-AD; thao tác phá data index, không hoàn tác tự động |
| 7 | Resync full toàn bộ đơn vị | nút "Resync full" | POST `api/DonViKhoChiTietSync/ResyncFullDonVi` | `DonViKhoChiTietSync_ResyncFullDonVi` (HT-AD) | internal | chỉ SuperUser | Chặn nếu `CurrentUser.IsSuperUser != true`; publish `EventSyncDonViKhoChiTiet` (không filter ngày) cho TẤT CẢ đơn vị | EF + RabbitMQ | Double-gate: vừa quyền HT-AD vừa check `IsSuperUser`; thao tác cực nặng |

## Endpoint nội bộ (private / service-to-service)
- `FixBug` (POST, `[SBAuthorize()]`) — **DEAD**: return `true` ngay dòng đầu; code sau (`SyncKhoChiTietLichSu` cho 1 transaction hardcode GUID) là unreachable dead code, để fix-bug thủ công. `DonViKhoChiTietSyncController.cs:166-184`.

## Quy tắc nghiệp vụ (gating / ETL)
1. **Tiện ích gate controller-level:** `[TienIch("DonViKhoChiTietSyncController", "Quản lý đơn vị đồng bộ kho", MaTienIchSCRM.TI_HeThong, true)]` — gate theo tiện ích hệ thống (cờ thứ 4 = true).
2. **Cờ sync nghịch đảo cấu hình:** `V2KhoChiTietSuDung = !Allows_Use_KhoChiTietSync` khi AddOrUpdate (DonViKhoChiTietSyncService.cs:168,205) — dễ nhầm dấu khi đọc data.
3. **Lọc đơn vị quét:** chỉ đơn vị `IsDelete=false` và `NgayHetHanSuDung` không null & ≥ (hôm nay − 1 tháng) (DonViKhoChiTietSyncService.cs:96-97).
4. **ResyncKhoChiTiet bỏ trống danh sách → ALL đơn vị**; ResyncFullDonVi yêu cầu thêm `IsSuperUser`.

## Quan hệ
- **Gọi sang:** `DonViKhoChiTietSyncService` (CRUD bảng DonViKhoChiTietSync), `CauHinhService.GetAllCauHinhSuperAdmin`, Core DB (DonViKhoModel) qua CoreConnectStr. KhoChiTietLichSuService chỉ dùng trong FixBug (dead).
- **Dữ liệu (entities):** `DonViKhoChiTietSync` (Id=IdDonVi, Ten, V2KhoChiTietSuDung, V2KhoChiTietNgayIndex/Total/Status PhieuNhap·PhieuXuat·PhieuChuyen). Job thực tế đụng `KhoChiTietLichSu`.
- **Ripple / đồng bộ:** controller chỉ PUBLISH event; xử lý thật ở `WarehouseProductConsumer.API` (`EventSyncDonViKhoChiTietConsumer`) — enqueue background task → `KhoChiTietLichSuService.ProcessSyncPhieuNhap/Xuat/Chuyen` reindex lịch sử kho chi tiết.

## Luồng xuyên module (resync kho chi tiết end-to-end)
1. Super-admin mở màn → bấm Resync (chọn đơn vị + loại phiếu) → controller publish `EventSyncDonViKhoChiTiet` lên RabbitMQ.
2. `EventSyncDonViKhoChiTietConsumer.Consume` (WarehouseProductConsumer.API) tách theo cờ IsSyncPhieuNhap/Xuat/Chuyen → enqueue 3 background queue riêng.
3. Background task → `KhoChiTietLichSuService.ProcessSyncPhieuNhap/Xuat/Chuyen` → rebuild dữ liệu kho chi tiết (KhoChiTietLichSu) cho đơn vị.
4. Xóa index: `EventDeleteSyncDonViKhoChiTiet` → consumer xóa index của đơn vị.

## Lỗi đã biết / lưu ý kỹ thuật
- `DonViKhoChiTietSyncController.cs:166-184` — `FixBug` là dead stub: `return true` ngay đầu hàm, toàn bộ code SyncKhoChiTietLichSu (hardcode IdDonVi/IdTransaction GUID) không bao giờ chạy.
- `DonViKhoChiTietSyncService.cs:243-252` — `AddOrUpdateDonvi` nuốt exception trong execution strategy: catch set `isSuccess=false` + `errorMessage` nhưng cuối hàm vẫn `return ResultWithData(true)` → lỗi bulk insert/delete bị che, caller luôn thấy thành công.
- `DonViKhoChiTietSyncService.cs:135` — điều kiện guard cấu hình rỗng dùng `== null || ... && ...` thiếu ngoặc (toán tử precedence) → có thể không chặn đúng trường hợp `Data` rỗng.
- `EventSyncDonViKhoChiTietConsumer.cs:58,77,93,109` — consumer catch `Exception` rỗng (nuốt lỗi) ở cả Consume lẫn 3 Process → job sync fail không log, khó phát hiện.
- `ResyncKhoChiTiet` bỏ trống `DanhSachDonVi` → resync TẤT CẢ đơn vị (DonViKhoChiTietSyncController.cs:79-93); thận trọng tránh ddos hệ thống.
- Controller dùng `IndexName` trên message ở consumer nhưng vài event publish không set TuNgay/DenNgay (ResyncFullDonVi) → cần xác nhận downstream xử lý null đúng.
