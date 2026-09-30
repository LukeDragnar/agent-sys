---
controller: DongBoQuyenController
module: he-thong
service: Core
man_hinh_ui: "Hệ thống > Phân quyền chức năng > nút 'Đồng bộ quyền hệ thống'"
muc_dich: "Đồng bộ (gom) toàn bộ danh mục quyền từ ~35 microservice của hệ thống về Core: gọi song song endpoint /api/DongBoQuyenBase/LayTatCaQuyenAsync của từng service + tự quét quyền của chính Core, gộp theo KeyCode rồi InsertOrUpdate vào bảng quyền."
alias: ["đồng bộ quyền", "đồng bộ permission", "sync quyền hệ thống", "phân quyền chức năng"]
auth: internal
state_machine: []
deprecated: false
entities: [Permision]
stored_procs: []
depends: ["IPermisionService", "IClientHelper (HTTP gọi các service)", "IActionDescriptorCollectionProvider (quét quyền Core)", "ServiceSettings (URL các service)", "IUserPrincipalService"]
cross_module_in: ["Frontend Hệ thống (Phân quyền chức năng)"]
cross_module_out: ["Gọi HTTP tới ~35 service: Booking, Customer, File, Contact, Order, Promotion, WarehouseProduct, Branch, Sms, Switchboard, Noti, Website, OrderLogistic, Report, Facebook, MarketingCampaign, FacebookChat, ESDocument, Authen, HumanResource, Log, Payment, ExternalChat, ConfigChiaSoChat, Invoice, Accounting, Deposit, CommissionFee, SanXuat, WorkFlow, PreOrder, AIChat"]
---

# DongBoQuyenController — Đồng bộ danh mục quyền toàn hệ thống

Controller gom quyền (permission) từ tất cả microservice về Core. **Lưu ý đặt tên:** file nằm trong folder `Controllers/Base` nhưng KHÔNG phải base class (tên không Base/SBBase) → là controller thật. Route `api/DongBoQuyen/...`. `[TienIch("PermisionController", "Phân quyền chức năng", TI_HeThong, true)]`. Có đúng 1 endpoint công khai `DongBoDanhMucQuyen`; phần còn lại là private helper.

Cơ chế: chạy song song ~34 `Task.Run` gọi `GetQuyen(url)` tới `{ServiceUrl}/api/DongBoQuyenBase/LayTatCaQuyenAsync` của từng service (kèm `CurrentUser.Token`), cộng 1 task `GetQuyenAuthen` quét quyền nội bộ Core qua reflection (`IActionDescriptorCollectionProvider` + attribute `[TienIch]`/`[SBAuthorize]`). `Task.WhenAll` → gộp tất cả → **distinct theo `KeyCode`** (`GroupBy().Select(First)`) → `_service.InsertOrUpdateAsync(listQuyen)`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Đồng bộ quyền hệ thống | Đồng bộ quyền hệ thống | POST `api/DongBoQuyen/DongBoDanhMucQuyen` | `DongBoQuyen_DongBoDanhMucQuyen` | internal | — | Gọi song song ~35 service lấy quyền + quét quyền Core, gộp distinct theo KeyCode, InsertOrUpdate vào bảng `Permision` | HTTP fan-out + EF InsertOrUpdate; reflection (`IActionDescriptorCollectionProvider`) cho Core | Service lỗi/timeout bị nuốt → quyền service đó thiếu, không cảnh báo |

## Endpoint nội bộ (private helper)
- `GetQuyen(url)` — gọi HTTP `GET {url}` lấy `MethodResult<List<PermisionModel>>`, map sang `Permision`. **try/catch nuốt mọi lỗi → trả list rỗng** (service down không làm hỏng cả đồng bộ nhưng âm thầm bỏ quyền của service đó).
- `GetQuyenAuthen()` → `DongBoDuLieu()` — quét chính Core qua `IActionDescriptorCollectionProvider`: duyệt controller có `[TienIch]`, tạo node chức năng (`KeyCode = Admin.{MaChucNang}` nếu `IsAdmin`), rồi với mỗi action có `[SBAuthorize].Quyen` tạo node quyền con (`KeyCode = {AdminArea}{Quyen}`).
- `IsProtectedAction(...)` — true nếu method có `[SBAuthorize]`.

## Luồng xuyên module
- Admin bấm "Đồng bộ quyền hệ thống" → Core fan-out HTTP tới `/api/DongBoQuyenBase/LayTatCaQuyenAsync` của ~35 service + tự quét Core → gộp distinct KeyCode → InsertOrUpdate bảng quyền dùng cho toàn bộ phân quyền. Mỗi service mới phải có sẵn endpoint `DongBoQuyenBase/LayTatCaQuyenAsync` và URL trong `ServiceSettings` mới được gom.

## Quan hệ
- **Gọi sang:** `IClientHelper.GetAsync` (HTTP tới từng service), `IPermisionService.InsertOrUpdateAsync`, `IActionDescriptorCollectionProvider` (reflection Core), `ServiceSettings` (URL).
- **Dữ liệu (entities):** `Permision` (map từ `PermisionModel`), distinct theo `KeyCode`.
- **Ripple / đồng bộ:** ghi lại toàn bộ danh mục quyền → là nguồn cho UI phân quyền role.

## Lỗi đã biết / lưu ý kỹ thuật
- `DongBoQuyenController.cs:199-220` — `GetQuyen` **try/catch nuốt mọi exception trả về list rỗng** → service lỗi/timeout làm quyền service đó biến mất khỏi danh mục mà không có cảnh báo/log → dễ "mất quyền" thầm lặng.
- `DongBoQuyenController.cs:49-80` — danh sách URL service **hardcode cứng ~34 dòng** (`ServiceSettings`); thêm service mới phải sửa code tại đây (đánh số nhảy `t1..t34` thiếu `t17` → dễ sót).
- `DongBoQuyenController.cs:84` — `Task.WhenAll` không có timeout tổng → 1 service treo kéo dài cả request đồng bộ (mỗi `GetQuyen` chỉ dựa vào timeout của `IClientHelper`).
- `DongBoQuyenController.cs:92` — gộp distinct theo `KeyCode` lấy `First()` → nếu 2 service trùng KeyCode, mô tả/quyền của service sau bị bỏ (thứ tự không xác định do chạy song song).
- File ở folder `Controllers/Base` nhưng KHÔNG phải base class — chỉ là vị trí lưu, route/behaviour như controller thường.
