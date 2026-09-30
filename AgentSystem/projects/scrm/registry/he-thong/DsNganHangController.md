---
controller: DsNganHangController
module: he-thong
service: Core
man_hinh_ui: "Hệ thống > Danh mục > Danh sách ngân hàng (danh mục ngân hàng dùng chung)"
muc_dich: "Quản lý danh mục ngân hàng (DsNganHang): CRUD chuẩn kế thừa SBBaseController + tìm theo điều kiện (phân trang/lọc)."
alias: ["danh sách ngân hàng", "danh mục ngân hàng", "ngân hàng", "ds ngan hang"]
auth: internal
state_machine: []
deprecated: false
entities: [DsNganHang]
stored_procs: []
depends: ["IDsNganHangService", "SBBaseController<DsNganHang, DsNganHangDto>"]
cross_module_in: ["Frontend Hệ thống/Danh mục; các màn hình cần chọn ngân hàng (kế toán, thanh toán) tra cứu danh mục này"]
cross_module_out: ["danh mục dùng chung — module kế toán/thanh toán tham chiếu"]
---

# DsNganHangController — Danh mục ngân hàng

Controller danh mục ngân hàng, **kế thừa `SBBaseController<DsNganHang, DsNganHangDto>`** → có sẵn CRUD chuẩn (`[SBAuthorize]` ở base class-level). Route `api/DsNganHang/...`. Controller chỉ bổ sung 1 action riêng `TimTheoDieuKien` (tìm/lọc/phân trang).

> Lưu ý: controller này **không** khai báo `[TienIch(...)]` ở class-level và các action CRUD kế thừa từ base **không gắn permission key riêng** (base chỉ có `[SBAuthorize]` chung) → quyền theo cơ chế đăng nhập chung, không có key chi tiết.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | CRUD chuẩn (kế thừa SBBaseController) | Xem/Thêm/Sửa/Xóa | GET `api/DsNganHang/TimTheoId/{id:Guid}`, GET `api/DsNganHang/LayTatCa`, POST `api/DsNganHang/ThemMoi`, POST `api/DsNganHang/CapNhatThongTin`, POST `api/DsNganHang/Xoa/{id:Guid}` | — (chỉ `[SBAuthorize]` chung từ base, không key riêng) | internal | — | Tìm theo id / lấy tất cả / thêm / cập nhật (check tồn tại) / xóa ngân hàng — map `DsNganHang`↔`DsNganHangDto` | EF (base service) | CRUD không có permission key chi tiết |
| 2 | Tìm theo điều kiện | (tìm/lọc) | POST `api/DsNganHang/TimTheoDieuKien` (body `DsNganHangFilterDto`) | — (không key riêng) | internal (kế thừa `[SBAuthorize]`) | — | Trả danh sách ngân hàng theo filter (phân trang/lọc) | EF (service) | Không có `[SBAuthorize("...")]` key riêng cho action |

## Quan hệ
- **Gọi sang:** `IDsNganHangService` (CRUD qua base + `TimTheoDieuKienAsync`).
- **Dữ liệu (entities):** `DsNganHang` ↔ `DsNganHangDto`.
- **Ripple / đồng bộ:** danh mục dùng chung — module kế toán/thanh toán tham chiếu khi chọn ngân hàng.

## Lỗi đã biết / lưu ý kỹ thuật
- `DsNganHangController.cs:35` — action `TimTheoDieuKien` **không có `[SBAuthorize("key", ...)]` riêng** (chỉ dựa `[SBAuthorize]` chung kế thừa từ base) → khó phân quyền chi tiết; cũng không có `[TienIch]` class-level nên không sinh node tiện ích trong DongBoQuyen.
