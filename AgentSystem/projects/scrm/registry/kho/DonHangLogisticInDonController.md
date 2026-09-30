---
controller: DonHangLogisticInDonController
module: kho
service: OrderLogistic
man_hinh_ui: "Đơn hàng logistic > Cấu hình in vận đơn (config template in cho đơn TMĐT) — list cấu hình của tôi/của chi nhánh, Thêm/Sửa/Xóa; dropdown loại tài liệu (docType) + kích cỡ (docSize) lấy từ sàn"
muc_dich: "Quản lý cấu hình in vận đơn/phiếu giao hàng cho đơn TMĐT: CRUD config in (lưu MongoDB theo user+chi nhánh), và lấy dropdown loại/kích cỡ tài liệu in (proxy sang ecommerce_connect). KHÔNG render PDF — chỉ giữ cấu hình; in thực do FE + sàn xử lý."
alias: ["cấu hình in vận đơn", "config in đơn", "in đơn TMĐT", "doc type doc size", "DonHangLogisticInDon", "cấu hình in phiếu giao hàng"]
auth: public
state_machine: []
deprecated: false
entities: [DonHangLogisticConfigInDonDto]
stored_procs: []
depends:
  - "IDonHangLogisticInDonService (OrderLogistic.Service/Generated/DonHangLogisticInDonService.cs)"
  - "IMongoDbClient (lưu DonHangLogisticConfigInDonDto — MongoDBCollection.DonHangLogisticConfigInDonDto)"
  - "IClientHelper + EcommerceConnectServiceSetting (proxy GetDocTypes/GetDocSizes sang ecommerce_connect)"
cross_module_in: []
cross_module_out:
  - "ecommerce_connect (order/GetDocTypes, order/GetDocSizes — lấy loại & kích cỡ tài liệu in từ sàn)"
---

# DonHangLogisticInDonController — Cấu hình in vận đơn (đơn TMĐT)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Thêm cấu hình in | "Thêm mới" | POST `api/DonHangLogisticInDon/ThemMoi` | `[SBAuthorize]` (mở — chỉ login) | public | - | Sinh Id + set IdDonVi/IdChiNhanh/NguoiTao/NgayTao → insert Mongo `DonHangLogisticConfigInDonDto`; trả về model vừa tạo | MongoDB | - |
| 2 | Danh sách cấu hình | load màn | POST `api/DonHangLogisticInDon/DsCauHinh` | `[SBAuthorize]` | public | - | List config theo IdDonVi+IdChiNhanh; config của user (IsOfMe=true, sort NgayCapNhat desc) xếp trước, rồi tới config người khác | MongoDB | - |
| 3 | Dropdown loại tài liệu | dropdown docType | POST `api/DonHangLogisticInDon/DropdownDsDocType` | `[SBAuthorize]` | public | - | Proxy `order/GetDocTypes` sang ecommerce_connect (theo req) → list `ResGetDocTypes` | proxy ECOMM | - |
| 4 | Dropdown kích cỡ tài liệu | dropdown docSize | POST `api/DonHangLogisticInDon/DropdownDsDocSize` | `[SBAuthorize]` | public | - | Proxy `order/GetDocSizes` sang ecommerce_connect → list kích cỡ | proxy ECOMM | - |
| 5 | Cập nhật cấu hình | "Sửa" | POST `api/DonHangLogisticInDon/CapNhat` | `[SBAuthorize]` | public | có Id | ReplaceOne Mongo theo Id; set lại tenant + NguoiTao + NgayCapNhat; Id rỗng → lỗi "Id là bắt buộc" | MongoDB | CapNhat ghi đè NguoiTaoUserId = user hiện tại (mất người tạo gốc nếu người khác sửa) |
| 6 | Xóa cấu hình | "Xóa" | POST `api/DonHangLogisticInDon/Delete/{id}` | `[SBAuthorize]` | public | có Id | Tìm + xóa config Mongo theo Id | MongoDB | - |

## Quan hệ
- **Gọi sang:** `IMongoDbClient` (CRUD config in — collection DonHangLogisticConfigInDonDto), `IClientHelper` → ecommerce_connect (`{EcommerceConnectServiceUrl}/api/order/GetDocTypes` & `GetDocSizes`).
- **Dữ liệu (entities):** `DonHangLogisticConfigInDonDto` (Mongo document: Id, IdDonVi, IdChiNhanh, NguoiTaoUserId, NgayTao/NgayCapNhat, IsOfMe + field cấu hình docType/docSize).
- **Ripple / đồng bộ:** không ripple sang module khác; chỉ đọc danh mục docType/docSize từ sàn. In vận đơn THỰC (render) do FE gọi sàn với config này, không qua BE.

## Lỗi đã biết / lưu ý kỹ thuật
- Tên controller dễ gây nhầm: KHÔNG phải "in vận đơn / render PDF" mà là **cấu hình** template in (lưu Mongo) + proxy danh mục doc của sàn. In phiếu/đơn thực tế nằm ở `DonHangLogisticController` (`InDon`, `GetDonHangInfoPrint`) và FE/sàn.
- `[TienIch("DonHangLogisticController", "Quản lý config in đơn", ...)]` — dùng tên tiện ích trùng DonHangLogisticController nhưng route base là `api/DonHangLogisticInDon/...` (controller name DonHangLogisticInDon).
- `CapNhat` ghi đè `NguoiTaoUserId`/`NguoiTaoUserName` = user đang sửa (DonHangLogisticInDonService.cs:93-94) → mất thông tin người tạo gốc nếu người khác sửa config chung của chi nhánh.
- Toàn bộ endpoint `[SBAuthorize]` mở (chỉ cần login), không phân quyền key.
- Controller `partial` (`public partial class DonHangLogisticInDonController`); node cover file Controllers/DonHangLogisticInDonController.cs (grep `class DonHangLogisticInDonController` → 1 file).
