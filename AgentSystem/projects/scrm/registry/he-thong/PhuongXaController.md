---
controller: PhuongXaController
module: he-thong
service: Core
man_hinh_ui: "Hệ thống > Danh mục địa giới > Phường/Xã: lưới phường xã + nút Thêm/Sửa/Xóa; cũng là nguồn dropdown địa chỉ cấp xã"
muc_dich: "CRUD danh mục Phường/Xã (lá cuối cây địa giới: Quốc gia → Tỉnh → Quận/Huyện → Phường/Xã), gắn theo Tỉnh/Thành + Quận/Huyện. Cung cấp dữ liệu địa chỉ cấp xã cho các màn nhập địa chỉ."
alias: ["phường xã", "phường/xã", "DMPhuongXa", "địa chỉ - phường xã", "PhuongXa"]
auth: mixed
state_machine: []
deprecated: false
entities: [DMPhuongXa]
stored_procs: []
depends: ["IPhuongXaService", "SBBaseController<DMPhuongXa, DMPhuongXaDto>", "AutoMapper"]
cross_module_in: ["FE Hệ thống/Phường xã gọi TimTheoDieuKien/ThemMoi/CapNhatThongTin/Xoa", "FE màn nhập địa chỉ lấy phường xã theo Quận/Huyện"]
cross_module_out: ["Là lá cây địa giới — mọi entity có địa chỉ (KH, đơn vị, đơn hàng) tham chiếu phường xã"]
---

# PhuongXaController — Danh mục Phường/Xã

CRUD phường xã kế thừa `SBBaseController<DMPhuongXa, DMPhuongXaDto>` (override `ThemMoi`/`CapNhatThongTin`/`XoaAsync`). Thêm `TimTheoDieuKien` (search). Controller-level `[SBAuthorize()]` (login) + `[TienIch(TI_HeThong)]`; từng method gắn key (HT-AD, HT-CSKH) → auth = mixed.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện | (load lưới) | POST `api/PhuongXa/TimTheoDieuKien` | `PhuongXa_TimTheoDieuKien` (HT-AD, HT-CSKH) | public | — | Lọc phường xã theo `DMPhuongXaFilterDto`. | EF | — |
| 2 | Thêm mới | Thêm | POST `api/PhuongXa/ThemMoi` | `PhuongXa_ThemMoi` (HT-AD, HT-CSKH) | public | — | Validate IdTinhThanh & IdQuanHuyen bắt buộc, Mã/Tên không trống & không trùng trong cùng quận/huyện, trim khoảng trắng → `base.ThemMoi`. | EF | — |
| 3 | Cập nhật | Sửa | POST `api/PhuongXa/CapNhatThongTin` | `PhuongXa_CapNhatThongTin` (HT-AD, HT-CSKH) | public | — | ModelState valid → map DTO→entity → kiểm tra tồn tại → `CapNhatThongTin`. | EF | — |
| 4 | Xóa | Xóa | POST `api/PhuongXa/Xoa/{id:Guid}` | `PhuongXa_Xoa` (HT-AD, HT-CSKH) | public | — | Kiểm tra tồn tại trước → `DeleteAsync`. | EF | Không chặn xóa khi còn địa chỉ tham chiếu (FK ở DB) |
| 5 | Tìm theo Id / Lấy tất cả | (kế thừa) | GET `api/PhuongXa/TimTheoId/{id}`, `LayTatCa` | — (login) | public | — | CRUD đọc kế thừa từ `SBBaseController`. | EF | — |

## Quan hệ
- **Gọi sang:** IPhuongXaService (EF), AutoMapper DMPhuongXaDto↔DMPhuongXa.
- **Dữ liệu (entities):** `DMPhuongXa` (gắn IdTinhThanh + IdQuanHuyen).
- **Ripple / đồng bộ:** Lá cây địa giới — sửa/xóa ảnh hưởng dropdown địa chỉ cấp xã toàn hệ thống.

## Lỗi đã biết / lưu ý kỹ thuật
- `ValidateRequest` (dòng 99–100) check trùng bằng cách so `GetCodeByRequest(Ma)` và `GetCodeByRequest(Ten)` cùng truyền `IdQuanHuyen`; chỉ chặn trùng khi `Id == null` (thêm mới) — khi cập nhật KHÔNG check trùng Mã/Tên.
- Xóa chỉ kiểm tra tồn tại, không kiểm tra ràng buộc địa chỉ con/đối tượng tham chiếu trước khi `DeleteAsync`.
