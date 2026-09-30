---
controller: CommonController
module: marketing
service: PartnerConnect
man_hinh_ui: "nội bộ — controller chung của service PartnerConnect; hiện CHƯA có action nào (placeholder)."
muc_dich: "Controller Common của PartnerConnect — chỗ chứa các API dùng chung (dropdown/tiện ích) cho service; hiện rỗng, chưa khai báo action."
alias: ["Common PartnerConnect", "common partner connect"]
auth: public
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends:
  - ICommonService (PartnerConnect.Services.Generated.Common)
cross_module_in: []
cross_module_out: []
---

# Common — Tiện ích chung PartnerConnect (placeholder, chưa có action)

> Controller-level `[SBAuthorize]` + `TienIch("Common_PartnerConnect_Controller", ..., TI_Chung)`. Inject `ICommonService` + `IConfiguration` nhưng **thân class rỗng — chưa có endpoint nào**. Node giữ chỗ trong graph; cập nhật khi thêm action.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| — | (chưa có action) | — | `api/Common/*` (route đăng ký, 0 endpoint) | `[SBAuthorize]` controller-level | public | — | Không expose endpoint nào. | — | Controller rỗng (CommonController.cs:25) — chỉ inject service, chưa dùng. |

## Quan hệ
- **Gọi sang:** `ICommonService` (đã inject, chưa gọi).
- **Dữ liệu (entities):** —.
- **Ripple/đồng bộ:** —.

## Lỗi đã biết / lưu ý kỹ thuật
- `Common/CommonController.cs` — class không có action; `ICommonService` + `IConfiguration` inject nhưng không sử dụng (dead injection).
