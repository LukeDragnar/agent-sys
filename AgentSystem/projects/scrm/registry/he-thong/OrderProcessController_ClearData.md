---
controller: OrderProcessController
module: he-thong
service: ClearData
man_hinh_ui: "nội bộ — tra cứu OrderProcess (SandBox/QuanTri), không phải màn user thường"
muc_dich: "Tra cứu OrderProcess theo điều kiện trong ClearData service (qua IOrderRollBackService)."
alias: ["order process", "tra cứu order process"]
auth: internal
state_machine: []
deprecated: false
entities: [OrderProcess]
stored_procs: []
depends: [IOrderRollBackService]
cross_module_in: ["FE/công cụ nội bộ gọi TimTheoDieuKien"]
cross_module_out: []
---

# OrderProcessController (ClearData) — Tra cứu OrderProcess

TienIch: `DMDonHangController` "Quản lý danh mục đơn hàng" (TI_Chung, ẩn=true — mô tả copy từ DM đơn hàng). Route `api/SanboxArea/OrderProcess`. Namespace `OrderProcess.API.Areas.SandBoxArea`. 1 action có key → internal.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện | Tìm | POST `api/SanboxArea/OrderProcess/TimTheoDieuKien` | OrderProcess_TimTheoDieuKien | internal | — | Lọc OrderProcess theo TimOrderProcessDto | EF | — |

## Quan hệ
- **Gọi sang:** `IOrderRollBackService.TimTheoDieuKien`.
- **Dữ liệu (entities):** `OrderProcess`.
- **Ripple / đồng bộ:** không.

## Lỗi đã biết / lưu ý kỹ thuật
- `OrderProcessController.cs:13` — `[TienIch]` đặt tên/mô tả là `DMDonHangController` "Quản lý danh mục đơn hàng" (copy-paste sai, không khớp OrderProcess).
- `OrderProcessController.cs:14` — route segment viết `SanboxArea` (thiếu chữ "d" so với "SandBoxArea") → khác convention các controller khác.
- Inject `IOrderRollBackService` (không phải IOrderProcessService) → controller OrderProcess thực chất gọi service RollBack.
