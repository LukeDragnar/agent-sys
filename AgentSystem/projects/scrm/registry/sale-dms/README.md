---
module: sale-dms
service: SaleDMS
scan_snapshot: e617abcbbf (origin/release_product, 2026-07-15)
doc_status: draft — Snapshot, verify code trước khi tin số/behavior (kho là bản đồ, code là chân lý)
watermark_note: "Module NET-NEW so với watermark 72ac442a (khi đó folder SaleDMS/ chưa tồn tại). Sau khi merge draft này, set watermark SaleDMS = e617abcbbf."
kind: module-index          # index cho 11 controller node — mỗi controller nên tách file riêng khi đắp Pha 2
controllers: [TuyenController, MobileController, CheckInController, DiaDiemController, DaiLyKhoController, DaiLyKhoChiTietController, DaiLyPhieuNhapController, DaiLyPhieuXuatController, DashboardDMSController, CauHinhController, CommonController]
tien_ich: TI_SaleDMS
roles: [DV-AD-TONG, DV-AD-CN, DV-QL-SALE, DV-SALE-LEAD, DV-SALE]
db: SCRM_SaleDMSStr
entities: [Tuyen, TuyenDiaDiem, TuyenSale, TuyenMauPhanCa, TuyenPhanCaThuCong, HuyLich, DiaDiem, DiaDiemCheckIn, DaiLyKho, DaiLyKhoChiTiet, DaiLyKhoChiTietLichSu, DaiLyCanKho, DaiLyCanKhoChiTiet, DaiLyPhieuNhap, DaiLyPhieuNhapSanPham, DaiLyPhieuXuat, DaiLyPhieuXuatSanPham, CauHinh, User]
cross_module_in: [Core (EventUserSyncData), OrderLogistic/Core order-events (CheckInDaTaoDonDMSMessage, TaoPhieuNhapDaiLyDMSMessage, TaoPhieuXuatDaiLyDMSMessage)]
cross_module_out: [File service (/Files/UploadFile — proxy ảnh checkin)]
deprecated: false
---

# SaleDMS — Distribution Management System (quản lý bán hàng đi tuyến)

> **Snapshot @ e617abcbbf. Đây là DRAFT sinh từ delta-scan — verify lại path:line với code hiện tại trước khi tin.**
> Module .NET độc lập (service `SaleDMS`, DB `SCRM_SaleDMSStr`), ~105 file / ~11.839 dòng: 11 controller + 19 entity + 4 MassTransit consumer + ~15 stored-proc. 5 commit trong range chỉ hotfix lịch-trình-sale + fix config + bump version prod; toàn bộ nội dung là **net-new** (không có behavior cũ bị đổi).

Nghiệp vụ cốt lõi:
1. **Tuyến đi bán** — CRUD tuyến, `LuuTongHop` atomic (tuyến + sale + rule + địa điểm), gán sale phụ trách, lịch "chip" phân ca (mẫu phân ca / phân ca thủ công / hủy / khôi phục chip).
2. **Check-in/out điểm bán (Mobile)** — validate chip lịch + GPS (Haversine) + upload ảnh (proxy sang File service), workflow duyệt/từ chối/chấm-công-hộ (bypass validate), duyệt hàng loạt.
3. **Địa điểm/điểm bán** — sale tạo qua mobile → chờ duyệt; admin auto duyệt; cập nhật thứ tự.
4. **Kho đại lý** — tồn kho, cân kho (đối chiếu tồn → sinh phiếu nhập + xuất chung `IdTransaction` trong 1 transaction), phiếu nhập/xuất đại lý (thủ công + tự động sinh từ event đơn hàng).
5. **KPI cá nhân sale** + **Dashboard giám sát đội** (4 widget tách API riêng).

Gate quyền toàn module theo bộ role `DV-AD-TONG / DV-AD-CN / DV-QL-SALE / DV-SALE-LEAD / DV-SALE`; TienIch = `TI_SaleDMS`. Controller-level đều `[SBAuthorize()]`, method-level gate bằng permission key `SaleDMS_<Controller>_<Action>` (trừ nhóm CAN-KHO — xem known-issue).

## Controller — bảng tổng quan (module index)

| Controller | Route | Mục đích | Auth | Known-issue |
|---|---|---|---|---|
| TuyenController | `api/Tuyen` | CRUD tuyến + `LuuTongHop` atomic + gán sale + chip lịch (`ThemPhanCaThuCong`/`HuyChip`/`KhoiPhucChip`). **[ADMIN web]** — sale không có quyền tạo/sửa tuyến | public (token) | — |
| MobileController | `api/Mobile` | API mobile sale: `Sale_CheckIn`/`Sale_CheckOut` (GPS + ảnh multipart), `Sale_TaoDiaDiem`, `Sale_LayLichToi`, KPI cá nhân | public | proxy upload cần header `SystemAuthorization` (xem dưới) |
| CheckInController | `api/CheckIn` | Admin duyệt/từ chối/bỏ checkin, `ChamCongHoSale` (bypass validate), duyệt/từ chối hàng loạt | public | — |
| DiaDiemController | `api/DiaDiem` | CRUD điểm bán tuyến + workflow duyệt (`LayDsChoDuyet`/`Duyet`/`TuChoi`) + `CapNhatThuTu` + picker | public | — |
| DaiLyKhoController | `api/DaiLyKho` | CRUD kho đại lý + CAN-KHO (`CapNhatTonKhoDiemBan` sinh phiếu nhập+xuất, `LichSuCanKho`, `ChiTietCanKho`) | public | **CAN-KHO gate bare `[SBAuthorize]`** (xem dưới) |
| DaiLyKhoChiTietController | `api/DaiLyKhoChiTiet` | Tồn kho đại lý + nhóm SP theo kho DMS | public | — |
| DaiLyPhieuNhapController | `api/DaiLyPhieuNhap` | CRUD phiếu nhập đại lý (trạng thái Nháp chưa cộng tồn) | public | — |
| DaiLyPhieuXuatController | `api/DaiLyPhieuXuat` | CRUD phiếu xuất đại lý (sale bán hàng) | public | — |
| DashboardDMSController | `api/DashboardDMS` | 4 widget giám sát đội: `DoPhu`/`TuanThu`/`DoanhSoTheoNgay`/`DoanhSoTheoTuyen` (tách API riêng) | public | — |
| CauHinhController | `api/CauHinh` | Cấu hình SaleDMS per đơn-vị + chi-nhánh (bán kính checkin...), pattern JSON 1-row | public | — |
| CommonController | `api/Common` | Dropdown sale/user/người-cân-kho | public | — |

## Hành động chi tiết — controller trọng tâm

### TuyenController (`api/Tuyen`) — verify `TuyenController.cs:22`
Toàn bộ endpoint **[ADMIN web]** (comment class:12). Permission key + role (từ attribute):

| # | Hành động | Endpoint | Quyền (key) | Role | Hành vi đúng |
|---|---|---|---|---|---|
| 1 | Danh sách tuyến | `POST LayDs` | `SaleDMS_Tuyen_LayDs` | tất cả 5 role | paging danh sách tuyến theo scope user |
| 2 | Chi tiết tuyến | `POST LayChiTiet/{id}` | `SaleDMS_Tuyen_LayChiTiet` | tất cả 5 role | header + sale + rule + địa điểm |
| 3 | Tạo/sửa tuyến | `POST Luu` | `SaleDMS_Tuyen_Luu` | AD-TONG, AD-CN | — |
| 4 | Xóa tuyến | `POST Xoa/{id}` | `SaleDMS_Tuyen_Xoa` | AD-TONG, AD-CN | — |
| 5 | Lưu tổng hợp (atomic) | `POST LuuTongHop` | `SaleDMS_Tuyen_LuuTongHop` | AD-TONG, AD-CN | tuyến + sale + rule + địa điểm trong 1 transaction |
| 6 | Sale phụ trách | `POST LayDsSale/{idTuyen}` · `GanSale` · `KetThucGanSale` · `XoaGanSale/{idGan}` | `SaleDMS_Tuyen_LayDsSale`/`GanSale`/`KetThucGanSale`/`XoaGanSale` | AD-TONG, AD-CN, QL-SALE | `KetThucGanSale` set ngày kết thúc (không xóa cứng) |
| 7 | Lịch chip | `POST LayLich` | `SaleDMS_Tuyen_LayLich` | tất cả 5 role | calendar chip theo tuyến |
| 8 | Chip actions | `POST ThemPhanCaThuCong` · `XoaPhanCaThuCong/{id}` · `HuyChip` · `KhoiPhucChip` | `SaleDMS_Tuyen_ThemPhanCaThuCong`/`XoaPhanCaThuCong`/`HuyChip`/`KhoiPhucChip` | Thêm/Xóa PhanCaThuCong: +QL-SALE; Huy/KhoiPhuc: chỉ AD-TONG, AD-CN | Hủy/khôi phục chip (soft) |

### MobileController (`api/Mobile`) — verify `MobileController.cs:31`
- `POST Sale_CheckIn` / `Sale_CheckOut` — multipart (GPS + ảnh); ảnh gom CSV `;` lưu `DsAnhCheckIn`/`DsAnhCheckOut`. Upload lỗi → trả error rõ ("Upload ảnh thất bại: ...").
- `POST Sale_TaoDiaDiem` (`SaleDMS_Mobile_Sale_TaoDiaDiem`, role DV-SALE/DV-SALE-LEAD/DV-QL-SALE) — sale tạo địa điểm mới, status auto **chờ duyệt**.
- `POST Sale_LayLichToi` — lịch của chính sale, auto filter theo token (không truyền `idsSale`).
- KPI cá nhân: `Sale_TongQuan` / `Sale_KpiCuaToi`.

## Endpoint nội bộ / cross-service

- **[out → File service]** `MobileController.UploadPhotosAsync` (`MobileController.cs:106`) proxy từng ảnh checkin sang `{FileServiceUrl}/Files/UploadFile` (KHÔNG có prefix `/api/`), gom Id trả CSV `;`. Phải tự gắn custom header `SystemAuthorization = "Bearer " + ServiceInfo.Token` + `SystemUserId` (line 121–125) vì `ApiPostFormData` **không** auto-gắn header service-to-service.
- **[in ← Core]** `EventUserSyncDataConsumer` (`EventUserSyncDataConsumer.cs:14`) consume `EventUserSyncData` → SqlSugar upsert bảng `User` sang DB `SCRM_SaleDMSStr` (alias namespace tránh xung đột `SB.Core.User`).
- **[in ← order-events]** `CheckInDaTaoDonDMSConsumer` (`CheckInDaTaoDonDMSConsumer.cs:12`) consume `CheckInDaTaoDonDMSMessage` khi đơn có gắn điểm bán DMS được tạo → set cờ `IsDaTaoDon` trên bản ghi checkin khớp (sale, điểm bán, ngày); không khớp → bỏ qua.
- **[in ← order-events]** `TaoPhieuNhapDaiLyDMSConsumer` — đơn hàng **VÀO** trạng thái "Đã giao hàng"/"Đã thanh toán" → tự sinh phiếu **NHẬP** kho đại lý.
- **[in ← order-events]** `TaoPhieuXuatDaiLyDMSConsumer` (`TaoPhieuXuatDaiLyDMSConsumer.cs:13`) — đơn hàng **RỜI** trạng thái "Đã giao"/"Đã thanh toán" (Đã hoàn/Đang hoàn/Hủy) → tự sinh phiếu **XUẤT** để đảo phiếu nhập. Điều kiện "đã từng nhập" + chống trùng theo `IdDonHangGoc` xử lý trong service.

> ⚠️ Correction vs bản phân tích delta: analysis ghi "cả Nhập lẫn Xuất sinh khi đơn vào Đã giao/Đã thanh toán". Theo comment code `TaoPhieuXuatDaiLyDMSConsumer.cs:8-11`: **NHẬP = khi đơn VÀO** trạng thái đó, **XUẤT = khi đơn RỜI** (đảo phiếu). Verify lại service `DaiLyPhieuXuatService.TaoTuDonHangRoiKhoAsync` / `DaiLyPhieuNhapService` khi đắp node chi tiết.

## Luồng xuyên module (ripple)

- **Đồng bộ User:** Core phát `EventUserSyncData` → SaleDMS clone bảng User. Đổi contract `EventUserSyncData` ở Core → gãy sync user DMS.
- **Cờ đã-tạo-đơn (KPI):** tạo đơn gắn điểm bán DMS → `CheckInDaTaoDonDMSMessage` → set `IsDaTaoDon`. Ảnh hưởng KPI cá nhân sale.
- **Tồn kho đại lý auto:** đơn hàng vào/rời "Đã giao/Đã thanh toán" → `TaoPhieuNhap/XuatDaiLyDMSMessage` → tự sinh phiếu nhập/xuất kho đại lý (chống trùng theo `IdDonHangGoc`). Đổi contract 3 message này hoặc state-machine đơn ở Core/OrderLogistic → gãy tồn kho DMS.

> **Chiều phụ thuộc:** SaleDMS là **consumer downstream** của event Core + OrderLogistic. SaleDMS không tự gọi ngược (trừ proxy ảnh sang File service). Khi sửa order-flow bên Core/OrderLogistic → check ripple sang 4 consumer này.

## Lỗi đã biết / lưu ý kỹ thuật

- **[known-issue — CAN-KHO gate bare]** `DaiLyKhoController` 4 endpoint cân-kho chỉ gate `[SBAuthorize]` trần, permission key chi tiết bị **comment out**:
  - `CapNhatTonKhoDiemBan` (`DaiLyKhoController.cs:25-27`, attribute key comment line 26)
  - `LayTonTheoSanPham` (`:37-38`)
  - `LichSuCanKho` (`:46-48`)
  - `ChiTietCanKho` (`:56-58`)
  → Chỉ chặn theo token + giới hạn dữ liệu theo `DonViId`/`IdChiNhanh` **trong service** (chủ ý — ghi rõ ở `CommonController.LayDsNguoiCanKho`). Các endpoint CRUD kho khác (`TimTheoDieuKien`/`ThemMoi`/`Xoa`...) vẫn có key đầy đủ. → Khi audit RBAC: đây là **cố ý**, không phải bug bỏ sót; nhưng lưu ý sale bất kỳ (miễn có token hợp lệ) gọi được 4 endpoint này, phân quyền dựa hoàn toàn vào lọc dữ liệu tầng service.
- **[proxy upload]** `MobileController.UploadPhotosAsync` (`:106`) — nếu **quên** header `SystemAuthorization` + `SystemUserId`, File service đọc token từ cookie (rỗng) → **401**. `ApiPostFormData` KHÔNG auto-gắn header service-to-service (khác các method ClientHelper khác). URL đúng = `{FileServiceUrl}/Files/UploadFile`, KHÔNG có prefix `/api/` (`FilesController` không có `[Route("api/[controller]")]` class-level).
- **[multipart trailing char]** Checkin mobile phải trim trailing `\n`/`\r`/space field `Ngay` do multipart client (OkHttp/iOS) hay thêm ký tự thừa.

## Nguồn (path:line — verify @ e617abcbbf)

- `SaleDMS/SaleDMS.API/Controllers/TuyenController.cs:22` · `MobileController.cs:31` (upload `:106`) · `CheckInController.cs:21` · `DiaDiemController.cs:25` · `DaiLyKhoController.cs:13` (`CapNhatTonKhoDiemBan:25`) · `DaiLyPhieuXuatController.cs:13` · `DaiLyPhieuNhapController.cs:13` · `DashboardDMSController.cs:13` · `CauHinhController.cs:17` · `CommonController.cs` · `DaiLyKhoChiTietController.cs`
- Consumer: `EventUserSyncDataConsumer.cs:14` · `CheckInDaTaoDonDMSConsumer.cs:12` · `TaoPhieuNhapDaiLyDMSConsumer.cs` · `TaoPhieuXuatDaiLyDMSConsumer.cs:13`
- Service: `SaleDMS.Service/Generated/TuyenService.cs` (~1340 dòng) · `CheckInService.cs` (~1003 dòng) · `DaiLyTonKhoService` (cân kho) · `DaiLyPhieuXuatService.TaoTuDonHangRoiKhoAsync`
- SP/scripts: `SaleDMS.DataContext/Scripts/` (28_DonHangLogistic_ThongKeDonSale, 31/32_DashboardDoi_DoanhSo, 34_DaiLyCanKho.sql...)

## TODO đắp kho (không thuộc draft này — gợi ý cho maintainer)

- Tách 11 controller node riêng theo `_SCHEMA.md` (draft này là index gộp).
- `features/SaleDMS/`: prose cho quản-ly-tuyen-va-chip-lich, checkin-gps-diem-ban, kho-dai-ly-can-kho (đối chiếu tồn → phiếu nhập+xuất atomic + auto từ event đơn), kpi-ca-nhan-sale, dashboard-giam-sat-doi.
- Thêm SaleDMS vào `projects/scrm/architecture.md` (danh sách ~40 service) + `project.json` modules.
- Cân nhắc lesson/convention: pattern proxy upload cross-service cần `SystemAuthorization` (`ApiPostFormData` không auto-gắn header).
- Set watermark SaleDMS = `e617abcbbf` sau khi merge.
