# Luồng — Lịch sử xóa (thùng rác công việc)

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc 1 đơn vị.
- Để xem danh sách: vai trò **DV-AD-TONG** (admin tổng đơn vị).
- Đã có bản ghi `LichSuXoa` — sinh tự động bởi `CongViec.XoaTheoId` / `QuyTrinhThucThi.XoaTheoId` khi xóa mềm.

## Luồng dùng (admin)
1. Mở **Công việc → Lịch sử xóa**.
2. Lọc grid (`TimTheoDieuKien`): Loai (CongViec / QuyTrinhThucThi), keyword (TenGoi), DaKhoiPhuc, khoảng ngày xóa. Trả SoCongViec / SoQuyTrinhThucThi / người xóa, sort NgayTao DESC.
3. **Khôi phục** (`KhoiPhuc/{id}`): load các bản ghi soft-deleted theo snapshot (bypass query filter) → reset IsDelete=false → set DaKhoiPhuc=true → publish CongViecChanged cho từng CongViecId.
4. Hoặc **Xóa vĩnh viễn** (`XoaVinhVien/{id}`): hard-delete bản ghi LichSuXoa.

## Luồng sinh bản ghi (nguồn — cross module)
- **Xóa công việc** (Quản lý công việc): gom subtree + lượt liên quan → soft-delete + tạo `LichSuXoa` (Loai="CongViec") mang snapshot Ids (CongViecIds, CongViecUserIds, CongViecMoRongIds, CongViecFileIds, QuyTrinhThucThiIds).
- **Xóa lượt thực thi** (Cấu hình công việc): delegate `CongViec.XoaTheoId` với Loai="QuyTrinhThucThi" → cùng cơ chế snapshot.

## Các trường hợp / case biên
- **Khôi phục bản ghi đã khôi phục** (`DaKhoiPhuc=true`) → lỗi "đã khôi phục trước đó".
- **Snapshot rỗng / hỏng** → lỗi khi khôi phục.
- **Một phần dữ liệu trong snapshot đã bị xóa cứng nơi khác** → khôi phục thiếu, im lặng (dùng `IgnoreQueryFilters` để load).
- **Xóa vĩnh viễn** → chỉ xóa bản ghi LichSuXoa; CV soft-deleted vẫn còn DB nhưng mất đường khôi phục (snapshot biến mất).
- **Lệch quyền**: list cần DV-AD-TONG nhưng KhoiPhuc/XoaVinhVien chỉ `[SBAuthorize]` → user thường biết id vẫn gọi được (xem dev.md).

## Ví dụ cụ thể có số
Admin xóa "Dự án A" (gốc + 3 con) lúc 10:00 ngày 15/06.
- Lịch sử xóa sinh 1 bản ghi: Loai=CongViec, TenGoi="Dự án A", SoCongViec=4, SoQuyTrinhThucThi=0, NgườiXóa=admin, DaKhoiPhuc=false.
- Ngày 16/06 admin lọc "Loai=CongViec, ngày xóa 15/06" → thấy bản ghi → bấm Khôi phục → 4 công việc reset IsDelete=false, DaKhoiPhuc=true → hiện lại trong lưới + publish CongViecChanged (reindex 4 doc).
- Bấm Khôi phục lần nữa → lỗi "đã khôi phục trước đó".
- Một bản ghi khác (lượt thực thi OB-1, SoCongViec=5) → bấm Xóa vĩnh viễn → bản ghi lịch sử biến mất; 5 CV soft-deleted vẫn nằm ẩn DB nhưng không còn khôi phục được.

## Hậu điều kiện
- **Khôi phục**: reset IsDelete/NgayXoa/NguoiXoa = null cho bản ghi trong snapshot (KHÔNG tạo bản ghi mới); set DaKhoiPhuc=true; transaction; publish CongViecChanged (isDelete=false) → ESExtended reindex.
- **Xóa vĩnh viễn**: chỉ hard-delete LichSuXoa, không dọn CV soft-deleted trong snapshot.
