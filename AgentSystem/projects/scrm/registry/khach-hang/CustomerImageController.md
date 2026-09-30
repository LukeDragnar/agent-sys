---
controller: CustomerImageController
module: khach-hang
service: Customer
man_hinh_ui: "Customer/quanly-khachhang-detail > tab customer-image (Hình ảnh khách hàng: list/upload avatar/xóa) · upload ảnh dịch vụ & ảnh thanh toán đơn hàng (gọi private từ Order)"
muc_dich: "Quản lý hình ảnh khách hàng: tìm/upload/cập nhật/xóa ảnh KH (avatar), và quản lý ảnh gắn theo dịch vụ / thanh toán đơn hàng (tạo, lấy, chuyển, xóa theo IdDonHang)"
alias: [CustomerImage, "Hình ảnh khách hàng", "Ảnh khách hàng", "Avatar KH", "Ảnh dịch vụ", "Ảnh thanh toán"]
auth: mixed
state_machine: []
deprecated: false
entities: [KhachHangImage]
stored_procs: []
depends: [ICustomerImageService, "Order (gọi private: UpdateAnhDichVu / TaoAnh / GetAnh / XoaAllAnhTheoIdDonHang / ChuyenAnhIdDonHang)", BaseDanhMucController]
cross_module_in: ["Order/OrderLogistic (đơn hàng tạo/lấy/chuyển/xóa ảnh dịch vụ & thanh toán theo IdDonHang)", "AuthenCustomer (GetDanhSachImageByKhachHangPrivate)"]
cross_module_out: []
---

# CustomerImage — Quản lý hình ảnh khách hàng

> Controller **`mixed`**: action UI dùng `[SBAuthorize("KEY",...)]`, nhóm cross-service (Order/AuthenCustomer) dùng `[PrivateSBAuthorize]`.
> Kế thừa `BaseDanhMucController<KhachHangImage, CustomerImageDto>` → có sẵn `TimTheoId/{id}`, `LayTatCa`, `ThemMoi`, `CapNhatThongTin` (CRUD danh mục mặc định, `[SBAuthorize]` controller-level).

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm ảnh KH theo điều kiện | Lưới ảnh (tab customer-image) | `POST TimTheoDieuKien` | CustomerImage_TimTheoDieuKien (DV-AD-TONG, DV-QLDV, DV-AD-CN/MCN, DV-KT, DV-KT-TONG, HT-AD, DV-SALE/QL-SALE/SALE-LEAD, DV-MKT/QL-MKT/MKT-LEAD) | public | — | `TimTheoDieuKienAsync(gridInfo)` — lọc ảnh theo filter (rộng quyền nhất: gồm cả Sale/MKT/KT) | EF | — |
| 2 | Xóa ảnh theo danh sách id | Nút "Xóa" (chọn nhiều) | `POST DeleteByListId` | CustomerImage_DeleteByListId (DV-AD-TONG/CN/MCN, DV-QLDV) | public | — | `DeleteListImage(lstId)` — xóa nhiều ảnh theo `List<Guid?>` | EF | — |
| 3 | Cập nhật ảnh KH | (lưu chỉnh sửa ảnh) | `POST CapNhatAsync` | CustomerImage_CapNhatAsync (DV-AD-TONG/CN/MCN, DV-QLDV) | public | — | `CapNhatAsync(model)` cập nhật `KhachHangImage` | EF | — |
| 4 | Upload avatar KH | Nút "Tải ảnh đại diện" | `POST UploadAvatar` | CustomerImage_UploadAvatar (DV-AD-TONG/CN/MCN, DV-QLDV) | public | — | `UploadAvatar(model)` từ `AvatarUploadModel` | EF + storage | — |
| 5 | Tạo ảnh từ dịch vụ đơn hàng | (gắn ảnh dịch vụ) | `POST TaoAnhTuDvDonHang` | CustomerImage_TaoAnhTuDvDonHang (DV-AD-TONG/CN, DV-QLDV) | public | — | `TaoAnhTuDvDonHang(model)` | EF | — |
| 6 | Lấy ảnh từ dịch vụ đơn hàng | (xem ảnh dịch vụ) | `POST GetAnhTuDvDonHang` | CustomerImage_GetAnhTuDvDonHang (DV-AD-TONG/CN, DV-QLDV) | public | — | `GetAnhTuDvDonHang(model)` | EF | — |
| 7 | Tạo ảnh thanh toán đơn hàng | (gắn ảnh thanh toán) | `POST TaoAnhThanhToanDonHang` | CustomerImage_TaoAnhThanhToanDonHang (DV-AD-TONG/CN, DV-QLDV) | public | — | `TaoAnhThanhToanDonHang(model)` | EF | — |
| 8 | Lấy ảnh từ thanh toán đơn hàng | (xem ảnh thanh toán) | `POST GetAnhTuThanhToanDonHang` | CustomerImage_GetAnhTuThanhToanDonHang (DV-AD-TONG/CN, DV-QLDV) | public | — | `GetAnhTuThanhToanDonHang(model)` | EF | — |

## Endpoint nội bộ (private / service-to-service)

> Nhóm `[PrivateSBAuthorize]` — không có UI trực tiếp, gọi cross-service (Order, AuthenCustomer):
- `POST UpdateAnhDichVu` (`RequestUploadAnhDichVu`) — Order cập nhật ảnh dịch vụ → `UpdateAnhDichVu(model)`.
- `POST GetDanhSachImageByKhachHangPrivate` (`GetDataCrossServiceDto`) — lấy danh sách ảnh theo KH (gọi nội bộ, có check `ModelState` → `MESS_Model_UnValidate`).
- `GET XoaAllAnhTheoIdDonHang/{idDonHang:Guid}` — xóa toàn bộ ảnh theo 1 đơn hàng (Order hủy đơn) → `XoaAllAnhTheoIdDonHang(idDonHang, CurrentUser)`.
- `POST ChuyenAnhIdDonHang` (`ChuyenAnhIdDonHangDto`) — chuyển ảnh giữa các IdDonHang (gộp/đổi đơn) → `ChuyenAnhIdDonHang(model)`.

## Quan hệ
- **Gọi sang:** `ICustomerImageService` (toàn bộ logic ảnh + storage upload).
- **Dữ liệu (entities):** `KhachHangImage`.
- **Ripple/đồng bộ:** Order/OrderLogistic tạo/lấy/chuyển/xóa ảnh dịch vụ & thanh toán theo `IdDonHang`; AuthenCustomer lấy ảnh KH private. Không phát event ra ngoài.

## Lỗi đã biết / lưu ý kỹ thuật
- `CustomerImageController.cs:33-34` (`TimTheoDieuKien`) — danh sách role **rất rộng** (gồm cả KT/Sale/MKT) so với các action xóa/cập nhật (chỉ AD/QLDV) → ai filter cũng xem được ảnh, nhưng chỉ AD/QLDV được sửa/xóa.
- `CustomerImageController.cs:85-95` (`GetDanhSachImageByKhachHangPrivate`) — endpoint private duy nhất có check `ModelState` → trả `MESS_Model_UnValidate`; các private khác không validate model.
- `CustomerImageController.cs:131-145` — `XoaAllAnhTheoIdDonHang` (GET, xóa theo đơn) và `ChuyenAnhIdDonHang` là private cross-service; xóa/chuyển theo IdDonHang → cần đảm bảo Order gọi đúng id, không có guard nghiệp vụ ở controller.
- Action gốc base (`ThemMoi`/`CapNhatThongTin` của BaseDanhMucController) vẫn expose với `[SBAuthorize]` controller-level — có thể trùng vai trò với `CapNhatAsync`/`UploadAvatar` (cần xác nhận FE dùng cái nào).
