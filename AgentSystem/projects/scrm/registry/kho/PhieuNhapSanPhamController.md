---
controller: PhieuNhapSanPhamController
module: kho
service: WarehouseProduct
man_hinh_ui: "nội bộ — không có UI (controller rỗng, endpoint TimTheoDieuKien bị comment). Nghiệp vụ phiếu nhập kho thực tế ở PhieuNhapController."
muc_dich: "STUB / scaffold: chỉ inject IPhieuNhapSanPhamService; KHÔNG có endpoint active (TimTheoDieuKien bị comment). Chưa phục vụ request nào."
alias: ["phiếu nhập sản phẩm", "phiếu nhập SP"]
auth: internal
state_machine: []
deprecated: false          # stub chưa implement, không phải bản cũ khóa cứng
entities: [PhieuNhapSanPham]
stored_procs: []
depends: [WarehouseProduct]
cross_module_in: []
cross_module_out: []
---

# PhieuNhapSanPhamController — Phiếu nhập sản phẩm (STUB, chưa có endpoint)

> **Controller rỗng** (`partial class`). Chỉ inject `IPhieuNhapSanPhamService`; endpoint `TimTheoDieuKien` bị comment toàn bộ (controller:26-31) → không endpoint active. Nghiệp vụ phiếu nhập kho thực tế nằm ở `PhieuNhapController` (node riêng).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| - | (không có) | - | - | - | - | - | Scaffold, chưa endpoint active | - | TimTheoDieuKien bị comment (controller:26-31) |

## Quan hệ
- **Gọi sang:** `IPhieuNhapSanPhamService` (inject, chưa dùng).
- **Dữ liệu (entities):** `PhieuNhapSanPham` (dự kiến).
- **Ripple / đồng bộ:** chưa có. Phiếu nhập kho chính: `PhieuNhapController`.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `WarehouseProduct/WarehouseProduct.API/Controllers/PhieuNhapSanPhamController.cs` (34 dòng) — stub, partial class, không endpoint. *Cần xác nhận* là controller bỏ dở / chờ phát triển. Có thể còn endpoint ở file partial khác — *cần xác nhận*.
