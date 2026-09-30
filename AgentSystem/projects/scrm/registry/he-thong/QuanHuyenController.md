---
controller: QuanHuyenController
module: he-thong
service: Core
man_hinh_ui: "Hệ thống > Danh mục địa giới > Quận/Huyện: lưới + nút Thêm/Sửa/Xóa; dropdown Quận/Huyện lọc theo Tỉnh/TP"
muc_dich: "CRUD danh mục Quận/Huyện (cấp 3 cây địa giới, thuộc 1 Tỉnh/TP) + cung cấp dropdown Quận/Huyện theo Tỉnh."
alias: ["QuanHuyen", "quận huyện", "danh mục quận huyện", "DMQuanHuyen"]
auth: mixed
state_machine: []
deprecated: false
entities: [DMQuanHuyen]
stored_procs: []
depends: ["IQuanHuyenService", "SBBaseController<DMQuanHuyen, DMQuanHuyenDto>"]
cross_module_in: ["FE Hệ thống/Quận-Huyện gọi TimTheoDieuKien/ThemMoi/CapNhatThongTin/Xoa", "FE màn nhập địa chỉ gọi GetQuanHuyenByTinh để load dropdown theo Tỉnh"]
cross_module_out: ["Là cấp 3 cây địa giới — PhuongXa tham chiếu xuống; mọi entity có địa chỉ (KH, đơn vị, đơn hàng) dùng IdQuanHuyen"]
---

# QuanHuyenController — Danh mục Quận/Huyện

CRUD quận/huyện kế thừa `SBBaseController` (override `ThemMoi`/`CapNhatThongTin`/`XoaAsync`). Thêm `TimTheoDieuKien` (search) và `GetQuanHuyenByTinh` (dropdown lọc theo Tỉnh). Controller-level `[SBAuthorize("QuanHuyenController")]` bị comment (dòng 15), bảo vệ dựa vào key từng method + `[TienIch]` TI_HeThong → auth = mixed.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện | (load lưới) | POST `api/QuanHuyen/TimTheoDieuKien` | `QuanHuyen_TimTheoDieuKien` (HT-AD, HT-CSKH) | mixed | — | Lọc danh sách quận/huyện theo `QuanHuyenFilterDto`. | EF | — |
| 2 | Thêm mới | Thêm | POST `api/QuanHuyen/ThemMoi` | `QuanHuyen_ThemMoi` (HT-AD, HT-CSKH) | mixed | — | Validate `IdTinhThanh` bắt buộc + Mã/Tên không trống + không trùng trong cùng Tỉnh (GetCodeByRequest), trim khoảng trắng thừa rồi `base.ThemMoi`. | EF | — |
| 3 | Cập nhật | Sửa | POST `api/QuanHuyen/CapNhatThongTin` | `QuanHuyen_CapNhatThongTin` (HT-AD, HT-CSKH) | mixed | — | ModelState valid → map DTO→entity → kiểm tra tồn tại (GetById) → `CapNhatThongTin`. | EF | Update KHÔNG re-validate trùng Mã/Tên (chỉ ThemMoi mới check) |
| 4 | Xóa | Xóa | POST `api/QuanHuyen/Xoa/{id:Guid}` | `QuanHuyen_Xoa` (HT-AD, HT-CSKH) | mixed | — | Kiểm tra tồn tại trước (GetById) → `DeleteAsync`. | EF | Không chặn xóa khi còn Phường/Xã con tham chiếu (FK ràng buộc ở DB) |
| 5 | Lấy quận/huyện theo tỉnh | (load dropdown) | GET `api/QuanHuyen/GetQuanHuyenByTinh/{id:Guid}` | `QuanHuyen_GetQuanHuyenByTinh` (HT-AD, HT-CSKH) | mixed | — | Trả danh sách quận/huyện thuộc `id` Tỉnh/TP cho dropdown địa chỉ. | EF | — |
| 6 | Tìm theo Id / Lấy tất cả | (kế thừa) | GET `api/QuanHuyen/TimTheoId/{id}`, `LayTatCa` | — (login) | mixed | — | CRUD đọc kế thừa từ `SBBaseController` (`[SBAuthorize]` rỗng). | EF | — |

## Quy tắc nghiệp vụ (gating)
1. ThemMoi: `IdTinhThanh == null` → lỗi "Tỉnh/TP không được bỏ trống". (QuanHuyenController.cs:60)
2. ThemMoi: `Ma` hoặc `Ten` trống → lỗi "Mã, Tên không được bỏ trống." (dòng 99)
3. ThemMoi (chỉ khi `Id == null`): `GetCodeByRequest(Ma, IdTinhThanh) > 0` HOẶC `GetCodeByRequest(Ten, IdTinhThanh) > 0` → lỗi "Mã, Tên không được phép trùng" → check trùng scope theo Tỉnh. (dòng 102-110)

## Quan hệ
- **Gọi sang:** IQuanHuyenService (EF), AutoMapper DMQuanHuyenDto↔DMQuanHuyen.
- **Dữ liệu (entities):** `DMQuanHuyen` (FK `IdTinhThanh` → DMTinhThanh).
- **Ripple / đồng bộ:** không bắn event; danh mục nền — sửa/xóa ảnh hưởng dropdown địa chỉ.

## Lỗi đã biết / lưu ý kỹ thuật
- `QuanHuyenController.cs:15` controller-level `[SBAuthorize("QuanHuyenController")]` bị comment → các method kế thừa `LayTatCa`/`TimTheoId` chỉ còn `[SBAuthorize]` rỗng từ base.
- `CapNhatThongTin` không gọi `ValidateRequest` → có thể đổi Mã/Tên trùng với bản ghi khác.
