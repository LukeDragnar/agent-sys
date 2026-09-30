---
controller: CommonController
module: he-thong
service: Core
man_hinh_ui: "nội bộ — không có UI riêng; cung cấp dropdown/danh mục dùng chung cho nhiều màn hình (nhân viên, phòng ban, nhóm, địa chỉ, ngân hàng, đối tác giao vận)"
muc_dich: "Controller dùng chung (Common_Core_Controller): tổng hợp danh mục/dropdown địa chỉ, nhân viên, phòng ban-nhóm, đối tác giao vận, cấu hình cột, địa chỉ hệ thống... cho toàn FE/private."
alias: ["common", "danh mục dùng chung", "dropdown", "địa chỉ", "address system"]
auth: mixed
state_machine: []
deprecated: false
entities: [AddressSystem, NhanVien, PhongBan, NhomNhanVien, DonVi]
stored_procs: []
depends: ["ICommonService", "IAddressSystemService", "IDeliveryPropertyService", "IPhanQuyenUserPhongBanService", "IDMDauSoService", "ITicketInfoService", "IConfiguration"]
cross_module_in: ["Hầu hết FE module gọi CommonController để đổ dropdown nhân viên/phòng ban/nhóm/địa chỉ", "Module khác gọi private (GetThongTinDiaChi_Many, ConfigSystemLayTheoMa, DoProcessTicket, GetCache_DonVi...) qua [PrivateSBAuthorize]"]
cross_module_out: ["Đồng bộ địa chỉ từ DeliveryConnect sang CRM (Private_SyncAddressByDeliveryConnect)", "Set Redis địa chỉ theo quốc gia (SetRedisAllAddressByNationalCode)"]
---

# CommonController — Service dùng chung (danh mục / dropdown / địa chỉ)

Controller "kho dropdown" cấp service Core (`api/Common/...`), mặc định `[SBAuthorize()]` mức controller. Tập hợp endpoint phục vụ nhiều màn hình: danh mục địa chỉ (quốc gia → tỉnh → quận → phường), nhân viên/sale/marketing theo cơ cấu, phòng ban-nhóm, đối tác/trạng thái giao vận, cấu hình cột (config column), địa chỉ hệ thống (AddressSystem). Ủy quyền cho nhiều service: `ICommonService` (chính), `IAddressSystemService`, `IDeliveryPropertyService`, `IPhanQuyenUserPhongBanService`, `IDMDauSoService`, `ITicketInfoService`.

> Đây là controller **rất lớn** (>~80 endpoint). Bảng dưới gom theo NHÓM mục đích; phần private gom ở mục riêng.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Danh mục địa chỉ phân cấp | Combobox tỉnh/quận/phường | GET `LayQuocGia`, `LayTinhThanh/{idQuocGia}`, `LayQuanHuyen/{idTinhThanh}`, `LayPhuongXa/{idQuanHuyen}`, `LayNganhNghe/{id}`, `LayLinhVuc/{id}` | — | anonymous | — | Trả danh mục phân cấp địa chỉ/ngành nghề/lĩnh vực để đổ dropdown. | EF | — |
| 2 | Dropdown nhân viên/sale/marketing | Combobox nhân viên | GET `LayDsNhanVien`, POST `LayDsSaleOrNhanVien`, POST `GetDsNhanVienTheoDK`, POST `GetDsNhanVien`, `LayDsNhanVienGroupNhom`, `LayDsSaleLeader`, `LayDsSaleManager`, `DropdownListNhanVien`, `DropdownListNhomNhanVien(20250620)` | đa số — (`[SBAuthorize]`); một số có key (`CommonCoreController_DropdownListNhomNhanVienVaUser`...) | public | tùy role | Trả danh sách nhân viên/nhóm theo cơ cấu + quyền user hiện tại. | EF | nhiều endpoint trùng version (vd `DropdownListNhomNhanVien` vs `DropdownListNhomNhanVien20250620`) → cần biết FE dùng bản nào. |
| 3 | Dropdown phòng ban-nhóm-user theo role | Search autocomplete cơ cấu | POST `DropdownList_PhongBan_Nhom_User`, `DropdownList_PhongBan_Nhom_User_Sale_Maketing`, `DropdownList_PhongBan_Nhom_User_By_Role`, `CheckPermissionViewrReportContactWithMultiUser`, `CheckPermissionViewrReport_ELSE_WithMultiUser` | nhiều key `CommonCoreController_*` (danh sách role dài) | public | theo danh sách role lớn trong key | Trả cơ cấu chi nhánh→phòng ban→nhóm→user theo quyền; check quyền view report. | EF (qua `IPhanQuyenUserPhongBanService`) | danh sách role hardcode rất dài trong attribute → dễ lệch khi đổi role. |
| 4 | Danh mục ngân hàng / đối tác giao vận | Combobox | GET `GetListNganHang`, `GetListBankInfo`, `GetDanhSachCacBenGiaoVan`, `GetDanhSachDoiTacGiaoVan`, `GetDanhSachTrangThaiGiaoHangPrivate`, POST `GetDanhSachDoiTacGiaoVanTheoDonVi` | — | public | — | Trả danh sách ngân hàng / đối tác / trạng thái giao vận. | EF | — |
| 5 | Cấu hình cột grid | Tùy chỉnh cột | POST `UpdateConfigCollum`, `GetConfigCollum`, `ResetConfigColumn`, `ApplyConfigCollumForAll`, `UpdateConfigTabCustomer`, `GetConfigTabCustomer` | — (`[SBAuthorize]` mức controller) | public | — | Lưu/đọc/reset cấu hình cột hiển thị grid theo user; ApplyForAll áp cho toàn đơn vị. | EF | — |
| 6 | Địa chỉ hệ thống (AddressSystem) | quản trị địa chỉ | GET `GetListQuocGiaSystem`, `SA_GetAllAddressByLevel`, `SA_GetAddressByCode`, POST `SA_SaveAddressSystem`, `SA_DeleteAddressSystem`, `LayDiaChiCuThe`, `GetThongTinDiaChi` | — | public | — | Tra cứu/lưu/xóa dữ liệu địa chỉ chuẩn hệ thống theo level. | EF `AddressSystem` (qua `IAddressSystemService`) | — |
| 7 | Lấy đơn vị theo domain/mã | (khởi tạo theo tenant) | POST `LayDonViTheoDomain`, GET `LayDonViTheoMaDonVi/{maDonVi}`, POST `GetOrgById` | — | anonymous | — | Resolve đơn vị theo domain/mã (có xử lý map domain debug ↔ prod/staging/sprint). | EF | logic map domain debug hardcode theo ASPNETCORE_ENVIRONMENT (`CommonController.cs:195-209`). |

## Endpoint nội bộ (private / service-to-service)
- [internal] Tra cứu địa chỉ nhiều bản ghi: `GetFullDiaChi_Private`, `GetThongTinDiaChi_Many`, `GetThongTinDiaChiMultiData`, `CRM_GetAddressByLevelPrivate`, `SA_GetAllAddressByLevelPrivate`, `SA_GetAllAddressLevel1`, `GetListQuocGiaSystem_Private` — `[PrivateSBAuthorize]`.
- [internal] Ticket/khóa export: `DoProcessTicket`, `ReleaseLockedTicket`, `CheckUserExportExcelByDonVi`, `ReleaseUserExportExcel` (qua `ITicketInfoService`).
- [internal] Cache/cấu hình: `ConfigSystemLayTheoMa`, `GetCache_DonVi/{idDonVi}`, `SetRedisAllAddressByNationalCode/{nationalCode}`.
- [internal] Nhóm sale quản lý: `GetGroupStaffSaleUnderManagementAsync_Private`.
- [anonymous đặc thù] `Private_SyncAddressByDeliveryConnect` — đồng bộ địa chỉ từ DeliveryConnect, bảo vệ bằng mật khẩu thời gian `Sandbox@{day+month+year}` (`CommonController.cs:966-976`).

## Quan hệ
- **Gọi sang:** `ICommonService`, `IAddressSystemService`, `IDeliveryPropertyService`, `IPhanQuyenUserPhongBanService`, `IDMDauSoService`, `ITicketInfoService`.
- **Dữ liệu (entities):** `AddressSystem`, nhân viên/phòng ban/nhóm, `DonVi`.
- **Ripple / đồng bộ:** đồng bộ địa chỉ DeliveryConnect→CRM; set Redis địa chỉ.

## Lỗi đã biết / lưu ý kỹ thuật
- `CommonController.cs:810-844` — region "dùng để test" (`*_TETTO`) expose endpoint không attribute permission, lấy user theo `IdUserForTest` → debug-only, không nên có trên prod.
- `CommonController.cs:195-209` — map domain theo môi trường hardcode (DOMAIN_*_DEBUG, replace fullbooking.vn→sandbox.com.vn).
- `CommonController.cs:966-976` — `Private_SyncAddressByDeliveryConnect` AllowAnonymous, bảo vệ chỉ bằng password phái sinh từ ngày tháng.
- Nhiều cặp endpoint trùng version (`...`, `...20250620`) → rủi ro FE/BE lệch bản.
