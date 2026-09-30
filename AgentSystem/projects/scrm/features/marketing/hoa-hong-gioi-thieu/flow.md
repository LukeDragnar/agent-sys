# Luồng — Hoa hồng giới thiệu

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc một đơn vị (DonViId) và chi nhánh (IdChiNhanh); CRUD cần vai trò admin (DV-AD-TONG/CN/MCN).
- Đã có **nhóm hoa hồng** cấu hình % trước khi tạo CTV (mỗi CTV phải gán 1 nhóm).
- Để có số liệu thống kê: đơn gắn CTV đã được **CommissionFeeUpdateOrder** xử lý ghi dữ liệu.

## Luồng cấu hình (admin)

**Nhóm hoa hồng CTV (master — cấu hình trước):**
1. **Thêm mới** (`ThemMoi`) → gắn phân loại, cờ `IsSuDungHoaHongChienDich`… → validate + insert.
2. Cấu hình con: hoa hồng sản phẩm (`CauHinhHoaHongSanPhamCTV`), hoa hồng doanh số (`CauHinhHoaHongDoanhSoCTV`), hoa hồng chiến dịch.
3. **Sao chép** (`Copy`) / **Áp dụng theo phân loại** (`Apply`); **Bật/tắt sử dụng** (`CapNhatTrangThaiSuDung/{id}/{loaiTrangThai}`).
4. **Xóa** (`Xoa`) → đi qua SP `PRC_CommissionFee_DeleteNhomHoaHong_ById` (dọn cấu hình con).
5. Import Excel: `DownloadFileTemplateImport` → `Import` → `DownloadDataWrong/{id}`.

**Cộng tác viên (CTV):**
1. **Thêm mới** (`ThemMoi`) → gắn `IdNhomHoaHongCTV`, kiểu loại (CTV/nhân viên), `IdUser`, `MaCTV`.
2. Sửa (`CapNhat`) / Xóa (`Xoa`) / Lấy theo Id (`LayTheoId`); lọc grid (`TimTheoDieuKien` — non-admin ép chi nhánh).
3. Import Excel: `DownloadFileTemplateImport` → `Import` → `DownloadDataWrong/{id}`.

## Luồng dùng (phát sinh + thống kê hoa hồng — xuyên module)
1. Đơn hàng gắn `IdCongTacVien` khi tạo/xử lý.
2. Khi đơn được xử lý → **CommissionFeeUpdateOrder** tính hoa hồng theo nhóm của CTV → ghi `DonHangChiTiet` / `DonHangChiTietChienDich`.
3. Màn thống kê đọc dữ liệu này qua các SP báo cáo:
   - HH sản phẩm: `GetStats_SanPham_ByCTV` (+ bản liên kết).
   - HH doanh số: `GetStats_DoanhSo_ByCTV` (+ liên kết).
   - HH chiến dịch: `GetStats_ChienDich_ByCTV` (+ chi tiết).

## Case biên
- **Non-admin lọc CTV/nhóm** → ép theo chi nhánh.
- **Nhóm tắt cờ `IsSuDungHoaHongChienDich`** → đơn **không phát sinh** `DonHangChiTietChienDich` dù chiến dịch có cấu hình.
- **Đơn gắn CTV nhưng chưa được CommissionFeeUpdateOrder xử lý (hoặc lỗi)** → thống kê **trống** dù đơn đã bán.
- **Xóa nhóm còn CTV gán** → cấu hình con bị dọn qua SP (không cascade qua CTV) → CTV mất cấu hình; cần kiểm tra ràng buộc trước.
- **Autocomplete/check CTV, lấy KM theo CTV** chỉ `[SBAuthorize()]` rỗng (chỉ login) → quyền lỏng.
- **Import lỗi dòng** → dòng sai lưu lại theo id, tải file lỗi về sửa.

## Ví dụ cụ thể có số
Shop F tạo nhóm "CTV Vàng": hoa hồng sản phẩm 8%, bật cờ hoa hồng chiến dịch.
- Thêm CTV "Anh Nam" gán nhóm "CTV Vàng".
- Đơn 5.000.000đ (toàn SP áp 8%) gắn CTV "Anh Nam", thanh toán → CommissionFeeUpdateOrder ghi hoa hồng `5.000.000 × 8% = 400.000đ`.
- Vào thống kê HH sản phẩm theo CTV "Anh Nam" → thấy 400.000đ.
- Nếu nhóm "CTV Vàng" **tắt** cờ hoa hồng chiến dịch → đơn nằm trong chiến dịch X vẫn **không** sinh hoa hồng chiến dịch.
- Admin đổi nhóm "CTV Vàng" còn 6% → các đơn mới của mọi CTV trong nhóm tính theo 6%.

## Hậu điều kiện
- CRUD nhóm/CTV → ghi DB; xóa nhóm dọn cấu hình con qua SP.
- Đơn gắn CTV được xử lý → ghi dữ liệu hoa hồng (DonHangChiTiet / DonHangChiTietChienDich).
- Thống kê đọc dữ liệu đã ghi (read-only) — là điểm đọc cuối của pipeline hoa hồng.
