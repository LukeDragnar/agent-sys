---
controller: MenuMobileController
module: he-thong
service: Core
man_hinh_ui: "nội bộ — không có UI: 2 endpoint placeholder dùng để KHAI BÁO KEY PHÂN QUYỀN (qua [SBAuthorize]) cho menu/report mobile, không trả dữ liệu thật"
muc_dich: "Khai báo quyền hiển thị menu mobile và report mobile cho các role. Body chỉ trả 'OK' cứng — mục đích thực là đăng ký permission key qua attribute để hệ thống phân quyền nhận diện."
alias: ["menu mobile", "report mobile", "phân quyền mobile", "MenuMobile"]
auth: public
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends: ["IPageService (inject nhưng không dùng)", "IRedisCacheService (inject nhưng không dùng)"]
cross_module_in: ["App mobile / hệ thống phân quyền đọc 2 permission key Menu_MenuMobile, Menu_ReportMobile để quyết định hiển thị menu/report"]
cross_module_out: []
---

# MenuMobileController — Khai báo quyền menu / report mobile (placeholder)

Controller "rỗng logic": 2 endpoint chỉ trả `MethodResult<string>` = "OK". Giá trị thực nằm ở **attribute `[SBAuthorize]`** khai báo 2 permission key (`Menu_MenuMobile`, `Menu_ReportMobile`) với danh sách role rất rộng → dùng để hệ thống phân quyền gán/hiển thị menu & report cho mobile. TienIch `MenuMobileController` thuộc nhóm `TI_Chung`. Inject `IPageService`/`IRedisCacheService` nhưng **không dùng**.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Quyền menu mobile | (không UI) | GET `api/MenuMobile/MenuMobile` | `Menu_MenuMobile` (HT-CSKH, HT-AD, DV-AD-TONG, DV-AD-CN, DV-AD-MCN, DV-QLLH, DV-KT, DV-KT-TONG, DV-QLDV, DV-NV, DV-MKT, DV-SALE, HT-NGT, DV-QL-SALE, DV-SALE-LEAD) | public (login) | — | Trả cứng `Success=true, Data="OK"` — không truy DB; mục đích là đăng ký key. | (không) | Endpoint placeholder, không có logic thật |
| 2 | Quyền report mobile | (không UI) | GET `api/MenuMobile/ReportMobile` | `Menu_ReportMobile` (HT-CSKH, HT-AD, DV-AD-TONG, DV-AD-CN, DV-AD-MCN, DV-QLLH, DV-QLDV, DV-MKT, DV-SALE, HT-NGT, DV-QL-SALE, DV-SALE-LEAD) | public (login) | — | Trả cứng "OK" — đăng ký key report mobile. | (không) | Placeholder; danh sách role hẹp hơn #1 (thiếu DV-KT/DV-KT-TONG/DV-NV) |

## Quan hệ
- **Gọi sang:** không (không gọi service nào dù đã inject).
- **Dữ liệu (entities):** không.
- **Ripple / đồng bộ:** hệ thống phân quyền dựa trên 2 key này để hiển thị menu/report bên app mobile.

## Lỗi đã biết / lưu ý kỹ thuật
- `MenuMobileController.cs:36-43, 51-58` — cả 2 endpoint trả "OK" cứng, **không có logic** → là placeholder khai báo permission key, không phải API dữ liệu (đừng tưởng nhầm trả menu thật).
- `MenuMobileController.cs:21-28` inject `IPageService` + `IRedisCacheService` nhưng không sử dụng → dead dependency.
- `MenuMobileController.cs:34` chuỗi role có khoảng trắng thừa quanh dấu phẩy (`"HT-CSKH , HT-AD ,..."`) → nếu parser tách role không trim sẽ sai key role.
- Namespace `Core.API.Areas.SanboxArea` nhưng route là `api/MenuMobile` (không có prefix QuanTriArea).
