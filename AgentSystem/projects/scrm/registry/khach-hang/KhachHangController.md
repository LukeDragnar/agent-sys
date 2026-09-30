---
controller: KhachHangController
module: khach-hang
service: Customer
man_hinh_ui: "Customer/quanly-khachhang (Khách hàng 360 — danh sách + thống kê) · khachhang-edit (thêm/sửa) · quanly-khachhang-detail (hồ sơ KH360, nhiều tab: công nợ, hóa đơn, hợp đồng, bệnh lý, lịch sử chăm sóc/điểm thưởng/tương tác, thẻ dịch vụ) · khachhang-import · phanbo-khachhhang"
muc_dich: "CRUD khách hàng + màn hình Khách hàng 360 (search/thống kê/export) + phân bổ Sale/MKT + tích hợp private cho các service khác (Order, Invoice, AuthenCustomer, NhaCungCap)"
alias: [KhachHang, Customer, KH, KH360, "Khách hàng 360"]
phu_thuoc:
  - IKhachHangService          # CRUD + nghiệp vụ chính
  - IKhachHangStoreService     # KH360 (SP SCRM_Customer_KhachHang360*)
  - "SP: SCRM_Customer_KhachHang360 / _TotalRecord / _GetIdsKhachHang / _ThongKe"
  - "Entity: KhachHang"
  - BaseController              # CurrentUser, ResponseResult
ripple:
  - Order (GetByPhoneOrCreateIfNotExist, import đơn → tạo KH)
  - Invoice (GetInfoKhachHangInvoice*)
  - AuthenCustomer (GetDetailKhachHangPrivate, CapNhatThongTinKhachPrivate, CreatePrivateAccount)
  - NhaCungCap (KH đồng thời là NCC — đồng bộ mã 2 chiều)
  - Sale/MKT (phân bổ + rollback gán nhân viên)
---

# KhachHang — Quản lý khách hàng (CRUD + Khách hàng 360)

> Controller lớn (~60 endpoint). Bảng **Hành động** chỉ liệt kê endpoint **chính** (màn hình UI dùng trực tiếp).
> Endpoint phụ (private cross-service, dropdown, NCC, mobile) gom ở mục **Nhóm endpoint phụ**.

## Hành động (endpoint chính — có UI)

| # | Hành động | Nhãn UI/nút | Endpoint | Quyền | Hành vi đúng (expected) | Known-issue |
|---|---|---|---|---|---|---|
| 1 | Tìm KH360 (list phân trang) | Lưới "Khách hàng 360" | `POST SearchKhachHang360` | KhachHang_SearchKhachHang360 (nhiều role: AD/QLDV/KT/SALE/MKT...) | ModelState invalid → trả lỗi MESS_Model_UnValidate; gọi SP `SCRM_Customer_KhachHang360` theo filter + scope `CurrentUser` (đơn vị/chi nhánh) | — |
| 2 | Tổng số bản ghi KH360 | (đếm cho paging) | `POST SearchKhachHang360_TotalRecord` | KhachHang_SearchKhachHang360_TotalRecord | Trả total cùng filter list để đồng bộ paging; SP `_TotalRecord` | — |
| 3 | Lấy Ids KH theo filter | (chọn-tất-cả-trang / bulk) | `POST SearchKhachHang360_GetIdsKhachHang` | `[SBAuthorize]` (chú thích role bị comment) | Trả List&lt;Guid&gt; toàn bộ KH khớp filter (để phân bổ/bulk) | Quyền nới lỏng so với các action 360 khác — chỉ yêu cầu đăng nhập |
| 4 | Thống kê KH360 | Card thống kê (tổng/sinh nhật...) | `POST SearchKhachHang360_ThongKe` | KhachHang_SearchKhachHang360_ThongKe | SP `_ThongKe` → Total/CreatedToday/Birthday today+month | — |
| 5 | Export Excel KH360 | Nút "Xuất Excel" | `POST ExportExcelKhachHang` | KhachHang_SearchKhachHang360 (role rút gọn) | Trả file `Baocaokhachhang.xlsx`. result null/!Success → 500; `Service_Unavailable` → **503**. Async lớn có thể đi qua `EventKhachHangExportExcel` | Trả mã HTTP raw (500/503) khác convention ResponseResult — FE phải bắt riêng |
| 6 | Thêm mới KH (chính, dùng) | Nút "Thêm mới" (khachhang-edit) | `POST ThemMoi_V2` | KhachHang_ThemMoi_V2 (full role gồm DV-NV) | `_serviceKhachHang.Create(model)`; FE dùng endpoint này | — |
| 7 | Thêm mới KH (liên quan NCC) | (form có mã NCC) | `POST KhachHang_Insert` | KhachHang_ThemMoi_V2 (dùng chung mã quyền với #6) | `KhachHang_Insert(model, CurrentUser)` — nhánh xử lý NCC | Dùng lại mã quyền "ThemMoi_V2" cho endpoint khác → khó audit |
| 8 | Cập nhật KH | Nút "Lưu" (sửa) | `POST KhachHang_Update` | `[SBAuthorize]` (role check trong service) | `KhachHang_Update(reqData, CurrentUser)` — bản update có liên quan NCC | — |
| 9 | Cập nhật KH (cũ) | (legacy) | `POST CapNhatThongTin` | `[SBAuthorize]` | `Update(model)`; block check NotFound đã bị comment | Không kiểm tra tồn tại trước update (code check đã comment) |
| 10 | Lấy chi tiết KH theo id | Mở hồ sơ KH | `GET TimTheoId/{id}` | KhachHang_TimTheoId | `GetKHById(id)` | — |
| 11 | Chi tiết KH để view | Tab xem (read-only) | `GET TimTheoIdToView/{id}` · `GET GetKHByIdForView/{id}` | KhachHang_TimTheoIdToView / _GetKHByIdForView | Bản view ẩn/format field | 2 endpoint view gần trùng → dễ nhầm |
| 12 | Tìm theo SĐT (edit/view) | Ô tìm nhanh theo SĐT | `GET TimTheoSdtEdit/{sdt}` · `GET TimTheoSdtEditToView/{sdt}` | KhachHang_TimTheoSdtEdit / ...ToView | Trả KH theo SĐT | — |
| 13 | Phân bổ KH cho Sale | Nút "Phân bổ Sale" (phanbo-khachhhang) | `POST PhanBoKhachHangChoSale` | KhachHang_PhanBoKhachHangChoSale (AD-TONG/CN/MCN) | set `payload.Type=1` → `PhanBoKhachHangChoSaleMKT` | Type gán cứng trong controller, không từ payload |
| 14 | Phân bổ KH cho MKT | Nút "Phân bổ MKT" | `POST PhanBoKhachHangChoMKT` | KhachHang_PhanBoKhachHangChoMKT | set `payload.Type=2` → cùng service #13 | như #13 |
| 15 | Download template import | Nút "Tải file mẫu" | `GET DownloadFileImportKhachHang` | KhachHang_DownloadFileImportKhachHang | Trả `ImportKhachHang.xlsx` | — |
| 16 | Import danh sách KH | Nút "Import" (khachhang-import) | `POST ImportKhachHang` (IFormFile) | KhachHang_ImportKhachHang | Map 21 cột Excel (bắt đầu dòng 5) → lọc dòng rỗng → `ImportKhachHang(data)`; file null → "File không hợp lệ" | Mapping cột cứng theo index — đổi template gãy silent; lọc rỗng OR nhiều field |
| 17 | Tải kết quả lỗi import | Nút "Tải file lỗi" | `GET DownloadDataWrongKhachHang/{id}` | KhachHang_DownloadDataWrongKhachHang | Trả `KetQuaImportKhachHang.xlsx` theo id phiên import | — |
| 18 | Bệnh lý KH (CRUD) | Tab "Bệnh lý" (khachhang-benhly) | `POST InsertBenhLyKhachHang` · `GetListBenhLyTheoKhachHang` · `GetDetailBenhLy` · `UpdateBenhLy` · `DeleteBenhLy` | KhachHang_InsertBenhLyKhachHang / _GetList... / _GetDetailBenhLy / _UpdateBenhLy / _DeleteBenhLy | CRUD bệnh lý gắn 1 KH | — |
| 19 | Cập nhật ghi chú KH | Ô "Ghi chú" inline | `POST CapNhatGhiChuKhachHang` | `[SBAuthorize]` | `CapNhatGhiChuKhachHang(reqDto)` | — |
| 20 | Cập nhật quê quán | Form địa chỉ/quê quán | `POST CapNhatThongTinQueQuan` · `...CustomResult` | `[SBAuthorize]` | `UpdateQueQuan(model, CurrentUser)`; CustomResult trả shape kết quả khác | 2 biến thể chỉ khác kiểu trả về |
| 21 | Số liệu KH theo đơn vị | Dashboard số liệu | `GET GetSoLieuKhachHangTheoDonVi` · `GET LayTheoDonVi` | KhachHang_GetSoLieuKhachHangTheoDonVi / _LayTheoDonVi | Theo scope `CurrentUser` | — |
| 22 | Hạng khách hàng | Tab/lọc theo hạng | `POST TimKiemHangKhachHangTheoDieuKien` · `GET GetHangKhachHangByIdKh/{id}` | KhachHang_TimKiemHangKhachHangTheoDieuKien / _GetHangKhachHangByIdKh | Lấy hạng KH | — |
| 23 | Tìm theo keyword | Autocomplete/ô search | `POST SearchByKeyword` · `POST SearchByKeywordGetAll` | KhachHang_SearchByKeyword / _SearchByKeywordGetAll | GetAll bỏ paging | — |
| 24 | **Xóa KH (vô hiệu hóa)** | Nút "Xóa" | `POST Xoa/{id}` | KhachHang_Xoa | **Luôn trả lỗi** "Tính năng xóa khách hàng không còn được sử dụng!" — KHÔNG xóa | Endpoint còn quyền nhưng đã disable cứng — KH không hỗ trợ xóa |
| 25 | Tìm KH theo điều kiện (cũ) | (legacy filter) | `POST TimTheoDieuKien` · `POST FindByCondition` | KhachHang_TimTheoDieuKien / `[SBAuthorize]` | Bản search cũ trước KH360 | Trùng vai trò với SearchKhachHang360 (#1) — ưu tiên #1 |

## Nhóm endpoint phụ (private/cross-service — KHÔNG có UI trực tiếp)

> Đa số `[PrivateSBAuthorize]` (gọi service-to-service) hoặc `[AllowAnonymous]`. Gom theo mục đích:

- **Tạo nhanh / find-or-create (đặt lịch, đơn hàng):** `FindOrCreated` (AllowAnonymous) · `FindOrCreatedCustomResult` · `FindOrCreatedPrivate` · `FindCustomersById` · `FindAndCreateKh` · `GetByPhoneOrCreateIfNotExist` (Order import) · `CreatePrivate` · `CreateAnonymous` · `CreatePrivateFromImportComboKhachHang`.
- **Get-by-id / base (gọi nội bộ):** `TimTheoId_Private` · `TimTheoIdBase` · `TimTheoIdsBase` · `GetByIdsBaseWithConfigHidePhoneNumber` (ẩn SĐT theo config) · `GetKhachHangById` · `GetByIdKhachHangPrivate/{idKhach}/{idDonVi}` · `CheckExistByIdKhachHang/{idKhach}/{idDonVi}` · `GetAllBasicInfo`.
- **Tìm theo SĐT (nội bộ):** `GetInfoKhachHangBySoDienThoai/{sdt}` · `GetInfoKhachHangByOmiCall/{sdt}` (OmiCall popup) · `GetCustomerByPhone` · `GetInfoCustomerByPhone/{phone}/{idDonVi}` · `PrivateSearchCustomer`.
- **Invoice (Service Invoice):** `GetInfoKhachHangInvoice/{id}/{idDonVi}` · `GetInfoKhachHangInvoiceByList`.
- **AuthenCustomer (tài khoản KH private):** `GetDetailKhachHangPrivate` · `CapNhatThongTinKhachPrivate` · `CreateOrUpdatePrivateAccount` · `CreateMultiPrivateAccount` · `UpdateKhachHangNhanPrivate` · `UpdateKhachHangTrangThaiPrivate`.
- **NhaCungCap (KH đồng thời là NCC):** `UpdateKhachHangFromNhaCungCap` (old) · `NhaCungCapDongThoiLaKhachHang` (đồng bộ mã 2 chiều) · `TimTheoMaNhaCungCap/{ma}` · `IsNccDaCoKhachHang/{maNcc}` · `TimKhachHangAndNcc/{id}` · `GetDsDonHangTheoDoiTuong` · `GetCustomerByCodes` · `GetMaKhachHangByNhom` · `DropdownKhachHangOfNhaCungCap` · `DropdownKhachHangOfNhaCungCap_V2`.
- **Sale/MKT (gán/rollback):** `KhachHangUpdateSaleMkt` · `KhachHangRollBackUpdateSaleMkt`.
- **Mobile / khác:** `UpdateKhForMobile` · `SaveKhachHang` · `UpdateThoiGianDenCuaHang` · `ThemMoi` (V1 legacy, đã thay bằng ThemMoi_V2).

## Quan hệ
- **Gọi sang:** `IKhachHangService` (toàn bộ CRUD + nghiệp vụ) · `IKhachHangStoreService` (KH360, gọi thẳng SP qua Dapper).
- **Dữ liệu (entities):** `KhachHang` (entity gốc); KH360 đọc qua SP tổng hợp (không map entity trực tiếp).
- **Ripple/đồng bộ:**
  - KH ⇄ **NhaCungCap**: mã KH và mã NCC dùng chung không gian; `NhaCungCapDongThoiLaKhachHang` đồng bộ khi NCC đổi mã/đồng thời là KH (xem comment code: "mã NCC và KH phải duy nhất trừ khi thuộc về nhau").
  - **Order** import đơn → `GetByPhoneOrCreateIfNotExist` tạo KH nếu chưa có.
  - **AuthenCustomer** ⇄ tài khoản đăng nhập KH (private account create/update).
  - **Sale/MKT** phân bổ + rollback gán nhân viên phụ trách KH.

## Luồng xuyên module (nếu có)
1. **Import đơn hàng (Order) → tạo KH:** Order gọi `GetByPhoneOrCreateIfNotExist` → nếu SĐT chưa có KH thì tạo mới → trả KH cho đơn. Ripple: KH list/360 xuất hiện KH mới.
2. **NCC đồng thời là KH:** NhaCungCap thay đổi mã → gọi `NhaCungCapDongThoiLaKhachHang` → cập nhật/đồng bộ bản ghi KH tương ứng. Đảo lại: tạo/sửa KH có mã NCC qua `KhachHang_Insert`/`KhachHang_Update`.
3. **Phân bổ Sale/MKT:** chọn KH (Ids từ `SearchKhachHang360_GetIdsKhachHang`) → `PhanBoKhachHangChoSale` (Type=1) / `...ChoMKT` (Type=2) → có thể rollback bằng `KhachHangRollBackUpdateSaleMkt`.
4. **Tài khoản KH (AuthenCustomer):** tạo/sửa tài khoản private → đồng bộ trạng thái/nhãn KH (`UpdateKhachHangTrangThaiPrivate`, `UpdateKhachHangNhanPrivate`).

## Lỗi đã biết / lưu ý kỹ thuật (path:line)
- `KhachHangController.cs:332-336` (`Xoa`) — endpoint xóa **disable cứng**, luôn trả lỗi dù còn `KhachHang_Xoa` quyền. KH không có chức năng xóa thật.
- `KhachHangController.cs:794-820` (`ExportExcelKhachHang`) — trả **HTTP raw 500/503** (không qua `ResponseResult`), `Service_Unavailable` → 503. FE phải bắt status code riêng, khác convention chung.
- `KhachHangController.cs:884` & `:897` (`PhanBo...`) — `payload.Type` gán cứng (1/2) trong controller, bỏ qua giá trị client gửi.
- `KhachHangController.cs:176` & `:1048` — endpoint `ThemMoi_V2` và `KhachHang_Insert` **dùng chung mã quyền** `"KhachHang_ThemMoi_V2"` → khó audit phân quyền riêng.
- `KhachHangController.cs:276-280` (`CapNhatThongTin`) — block check NotFound trước update đã bị **comment** → update không xác thực tồn tại.
- `KhachHangController.cs:448-582` (`ImportKhachHang`) — mapping 21 cột Excel theo **index cứng**, đọc từ dòng 5; đổi thứ tự/template → lỗi mapping âm thầm.
- `KhachHangController.cs:80` (`SearchKhachHang360_GetIdsKhachHang`) — quyền nới lỏng (`[SBAuthorize]` không kèm mã/role; dòng role gốc bị comment) trong khi các action 360 khác siết role rõ ràng.
- `KhachHangService` (Generated, SP `SCRM_Customer_KhachHang360*`) — logic KH360 nằm trong **stored procedure**; thay đổi field/filter phải sửa SP, không thấy trong C#. Service: `Customer/Customer.Service/Generated/KhachHangStoreService.cs:101,464,624,784,942`.
