# Danh mục đơn hàng & tác nghiệp (gốc — cấp hệ thống) — Luồng

## Tiền điều kiện
- Người dùng là **quản trị viên hệ thống**.
- Danh mục đơn hàng (bản quản trị): tiện ích superadmin (`TI_Chung`, cờ isAdmin).
- Danh mục tác nghiệp (bản hệ thống): role **HT-AD** / **HT-CSKH** (route `api/QuanTriArea/DMTacNghiep`).

## Luồng dùng (Danh mục đơn hàng — quản trị)
1. Mở màn → `POST api/SanboxArea/DMDonHang/TimTheoDieuKien` (filter `DMDonHangFilterDto`, grid).
2. Mở chi tiết → `GET .../TimTheoId/{id}` (key rỗng, login-only).
3. Thêm mới → `POST .../ThemMoi` (`Create`). **Không** clear cache trong controller.
4. Cập nhật → `POST .../CapNhat` (`Update`). **Không** clear cache trong controller.
5. Xóa → **`GET .../Xoa/{id}`** → `DeleteAsync` rồi `ClearCache_DMDonHang_HT()` → dropdown DMDonHang (kể cả `api/DMDonHang/DropDownList` public) lấy lại dữ liệu mới.

## Luồng dùng (Danh mục tác nghiệp — hệ thống)
1. Mở màn (bản hệ thống) → list theo điều kiện (`HeThong*` method của service); có thể lọc theo ngành nghề (`TimTheoDieuKienByNganhNghe`).
2. Thêm mới (`InsertAsync`) / Cập nhật (`UpdateOneAsync`) / Xóa (`DeleteOneItemAsync/{id}`).
3. Mỗi bản ghi có cờ `IsKhoiDau` (bước khởi đầu), `IsKhoSo` (khóa số + `KhoSoThoiGian`), `IsSuaGio`, `Color`.
4. Hành vi gắn chi nhánh phụ thuộc cờ `IsEnableTacNghiepTheoChiNhanh`: bật → gắn `IdChiNhanh`, tắt → dùng chung đơn vị.

## Các trường hợp / case biên
- **Thêm/Sửa danh mục đơn hàng → dropdown chưa cập nhật**: vì controller không clear cache ở Thêm/Sửa (chỉ ở Xóa) → mục mới chờ lần clear kế tiếp.
- **Xóa danh mục đơn hàng qua GET**: thao tác ghi bằng GET — rủi ro bị trigger qua link/crawler.
- **Hai màn tác nghiệp trùng key nhưng khác route/role**: bản hệ thống (`api/QuanTriArea/DMTacNghiep`, HT-AD/HT-CSKH) vs bản đơn vị (`api/DMTacNghiep`, role đơn vị) → đừng nhầm.
- **Drift quyền ở bản đơn vị**: `UpdateOneAsync` cho cả role lead/QL sửa, rộng hơn Insert/Delete (chỉ AD); 2 endpoint tìm-theo-chi-nhánh dùng `[SBAuthorize]` rỗng (chỉ login).

## Ví dụ cụ thể có số
Hệ thống có 5 loại đơn hàng master và 8 bước tác nghiệp.
- Admin thêm loại đơn "Đơn ký gửi" (`DMDonHang/ThemMoi`) → DB có 6 loại, nhưng dropdown ở đơn vị vẫn hiện 5 (cache chưa clear) cho tới khi có thao tác clear.
- Admin xóa loại đơn "Đơn test" (`GET DMDonHang/Xoa/{id}`) → xóa + clear cache → dropdown lập tức còn đúng tập mới.
- Admin thêm bước tác nghiệp "Gọi lần 1" với `IsKhoiDau=true` (`InsertAsync`) → trở thành bước khởi đầu của quy trình follow-up.
- Admin đặt bước "Hẹn gọi lại" `IsKhoSo=true`, `KhoSoThoiGian=120` phút → khi áp bước này, số bị khóa 120 phút.

## Hậu điều kiện
- Danh mục đơn hàng: thêm/sửa ghi DB (`DMDonHang`); **chỉ xóa mới** clear cache hệ thống → dropdown đồng bộ.
- Danh mục tác nghiệp: thêm/sửa/xóa ghi DB (`DMTacNghiep`) + clear cache theo đơn vị/chi nhánh; `DMTacNghiepCauHinh` và `DMTacNghiepImport` phụ thuộc dữ liệu này.
