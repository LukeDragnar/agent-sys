---
controller: TaiKhoanTienMatController
module: ke-toan
service: Accounting
man_hinh_ui: "Kế toán > Cấu hình quỹ/tài khoản tiền mặt: lưới list (lọc Chi nhánh, TK đơn vị, Keyword) + Thêm/Sửa/Xóa từng tài khoản tiền mặt. Mỗi bản ghi = 1 tài khoản tiền mặt gắn phương thức thanh toán (maLoaiQuy='TM'), có cờ thu/chi mặc định."
muc_dich: "CRUD danh mục TÀI KHOẢN tiền mặt (quỹ TM) theo đơn vị × chi nhánh: map phương thức thanh toán loại quỹ 'TM' → tài khoản kế toán, đặt thu/chi mặc định. Cung cấp danh sách TK tiền mặt cho luồng thu/chi & bán hàng (service khác gọi sang)."
alias: ["tài khoản tiền mặt", "tai khoan tien mat", "quỹ tiền mặt", "TK tiền mặt", "thu chi tiền mặt", "phương thức thanh toán tiền mặt", "TM"]
auth: internal
state_machine: []
deprecated: false
entities: [TaiKhoanTienMat]
stored_procs: []
depends:
  - ITaiKhoanTienMatService
  - Core/DMPhuongThucThanhToan/GetDMPhuongThucThanhToan (maLoaiQuy=TM)  # qua IClientHelper
  - IRedisCacheService (cache key GetDSTaiKhoanTienMatByChiNhanh_{donVi}_{chiNhanh})
cross_module_in:
  - Branch (Branch.Service CallService/AccountingService → GET GetDSTaiKhoanTienMatByChiNhanh)
cross_module_out:
  - Core (gọi sang DMPhuongThucThanhToan để build danh mục)
---

# TaiKhoanTienMatController — Cấu hình tài khoản / quỹ tiền mặt

> CRUD danh mục **TÀI KHOẢN tiền mặt** thuần (KHÔNG quản số dư/giao dịch quỹ tại đây — chỉ là cấu hình map phương thức thanh toán loại quỹ 'TM' ↔ tài khoản kế toán). Mọi bản ghi gắn `IdDonVi = userLogin.DonViId` (multi-tenant) + `IdChiNhanh`. Cờ `IsThuMacDinh`/`IsChiMacDinh` là duy nhất trong đơn vị: set true → clear cờ tương ứng ở mọi bản ghi khác cùng đơn vị. Endpoint dropdown `GetDSTaiKhoanTienMatByChiNhanh` gọi Core lấy danh mục phương thức (loại quỹ 'TM') rồi LEFT JOIN với TK đã cấu hình → trả full list (chưa cấu hình thì `IdTaiKhoan=Guid.Empty` để FE tự tích). **Controller-level mọi endpoint = `[SBAuthorize()]` trống (chỉ cần đăng nhập, không key/role).**

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm kiếm / load lưới | Lưới list + lọc (Chi nhánh, TK đơn vị, Keyword) | `POST api/TaiKhoanTienMat/TimTheoDieuKien` | `SBAuthorize()` (any login) | internal | màn cấu hình quỹ tiền mặt | Lọc `IdDonVi=userLogin.DonViId`; optional theo `IdChiNhanh`, `IsTkDonVi==true`, Keyword (Contains MaTaiKhoan/TenTaiKhoan). Order `NgayCapNhat DESC`, paging Skip/Take. Trả items + totalRecords. | **EF** `TaiKhoanTienMat` | — |
| 2 | Xem chi tiết 1 TK | (mở form sửa) | `POST api/TaiKhoanTienMat/GetByIdAsync` | `SBAuthorize()` | internal | chọn 1 dòng | Lấy theo `Id && IdDonVi`; null → "Đối tượng không tồn tai." | **EF** | — |
| 3 | Thêm mới | Nút "Thêm" / Lưu | `POST api/TaiKhoanTienMat/CreateAsync` | `SBAuthorize()` | internal | nút Thêm | Validate (xem mục Quy tắc). Nếu `IsThuMacDinh=true` → clear cờ thu của mọi TK cùng đơn vị; tương tự `IsChiMacDinh`. Insert mới `IdDonVi=userLogin.DonViId`. Clear redis cache dropdown. | **EF** Insert | — |
| 4 | Cập nhật | Nút "Sửa" / Lưu | `POST api/TaiKhoanTienMat/UpdateAsync` | `SBAuthorize()` | internal | dòng đã có | Validate (isUpdate, bỏ qua trùng chính nó). Tìm theo `Id` (KHÔNG lọc IdDonVi — xem known-issue). Clear cờ thu/chi mặc định khác nếu set true. Cập nhật field + MarkDirty. Clear cache. | **EF** Update | (a) `existing` truy theo `Id` thôi, **không kiểm IdDonVi** → user đơn vị A có thể update bản ghi đơn vị khác nếu biết Id. (b) Update set `IdLoaiQuy` từ reqData nhưng Create KHÔNG set IdLoaiQuy → lệch giữa 2 luồng. |
| 5 | Xóa | Nút "Xóa" / confirm | `POST api/TaiKhoanTienMat/DeleteAsync` | `SBAuthorize()` | internal | mỗi dòng | Tìm theo `Id && IdDonVi`; null → "Không tìm thấy dữ liệu cần xóa."; OK → `DeleteAsync(entity)` + clear cache. | **EF** Delete | — |
| 6 | Dropdown TK tiền mặt theo chi nhánh | (combobox chọn TK khi thu/chi/bán hàng) | `GET api/TaiKhoanTienMat/GetDSTaiKhoanTienMatByChiNhanh` | `SBAuthorize()` | internal | form chọn phương thức TM | Gọi Core `DMPhuongThucThanhToan?maLoaiQuy=TM`. Lấy TK đã cấu hình ưu tiên theo `IdChiNhanh==userLogin.IdChiNhanh && SuDung`; nếu rỗng → fallback TK `IdChiNhanh==null`. LEFT JOIN danh mục Core với TK theo (MaLoaiQuy, MaPhuongThuc): chưa cấu hình → `IdTaiKhoan=Guid.Empty`, `MaTaiKhoan=MaPhuongThuc`, `TenTaiKhoan="TK {MaPhuongThuc}"`. | **EF** + Core HTTP | Core trả rỗng → trả `ResultWithData("Lỗi get danh sách...")` (string vào ô data, **không phải** ResultWithError) → FE phải tự đoán. Cache key build từ DonViId+IdChiNhanh nhưng method này **không thực sự đọc/ghi cache** (chỉ Create/Update/Delete remove key) → cache hiệu lực mơ hồ. |

## Quy tắc nghiệp vụ (gating / tier / ngưỡng)
- **Validate Create/Update** (`ValidateAsync`): non-AdminTong → `IdChiNhanh` bắt buộc (null/Empty = lỗi "Id chi nhánh là bắt buộc."); `IdPhuongThuc`, `IdPhuongThucThanhToan` bắt buộc; `MaTaiKhoan`, `TenTaiKhoan` bắt buộc; chống trùng theo (IdDonVi, IdChiNhanh, IdPhuongThuc, MaTaiKhoan) — update bỏ qua chính nó → "Đối tượng đã tồn tại trong hệ thống."
- **Thu/Chi mặc định độc nhất**: set `IsThuMacDinh=true` → mọi TK khác cùng đơn vị bị clear `IsThuMacDinh`; tương tự `IsChiMacDinh`. (Áp dụng cả Create lẫn Update.)

## Quan hệ
- **Gọi sang:** Core `DMPhuongThucThanhToan/GetDMPhuongThucThanhToan?maLoaiQuy=TM` (build danh mục dropdown).
- **Dữ liệu (entities):** `TaiKhoanTienMat` (Id, IdDonVi, IdChiNhanh, IdPhuongThuc/MaPhuongThuc/TenPhuongThuc, IdPhuongThucThanhToan, MaTaiKhoan/TenTaiKhoan, SuDung, IsTkDonVi, IsThuMacDinh, IsChiMacDinh, Image, MaTkKeToan, IdLoaiQuy).
- **Ripple / đồng bộ:** KHÔNG bắn message/event. Là bảng cấu hình; được **Branch** gọi sang lấy danh sách TK tiền mặt (`GetDSTaiKhoanTienMatByChiNhanh`) để phục vụ thu/chi/thanh toán.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Accounting/Accounting.API/Controllers/TaiKhoanTienMatController.cs`
  - Mọi action `[SBAuthorize()]` trống (L32,44,56,68,80,91) → chỉ cần đăng nhập, không phân quyền key/role.
- BE `Accounting/Accounting.Service/Generated/TaiKhoanTienMatService.cs`
  - `UpdateAsync` L213: `existing` lấy theo `Id` thôi (không AND IdDonVi) → rủi ro update chéo đơn vị (Get/Delete đều có AND IdDonVi, riêng Update thiếu).
  - `CreateAsync` (L176+) không set `IdLoaiQuy`; `UpdateAsync` (L251) lại set → record tạo mới có IdLoaiQuy null.
  - `GetDSTaiKhoanTienMatByChiNhanh` L355-358: Core rỗng → `ResultWithData(string)` thay vì ResultWithError; comment nói cache nhưng không có read-cache.
- Lưu ý: controller này chỉ là **cấu hình map phương thức ↔ tài khoản**, KHÔNG lưu số dư quỹ/giao dịch tiền mặt (không có ripple công nợ tại đây).
