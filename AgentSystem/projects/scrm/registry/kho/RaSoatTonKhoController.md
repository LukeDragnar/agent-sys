---
controller: RaSoatTonKhoController
module: kho
service: WarehouseProduct
man_hinh_ui: "Quản lý kho > công cụ 'Rà soát / Reset tồn kho & giá vốn' (màn chạy job nền, join group SignalR nhận tiến trình). Nút Reset/Tái tạo NGUY HIỂM — chỉ Admin tổng; nên chạy lúc kho ít hoạt động."
muc_dich: "Rà soát & tái tạo tồn kho + giá vốn của cả đơn vị: dò lệch theo đơn/scan toàn kho, hoặc reset/dựng lại ledger phiếu rồi replay giá vốn. Job chạy nền ở WarehouseProductConsumer, resumable theo checkpoint."
alias: [rà soát tồn kho, ra soat ton kho, reset toàn kho, reset tồn kho theo phiếu, tái tạo phiếu, tai tao phieu, tính lại giá vốn, replay giá vốn, RaSoatTonKho, đối soát tồn kho]
auth: public
state_machine: [Pending, Running, Completed, Failed]
deprecated: false
entities: [RaSoatTonKhoSession, RaSoatTonKhoKetQua, PhieuXuatTaiTaoTmp, PhieuNhapTaiTaoTmp, RaSoatGiaVonStaging, KhoChiTiet, KhoChiTietGiaVonTheoKy, SanPhamImei, SanPhamLoDate, ConfigSystem]
stored_procs: []
depends: [WarehouseProductConsumer (job runner), RedisCacheService (lock 1 job/đơn vị), SuaPhieuHoanThanhService, CommonService (SinhMaPhieu + SyncKhoChiTietLichSu), DongBoPhieuSangKeToanService, CommonElasticSearchService, SqlSugar (_dapperDB keyset replay)]
cross_module_in: []
cross_module_out: [Accounting (DongBoPhieuSangKeToanService — đẩy lại chứng từ phiếu dựng/reset nếu đơn vị bật kế toán), KhoChiTietLichSu (SyncKhoChiTietLichSu → báo cáo kho), KhoChiTietGiaVonTheoKy (mark stale TrangThaiCanhBao=CanTinhLaiDoResetTonKho → cả đơn vị cần tính lại giá vốn theo kỳ), SanPhamImeiTaiTaoLog / SanPhamLoDateTaiTaoLog (audit re-point serial/lô)]
---

# RaSoatTonKhoController — Rà soát / Reset / Tái tạo tồn kho & giá vốn

> Công cụ **tái tạo tồn kho + giá vốn cả đơn vị** (khác `ResetTonKhoImeiController` chỉ reset 1 SP×1 kho về 0). Người dùng bấm **Bắt đầu** → BE tạo 1 `RaSoatTonKhoSession` + publish message → **job chạy nền ở WarehouseProductConsumer**, FE join group SignalR `RaSoatKho_{IdDonVi}_{IdChiNhanh}_{IdSession}` nhận tiến trình. Job **resumable theo checkpoint**: crash/timeout → **auto-retry tối đa 3 lần** (backoff `3×(retry+1)`s), hết budget → `Failed` giữ checkpoint để user bấm **Tiếp tục** thủ công. Toàn bộ khóa bằng **Redis lock 1 job/đơn vị**; job coi là *stale* nếu Running quá **300 phút** (chống khóa vĩnh viễn). **4 loại phiên** (RaSoatTheoDon / ScanToanKho / ResetToanKho / TaiTaoPhieu); **2 hướng tái tạo giá vốn** (Anchor / Stream) — hiện **ép cứng Anchor**, Stream tạm tắt.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Bắt đầu 1 phiên rà soát | "Bắt đầu rà soát" | `POST api/RaSoatTonKho/BatDau` body `RaSoatTonKhoFilterDto` | `RaSoatTonKho_BatDau` | public (`SBAuthorize`: AD-TONG/CN/MCN, QLDV, QL-KHO, QL-KHO-TONG, TP/TN-QL-KHO) | luôn hiện với quyền | Tạo `RaSoatTonKhoSession` (`NgayHetHan=now+300'`) + publish `QueueRaSoatTonKho` → job nền. `filter.LoaiPhien==ScanToanKho` → giữ ScanToanKho, còn lại ép **RaSoatTheoDon**. Trả `{IdSession, GroupName}` để FE join SignalR. | EF + publish MassTransit | — |
| 2 | Reset TOÀN KHO đơn vị | "Reset toàn bộ tồn kho theo phiếu" (danger) | `POST api/RaSoatTonKho/ResetToanKho` (no body) | `RaSoatTonKho_ResetToanKho` | public (**chỉ DV-AD-TONG**) | chỉ admin tổng | Tạo phiên `LoaiPhien=ResetToanKho` → job **ghi đè** `KhoChiTiet.SoLuong/SoLuongChoXuat` = tồn tính lại từ **TOÀN BỘ phiếu active**. Xong → mark stale giá vốn theo kỳ cả đơn vị. | EF | Ghi đè **absolute** → **nuốt thay đổi đồng thời** nếu kho đang hoạt động (chạy lúc rảnh). |
| 3 | Tái tạo phiếu + reset (loại 4) | "Tái tạo toàn bộ" (danger) | `POST api/RaSoatTonKho/TaiTaoToanBo` (no body) | `RaSoatTonKho_ResetToanKho` | public (**chỉ DV-AD-TONG**) | chỉ admin tổng | Phiên `LoaiPhien=TaiTaoPhieu`: làm sạch ledger phiếu từ trạng thái đơn → dựng lại phiếu → reset KhoChiTiet → replay giá vốn (Anchor) → scan E verify. **13 stage** checkpoint (xem dưới). Xong → mark stale giá vốn theo kỳ. | EF (temp-then-swap) | Nặng, nhiều stage; fail sau 3 retry → giữ checkpoint, resume tay. |
| 4 | Tiếp tục phiên dở | "Tiếp tục" | `POST api/RaSoatTonKho/TiepTuc/{idSession}` | `RaSoatTonKho_BatDau` | public (như #1) | phiên `Failed`/dở | Resume từ checkpoint (`session.NgayHetHan=now+300'` + re-publish). **KHÔNG auto-retry** — user chủ động vào lại màn bấm/auto TiepTuc. | EF + publish | — |
| 5 | Phiên gần nhất | (auto-load khi mở màn) | `GET api/RaSoatTonKho/PhienGanNhat` | `RaSoatTonKho_XemKetQua` | public (AD/QLDV/QL-KHO...) | mở màn | Trả phiên gần nhất của đơn vị → FE resume phiên dở / load kết quả. | EF | — |
| 6 | Kết quả phiên (paging) | bảng kết quả | `GET api/RaSoatTonKho/KetQua?idSession&pageIndex&pageSize` | `RaSoatTonKho_XemKetQua` | public | phiên có kết quả | Trả `RaSoatTonKhoKetQua` phân trang (mặc định pageSize=50). | EF | — |
| 7 | Trạng thái/tiến trình | progress bar (fallback SignalR) | `GET api/RaSoatTonKho/TrangThai?idSession` | `RaSoatTonKho_XemKetQua` | public | khi mất SignalR | Trả trạng thái/tiến trình phiên (poll fallback). | EF | — |
| 8 | Xóa phiếu sai | "Xóa phiếu sai" | `POST api/RaSoatTonKho/XoaPhieuSai` body `XoaPhieuSaiRequest` | `RaSoatTonKho_XoaPhieuSai` | public (**AD-TONG/AD-MCN/QLDV**) | có phiếu bị đánh sai | Re-validate ở BE từng phiếu → xóa + hoàn kho; trả kết quả từng phiếu. | EF | — |
| 9 | Lấy hướng reset hiện tại | radio "1 Anchor / 2 Stream" | `GET api/RaSoatTonKho/HuongReset` | `RaSoatTonKho_XemKetQua` | public | mở màn cấu hình | Trả `int` (1 Anchor / 2 Stream). **HIỆN LUÔN trả 1 (Anchor) do ép cứng.** | EF (ConfigSystem) | Giá trị đọc từ config **bị bypass** — hàm `LayHuongResetAsync` return cứng Anchor. |
| 10 | Set hướng reset (super) | radio (super user) | `POST api/RaSoatTonKho/HuongReset/{huong}` | `RaSoatTonKho_ResetToanKho` | public (**chỉ DV-AD-TONG**) | super user | Validate `huong∈{1,2}` → upsert `ConfigSystem.Ma=RaSoatTonKho_HuongReset`. | EF | Set được nhưng **KHÔNG có tác dụng** (đọc bị ép Anchor) cho tới khi bỏ hardcode. |

## Quy tắc nghiệp vụ (gating / tier / ngưỡng)

**4 loại phiên (`RaSoatTonKhoLoaiPhienEnum`):**
1. **RaSoatTheoDon** — dò lệch theo luật A–D drill theo kho+SP (BatDau mặc định).
2. **ScanToanKho** — E→A–D→G quét toàn kho.
3. **ResetToanKho** — ghi đè tồn = tính lại từ toàn bộ phiếu active (ResetToanKho).
4. **TaiTaoPhieu** — rebuild ledger phiếu + reset + replay (TaiTaoToanBo, loại 4).

**2 hướng tái tạo giá vốn:**
- **Hướng 1 Anchor** (C# thuần, bình quân theo timeline, tồn-nhập-trước→giá tích lũy) — **hiện ĐANG ÉP CỨNG**.
- **Hướng 2 Stream** (SQL keyset replay qua `_dapperDB`, staging `RaSoatGiaVonStaging`) — **tạm TẮT ở BE + ẨN ở FE**. `LayHuongResetAsync()` return cứng Anchor, đoạn đọc `ConfigSystem` bị comment (RaSoatTonKhoService.cs:221-228).

**13 stage rebuild (Anchor, checkpoint `session.CheckpointJson.Stage`):**
`TT_AM` (chuẩn hóa SL âm→dương) → `TT_CHUYEN` (backfill cặp phiếu chuyển) → `TT_BUILD_OFF`/`TT_BUILD_CHOT`/`TT_BUILD_DANG`/`TT_BUILD_HOAN`/`TT_BUILD_HUY` (dựng phiếu offline/chốt/đang/hoàn 6/hủy vào TEMP) → `TT_CUTOVER` (xóa phiếu auto < T_bấm + INSERT temp→bảng chính) → `TT_DELTA` (bù phiếu phát sinh trong [T_bấm, T_xong]) → `TT_IMEI` (replay serial + re-point lô-date) → `A_REPLAY` (replay giá vốn) → `TT_RESET`/`TT_LANGV` → `A_LICHSU` (resync KhoChiTietLichSu) → `A_KETOAN` (đẩy lại kế toán) → `TT_SCAN` (scan E verify ~0 lệch).

**Cơ chế job (bất biến — blueprint trace):**
- Redis lock 1 job/đơn vị: `LockTakeAsync(lockKey, lockId, 300')`; nhả ở `finally`.
- Job *stale* nếu Running quá `JOB_HET_HAN_PHUT=300`.
- DB timeout nâng **600s** cho cutover/replay (`Ado.CommandTimeOut=600` + `SetCommandTimeout(600)`).
- Auto-retry `MAX_AUTO_RETRY=3`; giữ trạng thái Running (UI "đang thử lại") + re-publish `SoLanRetry+1`; backoff `Task.Delay(3×(soLanRetry+1)s)`. Resume idempotent từ checkpoint.
- Hết retry → `Failed`, giữ checkpoint → user bấm **Tiếp tục**.

## Quan hệ
- **Gọi sang:** `WarehouseProductConsumer` (`RaSoatTonKhoConsumer` chạy job qua `QueueRaSoatTonKho`); `RedisCacheService.LockTakeAsync/LockRelease`; `SuaPhieuHoanThanhService`, `CommonService.SinhMaPhieu`/`SyncKhoChiTietLichSu`; `DongBoPhieuSangKeToanService`; `CommonElasticSearchService`; SqlSugar `_dapperDB` (Stream keyset replay).
- **Dữ liệu (entities):** `RaSoatTonKhoSession` (trạng thái + checkpoint + NgayHetHan), `RaSoatTonKhoKetQua`; ghi/ swap `PhieuXuatTaiTaoTmp`/`PhieuNhapTaiTaoTmp`; `RaSoatGiaVonStaging` (Stream); update `KhoChiTiet`, `SanPhamImei`, `SanPhamLoDate`; mark `KhoChiTietGiaVonTheoKy`; upsert `ConfigSystem` (hướng reset).
- **Ripple / đồng bộ:** phiếu dựng/reset → **Accounting** (nếu bật kế toán) + **KhoChiTietLichSu** (báo cáo kho); sau reset/rebuild → `KhoChiTietGiaVonTheoKy.TrangThaiCanhBao = CanTinhLaiDoResetTonKho` (cả đơn vị cần **tính lại giá vốn theo kỳ**); audit re-point serial/lô qua `SanPhamImeiTaiTaoLog`/`SanPhamLoDateTaiTaoLog`.

## Luồng xuyên module
- **Reset/Tái tạo tồn kho:** admin tổng bấm Reset/Tái tạo → tạo `RaSoatTonKhoSession` + publish → job nền: (với TaiTaoPhieu) dựng lại phiếu vào TEMP → **cutover** (temp-then-swap, không đụng bảng thật tới lúc này) → reset `KhoChiTiet` → replay giá vốn (Anchor) → replay IMEI/lô → resync `KhoChiTietLichSu` (báo cáo kho) → đẩy lại **Kế toán** → scan verify. Sau xong → **mark stale** giá vốn theo kỳ (TrangThaiCanhBao) → màn "Giá vốn theo kỳ" cả đơn vị hiện cảnh báo cần tính lại. FE nhận tiến trình realtime qua SignalR group.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `WarehouseProduct/WarehouseProduct.API/Controllers/RaSoatTonKhoController.cs`
  - L9 `TienIchAttribute("RaSoatTonKhoController", "Rà soát số liệu tồn kho", TI_Kho)`.
  - Endpoint `HuongReset`: GET `HuongReset` (L102) + POST `HuongReset/{huong}` (L111) — **KHÔNG phải** GET/POST cùng path `/HuongReset`.
- BE `WarehouseProduct/WarehouseProduct.Service/Generated/RaSoatTonKho/RaSoatTonKhoService.cs`
  - L221-228 `LayHuongResetAsync()` **return cứng Anchor**, đoạn đọc `ConfigSystem` bị comment → hướng Stream **disabled**; `Get/SetHuongReset` config không có tác dụng cho tới khi bỏ hardcode.
  - L102 `JOB_HET_HAN_PHUT=300` (stale guard); L431 Redis lock; L436-437 CommandTimeout 600; L422 `MAX_AUTO_RETRY=3`; L535 backoff `3×(retry+1)`s; L536 re-publish `SoLanRetry+1`; L540-546 hết retry → Failed giữ checkpoint.
  - L501-570 sau ResetToanKho/TaiTaoPhieu → UPDATE `KhoChiTietGiaVonTheoKy SET TrangThaiCanhBao=CanTinhLaiDoResetTonKho` cho `IdDonVi` (WHERE TrangThaiCanhBao=0) — **mark cả đơn vị stale**, không chỉ SP đụng.
  - `ResetToanKho` ghi đè **absolute** tồn từ phiếu active → **nuốt thay đổi đồng thời** nếu kho đang giao dịch.
- BE `.../Huong2_Stream/RaSoatTonKhoService.ReplayGiaVon.cs`
  - L146-148 **clamp SL âm→0 CHỈ cho công thức bình quân** (tử + mẫu); L152 tồn LƯU (`slMoi`) **vẫn có thể âm** (giữ để clamp lần sau + reset KhoChiTiet đúng). Không clamp → tồn âm kéo méo giá vốn.
  - Giá nhập phái sinh: cặp chuyển-đến = giá vốn vế đi cùng `IdTransaction` (L138-140); hoàn giao vận 6 = GiáTB hiện tại, không đổi GiáTB (L141-142); mua/thủ công = GiaNhap gốc, KHÔNG ghi đè (L143-144). Xuất 11 (giữ chỗ) đã loại ở query.
- **Orphan serial:** nếu replay IMEI không map được đơn/phiếu mới → đếm `soOrphan`, ops sửa tay (không tự chặn).
- **Tái tạo là thao tác nặng + phá ledger** — chỉ admin tổng, chạy lúc kho ít hoạt động; fail giữa chừng phải verify/clean temp trước khi resume.
