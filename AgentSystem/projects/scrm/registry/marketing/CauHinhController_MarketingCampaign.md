---
controller: CauHinhController
module: marketing
service: MarketingCampaign
man_hinh_ui: "Marketing > Cấu hình service khách hàng (màn cấu hình nội bộ MarketingCampaign)"
muc_dich: "Quản lý cấu hình của service MarketingCampaign: lấy cấu hình hiện tại, lưu cấu hình khởi tạo (init) cho đơn vị; expose 1 endpoint private cho service khác lấy config."
alias: ["cấu hình marketing campaign", "cấu hình service khách hàng (marketing)", "cau hinh init marketing", "config marketing campaign"]
auth: mixed
state_machine: []
deprecated: false
entities: [CauHinh]
stored_procs: []
depends: [CauHinhService (MarketingCampaign.Services.Generated)]
cross_module_in: [service khác (PrivateGetConfig)]
cross_module_out: []
---

# CauHinhController (MarketingCampaign) — Cấu hình service khách hàng

> ⚠️ Trùng tên controller với `CauHinhController` service **SMS** (xem `CauHinhController.md`). Node này là bản **MarketingCampaign** (TienIch = `CauHinh_MarketingCampaign_Controller`, MaTienIch = TI_MarketingCampaign). Hai controller khác service, khác endpoint, đừng nhầm.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy cấu hình | (load màn) | GET `api/CauHinh/Get` | `[SBAuthorize]` (chỉ đăng nhập) | public | - | Trả cấu hình hiện tại theo currentUser | EF `CauHinh` | Không gắn permission key riêng |
| 2 | Lưu cấu hình khởi tạo | "Lưu" | POST `api/CauHinh/SaveCauHinhInit` | `CauHinh_MarketingCampaign_LuuCauHinhKhachHang` | public | role DV-AD-TONG | Validate ModelState; lưu cấu hình init cho đơn vị (CauHinhSettingInitDto) | EF `CauHinh` | Chỉ DV-AD-TONG (admin tổng đơn vị) |
| 3 | Lấy cấu hình khởi tạo | (load màn init) | GET `api/CauHinh/GetCauHinhInit` | `[SBAuthorize]` (chỉ đăng nhập) | public | - | Trả cấu hình init theo currentUser | EF `CauHinh` | Không gắn permission key riêng |

## Endpoint nội bộ (private / service-to-service)
- [internal] `PrivateGetConfig` (`POST api/CauHinh/PrivateGetConfig`, `[PrivateSBAuthorize]`) — service khác gọi lấy config MarketingCampaign (cùng logic `GetAsync(CurrentUser)`). Auth = internal.

## Quan hệ
- **Gọi sang:** `ICauHinhService` (MarketingCampaign.Services.Generated) — đọc/ghi CauHinh.
- **Dữ liệu (entities):** `CauHinh`.
- **Ripple / đồng bộ:** cấu hình chung đồng bộ qua consumer `EventSyncCauHinhChungConsumer`.

## Lỗi đã biết / lưu ý kỹ thuật
- `Get` và `GetCauHinhInit` dùng `[SBAuthorize]` rỗng (chỉ cần đăng nhập), không gắn permission key — chỉ `SaveCauHinhInit` siết quyền DV-AD-TONG.
- **Trùng tên với SMS/CauHinhController** — khi search/tra cứu cần phân biệt theo route prefix giống nhau (`api/CauHinh/...`) nhưng khác service/host. Endpoint khác hẳn (SMS: Save/Get/LayCauHinh_GuiTin_Chung; MarketingCampaign: Get/SaveCauHinhInit/GetCauHinhInit/PrivateGetConfig).
