---
controller: TaiKhoanViDienTuController
module: ke-toan
service: Accounting
man_hinh_ui: "Kế toán > Cấu hình tài khoản ví điện tử: lưới list (lọc Chi nhánh, TK đơn vị, Keyword) + Thêm/Sửa/Xóa từng tài khoản ví. Mỗi bản ghi = 1 ví điện tử gắn phương thức thanh toán (maLoaiQuy='VDT'), có cờ thu/chi mặc định."
muc_dich: "CRUD danh mục TÀI KHOẢN ví điện tử (quỹ VDT) theo đơn vị × chi nhánh: map phương thức thanh toán loại quỹ 'VDT' → tài khoản kế toán, đặt thu/chi mặc định. Cung cấp danh sách ví cho luồng thu/chi & bán hàng (service khác gọi sang). Song song 1:1 với TaiKhoanTienMat (khác mỗi maLoaiQuy)."
alias: ["tài khoản ví điện tử", "tai khoan vi dien tu", "ví điện tử", "TK ví điện tử", "thanh toán ví", "phương thức thanh toán ví", "VDT", "ewallet"]
auth: internal
state_machine: []
deprecated: false
entities: [TaiKhoanViDienTu]
stored_procs: []
depends:
  - ITaiKhoanViDienTuService
  - Core/DMPhuongThucThanhToan/GetDMPhuongThucThanhToan (maLoaiQuy=VDT)  # qua IClientHelper
  - IRedisCacheService (cache key GetDSViDienTuThanhToanByChiNhanh_{donVi}_{chiNhanh})
cross_module_in:
  - Branch (Branch.Service CallService/AccountingService → GET GetDSViDienTuThanhToanByChiNhanh)
cross_module_out:
  - Core (gọi sang DMPhuongThucThanhToan để build danh mục)
---

# TaiKhoanViDienTuController — Cấu hình tài khoản ví điện tử

> **Bản sao 1:1 của `TaiKhoanTienMatController`**, chỉ khác entity `TaiKhoanViDienTu` + `maLoaiQuy='VDT'` (thay 'TM') + DTO `TaiKhoanViDienTuDTO/Info`. CRUD danh mục TÀI KHOẢN ví điện tử thuần (không quản số dư). Mọi bản ghi gắn `IdDonVi`+`IdChiNhanh`. Cờ `IsThuMacDinh`/`IsChiMacDinh` duy nhất trong đơn vị. Dropdown `GetDSViDienTuThanhToanByChiNhanh` gọi Core lấy danh mục phương thức loại 'VDT' rồi LEFT JOIN. **Controller-level mọi endpoint = `[SBAuthorize()]` trống (chỉ cần đăng nhập).**

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm kiếm / load lưới | Lưới list + lọc (Chi nhánh, TK đơn vị, Keyword) | `POST api/TaiKhoanViDienTu/TimTheoDieuKien` | `SBAuthorize()` (any login) | internal | màn cấu hình ví điện tử | Lọc `IdDonVi`; optional `IdChiNhanh`, `IsTkDonVi==true`, Keyword (Contains MaTaiKhoan/TenTaiKhoan). Order `NgayCapNhat DESC`, paging. Trả items + totalRecords. | **EF** `TaiKhoanViDienTu` | — |
| 2 | Xem chi tiết 1 ví | (mở form sửa) | `POST api/TaiKhoanViDienTu/GetByIdAsync` | `SBAuthorize()` | internal | chọn 1 dòng | Lấy theo `Id && IdDonVi`; null → "Đối tượng không tồn tai." | **EF** | — |
| 3 | Thêm mới | Nút "Thêm" / Lưu | `POST api/TaiKhoanViDienTu/CreateAsync` | `SBAuthorize()` | internal | nút Thêm | Validate. Set cờ thu/chi mặc định → clear cờ tương ứng của ví khác cùng đơn vị. Insert `IdDonVi=userLogin.DonViId`. Clear redis cache. | **EF** Insert | — |
| 4 | Cập nhật | Nút "Sửa" / Lưu | `POST api/TaiKhoanViDienTu/UpdateAsync` | `SBAuthorize()` | internal | dòng đã có | Validate (isUpdate). Tìm theo `Id` (KHÔNG lọc IdDonVi — xem known-issue). Clear cờ thu/chi mặc định khác. Cập nhật + MarkDirty. Clear cache. | **EF** Update | (a) `existing` theo `Id` thôi, **không AND IdDonVi** → rủi ro update chéo đơn vị. (b) Create không set `IdLoaiQuy`, Update có. |
| 5 | Xóa | Nút "Xóa" / confirm | `POST api/TaiKhoanViDienTu/DeleteAsync` | `SBAuthorize()` | internal | mỗi dòng | Tìm theo `Id && IdDonVi`; null → "Không tìm thấy dữ liệu cần xóa."; OK → Delete + clear cache. | **EF** Delete | — |
| 6 | Dropdown ví theo chi nhánh | (combobox chọn ví khi thu/chi/thanh toán) | `GET api/TaiKhoanViDienTu/GetDSViDienTuThanhToanByChiNhanh` | `SBAuthorize()` | internal | form chọn phương thức VDT | Gọi Core `DMPhuongThucThanhToan?maLoaiQuy=VDT`. baseQuery đã lọc `SuDung==true`; ưu tiên ví `IdChiNhanh==userLogin.IdChiNhanh`; rỗng → fallback `IdChiNhanh==null`. LEFT JOIN (MaLoaiQuy, MaPhuongThuc): chưa cấu hình → `IdTaiKhoan=Guid.Empty`, `MaTaiKhoan=MaPhuongThuc`, `TenTaiKhoan="TK {MaPhuongThuc}"`. | **EF** + Core HTTP | Core rỗng → trả `ResultWithData("Lỗi get danh sách...")` (string vào data, không phải ResultWithError). Code chết (block LINQ join cũ comment-out L374-388). |

## Quy tắc nghiệp vụ (gating / tier / ngưỡng)
- **Validate Create/Update** (`ValidateAsync`): non-AdminTong → `IdChiNhanh` bắt buộc; `IdPhuongThuc`, `IdPhuongThucThanhToan`, `MaTaiKhoan`, `TenTaiKhoan` bắt buộc; chống trùng (IdDonVi, IdChiNhanh, IdPhuongThuc, MaTaiKhoan) — update bỏ qua chính nó.
- **Thu/Chi mặc định độc nhất**: set true → clear cờ tương ứng ở mọi ví khác cùng đơn vị (Create + Update).

## Quan hệ
- **Gọi sang:** Core `DMPhuongThucThanhToan/GetDMPhuongThucThanhToan?maLoaiQuy=VDT`.
- **Dữ liệu (entities):** `TaiKhoanViDienTu` (cùng cấu trúc field như TaiKhoanTienMat).
- **Ripple / đồng bộ:** KHÔNG bắn message/event. Được **Branch** gọi sang `GetDSViDienTuThanhToanByChiNhanh` để lấy danh sách ví.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Accounting/Accounting.API/Controllers/TaiKhoanViDienTuController.cs`
  - Mọi action `[SBAuthorize()]` trống (L32,44,56,68,80,91) → không phân quyền key/role.
  - Comment XML class L9 ghi nhầm `TaiKhoanViDienTuController` ở cả file TienMat lẫn ViDienTu (copy-paste) — không ảnh hưởng runtime.
- BE `Accounting/Accounting.Service/Generated/TaiKhoanViDienTuService.cs`
  - `UpdateAsync` L210: `existing` lấy theo `Id` thôi (không AND IdDonVi) → rủi ro update chéo đơn vị.
  - `CreateAsync` không set `IdLoaiQuy`; `UpdateAsync` (L246) set → record mới IdLoaiQuy null.
  - `GetDSViDienTuThanhToanByChiNhanh` L353-356: Core rỗng → `ResultWithData(string)` thay vì ResultWithError; L374-388 code join cũ bị comment-out.
- Lưu ý: chỉ cấu hình map phương thức ↔ tài khoản ví, KHÔNG lưu số dư/giao dịch ví (không ripple công nợ tại đây).
