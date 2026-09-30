---
controller: TongDaiIpUserController
module: sales
service: Switchboard
man_hinh_ui: "Tổng đài IP > Quản lý user (map user CRM ↔ user tổng đài, gia hạn/đổi user, đăng ký đầu số, cấu hình số tiền/user)"
muc_dich: "Quản lý user tổng đài IP: map user đơn vị (CRM) với user tổng đài, thay đổi/gia hạn/xóa user, lấy danh sách user CRM và user đã map, tính số tiền khi thay đổi user, và đăng ký đầu số cho đơn vị."
alias: ["user tổng đài IP", "map user tổng đài", "gia hạn user tổng đài", "đăng ký đầu số"]
auth: public
state_machine: []
deprecated: false
entities: [TongDaiIpUser, UserConnect, DauSo]
stored_procs: []
depends: [ITongDaiIpUserService, "Omicall/tenant tổng đài IP (external)"]
cross_module_in: ["CRM đơn vị (GetDanhSachUserBenCrm — lấy user CRM để map)"]
cross_module_out: ["External tenant tổng đài IP (map/đổi/xóa user, đăng ký đầu số)"]
---

# TongDaiIpUser — Quản lý user tổng đài IP

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Get thông tin user đơn vị hiện tại | (load) | POST `GetCurrentInfoUserDonVi` | `TongDaiIpUser_GetCurrentInfoUserDonVi` (HT-AD, HT-KT, DV-AD-TONG/CN/MCN) | public | mở màn user tổng đài | Trả thông tin user đơn vị theo `CurrentUser`. | EF | — |
| 2 | Map user CRM ↔ user tổng đài | Map | POST `MapUserDonViVoiUserTongDai` | `TongDaiIpUser_MapUserDonViVoiUserTongDai` | public | tab map user | Gán user CRM với user tổng đài (đẩy lên tenant). | EF + tenant | — |
| 3 | Thay đổi user tổng đài IP | Đổi user | POST `ThayDoiUserTongDaiIp` | `TongDaiIpUser_ThayDoiUserTongDaiIp` | public | user đã map | Hoán đổi user tổng đài (TODO marker trong code). | EF + tenant | Có comment `//TODO` tại endpoint |
| 4 | Lấy danh sách user CRM | (dropdown) | POST `GetDanhSachUserBenCrm` | `TongDaiIpUser_GetDanhSachUserBenCrm` | public | màn map user | Trả danh sách user bên CRM để chọn map. | CRM | — |
| 5 | Cấu hình số tiền user/tháng | (load) | GET `GetCauHinhSoTienUserTrenThang` | `TongDaiIpUser_GetCauHinhSoTienUserTrenThang` | public | tab gia hạn | Trả cấu hình đơn giá số tiền/user/tháng. | EF | — |
| 6 | Danh sách user đã map (phân trang) | (load) | POST `GetDanhSachUserDaMapTongDai` | `TongDaiIpUser_GetDanhSachUserDaMapTongDai` | public | màn user đã map | Trả danh sách user đã map tổng đài (có phân trang). | EF | — |
| 7 | Danh sách user đã map CRM | (load) | POST `GetListUserConnectMapCrm` | `TongDaiIpUser_GetListUserConnectMapCrm` | public | — | Trả danh sách user connect đã map CRM. | EF | — |
| 8 | Danh sách user trong kịch bản | (load) | POST `GetUserInScripts` | `TongDaiIpUser_GetUserInScripts` | public | màn kịch bản | Trả user gắn trong kịch bản cuộc gọi. | EF | — |
| 9 | Tính số tiền khi gia hạn/đổi user | (preview) | POST `CalculateSoTienKhiThayDoiUser` | `TongDaiIpUser_CalculateSoTienKhiThayDoiUser` | public | trước khi xác nhận đổi | Tính trước số tiền phải trả khi thay đổi/gia hạn user. | EF | — |
| 10 | Cập nhật user xóa tổng đài | Xóa user | POST `CapNhatUserXoaTongDai` | `TongDaiIpUser_CapNhatUserXoaTongDai` | public | user đã map | Gỡ/xóa user khỏi tổng đài. | EF + tenant | — |
| 11 | Đơn vị đăng ký đầu số | Đăng ký đầu số | POST `DonViDangKyDauSo` | `TongDaiIpUser_DonViDangKyDauSo` | public | tab đầu số | Đăng ký đầu số cho đơn vị (đẩy lên tenant). | EF + tenant | — |

## Quan hệ
- **Gọi sang:** `ITongDaiIpUserService` → external tenant (map/đổi/xóa user, đăng ký đầu số); CRM (lấy danh sách user).
- **Dữ liệu (entities):** user tổng đài IP, mapping user CRM ↔ tổng đài, cấu hình đơn giá user/tháng, đầu số.
- **Ripple / đồng bộ:** thao tác user/đầu số đẩy sang tenant tổng đài IP (external).

## Lỗi đã biết / lưu ý kỹ thuật
- `TongDaiIpUserController.cs:66-73` — `ThayDoiUserTongDaiIp` có `//TODO`, lưu ý logic chưa chốt hẳn.
- `TongDaiIpUserController.cs:75-99` — 2 endpoint `JobThayDoiUserTongDai` (AllowAnonymous, test job) và `GetThongTinSoTienUserTongDai` đã bị comment → không expose.
