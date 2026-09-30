---
controller: QuocGiaController
module: he-thong
service: Core
man_hinh_ui: "Hệ thống > Danh mục địa giới > Quốc gia: lưới quốc gia + nút Thêm/Sửa/Xóa; cũng là nguồn dropdown địa chỉ"
muc_dich: "CRUD danh mục Quốc gia (gốc cây địa giới: Quốc gia → Tỉnh → Quận/Huyện → Phường/Xã) + cung cấp dữ liệu địa chỉ tổng hợp cho các màn nhập địa chỉ."
alias: ["QuocGia", "quốc gia", "danh mục quốc gia", "địa chỉ - quốc gia"]
auth: mixed
state_machine: []
deprecated: false
entities: [DMQuocGia]
stored_procs: []
depends: ["IQuocGiaService", "SBBaseController<DMQuocGia, DMQuocGiaDto>"]
cross_module_in: ["FE Hệ thống/Quốc gia gọi TimTheoDieuKien/ThemMoi/CapNhatThongTin/Xoa", "FE màn nhập địa chỉ gọi GetThongTinDiaChi", "Service nội bộ gọi GetThongTinDiaChi_Private (PrivateSBAuthorize) để build cây địa chỉ"]
cross_module_out: ["Là gốc cây địa giới — TinhTP/QuanHuyen/PhuongXa tham chiếu xuống; mọi entity có địa chỉ (KH, đơn vị, đơn hàng) dùng dữ liệu này"]
---

# QuocGiaController — Danh mục Quốc gia

CRUD quốc gia kế thừa `SBBaseController` (override `ThemMoi`/`CapNhatThongTin`/`XoaAsync`). Thêm `TimTheoDieuKien` (search) và 2 endpoint `GetThongTinDiaChi` (public + private) trả cây địa chỉ tổng hợp. Controller-level KHÔNG có `[SBAuthorize]` (attribute bị comment dòng 16), nhưng từng method gắn key riêng → auth = mixed.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện | (load lưới) | POST `api/QuocGia/TimTheoDieuKien` | `QuocGia_TimTheoDieuKien` (HT-AD, HT-CSKH) | public | — | Lọc danh sách quốc gia theo `DMQuocGiaFilterDto`. | EF | — |
| 2 | Thêm mới | Thêm | POST `api/QuocGia/ThemMoi` | `QuocGia_ThemMoi` (HT-AD, HT-CSKH) | public | — | Validate Mã/Tên không trống + không trùng (GetCodeByRequest), trim khoảng trắng thừa rồi `base.ThemMoi`. | EF | Điều kiện validate trống dùng `Ma && Ma` (lặp `Ma`, bỏ sót `Ten`) — dòng 88: "Mã hoặc Tên" thực tế chỉ check Ma |
| 3 | Cập nhật | Sửa | POST `api/QuocGia/CapNhatThongTin` | `QuocGia_CapNhatThongTin` (HT-AD, HT-CSKH) | public | — | ModelState valid → map DTO→entity → kiểm tra tồn tại (GetById) → `CapNhatThongTin`. | EF | — |
| 4 | Xóa | Xóa | POST `api/QuocGia/Xoa/{id:Guid}` | `QuocGia_Xoa` (HT-AD, HT-CSKH) | public | — | Kiểm tra tồn tại trước → `DeleteAsync`. | EF | Không chặn xóa khi còn Tỉnh/TP con tham chiếu (FK ràng buộc ở DB) |
| 5 | Lấy thông tin địa chỉ | (load form địa chỉ) | POST `api/QuocGia/GetThongTinDiaChi` | — (login) | public | — | Trả cây/danh sách địa chỉ theo `RequestThongTin`. | EF | — |
| 6 | Tìm theo Id / Lấy tất cả | (kế thừa) | GET `api/QuocGia/TimTheoId/{id}`, `LayTatCa` | — (login) | public | — | CRUD đọc kế thừa từ `SBBaseController` (`[SBAuthorize]` rỗng). | EF | — |

## Endpoint nội bộ (private / service-to-service)
- [internal] **POST `GetThongTinDiaChi_Private`** `[PrivateSBAuthorize]` — service khác (đơn hàng/KH/đơn vị) gọi để build cây địa chỉ Quốc gia→Tỉnh→Quận→Xã không cần token user.

## Quan hệ
- **Gọi sang:** IQuocGiaService (EF), AutoMapper DMQuocGiaDto↔DMQuocGia.
- **Dữ liệu (entities):** `DMQuocGia` (gốc cây địa giới).
- **Ripple / đồng bộ:** không bắn event; là danh mục nền — sửa/xóa ảnh hưởng dropdown địa chỉ toàn hệ thống.

## Lỗi đã biết / lưu ý kỹ thuật
- `QuocGiaController.cs:88` `ValidateRequest` điều kiện `IsNullOrWhiteSpace(request.Ma) && IsNullOrWhiteSpace(request.Ma)` — lặp `Ma`, KHÔNG check `Ten` → tạo được quốc gia chỉ có Tên (thiếu Mã) mà không báo lỗi như mong đợi.
- Controller-level `[SBAuthorize("QuocGiaController")]` bị comment (dòng 16) → bảo vệ dựa hoàn toàn vào key từng method; method kế thừa (`LayTatCa`, `TimTheoId`) chỉ còn `[SBAuthorize]` rỗng.
