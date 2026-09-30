---
controller: PageMobileController
module: he-thong
service: Core
man_hinh_ui: "nội bộ — app mobile gọi để lấy danh sách trang/màn (key) được phép theo quyền; có bản mã hóa _Encrypt"
muc_dich: "Phục vụ app mobile: lấy danh sách 'page key' (màn được phép) theo user/role, kèm bản mã hóa AES (_Encrypt). Đọc chi tiết/danh sách page mobile. CRUD/quản trị page mobile nằm ở bản QuanTriArea (_Core)."
alias: ["page mobile", "key mobile", "màn mobile", "PageMobile"]
auth: public
state_machine: []
deprecated: false
entities: [PageMobile]
stored_procs: []
depends: ["IPageMobileService", "BaseController", "AES_ECB_128 (mã hóa)", "AutoMapper"]
cross_module_in: ["App mobile gọi GetAllKeyMobile/GetKeyByUserMobile (+ _Encrypt) để biết màn được phép", "Màn cấu hình gọi TimTheoDieuKien/GetAllForMobileAsync/TimTheoId"]
cross_module_out: []
---

# PageMobileController — Page/key mobile (đọc)

Bản `Controllers/`, kế thừa `BaseController`, controller-level `[SBAuthorize]` (login). Phơi đọc danh sách page mobile + key theo quyền user, kèm cặp `_Encrypt` mã hóa AES-ECB-128. Không có Thêm/Sửa/Xóa (CRUD ở bản QuanTriArea).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Xem chi tiết | (mở form) | GET `api/PageMobile/TimTheoId/{id:Guid}` | — (login, key đã comment) | public | — | `GetByIdAsync(id)` + `GetRolesByPageAsync` → map PageMobile→PageMDto kèm Roles. | EF | — |
| 2 | Lấy tất cả | (load list) | GET `api/PageMobile/LayTatCa` | — (login) | public | — | `GetAllAsync()`. | EF | — |
| 3 | Lấy cho mobile (filter) | (table mobile) | POST `api/PageMobile/GetAllForMobileAsync` | — (login) | public | — | `GetAllForMobileAsync(gridInfo)` trả page phục vụ hiển thị mobile. | EF | — |
| 4 | Tìm theo điều kiện | (search) | POST `api/PageMobile/TimTheoDieuKien` | — (login) | public | — | Lọc theo `PageMFilterDto`. | EF | — |
| 5 | Lấy tất cả key mobile | (mobile init) | GET `api/PageMobile/GetAllKeyMobile` (+ `_Encrypt`) | — (login) | public | — | `GetAllPage(CurrentUser, PAGETYPE_MOBILE)` trả toàn bộ key; bản `_Encrypt` mã hóa AES camelCase. | EF | — |
| 6 | Lấy key theo user | (mobile init) | GET `api/PageMobile/GetKeyByUserMobile` (+ `_Encrypt`) | — (login) | public | — | `GetPageByUser(CurrentUser, PAGETYPE_MOBILE)` trả key user được phép; bản `_Encrypt` mã hóa AES. | EF | — |

## Quan hệ
- **Gọi sang:** IPageMobileService (EF), AES_ECB_128 (mã hóa cho client mobile), AutoMapper.
- **Dữ liệu (entities):** `PageMobile`.
- **Ripple / đồng bộ:** chỉ đọc — quyết định màn/route mobile được phép hiển thị theo quyền user.

## Lỗi đã biết / lưu ý kỹ thuật
- `TimTheoId` key `Page_TimTheoId` bị comment (dòng 34) → chỉ còn login.
- CRUD (Thêm/Sửa/Xóa/Import) KHÔNG có ở bản này; xem bản QuanTriArea `PageMobileController_Core`.
