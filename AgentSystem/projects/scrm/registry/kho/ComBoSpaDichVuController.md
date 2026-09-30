---
controller: ComBoSpaDichVuController
module: kho
service: WarehouseProduct
man_hinh_ui: "nội bộ — không có UI riêng. Bảng chi tiết DỊCH VỤ trong combo được quản lý GIÁN TIẾP qua màn combo (combo-spa.component / combo-spa-edit): khi tạo/sửa combo, dsDichVu nằm trong payload của ComBoSpaController, không gọi controller này."
muc_dich: "CRUD generic cho bảng chi tiết DỊCH VỤ của combo (ComBoSpaDichVu: mỗi dòng = 1 dịch vụ trong 1 combo, kèm SoLan/DonGia/ThanhTien). Là controller scaffold sinh từ SBBaseController; thực tế dsDichVu được xử lý nội bộ qua ComBoSpaController (CreateCombo/Update), nên các endpoint ở đây hầu như không được dùng trực tiếp."
alias: ["chi tiết dịch vụ combo", "combo dịch vụ", "dịch vụ trong combo", "ComBoSpaDichVu", "dòng dịch vụ combo"]
auth: public
state_machine: []
deprecated: false
entities: [ComBoSpaDichVu]
stored_procs: []
depends: [WarehouseProduct]
cross_module_in: []
cross_module_out: [ComBoSpa (node cha — dsDichVu là chi tiết của combo)]
---

# ComBoSpaDichVuController — Chi tiết dịch vụ của combo (bảng con)

> Controller scaffold kế thừa `SBBaseController<ComBoSpaDichVu, ComBoSpaDichVuDto>`. Lớp con KHÔNG khai báo thêm endpoint nào (endpoint `TimTheoDieuKien` bị comment — Controller:23-28). Toàn bộ hành động đến từ base. **Xác minh: FE không gọi `api/ComBoSpaDichVu/*`** (chỉ có interface TS `icombo-spa.ts` dùng tên kiểu, không có HTTP call); cũng không có cross-controller BE nào gọi sang. Bảng chi tiết dịch vụ của combo được tạo/sửa/xóa GIÁN TIẾP trong `ComBoSpaController.CreateCombo`/`Update` (dsDichVu trong payload combo).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy 1 dòng chi tiết DV combo theo Id | (không gắn nút UI) | GET `api/ComBoSpaDichVu/TimTheoId/{id}` | (none — chỉ `[SBAuthorize]` class-level) | public | - | Base: trả `ComBoSpaDichVuDto` theo id (map từ entity) | EF | Không có permission key riêng (chỉ yêu cầu đăng nhập) |
| 2 | Lấy tất cả chi tiết DV combo | (không gắn nút UI) | GET `api/ComBoSpaDichVu/LayTatCa` | (none) | public | - | Base: trả toàn bộ `ComBoSpaDichVu` (GetAllAsync) — **không lọc đơn vị/combo** | EF | Base LayTatCa không filter IdDonVi/IdCombo → rủi ro trả chéo dữ liệu nếu bị gọi |
| 3 | Thêm 1 dòng chi tiết DV combo | (không gắn nút UI) | POST `api/ComBoSpaDichVu/ThemMoi` | (none) | public | - | Base: map DTO→entity, InsertAsync 1 dòng dịch vụ combo | EF | Không validate dòng có thuộc combo hợp lệ — không nên dùng trực tiếp |
| 4 | Cập nhật 1 dòng chi tiết DV combo | (không gắn nút UI) | POST `api/ComBoSpaDichVu/CapNhatThongTin` | (none) | public | - | Base check tồn tại (null → MESS_NotFound) rồi gọi service `UpdateAsync` (MarkDirty toàn bộ field: IdCombo/IdDichVu/SoLan/DonGia/ThanhTien/SuDung...) | EF | Override Update MarkDirty cả audit field (NgayTao/NguoiTao...) → có thể ghi đè dữ liệu tạo |

## Endpoint nội bộ (private / service-to-service)
- KHÔNG có endpoint `[PrivateSBAuthorize]`. Service có `TimTheoDieuKienAsync` nhưng endpoint controller bị comment → không expose.

## Quan hệ
- **Gọi sang:** không (CRUD generic 1 bảng).
- **Dữ liệu (entities):** `ComBoSpaDichVu` (IdCombo, IdDichVu, IdNhomDichVu, SoLan, DonGia, GiaBanGoc, ThanhTien, UnLimited, SuDung, MoTa, IdImage).
- **Ripple / đồng bộ:** là bảng CON của `ComBoSpa` → vòng đời gắn combo cha. Khi combo bán cho khách, các dòng dịch vụ này được COPY sang `ComBoSpaKhachHangChiTiet` (instance khách, có SoLanConLai trừ dần) — xem node `ComBoSpaKhachHangController`.

## Lỗi đã biết / lưu ý kỹ thuật
- **Controller scaffold gần như rỗng**: lớp con không thêm endpoint (chỉ ctor); chỉ còn 4 endpoint base. `ComBoSpaDichVuController.cs:23-28` — endpoint `TimTheoDieuKien` đã comment.
- **Không có permission key riêng**: dùng `[SBAuthorize]` class-level (Base) → chỉ cần đăng nhập, khó audit phân quyền nếu vô tình bị gọi.
- **`LayTatCa` (base) không lọc đơn vị/combo** → nếu endpoint bị gọi có thể trả toàn bộ bảng chi tiết DV combo của mọi đơn vị (rủi ro lộ dữ liệu) — `SBBaseController.cs:45-50`.
- **`partial class`** (Controller:13) — có thể còn endpoint ở file partial khác; node này cover file `ComBoSpaDichVuController.cs` hiện tại (không thấy partial bổ sung).
- **Quy trình thật đi qua combo cha**: tạo/sửa chi tiết DV combo nằm trong `ComBoSpaService.CreateCombo`/`Update` (validate combo thương mại không được có dịch vụ, SP/DV thuộc chi nhánh...) — đừng dùng controller này để chỉnh trực tiếp, dễ vỡ ràng buộc combo. Xem node cha `ComBoSpaController.md`.
- **`UpdateAsync` MarkDirty cả audit field** (NgayTao/NguoiTaoUserId/NgayXoa/IsDelete...) (Service:33-58) → cập nhật trực tiếp dễ ghi đè thông tin tạo/xóa.
