---
controller: DichVuChiNhanhController
module: kho
service: WarehouseProduct
man_hinh_ui: "Không có màn quản lý riêng. Endpoint phục vụ popup Thêm/Sửa Combo Spa (combo-spa-edit + combo-spa-chinhanh-edit): khi user chọn nhóm dịch vụ → nạp danh sách dịch vụ của các nhóm đó, lọc theo chi nhánh của user."
muc_dich: "Tra bảng map dịch vụ ↔ chi nhánh (DichVuChiNhanh): lấy danh sách dịch vụ thuộc các nhóm dịch vụ truyền vào, nhưng chỉ những dịch vụ đã gán cho chi nhánh hiện tại của user — dùng để chọn dịch vụ khi build combo spa theo chi nhánh."
alias: ["dịch vụ chi nhánh", "dịch vụ theo chi nhánh", "service chi nhánh", "dịch vụ riêng chi nhánh", "lấy dịch vụ theo nhóm chi nhánh"]
auth: internal
state_machine: []
deprecated: false
entities: [DichVuChiNhanh, DichVu]
stored_procs: []
depends: [WarehouseProduct]
cross_module_in: [Combo Spa (combo-spa-edit / combo-spa-chinhanh-edit gọi LayTheoDanhMucDichVu qua dichVu.service.layTheoNhomDichVu khi đổi nhóm dịch vụ)]
cross_module_out: []
---

# DichVuChiNhanhController — Dịch vụ theo chi nhánh (map DichVu ↔ ChiNhanh)

> Controller `partial` rất nhỏ (`public partial class DichVuChiNhanhController : BaseController`), `[Route("api/[controller]")]` → base path `api/DichVuChiNhanh`, gắn `MaTienIchSCRM.TI_Kho`. Chỉ có **1 endpoint active** (`LayTheoDanhMucDichVu`); endpoint `TimTheoDieuKien` đã bị **comment trong controller** (chỉ còn ở service, không expose). Entity `DichVuChiNhanh` là bảng nối tối giản: chỉ `IdDichVu` + `IdChiNhanh` (+ field audit của BaseEntity). Không có CRUD UI riêng cho controller này.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy dịch vụ theo nhóm (giới hạn chi nhánh user) | (không nút riêng) tự gọi khi đổi "Nhóm dịch vụ" trong popup edit Combo Spa | POST `api/DichVuChiNhanh/LayTheoDanhMucDichVu` (body = `List<Guid>` ids nhóm dịch vụ) | `[SBAuthorize]` (chỉ cần đăng nhập, không key riêng) | internal | luôn (trigger từ `dsNhomDichVuChange` của combo-spa) | Join `DichVuChiNhanh` (chi nhánh = `CurrentUser.IdChiNhanh`, chưa xóa) với `DichVu` (chưa xóa, `SuDung == true`, `IdNhomDichVu ∈ ids`) → trả `List<DichVuDto>` (Id, Ten, Ma, GiaDichVu, GiaVonDichVu, IdNhomDichVu, TenTiengAnh, MoTa, IdImage). `CurrentUser == null` → ném "Thông tin đăng nhập không hợp lệ" | EF | Mọi exception bị bọc lại thành `ArgumentException(ex.Message)` → mất stack gốc, khó debug (controller line 54-57) |

## Endpoint nội bộ (private / service-to-service)
- Không có endpoint `[PrivateSBAuthorize]`. Endpoint duy nhất `LayTheoDanhMucDichVu` chỉ là `[SBAuthorize]`, được FE gọi nội bộ trong luồng dựng combo spa (không phải service-to-service).

## Quan hệ
- **Gọi sang:** chỉ repo nội bộ service `WarehouseProduct` — `IRepository<DichVuChiNhanh>` (bảng map) join `IRepository<DichVu>` (danh mục dịch vụ). Không gọi service ngoài.
- **Dữ liệu (entities):** `DichVuChiNhanh` (`IdDichVu`, `IdChiNhanh` — bảng nối; entity Clone() chỉ copy 2 field này), `DichVu` (Ten, Ma, GiaDichVu, GiaVonDichVu, IdNhomDichVu, SuDung, TenTiengAnh, MoTa, IdImage). Lọc bắt buộc theo `CurrentUser.IdChiNhanh`.
- **Ripple / đồng bộ:** không phát side-effect ra ngoài (chỉ đọc). Là nguồn DATA đầu vào cho màn Combo Spa chi nhánh — nếu bảng `DichVuChiNhanh` không có row map dịch vụ ↔ chi nhánh thì combo spa sẽ KHÔNG thấy dịch vụ dù `DichVu` tồn tại.

## Lỗi đã biết / lưu ý kỹ thuật
- **Auth lỏng:** chỉ `[SBAuthorize]` rỗng, không gắn permission key riêng — bất kỳ user đăng nhập nào cũng gọi được (DichVuChiNhanhController.cs:44). Dữ liệu vẫn an toàn vì query cứng theo `CurrentUser.IdChiNhanh`.
- **Try/catch nuốt lỗi:** controller bọc mọi exception thành `throw new ArgumentException(ex.Message)` (DichVuChiNhanhController.cs:54-57) → mất stack trace gốc.
- **`TimTheoDieuKienAsync` dead code phía controller:** endpoint `TimTheoDieuKien` bị comment (DichVuChiNhanhController.cs:31-36); method còn trong service (DichVuChiNhanhService.cs:56-73) nhưng `if/else` sort là **trùng nhánh** (cả 2 đều `OrderBy(NgayTao)`) và không filter theo điều kiện nào — coi như chưa hoàn thiện, không expose.
- **`UpdateAsync` override `MarkDirty` thủ công** toàn bộ field (DichVuChiNhanhService.cs:37-54) nhưng controller không có endpoint Update → chỉ dùng được khi gọi service nội bộ (vd seed/sync map dịch vụ-chi nhánh).
- **Cast `(Guid)dvCtx.IdNhomDichVu`** trong join (DichVuChiNhanhService.cs:82): nếu `DichVu.IdNhomDichVu` null sẽ lỗi cast — giả định mọi dịch vụ đều có nhóm. Lưu ý khi trace dữ liệu dịch vụ thiếu nhóm.
- **Phân biệt với `DichVuController`:** luồng "dịch vụ chi nhánh tự tạo" (CRUD) nằm ở `DichVuController` (`*DvChiNhanh`), KHÔNG ở controller này. Controller này chỉ là bảng map + 1 query đọc.
