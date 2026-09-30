---
controller: TinhTPController
module: he-thong
service: Core
man_hinh_ui: "Hệ thống > Danh mục địa giới > Tỉnh/Thành phố: lưới + nút Thêm/Sửa/Xóa; dropdown Tỉnh theo Quốc gia"
muc_dich: "CRUD danh mục Tỉnh/Thành phố (cấp 2 cây địa giới, thuộc 1 Quốc gia) + cung cấp dropdown Tỉnh lọc theo Quốc gia."
alias: ["TinhTP", "tỉnh thành", "tỉnh/TP", "danh mục tỉnh", "DMTinhThanh"]
auth: public
state_machine: []
deprecated: false
entities: [DMTinhThanh]
stored_procs: []
depends: ["ITinhTPService", "SBBaseController<DMTinhThanh, DMTinhThanhDto>"]
cross_module_in: ["FE Hệ thống/Tỉnh TP gọi TimTheoDieuKien/ThemMoi/CapNhatThongTin/Xoa", "FE dropdown địa chỉ gọi GetTinhByQuocGia"]
cross_module_out: ["Cấp cha của QuanHuyen/PhuongXa; entity có địa chỉ tham chiếu IdTinhThanh"]
---

# TinhTPController — Danh mục Tỉnh/Thành phố

CRUD tỉnh/TP kế thừa `SBBaseController` (override `ThemMoi`/`CapNhatThongTin`/`XoaAsync`). Thêm `TimTheoDieuKien` (search) và `GetTinhByQuocGia` (dropdown cascade theo Quốc gia). Mỗi method gắn key `TinhTP_*` cho HT-AD/HT-CSKH.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện | (load lưới) | POST `api/TinhTP/TimTheoDieuKien` | `TinhTP_TimTheoDieuKien` (HT-AD, HT-CSKH) | public | — | Lọc Tỉnh theo `DMTinhThanhFilterDto`. | EF | — |
| 2 | Thêm mới | Thêm | POST `api/TinhTP/ThemMoi` | `TinhTP_ThemMoi` (HT-AD, HT-CSKH) | public | — | Bắt buộc `IdQuocGia`; validate Mã/Tên không trống + không trùng trong cùng Quốc gia (GetCodeByRequest), trim rồi `base.ThemMoi`. | EF | — |
| 3 | Cập nhật | Sửa | POST `api/TinhTP/CapNhatThongTin` | `TinhTP_CapNhatThongTin` (HT-AD, HT-CSKH) | public | — | ModelState valid → map DTO→entity → kiểm tra tồn tại → `CapNhatThongTin`. | EF | Không re-check trùng Mã/Tên khi sửa (chỉ check lúc thêm) |
| 4 | Xóa | Xóa | POST `api/TinhTP/Xoa/{id:Guid}` | `TinhTP_Xoa` (HT-AD, HT-CSKH) | public | — | Kiểm tra tồn tại trước → `DeleteAsync`. | EF | Không chặn xóa khi còn Quận/Huyện con |
| 5 | Lấy Tỉnh theo Quốc gia | (dropdown cascade) | GET `api/TinhTP/GetTinhByQuocGia/{id:Guid}` | `TinhTP_GetTinhByQuocGia` (HT-AD, HT-CSKH) | public | — | Trả danh sách Tỉnh thuộc `IdQuocGia`. | EF | — |
| 6 | Tìm theo Id / Lấy tất cả | (kế thừa) | GET `api/TinhTP/TimTheoId/{id}`, `LayTatCa` | — (login) | public | — | Đọc kế thừa từ `SBBaseController`. | EF | — |

## Quy tắc nghiệp vụ (validate ThemMoi)
1. `IdQuocGia == null` → lỗi "Quốc gia không được bỏ trống" (chặn trước cả validate Mã/Tên).
2. Mã/Tên trống → lỗi "Mã, Tên không được bỏ trống".
3. `request.Id == null` (thêm mới) & (`GetCodeByRequest(Ma) > 0` || `GetCodeByRequest(Ten) > 0`) → lỗi "Mã, Tên không được phép trùng" — kiểm tra trùng **scope theo IdQuocGia**.

## Quan hệ
- **Gọi sang:** ITinhTPService (EF), AutoMapper.
- **Dữ liệu (entities):** `DMTinhThanh` (FK `IdQuocGia`).
- **Ripple / đồng bộ:** danh mục nền — sửa/xóa ảnh hưởng dropdown địa chỉ cascade Quốc gia→Tỉnh→Quận→Xã.

## Lỗi đã biết / lưu ý kỹ thuật
- `TinhTPController.cs:74-91` `CapNhatThongTin` không gọi `ValidateRequest`/check trùng → sửa Tỉnh có thể trùng Mã/Tên với Tỉnh khác cùng Quốc gia.
- Xóa không kiểm tra Quận/Huyện con (phụ thuộc FK ở DB), dễ để lại dữ liệu mồ côi nếu DB không ràng buộc.
