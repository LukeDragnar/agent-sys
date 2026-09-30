---
controller: InPhieuController
module: san-xuat
service: SanXuat
man_hinh_ui: "Sản xuất > In phiếu sản xuất — in/tạo phiếu nhập & phiếu xuất cho lệnh sản xuất (cùng nghiệp vụ WorkOrder, tách route riêng cho thao tác in)"
muc_dich: "Tạo phiếu nhập / phiếu xuất sản xuất từ một Work Order request. Hai endpoint InPhieuNhapSX/InPhieuXuatSX đều ủy quyền cho IWorkOrderService.InsertAsyncV2 (cùng logic tách/tạo WO của WorkOrderController.ThemMoi)."
alias: ["in phiếu sản xuất", "in phiếu nhập sản xuất", "in phiếu xuất sản xuất", "InPhieu", "phiếu nhập SX", "phiếu xuất SX"]
auth: public                                   # controller [SBAuthorize]; 2 endpoint có key riêng
state_machine: []
deprecated: false
entities: [WorkOrder, WorkOrderStep, WorkOrderStepProduct]
stored_procs: []
depends: [SanXuat, IWorkOrderService, IWarehouseProductService (HTTP → Warehouse/Kho)]
cross_module_in: []
cross_module_out: [WarehouseProduct/Kho (gián tiếp qua InsertAsyncV2 → PrivateGetSanPhamByIds)]
---

# InPhieuController — In phiếu nhập/xuất sản xuất

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | In phiếu nhập SX | In phiếu nhập sản xuất | POST `api/InPhieu/InPhieuNhapSX` | `InPhieu_PhieuNhapSX` (AD-TONG/AD-CN/SANXUAT) | public | màn in phiếu | ModelState không hợp lệ → trả lỗi validate; gọi `IWorkOrderService.InsertAsyncV2(req, CurrentUser)` — cùng logic tách/tạo WO của WorkOrderController.ThemMoi | EF + HTTP Warehouse | InPhieuController.cs:34 |
| 2 | In phiếu xuất SX | In phiếu xuất sản xuất | POST `api/InPhieu/InPhieuXuatSX` | `InPhieu_PhieuXuatSX` (AD-TONG/AD-CN/SANXUAT) | public | màn in phiếu | Giống #1 — cũng gọi `InsertAsyncV2(req, CurrentUser)` (KHÔNG phân biệt nhập/xuất ở tầng service) | EF + HTTP Warehouse | InPhieuController.cs:51 |

## Quan hệ
- **Gọi sang:** `IWorkOrderService.InsertAsyncV2` (xem `WorkOrderController` để biết hành vi tách WO + gọi Warehouse `PrivateGetSanPhamByIds`).
- **Dữ liệu (entities):** WorkOrder / WorkOrderStep / WorkOrderStepProduct (qua service dùng chung).
- **Ripple / đồng bộ:** không trực tiếp; ripple kho phát sinh ở tầng WorkOrderService khi tạo phiếu theo bước.

## Lỗi đã biết / lưu ý kỹ thuật
- Cả 2 endpoint gọi **cùng** `InsertAsyncV2` (InPhieuController.cs:40, 57) — tên endpoint gợi ý nhập/xuất khác nhau nhưng tầng service không tách nhánh nhập/xuất ở đây. Cần xác nhận đúng ý đồ (có thể nhãn UI gây hiểu nhầm).
- Controller dùng chung `IWorkOrderService` với WorkOrderController → mọi lưu ý của WorkOrder (không hoàn phiếu khi xóa, v.v.) áp dụng gián tiếp.
