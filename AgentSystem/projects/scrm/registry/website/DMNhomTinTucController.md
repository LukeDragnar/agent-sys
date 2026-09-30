---
controller: DMNhomTinTucController
module: website
service: Website
man_hinh_ui: "Website > Quản lý nhóm tin tức — list nhóm tin tức (danh mục bài viết) của đơn vị; Thêm/Sửa/Xóa per-row + dropdown chọn nhóm khi tạo tin tức"
muc_dich: "Quản lý danh mục nhóm tin tức của đơn vị (CRUD): lấy tất cả (kèm số lượng bài), thêm/sửa/xóa, tìm theo id; cung cấp dropdown nhóm cho màn tin tức."
alias: ["nhóm tin tức", "danh mục tin tức", "quản lý nhóm tin tức", "category tin tức", "DM nhóm tin tức"]
auth: public
state_machine: []
deprecated: false
entities: [DMNhomTinTuc]
stored_procs: []
depends: [Website (DMNhomTinTucService)]
cross_module_in: []
cross_module_out: [TinTuc (bài viết tham chiếu nhóm tin tức)]
---

# DMNhomTinTucController — Quản lý nhóm tin tức (danh mục bài viết website)

> `partial class : BaseCoreController`, route `api/DMNhomTinTuc`, `[TienIchAttribute(..., MaTienIchSCRM.TI_Website)]`. Quyền đọc mở rộng nhiều role `DV-*`; thao tác ghi (Thêm/Sửa/Xóa) chỉ `DV-AD-TONG`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy tất cả kèm số lượng | list nhóm tin tức (hiện số bài/nhóm) | POST `api/DMNhomTinTuc/LayTatCaKemSoLuong` | `DMNhomTinTuc_LayTatCaKemSoLuong` (DV-AD/KT/QLDV/NV) | public | luôn | Trả tất cả nhóm tin tức của đơn vị kèm số lượng bài viết mỗi nhóm | EF | - |
| 2 | Lấy tất cả (dropdown) | dropdown chọn nhóm (màn tạo tin tức) | POST `api/DMNhomTinTuc/LayTatCa` | `DMNhomTinTuc_LayTatCa` (DV-AD/KT/QLDV/NV) | public | - | Trả tất cả nhóm tin tức của đơn vị (không kèm số lượng) cho dropdown | EF | - |
| 3 | Thêm mới nhóm | "Thêm mới" → popup | POST `api/DMNhomTinTuc/ThemMoi` | `DMNhomTinTuc_ThemMoi` | public | luôn | Tạo nhóm tin tức mới (ThemMoiDMNhomTinTucDto + CurrentUser) | EF | - |
| 4 | Tìm theo id | "Sửa"/xem → popup | GET `api/DMNhomTinTuc/TimTheoId/{id}` | `DMNhomTinTuc_TimTheoId` (DV-AD/KT/QLDV/NV) | public | per-row | Lấy theo id; chỉ trả nếu `IdDonVi == CurrentUser.DonViId`, ngược lại "Không tìm thấy nhóm tin tức!" (NotFound) | EF | - |
| 5 | Cập nhật nhóm | "Lưu" (popup sửa) | POST `api/DMNhomTinTuc/CapNhatThongTin` | `DMNhomTinTuc_CapNhatThongTin` | public | per-row | Cập nhật nhóm (CapNhatDMNhomTinTucDto + CurrentUser) | EF | - |
| 6 | Xóa nhóm | "Xóa" (icon delete) | POST `api/DMNhomTinTuc/Xoa/{id}` | `DMNhomTinTuc_Xoa` | public | per-row | Xóa nhóm theo id (kèm CurrentUser) | EF | - |

## Quan hệ
- **Gọi sang:** nội bộ `DMNhomTinTucService`.
- **Dữ liệu (entities):** `DMNhomTinTuc` (cô lập theo `IdDonVi`). `TimTheoId` chỉ trả khi đúng đơn vị user.
- **Ripple / đồng bộ:** nhóm tin tức được tham chiếu bởi bài viết tin tức (TinTuc) hiển thị trên website. Xóa nhóm đang có bài → cần lưu ý ảnh hưởng (controller không hiện guard, kiểm tra trong service nếu trace).

## Lỗi đã biết / lưu ý kỹ thuật
- `TimTheoId` cô lập đơn vị ở tầng controller (so `IdDonVi == DonViId`); nhưng Thêm/Sửa/Xóa đẩy `CurrentUser` xuống service → cần kiểm tra service có lọc đơn vị tương tự khi cập nhật/xóa (`DMNhomTinTucController.cs:33,49,58`).
- Method ghi là `virtual` → kiểm tra lớp con khi trace.
- Không thấy guard "xóa nhóm còn bài viết" ở controller → kiểm tra `DMNhomTinTucService.XoaTheoId` để xác nhận ràng buộc với TinTuc.
