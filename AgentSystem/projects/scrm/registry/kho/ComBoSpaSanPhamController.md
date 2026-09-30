---
controller: ComBoSpaSanPhamController
module: kho
service: WarehouseProduct
man_hinh_ui: "nội bộ — không có UI riêng. Bảng chi tiết SẢN PHẨM trong combo được quản lý GIÁN TIẾP qua màn combo (combo-spa.component / combo-spa-edit): khi tạo/sửa combo, dsSanPham nằm trong payload của ComBoSpaController, không gọi controller này."
muc_dich: "CRUD generic cho bảng chi tiết SẢN PHẨM của combo (ComBoSpaSanPham: mỗi dòng = 1 sản phẩm trong 1 combo, kèm SoLuong/DonGia/ThanhTien). Là controller scaffold sinh từ SBBaseController; thực tế dsSanPham được xử lý nội bộ qua ComBoSpaController (CreateCombo/Update), nên các endpoint ở đây hầu như không được dùng trực tiếp."
alias: ["chi tiết sản phẩm combo", "combo sản phẩm", "sản phẩm trong combo", "ComBoSpaSanPham", "dòng sản phẩm combo"]
auth: public
state_machine: []
deprecated: false
entities: [ComBoSpaSanPham]
stored_procs: []
depends: [WarehouseProduct]
cross_module_in: []
cross_module_out: [ComBoSpa (node cha — dsSanPham là chi tiết của combo)]
---

# ComBoSpaSanPhamController — Chi tiết sản phẩm của combo (bảng con)

> Controller scaffold kế thừa `SBBaseController<ComBoSpaSanPham, ComBoSpaSanPhamDto>`. Lớp con KHÔNG khai báo thêm endpoint nào (endpoint `TimTheoDieuKien` bị comment — Controller:24-29). Toàn bộ hành động đến từ base. **Xác minh: FE không gọi `api/ComBoSpaSanPham/*`** (chỉ có interface TS `icombo-spa.ts` dùng tên kiểu, không có HTTP call); cũng không có cross-controller BE nào gọi sang. Bảng chi tiết sản phẩm của combo được tạo/sửa/xóa GIÁN TIẾP trong `ComBoSpaController.CreateCombo`/`Update` (dsSanPham trong payload combo).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy 1 dòng chi tiết SP combo theo Id | (không gắn nút UI) | GET `api/ComBoSpaSanPham/TimTheoId/{id}` | (none — chỉ `[SBAuthorize]` class-level) | public | - | Base: trả `ComBoSpaSanPhamDto` theo id (map từ entity) | EF | Không có permission key riêng (chỉ yêu cầu đăng nhập) |
| 2 | Lấy tất cả chi tiết SP combo | (không gắn nút UI) | GET `api/ComBoSpaSanPham/LayTatCa` | (none) | public | - | Base: trả toàn bộ `ComBoSpaSanPham` (GetAllAsync) — **không lọc đơn vị/combo** | EF | Base LayTatCa không filter IdDonVi/IdCombo → rủi ro trả chéo dữ liệu nếu bị gọi |
| 3 | Thêm 1 dòng chi tiết SP combo | (không gắn nút UI) | POST `api/ComBoSpaSanPham/ThemMoi` | (none) | public | - | Base: map DTO→entity, InsertAsync 1 dòng sản phẩm combo | EF | Không validate dòng có thuộc combo hợp lệ — không nên dùng trực tiếp |
| 4 | Cập nhật 1 dòng chi tiết SP combo | (không gắn nút UI) | POST `api/ComBoSpaSanPham/CapNhatThongTin` | (none) | public | - | Base check tồn tại (null → MESS_NotFound) rồi gọi service `UpdateAsync` (MarkDirty toàn bộ field: IdCombo/IdSanPham/SoLuong/DonGia/ThanhTien/SuDung...) | EF | Override Update MarkDirty cả audit field (NgayTao/NguoiTao...) → có thể ghi đè dữ liệu tạo |

## Endpoint nội bộ (private / service-to-service)
- KHÔNG có endpoint `[PrivateSBAuthorize]`. Service có `TimTheoDieuKienAsync` nhưng endpoint controller bị comment → không expose.

## Quan hệ
- **Gọi sang:** không (CRUD generic 1 bảng).
- **Dữ liệu (entities):** `ComBoSpaSanPham` (IdCombo, IdSanPham, IdNhomSanPham, SoLuong, DonGia, ThanhTien, SuDung, MoTa, IdImage).
- **Ripple / đồng bộ:** là bảng CON của `ComBoSpa` → vòng đời gắn combo cha. Khi combo bán cho khách, các dòng sản phẩm này được COPY sang `ComBoSpaKhachHangChiTiet` (instance khách, có SoLanConLai trừ dần) — xem node `ComBoSpaKhachHangController`.

## Lỗi đã biết / lưu ý kỹ thuật
- **Controller scaffold gần như rỗng**: lớp con không thêm endpoint (chỉ ctor); chỉ còn 4 endpoint base. `ComBoSpaSanPhamController.cs:24-29` — endpoint `TimTheoDieuKien` đã comment.
- **Không có permission key riêng**: dùng `[SBAuthorize]` class-level (Base) → chỉ cần đăng nhập, khó audit phân quyền nếu vô tình bị gọi.
- **`LayTatCa` (base) không lọc đơn vị/combo** → nếu endpoint bị gọi có thể trả toàn bộ bảng chi tiết SP combo của mọi đơn vị (rủi ro lộ dữ liệu) — `SBBaseController.cs:45-50`.
- **`partial class`** (Controller:13) — có thể còn endpoint ở file partial khác; node này cover file `ComBoSpaSanPhamController.cs` hiện tại (không thấy partial bổ sung).
- **Quy trình thật đi qua combo cha**: tạo/sửa chi tiết SP combo nằm trong `ComBoSpaService.CreateCombo`/`Update` (validate SP thuộc tất cả chi nhánh, combo thương mại...) — đừng dùng controller này để chỉnh trực tiếp, dễ vỡ ràng buộc combo. Xem node cha `ComBoSpaController.md`.
- **`UpdateAsync` MarkDirty cả audit field** (NgayTao/NguoiTaoUserId/NgayXoa/IsDelete...) (Service:33-56) → cập nhật trực tiếp dễ ghi đè thông tin tạo/xóa.
- **Song sinh với `ComBoSpaDichVuController`** (gần như identical, chỉ khác SP↔DV và SoLuong↔SoLan) — sửa 1 cái nhớ đối chiếu cái kia.
