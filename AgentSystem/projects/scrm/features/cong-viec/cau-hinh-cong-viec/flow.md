# Luồng — Cấu hình công việc

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc 1 đơn vị + chi nhánh.
- Các màn quy trình / lượt thực thi gate qua tiện ích (`KiemTraTienIch`).
- Đơn vị có **danh mục trạng thái** với mã chuẩn `CHUA_BAT_DAU` / `DANG_XU_LY` để lượt thực thi gán đúng (thiếu mã → IdTrangThai null).
- Để sinh người thực hiện gốc/phân công theo flow: cần dữ liệu **cơ cấu tổ chức**.

## Luồng cấu hình (admin / người thiết kế)
**Mẫu công việc (`CongViecMau`):**
1. Mở Danh sách mẫu → Thêm/Lưu (`Luu`) → đặt tên, IsActive, chọn field thuộc mẫu (`CongViecMauField`).
2. `LayBanDoFieldTheoMau` trả map `congViecMauId → field Ids hợp lệ` để Quản lý công việc chặn sửa field không thuộc mẫu.

**Quy trình (`QuyTrinh`):**
1. Mở Danh sách quy trình → Thêm/sửa → trình thiết kế flow.
2. Lưu (`Luu`) → ghi entity QuyTrinh + **insert 1 `QuyTrinhLichSu` Version = lastVersion+1** (lưu FlowData snapshot).
3. Tab Lịch sử phiên bản (`LichSuTheoQuyTrinh`) → danh sách version sort DESC.

**Thiết lập chung:** đặt số cấp công việc con cho phép (`CauHinh.DoSauCongViecCon`) + tham số khác.

## Luồng dùng (vận hành — chạy quy trình)
1. **Khởi chạy** (`KhoiChay`): chọn 1 quy trình → lấy phiên bản mới nhất (chưa có → lỗi "Quy trình chưa có phiên bản") → sinh/nhận mã (check trùng) → tạo lượt + **công việc gốc** (Level 1, trạng thái CHUA_BAT_DAU) → gán người thực hiện gốc (chọn lúc khởi chạy, fallback người khởi chạy) → áp ThongTinMacDinh. Transaction. Publish `CongViecChanged`.
2. **Bắt đầu** (`BatDau/{id}`): lượt phải `NgayBatDau == null` → chuyển gốc + lượt sang DANG_XU_LY, set NgayBatDau=now → `SpawnCongViecConTuFlowAsync` sinh cây công việc con. Publish CongViecChanged cho TẤT CẢ CV của lượt.
3. Lượt có thể nest (`IdQuyTrinhThucThiCha`).

## Các trường hợp / case biên
- **Khởi chạy quy trình chưa có version** → lỗi "Quy trình chưa có phiên bản".
- **Bắt đầu lại lượt đã bắt đầu** (`NgayBatDau.HasValue`) → lỗi "Lượt đã được bắt đầu".
- **Spawn cây con lỗi giữa chừng** → rollback toàn bộ transaction.
- **Đơn vị thiếu DmTrangThai mã chuẩn** → IdTrangThai = null (lượt/CV không có trạng thái).
- **Sinh mã lượt**: `{MaCode quy trình}-{seq+1}` trong đơn vị; quy trình không MaCode → prefix "QTTT".
- **Xóa lượt có CV gốc** → delegate `CongViec.XoaTheoIdAsync` (cascade subtree + lượt nested) với Loai="QuyTrinhThucThi". **Chưa bắt đầu (không CV gốc)** → soft-delete lượt + lượt con đệ quy + snapshot LichSuXoa.
- **Xóa mẫu công việc** → không kiểm tra CV đang dùng mẫu → dangling reference (`CongViec.CongViecMauId` trỏ mẫu đã xóa).
- **Lưu quy trình không đổi flow** → vẫn sinh version mới (không dedupe).

## Ví dụ cụ thể có số
Quy trình "Onboarding khách" có MaCode = "OB", đã lưu 3 lần (Version 1, 2, 3).
- Khởi chạy: hệ thống dùng Version 3, sinh mã lượt `OB-1` (lượt đầu), tạo công việc gốc "Onboarding khách - OB-1" trạng thái CHUA_BAT_DAU. Lúc này SoCongViec con = 0.
- Bấm Bắt đầu: gốc → DANG_XU_LY, sinh 4 công việc con theo flow (Gọi chào, Gửi tài liệu, Hẹn lịch, Ghi nhận phản hồi). Grid lượt hiển thị SoCongViec = 4.
- Khởi chạy lượt 2 → mã `OB-2`. Trong khi đó sửa quy trình thành Version 4 → lượt OB-1 và OB-2 vẫn chạy theo version cũ của mình (version pinning).
- Xóa lượt OB-1 (đã bắt đầu): `LayThongTinXoa` báo SoCongViec=5 (gốc+4 con), SoQuyTrinhThucThi=1 → vào Lịch sử xóa 1 cụm khôi phục được.

## Hậu điều kiện
- Lưu quy trình → ghi QuyTrinh + QuyTrinhLichSu (version mới).
- Khởi chạy/Bắt đầu → tạo CongViec (gốc + con) + publish `CongViecChanged` → ESExtended reindex.
- Xóa lượt → snapshot `LichSuXoa` (khôi phục); KHÔNG xóa cứng.
- Lượt đã khởi chạy giữ `QuyTrinhLichSuId` cố định → sửa quy trình không ảnh hưởng lượt đang chạy.
