---
controller: CommonController
module: sales
service: Contact
man_hinh_ui: "Dùng chung — cung cấp dropdown/combobox cho các màn Contact, Landing, cấu hình chia số, thống kê (không phải 1 màn riêng)"
muc_dich: "Tập hợp API dropdown/lookup dùng chung cho module Contact: danh sách landing, danh mục tác nghiệp/kết quả/sau bao lâu, kênh tiếp nhận/quảng cáo, enum cấu hình chia số & kho số, user MKT theo chi nhánh, các bộ lọc thống kê."
alias: [Common, "dropdown chung", "combo chung", lookup]
auth: mixed
state_machine: []
deprecated: false
entities: [SanPhamLanding, DMTacNghiep, DMKetQuaTacNghiep, KenhTiepNhan]
stored_procs: []
depends: [CommonService, "CauHinhService (đọc IsEnableTacNghiepTheoChiNhanh để lọc theo chi nhánh)", "Redis (user MKT theo chi nhánh)"]
cross_module_in: ["service nội bộ (DropdownListLandingPrivateByIdChiNhanh, GetDMTacNghiepPrivate, DropdownDMTacNghiepByXepHang)"]
cross_module_out: []
---

# Common — Dropdown / lookup dùng chung Contact

> Controller-level `[SBAuthorize()]` (mặc định cần login). Hầu hết endpoint là GET trả danh sách cho combobox; gom theo nhóm mục đích.

## Hành động (UI / công khai)
| # | Nhóm hành động | Endpoint (HTTP path) | Quyền (key) | Auth | Hành vi đúng (expected) | Nguồn | Known-issue |
|---|---|---|---|---|---|---|---|
| 1 | Dropdown landing (sale/MKT/contact) | POST `DropdownListLanding`, `DropdownListLandingSaleDanhSachContact`, `DropdownListLandingMktDanhSachContact`, `DropdownLanding`, POST `DropdownListLandingByIdChiNhanh/{idChiNhanh}` | `[SBAuthorize()]` (login) | public | Trả danh sách landing để lọc theo ngữ cảnh (toàn đơn vị / theo chi nhánh / cho sale / cho MKT). | EF | — |
| 2 | Danh mục tác nghiệp (DM tác nghiệp / all / kết quả / sau bao lâu / cấu hình) | GET `DropdownDMTacNghiep`, `DropdownDMTacNghiepAll`, `DropdownDMKetQuaTacNghiep`, `DropdownDMTacNghiepSauBaoLau`, `DropdownDMTacNghiepCauHinh` | `[SBAuthorize()]` (login) | public | Đọc cấu hình `IsEnableTacNghiepTheoChiNhanh`; nếu bật → lọc theo `CurrentUser.IdChiNhanh`, ngược lại lấy toàn đơn vị. | EF | — |
| 3 | Enum thống kê (kiểu ngày / tác nghiệp / loại khách / điều kiện / loại kết nối / kênh QC / loại order / doanh số) | GET `DropdownKieuNgayThongKe`, `DropdownTacNghiepThongKe`, `DropdownLoaiKhachThongKe`, `DropdownContactDieuKienThongKe`, `DropdownLoaiKetNoi`, `DropdownKenhQuangCao`, `DropdownLoaiOrderThongKe`, `DropdownLoaiDoanhSoThongKe` | `[SBAuthorize()]` (login) — riêng `DropdownKieuNgayThongKe` `[AllowAnonymous]` | mixed | Trả enum/danh sách cho bộ lọc thống kê. | Enum/EF | `DropdownKieuNgayThongKe` mở `[AllowAnonymous]` |
| 4 | Enum cấu hình chia số | GET `DropdownCachChiaSo`, `DropdownCachChiaHanhDongTiep`, `DropdownSM_CachChia`, `DropdownSMT_SKCT_CachChia`, `DropdownSKC_CachChia`, `DropdownSCS_SCSL_CachChia`, `DropdownSoTMDT_CachChia`, `DropdownSoVerified_CachChia`, `DropdownKieuSoThayDoi/{kieuSoMacDinh}`, `DropdownActionKhongTimThayTaiKhoan`, `DropdownActionTaiKhoanBiKhoa`, `DropdownActionKhong_CauHinh_NhanData_TrongCaLamViec` | `[SBAuthorize]` (login) | public | Trả enum phục vụ màn cấu hình chia số (cách chia theo từng loại số). | Enum | — |
| 5 | Enum cấu hình kho số | GET `DropdownThietLapDuaThangSoVeKhoSo`, `DropdownKieuSoApDungChiaThangVeKhoSo` | `[SBAuthorize]` (login) | public | Build từ enum (`EnumThietLapDuaThangSoVeKhoSo` / `EnumKieuSoApDungChiaThangVeKhoSo`) → `DropdownStruct`. | Enum | — |
| 6 | Kênh tiếp nhận | GET `DropdownKenhTiepNhan` | `[SBAuthorize]` (login) | public | Danh sách kênh tiếp nhận theo đơn vị user. | EF | — |
| 7 | User MKT theo chi nhánh | GET `DropdownUserMktTheoChiNhanh` | `[SBAuthorize]` (login) | public | Lấy user MKT theo (DonViId, IdChiNhanh) **từ cache Redis**. | Redis | — |

## Endpoint nội bộ (private / service-to-service)
- `[PrivateSBAuthorize]` `DropdownListLandingPrivateByIdChiNhanh/{idChiNhanh}` — service khác lấy landing theo chi nhánh.
- `[PrivateSBAuthorize]` `GetDMTacNghiepPrivate(idDonVi, idChiNhanh?)` — trả `List<DMTacNghiep>` theo đơn vị/chi nhánh.
- `[PrivateSBAuthorize]` `DropdownDMTacNghiepByXepHang/{typeLoaiNhanVien}` — DM tác nghiệp lọc theo xếp hạng nhân viên (vẫn áp filter chi nhánh theo cấu hình).

## Quan hệ
- **Gọi sang:** `CommonService` (toàn bộ dropdown), `CauHinhService.GetAllAsync` (đọc cờ `IsEnableTacNghiepTheoChiNhanh` quyết định lọc theo chi nhánh), Redis (user MKT cache).
- **Dữ liệu (entities):** SanPhamLanding, DMTacNghiep, DMKetQuaTacNghiep, KenhTiepNhan + nhiều enum cấu hình.
- **Ripple / đồng bộ:** chỉ đọc — không ghi.

## Lỗi đã biết / lưu ý kỹ thuật
- `CommonController.cs:168` — `DropdownKieuNgayThongKe` `[AllowAnonymous]` (key role gốc đã comment) → lộ public dù chỉ là enum tĩnh, không nhạy cảm.
- Nhiều endpoint (mục 2/3) gọi `CauHinhService.GetAllAsync(CurrentUser)` lặp lại chỉ để lấy 1 cờ `IsEnableTacNghiepTheoChiNhanh` → có thể tối ưu cache nếu hot path.
