---
controller: ContactPhoneBlacklistController
module: sales
service: Contact
man_hinh_ui: "Sale > Danh sách black list (danh-sach-black-list — lưới + popup chi tiết blacklist / chi tiết contact blacklist)"
muc_dich: "Quản lý blacklist số điện thoại contact: thêm/sửa/xóa, tra cứu blacklist theo phone, liên kết blacklist với OrderLogistic và Contact; cung cấp check private cho service khác"
alias: [Blacklist, "Black list", "Số điện thoại đen", ContactPhoneBlacklist]
auth: mixed
state_machine: []
deprecated: false
entities: [ContactPhoneBlacklist]
stored_procs: []
depends: [IContactPhoneBlacklistService]
cross_module_in: [OrderLogistic, Contact]
cross_module_out: [OrderLogistic, Contact]
---

# ContactPhoneBlacklistController — Quản lý blacklist số điện thoại

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Danh sách blacklist (lọc) | Lưới "Black list" | `POST TimTheoDieuKien` | ContactPhoneBlacklistController_TimTheoDieuKienAsync (DV-AD-CN/MCN/TONG) | public | — | `TimTheoDieuKienV1Async(gridInfo, CurrentUser)` theo scope đơn vị | EF | — |
| 2 | Chi tiết blacklist | Mở popup chi tiết | `POST GetDataById` | `[SBAuthorize()]` (chỉ đăng nhập) | public | — | Trả 1 bản ghi blacklist | EF | Mã quyền rỗng (chỉ đăng nhập) |
| 3 | Thêm / sửa blacklist | Nút "Thêm", "Lưu" | `POST InsertAsync` · `POST UpdateAsync` · `POST InsertOrUpdateAsync` | `[SBAuthorize()]` | public | — | Thêm/sửa SĐT blacklist | EF | Tất cả CRUD chỉ `[SBAuthorize()]` — không mã/role |
| 4 | Xóa blacklist (theo id) | Nút "Xóa" | `POST DeleteDataById/{id:Guid}` | `[SBAuthorize()]` | public | — | Xóa blacklist theo id | EF | — |
| 5 | Blacklist gắn OrderLogistic | (popup chi tiết theo đơn) | `POST GetDataByIdOrderLogistic` · `POST DeleteDatabyIdOrderLogistic` | `[SBAuthorize()]` | public | — | Lấy/xóa blacklist liên kết đơn logistic | EF | — |
| 6 | Blacklist gắn Contact | (popup chi tiết contact blacklist) | `POST GetDataByIdContact` · `POST DeleteDataByIdContact` | `[SBAuthorize()]` | public | — | Lấy/xóa blacklist theo id contact | EF | — |
| 7 | Tra cứu theo SĐT | (ô tra cứu / check khi tạo đơn) | `GET GetBlackListByPhone/{phone}` · `GET CheckPhoneBlackList/{phone}` · `POST GetListPhoneBlackListByPhone` | `[SBAuthorize()]` | public | — | Trả blacklist / true-false theo phone | EF | — |

## Endpoint nội bộ (private / service-to-service)
- **[internal] Check blacklist private:** `[PrivateSBAuthorize] POST IsBlackListByPhonePrivate` (IsBlackListByPhoneInput) → trả cờ blacklist cho service khác (OrderLogistic check khi tạo đơn).
- **[internal] Upsert private:** `[PrivateSBAuthorize] POST InsertOrUpdatePrivateAsync` (ContactPhoneBlacklistDto) → thêm/sửa blacklist từ service khác.

## Quan hệ
- **Gọi sang:** `IContactPhoneBlacklistService` (toàn bộ CRUD + check).
- **Dữ liệu (entities):** `ContactPhoneBlacklist` (gắn theo phone, có thể liên kết idContact / idOrderLogistic).
- **Ripple/đồng bộ:** OrderLogistic gọi `IsBlackListByPhonePrivate` khi tạo/duyệt đơn; Contact thêm blacklist qua `ContactController.InsertOrUpdateBlackList`.

## Lỗi đã biết / lưu ý kỹ thuật
- `ContactPhoneBlacklistController.cs:41-134` — gần như **toàn bộ CRUD + tra cứu chỉ `[SBAuthorize()]`** (không mã quyền/role); chỉ `TimTheoDieuKien` (`:33`) có mã + role rõ ràng → blacklist là dữ liệu nhạy cảm nhưng phân quyền lỏng.
- `ContactPhoneBlacklistController.cs:136-149` — endpoint private `IsBlackListByPhonePrivate` / `InsertOrUpdatePrivateAsync` dùng cho OrderLogistic; thay đổi DTO phải sync 2 service.
