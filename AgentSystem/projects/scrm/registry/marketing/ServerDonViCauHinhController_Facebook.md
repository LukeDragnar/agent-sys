---
controller: ServerDonViCauHinhController
module: marketing
service: Facebook
man_hinh_ui: "Quản trị > Quản lý cấu hình đơn vị (Area SanboxArea, route api/QuanTriArea/ServerDonViCauHinh). Đồng bộ đơn vị sang server webhook FB + xem/lưu cấu hình đơn vị."
muc_dich: "Quản lý cấu hình đơn vị trên server webhook Facebook chung: đồng bộ (add/update) toàn bộ đơn vị sang server, tìm/lấy/lưu cấu hình đơn vị. Mọi nghiệp vụ uỷ quyền cho ICommonService → FacebookPrivateHelper (gọi server webhook private theo ServerCode)."
alias: ["cấu hình đơn vị FB", "đồng bộ đơn vị server webhook", "ServerDonViCauHinh", "AddOrUpdateDonVi"]
auth: public
state_machine: []
deprecated: false
entities: [ServerDonViCauHinh, DonVi]
stored_procs: []
depends:
  - "ICommonService.ServerDonViCauHinh_AddOrUpdateDonvi / _TimTheoDieuKienAsync / _GetAsync / _Save"
  - "FacebookPrivateHelper (qua _facebookWebhookPrivateSetting.ServerCode) — gọi server webhook private"
  - "GetAllDonVi — lấy danh sách đơn vị để đồng bộ"
cross_module_in:
  - "Web quản trị cấu hình đơn vị — đồng bộ/tra cứu/lưu"
cross_module_out:
  - "Server webhook private (FacebookPrivateHelper) — add/update + đọc cấu hình đơn vị theo ServerCode"
---

# ServerDonViCauHinhController (QuanTriArea) — Cấu hình đơn vị trên server webhook FB

> Area `SanboxArea`, route `api/QuanTriArea/ServerDonViCauHinh`. Controller-level `[SBAuthorize]` + `[TienIch("ServerDonViCauHinhController","Quản lý cấu hình đơn vị", TI_HeThong, true)]`. Permission KEY từng action bị comment → chỉ check đăng nhập. Service gắn `ServerCode` từ private setting rồi forward sang `FacebookPrivateHelper`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Đồng bộ đơn vị sang server | Đồng bộ | POST `api/QuanTriArea/ServerDonViCauHinh/AddOrUpdateDonVi` | (chỉ check đăng nhập — key comment) | public | Có dữ liệu đơn vị | `GetAllDonVi()`; rỗng → "Không có dữ liệu Đơn vị"; có → gắn `ServerCode`, `FacebookPrivateHelper.ServerDonViCauHinh_AddOrUpdateDonvi` add/update đơn vị lên server webhook. | private helper (server webhook) | Key permission comment → chỉ đăng nhập. |
| 2 | Tìm cấu hình đơn vị | Tìm kiếm | POST `api/QuanTriArea/ServerDonViCauHinh/TimTheoDieuKienAsync` **và** `.../TimTheoDieuKien` (2 route cùng action) | (chỉ check đăng nhập) | public | — | Gắn `ServerCode`, `ServerDonViCauHinh_TimTheoDieuKienAsync` → list + totalRecord. | private helper | 1 action gắn 2 `[HttpPost]` route (`...:55-56`) → trùng alias đường dẫn. |
| 3 | Lấy cấu hình đơn vị | (xem) | POST `api/QuanTriArea/ServerDonViCauHinh/GetAsync` | (chỉ check đăng nhập) | public | — | Gắn `ServerCode`, `ServerDonViCauHinh_GetAsync(dto)`; helper null → "Lỗi lấy dữ liệu cấu hình fb!". | private helper | — |
| 4 | Lưu cấu hình đơn vị | Lưu | POST `api/QuanTriArea/ServerDonViCauHinh/Save` | (chỉ check đăng nhập) | public | — | Gắn `ServerCode`, `ServerDonViCauHinh_Save(dto)` → lưu cấu hình đơn vị lên server webhook. | private helper | — |

## Quy tắc nghiệp vụ
- Mọi method service: `if (userLogin==null) userLogin=new UserLogin();` rồi gán `gridInfo/dto.ServerCode = _facebookWebhookPrivateSetting.ServerCode` → cô lập dữ liệu theo server webhook hiện hành trước khi gọi `FacebookPrivateHelper`.
- `AddOrUpdateDonVi` đồng bộ TẤT CẢ đơn vị (`GetAllDonVi`) trong 1 lần → thao tác bulk.

## Quan hệ
- **Gọi sang:** `ICommonService` → `FacebookPrivateHelper` (HTTP sang server webhook private theo `ServerCode`).
- **Dữ liệu (entities):** cấu hình đơn vị (`ServerDonViCauHinh`) lưu bên server webhook private; đọc `DonVi` để đồng bộ.
- **Ripple / đồng bộ:** đồng bộ đơn vị → server webhook biết đơn vị nào dùng webhook FB (định tuyến/cấp phát).

## Lỗi đã biết / lưu ý kỹ thuật
- `ServerDonViCauHinhController.cs:38-39,53-54,69-70,84-85` — `[SBAuthorize(key,...)]` từng action bị comment; chỉ còn `[SBAuthorize]` controller-level → không phân quyền chi tiết theo thao tác.
- `ServerDonViCauHinhController.cs:55-56` — `TimTheoDieuKienAsync` gắn đồng thời route `TimTheoDieuKienAsync` và `TimTheoDieuKien` (2 đường dẫn → cùng 1 action).
