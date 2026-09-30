---
controller: DuyController
module: he-thong
service: WarehouseProduct
man_hinh_ui: "nội bộ — không có UI (endpoint demo/debug của dev)"
muc_dich: "Controller demo/debug (đặt theo tên dev): dò sản phẩm chi nhánh marketing bị trùng tên (sau normalize) và trả về danh sách id trùng."
alias: ["Duy demo", "Demo1", "debug warehouse product"]
auth: internal
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends: ["ICommonService (WarehouseProduct)", "IClientHelper", "ServiceSettings"]
cross_module_in: []
cross_module_out: []
---

# DuyController — endpoint demo/debug (WarehouseProduct)

Controller tạm của dev (1 action `Demo1`), không gắn vào màn hình nào. Gọi `DropdownAllSanPhamChiNhanhMarketing`, group sản phẩm theo tên đã normalize, lọc nhóm có count > 1 (trùng tên) và trả về danh sách id trùng. Dùng để kiểm tra dữ liệu, không phải nghiệp vụ chính thức.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Demo dò trùng tên SP | (không có UI) | GET `api/Duy/Demo1` | (SBAuthorize, không key) | public | — | Lấy dropdown SP chi nhánh marketing, group theo tên normalize, lọc nhóm trùng (count>1), trả list id. | EF (qua ICommonService) | Endpoint debug, nên gỡ trước khi production |

## Lỗi đã biết / lưu ý kỹ thuật
- Controller đặt theo tên dev, chỉ có endpoint `Demo1` mang tính debug: `WarehouseProduct/WarehouseProduct.API/Controllers/DuyController.cs:40-77`.
