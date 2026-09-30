---
controller: LogProcessController
module: he-thong
service: WarehouseProduct
man_hinh_ui: "Sàn TMĐT > nhật ký xử lý đồng bộ (log process): xem danh sách/chi tiết log đồng bộ theo kết nối đơn vị; nút Hủy log đang xử lý"
muc_dich: "Tra cứu nhật ký quá trình xử lý đồng bộ Ecommerce (theo unit-connect) và hủy một tiến trình log đang chạy."
alias: ["LogProcess", "nhật ký đồng bộ sàn", "log process ecommerce", "CRM_GetListLogProcess"]
auth: public
state_machine: []
deprecated: false
entities: [LogProcess]
stored_procs: []
depends: ["ILogProcessService (WarehouseProduct.Service.Ecommerce)", "IUserPrincipalService (CurrentUser)"]
cross_module_in: ["FE Sàn TMĐT/nhật ký đồng bộ gọi list/detail/cancel"]
cross_module_out: []
---

# LogProcessController — Nhật ký xử lý đồng bộ Ecommerce (WarehouseProduct)

Đọc log quá trình đồng bộ giữa SCRM và sàn TMĐT theo kết nối đơn vị (unit-connect): danh sách, chi tiết, lấy theo unit-connect id; và hủy một tiến trình log đang xử lý. Route `api/LogProcess`, controller-level `[SBAuthorize]` (mọi action đều yêu cầu đăng nhập, không gắn permission key riêng).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy log theo unit-connect | — | POST `api/LogProcess/CRM_GetLogProcessByUnitConnectId` | (SBAuthorize, không key) | public | — | Trả log process theo unit-connect id của CurrentUser. | EF (ILogProcessService) | — |
| 2 | Danh sách log | List/lọc | POST `api/LogProcess/CRM_GetListLogProcess` | (SBAuthorize, không key) | public | — | Trả danh sách log process theo filter + CurrentUser. | EF | — |
| 3 | Chi tiết log | Mở | POST `api/LogProcess/CRM_GetLogProcessDetail` | (SBAuthorize, không key) | public | — | Trả chi tiết 1 log process. | EF | — |
| 4 | Hủy log | Hủy | POST `api/LogProcess/CRM_CancelLogProcess` | (SBAuthorize, không key) | public | log đang xử lý | Hủy tiến trình log process theo request. | EF | — |

## Quan hệ
- **Gọi sang:** `ILogProcessService` (WarehouseProduct.Service.Generated.Ecommerce).
- **Dữ liệu (entities):** log process đồng bộ Ecommerce theo unit-connect.

## Lỗi đã biết / lưu ý kỹ thuật
- Tất cả action `[SBAuthorize()]` không key → chỉ chặn theo đăng nhập, không phân quyền chi tiết: `WarehouseProduct/WarehouseProduct.API/Controllers/Ecommerce/LogProcessController.cs:13-78`.
