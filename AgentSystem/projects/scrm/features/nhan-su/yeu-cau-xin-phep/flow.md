# Luồng — Yêu cầu & xin phép (Phiếu yêu cầu)

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc đơn vị + chi nhánh.
- Chi nhánh đã có **khung giờ làm việc** (lấy từ Booking) để đăng ký ca.
- Với làm thay (Loại 2): người được làm thay (B) phải cùng đơn vị, có chi nhánh, có ca trong ngày.
- Cấu hình đơn vị xác định **vai được duyệt phiếu** (`RoleDuocPhepDuyetPhieuYeuCau`) và **có bắt buộc chụp ảnh khi làm hộ không** (`KhongBatBuocChupAnhKhiLamHo`).

## Luồng dùng (nhân viên)
1. **Xem phiếu của tôi:** `LayPhieuYeuCauCuaToi` — phiếu liên quan tôi (đăng ký HOẶC được làm hộ) theo ngày, loại trừ phiếu đã duyệt.
2. **Đăng ký làm ca của mình (Loại 1):** `DangKyLam` — chọn khung giờ + ngày → tạo phiếu Loại 1 trạng thái ChoDuyet (có Redis lock chống bấm trùng + check trùng ca).
3. **Đăng ký làm thay (Loại 2):** `LayCaCuaNhanVien` (chọn ca của B theo ngày, kèm cờ DaCoPhieuChoDuyet) → `DangKyLamThayThe` (tạo phiếu Loại 2 ChoDuyet).
4. **Chấm công trên phiếu (Loại 2):** `ChamCong_PhieuYeuCau` (multipart) — `TrangThaiCheckIn` 1=check-in, 2=check-out; selfie bắt buộc tùy cấu hình; upload qua FileService.
5. **Hủy phiếu:** `HuyDangKy/{id}` — chỉ chủ phiếu, chỉ phiếu ChoDuyet → HuyBoiNhanVien.

## Luồng cấu hình / duyệt (quản lý)
1. **Xem grid quản lý:** `TimTheoDieuKien` (ép tenant + filter loại/trạng thái/ngày/NV, phân trang), chi tiết `TimTheoId/{id}`.
2. **Duyệt Loại 1:** `DuyetDangKyLam` — re-check trùng ca → tạo `CaLamViecNhanVienNangCao` (IsDuyet=true) → phiếu DaDuyet + gắn IdNangCao.
3. **Từ chối Loại 1:** `TuChoiDangKyLam` — bắt buộc lý do (GhiChu) → phiếu TuChoi.
4. **Duyệt Loại 2:** `DuyetLamThayThe` — tạo GLV/NangCao cho A + GLV NghiLam cho B (transaction); sync user + clear cache best-effort ngoài transaction.
5. **Từ chối Loại 2:** `TuChoiLamThayThe` — bắt buộc lý do → phiếu TuChoi.

## Các trường hợp / case biên
- **State machine** (EnumTrangThaiPhieuYeuCau): ChoDuyet(1) → DaDuyet(2) | TuChoi(3) | HuyBoiNhanVien(4). Chỉ ChoDuyet mới duyệt/từ chối/hủy/chấm công; sai trạng thái bị chặn.
- **Loại phiếu** (EnumLoaiPhieuYeuCau): DangKyLam(1), DangKyLamThayThe(2). Mỗi action chặn nếu sai loại.
- **DangKyLam:** chặn khung giờ không thuộc CN của user; chặn ca quá khứ; Redis lock theo (NV, ngày, khungGio, đơn vị); check trùng ca; chống 2 phiếu ChoDuyet cùng key.
- **DangKyLamThayThe:** B cùng đơn vị + có chi nhánh; khung giờ thuộc CN của B; B thực có ca (`KiemTraNhanVienCoCa`) và chưa đi làm; A không trùng giờ; Redis lock; chống trùng phiếu Loại 2 ChoDuyet; **không làm thay chính mình**.
- **ChamCong_PhieuYeuCau:** chỉ chủ phiếu; chỉ phiếu Loại 2 ChoDuyet; CheckIn chỉ khi chưa có GioDen; CheckOut chỉ khi đã có GioDen & chưa có GioVe; selfie bắt buộc trừ khi `KhongBatBuocChupAnhKhiLamHo == true`.
- **DuyetLamThayThe khác chi nhánh:** cùng CN → IdKhungGioMap mặc định = IdKhungGio của phiếu; khác CN → **bắt buộc IdKhungGioMap thuộc CN của A**. Validate B chưa check-in/out (ScanStatus 1/2). Case 2A (chưa chấm công trên phiếu) → tạo NangCao cho A; Case 2B (đã chấm trên phiếu) → tạo GLV DiLam cho A tính đi muộn/sớm/về sớm. Luôn tạo GLV NghiLam cho B.
- **Gửi trùng:** Redis lock ~15s cho DangKyLam/DangKyLamThayThe (chống bấm 2 lần).

## Ví dụ cụ thể có số
Chi nhánh Q1 và Q3 cùng đơn vị. Ca chiều 13:00–17:00 ngày 21/06.
- Nhân viên An (Q1) đăng ký làm ca chiều của mình (`DangKyLam`) → phiếu Loại 1 ChoDuyet. Quản lý duyệt → tạo ca nâng cao cho An, phiếu → DaDuyet.
- An thử đăng ký lại đúng ca đó lần nữa → bị chặn "trùng phiếu/trùng ca".
- Nhân viên Bình (Q1) xin làm thay ca chiều của Châu (Q3). Hệ thống kiểm tra Châu cùng đơn vị, có ca, chưa đi làm → cho tạo phiếu Loại 2 ChoDuyet.
- Bình vào phiếu Check-in lúc 13:05 (chụp selfie) → đến 17:00 Check-out.
- Quản lý duyệt làm thay: vì khác chi nhánh (Q1 vs Q3), phải chọn khung giờ Q1 tương ứng → hệ thống tạo buổi công DiLam cho Bình (tính đi muộn 5 phút) + ghi NghiLam cho Châu, đồng bộ user 2 chi nhánh.
- Nếu Bình thử đăng ký làm thay chính Bình → bị chặn "không làm thay chính mình".

## Hậu điều kiện
- Duyệt phiếu → sinh `CaLamViecNhanVienNangCao` và/hoặc `GioLamViecNhanVien` (ripple sang lịch + bảng công); người được làm thay (B) được ghi NghiLam.
- Sau commit duyệt làm thay: gọi `SyncDataUser` + `ClearCache_User_Group_By_ChiNhanh` (Core) cho cả CN A và CN B nếu khác nhau (best-effort, không fail nghiệp vụ).
- Chấm công Loại 2 → upload ảnh selfie lên FileService, append `ThongTinChamCong` vào phiếu.
- Phiếu chuyển trạng thái cuối (DaDuyet/TuChoi/HuyBoiNhanVien) → không thao tác tiếp được.
