# Luồng — Quản lý hồ sơ nhân viên

## Tiền điều kiện
- Người thao tác đã đăng nhập với vai phù hợp: tài khoản nhân viên cần HT-AD (một số tra cứu đơn vị mở thêm HT-CSKH); phòng ban/nhóm cấp đơn vị cần DV-AD-TONG, cấp chi nhánh cần DV-AD-CN.
- Đơn vị đã tồn tại (để gắn nhân viên); ngành nghề/domain của đơn vị đã có (hệ thống tự lấy khi tạo user).
- Đã có cây phòng ban / nhóm (nếu muốn gắn nhân viên vào).

## Luồng dùng (admin — tài khoản nhân viên)
1. Mở **Quản lý nhân viên** → `TimTheoDieuKien` load lưới theo bộ lọc.
2. **Thêm mới** (`ThemMoi`): nhập thông tin + chọn đơn vị → hệ thống resolve đơn vị (GetOrgByIdAsync) để lấy ngành nghề + domain; nếu không có đơn vị → set IdDonVi/IdChiNhanh/IdNganhNghe = Guid.Empty, domain theo môi trường → tạo tài khoản (ThemMoiTaiKhoanAsync).
3. **Sửa** (`CapNhatThongTin`): chỉ resolve lại ngành nghề/domain khi có đơn vị hợp lệ.
4. **Khóa/Mở** (`KhoaTaiKhoan`), **Đổi mật khẩu** (`DoiMatKhau`), **Xóa** (`XoaTaiKhoan`) theo nhu cầu.
5. **Đồng bộ** (`DongBoUserByDonVi`) khi dữ liệu user lệch; theo dõi tiến trình bằng `GetStatusDongBoUserByDonVi`.

## Luồng cấu hình (admin — phòng ban & nhóm)
1. **Phòng ban:** `GetCoCauPhongBan_Unit/_Branch` load cây → `ThemMoi_Unit/_Branch`, `CapNhat_*`, `Xoa_*`, `SapXepCoCauPhongBan` (kéo-thả), `ThemMoi_UnitQuanLy/_BranchQuanLy` (thêm phòng ban kèm người quản lý). Bản Branch tự ép `IdChiNhanh = CurrentUser.IdChiNhanh`.
2. **Nhóm nhân viên:** `TimTheoDieuKien` load lưới nhóm → `ThemMoi-Nhom` (kèm danh sách user), `CapNhap-Nhom`, `Xoa/{id}`. Lấy nhân viên trong nhóm: `LayNhanVienByNhom/{idNhom}`.
3. **Thêm/bớt người trong nhóm:** `PhongBanNhanVien.ThemMoi_Unit/_Branch` (AddMultiUser), `Xoa_Unit/_Branch`.
4. **Khởi tạo mẫu:** `InitStaffGroupSystem` / `InitStaffGroupOrg` sinh nhóm mẫu theo ngành nghề.

## Các trường hợp / case biên
- **Tạo user không có đơn vị:** IdDonVi/IdChiNhanh/IdNganhNghe = Guid.Empty; Domain theo môi trường (Production→quantri.sandbox.com.vn, Staging→staging…, Sprint→sprint…, Development→localhost).
- **Tạo user có đơn vị nhưng đơn vị không tồn tại:** ThemMoi trả NotFound (resolve đơn vị thất bại).
- **Thao tác phòng ban/nhóm cấp Branch:** luôn ép chi nhánh theo CurrentUser → admin chi nhánh không đụng được dữ liệu chi nhánh khác.
- **Sắp xếp cơ cấu:** key role khai báo `"DV-AD-TONG DV-AD-CN"` thiếu dấu phẩy → cần kiểm tra parse role (xem dev.md).
- **Thêm phòng ban kèm quản lý (\_UnitQuanLy/\_BranchQuanLy):** dùng lại key `PhongBan_ThemMoi_*` của thêm thường → không phân quyền tách riêng cho hành vi gán quản lý.
- **Route nhóm sai chuẩn:** `ThemMoi-Nhom`/`CapNhap-Nhom` (không phải `ThemMoi`/`CapNhatThongTin` kế thừa) → FE phải gọi đúng tên.
- **Init nhóm mẫu:** không gắn key → mọi user đã login có thể gọi (rủi ro tạo dữ liệu mẫu ngoài ý muốn — xem dev.md).

## Ví dụ cụ thể có số
Đơn vị X có 1 chi nhánh trung tâm + 2 chi nhánh phụ.
- Admin tổng (DV-AD-TONG) tạo cây phòng ban: "Kinh doanh" (cha) → "Sale Online", "Sale Offline" (con) bằng `ThemMoi_Unit` rồi kéo-thả sắp xếp.
- Admin chi nhánh phụ A (DV-AD-CN) tạo nhóm "Sale CN A" bằng `ThemMoi-Nhom`, hệ thống tự gán chi nhánh A; thêm 4 nhân viên vào nhóm bằng `PhongBanNhanVien.ThemMoi_Branch`.
- Admin hệ thống (HT-AD) tạo tài khoản cho nhân viên mới "Hùng" gắn đơn vị X, chi nhánh A → hệ thống lấy ngành nghề + domain của X → tạo xong; sau đó thêm Hùng vào nhóm "Sale CN A".
- Hùng nghỉ việc 2 tháng sau → HT-AD bấm **Khóa tài khoản** (giữ lịch sử công + hoa hồng), không xóa.
- Tài khoản 1 nhân viên lệch dữ liệu → admin super chạy `DongBoUserByDonVi` cho đơn vị X, theo dõi `GetStatusDongBoUserByDonVi` đến khi xong.

## Hậu điều kiện
- Thêm/sửa tài khoản → đồng bộ user sang các service liên quan theo đơn vị; tác động phân quyền + đăng nhập.
- Đổi cây phòng ban / thành viên nhóm → build & cache lại cơ cấu (`GetAndSetCoCauCache_TheoDonVi`) → ảnh hưởng phân cấp lead/quản lý sale, phân công sale/CSKH/chia đơn/chia chat.
- Khóa tài khoản → nhân viên không đăng nhập được nhưng dữ liệu lịch sử vẫn giữ.

---

## (Bổ sung Pha 2 — phần Hồ sơ nhân sự HR; product xác nhận có tách feature riêng không)

> Luồng dưới đây cho màn **Hồ sơ nhân sự HR** (`HumanController`, service HumanResource) — mở/sửa nhân thân + nghiệp vụ HR + ảnh + giấy tờ của 1 nhân viên. Phần trên dùng service Core; phần này độc lập, gắn vào nhân viên qua `IdUser`.

### Tiền điều kiện (HR)
- Nhân viên (tài khoản User) đã tồn tại (`id` mở hồ sơ = `User.Id`).
- Người thao tác đăng nhập với 1 trong các vai đơn vị: DV-AD-TONG, DV-AD-CN, DV-AD-MCN, DV-KT, DV-KT-TONG, DV-QLDV.

### Luồng dùng (mở & sửa hồ sơ nhân sự)
1. **Mở hồ sơ** (`GET GetHumanById/{id}`): hệ thống lấy User theo `id` → tìm bản ghi `NhanSu` theo `IdUser`.
   - Nếu chưa có `NhanSu` → **tự tạo mới 1 bản ghi NhanSu rỗng** gắn `IdUser` (commit trong chính lần GET này), rồi map dữ liệu.
   - Trả về `NhanSuDto` = nhân thân (HoTen/GioiTinh/Email/SDT/NgaySinh/DiaChi/AvatarUrl lấy từ User) + field HR (BiDanh, Facebook, các mốc ngày, NguonTuyenDung, QueQuan) + danh sách giấy tờ (`FileNhanSu` có `UserType=1`, sắp theo `Type`).
2. **Thêm thông tin HR** (`POST ThemMoi`): `ValidateRequest` kiểm tra quan hệ ngày (KHÔNG check HoTen) → insert `NhanSu` (chỉ field HR).
3. **Sửa hồ sơ** (`POST CapNhat`): validate ngày → update field HR; rồi **đồng bộ danh sách giấy tờ** trong cùng transaction:
   - Item `Id == null` → insert mới (`UserType=1`).
   - Item có `Id` → update; đồng thời loại item đó khỏi tập "đang có".
   - Các giấy tờ "đang có" mà không xuất hiện trong request → **xóa** (soft-delete).
4. **Ảnh nhân sự**: `POST GetImage` (lọc theo `IdNhanSu` + khoảng `TuNgay/DenNgay`), `POST UploadImage` (insert `HinhAnh`), `POST XoaImage/{id}` (xóa ảnh theo Id).
5. **Giấy tờ rời**: `POST ThemLoaiGiayTo` (thêm 1 loại giấy tờ — validate IdUser/TenFile/Type, LinkFile có thể null), `POST XoaFileGiayTo/{id}` (xóa 1 giấy tờ theo Id).

### Case biên (HR)
- **Mở hồ sơ của User không tồn tại** (`id` sai): trả lỗi 404 "Không tìm thấy user".
- **Mở hồ sơ lần đầu (chưa có NhanSu)**: GET tạo mới NhanSu rỗng rồi mới trả dữ liệu → 1 GET sinh ghi DB (side-effect đọc→ghi; nếu insert lỗi trả "Lỗi kết nối!").
- **Nhập ngày ngược**: ngày nghỉ việc < ngày vào công ty → "Ngày nghỉ việc phải sau ngày vào công ty."; ngày lên chính thức < ngày vào công ty → "Ngày lên chính thức phải sau ngày vào công ty.".
- **Sửa hồ sơ với Id NhanSu không tồn tại** (`CapNhat`): trả NotFound.
- **Bỏ giấy tờ khỏi danh sách khi lưu**: giấy tờ không có trong `giayToDtos` lúc `CapNhat` sẽ bị xóa khỏi DB (đồng bộ theo request).
- **Thêm loại giấy tờ thiếu dữ liệu**: thiếu `IdUser`/`TenFile`/`Type` → trả lỗi validate tương ứng ("Thiếu IdUser…", "Tên giấy tờ không được bỏ trống.", "Phải chọn biểu tượng.").
- **Quyền thêm/xóa giấy tờ**: cả `ThemLoaiGiayTo` và `XoaFileGiayTo` đang dùng nhầm key `Human_XoaImage` → không gate riêng cho hành vi thêm/xóa giấy tờ (xem dev.md).

### Ví dụ cụ thể có số (HR)
Nhân viên "Hùng" (đã tạo tài khoản ở phần trên, `User.Id = U1`):
- Admin đơn vị mở hồ sơ Hùng lần đầu (`GetHumanById/U1`) → chưa có NhanSu → hệ thống tạo `NhanSu{ IdUser=U1 }` rỗng, trả về form: HoTen/Email/SĐT (từ tài khoản) + các ô HR trống.
- Nhập: NgàyVàoCTy = 01/03/2026, NgàyLênChínhThức = 01/06/2026, NguồnTuyểnDụng = "Giới thiệu" → bấm Lưu (`CapNhat`) → validate ngày OK (01/06 > 01/03) → cập nhật.
- Sang tab Giấy tờ: thêm 2 loại "CMND/CCCD" (Type=1) và "Quyết định bổ nhiệm" (Type=3) bằng `ThemLoaiGiayTo` → 2 bản ghi `FileNhanSu{ UserType=1 }`.
- Lần lưu hồ sơ sau, FE chỉ gửi 1 giấy tờ "CMND/CCCD" trong `giayToDtos` → "Quyết định bổ nhiệm" không còn trong request → hệ thống **xóa** giấy tờ đó. (Muốn giữ thì phải gửi cả 2.)
- Upload 3 ảnh chân dung qua `UploadImage` → tab ảnh lọc theo khoảng ngày 01/03/2026–30/06/2026 trả về 3 ảnh.

### Hậu điều kiện (HR)
- Thông tin HR + giấy tờ + ảnh lưu trong service HumanResource (`NhanSu`, `FileNhanSu`, `HinhAnh`), gắn nhân viên qua `IdUser`.
- Không sinh side-effect cross-service (không đồng bộ sang service khác — khác với phần tài khoản ở trên).
- Nhân thân (HoTen/Email/…) KHÔNG bị thay đổi bởi màn HR (chỉ đọc từ User để hiển thị).
