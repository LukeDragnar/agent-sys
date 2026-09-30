---
controller: MyHubHotlineController
module: sales
service: Switchboard
man_hinh_ui: "Tổng đài MyHub (sim số) > Hotline (danh sách hotline user, hotline theo user partner, xóa hotline)"
muc_dich: "Quản lý hotline tổng đài MyHub (sim số): lấy danh sách hotline của user đăng nhập, hotline theo id user partner (auto-scope đơn vị), và xóa hotline."
alias: ["hotline MyHub", "hotline sim số", "xóa hotline MyHub"]
auth: public
state_machine: []
deprecated: false
entities: [Hotline]
stored_procs: []
depends: [IHotlineService]
cross_module_in: []
cross_module_out: []
---

# MyHubHotline — Hotline tổng đài MyHub (sim số)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy danh sách hotline user đăng nhập | (dropdown) | GET `GetHotline` | `[SBAuthorize]` (login) | public | mở màn gọi | Trả danh sách hotline của user đăng nhập. | EF | Key `MyHubHotline_GetHotline` đã comment → còn `[SBAuthorize]` rỗng |
| 2 | Hotline theo id user partner | (load) | POST `GetHotlineByIdUserPartner` | `MyHubHotline_GetHotlineByIdUserPartner` (nhiều role DV) | public | màn quản lý hotline | Nếu **không** super/kế-toán → ép `IdDonVi=DonViId`; trả hotline theo user partner. | EF | — |
| 3 | Xóa hotline | Xóa | POST `DeleteHotline/{idDonVi}/{id}` | `[SBAuthorize(true)]` | public | dòng hotline | Nếu không super/CSKH/kế-toán → ép `idDonVi=DonViId`; xóa hotline. | EF | Key `MyHubHotline_DeleteHotline` đã comment → dùng `[SBAuthorize(true)]` (cờ true, không key) |

## Quan hệ
- **Gọi sang:** `IHotlineService` (get/xóa hotline).
- **Dữ liệu (entities):** hotline tổng đài MyHub.
- **Ripple / đồng bộ:** không.

## Lỗi đã biết / lưu ý kỹ thuật
- `MyHubHotlineController.cs:25,48` — `GetHotline` và `DeleteHotline` key role đã comment; `DeleteHotline` dùng `[SBAuthorize(true)]` (overload cờ, không permission key) → cần xem lại phân quyền xóa hotline.
- `GetHotlineByIdUserPartner` + `DeleteHotline` auto-scope `idDonVi` theo user nếu không super/kế-toán.
