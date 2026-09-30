---
controller: DmCongDoanController
module: san-xuat
service: SanXuat
man_hinh_ui: "Sản xuất > Quản lý danh mục > Công đoạn (DmCongDoan) — master data khai báo các công đoạn sản xuất (Ma, Ten) dùng để dựng quy trình BOM"
muc_dich: "CRUD danh mục Công đoạn sản xuất (DmCongDoan: Ma, Ten) — master data phụ trợ cho việc khai báo quy trình BOM (BomStep)."
alias: ["công đoạn", "danh mục công đoạn", "DmCongDoan", "công đoạn sản xuất", "process step master"]
auth: public                                   # controller [ApiController]; toàn bộ permission key bị comment → chỉ cần đăng nhập
state_machine: []
deprecated: false
entities: [DmCongDoan]
stored_procs: []
depends: [SanXuat]
cross_module_in: [BomSetupController (BomStep tham chiếu công đoạn theo Ten/Ma — liên kết lỏng)]
cross_module_out: []
---

# DmCongDoanController — Danh mục Công đoạn sản xuất

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Thêm công đoạn | Thêm mới | POST `api/DmCongDoan/ThemMoi` | key comment (`DmCongDoan_ThemMoi`) | public | - | ModelState lỗi → validate; map DTO→entity; InsertAsync | EF | key bị comment |
| 2 | Sửa công đoạn | Lưu | POST `api/DmCongDoan/CapNhatThongTin` | key comment | public | tồn tại | Map DTO→entity; GetById null → "Không tìm thấy"; UpdateAsync | EF | DmCongDoanService.cs:31 |
| 3 | Xóa công đoạn | Xóa | POST `api/DmCongDoan/Xoa/{id}` | key comment | public | - | GetById null → "Không tìm thấy"; DeleteAsync → true | EF | key bị comment |
| 4 | Xem chi tiết | mở công đoạn | GET `api/DmCongDoan/TimTheoId/{id}` | key comment | public | - | Trả 1 công đoạn theo id | EF | - |
| 5 | Tìm theo điều kiện | grid lọc | POST `api/DmCongDoan/TimTheoDieuKien` | (không key) | public | màn list | Phân trang công đoạn theo filter | EF | DmCongDoanService.cs:41 |

## Quan hệ
- **Gọi sang:** không (CRUD thuần).
- **Dữ liệu (entities):** `DmCongDoan {Ma, Ten}`.
- **Ripple / đồng bộ:** không. Được dùng làm tham chiếu khi dựng quy trình BOM (BomStep) — liên kết lỏng theo Ten/Ma, không FK cứng.

## Lỗi đã biết / lưu ý kỹ thuật
- Toàn bộ permission key bị comment (controller L31/43/61/87) → chỉ cần đăng nhập là thao tác được.
- Không có tenant guard ở controller → kiểm tra ràng buộc đơn vị nếu cần ở tầng service.
