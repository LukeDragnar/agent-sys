# Luồng — Nghiệp vụ tự động theo trạng thái giao hàng

> Đây là feature **nền** (Pass B): không có màn người dùng bấm tay. Luồng = hàng đợi `NghiepVuPending` → claim batch → handler xử lý → ghi `NghiepVuHistory`. Nghiệp vụ đang bật: **CHI_TIEU** (chi tiêu khách + công nợ Nợ/Trả).

## Tiền điều kiện
- Đơn hàng logistics có `KhachHangId` hợp lệ (không có khách → bỏ qua, không tạo nghiệp vụ).
- Đơn vị có **cấu hình nghiệp vụ** (`DonHangLogisticNghiepVuCauHinh`) bật cho nghiệp vụ CHI_TIEU. Nếu không có cấu hình nào → không tạo pending.
- `PhutDelay >= 0` (nếu = -1 → nghiệp vụ tắt cho đơn vị đó).
- Sự kiện kích hoạt khớp `EventRun` của cấu hình: `UPDATE_STATUS` (đổi trạng thái giao) hoặc `DOISOAT` (đối soát).
- Hạ tầng chạy: RabbitMQ (consumer `XuLyNghiepVuConsumer`) + Quartz job (`JobXuLyChiTieuKhachHang` quét queue, `JobXuLyNghiepVuLoi` retry/cleanup).

## Luồng dùng (tự động — không có user thao tác)
**A. Enqueue (khi đơn đổi trạng thái):**
1. Một điểm trong code đơn hàng cập nhật trạng thái giao vận của đơn (thủ công qua thủ kho, webhook đối tác vận chuyển, hoặc đồng bộ ecommerce) → gọi `TaoNghiepVuPendingInTransactionAsync(donHang)` **trong cùng transaction** với thao tác update đơn.
2. Service đọc danh sách cấu hình nghiệp vụ của đơn vị (từ cache). Với mỗi cấu hình khớp event:
   - Tính **hướng xử lý**: nếu trạng thái mới ∈ `TrangThaiTang` (mặc định 31;32) → **Tăng (huong=1)**; ngược lại → **Giảm (huong=-1)**.
   - Tính **SoTienTarget** (snapshot): Giảm → 0; trạng thái 35 (giao một phần) → COD một phần + cọc; còn lại → `DonHangTienCuoi`.
   - Snapshot thêm `SoTienNo` (= SoTienTarget), `ThoiGianTrangThaiDon` (MAX của ngày cập nhật thủ công & webhook).
   - **Hạ `IsMoiNhat`** của các pending cũ cùng `(IdDonHang, NghiepVu)` (store `HaIsMoiNhat`).
   - Insert 1 bản ghi `NghiepVuPending` mới (`ThoiGianXuLy = now + PhutDelay`).
   - **Publish `XuLyNghiepVuMessage{IdPending, NghiepVu}`** lên MQ.

**B. Xử lý (consumer + job):**
3. Consumer nhận message → gọi `XuLyMotNghiepVuPendingAsync` → **claim batch** qua store `ClaimBatch` (ưu tiên đúng IdDonHang trong message, fallback FIFO). Song song, `JobXuLyChiTieuKhachHang` chạy định kỳ cũng claim mọi batch đến hạn (`ThoiGianXuLy <= now`). Cả hai đi chung 1 đường store → idempotent.
4. **1 batch = tất cả pending cùng `(IdDonHang, NghiepVu)`**. Job sort batch theo `ThoiGianTrangThaiDon` ASC (đúng thứ tự đổi trạng thái thực tế) rồi gọi `handler.XuLyBatchAsync`.
5. Handler **CHI_TIEU**:
   - Đọc `netHienTai` của đơn từ `DonHangLogisticChiTieuLog` (Σ Huong=1 − Σ Huong=-1).
   - Loop từng pending: `deltaStep = SoTienTarget − runningNet`; `runningNet = SoTienTarget`. Bỏ qua pending đã có log (dedup khi retry) và pending legacy thiếu target.
   - `deltaTong = Σ step chưa log`. Gọi Customer **1 lần** (`CapNhatChiTieuLogistic`) với delta tổng + cờ `IsDonMoi` (net cũ=0, mới>0) / `IsHoanToanBo` (net cũ>0, mới=0) + `SoTienNo`.
   - **Customer OK → mới ghi log** từng step (thứ tự "gọi trước, log sau" để retry an toàn).
6. Mỗi pending xử lý xong → `MoveToHistory` (insert `NghiepVuHistory` trạng thái `ThanhCong=1`, hard-delete pending). Lỗi → `RetryOrMoveToHistory`.

## Luồng cấu hình (admin / hệ thống)
- Bảng `DonHangLogisticNghiepVuCauHinh` theo `(IdDonVi, NghiepVu)`: `EventRun` (UPDATE_STATUS|DOISOAT), `PhutDelay` (0=ngay, -1=tắt, mặc định 60), `TrangThaiTang` (mặc định "31;32", thêm 35 nếu muốn tính giao một phần là Tăng).
- Không có cấu hình riêng → dùng default từng nghiệp vụ. (Màn cấu hình UI: `cần product xác nhận` về vị trí/quyền chỉnh.)

## Các trường hợp / case biên
- **Đơn không có khách hàng** → `TaoNghiepVuPendingCore` return sớm, không tạo pending.
- **Đổi trạng thái ngược (giao → hoàn):** lần "giao" tạo pending Tăng (target=TienCuoi), lần "hoàn" tạo pending Giảm (target=0). Job batch xử lý tuần tự theo thời gian → delta cuối đưa net về 0 (`IsHoanToanBo=true`, xóa sạch Nợ/Trả).
- **Trùng webhook / 2 pending cùng nghiệp vụ:** `IsMoiNhat` đảm bảo chỉ bản mới nhất được bốc; bản cũ chưa xử lý bị hạ cờ. Batch gom cả cụm cùng đơn xử lý 1 lần.
- **Retry lỗi tạm thời:** handler throw → `RetryOrMoveToHistory`: `SoLanThuLai < 3` → reschedule `ThoiGianXuLy = now + 5 phút`, clear token; `>= 3` → `MoveToHistory` trạng thái `Loi(2)`.
- **Pending kẹt (stuck):** `IsProcessing=1` quá 5 phút → store `ResetStuck` clear token + tăng SoLanThuLai → re-claim ở tick sau. Chạy đầu mỗi job cycle.
- **History lỗi:** `JobXuLyNghiepVuLoi` quét `TrangThaiXuLy=Loi`, trong 24h, `SoLanThuLai < 6` → build pending ảo, chạy lại; OK → `FixDataThanhCong(5)`, fail → quay lại `Loi(2)` + ghi `GhiChuLoi`.
- **Customer OK nhưng ghi log fail:** vòng retry sau net=cũ → tính lại delta; rủi ro nhỏ là double chi tiêu (chấp nhận, xác suất thấp — đã ghi chú trong code).
- **Pending legacy thiếu `SoTienTarget`:** skip để tránh target=0 gây rollback nhầm.
- **Thử lại từ History đã ThanhCong:** bị chặn (chỉ Loi/BoQua mới tạo lại được).
- **Input rỗng / batch toàn legacy:** không gọi Customer, không chạm Nợ/Trả.
- **Cleanup định kỳ:** pending đã xong giữ 7 ngày, History giữ 60 ngày, ChiTieuLog giữ 120 ngày (xóa cứng qua store, top 1000/lượt).

## Ví dụ cụ thể có số
Đơn `MaDon = DH001`, `DonHangTienCuoi = 1.000.000đ`, khách K. Cấu hình mặc định: `TrangThaiTang=31;32`, `PhutDelay=60`.

1. **10:00 đơn → "Đã giao" (31):** tạo pending Tăng, target=1.000.000, `ThoiGianXuLy=11:00`. Net log ban đầu=0.
   - 11:00 job xử lý: deltaTong = 1.000.000 − 0 = **+1.000.000**, `IsDonMoi=true`. Chi tiêu khách K +1.000.000, ghi Nợ 1.000.000. History: ThanhCong.
2. **14:00 cùng đơn → "Đã hoàn" (41, không thuộc TrangThaiTang):** tạo pending Giảm, target=0, hạ IsMoiNhat pending cũ. 15:00 job xử lý:
   - net hiện tại=1.000.000 → delta = 0 − 1.000.000 = **−1.000.000**, `IsHoanToanBo=true`. Chi tiêu khách K trừ về 0, xóa Nợ/Trả. History: ThanhCong.
3. **Giao một phần (35):** đơn có `GiaoHangMotPhanCOD=600.000`, `DonHangDatCoc=100.000` → target = **700.000** (không phải 1.000.000).
4. **Customer service lỗi 1 lần:** handler throw → pending `SoLanThuLai=1`, dời `ThoiGianXuLy` +5 phút. Lần sau OK → ThanhCong. Nếu lỗi 3 lần → History trạng thái Loi → CSKH bấm "Thử lại" hoặc job Loi tự quét lại.

## Hậu điều kiện
- Chi tiêu khách + công nợ (Nợ/Trả) bên **Customer** được cập nhật khớp trạng thái giao mới nhất của đơn.
- Bản ghi xử lý xong rời `NghiepVuPending` (hard-delete) → ghi vào `NghiepVuHistory` (audit).
- `DonHangLogisticChiTieuLog` lưu từng bước cộng/trừ (audit trail, dedup theo `IdPending`).
- Không thao tác nào sửa đơn hàng gốc; chỉ đọc snapshot đã lưu trong pending.
