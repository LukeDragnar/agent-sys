---
node: GiaVonTheoKyController
module: WarehouseProduct
service: WarehouseProduct.API
route: api/GiaVonTheoKy
tien_ich: TI_Kho (TienIch "GiaVonTheoKyController" — "Tính lại giá vốn theo kỳ")
kind: controller
status: new
snapshot: e617abcbbf
verify: "Snapshot — kho là bản đồ, code là chân lý. Mọi path:line PHẢI mở lại source verify trước khi tin (behavior/field có thể đã đổi sau watermark)."
depends_on:
  - GiaVonTheoKyJobService (WarehouseProduct.Service)
  - MassTransit / RabbitMQ (saga 7 phase)
cross_module_out:
  - OrderProduct (Order service) — Phase 5 PublishDonHang: apply lại GiaVon lên đơn hàng
  - OrderLogistic — Phase 6 PublishDonHangLogistic: apply GiaVon lên đơn logistic
  - Accounting (Kế toán) — Phase 7 SyncAccounting
  - ElasticSearch — Phase 3 re-index KhoChiTiet đã đổi giá vốn
---

# GiaVonTheoKyController — Tính lại giá vốn theo kỳ

> ⚠️ **Snapshot @ `e617abcbbf`** (HEAD origin/release_product). Kho là **bản đồ**, code là **chân lý**: mở lại source qua codemap/ripgrep verify path:line trước khi dựa vào. Node MỚI (chưa có trong kho trước watermark `72ac442a`).

## Tóm tắt

Controller MỚI cho tính năng **tính lại giá vốn theo kỳ** (recalculate average cost theo kỳ Tháng/Quý/Năm). Mở từ modal trên màn `/warehouse-product/quan-ly-ton-kho` (FE). Mỗi lần chạy = **INSERT 1 row `GiaVonTheoKyJob`** (bảng là log lịch sử, không UPDATE-from-history), rồi chạy dưới dạng **saga 7 phase qua MassTransit** — mỗi phase là 1 message + consumer riêng, tự chain phase kế khi xong.

- Controller: `WarehouseProduct/WarehouseProduct.API/Controllers/GiaVonTheoKyController.cs` (verify: 12 endpoint tại `:28-92`).
  - ⚠️ **Doc-drift**: XML summary dòng `:12` ghi "9 endpoint" nhưng thực tế có **12 action** — đừng tin comment, đếm từ source.
- Facade service: `WarehouseProduct.Service/Generated/GiaVonTheoKy/GiaVonTheoKyJobService.cs`.
- Apply/saga logic: `.../GiaVonTheoKy/GiaVonTheoKyApplyService.cs`, `AvgEligibleImportRecalculator.cs`.
- Consumers: `WarehouseProductConsumer.API/Consumer/GiaVonTheoKy/PhaseConsumers.cs` (7 phase consumer), `CrossServiceApplyBatchDoneConsumer.cs`, `EventMarkSnapshotsStaleConsumer.cs`.
- Background job treo: `WarehouseProductConsumer.API/Jobs/JobScanGiaVonTheoKyStuck.cs`.
- Message contract: `Libs/Models.Core/MessageBroker/RecalculateGiaVonTheoKyPhaseMessages.cs`.

## Endpoints (12) — tất cả `[SBAuthorize]`, quyền + feature-flag check trong service (`PreCheckAsync`)

| # | Method | Route | Body/Param | Hành vi đúng |
|---|--------|-------|-----------|--------------|
| 1 | GET  | `DangCoTienTrinhDangChay` | — | Có job đang chạy cho đơn vị hiện tại không (gate 1-job-per-đơn-vị) |
| 2 | POST | `EnqueueRecalculate` | `EnqueueRecalculateGiaVonTheoKyRequest` | INSERT job + Publish `RecalcGVKy_Phase1_ComputeMessage`. Ghi lại IP client |
| 3 | POST | `KiemTraChongLan` | `KiemTraChongLanRequest` | Kiểm tra kỳ chồng lấn với job đã chạy |
| 4 | POST | `UocLuongPhamVi` | `UocLuongPhamViRequest` | Ước lượng phạm vi ảnh hưởng (số SP/kho/phiếu/đơn) trước khi chạy |
| 5 | POST | `TimTheoDieuKien` | `GiaVonTheoKyJobFilter` | List/tìm job theo điều kiện |
| 6 | GET  | `ChiTietJob/{idJob:Guid}` | `idJob` | Chi tiết 1 job (progress counters + phase hiện tại) |
| 7 | POST | `ChiTietSnapshot` | `ChiTietSnapshotFilter` | Chi tiết snapshot `KhoChiTietGiaVonTheoKy` của job |
| 8 | POST | `ChiTietUpdateLog` | `ChiTietUpdateLogFilter` | Chi tiết log update (Mongo `GiaVonTheoKyJobUpdateLog`) |
| 9 | POST | `HuyJob` | `HuyJobGiaVonTheoKyRequest` | Hủy job (set TrangThai=Cancelled) |
| 10 | POST | `KhoiDongLaiJob/{idJob:Guid}` | `idJob` | Khởi động lại job (retry từ phase dở) |
| 11 | POST | `LichSnapshot` | `LichSnapshotFilter` | Lịch/lịch sử snapshot theo kỳ |
| 12 | POST | `NhapGiaVonThuCong` | `NhapGiaVonThuCongRequest` | Nhập giá vốn thủ công (modal cell calendar) — UPSERT snapshot rồi tạo job `IsManualEntry=true` (Phase 1 skip compute). Ghi IP client |

## Saga 7 phase — thứ tự CHÍNH XÁC (từ `RecalculateGiaVonTheoKyPhaseMessages.cs` + `PhaseConsumers.cs`)

```
Enqueue API → INSERT GiaVonTheoKyJob → Publish Phase1
  1  ComputeSnapshot            (RecalcGVKy_Phase1_ComputeConsumer)
  2  UpdateKhoChiTietHienTai    (Phase2) ── skip nếu !IsKyBaoGomHienTai
  3  SyncES (re-index KhoChiTiet)(Phase3) ── skip cùng điều kiện !IsKyBaoGomHienTai
  4  UpdatePhieuXuat.GiaVon     (Phase4) ── LUÔN chạy, không phụ thuộc IsKyBaoGomHienTai
  5  PublishDonHang             (Phase5) → publish ApplyGiaVonOnDonHangMessage cho Order service
  6  PublishDonHangLogistic     (Phase6) → publish ApplyGiaVonOnDonHangLogisticMessage cho OrderLogistic
  7  SyncAccounting             (Phase7) → set TrangThai=Completed (không có Phase 8 message)
```

- **Payload tối thiểu** (`IdJob`, `IdDonVi`, `UserLogin?`): consumer load lại job từ DB theo `IdJob` → state luôn tươi.
- **Resilience**: restart service giữa chừng → message còn trên queue → consumer pick up đúng phase; mỗi phase retry độc lập; time-box per phase (`PhaseStartedAt`, check `Now - PhaseStartedAt > 5'`), paging dở lưu `PhaseOffset`, state phụ ở `PhaseProgress` (JSON).
- **Progress**: đếm qua counter trên job (`Tong*/Da* Snapshot/KhoChiTiet/PhieuXuat/DonHang/DonLogistic`) — FE **reload thủ công**, KHÔNG SignalR.
- **Cross-service done**: `CrossServiceApplyBatchDoneConsumer` nhận báo hoàn tất batch apply từ Order/OrderLogistic.
- **Job treo**: `JobScanGiaVonTheoKyStuck` (background) quét job Running/PausedByError quá lâu → retry/khởi động lại.
- `IsKyBaoGomHienTai` computed lúc enqueue: `NgayBatDauKy ≤ now ≤ NgayKetThucKy` → mới có cập nhật `KhoChiTiet.GiaNhapTrungBinh` (nên kỳ quá khứ skip Phase 2+3).

## Ràng buộc nghiệp vụ (spec)

- **Cấm kỳ tương lai** — validate NgayBatDauKy/NgayKetThucKy không được ở tương lai.
- **1 job / đơn vị tại 1 thời điểm** — lock theo `IdDonVi` + `TrangThai IN (Queued, Running, PausedByError)` (xem doc-comment entity `:11`).
- **`IsTatCa* ⇔ Ids*Filter`** — 3 cặp scope (ChiNhanh / Kho / SanPham): khi `IsTatCa*=true` thì `Ids*Filter` bỏ qua, ngược lại filter theo Ids.
- **Feature flag** — check trong service `PreCheckAsync` (không mở mặc định).
- **Công thức**: `CongThuc` 1=AvgEligibleImport (V1), 2=HistoricalReplay (V2).
- **Ghi đè**: `IsGhiDe` cho phép ghi đè snapshot đã có.
- Index hỗ trợ: `IX_KhoChiTietGiaVonTheoKy_IdDonVi_KyNam_KyThang_IdKho`.

## Entities liên quan (MỚI)

- `GiaVonTheoKyJob` — `WarehouseProduct.DataContext/Entity/GiaVonTheoKyJob.cs` (verify `:13-110`): tham số kỳ (KyLoai/KyNam/KyThang/KyQuy, NgayBatDau/KetThucKy), CongThuc, IsGhiDe, scope 3 cặp IsTatCa*/Ids*Filter, TrangThai (1 Queued/2 Running/3 Completed/4 Failed/5 Cancelled/6 PausedByError), PhaseHienTai, IsProcessing (lock optimistic), SoLanRetry, progress counters, time-box (PhaseStartedAt/PhaseOffset/PhaseProgress), MessageLoi (NVARCHAR(2000)), NguonChay, IpChay (VARCHAR(50)), IsManualEntry.
- `KhoChiTietGiaVonTheoKy` — `.../Entity/KhoChiTietGiaVonTheoKy.cs`: snapshot giá vốn theo kỳ per kho/SP/tháng.
- `GiaVonTheoKyJobUpdateLog` — `.../CollectionMongoDB/GiaVonTheoKyJobUpdateLog.cs`: log Mongo lịch sử update.
- `EnumGiaVonTheoKy` / `PhaseGiaVonTheoKyJob` — `Libs/Models.Core/Enum/Warehouse/EnumGiaVonTheoKy.cs`.

## Ripple — đổi X gãy đâu

- Sửa thứ tự/điều kiện phase → phải sửa đồng bộ **cả 3 nguồn**: message contract (`RecalculateGiaVonTheoKyPhaseMessages.cs`), consumer (`PhaseConsumers.cs`), **và** doc-comment enum trong `GiaVonTheoKyJob.PhaseHienTai`.
- Phase 5/6 publish sang **Order / OrderLogistic** → thay đổi apply GiaVon ở đây ripple sang node OrderProduct/OrderLogistic (giá vốn đơn hàng → ảnh hưởng báo cáo lãi/lỗ).
- Phase 7 sang **Accounting** → ảnh hưởng số liệu kế toán giá vốn.
- Phase 3 re-index **ES** → nếu skip sai (logic `IsKyBaoGomHienTai`) → ES lệch giá vốn so DB.

## Known-issue / bẫy

1. 🔴 **Doc-drift thứ tự phase**: doc-comment `GiaVonTheoKyJob.PhaseHienTai` (`:51`) ghi thứ tự **1 ComputeSnapshot / 2 UpdateKhoChiTiet / 3 UpdatePhieuXuat / 4 UpdateDonHang / 5 UpdateDonHangLogistic / 6 SyncES / 7 SyncKeToan / 8 Done** — **KHÁC** thứ tự saga thực tế trong message/consumer (ES là **phase 3**, PhieuXuat là **phase 4**). Nguồn chuẩn = `RecalculateGiaVonTheoKyPhaseMessages.cs` + `PhaseConsumers.cs`. Đừng dựa vào enum comment để suy luận thứ tự chạy.
2. **XML summary controller ghi "9 endpoint"** nhưng có 12 — comment stale.
3. **1-job-per-đơn-vị** dựa trên lock TrangThai — nếu job kẹt ở PausedByError không được scan/hủy → khóa cứng đơn vị, không enqueue mới được (đã có `JobScanGiaVonTheoKyStuck` giảm rủi ro này, cần verify ngưỡng thời gian).

---

## Ghi chú các node liên quan (delta cùng module — cần cập nhật kho riêng)

- **LazadaConnectController** (MỚI) — `WarehouseProduct.API/Controllers/Ecommerce/LazadaConnectController.cs`. ⚠️ **Verify `:26-32`: `ConnectLazada` hiện là STUB** — trả hardcode `"true"`, lời gọi `_ecommerceConnectService.CRM_GetAuthorizationUrl` đang **comment out**. Quyền `LazadaConnectController_ConnectLazada` cho DV-AD-TONG/CN/MCN, DV-QL-MKT, DV-MKT-LEAD, DV-MKT; TienIch `TI_Lazada`. → Chưa phải tích hợp Lazada hoàn chỉnh; mô tả "Lazada ngang Shopee/Tiktok" là **định hướng**, connect thực tế chưa nối. Đừng ghi kho là đã xong.
- **NhanSanPhamController** (cập nhật) — bổ sung import Excel: `DownloadFileTemplateImportNhanSanPham` + `ImportNhanSanPham(IFormFile)` (verify `:111-129`, quyền `NhanSanPham_ImportNhanSanPham` cho DV-AD-TONG/CN/MCN); service `Generated/Imports/NhanSanPhamImportService.cs`. Kèm CRUD (UpdateMany/Xoa/ThemMoi/CapNhat/GetById).
- **PhieuNhap/PhieuXuat/PhieuChuyenKho** (cập nhật) — export Excel **theo dòng tích chọn** qua `ListId` + `@StrIdPhieu`.
- **WarehouseReportService** (cập nhật) — quy đổi ĐVT trong báo cáo nhập/xuất kho + export Excel; fix báo cáo sai số lượng chờ xuất.
- **SuaPhieuHoanThanhService** (cập nhật) — fix sửa phiếu nhập/xuất đã hoàn thành (Imei/LoDate/PhieuChuyenKho/XoaPhieu).

> **BỎ QUA cụm RaSoatTonKho** (đã fresh 13/7, không nằm trong delta doc này).
>
> **Watermark**: cập nhật WarehouseProduct → `e617abcbbf` (HEAD origin/release_product tại thời điểm quét; phân tích upstream đề nghị `5245133b` là mốc delta nội bộ — dùng `e617abcbbf` làm watermark git thực tế theo yêu cầu).
