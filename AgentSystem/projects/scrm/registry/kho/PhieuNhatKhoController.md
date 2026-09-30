---
controller: PhieuNhatKhoController
module: kho
service: WarehouseProduct
man_hinh_ui: "Kho > Quản lý phiếu nhặt kho — list phiếu nhặt (tìm theo điều kiện), cập nhật thông tin, xem chi tiết, in phiếu nhặt."
muc_dich: "Quản lý phiếu nhặt kho (picking): tìm/cập nhật, lấy chi tiết nhiều phiếu, in phiếu nhặt. Hỗ trợ luồng nhặt hàng (soạn hàng) trong kho."
alias: ["phiếu nhặt kho", "phiếu nhặt", "picking", "in phiếu nhặt", "soạn hàng"]
auth: public               # toàn bộ [SBAuthorize] rỗng (chỉ đăng nhập, không gắn permission key)
state_machine: []          # cần xác nhận: phiếu nhặt có trạng thái chia/hoàn thành không (ChiaPhieuNhat bị comment)
deprecated: false
entities: [PhieuNhatKho]
stored_procs: []
depends: [WarehouseProduct]
cross_module_in: []
cross_module_out: []
---

# PhieuNhatKhoController — Quản lý phiếu nhặt kho

> Controller picking. **Toàn bộ endpoint dùng `[SBAuthorize]` rỗng** (chỉ cần đăng nhập, KHÔNG gắn permission key) → không phân quyền chi tiết. 2 endpoint bị comment (`ChiaPhieuNhat`, `ChiTietPhieuNhat`).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện (grid) | "Tìm kiếm" | POST `api/PhieuNhatKho/TimTheoDieuKien` | `[SBAuthorize]` (rỗng) | public | màn list | List phiếu nhặt theo `PhieuNhatKhoFilterDto` + CurrentUser | EF | không có permission key |
| 2 | Cập nhật thông tin | "Lưu" | POST `api/PhieuNhatKho/CapNhatThongTin` | `[SBAuthorize]` (rỗng) | public | màn edit | Update phiếu nhặt theo entity `PhieuNhatKho` | EF | nhận thẳng entity (không qua DTO) |
| 3 | Lấy chi tiết nhiều phiếu | (xem/in nhiều phiếu) | POST `api/PhieuNhatKho/GetDsDetailPhieuNhat` | `[SBAuthorize]` (rỗng) | public | - | Trả chi tiết danh sách phiếu theo `List<Guid> ids` | EF | - |
| 4 | In phiếu nhặt | "In" | POST `api/PhieuNhatKho/InPhieuNhat` | `[SBAuthorize]` (rỗng) | public | - | Trả dữ liệu in theo `InPhieuNhatRequestDto` | EF | - |

## Quan hệ
- **Gọi sang:** `IPhieuNhatKhoService` (toàn bộ).
- **Dữ liệu (entities):** `PhieuNhatKho`.
- **Ripple / đồng bộ:** phiếu nhặt phục vụ soạn hàng cho đơn — *cần xác nhận* liên kết với đơn hàng/phiếu xuất.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `WarehouseProduct/WarehouseProduct.API/Controllers/PhieuNhatKhoController.cs` (65 dòng).
  - Toàn bộ action `[SBAuthorize]` rỗng — không gắn permission key, chỉ chặn theo đăng nhập. *Cần xác nhận* có chủ ý (thiếu phân quyền chi tiết).
  - L24-29 `ChiaPhieuNhat` (gọi `ChiaPhieuNhat_Ver1`) và L44-49 `ChiTietPhieuNhat` bị comment — chức năng chia phiếu nhặt hiện tắt; nếu cần bật lại phải uncomment + kiểm tra service.
  - `CapNhatThongTin` nhận trực tiếp entity `PhieuNhatKho` (không qua DTO) — lưu ý over-posting.
