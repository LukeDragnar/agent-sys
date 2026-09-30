---
controller: CommonController
module: sales
service: Switchboard
man_hinh_ui: "nội bộ — dropdown/lookup dùng chung cho UI tổng đài (danh sách tổng đài, info login partner, loại tổng đài đang dùng)"
muc_dich: "API dùng chung cho module tổng đài (Switchboard): danh sách loại tổng đài, lấy thông tin login partner theo user CRM, và loại tổng đài đang sử dụng của user. (Trùng tên CommonController của Contact → node này hậu tố _Switchboard)."
alias: ["common tổng đài", "danh sách tổng đài", "loại tổng đài đang dùng"]
auth: public
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends: [ICommonService, ITongDaiUserService]
cross_module_in: []
cross_module_out: []
---

# Common (Switchboard) — Lookup dùng chung tổng đài

> ⚠️ Trùng tên `CommonController` với bản module Contact (service: Contact). Node này = bản Switchboard (`Controllers\Common\CommonController.cs`) → hậu tố `_Switchboard`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Danh sách tổng đài | (dropdown) | GET `DanhSachTongDai` | `[SBAuthorize()]` (login) | public | màn cấu hình tổng đài | Trả danh sách loại tổng đài hệ thống hỗ trợ. | EF | — |
| 2 | Thông tin login partner theo user CRM | (load) | GET `GetInfoLoginPartnerByCrmUser` | `[SBAuthorize]` (login) | public | khi đăng nhập tổng đài | Trả thông tin login partner cho `CurrentUser`. | EF + partner | — |
| 3 | Loại tổng đài đang sử dụng | (load) | GET `GetLoaiTongDaiDangSuDung` | `[SBAuthorize]` (login) | public | mở màn tổng đài | Trả loại tổng đài đơn vị/user đang dùng. | EF | — |

## Quan hệ
- **Gọi sang:** `ICommonService` (danh sách tổng đài), `ITongDaiUserService` (info login partner, loại tổng đài đang dùng).
- **Dữ liệu (entities):** lookup tổng đài (không có entity ghi riêng).
- **Ripple / đồng bộ:** không.

## Lỗi đã biết / lưu ý kỹ thuật
- Controller không có `[TienIch]` attribute; chỉ `[SBAuthorize]` per-action (login). Route `api/Common`.
