---
controller: CauHinhKetNoiKeToanController
module: ke-toan
service: Accounting
man_hinh_ui: "Kế toán > Kết nối phần mềm kế toán (MISA): lưới cấu hình theo chi nhánh (Tên cấu hình, chi nhánh kết nối MISA, cờ Mặc định / Áp dụng đơn vị, danh sách loại phiếu đồng bộ). Nút: Thêm mới (lấy token MISA + chọn chi nhánh MISA), Sửa, Xóa, Lấy chi nhánh MISA. Lọc: Chi nhánh, Keyword."
muc_dich: "Cấu hình kết nối phần mềm kế toán ngoài (MISA): lưu token/AppId/BranchId MISA + danh sách loại phiếu được phép đồng bộ (LstPhieuDongBo) theo đơn vị/chi nhánh. Là CỜ GATING quyết định chứng từ/phiếu CRM có được đẩy sang MISA hay không."
alias: ["cấu hình kết nối kế toán", "kết nối MISA", "cấu hình MISA", "đồng bộ phần mềm kế toán", "CauHinhKetNoiKeToan", "kết nối phần mềm kế toán ngoài", "cấu hình đẩy phiếu sang MISA"]
auth: public
state_machine: []
deprecated: false
entities: [CauHinhKetNoiKeToan, ConfigPartner]
stored_procs: []
depends: [IRepository<CauHinhKetNoiKeToan>, IRepository<ConfigPartner>, IClientHelper (gọi Core /ChiNhanh + MISA OAuth/get_dictionary), RestSharp, ServiceSettings]
cross_module_in:
  - "Core service (qua IClientHelper) — lấy list chi nhánh đơn vị để lọc lưới"
cross_module_out:
  - "MISA (đối tác kế toán ngoài) — lấy token + branch; cấu hình này gate luồng DongBoPhieuCrm đẩy phiếu sang MISA"
---

# CauHinhKetNoiKeToanController — Cấu hình kết nối phần mềm kế toán ngoài (MISA) + gating đẩy phiếu

> Mỗi bản ghi = 1 cấu hình kết nối MISA theo `(IdDonVi, IdChiNhanh)`: `AppId`, `AccessCode`, `AccessToken`/`ExpiredTime`, `BranchId` (chi nhánh bên MISA), `LstPhieuDongBo` (chuỗi loại phiếu nối `;`), cờ `IsMacDinh` (mặc định của chi nhánh) + `IsApDungDonVi` (áp dụng toàn đơn vị — **chỉ admin tổng**). Toàn bộ endpoint `[SBAuthorize()]` trống (chỉ cần login). Controller trả `Ok(result)` thẳng (không bọc `ResponseResult`).
> **Đây là cờ gating chính:** luồng `DongBoPhieuCrmService` chỉ đẩy phiếu sang MISA khi tìm được cấu hình mặc định (IsMacDinh) của chi nhánh, fallback cấu hình IsApDungDonVi của đơn vị, và loại phiếu nằm trong `LstPhieuDongBo`.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Load lưới cấu hình | Lưới + lọc (Chi nhánh, Keyword) | `POST api/CauHinhKetNoiKeToan/TimTheoDieuKien` | — | public | màn kết nối kế toán | Lấy list chi nhánh đơn vị (gọi Core `/api/ChiNhanh/LayListChiNhanh`) để giới hạn IdChiNhanh + đổ TenChiNhanh. Lọc keyword (TenCauHinh). Admin-CN/QLDV/đơn vị kế toán & admin tổng → lọc thêm theo `gridInfo.IdChiNhanh`. Paging, OrderBy NgayCapNhat DESC. | EF `CauHinhKetNoiKeToan` + Core API | `query.Where(IdDonVi==... && IdChiNhanh==user.IdChiNhanh)` ở L65 **không gán lại biến** (`query =` thiếu) → filter đơn vị/chi nhánh đó **bị mất** (chỉ còn lọc theo lstChiNhanhId của đơn vị). |
| 2 | Thêm mới | Popup / Lưu (sau khi lấy token + chọn chi nhánh MISA) | `POST api/CauHinhKetNoiKeToan/ThemMoi` | — | public | nút Thêm mới | Validate (TenCauHinh, AccessCode, AppId, IdChiNhanh, LstPhieuDongBo bắt buộc; trùng tên trong chi nhánh → lỗi). Bắt buộc BranchId + token còn hạn. `IsApDungDonVi=true` chỉ admin tổng & đơn vị chỉ được 1 cấu hình IsApDungDonVi. Nếu không đặt mặc định thì chi nhánh phải đã có 1 cấu hình mặc định. Đặt mặc định → reset các bản ghi IsMacDinh khác cùng chi nhánh. Insert. | EF Insert/Update | — |
| 3 | Sửa | Popup sửa / Lưu | `POST api/CauHinhKetNoiKeToan/CapNhat` | — | public | chọn 1 dòng | Validate (isUpdate, thêm check BranchId). Bắt buộc BranchId + token còn hạn. `IsApDungDonVi` chỉ admin tổng & duy nhất 1 trong đơn vị. Bật mặc định → reset mặc định khác; bỏ mặc định → phải còn ≥1 cấu hình mặc định khác cho chi nhánh. Update + MarkDirty (gồm AppId). | EF Update | `GetByIdAsync` không lọc IdDonVi (sửa theo Id). |
| 4 | Xem chi tiết | (load form sửa) | `GET api/CauHinhKetNoiKeToan/GetChiTiet/{id}` | — | public | trước khi sửa | Trả `CauHinhKetNoiKeToanDto` theo Id (gồm Token, ExpiredTime). | EF | Không lọc IdDonVi; trả AccessToken ra client. |
| 5 | Xóa | Nút Xóa / confirm | `POST api/CauHinhKetNoiKeToan/Xoa/{id}` | — | public | mỗi dòng | `GetByIdAsync(id)` → `DeleteAsync` (xóa cứng). | EF Delete (hard) | Không lọc IdDonVi; không kiểm tra cấu hình có đang là mặc định duy nhất (xóa xong chi nhánh có thể mất mặc định). |
| 6 | Lấy chi nhánh MISA | Nút "Kết nối / Lấy chi nhánh" (trong popup) | `POST api/CauHinhKetNoiKeToan/GetBranchMisa` | — | public | popup thêm/sửa | Lấy `ConfigPartner` PartnerName="MISA". Gọi MISA OAuth (`/api/oauth/actopen/connect`) bằng AppId+AccessCode+OrgCompanyCode → access_token; rồi `get_dictionary` (GetBranchInfo) lấy list chi nhánh MISA (distinct branch_id). Trả token + ExpiredTime + MisaBranches để FE chọn BranchId. | EF `ConfigPartner` + MISA API (RestSharp) | `request.Timeout = 60000` nhưng comment ghi "10s" (L510) — lệch comment. Token MISA trả thẳng về client. |

## Quy tắc nghiệp vụ (gating — quyết định đẩy phiếu sang MISA)
> Đọc ở `DongBoPhieuCrmService` (không ở controller này). Controller chỉ tạo/sửa dữ liệu cấu hình; luồng đồng bộ dùng dữ liệu đó như cờ.
1. **Có lưu vào hàng đợi đẩy không** (`XuLy...`): tìm cấu hình `IdDonVi==input.IdDonVi && IdChiNhanh==input.IdChiNhanh && IsMacDinh==true`; null → fallback `IdDonVi==input.IdDonVi && IsApDungDonVi==true`. Có cấu hình → insert `DataReceiveTaoPhieu` (đủ điều kiện đẩy/đẩy thủ công); không có → **bỏ qua** (không đẩy). (DongBoPhieuCrmService L223-239, L1046-1058)
2. **Khi đẩy 1 phiếu** (ProcessTaoPhieuPartner): cấu hình null → lỗi "Không tìm thấy cấu hình kết nối kế toán mặc định...". `AppId` rỗng → lỗi. **Loại phiếu phải nằm trong `LstPhieuDongBo`** (`Split(';')` chứa `TypePhieu`); không → "Loại phiếu {x} không được cấu hình gửi sang đối tác." (L1059-1086)
3. **Chỉ admin tổng** mới được `IsApDungDonVi=true`; đơn vị chỉ giữ **1** cấu hình IsApDungDonVi và mỗi chi nhánh **≥1** cấu hình IsMacDinh (ràng buộc ở Thêm/Sửa).

## Quan hệ
- **Gọi sang:** Core service `/api/ChiNhanh/LayListChiNhanh` (lọc lưới); MISA OAuth + get_dictionary (lấy token & chi nhánh); `ConfigPartner` (URL/OrgCompanyCode đối tác).
- **Dữ liệu (entities):** `CauHinhKetNoiKeToan` (IdDonVi, IdChiNhanh, TenCauHinh, AppId, AccessCode, AccessToken, ExpiredTime, BranchId, TenChiNhanhKetNoi, LstPhieuDongBo, IsMacDinh, IsApDungDonVi). `ConfigPartner` (đối tác MISA).
- **Ripple / đồng bộ:** là cờ điều kiện cho `DongBoPhieuCrmService` → quyết định phiếu CRM (BH/MH/Nhập/Xuất/Chuyển kho/Thu chi...) có được đẩy sang MISA và đẩy loại phiếu nào. Sửa/xóa cấu hình ảnh hưởng trực tiếp việc đẩy chứng từ.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Accounting/Accounting.Service/Generated/CauHinhKetNoiKeToanService.cs`
  - L65: `query.Where(x => x.IdDonVi == userLogin.DonViId && x.IdChiNhanh == userLogin.IdChiNhanh);` **thiếu `query =`** → mệnh đề lọc bị bỏ (LINQ immutable), filter đơn vị/chi nhánh chính không áp dụng.
  - L510: `request.Timeout = 60000;` nhưng comment "//10s timeout" (thực 60s).
  - `CapNhat`/`Xoa`/`GetChiTiet` dùng `GetByIdAsync` không lọc IdDonVi → thao tác/đọc chéo đơn vị nếu biết Id; `GetChiTiet` còn trả `AccessToken` ra client.
  - `Xoa` không kiểm tra cấu hình có phải mặc định duy nhất của chi nhánh trước khi xóa cứng.
- Controller `Accounting/Accounting.API/Controllers/CauHinhKetNoiKeToanController.cs`: mọi endpoint `[SBAuthorize()]` trống; trả `Ok(result)` thẳng (không `ResponseResult`).
- Consumer gating: `Accounting/Accounting.Service/Generated/DongBoPhieuCrmService.cs:223-239, 1046-1086`.
- Entity `Accounting/Accounting.DataContext/Entity/CauHinhKetNoiKeToan.cs`.
