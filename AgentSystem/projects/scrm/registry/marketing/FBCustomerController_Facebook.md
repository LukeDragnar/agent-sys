---
controller: FBCustomerController
module: marketing
service: Facebook
man_hinh_ui: "nội bộ — không có UI riêng (gọi ngầm từ màn hội thoại ChatHub để làm tươi avatar khách FB)"
muc_dich: "Làm tươi (refresh) ảnh đại diện FB của khách hàng (FBCustomer) khi link avatar cũ đã hết hạn: kiểm tra URL profile cũ còn truy cập được không, nếu hỏng thì gọi Graph lấy avatar mới rồi cập nhật DB."
alias: ["cập nhật avatar khách FB", "refresh profile pic", "FBCustomer", "ảnh khách facebook"]
auth: public
state_machine: []
deprecated: false
entities: [FBCustomer, FBFanpage]
stored_procs: []
depends:
  - "IFBCustomerService (Facebook.Service.Generated)"
  - "MessageHelper.IsFacebookUrlAccessible / GetUserInfo (Graph) — check + lấy avatar mới"
cross_module_in:
  - "ChatHub/hội thoại UI — load avatar khách FB hết hạn"
cross_module_out:
  - "Facebook Graph API — GetUserInfo (lấy ProfilePic mới)"
---

# FBCustomerController (Facebook) — Làm tươi avatar khách hàng FB

> Controller 1 endpoint, không có `[TienIch]`. Là tiện ích phụ trợ cho luồng chat (avatar FB có link hết hạn theo thời gian).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Cập nhật avatar khách FB | (ngầm khi render hội thoại) | GET `api/FBCustomer/updatecustomer?PsId` | (key rỗng `SBAuthorize("","",6 role)`) | public | Avatar cũ đã hỏng/hết hạn | Tìm `FBCustomer` theo `PSId`+đơn vị+chi nhánh; lấy fanpage theo `customer.IdPage`. Check `IsFacebookUrlAccessible(Profile_Pic)`: còn sống → trả nguyên customer; hỏng → gọi Graph `GetUserInfo(PsId, page.AccessToken)`, gán `Profile_Pic = ProfilePic` mới, `UpdateAsync`. | EF (FBCustomer, FBFanpage) + Graph | `long.Parse(customer.IdPage)` không guard null/parse (FBCustomerService.cs:41) → NRE/format-exception nếu IdPage rỗng. `customer` không null-check trước khi đọc `.IdPage` → NRE nếu PsId không tồn tại. `UpdateAsync(customer)` KHÔNG await (FBCustomerService.cs:55) → fire-and-forget, có thể trả về trước khi lưu xong. |

## Quan hệ
- **Gọi sang:** Graph (`GetUserInfo`) qua `MessageHelper`.
- **Dữ liệu (entities):** `FBCustomer` (chính), `FBFanpage` (lấy AccessToken theo IdPage).
- **Ripple / đồng bộ:** cập nhật trực tiếp `Profile_Pic` của customer; không event cross-service.

## Lỗi đã biết / lưu ý kỹ thuật
- `FBCustomerService.cs:41` — `long.Parse(customer.IdPage)` + `customer` chưa null-check → 500 khi PsId sai/IdPage rỗng.
- `FBCustomerService.cs:55` — gọi `UpdateAsync(customer)` thiếu `await` (warning bỏ qua kết quả) → không đảm bảo đã persist.
- Graph version hardcode `"v21.0"` + `https://graph.facebook.com/` (FBCustomerService.cs:49) thay vì lấy từ config.
- `[SBAuthorize]` key rỗng (chỉ check đăng nhập + role).
