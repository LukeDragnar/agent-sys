---
controller: UpdateListDomainCorsController
module: nhan-su
service: CommissionFee
man_hinh_ui: "Quản trị > Domain CORS (area QuanTriArea — refresh danh sách domain CORS của service); tiện ích ẩn (hidden)"
muc_dich: "Reset cache danh sách domain CORS của service CommissionFee (hạ tầng) — không liên quan nghiệp vụ hoa hồng."
alias: [UpdateListDomainCors, "domain cors", "refresh cors"]
auth: internal
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends: [GlobalConfig.ListDomainCors, CoreConnectStr]
cross_module_in: []
cross_module_out: []
---

# UpdateListDomainCors (CommissionFee) — Refresh danh sách domain CORS

> Controller hạ tầng (TI_HeThong, tiện ích ẩn) lặp ở mọi service SCRM. Route `api/QuanTriArea/UpdateListDomainCors`.

## Hành động
| # | Hành động | Endpoint | Quyền (key) | Auth | Hành vi đúng (expected) | Nguồn |
|---|---|---|---|---|---|---|
| 1 | Cập nhật list domain CORS | POST `UpdateCors` | `DomainCors_UpdateCors` (HT-AD) | internal | Set `GlobalConfig.ListDomainCors = null` (xóa cache) rồi gọi `CorsHelper.IsOriginAllowed(...)` để nạp lại từ `CoreConnectStr`; luôn trả `true`. | GlobalConfig + DB cấu hình |

## Quan hệ
- **Dữ liệu:** không có entity domain riêng; thao tác trên `GlobalConfig.ListDomainCors` (in-memory) + đọc connection string `CoreConnectStr`.
- **Ripple:** ảnh hưởng policy CORS của service sau khi đổi danh sách domain ở DB cấu hình.

## Lỗi đã biết / lưu ý kỹ thuật
- `UpdateListDomainCorsController.cs:24` — kết quả `IsOriginAllowed` được tính nhưng bỏ qua; mục đích thực là **làm rỗng cache để lần sau nạp lại**. Boilerplate hạ tầng, không thuộc luồng hoa hồng.
