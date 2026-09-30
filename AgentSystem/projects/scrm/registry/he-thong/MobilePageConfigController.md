---
controller: MobilePageConfigController
module: he-thong
service: AuthenCustomer
man_hinh_ui: "Không có UI — controller đã bị comment toàn bộ (không build, không route)."
muc_dich: "(Đã vô hiệu) Cấu hình các trang/màn của app mobile khách (GetPagesConfigFilter / GetAllPagesConfig / Save). Hiện chức năng cấu hình app nằm ở CauHinhController (MobilePageJson)."
alias: ["cấu hình page mobile", "mobile page config", "ẩn hiện page app"]
auth: internal
state_machine: []
deprecated: true
entities: [MobilePageConfig]
stored_procs: []
depends: []
cross_module_in: []
cross_module_out: []
---

# MobilePageConfigController — (ĐÃ VÔ HIỆU / comment toàn bộ)

**Toàn bộ file `MobilePageConfigController.cs` bị comment** (mọi dòng prefix `//`) → controller KHÔNG được compile, KHÔNG đăng ký route, KHÔNG có endpoint sống. Giữ lại như bản nháp/lịch sử. `deprecated: true`.

Trong code (đã comment) controller định kế thừa `ApiController<MobilePageConfig, MobilePageConfigDto>` với 3 method dự kiến:
- `[PrivateSBAuthorize] POST GetPagesConfigFilter/{idDonVi}` (lọc page config) — service-to-service.
- `[SBAuthorizeCustomer] POST GetAllPagesConfig/{idDonVi}` (lấy toàn bộ page config) — app khách.
- `[PrivateSBAuthorize] POST Save` (lưu MobilePageConfig) — service-to-service.

Chức năng cấu hình giao diện app mobile **hiện do `CauHinhController_AuthenCustomer` đảm nhiệm** (qua `JsonSetting.MobilePageJson.Pages[].Sections[]`). Khi cần cấu hình page/section app khách → xem `CauHinhController_AuthenCustomer.md`, KHÔNG dùng controller này.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| — | (không có) | — | — | — | — | Controller comment toàn bộ | Không endpoint nào hoạt động | — | Dead controller |

## Lỗi đã biết / lưu ý kỹ thuật
- `MobilePageConfigController.cs:1-46` — toàn bộ comment; controller không tồn tại lúc runtime. AI nên bỏ qua nhanh và chuyển sang `CauHinhController_AuthenCustomer` cho cấu hình app mobile.
