---
controller: ContactController
module: he-thong
service: AuthenPartner
man_hinh_ui: "Nội bộ — không có UI. API cho đối tác tra danh sách contact/khách hàng theo điều kiện (lọc, phân trang)."
muc_dich: "Cổng API đối tác để tìm danh sách contact theo điều kiện (chi nhánh, tác nghiệp, keyword, ngày...) — tìm bằng ElasticSearch hoặc SQL store tùy cấu hình đơn vị; trả kèm thống kê tác nghiệp."
alias: ["api lấy contact đối tác", "tra contact partner", "GetContactByConditions", "danh sách contact cho đối tác"]
auth: internal
state_machine: []
entities: [Contact, DMTacNghiep, ChiNhanh]
stored_procs: [PRC_SCRM_Contact_Contact_DanhSachContactSale_Search, PRC_SCRM_Contact_GetListByIds]
deprecated: false
depends: ["SBAuthorizePartner (JWT partner + CauHinh AccessToken/DeviceId + IsSuDungApiGetData)", "Contact service (CauHinh/GetCHPrivate, Common/GetDMTacNghiepPrivate, UpdateKetQuaTacNghiep...)", "ElasticSearch (index ContactES)", "SQL Server (SqlSugar store PRC_SCRM_Contact_*)", "MongoDB", "Redis (throttle 60s/lần)"]
cross_module_in: ["Hệ thống đối tác → POST GetContactByConditions (qua JWT partner)"]
cross_module_out: ["Contact service (lấy cấu hình + DM tác nghiệp)", "ElasticSearch / SQL Contact (đọc danh sách)"]
---

# ContactController — API tra danh sách contact cho đối tác (AuthenPartner)

Controller AuthenPartner cho phép đối tác tra **danh sách contact (khách hàng tiềm năng)** theo điều kiện. Kế thừa `BasePartnerController`, gắn `[SBAuthorizePartner]` (xem mô hình auth đối tác ở node `CommonController_AuthenPartner`). `CurrentUserPartner.DonViId` lấy từ token và truyền xuống service → cách ly theo đơn vị.

Throttle 60s/lần qua `CommonCacheService`. `PageSize` bị **kẹp tối đa 100** (controller). Logic chính nằm ở `ContactService.GetContactByConditions`: bắt buộc chọn chi nhánh; lấy cấu hình contact của đơn vị (`CauHinh_Contact_Dto`); chọn nguồn tìm kiếm **ElasticSearch** (mặc định) hoặc **SQL store** tùy cờ `Allows_Search_Contact_By_SQL` của super admin; trả về list contact + nhóm thống kê theo "tác nghiệp cần". SĐT khách bị che (`ConvertPhoneNumber(true,...)`) khi user role sale/marketing xem số không thuộc mình.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm contact theo điều kiện | (API) | POST `api/Contact/GetContactByConditions` | — | internal `[SBAuthorizePartner]` | — | Throttle 60s → kẹp `PageSize<=100` → service: bắt buộc `IdChiNhanh` (≠ Guid.Empty), lấy `CauHinh/GetCHPrivate`, build query ES hoặc SQL store, trả list contact + `LayTacNghiepCanResponses` (thống kê) + totalRecord. | ES hoặc SP `PRC_SCRM_Contact_Contact_DanhSachContactSale_Search` / `PRC_SCRM_Contact_GetListByIds` | Thiếu chi nhánh → "Hãy chọn chi nhánh!". `DenNgay` bị `.ToMaxMinutes()` (bắt buộc có ngày) |

## Quy tắc nghiệp vụ (gating)
1. Auth `SBAuthorizePartner` (JWT + CauHinh AccessToken/DeviceId + `IsSuDungApiGetData`).
2. Throttle: gọi lại trong 60s → lỗi "Bạn cần chờ Ns...".
3. `gridInfo.IdChiNhanh` rỗng/Guid.Empty → "Hãy chọn chi nhánh!".
4. `PageSize > 100` → ép về 100 (controller).
5. Nguồn dữ liệu: `CauHinhSettingSuperAdminDto.Allows_Search_Contact_By_SQL == false` → ElasticSearch; ngược lại → SQL store. Khi ES bật `Allows_Return_Contact_Results_ElasticSearch_From_SQL` thì lấy id từ ES rồi enrich bằng SQL.
6. `ConfigSearchIgnoreAllFilter == true` (và chi nhánh không nằm trong danh sách loại trừ) + có keyword → bỏ qua mọi filter, chỉ search keyword.
7. Che SĐT: user là TruongNhom/TruongPhongBan/Sale/Marketing và `SaleUserId != user.Id` → SĐT bị che.

## Quan hệ
- **Gọi sang:** Contact service (`CauHinh/GetCHPrivate`, `Common/GetDMTacNghiepPrivate`); ElasticSearch (`ContactES`); SQL Contact store; MongoDB (enrich, hiện phần lớn đã comment).
- **Dữ liệu (entities):** `Contact` (read), `DMTacNghiep`, `ChiNhanh`.
- **Ripple / đồng bộ:** chỉ đọc — không ghi.

## Lỗi đã biết / lưu ý kỹ thuật
- `ContactService.cs:783` — `model.DenNgay.Value.ToMaxMinutes()` không null-check `DenNgay` → nếu đối tác không gửi `DenNgay` sẽ NullReferenceException (nhánh ES). Tương tự `TuNgay`/`DenNgay` parse ở dòng 850-851.
- `ContactService.cs:355-357` — gán `strIdUserSale = idUserSaleString` chỉ khi `idNhomSaleString` không rỗng (điều kiện chéo nhóm/user) → lọc theo user sale có thể bị bỏ nếu không truyền nhóm.
- `ContactService.cs:432-435` — nhánh SQL store nuốt exception, trả "Lỗi lấy thông tin!" + err 500; phần lớn logic enrich sản phẩm/mongo đã bị comment → response không trả `SanPhamInfo`.
- Throttle key gồm `Id` user nhưng partner `Id=Guid.Empty` → throttle thực chất theo đơn vị (chung cho mọi request đối tác cùng đơn vị).
