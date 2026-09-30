---
controller: PhieuXuatSanPhamController
module: kho
service: WarehouseProduct
man_hinh_ui: "nội bộ — không có UI (controller rỗng, endpoint TimTheoDieuKien bị comment). Nghiệp vụ phiếu xuất kho thực tế ở PhieuXuatController."
muc_dich: "STUB / scaffold: chỉ inject IPhieuXuatSanPhamService; KHÔNG có endpoint active (TimTheoDieuKien bị comment). Chưa phục vụ request nào."
alias: ["phiếu xuất sản phẩm", "phiếu xuất SP"]
auth: internal
state_machine: []
deprecated: false
entities: [PhieuXuatSanPham]
stored_procs: []
depends: [WarehouseProduct]
cross_module_in: []
cross_module_out: []
---

# PhieuXuatSanPhamController — Phiếu xuất sản phẩm (STUB, chưa có endpoint)

> **Controller rỗng** (`partial class`). Chỉ inject `IPhieuXuatSanPhamService`; endpoint `TimTheoDieuKien` bị comment (controller:26-31) → không endpoint active. Nghiệp vụ phiếu xuất kho thực tế nằm ở `PhieuXuatController` (node riêng).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| - | (không có) | - | - | - | - | - | Scaffold, chưa endpoint active | - | TimTheoDieuKien bị comment (controller:26-31) |

## Quan hệ
- **Gọi sang:** `IPhieuXuatSanPhamService` (inject, chưa dùng).
- **Dữ liệu (entities):** `PhieuXuatSanPham` (dự kiến).
- **Ripple / đồng bộ:** chưa có. Phiếu xuất kho chính: `PhieuXuatController`.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `WarehouseProduct/WarehouseProduct.API/Controllers/PhieuXuatSanPhamController.cs` (34 dòng) — stub, partial class, không endpoint. *Cần xác nhận* là controller bỏ dở. Có thể còn endpoint ở file partial khác — *cần xác nhận*.
