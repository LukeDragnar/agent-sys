---
controller: HumanController
module: nhan-su
service: HumanResource
man_hinh_ui: "Nhân sự > Hồ sơ nhân sự (form chỉnh sửa thông tin nhân thân HR + tab Giấy tờ liên quan / Quyết định bổ nhiệm + ảnh nhân sự)"
muc_dich: "Quản lý hồ sơ nhân sự (phần nghiệp vụ HR gắn vào User core): xem/thêm/sửa thông tin nhân sự, quản lý ảnh và file giấy tờ của nhân sự."
alias: [Human, NhanSu, "nhân sự", "hồ sơ nhân sự", "thông tin nhân viên"]
auth: public
state_machine: []
deprecated: false
entities: [NhanSu, FileNhanSu, HinhAnh, User]
stored_procs: []
depends: [User (Core — IdUser), ServiceSettings]
cross_module_in: [Core/User (NhanSu.IdUser map 1-1 với User.Id)]
cross_module_out: []
---

# Human — Quản lý hồ sơ nhân sự

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy thông tin nhân sự theo Id (User) | Mở hồ sơ | GET `GetHumanById/{id}` | `Human_GetHumanById` (AD-TONG/CN/MCN, KT, KT-TONG, QLDV) | public | id = User.Id | Lấy User rồi map sang NhanSu theo `IdUser`; nếu chưa có NhanSu thì **tự tạo mới** record NhanSu rỗng gắn IdUser; trả NhanSuDto (nhân thân từ User + field HR + list giấy tờ `UserType=1`). | EF (`User`, `NhanSu`, `FileNhanSu`) | Tạo NhanSu ngầm trong GET (side-effect đọc→ghi) |
| 2 | Thêm mới nhân sự | Lưu | POST `ThemMoi` | `Human_ThemMoi` | public | — | Validate ngày (nghỉ việc ≥ vào CTy, lên chính thức ≥ vào CTy) → insert NhanSu (chỉ field HR, KHÔNG có HoTen). | EF | — |
| 3 | Cập nhật nhân sự | Lưu | POST `CapNhat` | `Human_CapNhat` | public | — | Validate ngày → update field HR; đồng bộ list giấy tờ (`giayToDtos`): Id null = insert, có Id = update, thiếu trong req = xóa. | EF | — |
| 4 | Lấy ảnh nhân sự | (tab ảnh) | POST `GetImage` | `Human_GetImage` | public | — | Lọc HinhAnh theo `IdNhanSu` + khoảng `TuNgay/DenNgay`. | EF (`HinhAnh`) | — |
| 5 | Thêm ảnh nhân sự | Upload ảnh | POST `UploadImage` | `Human_UploadImage` | public | — | Insert HinhAnh (IdNhanSu, IdHinhAnh, TenAnh). | EF | — |
| 6 | Xóa ảnh nhân sự | Xóa ảnh | POST `XoaImage/{id}` | `Human_XoaImage` | public | — | Xóa HinhAnh theo Id. | EF | — |
| 7 | Thêm loại giấy tờ | Thêm giấy tờ | POST `ThemLoaiGiayTo` | `Human_XoaImage` (key dùng lại) | public | — | Insert FileNhanSu (UserType=1, TenFile=tên loại, Type=icon, LinkFile có thể null). | EF (`FileNhanSu`) | Quyền dùng nhầm key `Human_XoaImage` thay vì key thêm |
| 8 | Xóa file giấy tờ | Xóa giấy tờ | POST `XoaFileGiayTo/{id}` | `Human_XoaImage` (key dùng lại) | public | — | Xóa 1 record FileNhanSu theo Id (soft-delete qua EfRepository). | EF | Quyền dùng nhầm key `Human_XoaImage` |

<!-- Lưu ý: GetAllAsync đã bị comment trong controller (không expose). -->

## Quan hệ
- **Gọi sang:** không gọi service ngoài; thuần EF qua repository (`NhanSu`, `FileNhanSu`, `HinhAnh`, `User`).
- **Dữ liệu (entities):** `NhanSu` (field nghiệp vụ HR: BiDanh, Facebook, NgayVaoCTy, NgayLenChinhThuc, NgayNghiViec, NguonTuyenDung, QueQuan, `IdUser`) map 1-1 với `User` core (nhân thân: HoTen, GioiTinh, Email, SDT, NgaySinh...). `FileNhanSu` (UserType=1) là giấy tờ liên quan/quyết định bổ nhiệm. `HinhAnh` = ảnh nhân sự.
- **Ripple / đồng bộ:** không sinh side-effect cross-service.

## Lỗi đã biết / lưu ý kỹ thuật
- `HumanController.cs:29-34` — `GetHumanById` là HTTP GET nhưng service `TimTheoIdAsync` **tự tạo mới NhanSu** nếu User chưa có record → GET có side-effect ghi DB.
- `HumanController.cs:73,81` — `ThemLoaiGiayTo` và `XoaFileGiayTo` cùng dùng key quyền `Human_XoaImage` (copy nhầm), không có key riêng cho thêm/xóa giấy tờ.
- `NhanSuService.cs:387-400` — `ValidateRequest` cố ý KHÔNG check HoTen (HoTen lưu ở User core, form HR không gửi); chỉ validate quan hệ ngày.
