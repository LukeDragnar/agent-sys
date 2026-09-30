---
controller: DongBoQuyenController
module: ke-toan
service: AccountingReceive
man_hinh_ui: "nội bộ — không có UI (đồng bộ danh mục quyền/chức năng của service vào hệ thống phân quyền)"
muc_dich: "Quét toàn bộ controller/action của service AccountingReceive qua reflection, dựng danh sách quyền (tiện ích + chức năng từ [TienIch] và [SBAuthorize]) để hệ thống phân quyền đồng bộ."
alias: ["đồng bộ quyền", "lấy tất cả quyền", "sync permission", "DongBoQuyen"]
auth: internal
state_machine: []
deprecated: false
entities: [PermisionModel]
stored_procs: []
depends: ["IActionDescriptorCollectionProvider", "TienIchAttribute", "SBAuthorizeAttribute (reflection)"]
cross_module_in: ["Hệ thống phân quyền / Core gọi để lấy danh mục quyền của service AccountingReceive"]
cross_module_out: []
---

# DongBoQuyenController (AccountingReceive) — Đồng bộ danh mục quyền của service

Controller hạ tầng phân quyền (KHÔNG phải BaseController dù nằm trong folder Base). Dùng `IActionDescriptorCollectionProvider` + reflection để liệt kê mọi controller có `[TienIchAttribute]` và mọi action có `[SBAuthorizeAttribute]` (có `Quyen`), build thành `List<PermisionModel>` (tiện ích = IsPage, chức năng = IsChucNang, gắn group/version). Class gắn `[SBAuthorize()]` rỗng → chỉ cần đăng nhập.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy tất cả quyền | (nội bộ) | GET `api/DongBoQuyen/LayTatCaQuyenAsync` | (SBAuthorize rỗng — chỉ đăng nhập) | internal | controller phải có `[TienIch]` và action có `[SBAuthorize].Quyen` mới được liệt kê | Quét ActionDescriptors → group theo controller → với mỗi `[TienIch]` tạo PermisionModel tiện ích (KeyCode = [Admin.]MaChucNang), với mỗi action protected tạo PermisionModel chức năng (KeyCode = [Admin.]Quyen) → trả về danh sách | Reflection (không DB) | — |

## Quan hệ
- **Gọi sang:** không gọi service nghiệp vụ; chỉ đọc metadata routing qua `IActionDescriptorCollectionProvider`.
- **Dữ liệu (entities):** `PermisionModel` (DTO, không lưu DB ở đây).
- **Ripple / đồng bộ:** Bên gọi (Core/phân quyền) dùng kết quả để upsert danh mục quyền của service.

## Lỗi đã biết / lưu ý kỹ thuật
- `DongBoQuyenController.cs:42-47` — controller không có `[TienIch]`, hoặc `MaTienIch` rỗng → bị bỏ qua hoàn toàn (không sinh quyền) → quên gắn `[TienIch]` = action không lên danh mục phân quyền.
- `DongBoQuyenController.cs:71-92` — chỉ action có `SBAuthorize.Quyen` KHÁC rỗng mới thành "chức năng"; action `[SBAuthorize()]` rỗng KHÔNG sinh key riêng → không quản lý quyền chi tiết được.
- File nằm trong folder `Controllers/Base/` nhưng là controller thật (không phải BaseController) — dễ bị bỏ sót khi lọc theo folder.
