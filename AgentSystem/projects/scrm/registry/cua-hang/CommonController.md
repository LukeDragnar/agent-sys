---
controller: CommonController
module: cua-hang
service: PreOrder
man_hinh_ui: "nội bộ — không có UI (controller rỗng, chỉ inject ICommonService, chưa có action)"
muc_dich: "Placeholder/scaffold cho các tiện ích chung của service PreOrder; hiện chưa có endpoint nào."
alias: ["common preorder", "tiện ích chung preorder"]
auth: public
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends: [PreOrder (ICommonService)]
cross_module_in: []
cross_module_out: []
---

# CommonController — Tiện ích chung PreOrder (rỗng)

## Hành động (UI / công khai)
_Không có action._ Controller chỉ khai báo `[Route("api/[controller]")]` + inject `ICommonService` trong constructor, **chưa định nghĩa endpoint nào** (không method có `[HttpGet]`/`[HttpPost]`).

## Quan hệ
- **Gọi sang:** inject `ICommonService` (chưa dùng).
- **Dữ liệu (entities):** không.
- **Ripple / đồng bộ:** không.

## Lỗi đã biết / lưu ý kỹ thuật
- **Controller rỗng (scaffold)** — không `[TienIch]`, không action; chỉ là khung. Không cung cấp API thật; có thể bỏ qua khi triage cho tới khi có endpoint.
- Code thật: BE `PreOrder/PreOrder.API/Controllers/Common/CommonController.cs`.
