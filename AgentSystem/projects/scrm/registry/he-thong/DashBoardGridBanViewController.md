---
controller: DashBoardGridBanViewController
module: he-thong
service: Core
man_hinh_ui: "Dashboard CEO / màn cấu hình widget bảng (grid bán) theo khu vực + người dùng"
muc_dich: "Lấy/cập nhật/sao chép cấu hình widget dashboard dạng grid (bán hàng) theo user + khu vực; có 2 endpoint anonymous phục vụ dashboard tổng/ngoài đăng nhập."
alias: ["Dashboard grid bán", "CEO Dashboard", "config widget", "DashBoardGridBanView"]
auth: mixed
state_machine: []
deprecated: false
entities: [ConfigWidgetBan]
stored_procs: []
depends: ["IDashBoardGridBanViewService"]
cross_module_in: ["[UI] dashboard CEO gọi GetConfigWidgetByUser/ConfigWidgetUpdate/SaoChepConfigWidget", "[anonymous] dashboard tổng / màn public gọi GetConfigWidgetAdTong, GetConfigWidgetByUserFullKhuVucAllow"]
cross_module_out: []
---

# DashBoardGridBanViewController — Cấu hình widget dashboard grid bán

Kế thừa `BaseController`, `[SBAuthorize]` đặt **controller-level** nên mặc định mọi action yêu cầu đăng nhập — TRỪ 2 action gắn `[AllowAnonymous]` để phục vụ dashboard tổng/màn ngoài. Lấy `CurrentUser` để cá nhân hóa cấu hình theo người dùng + khu vực. Toàn bộ nghiệp vụ qua `IDashBoardGridBanViewService`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy config widget theo user + khu vực | (load dashboard) | GET `api/DashBoardGridBanView/GetConfigWidgetByUser/{idkhuvuc:Guid}` | — (`[SBAuthorize]` ctrl) | public | — | Trả cấu hình widget của `CurrentUser` theo `idkhuvuc`. | EF qua service | — |
| 2 | Cập nhật config widget | Lưu cấu hình | POST `api/DashBoardGridBanView/ConfigWidgetUpdate` | — (`[SBAuthorize]` ctrl) | public | — | Nhận `RequestConfigBanWidget`, lưu cấu hình cho `CurrentUser`. | EF qua service | — |
| 3 | Sao chép config widget cho danh sách user | Sao chép | POST `api/DashBoardGridBanView/SaoChepConfigWidget` | — (`[SBAuthorize]` ctrl) | public | — | Nhận `FormInitDto` (danh sách user), copy cấu hình của `CurrentUser` sang các user đó. | EF qua service | — |
| 4 | Lấy config widget toàn khu vực | (load dashboard) | GET `api/DashBoardGridBanView/GetConfigWidgetByUserFullKhuVuc` | — (`[SBAuthorize]` ctrl) | public | — | Trả cấu hình widget của `CurrentUser` cho tất cả khu vực. | EF qua service | — |
| 5 | Lấy config widget AD tổng | (dashboard tổng) | POST `api/DashBoardGridBanView/GetConfigWidgetAdTong` | — | anonymous (`[AllowAnonymous]`) | — | Nhận `GetConfigWidgetAdTongDto`, trả cấu hình widget tổng (KHÔNG dùng CurrentUser dù có lấy biến). | EF qua service | Endpoint anonymous trả dữ liệu cấu hình → cần kiểm soát lộ thông tin |
| 6 | Lấy config full khu vực (allow) | (dashboard public) | POST `api/DashBoardGridBanView/GetConfigWidgetByUserFullKhuVucAllow` | — | anonymous (`[AllowAnonymous]`) | — | Nhận `GetConfigWidgetByUserFullKhuVucAllowDto`, trả cấu hình theo tham số trong request (không cần đăng nhập). | EF qua service | Endpoint anonymous trả cấu hình theo input → cần kiểm soát truy cập |

## Quan hệ
- **Gọi sang:** `IDashBoardGridBanViewService`.
- **Dữ liệu (entities):** cấu hình widget dashboard bán (theo user + khu vực).
- **Ripple / đồng bộ:** sao chép cấu hình (action 3) ghi config cho nhiều user khác → ảnh hưởng dashboard của họ.

## Lỗi đã biết / lưu ý kỹ thuật
- `DashBoardGridBanViewController.cs:65-83` — 2 endpoint `[AllowAnonymous]` (`GetConfigWidgetAdTong`, `GetConfigWidgetByUserFullKhuVucAllow`) trả dữ liệu cấu hình mà không yêu cầu đăng nhập → cần rà soát rủi ro lộ cấu hình/dữ liệu.
- `DashBoardGridBanViewController.cs:67-69` — `GetConfigWidgetAdTong` lấy `var user = CurrentUser` nhưng **không truyền vào service** (chỉ truyền `rq`) → biến thừa, và `CurrentUser` ở route anonymous có thể null.
