---
controller: GetAPITestController
module: he-thong
service: AuthenPartner
man_hinh_ui: "Nội bộ — không có UI. Endpoint smoke-test cho đối tác kiểm tra kết nối/token (ping + xem thông tin login)."
muc_dich: "Endpoint helper để đối tác (hoặc nội bộ) kiểm tra nhanh kết nối & token partner: ping trả 'xin chào' và trả về thông tin UserLogin đã resolve từ token."
alias: ["test api partner", "ping api đối tác", "kiểm tra token partner", "smoke test authenpartner"]
auth: internal
state_machine: []
entities: []
stored_procs: []
deprecated: false
depends: ["SBAuthorizePartner (JWT partner + CauHinh AccessToken/DeviceId + IsSuDungApiGetData)", "IUserPrincipalService (đọc UserLogin từ HttpContext)"]
cross_module_in: ["Đối tác / dev → GET GetAPI, GET test (kiểm tra kết nối + token)"]
cross_module_out: []
---

# GetAPITestController — Smoke-test kết nối & token đối tác (AuthenPartner)

Controller helper của AuthenPartner để **đối tác (hoặc dev) kiểm tra nhanh** rằng token partner đã cấp phát hoạt động và đi qua được filter `SBAuthorizePartner`. **Không phải unit-test** (không phải file test bị skip) — đây là endpoint thật, nhưng **mục đích chỉ là kiểm tra tích hợp**, không có nghiệp vụ.

Kế thừa `BasePartnerController` + `[SBAuthorizePartner]` (mô hình auth đối tác xem node `CommonController_AuthenPartner`). Vì có `[SBAuthorizePartner]`, gọi được nghĩa là token + cấu hình đơn vị (`AccessToken`/`DeviceId`/`IsSuDungApiGetData`) đều hợp lệ.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Ping | (API) | GET `api/GetAPITest/GetAPI` | — | internal `[SBAuthorizePartner]` | — | Trả `Ok("xin chào")` nếu qua được filter auth (xác nhận token + cấu hình đơn vị hợp lệ). | (không query) | Chuỗi phản hồi hardcode "xin chào" |
| 2 | Xem thông tin login | (API) | GET `api/GetAPITest/test` | — | internal `[SBAuthorizePartner]` | — | Gọi `ChiNhanhService.GetUserLoginDonVi` → trả `UserLogin` resolve từ token (DonViId, IdNganhNghe...). Dùng để dev kiểm tra claim/đơn vị token đang map đúng. | từ `IUserPrincipalService` | Endpoint tên `test` lộ thông tin login — chỉ nên bật ở môi trường tích hợp |

## Quy tắc nghiệp vụ (gating)
1. Auth `SBAuthorizePartner` (JWT + CauHinh AccessToken/DeviceId + `IsSuDungApiGetData`). Nếu fail → 401, không tới body.
2. Không có gating nghiệp vụ khác — endpoint thuần kiểm tra.

## Quan hệ
- **Gọi sang:** `ChiNhanhService.GetUserLoginDonVi` (đọc `UserLogin` từ `IUserPrincipalService`, không gọi service ngoài).
- **Dữ liệu (entities):** không.
- **Ripple / đồng bộ:** không.

## Lỗi đã biết / lưu ý kỹ thuật
- `GetAPITestController.cs:27-31` — endpoint `test` trả thẳng đối tượng `UserLogin` (thông tin login đã resolve) ra ngoài; nên giới hạn ở môi trường staging/integration, tránh expose ở production.
- Cả 2 method `async` nhưng không có `await` thực (warning compiler) — chấp nhận được vì chỉ là helper.
