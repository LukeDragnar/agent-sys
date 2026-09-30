---
controller: MobilePageConfigController
module: website
service: Website
man_hinh_ui: "Website > Cấu hình page app mobile — màn cấu hình các trang/section của app khách (lọc page config + lưu); thực chất proxy sang AuthenCustomer."
muc_dich: "Cấu hình các trang (page) của app mobile khách: lọc page config theo đơn vị + filter, và lưu cấu hình. Controller chỉ là proxy — gọi sang service AuthenCustomer (api/MobilePageConfig) qua ClientHelper."
alias: ["cấu hình page mobile", "mobile page config", "cấu hình trang app", "page config app khách"]
auth: public
state_machine: []
deprecated: false
entities: [MobilePageConfig]
stored_procs: []
depends: [Website (MobilePageConfigService proxy), AuthenCustomer (api/MobilePageConfig)]
cross_module_in: []
cross_module_out: [AuthenCustomer (api/MobilePageConfig/GetPagesConfigFilter, api/MobilePageConfig/Save)]
---

# MobilePageConfigController (Website) — Cấu hình page app mobile (proxy)

> Tên file `MobilePageConfigController_Website.md` vì controller cùng tên đã có ở module **he-thong** (collision). **LƯU Ý:** bản he-thong bị comment toàn bộ (`deprecated`), còn bản **Website này SỐNG**: có route + 2 endpoint hoạt động. Route `api/MobilePageConfig` (`Controllers/Api`). Cả 2 endpoint `[SBAuthorize]` rỗng (cần đăng nhập). Controller chỉ **proxy**: `MobilePageConfigService` POST sang AuthenCustomer service, không tự đọc/ghi DB Website.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lọc page config | "Tìm kiếm" page config | POST `api/MobilePageConfig/GetPagesConfigFilter` | `[SBAuthorize]` rỗng | public | luôn | Lấy `CurrentUser.DonViId` → POST AuthenCustomer `/api/MobilePageConfig/GetPagesConfigFilter/{idDonVi}` với filter; trả list MobilePageConfigDto | proxy (AuthenCustomer) | - |
| 2 | Lưu page config | "Lưu" | POST `api/MobilePageConfig/Save` | `[SBAuthorize]` rỗng | public | luôn | POST AuthenCustomer `/api/MobilePageConfig/Save` với req (DonViId nằm trong payload, KHÔNG truyền từ CurrentUser) | proxy (AuthenCustomer) | Save không gắn idDonVi từ CurrentUser vào URL như Get → phụ thuộc client truyền đúng DonViId trong body (MobilePageConfigService.cs:34-37) |

## Quan hệ
- **Gọi sang:** `MobilePageConfigService` (Website) → AuthenCustomer `{AuthenCustomerServiceUrl}/api/MobilePageConfig/*` qua `ClientHelper` (MobilePageConfigService.cs:34-42).
- **Dữ liệu (entities):** `MobilePageConfig` (lưu thực tế ở AuthenCustomer DB, không phải Website).
- **Ripple / đồng bộ:** thay đổi page config → ảnh hưởng giao diện app khách; nguồn dữ liệu thật do AuthenCustomer quản.

## Lỗi đã biết / lưu ý kỹ thuật
- **Khác bản he-thong:** controller Website này KHÔNG bị comment, đang hoạt động (chỉ trùng tên). Bản `he-thong/MobilePageConfigController.md` là dead controller; ĐỪNG nhầm.
- `MobilePageConfigService.cs:34-37` (`SaveAsync`) — gửi `req` thẳng sang AuthenCustomer, không chèn `CurrentUser.DonViId` vào URL (khác `GetPagesConfigFilter` có `{idDonVi}`); tin cậy DonViId trong body → cần xác nhận client luôn set đúng để tránh ghi nhầm đơn vị.
- Cả 2 endpoint `[SBAuthorize]` rỗng → chỉ cần đăng nhập, không phân quyền chi tiết.
