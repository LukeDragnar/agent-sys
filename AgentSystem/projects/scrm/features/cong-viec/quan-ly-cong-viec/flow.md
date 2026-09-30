# Luồng — Quản lý công việc

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc 1 đơn vị (DonViId) + chi nhánh (IdChiNhanh).
- Các màn quản lý gate qua tiện ích (`KiemTraTienIch`); riêng màn **Báo cáo công việc** không gate tiện ích (chỉ cần đăng nhập).
- Đơn vị đã có danh mục **trạng thái** (`DmTrangThai`) và **mức độ ưu tiên** (`DmMucDoUuTien`) để gán cho công việc.
- Cấu hình **độ sâu cấp con** (`CauHinh.DoSauCongViecCon`, mặc định 3) và cấu hình **đường tìm kiếm** (`IsUseElasticSearch`/global `Search:CongViec:UseES`, default ES = true).

## Luồng dùng (user)
1. Mở **Công việc → Danh sách công việc**.
2. Chọn tab lọc / nhập điều kiện (`TabFilter.Conditions` match all/any, trường mở rộng `ext_<Ma>`, người thực hiện/phê duyệt) → tìm.
3. Hệ thống chọn đường tìm: ES (`TimTheoDieuKienESAsync`) hoặc SP (`TimTheoDieuKienBySpAsync`) tùy cấu hình; trả grid + total + ExtFields + Files + số công việc con. CV cha sort NgayTao DESC, CV con ASC.
4. Bấm vào dòng → popup chi tiết (`TimTheoId`) gồm field mở rộng, người thực hiện/phê duyệt, file, tab Lịch sử.
5. Thao tác: Thêm/Sửa (`Save`), sửa nhanh 1 ô (`CapNhatTruong`), tự động sắp xếp (`TuDongSapXepThoiGian` preview), import Excel (`ImportExcel`).

## Luồng cấu hình (admin)
- Mẫu công việc / field động / số cấp con đặt ở feature **Cấu hình công việc** (xem folder `cau-hinh-cong-viec`). Quản lý công việc tiêu thụ các cấu hình đó.

## Các trường hợp / case biên
- **Tạo CV con vượt cấp**: `level = cha.Level + 1`; nếu `level > DoSauCongViecCon + 1` → lỗi "Chỉ cho phép tối đa N cấp công việc con".
- **Trùng mã**: mã user nhập → `IsTrungMaCongViecAsync` trong đơn vị (bỏ qua chính nó khi sửa); mã tự sinh luôn duy nhất.
- **Xóa công việc (mềm)**: gom subtree (đệ quy IdCongViecCha) + các lượt QuyTrinhThucThi liên quan → soft-delete + snapshot Ids vào `LichSuXoa` (Loai="CongViec"), publish `CongViecChanged` isDelete=true. Khôi phục được ở Lịch sử xóa.
- **Trước khi xóa**: `LayThongTinXoa` trả số công việc + số lượt thực thi sẽ bị ảnh hưởng (không ghi DB).
- **Import file rỗng** → "Chưa chọn file."; **> 1.000 dòng** → bị chặn (giới hạn cứng); **kiemThu=true** → chỉ validate, không ghi, trả lỗi theo dòng.
- **2 user sửa cùng ô**: optimistic-concurrency đã tắt (xem dev.md) → last-write-wins, không cảnh báo.
- **ES path**: security filter theo người tạo/giao đã comment → mọi user có thể thấy toàn bộ CV đơn vị (chỉ chặn AdminTong ở SP path); tập hiển thị ES cap 10.000 node → cây rất lớn có thể thiếu nhánh.

## Ví dụ cụ thể có số
Đơn vị có cấu hình `DoSauCongViecCon = 3`.
- Tạo cây: "Dự án A" (Level 1) → "Giai đoạn 1" (Level 2) → "Task 1.1" (Level 3) → "Subtask 1.1.1" (Level 4). Tạo thêm cấp dưới Subtask (Level 5) → `5 > 3+1` → lỗi "Chỉ cho phép tối đa 3 cấp công việc con".
- Báo cáo tổng quan: tổng 120 công việc; quá hạn 18 (NgayBatDauDuKien + Estimate giờ < now); theo trạng thái: Chưa bắt đầu 40, Đang xử lý 55, Hoàn thành 25.
- Xóa "Dự án A": `LayThongTinXoa` báo SoCongViec=4, SoQuyTrinhThucThi=1 → xác nhận → cả 4 CV + 1 lượt thực thi vào Lịch sử xóa thành 1 cụm khôi phục được.
- Import 1.200 dòng Excel → vượt giới hạn 1.000 → phải tách thành 2 file.

## Hậu điều kiện
- Mọi action ghi (Save / CapNhatTruong / xóa / spawn) → publish `CongViecChanged` (MassTransit) cho ESExtended reindex/xóa doc + ghi `CongViecLichSu` (MongoDB, audit).
- Xóa → snapshot vào `LichSuXoa` (khôi phục được), KHÔNG xóa cứng.
- Tự động sắp xếp → KHÔNG persist cho tới khi user confirm (gọi `CapNhatTruong`).
- Báo cáo công việc → read-only, không ghi, không phát event.
