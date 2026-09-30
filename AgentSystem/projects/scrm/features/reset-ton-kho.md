# Reset / Rà soát tồn kho (`RaSoatTonKho`)

> Module: **WarehouseProduct**. Kiểm tra & sửa sai lệch tồn kho, và tái tạo tồn + giá vốn của **toàn đơn vị từ đầu**.
> Engine giá vốn ở đây (`ReplayGiaVon`) là **bản tham chiếu chuẩn** cho bình quân gia quyền của hệ thống — xem [gia-von-theo-ky.md](./gia-von-theo-ky.md) đang hội tụ về cùng công thức.

---

## 1. Tính năng là gì

Rà soát tồn kho = công cụ admin để phát hiện & sửa sai lệch số liệu tồn kho của 1 đơn vị, và (ở mức mạnh nhất) **tái tạo lại toàn bộ phiếu + tồn + giá vốn từ trạng thái đơn hàng**.

**4 loại phiên** (`RaSoatTonKhoSession.LoaiPhien`):

| Loại | Tên | Việc |
|---|---|---|
| 1 | RaSoatTheoDon | Quét sai lệch theo đơn — luật A–D (drill-down theo scope kho/SP) |
| 2 | ScanToanKho | Quét toàn kho, đối chiếu `KhoChiTiet` vs tồn-theo-phiếu — luật E |
| 3 | ResetToanKho | Ghi đè `KhoChiTiet.SoLuong/SoLuongChoXuat` theo tồn derive từ phiếu (luật F) |
| 4 | **TaiTaoPhieu** | **Tái tạo phiếu từ 0 + reset tồn + replay giá vốn** — phần liên quan giá vốn |

**Luật phát hiện sai lệch (loại 1/2):**
- **A** — trùng phiếu (cùng đơn + cùng loại ≥2 active)
- **B** — lệch SL (tổng phiếu vs SL đơn)
- **C** — mồ côi kho/chi nhánh (phiếu ở kho khác đơn)
- **D** — active sai trạng thái (phiếu active không khớp state đơn)
- **E** — lệch tồn (KhoChiTiet vs tồn-theo-phiếu)
- **F** — audit thay đổi khi reset
- **G** — đơn chốt thiếu phiếu chờ xuất (chỉ full-scan, bỏ qua nếu có filter SP)

---

## 2. Backend — file layout

```
WarehouseProduct.Service/Generated/RaSoatTonKho/
  RaSoatTonKhoService.cs                 # orchestration chính (quét, reset loại 1-3)
  RaSoatTonKhoService.TaiTaoLoDate.cs    # replay lô-date
  RaSoatTonKhoService.TaiTaoImeiSerial.cs# replay IMEI/serial
  Huong1_Anchor/
    RaSoatTonKhoService.TaiTaoPhieuAnchor.cs  # pipeline tái tạo ANCHOR (đang dùng)
  Huong2_Stream/
    RaSoatTonKhoService.TaiTaoPhieu.cs        # pipeline tái tạo STREAM (tạm tắt) + cutover
    RaSoatTonKhoService.ReplayGiaVon.cs       # engine bình quân gia quyền (stream)

WarehouseProductConsumer.API/Consumer/RaSoatTonKho/RaSoatTonKhoConsumer.cs  # message handler
WarehouseProduct.DataContext/Entity/RaSoatTonKhoSession.cs                  # entity session
WarehouseProduct.DataContext/Entity/RaSoatTonKhoKetQua.cs                   # kết quả sai lệch
Libs/Models.Core/Enum/Warehouse/RaSoatTonKhoEnum.cs                         # enum loại/trạng thái/luật
```

---

## 3. Luồng end-to-end

### Loại 1/2/3 (rà soát / reset thường)
1. **BatDauRaSoatAsync** — user bấm → guard 1-job-per-đơn-vị (Redis lock + stale check) → tạo `RaSoatTonKhoSession` (Pending) + checkpoint → publish sang consumer.
2. **ChayRaSoatJobAsync** — consumer nhận → Running → branch theo `LoaiPhien`.
3. Quét **keyset paging** theo Id (PAGE_PHIEU=100), 3 stage tuần tự **XUAT → NHAP → LUATG**, update checkpoint sau mỗi trang → resume được nếu fail.
4. Loại 3: tính expected (tồn theo phiếu) vs actual (KhoChiTiet) → ghi đè SL (clamp ≥ 0) + audit (luật F); KhoChiTiet thiếu thì tạo mới.

### Loại 4 (tái tạo phiếu) — pipeline 12 stage, checkpoint resume
`ChayTaiTaoPhieuAnchorAsync`:

| Stage | Việc |
|---|---|
| TT_AM | Chuẩn hóa SL âm phiếu xuất thủ công |
| TT_CHUYEN | Backfill cặp phiếu chuyển kho → TEMP |
| TT_BUILD_OFF | Build phiếu xuất 10 (offline, ngày thanh toán < T_bấm) → TEMP |
| TT_BUILD_CHOT | Build phiếu xuất 11 (online chốt) → TEMP |
| TT_BUILD_DANG | Build phiếu xuất 12 (online đăng) → TEMP |
| TT_BUILD_HOAN | Build nhập 6 (hoàn, ngày đối soát < T_bấm) → TEMP |
| **TT_CUTOVER** | **Xóa phiếu auto cũ < T_bấm + insert TEMP vào bảng chính** |
| TT_DELTA | Bù phiếu THIẾU cho đơn phát sinh trong `[T_bấm, T_xong]` (job không khóa đơn vị) |
| TT_IMEI | Replay serial IMEI + lô-date |
| **A_REPLAY** | **Replay giá vốn** → `Dictionary<(Kho,SP), GiáTB cuối>` |
| TT_RESET | Reset `KhoChiTiet` (SL + chờ xuất + `GiaNhapTrungBinh`) |
| TT_LANGV / TT_SCAN | Lan giá vốn xuống đơn + dọn TEMP + scan E verify |

---

## 4. Cutover (loại 4)

`CutOverStoreSync` ([Huong2_Stream/RaSoatTonKhoService.TaiTaoPhieu.cs](D:/WORK/SCRM/GIT/BE/scrm/WarehouseProduct/WarehouseProduct.Service/Generated/RaSoatTonKho/Huong2_Stream/RaSoatTonKhoService.TaiTaoPhieu.cs)):
- **T_bấm** = `NgayBatDau` (lúc user bấm); **T_xong** = `NgayXongTemp` (build xong TEMP).
- Phạm vi: phiếu `NgayTacDong < T_bấm`.
- Bước: (1) xóa phiếu auto cũ có marker `RaSoat_TaiTaoPhieu` (xuất 10/11/12, nhập 4/5/6) — **KHÔNG xóa phiếu thủ công**; (2) insert TEMP → bảng chính (PhieuXuat/SP + PhieuNhap/SP); (3) lưu `NgayXongTemp`.
- **Delta**: đơn phát sinh trong `[T_bấm, T_xong]` → bù phiếu THIẾU vào bảng chính (idempotent, không xóa).

---

## 5. Engine giá vốn — `ReplayGiaVon` / Anchor ⭐

**Bình quân gia quyền LIÊN HOÀN** (moving average) — stream TOÀN BỘ movement (nhập+xuất active, hoàn thành) theo thứ tự `(NgayTacDong, NgayTacDongChiTiet, NgayTao, Id)`, giữ running `(GiáTB, SL)` per `(Kho, SP)`:

- **Nhập**: `GiáTB = (GiáTB×SL + GiaNhap×SLn + ChiPhíMuaHàng) / (SL + SLn)`; tồn âm **clamp về 0** khi tính bình quân (cả tử & mẫu) nhưng tồn lưu vẫn là tồn thật.
- **Xuất**: giá vốn dòng = GiáTB hiện tại; SL giảm; **GiáTB KHÔNG đổi**.
- **Xuất phân loại 11**: bỏ qua (không tác động SL/giá vốn).
- **Hoàn giao vận (nhập 6)**: re-nhập ở **GiáTB hiện tại** → hàng trả về theo giá vốn lúc đi → không bóp méo bình quân.
- **Chuyển kho** (cặp gắn `IdTransaction`): vế ĐI (xuất đóng gói) ghi giá vốn = GiáTB kho đi; vế ĐẾN (nhập đóng gói) lấy đúng giá vốn vế đi → phải replay **toàn cục theo thời gian**, không tách per kho.
- **Loại trừ**: `IsDaHuyTrongDonHang = 1`, xuất phân loại 11.

**I/O**: input = session (bảng chính đã hoàn chỉnh sau cutover+delta); output = `Dictionary<(Kho,SP), double>` GiáTB cuối → dùng reset `KhoChiTiet.GiaNhapTrungBinh`. Đồng thời ghi `GiaVon/SoLuongTruoc/SoLuongSau` vào từng dòng phiếu (staging → UPDATE JOIN).

**2 hướng** (config `RaSoatTonKho_HuongReset`, chốt vào `session.HuongReset` lúc bắt đầu):

| | Anchor (Hướng 1) | Stream (Hướng 2) |
|---|---|---|
| Tính | C# thuần, keyset bảng chính | SQL replay streaming |
| Maintain | Dễ | Khó |
| Hiệu năng | Chậm hơn | Nhanh hơn |
| Trạng thái | **Đang dùng** | Tạm tắt (ẩn FE+BE) |

---

## 6. Entity `RaSoatTonKhoSession` — field chính

| Field | Mục đích |
|---|---|
| `LoaiPhien` | 1=RaSoatTheoDon, 2=ScanToanKho, 3=ResetToanKho, 4=TaiTaoPhieu |
| `HuongReset` | (loại 4) 1=Anchor, 2=Stream — chốt lúc bắt đầu, resume theo đó |
| `TuNgay/DenNgay` | Khoảng ngày quét (bound `NgayTacDong`); NULL = quét all |
| `FilterJson` | scope (idKhos, idChiNhanhs, idSanPhams...) |
| `TrangThai` | Pending(0), Running(1), Completed(2), Failed(3), Canceled(4) |
| `TongDon/DaXuLy/SoDonNghi` | progress + số phát hiện sai |
| `CheckpointJson` | `{ Stage, LastId, LastNgayTao, GSkip, DaTinhTongPhieu }` — resume |
| `NgayBatDau` | T_bấm |
| `NgayHetHan` | mốc stale (~300') — guard khóa vĩnh viễn |
| `NgayXongTemp` | (loại 4) T_xong — mốc cutover/delta |
| `FinalAvgJson` | (loại 4) giá vốn TB cuối từ replay — dùng stage RESET |

---

## 7. Phạm vi

- **Loại 1/2/3**: filter được (TuNgay/DenNgay + kho/chi nhánh/SP). Bỏ trống → quét toàn lịch sử (vẫn paging + checkpoint).
- **Loại 4**: **LUÔN toàn đơn vị** (build phiếu từ 0), filter bỏ qua. Không giới hạn kỳ.

---

## 8. So sánh với "Giá vốn theo kỳ"

| | Reset tồn kho (file này) | Giá vốn theo kỳ (V1) |
|---|---|---|
| Công thức | BQ gia quyền **liên hoàn** (replay) | TB gộp **chỉ nhập trong kỳ** |
| Tồn đầu kỳ | Mang theo running state | ❌ Không |
| Xuất/hoàn/chuyển kho | Xử lý đầy đủ | ❌ Bỏ qua |
| Phạm vi | Toàn đơn vị, từ đầu | 1 kỳ + scope |

→ Hướng phát triển: chuyển giá vốn theo kỳ sang **replay có seed tồn đầu kỳ** (tái dùng engine này, giới hạn theo kỳ). Chi tiết & các điểm cần thống nhất (cờ loại trừ phiếu hoàn, bootstrap opening cost): xem mục so sánh trong [gia-von-theo-ky.md](./gia-von-theo-ky.md).

---

## 9. Liên quan

- [warehouse.md](./warehouse.md) — context WarehouseProduct module.
- [gia-von-theo-ky.md](./gia-von-theo-ky.md) — tính lại giá vốn theo kỳ (đang hội tụ về engine replay này).
- [KhoChiTietLichSu](D:/WORK/SCRM/GIT/BE/scrm/WarehouseProduct/WarehouseProduct.DataContext/Entity/KhoChiTietLichSu.cs) — sổ cái tồn đầy đủ (nguồn lấy tồn đầu kỳ cho replay theo kỳ).

---

## Changelog

| Ngày | Người | Thay đổi |
|------|-------|----------|
| 2026-06-20 | teamdev1 | Khởi tạo doc — 4 loại phiên, pipeline tái tạo 12 stage, cutover/delta, engine ReplayGiaVon (Anchor/Stream), entity session, so sánh với giá vốn theo kỳ |
| 2026-06-22 | teamdev1 | Sau khi hoàn thành reset (loại 3 ResetToanKho / loại 4 TaiTaoPhieu) → đánh dấu MỌI snapshot giá vốn theo kỳ của đơn vị "cần tính lại" (`MarkSnapshotsGiaVonStaleSauResetAsync`, UPDATE thẳng SQL, `TrangThaiCanhBao=CanTinhLaiDoResetTonKho`). Xem [gia-von-theo-ky.md §8.9](./gia-von-theo-ky.md) |
| 2026-06-22 | teamdev1 | **Fix: reset KhoChiTiet chưa đồng bộ ES.** `ChayResetToanKhoAsync` update/insert `KhoChiTiet` qua raw SQL nhưng KHÔNG re-index ES (luồng thường gọi `ESIndexKhoChiTiet`). Đã inject `ICommonElasticSearchService`, gom Id KhoChiTiet đã đổi → publish `ESIndexKhoChiTiet` theo batch 100 sau reset (cả loại 3 + 4). Build 0 errors |
