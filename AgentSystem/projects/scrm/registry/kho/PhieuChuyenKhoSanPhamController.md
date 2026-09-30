---
controller: PhieuChuyenKhoSanPhamController
module: kho
service: WarehouseProduct
man_hinh_ui: "nội bộ — không có UI (controller rỗng, endpoint TimTheoDieuKien bị comment). Nghiệp vụ chuyển kho thực tế ở PhieuChuyenKhoController."
muc_dich: "STUB / scaffold: chỉ inject IPhieuChuyenKhoSanPhamService; KHÔNG có endpoint active (TimTheoDieuKien bị comment). Chưa phục vụ request nào."
alias: ["phiếu chuyển kho sản phẩm", "phiếu chuyển kho SP"]
auth: internal
state_machine: []
deprecated: false
entities: [PhieuChuyenKhoSanPham]
stored_procs: []
depends: [WarehouseProduct]
cross_module_in: []
cross_module_out: []
---

# PhieuChuyenKhoSanPhamController — Phiếu chuyển kho sản phẩm (STUB, chưa có endpoint)

> **Controller rỗng** (`partial class`). Chỉ inject `IPhieuChuyenKhoSanPhamService`; endpoint `TimTheoDieuKien` bị comment (controller:26-31) → không endpoint active. Nghiệp vụ chuyển kho thực tế nằm ở `PhieuChuyenKhoController` (node riêng).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| - | (không có) | - | - | - | - | - | Scaffold, chưa endpoint active | - | TimTheoDieuKien bị comment (controller:26-31) |

## Quan hệ
- **Gọi sang:** `IPhieuChuyenKhoSanPhamService` (inject, chưa dùng).
- **Dữ liệu (entities):** `PhieuChuyenKhoSanPham` (dự kiến).
- **Ripple / đồng bộ:** chưa có. Chuyển kho chính: `PhieuChuyenKhoController`.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `WarehouseProduct/WarehouseProduct.API/Controllers/PhieuChuyenKhoSanPhamController.cs` (34 dòng) — stub, partial class, không endpoint. *Cần xác nhận* là controller bỏ dở. Có thể còn endpoint ở file partial khác — *cần xác nhận*.
