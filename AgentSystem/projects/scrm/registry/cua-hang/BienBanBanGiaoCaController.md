---
controller: BienBanBanGiaoCaController
module: cua-hang
service: WarehouseProduct
man_hinh_ui: "Cửa hàng > Biên bản bàn giao ca (route bien-ban-giao-ca, bien-ban-giao-ca.component) — list biên bản; popup edit (bien-ban-giao-ca-edit), nhập đầu ca (bien-ban-giao-ca-nhan-dau-ca), nhập cuối ca (bien-ban-giao-ca-nhan-cuoi-ca), xem chi tiết. Tiện ích thuộc nhóm TI_Order ('Đơn hàng post')."
muc_dich: "Quản lý biên bản bàn giao ca làm việc tại cửa hàng: tạo biên bản, ghi nhận người nhận + tổng sản phẩm/tiền đầu ca và cuối ca, xác nhận đầu/cuối ca, ghi nhận tiền thiếu cuối ca. Có vòng trạng thái xác nhận."
alias: ["biên bản bàn giao ca", "bàn giao ca", "giao ca", "đầu ca", "cuối ca", "xác nhận ca", "tiền thiếu cuối ca", "biên bản ca"]
auth: public               # toàn bộ [SBAuthorize(key, ..., roles)]; role gồm DV-AD-TONG/DV-AD-CN/HT-KT/DV-KT/DV-KT-TONG/DV-QLDV
state_machine: [Nhap, DaXacNhanDauCa, DaXacNhanCuoiCa]   # qua cờ IsXacNhanDauCa / IsXacNhanCuoiCa
deprecated: false
entities: [BienBanBanGiaoCa, BienBanBanGiaoCaChiTiet]
stored_procs: []
depends: [WarehouseProduct]
cross_module_in: []
cross_module_out: []
---

# BienBanBanGiaoCaController — Biên bản bàn giao ca

> Biên bản giao ca tại cửa hàng (`partial class`, tiện ích `TI_Order`). Vòng trạng thái qua 2 cờ: `IsXacNhanDauCa`, `IsXacNhanCuoiCa`. Tách rõ thao tác đầu ca vs cuối ca. Chi tiết sản phẩm/tiền ở entity `BienBanBanGiaoCaChiTiet` (controller riêng).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện (grid) | "Tìm kiếm" | POST `api/BienBanBanGiaoCa/TimTheoDieuKien` | `BienBanBanGiaoCaController_TimTheoDieuKienAsync` | public | màn list | List biên bản theo filter + CurrentUser | EF | - |
| 2 | Thêm mới biên bản | "Thêm mới" → bien-ban-giao-ca-edit | POST `api/BienBanBanGiaoCa/ThemMoi` | `BienBanBanGiaoCa_ThemMoi` | public | popup Thêm | Tạo biên bản; ghi `NguoiTaoBienBanUserId = CurrentUser` (dùng cho guard xóa) | EF | - |
| 3 | Tìm theo Id | mở popup | GET `api/BienBanBanGiaoCa/TimTheoId/{id:Guid}` | `BienBanBanGiaoCa_TimTheoId` | public | per-row | Trả biên bản theo id | EF | - |
| 4 | Tìm chi tiết theo Id | xem chi tiết | POST `api/BienBanBanGiaoCa/TimChiTietTheoId` | `BienBanBanGiaoCa_TimChiTietTheoId` | public | - | Trả biên bản kèm chi tiết theo `...TimChiTietTheoIdFilterDto` | EF | - |
| 5 | Cập nhật thông tin | "Lưu" | POST `api/BienBanBanGiaoCa/CapNhatThongTin` | `BienBanBanGiaoCa_CapNhatThongTin` | public | popup Sửa | Update biên bản theo `BienBanBanGiaoCaDto` | EF | - |
| 6 | Cập nhật người nhận ĐẦU ca | màn nhập đầu ca | POST `api/BienBanBanGiaoCa/CapNhatThongTinNguoiNhanDauCa` | `BienBanBanGiaoCa_CapNhatThongTinNguoiNhanDauCa` | public | đầu ca | Ghi người nhận đầu ca theo `CapNhatThongTinNguoiNhanDauCaDto` | EF | - |
| 7 | Cập nhật người nhận CUỐI ca | màn nhập cuối ca | POST `api/BienBanBanGiaoCa/CapNhatThongTinNguoiNhanCuoiCa` | `BienBanBanGiaoCa_CapNhatThongTinNguoiNhanCuoiCa` | public | cuối ca | Ghi người nhận cuối ca theo `CapNhatThongTinNguoiNhanCuoiCaDto` | EF | nhãn quyền ghi "Thêm mới người nhận cuối ca" (copy từ đầu ca) |
| 8 | Thêm chi tiết (danh sách) | thêm SP vào biên bản | POST `api/BienBanBanGiaoCa/ThemMoiChiTietAsync` | `BienBanBanGiaoCa_ThemMoiChiTietAsync` | public | - | Thêm danh sách chi tiết SP nhận theo `ThemMoiDsBienBanBanGiaoCaChiTietDto` | EF | - |
| 9 | Xác nhận ĐẦU ca | nút "Xác nhận đầu ca" | POST `api/BienBanBanGiaoCa/UpdateIsXacNhanDauCa` | `BienBanBanGiaoCa_UpdateIsXacNhanDauCa` | public | trạng thái Nhap | Set `IsXacNhanDauCa = true` → khóa sửa/xóa phần đầu ca | EF | - |
| 10 | Xác nhận CUỐI ca | nút "Xác nhận cuối ca" | POST `api/BienBanBanGiaoCa/UpdateIsXacNhanCuoiCa` | `BienBanBanGiaoCa_UpdateIsXacNhanCuoiCa` | public | sau đầu ca | Set `IsXacNhanCuoiCa = true` → đóng ca | EF | - |
| 11 | Set tổng SP nhận theo kiểu dữ liệu | (tính tổng) | POST `api/BienBanBanGiaoCa/SetTongSanPhamNhanTheoIdVaKieuDuLieu` | `BienBanBanGiaoCa_SetTongSanPhamNhanTheoIdVaKieuDuLieu` | public | - | Set tổng SP nhận theo id + kiểu dữ liệu (`SetDuLieuTheoIdVaKieuDuLieuFilterDto`) | EF | - |
| 12 | Cập nhật tiền thiếu cuối ca | màn cuối ca | POST `api/BienBanBanGiaoCa/CapNhatTienThieuCuoiCa` | `BienBanBanGiaoCa_CapNhatTienThieuCuoiCa` | public | cuối ca | Ghi tiền thiếu cuối ca theo `CapNhatTienThieuCuoiCaInput` | EF | nhãn quyền ghi "Thêm mới người nhận cuối ca" (copy-paste sai) |
| 13 | Xóa biên bản | "Xóa" | POST `api/BienBanBanGiaoCa/Xoa/{id:Guid}` | `BienBanBanGiaoCa_Xoa` | public | chỉ khi CHƯA xác nhận | Guard tenant (`IdDonVi == CurrentUser.DonViId`); **chặn xóa nếu** `IsXacNhanDauCa==true` hoặc `IsXacNhanCuoiCa==true` ("Không thể xóa biên bản đã được xác nhận!"); chỉ **người tạo** (`NguoiTaoBienBanUserId == CurrentUser.Id`) hoặc super user mới được xóa | EF | nhiều tầng guard — xem Quy tắc nghiệp vụ |

## Quy tắc nghiệp vụ (gating)
- **Xóa biên bản** (controller:92-110), thứ tự kiểm tra:
  1. `data == null || IdDonVi != CurrentUser.DonViId` → lỗi NotFound (guard tenant).
  2. `IsXacNhanDauCa == true || IsXacNhanCuoiCa == true` → lỗi "Không thể xóa biên bản đã được xác nhận!".
  3. `NguoiTaoBienBanUserId == CurrentUser.Id || CurrentUser.IsSuperUser` → cho xóa; ngược lại lỗi "Người tạo phiếu mới có thể xóa!".
  - (Block check `IsSuperUser` ở đầu method đang bị comment — chỉ áp 3 điều kiện trên.)
- **Vòng trạng thái:** Nhap → (UpdateIsXacNhanDauCa) DaXacNhanDauCa → (UpdateIsXacNhanCuoiCa) DaXacNhanCuoiCa. Đã xác nhận → khóa xóa (và *cần xác nhận* khóa sửa các field tương ứng trong service).

## Quan hệ
- **Gọi sang:** `IBienBanBanGiaoCaService` (toàn bộ).
- **Dữ liệu (entities):** `BienBanBanGiaoCa` (header: IsXacNhanDauCa, IsXacNhanCuoiCa, NguoiTaoBienBanUserId, IdDonVi, tiền thiếu cuối ca), `BienBanBanGiaoCaChiTiet` (chi tiết SP nhận — controller riêng).
- **Ripple / đồng bộ:** chi tiết quản ở `BienBanBanGiaoCaChiTietController`. *Cần xác nhận* biên bản có đối chiếu với doanh thu/đơn hàng trong ca không.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `WarehouseProduct/WarehouseProduct.API/Controllers/BienBanBanGiaoCaController.cs`
  - L96-99: block guard `IsSuperUser` ở đầu `Xoa` bị comment — chỉ còn 3 điều kiện guard sau đó.
  - L132 nhãn quyền `CapNhatTienThieuCuoiCa` = "Thêm mới người nhận cuối ca" (copy-paste sai); L54 `CapNhatThongTinNguoiNhanCuoiCa` cũng dùng "Thêm mới người nhận cuối ca" — chỉ label.
  - `[TienIchAttribute(..., "Đơn hàng post", MaTienIchSCRM.TI_Order)]` (controller:13) — phân nhóm vào TI_Order dù entity nằm ở service WarehouseProduct; mô tả tiện ích chung chung ("Đơn hàng post") — lưu ý khi map menu.
  - `partial class` — *cần xác nhận* không có partial bổ sung.
