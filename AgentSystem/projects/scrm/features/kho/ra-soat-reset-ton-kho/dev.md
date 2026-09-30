# Dev — Rà soát / Reset tồn kho & giá vốn (CHỈ dev)

> Service: **WarehouseProduct** + **WarehouseProductConsumer** (job runner). MaTienIch: `MaTienIchSCRM.TI_Kho`.
> Node registry: [`registry/kho/RaSoatTonKhoController.md`](../../../registry/kho/RaSoatTonKhoController.md).
> ⚠️ Khác `ResetTonKhoImeiController` (reset 1 SP×1 kho về 0, đồng bộ). Đây là **rà soát/tái tạo tồn+giá vốn cả đơn vị**, chạy **nền + resumable**.

## Endpoints / quyền — `WarehouseProduct.API/Controllers/RaSoatTonKhoController.cs`
| Endpoint | HTTP | Quyền (SBAuthorize) | Method service |
|---|---|---|---|
| BatDau | POST `BatDau` (body `RaSoatTonKhoFilterDto`) | `RaSoatTonKho_BatDau` (AD-TONG/CN/MCN, QLDV, QL-KHO*, TP/TN-QL-KHO) | `BatDauRaSoatAsync` :24 |
| ResetToanKho | POST `ResetToanKho` | `RaSoatTonKho_ResetToanKho` (**chỉ DV-AD-TONG**) | `BatDauResetToanKhoAsync` :36 |
| TaiTaoToanBo | POST `TaiTaoToanBo` | `RaSoatTonKho_ResetToanKho` (**chỉ DV-AD-TONG**) | `BatDauTaiTaoPhieuAsync` :49 |
| TiepTuc | POST `TiepTuc/{idSession}` | `RaSoatTonKho_BatDau` | `TiepTucRaSoatAsync` :58 |
| PhienGanNhat | GET `PhienGanNhat` | `RaSoatTonKho_XemKetQua` | `PhienGanNhatAsync` :67 |
| KetQua | GET `KetQua?idSession&pageIndex&pageSize` | `RaSoatTonKho_XemKetQua` | `LayKetQuaAsync` :76 |
| TrangThai | GET `TrangThai?idSession` | `RaSoatTonKho_XemKetQua` | `LayTrangThaiAsync` :85 |
| XoaPhieuSai | POST `XoaPhieuSai` (body `XoaPhieuSaiRequest`) | `RaSoatTonKho_XoaPhieuSai` (AD-TONG/AD-MCN/QLDV) | `XoaPhieuSaiAsync` :94 |
| GetHuongReset | GET `HuongReset` | `RaSoatTonKho_XemKetQua` | `GetHuongResetConfigAsync` :103 |
| SetHuongReset | POST `HuongReset/{huong}` | `RaSoatTonKho_ResetToanKho` (**chỉ DV-AD-TONG**) | `SetHuongResetConfigAsync` :112 |

> ⚠️ `HuongReset` = **GET `HuongReset` + POST `HuongReset/{huong}`** (path khác nhau), KHÔNG phải GET/POST cùng `/HuongReset`.

## Loại phiên & hướng — `RaSoatTonKhoService.cs`
- `RaSoatTonKhoLoaiPhienEnum`: **1 RaSoatTheoDon · 2 ScanToanKho · 3 ResetToanKho · 4 TaiTaoPhieu**. BatDau ép RaSoatTheoDon trừ khi client gửi ScanToanKho (:265-267). Reset/TaiTao có method riêng (:278-281 / BatDauTaiTaoPhieuAsync).
- `RaSoatTonKhoHuongResetEnum`: 1 Anchor / 2 Stream. `LayHuongResetAsync()` **return cứng Anchor** (:221-228) — đoạn đọc `ConfigSystem.Ma=CONFIG_HUONG_RESET` bị comment. `SetHuongResetConfigAsync` upsert config (:234-258) nhưng **không có tác dụng** cho tới khi bỏ hardcode.
- Trạng thái: `RaSoatTonKhoTrangThaiEnum` (Pending/Running/Completed/Failed).
- Group SignalR: `GetGroupName(idDonVi, idChiNhanh, idSession)` = `RaSoatKho_{...}` (:211-212).

## Cơ chế job (`ChayRaSoatJobAsync`) — RaSoatTonKhoService.cs
- `JOB_HET_HAN_PHUT = 300` (:102) — stale guard + `session.NgayHetHan = now + 300'`.
- Redis lock 1 job/đơn vị: `LockTakeAsync(lockKey, lockId, 300')` (:431); nhả ở `finally` (:550).
- DB timeout 600s cho cutover/replay: `_sqlSugarClient.Ado.CommandTimeOut = 600` + `_dbContext.Database.SetCommandTimeout(600)` (:436-437).
- Dispatch theo LoaiPhien (:465-496): ScanToanKho → `ChayScanToanKhoAsync` (:1794); RaSoatTheoDon → `ChayRaSoatTheoDonAsync` (:582); ResetToanKho → `ChayResetToanKhoAsync`; TaiTaoPhieu → rebuild (Anchor).
- **Auto-retry** `MAX_AUTO_RETRY = 3` (:422): catch → nếu `soLanRetry < 3` giữ Running + `Task.Delay(3×(soLanRetry+1)s)` (:535) + re-publish `QueueRaSoatTonKho { SoLanRetry+1 }` (:536); resume idempotent từ checkpoint. Hết → `Failed` giữ checkpoint (:540-546).
- **TiepTuc** (:361-402): resume phiên dở, `NgayHetHan = now+300'` + re-publish — **KHÔNG auto-retry** (user chủ động).

## 13 stage rebuild (Anchor) — `Huong1_Anchor/RaSoatTonKhoService.TaiTaoPhieuAnchor.cs`
Checkpoint `session.CheckpointJson.Stage`; constant A_REPLAY/A_LICHSU/A_KETOAN (:25-27), tái dùng tên `TT_*` của hướng 2 cho bước chung (:24). Thứ tự (:61-179):
`TT_AM` → `TT_CHUYEN` → `TT_BUILD_OFF` → `TT_BUILD_CHOT` → `TT_BUILD_DANG` → `TT_BUILD_HOAN` → `TT_BUILD_HUY` → `TT_CUTOVER` → `TT_DELTA` → `TT_IMEI` → `A_REPLAY` → `TT_RESET` → `TT_LANGV` → `A_LICHSU` → `A_KETOAN` → `TT_SCAN`.
(Bước dựng phiếu dùng bảng tạm `PhieuXuatTaiTaoTmp`/`PhieuNhapTaiTaoTmp`; CUTOVER = temp-then-swap, không đụng bảng thật tới đó.)

## Replay giá vốn — `Huong2_Stream/RaSoatTonKhoService.ReplayGiaVon.cs`
- Bình quân theo timeline, keyset paging `(IdKho, IdSanPham)` → `avg[key] = (GiaTB, SL)`.
- **Clamp tồn âm về 0 CHỈ cho công thức bình quân** (:146-148): `slCuTinh = cur.SL <= EPS_SL ? 0 : cur.SL`, mẫu số `slCuTinh + SoLuong`, cộng `ChiPhiMuaHang` vào tử (:151). **Tồn LƯU `slMoi = cur.SL + SoLuong` vẫn có thể âm** (:152) — giữ để clamp lần sau + reset KhoChiTiet đúng.
- Giá nhập dùng (:136-144): cặp chuyển-đến (`LOAI_NHAP_DONGGOI` + `IdTransaction`) = giá vốn vế đi cùng txn (ghi đè GiaNhap); hoàn giao vận (`HoanGiaoVan`) = GiáTB hiện tại (ghi đè, không đổi GiáTB); mua/thủ công = `GiaNhap` gốc (KHÔNG ghi đè, tránh ăn mòn ÷TiLe).
- Xuất: GiáTB không đổi; xuất 11 (giữ chỗ) đã loại ở query (:165-168).

## Mark stale giá vốn theo kỳ — RaSoatTonKhoService.cs :554-575
Sau `ResetToanKho` hoặc `TaiTaoPhieu` (:501-502): raw SQL UPDATE `KhoChiTietGiaVonTheoKy SET TrangThaiCanhBao = CanTinhLaiDoResetTonKho, NgayDanhDauCanhBao, LyDoCanhBao WHERE IdDonVi=@ AND TrangThaiCanhBao=0 AND IsDelete=0` (:565-570) — **mark cả đơn vị**, không chỉ SP đụng. Enum `TrangThaiCanhBaoSnapshot.CanTinhLaiDoResetTonKho`.

## IMEI / Lô date — `RaSoatTonKhoService.TaiTaoImeiSerial.cs`, `.TaiTaoLoDate.cs`
Replay serial + re-point lô-date theo phiếu mới; audit `SanPhamImeiTaiTaoLog` / `SanPhamLoDateTaiTaoLog`. Serial không map được → đếm `soOrphan` (ops sửa tay).

## Consumer — `WarehouseProductConsumer.API/Consumer/RaSoatTonKho/RaSoatTonKhoConsumer.cs`
Consume `QueueRaSoatTonKho { IdSession, SoLanRetry }` → gọi `ChayRaSoatJobAsync`. Re-publish (auto-retry) đi qua đây; MassTransit `IPublishEndpoint`.

## Entities — `WarehouseProduct.DataContext/Entity/`
- `RaSoatTonKhoSession` — TrangThai, LoaiPhien, CheckpointJson, NgayHetHan, ThongBaoLoi.
- `RaSoatTonKhoKetQua` — kết quả từng dòng lệch.
- `PhieuXuatTaiTaoTmp` / `PhieuNhapTaiTaoTmp` — bảng tạm dựng phiếu (temp-then-swap).
- `RaSoatGiaVonStaging` — staging hướng Stream (đang disabled).
- Ripple: `KhoChiTiet`, `KhoChiTietGiaVonTheoKy`, `SanPhamImei`, `SanPhamLoDate`, `ConfigSystem`.

## Bug-prone
- **Stream disabled + hardcode Anchor** (:221-228): mọi cấu hình hướng reset bị bypass. Sửa: khôi phục đoạn đọc ConfigSystem, bỏ `return cứng`.
- **ResetToanKho ghi đè absolute** → nuốt giao dịch đồng thời nếu kho đang hoạt động. Chỉ chạy lúc rảnh.
- **Không clamp tồn âm** khi tính bình quân → méo giá vốn (đã clamp ở :148 — đừng bỏ).
- **Fail sau 3 retry** → giữ checkpoint; resume tay phải **verify/clean bảng temp** trước.
- **Orphan serial** không tự chặn — cần theo dõi `soOrphan`.
- **Mark stale quét cả đơn vị** (WHERE TrangThaiCanhBao=0) — sau reset kế toán phải tính lại giá vốn theo kỳ toàn đơn vị.

## File source (path:line) — verify @ BE 4c4f92260a
BE (`D:\PROJECT\Git\_SourceForBuild\scrm_build\WarehouseProduct\`):
- `WarehouseProduct.API/Controllers/RaSoatTonKhoController.cs` (TI_Kho :9; BatDau :22-28; ResetToanKho :34-40; TaiTaoToanBo :47-53; TiepTuc :56-62; PhienGanNhat :65-71; KetQua :74-80; TrangThai :82-89; XoaPhieuSai :91-98; GetHuongReset :100-107; SetHuongReset :109-116)
- `WarehouseProduct.Service/Generated/RaSoatTonKho/RaSoatTonKhoService.cs` (JOB_HET_HAN_PHUT :102; LayHuongResetAsync hardcode :221-228; CONFIG_HUONG_RESET :215; BatDau ép loại :265-267; ResetToanKho :278-281; MAX_AUTO_RETRY :422; Redis lock :431; CommandTimeout 600 :436-437; dispatch :465-496; retry+backoff :522-536; Failed :540-546; mark stale :554-575)
- `.../RaSoatTonKho/Huong1_Anchor/RaSoatTonKhoService.TaiTaoPhieuAnchor.cs` (stage const :24-27; pipeline :61-179)
- `.../RaSoatTonKho/Huong2_Stream/RaSoatTonKhoService.ReplayGiaVon.cs` (giá nhập phái sinh :136-144; clamp âm :146-148; tồn thật giữ âm :152; giá TB mới :151; xuất :165-168)
- `.../RaSoatTonKho/RaSoatTonKhoService.TaiTaoImeiSerial.cs`, `.TaiTaoLoDate.cs`
- `WarehouseProductConsumer.API/Consumer/RaSoatTonKho/RaSoatTonKhoConsumer.cs`
- `WarehouseProduct.DataContext/Entity/RaSoatTonKho*.cs`
