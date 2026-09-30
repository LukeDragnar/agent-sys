---
controller: CommonController
module: ke-toan
service: Accounting
man_hinh_ui: "nội bộ — không có UI riêng. Endpoint helper/dropdown dùng chung trong service Accounting: enum (loại mua hàng, tình trạng, phương thức thanh toán...), danh mục kế toán/tài sản/mẫu HĐ, sinh mã phiếu. FE các màn kế toán gọi để fill combobox; 1 endpoint internal cho Order."
muc_dich: "Tập hợp endpoint dùng chung của Accounting: convert enum → dropdown, get danh mục (kế toán/tài khoản/tài sản cố định/mẫu hóa đơn/chứng từ), get tài khoản ngầm định theo loại chứng từ, sinh mã phiếu bán từ đơn hàng (internal cho Order)."
alias: ["common accounting", "dropdown kế toán", "danh mục chứng từ", "tài khoản ngầm định", "sinh mã phiếu bán", "helper kế toán", "GetDmChungTu"]
auth: mixed   # đa số SBAuthorize() (any login) + 1 PrivateSBAuthorize (internal)
state_machine: []
deprecated: false
entities: [DanhMucKeToan, TaiKhoanKeToan, TaiSanCoDinh, MauHoaDon, DmChungTu]
stored_procs: []
depends:
  - ICommonService
cross_module_in:
  - Order (Order.Service OtherService/AccountingService → GET SinhMaPhieuBanCommon)
cross_module_out: []
---

# CommonController (Accounting) — Helper / dropdown dùng chung của service Accounting

> ⚠️ **TÊN TRÙNG**: có 1 `CommonController` khác ở module ke-toan thuộc **service Invoice** (đã có node `CommonController.md`). Đây là bản thuộc **service Accounting** (`Accounting.API/Controllers/Common/CommonController.cs`) → node lưu tên `CommonController_Accounting.md`. Hai controller KHÁC NHAU (khác service, khác endpoint).
>
> Controller helper thuần: chủ yếu wrap `ICommonService.ConvertEnumToList<T>()` để trả dropdown enum, + một số get danh mục (kế toán/tài khoản/TSCĐ/mẫu HĐ/chứng từ) và tài khoản ngầm định. Phần lớn `[SBAuthorize()]` trống (any login); riêng `SinhMaPhieuBanCommon` là `[PrivateSBAuthorize()]` (chỉ service-to-service, Order gọi sang). Có attribute `[TienIch(...)]` ở class bị comment-out → controller không gắn tiện ích phân quyền.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Dropdown enum (nhóm) | combobox các màn kế toán | `POST api/Common/Dropdown{LoaiMuaHang \| LuaChonHoaDon \| TinhTrangPhieuMuaHang \| LoaiBanHang \| TinhTrangThanhToan \| TinhTrangHoaDon \| AccountingPhuongThucThanhToan}` | `SBAuthorize()` (any login) | mixed | fill combobox | Mỗi endpoint = `ConvertEnumToList<TEnum>()` → list {value, text} từ enum tương ứng. In-memory, không DB. | enum (in-memory) | — |
| 2 | Danh mục kế toán theo kiểu loại | combobox danh mục KT | `POST api/Common/DropdownDanhMucKeToanTheoKieuLoai` (param `kieuLoai`) | `SBAuthorize()` | mixed | — | `GetAllDanhMucKeToanTheoKieuLoai(kieuLoai)`. | **EF** DanhMucKeToan | — |
| 3 | Tìm danh mục kế toán | lưới/lookup danh mục KT | `POST api/Common/TimTheoDieuKien_DanhMucKeToan` (DanhMucKeToanBaseFilterDto) | `SBAuthorize()` | mixed | — | `TimTheoDieuKien_DanhMucKeToan(gridInfo)`. | **EF** | — |
| 4 | Get all TSCĐ / TK kế toán / Mẫu HĐ | combobox | `GET api/Common/{GetAllTaiSanCoDinh \| GetAllTaiKhoanKeToan \| GetAllMauHoaDon}` | `SBAuthorize()` | mixed | — | Get all danh mục tương ứng; `GetAllTaiKhoanKeToan` truyền `CurrentUser` (lọc theo đơn vị). | **EF** TaiSanCoDinh/TaiKhoanKeToan/MauHoaDon | — |
| 5 | Tìm theo điều kiện (TSCĐ/Mẫu HĐ/TK kế toán) | lookup | `POST api/Common/{TimTheoDieuKien_TaiSanCoDinh \| TimTheoDieuKien_DmMauHoaDon \| TimTheoDieuKien_DmTaiKhoanKeToan}` | `SBAuthorize()` | mixed | — | Search theo Filter DTO; `_DmTaiKhoanKeToan` có `CurrentUser`. | **EF** | — |
| 6 | Danh mục chứng từ + loại chứng từ | combobox cấu hình chứng từ | `GET api/Common/{GetDmChungTu \| GetLoaiChungTuByMa(maChungTu) \| GetLoaiChungTuNgamDinh(maChungTu,maLoaiChungTu,maLoaiQuy?) \| GetDanhMucCotTaiKhoan}` | `SBAuthorize()` | mixed | — | Lấy danh mục chứng từ / loại chứng từ theo mã / loại chứng từ ngầm định / danh mục cột tài khoản. | **EF** DmChungTu | — |
| 7 | Tài khoản ngầm định theo loại chứng từ | (fill TK mặc định khi lập phiếu) | `GET api/Common/GetTaiKhoanNgamDinhByLoaiChungTu(maChungTu, maLoaiChungTu?, maLoaiQuy?)` | `SBAuthorize()` | mixed | lập chứng từ/phiếu | `GetTaiKhoanNgamDinhByLoaiChungTu(...,CurrentUser)` → TK hạch toán mặc định. | **EF** | — |
| 8 | Sinh mã phiếu bán từ đơn hàng | (nội bộ — service Order gọi) | `GET api/Common/SinhMaPhieuBanCommon(type, id_don_vi, idDonHang?)` | — (internal) | internal | service-to-service | `[PrivateSBAuthorize()]`. `SinhMaPhieuFromDonHang((TypeSinhMaPhieuEnum)type, id_don_vi, idDonHang)` → sinh mã phiếu bán theo đơn hàng. Trả `Ok(rs)` (KHÔNG bọc ResponseResult). | **EF** sinh mã | trả `Ok(rs)` raw thay vì `ResponseResult` như các action khác → format response lệch chuẩn. |

## Endpoint nội bộ (private / service-to-service)
- [internal] **Order** (Order.Service `OtherService/AccountingService.SinhMaPhieuBanCommon`) gọi `GET /Common/SinhMaPhieuBanCommon?type=&id_don_vi=&idDonHang=` để sinh mã phiếu bán khi tạo phiếu bán tự động cho đơn hàng. Đây là endpoint duy nhất `[PrivateSBAuthorize()]`.

## Quan hệ
- **Gọi sang:** `ICommonService` (toàn bộ logic ủy thác về service); không gọi cross-service ra ngoài.
- **Dữ liệu (entities):** đọc danh mục DanhMucKeToan, TaiKhoanKeToan, TaiSanCoDinh, MauHoaDon, DmChungTu (read-only helper).
- **Ripple / đồng bộ:** KHÔNG bắn message/event. Chỉ phục vụ dropdown/lookup; được Order gọi sang lấy mã phiếu.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Accounting/Accounting.API/Controllers/Common/CommonController.cs`
  - L19 `[TienIchAttribute(...)]` ở class bị comment-out → không gắn tiện ích phân quyền; mọi action chỉ `[SBAuthorize()]` trống (any login) trừ #8.
  - L249-255 `SinhMaPhieuBanCommon` là `[PrivateSBAuthorize()]` và trả `Ok(rs)` thẳng (không qua `ResponseResult`) → response format khác các action còn lại.
  - **Trùng tên controller** với `CommonController` (service Invoice) trong cùng module ke-toan → node này đặt tên `CommonController_Accounting.md`.
