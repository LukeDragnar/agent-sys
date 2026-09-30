---
controller: DMPhuongThucThanhToanController
module: he-thong
service: Core
man_hinh_ui: "nội bộ — không có UI (service-to-service / dropdown nguồn phương thức thanh toán theo loại quỹ)"
muc_dich: "Trả về danh mục phương thức thanh toán (DMPhuongThucThanhToan) lọc theo mã loại quỹ (maLoaiQuy)."
alias: ["Phương thức thanh toán", "DM phương thức thanh toán", "loại quỹ", "DMPhuongThucThanhToan"]
auth: internal
state_machine: []
deprecated: false
entities: [DMPhuongThucThanhToan]
stored_procs: []
depends: ["IRepository<DMPhuongThucThanhToan> (EF, TableWithNolock)"]
cross_module_in: ["[internal] module Kế toán/Thu chi gọi GetDMPhuongThucThanhToan để dựng dropdown phương thức theo loại quỹ"]
cross_module_out: []
---

# DMPhuongThucThanhToanController — Danh mục phương thức thanh toán theo loại quỹ

Controller chỉ-đọc, một endpoint. Truy vấn `DMPhuongThucThanhToan` (EF, `TableWithNolock`) lọc theo `MaLoaiQuy`, project ra `DMPhuongThucThanhToanDto` (IdLoaiQuy, MaLoaiQuy, MaPhuongThuc, TenPhuongThuc, IdPhuongThucThanhToan). Kế thừa `BaseController` (không phải SBBaseController), không có CRUD chuẩn. Auth ở mức method bằng `[PrivateSBAuthorize]` → gọi nội bộ service-to-service.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy phương thức thanh toán theo loại quỹ | (không UI — nguồn dropdown) | GET `api/DMPhuongThucThanhToan/GetDMPhuongThucThanhToan?maLoaiQuy=` | — (`[PrivateSBAuthorize]`) | internal | — | Lọc `x.MaLoaiQuy == maLoaiQuy`, trả `List<DMPhuongThucThanhToanDto>` qua `Ok(data)`. Không lọc null/empty `maLoaiQuy` → truyền chuỗi rỗng sẽ trả phương thức có MaLoaiQuy rỗng (nếu có). | EF `TableWithNolock()` | Trả thẳng `Ok(data)` thay vì `ResponseResult` → khác convention envelope chung |

## Quan hệ
- **Gọi sang:** không (chỉ đọc DB qua repository).
- **Dữ liệu (entities):** `DMPhuongThucThanhToan` → DTO `DMPhuongThucThanhToanDto`.
- **Ripple / đồng bộ:** không.

## Lỗi đã biết / lưu ý kỹ thuật
- `DMPhuongThucThanhToanController.cs:50` — trả `Ok(data)` (không bọc `ResponseResult/MethodResult`) → response không cùng envelope với phần lớn controller khác, FE cần xử lý riêng.
- `DMPhuongThucThanhToanController.cs:35-38` — không kiểm tra `maLoaiQuy` null/empty trước khi filter.
- `[PrivateSBAuthorize]` đặt ở method (line 33) → endpoint chỉ dành cho gọi nội bộ, không expose ra UI public.
