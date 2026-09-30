---
controller: FBCustomerController
module: marketing
service: FacebookChat
man_hinh_ui: "Marketing > ChatHub: panel thông tin khách hàng bên phải khung chat (avatar, profile) — nút/đồng bộ 'Cập nhật thông tin khách'"
muc_dich: "Lo thông tin khách hàng Facebook (FBCustomer) trong ChatHub: lấy chi tiết khách theo PSId, đồng bộ lại ảnh đại diện từ Graph API, và cung cấp endpoint nội bộ lấy khách (private) cho service khác."
alias: ["khách hàng FB", "FBCustomer", "thông tin khách chat", "profile khách Facebook", "PSId khách"]
auth: mixed
state_machine: []
deprecated: false
entities: [FBCustomer, FBFanpage]
stored_procs: []
depends:
  - "IFBCustomerService (BaseService<FBCustomer>) — CRUD + GetDetailCustomer / GetDetailCustomerPrivate / UpdateProFile_Picture / UpdateCustomerSoContact"
  - "ICommonService.GetFbFanpageByFanPageId — lấy fanpage (access token) từ cache"
  - "MessageHelper.GetUserInfo — gọi Graph API v21.0 lấy profile/avatar khách"
  - "EF: IRepository<FBCustomer> (lọc theo IdDonVi+IdChiNhanh+PSId)"
cross_module_in:
  - "Service nội bộ khác — gọi GetCustomerFB_Private (AllowAnonymous) để lấy khách theo IdDonVi/IdChiNhanh/PsId"
cross_module_out:
  - "Facebook Graph API (graph.facebook.com v21.0) — GetUserInfo lấy ảnh đại diện khi cập nhật"
---

# FBCustomerController — Thông tin khách hàng Facebook (ChatHub)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Cập nhật ảnh đại diện khách | Cập nhật thông tin khách | GET `api/FBCustomer/UpdateCustomer?PsId` | `FBCustomer_FacebookChat_UpdateCustomer` | internal | Khách tồn tại trong DV+CN; fanpage còn access token | Tìm FBCustomer theo PSId+DonVi+ChiNhanh; lấy fanpage theo `IdPage` → gọi Graph `GetUserInfo` → set `Profile_Pic = Picture.data.url`, update EF. Lỗi không thấy khách/fanpage → trả error message. | EF `FBCustomer` + Graph API | Phần check ảnh còn hạn (`IsFacebookUrlAccessible`) đã **comment hết** (`FBCustomerService.cs:72-96`) → luôn gọi lại Graph dù ảnh chưa hết hạn (tốn call). |
| 2 | Lấy chi tiết khách | (panel thông tin khách) | GET `api/FBCustomer/GetDetailCustomerFB?psId` | (SBAuthorize rỗng — chỉ cần đăng nhập) | internal | — | Query FBCustomer theo PSId+DonVi+ChiNhanh, trả nguyên entity (null nếu không có). | EF `FBCustomer` | — |
| 3 | Lấy chi tiết khách (nội bộ) | — | POST `api/FBCustomer/GetCustomerFB_Private` (`FBCustomerFilter`) | (không) | anonymous | Service khác gọi với IdDonVi+IdChiNhanh+PsId | Query theo filter; không thấy → "Không tìm thấy khách hàng"; có → map sang `FBCustomerDto`. | EF `FBCustomer` | `[AllowAnonymous]` — không gắn token, dựa vào IdDonVi/IdChiNhanh trong body để khoanh vùng dữ liệu (service-to-service). |

## Endpoint nội bộ (private / service-to-service)
- `[internal/anonymous]` `GetCustomerFB_Private` — endpoint cho service khác (webhook/consumer) lấy thông tin khách FB theo đơn vị; trả DTO đã map.
- (Service-level, không expose qua controller này) `UpdateCustomerSoContact` — tăng `SoContact` của khách +1; gọi từ luồng tạo contact/đơn ở nơi khác.

## Quan hệ
- **Gọi sang:** `IFBCustomerService` → EF `FBCustomer`; Graph API v21.0 (`GetUserInfo`) lấy avatar; `ICommonService.GetFbFanpageByFanPageId` lấy fanpage/access token.
- **Dữ liệu (entities):** `FBCustomer` (PSId, IdPage, Profile_Pic, SoContact), `FBFanpage` (AccessToken).
- **Ripple / đồng bộ:** không bắn event; chỉ đọc/ghi FBCustomer + gọi Graph. Avatar cập nhật ở đây được ConversationController dùng để gắn vào danh sách hội thoại.

## Lỗi đã biết / lưu ý kỹ thuật
- `FBCustomerService.cs:72-96` — toàn bộ logic short-circuit "ảnh còn hạn thì khỏi gọi Graph" bị comment → mỗi lần `UpdateCustomer` đều gọi Graph API (lãng phí, dễ dính rate-limit).
- `FBCustomerController.cs:37` — `GetCustomerFB_Private` `[AllowAnonymous]`: bảo mật dựa hoàn toàn vào IdDonVi/IdChiNhanh trong body, cần đảm bảo caller là internal (không expose ra ngoài).
- `GetDetailCustomerFB` trả thẳng entity EF (không qua DTO) → lộ field nội bộ; khác với `GetCustomerFB_Private` đã map DTO.
