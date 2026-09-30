---
controller: CommonMobileController
module: website
service: Website
man_hinh_ui: "App khách (mobile) — chọn địa chỉ giao hàng: dropdown tỉnh/huyện/xã theo cấp (level)."
muc_dich: "Cấp dữ liệu địa chỉ hành chính (tỉnh/huyện/xã) cho app mobile khách theo cấp (level) và mã cha (code); proxy sang Core service lấy danh mục địa chỉ Việt Nam."
alias: ["địa chỉ app", "common mobile", "dropdown tỉnh huyện xã", "địa chỉ giao hàng mobile"]
auth: public
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends: [Website (CommonApiService), Core (SA_GetAllAddressByLevelPrivate)]
cross_module_in: []
cross_module_out: [Core (api/Common/SA_GetAllAddressByLevelPrivate)]
---

# CommonMobileController — Địa chỉ hành chính cho app mobile

> Controller app khách (`Controllers/MobileApi`, namespace `...CommonMobileController`), route `api/CommonMobile`. Dùng `[TokenMobileAuthentication]` — auth bằng customer token (header `sb-device: mobile` + `Authorization: Bearer`). Quốc gia cố định `VN`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy địa chỉ theo cấp | dropdown tỉnh/huyện/xã | GET `api/CommonMobile/GetDiaChi?code={parentCode}&level={level}` | `[TokenMobileAuthentication]` | public | theo level | Build `ReqGetAddressByLevel`(NationalCode=VN, LevelAddress=level, ParentCode=code) → `CommonApiService.GetAddressByLevelPrivate` proxy sang Core `api/Common/SA_GetAllAddressByLevelPrivate`; trả list địa chỉ cấp tương ứng | private cross-service (Core) | - |

## Quan hệ
- **Gọi sang:** `CommonApiService.GetAddressByLevelPrivate` (Website) → POST Core `{CoreServiceUrl}/api/Common/SA_GetAllAddressByLevelPrivate` qua `ClientHelper` (CommonApiService.cs:439-445).
- **Dữ liệu (entities):** không truy DB Website; dữ liệu địa chỉ (`AddressSystemDto`) lấy từ Core.
- **Ripple / đồng bộ:** read-only proxy, không side-effect.

## Lỗi đã biết / lưu ý kỹ thuật
- `CommonApiService.cs:443` — nếu Core trả null/Success=false → trả `ResultWithError("res null")` (message kỹ thuật thô, không thân thiện UI).
- Cùng method `GetAddressByLevelPrivate` được `CartController` dùng để dựng tỉnh/huyện/xã khi checkout (CartController.cs:560+) → thay đổi hành vi địa chỉ ảnh hưởng cả luồng giỏ hàng.
- Auth bắt buộc header `sb-device: mobile`; thiếu → 401 (TokenMobileAuthenticationFilter.cs:23-47).
