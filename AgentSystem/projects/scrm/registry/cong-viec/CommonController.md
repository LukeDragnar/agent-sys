---
controller: CommonController
module: cong-viec
service: ChiaSoChat
man_hinh_ui: "nội bộ — cung cấp dropdown dùng chung cho màn cấu hình chia chat (cách chia, cách chia kiểu KM)."
muc_dich: "Cung cấp dữ liệu dropdown dùng chung cho UI chia chat: danh sách 'cách chia chat' và 'cách chia chat kiểu khuyến mãi (KM)'."
alias: ["Common ChiaSoChat", "dropdown cách chia chat", "DropdownKM CachChiaChat", "common chia chat"]
auth: public
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends:
  - ICommonService (ChiaSoChat.Service.Generated.Common)
cross_module_in: []
cross_module_out: []
---

# Common — Dropdown dùng chung ChiaSoChat

> Controller tiện ích: 2 endpoint trả dropdown cho màn cấu hình chia chat — `Dropdown_CachChiaChat` (danh sách cách chia) và `DropdownKM_CachChiaChat` (cách chia kiểu KM). Cả hai `[SBAuthorize]` trống, không tham số. Không có route đặc biệt (route mặc định `api/Common`).

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Dropdown cách chia chat | Combo "Cách chia" | `GET api/Common/Dropdown_CachChiaChat` | `[SBAuthorize]` | public | màn cấu hình chia | Trả list cách chia chat. | EF/enum | — |
| 2 | Dropdown cách chia (KM) | Combo "Cách chia (KM)" | `GET api/Common/DropdownKM_CachChiaChat` | `[SBAuthorize]` | public | màn cấu hình chia (KM) | Trả list cách chia kiểu khuyến mãi. | EF/enum | — |

## Quan hệ
- **Gọi sang:** `ICommonService` (ChiaSoChat).
- **Dữ liệu (entities):** —.
- **Ripple/đồng bộ:** —.

## Lỗi đã biết / lưu ý kỹ thuật
- Route mặc định `api/Common` — trùng tên controller Common của các service khác; phân biệt ở tầng routing theo service deploy riêng (ConfigChiaSoChat.API).
