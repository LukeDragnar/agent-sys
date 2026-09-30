---
controller: DonViController
module: he-thong
service: Core
man_hinh_ui: "Cấu hình hệ thống > Đơn vị / Thông tin đơn vị (bản app: thông tin đơn vị của user); bản QuanTriArea: Quản trị > Đơn vị (HT-AD/HT-KT/HT-CSKH CRUD toàn hệ thống + duyệt mã, gói sử dụng, merchant)"
muc_dich: "CRUD + tra cứu đơn vị (tenant). Có 2 class trùng tên: bản app (route api/DonVi, thao tác đơn vị của user) và bản quản trị (route api/QuanTriArea/DonVi, admin hệ thống quản lý mọi đơn vị + duyệt mã/gói/merchant)."
alias: ["đơn vị", "DonVi", "tenant", "tổ chức", "org"]
auth: mixed
state_machine: []
deprecated: false
entities: [DonVi]
stored_procs: []
depends: ["IDonViService", "IDonViKhoiTaoServiceService"]
cross_module_in: ["FE Thông tin đơn vị gọi GetDonViById/CapNhatThongTin", "FE Quản trị đơn vị gọi CRUD + duyệt mã + gói sử dụng + merchant", "Module khác gọi private (GetDonViByListIds, GetDonViById/{id}, GetAllIdDonVi, GetTenMienDanhGiaByListId...) để map thông tin đơn vị"]
cross_module_out: ["Tạo đơn vị mới qua IDonViKhoiTaoServiceService (khởi tạo dữ liệu tenant)", "Yêu cầu/duyệt thay đổi gói sử dụng đơn vị"]
---

# DonViController — Quản lý đơn vị (tenant)

Có **2 class cùng tên `DonViController`** trong service Core:
- **Bản app** (`Controllers/DonViController.cs`, `[TienIch(..., TI_Chung)]`, route `api/DonVi/...`): cho user thao tác trên đơn vị của chính mình — lấy thông tin đơn vị (`GetDonViById`, `GetDonViByIdForEdit` không lấy cache), cập nhật, gen code, lấy lĩnh vực, + nhiều endpoint private cho service khác.
- **Bản quản trị** (`Areas/SanboxArea/DonViController.cs`, `[TienIch(..., TI_HeThong, true)]`, route `api/QuanTriArea/DonVi/...`): admin hệ thống (HT-AD, HT-KT, HT-CSKH) CRUD toàn bộ đơn vị, xuất Excel, duyệt mã, quản lý gói sử dụng & merchant.

Cả hai dùng `IDonViService` + `IDonViKhoiTaoServiceService`. `ThemMoi` luôn đi qua `IDonViKhoiTaoServiceService.ThemMoiAsync` (khởi tạo tenant); bản app `ThemMoi` là `[AllowAnonymous]` (đăng ký đơn vị mới).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy thông tin đơn vị của user | (header/cấu hình đơn vị) | GET `api/DonVi/GetDonViById`, `GetDonViByIdForEdit`, `GetInfoDonViBasic/{id}`, POST `GetInfoBasicByIds`, GET `GetAllInfoBasic` | — (`[SBAuthorize]`) | public | — | Trả thông tin đơn vị hiện tại của user. `GetDonViByIdForEdit` lấy KHÔNG qua cache (để sửa). `GetInfoBasicByIds`: non-super/non-kế-toán-hệ-thống bị lọc chỉ còn đơn vị của mình. | EF `DonVi` | — |
| 2 | Tìm theo điều kiện (toàn hệ thống) | Grid/Lọc | POST `api/QuanTriArea/DonVi/TimTheoDieuKien`, GET `.../LayTatca` | `DonVi_TimTheoDieuKien` / `DonVi_LayTatCa` | public (HT-AD/HT-KT/HT-CSKH) | — | Lọc/lấy toàn bộ đơn vị — chỉ bản quản trị. | EF `DonVi` | — |
| 3 | Xem chi tiết (quản trị) | Mở chi tiết | GET `api/QuanTriArea/DonVi/TimTheoId/{id}` | `DonVi_TimTheoId` | public | — | `TimTheoIdAsyncNoCache(id)` (chỉnh sửa không lấy cache). | EF `DonVi` | — |
| 4 | Xuất Excel | Xuất Excel | POST `api/QuanTriArea/DonVi/ExportExcel` | — (`[SBAuthorize]`) | public | — | Xuất toàn bộ đơn vị theo điều kiện (GemBox), trả file `DanhSachDonVi.xlsx`. | EF `DonVi` | endpoint export chỉ `[SBAuthorize]` không key riêng (comment key đã bỏ). |
| 5 | Thêm/Sửa/Xóa đơn vị | Thêm/Sửa/Xóa | POST `api/DonVi/ThemMoi` (anonymous), `api/QuanTriArea/DonVi/ThemMoi`, `.../CapNhatThongTin`, `.../Xoa/{id}` | bản quản trị: `DonVi_ThemMoi`/`DonVi_CapNhatThongTin`/`DonVi_Xoa`; bản app CapNhat: `DonVi_CapNhatThongTin` | mixed | — | ThemMoi → khởi tạo tenant qua `IDonViKhoiTaoServiceService`. Bản app ThemMoi `[AllowAnonymous]` (đăng ký mới). | EF `DonVi` | bản app `ThemMoi` là AllowAnonymous → đăng ký đơn vị không cần auth, cần xác minh chống lạm dụng. |
| 6 | Gen / Duyệt mã đơn vị | Tạo mã / Duyệt mã | POST `.../GenCode` (cả 2), POST `api/QuanTriArea/DonVi/ApproveCode` | `DonVi_GenCode` / `DonVi_ApproveCode` | public | — | Sinh mã đơn vị; duyệt/hủy mã (quản trị). | EF `DonVi` | — |
| 7 | Gói sử dụng & Merchant (quản trị) | quản lý gói/merchant | GET `GetGoiVaUserByDonVi/{id}`, POST `YeuCauThayDoiGoiSuDungDonVi`, GET `GetDonViMerchant/{id}`, POST `UpdateDonViMerchant` | `DonVi_GetGoiVaUserByDonVi`, `DonVi_YeuCauThayDoiGoiSuDungDonVi`, `DonVi_GetDonViMerchant`, `DonVi_UpdateDonViMerchant` | public | — | Xem gói + user của đơn vị; yêu cầu đổi gói; xem/đổi merchant. | EF `DonVi` | — |
| 8 | Tìm theo keyword | Search | POST `api/QuanTriArea/DonVi/SearchByKeyword` | `DonVi_SearchByKeyword` | public | ModelState valid | Search nhanh đơn vị theo keyword. | EF `DonVi` | — |
| 9 | Lĩnh vực / role đơn vị | — | GET `api/DonVi/GetLinhVucByUser`, POST `GetUnitByRole` | — (`[SBAuthorize]`) | public | — | Lấy lĩnh vực user; lấy đơn vị theo role (dùng ở account SMS). | EF | — |

## Endpoint nội bộ (private / service-to-service)
- [internal] `api/DonVi/GetDonViByListIds`, `GetDonViById/{id}`, `GetAllIdDonVi`, `GetTenMienDanhGiaByListId` — `[PrivateSBAuthorize]`: module khác tra thông tin đơn vị / danh sách id / tên miền đánh giá.

## Quan hệ
- **Gọi sang:** `IDonViService` (CRUD/tra cứu), `IDonViKhoiTaoServiceService` (khởi tạo tenant khi ThemMoi).
- **Dữ liệu (entities):** `DonVi`. DTO/Req: `DonViCreateReq`, `DonViFilterReq`, `DonViGenCodeReq`, `DonViCodeApproveReq`, `DonViSearchDto`, `YeuCauDoiGoiSuDungDonViDto`, `UpdateDonViMerchant`, `DonViViewBasicDto`.
- **Ripple / đồng bộ:** tạo đơn vị → khởi tạo dữ liệu tenant; yêu cầu/duyệt đổi gói sử dụng.

## Lỗi đã biết / lưu ý kỹ thuật
- `Controllers/DonViController.cs:104-109` — `ThemMoi` `[AllowAnonymous]` (đăng ký đơn vị không cần auth).
- `Areas/SanboxArea/DonViController.cs:64-65` — `ExportExcel` đã bỏ permission key (chỉ `[SBAuthorize]`).
- Bản quản trị vẫn có endpoint comment-out (`DisableMultiRole`) — không active.
