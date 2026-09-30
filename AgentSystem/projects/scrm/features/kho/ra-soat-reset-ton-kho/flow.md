# Luồng — Rà soát / Reset tồn kho & giá vốn

> Mô tả luồng dùng + cấu hình cho người vận hành / QA / đào tạo. Số liệu ví dụ là giả lập.

## Tiền điều kiện
- Đã có **chi nhánh / kho / sản phẩm** và ledger phiếu (nhập/xuất/chuyển/kiểm kho) của đơn vị.
- Người dùng có quyền: Bắt đầu/Xem — Admin tổng/CN/MCN, QLDV, Quản kho các cấp; **Reset & Tái tạo toàn kho — chỉ Admin tổng**; Xóa phiếu sai — Admin tổng/MCN/QLDV.
- Nên chạy Reset/Tái tạo **lúc kho ít hoạt động** (tránh ghi đè nuốt giao dịch đồng thời).

---

## Luồng dùng — Rà soát dò lệch (theo đơn / quét toàn kho)
1. Vào công cụ **Rà soát tồn kho**.
2. Chọn loại phiên: **Rà soát theo đơn** (mặc định) hoặc **Quét toàn kho**.
3. Bấm **Bắt đầu** → hệ thống tạo phiên, chạy job nền, FE join group SignalR nhận **tiến trình realtime**.
4. Xem **Kết quả** (phân trang): dòng SP×kho nào lệch, phiếu nào sai.
5. (Tùy chọn) chọn các **phiếu sai → Xóa phiếu sai** → hệ thống re-validate + hoàn kho, trả kết quả từng phiếu.

## Luồng dùng — Reset toàn kho (admin tổng)
1. Admin tổng bấm **Reset toàn kho** (xác nhận danger).
2. Job **ghi đè** `KhoChiTiet.SoLuong/SoLuongChoXuat` = tồn tính lại từ **toàn bộ phiếu active**.
3. Xong → **giá vốn theo kỳ cả đơn vị được đánh dấu "cần tính lại"**.

## Luồng dùng — Tái tạo phiếu + reset (loại 4, admin tổng)
1. Admin tổng bấm **Tái tạo toàn bộ** (xác nhận danger).
2. Job chạy 13 bước (checkpoint): chuẩn hóa SL âm → backfill phiếu chuyển → dựng phiếu (offline/chốt/đang/hoàn/hủy) vào **bảng tạm** → **cutover** (xóa phiếu auto cũ + đổ tạm → bảng thật) → bù phiếu phát sinh → replay IMEI/lô → **replay giá vốn (Anchor)** → resync lịch sử kho → đẩy lại kế toán → scan verify.
3. Xong → mark stale giá vốn theo kỳ.

## Luồng resume — tiến trình gián đoạn
1. Lỗi/timeout tạm thời → hệ thống **tự thử lại tối đa 3 lần** (chờ 3s, 6s, 9s), UI hiện "đang thử lại".
2. Hết 3 lần → phiên **Failed**, giữ checkpoint.
3. User vào lại màn → bấm **Tiếp tục** → job resume từ đúng bước đang dở (idempotent).

---

## Case biên
- **Bấm Bắt đầu khi đơn vị đang có job chạy** → khóa Redis chặn, không tạo job trùng (1 job/đơn vị).
- **Job Running quá 300 phút** → coi là *stale* → đơn vị được phép chạy lại (chống khóa vĩnh viễn khi crash).
- **Tồn âm (kho cho chốt âm)** → tính giá vốn bình quân **coi tồn âm = 0** (tử + mẫu), nhưng tồn lưu vẫn âm.
- **Phiếu chuyển kho** → vế đến nhập theo **giá vốn vế đi** cùng transaction; **hoàn giao vận (loại 6)** → nhập lại theo **giá vốn hiện tại**, không đổi giá bình quân.
- **Phiếu 11 (giữ chỗ)** → SKIP khi replay giá vốn (không tác động SL).
- **Serial không map được đơn/phiếu mới** → đếm `soOrphan`, ops sửa tay.
- **Chọn hướng Stream** → hiện **không có tác dụng** (ép cứng Anchor).
- **Reset khi kho đang giao dịch** → ghi đè absolute có thể **nuốt thay đổi đồng thời** (rủi ro nghiệp vụ).

---

## Ví dụ có số
Sản phẩm "Sữa rửa mặt B" tại Kho HCM, ledger sau khi replay giá vốn (Anchor bình quân):
- Nhập 10 cái @ 100.000 → tồn 10, giá vốn TB = 100.000.
- Nhập thêm 5 @ 130.000 → tồn 15, giá vốn TB = (10×100.000 + 5×130.000)/15 = 110.000.
- Xuất 4 cái → tồn 11, giá vốn TB **không đổi** = 110.000.
- Xuất tiếp 12 (kho cho chốt âm) → tồn thật = **−1** (giữ âm để clamp lần sau).
- Nhập 3 @ 120.000 → mẫu số clamp tồn âm về 0: (0 + 3) = 3; giá vốn TB = (110.000×0 + 120.000×3)/3 = 120.000; tồn thật = −1 + 3 = **2**.

Sau **Reset toàn kho** đơn vị: mọi `KhoChiTietGiaVonTheoKy` của đơn vị được set `TrangThaiCanhBao = CanTinhLaiDoResetTonKho` → màn giá vốn theo kỳ hiện cảnh báo cần tính lại.

---

## Hậu điều kiện
- Tồn kho + giá vốn được tính lại theo ledger; phiếu dựng/reset đẩy lại sang **kế toán** + resync **lịch sử kho**.
- **Giá vốn theo kỳ cả đơn vị** bị đánh dấu cần tính lại (sau Reset/Tái tạo).
- Serial/lô re-point theo phiếu mới; audit ghi vào `SanPhamImeiTaiTaoLog`/`SanPhamLoDateTaiTaoLog`.
- Phiên có kết quả tra cứu được (PhienGanNhat / KetQua / TrangThai).
