---
controller: ConfigSystemController
module: marketing
service: MarketingCampaign
man_hinh_ui: "Quản trị hệ thống (SandBoxArea) > ConfigSystem MarketingCampaign — CRUD config hệ thống của service + điều khiển Job chiến dịch (start/stop)"
muc_dich: "CRUD bảng ConfigSystem (config hệ thống) riêng cho service MarketingCampaign; kèm nhóm endpoint điều khiển Job chiến dịch: start/stop/đếm/lấy trạng thái job."
alias: ["config system marketing", "cấu hình hệ thống marketing campaign", "start stop job campaign", "điều khiển job chiến dịch", "configsystem sandbox"]
auth: public
state_machine: []
deprecated: false
entities: [ConfigSystem]
stored_procs: []
depends: [ConfigSystemService, JobCampaign (StaticVariable điều khiển job)]
cross_module_in: []
cross_module_out: []
---

# ConfigSystemController (MarketingCampaign) — Config hệ thống + điều khiển Job

> SandBoxArea / route `api/QuanTriArea/ConfigSystemController`. TienIch = `ConfigSystemController_MarketingCampaign`, MaTienIch = **TI_HeThong** (controller hệ thống, không phải UI marketing thường). Đặt trong module marketing vì thuộc service MarketingCampaign. Controller-level `[SBAuthorize]`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy config theo mã | (tra cứu) | GET `api/QuanTriArea/ConfigSystemController/TimTheoMa?ma=` | `ConfigSystem_MarketingCampaign_TimTheoMa` | public | - | Trả config theo mã | EF `ConfigSystem` | - |
| 2 | Lấy tất cả config | (list) | GET `.../LayTatCa` | `ConfigSystem_MarketingCampaign_LayTatCa` | public | - | List toàn bộ config | EF | - |
| 3 | Lấy config theo group + sort | (tree view) | POST `.../LayTatCaByGroupSort` | `ConfigSystem_MarketingCampaign_LayTatCaTheoGroupVaSort` | public | - | Trả config dạng cây theo group, sắp xếp | EF | - |
| 4 | Lấy config theo group | (tree) | GET `.../LayTatCaByGroup` | `ConfigSystem_MarketingCampaign_LayTatCaTheoGroup` | public | - | Trả config dạng cây theo group | EF | - |
| 5 | Thêm config | "Thêm mới" | POST `.../ThemMoi` | `ConfigSystem_MarketingCampaign_ThemMoi` | public | - | Validate ModelState; map DTO→entity, set NgayTao/NguoiTao/Id mới, insert | EF | - |
| 6 | Xóa config | "Xóa" | POST `.../Xoa/{id}` | `ConfigSystem_MarketingCampaign_XoaConfigSystem` | public | - | Check tồn tại → xóa. Mô tả ghi "ONLY_SUPERUSER" | EF | Nhạy cảm — mô tả nói chỉ superuser nhưng key vẫn dùng SBAuthorize thường |
| 7 | Cập nhật config | "Sửa" | POST `.../CapNhatThongTin` | `ConfigSystem_MarketingCampaign_CapNhatThongTin` | public | - | Validate; check tồn tại (404 nếu không); set NguoiCapNhat/NgayCapNhat; update | EF | - |
| 8 | Chi tiết config theo Id | "Xem" | GET `.../TimTheoId/{id}` | `ConfigSystem_MarketingCampaign_TimTheoId` | public | - | Trả config theo id (map sang DTO) | EF | - |
| 9 | Lấy các group config | (dropdown group) | GET `.../LayCacNhomCauHinh` | `ConfigSystem_MarketingCampaign_LayCacGroupConfigSystem` | public | - | List group code | EF | - |
| 10 | Update config qua Postman | (tool) | POST `.../UpdateConfigSystemWithPostMan` | `ConfigSystem_MarketingCampaign_UpdateConfigSystemWithPostMan` | public | - | Update config theo payload (công cụ vận hành) | EF | - |

## Quy tắc nghiệp vụ — điều khiển Job chiến dịch
| # | Hành động | Endpoint | Quyền (key) | Hành vi đúng |
|---|---|---|---|---|
| 11 | Dừng Job chiến dịch | POST `.../StopJobCampaign` | `ConfigSystem_MarketingCampaign_StopJobCampaign` | Set cờ static → Job chiến dịch ngừng chạy vòng mới |
| 12 | Chạy Job chiến dịch | POST `.../StartJobCampaign` | `ConfigSystem_MarketingCampaign_StartJobCampaign` | Bật lại cờ → Job chạy |
| 13 | Trạng thái start Job | POST `.../GetStatusStartJobCampaign` | `ConfigSystem_MarketingCampaign_GetStatusStartJobCampaign` | Trả cờ start/stop hiện tại |
| 14 | Đếm Job đang xử lý | POST `.../CountJobCampaignIsProcessing` | `ConfigSystem_MarketingCampaign_CountJobCampaignIsProcessing` | Đếm số job chiến dịch đang chạy |

## Quan hệ
- **Gọi sang:** `ConfigSystemService` (CRUD ConfigSystem + StaticVariable điều khiển job).
- **Dữ liệu (entities):** `ConfigSystem`.
- **Ripple / đồng bộ:** Start/Stop job ảnh hưởng toàn bộ luồng chạy chiến dịch (CampaignController/Job). Dừng job → chiến dịch không gửi cho tới khi start lại.

## Lỗi đã biết / lưu ý kỹ thuật
- Controller hệ thống (SandBoxArea) — không phải UI marketing nghiệp vụ; nằm trong service MarketingCampaign nên đặt module marketing.
- Endpoint `Xoa` mô tả "ONLY_SUPERUSER" nhưng vẫn dùng SBAuthorize key thường — cần xác nhận có gate superuser thật sự ở tầng nào.
- Stop/Start job là static-flag in-RAM (StaticVariableJobScanCampaignRunning...) — restart pod sẽ reset trạng thái.
