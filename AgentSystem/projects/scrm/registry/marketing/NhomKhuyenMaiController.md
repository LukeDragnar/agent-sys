---
controller: NhomKhuyenMaiController
module: marketing
service: Promotion
man_hinh_ui: "Marketing > Khuyến mãi > Nhóm khuyến mãi (CRUD nhóm để gom/phân loại các chương trình KM)"
muc_dich: "CRUD nhóm khuyến mãi (gom chương trình KM theo nhóm) và lấy danh sách nhóm kèm số lượng KM trong nhóm."
alias: ["nhóm khuyến mãi", "NhomKhuyenMai", "phân loại KM"]
auth: public
state_machine: []
deprecated: false
entities: [NhomKhuyenMai, KhuyenMai]
stored_procs: []
depends:
  - "Core/TienIch (TienIchAttribute TI_KhuyenMai)"
cross_module_in: []
cross_module_out: []
---

# NhomKhuyenMai — Quản lý nhóm khuyến mãi

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|
| 1 | Danh sách nhóm + số lượng | Danh sách | POST `GetAllWithCount` | `[SBAuthorize]` (chỉ login) | public | Trả nhóm KM kèm count KM trong nhóm, theo CurrentUser (đơn vị). | EF | quyền key bị comment |
| 2 | Lấy tất cả nhóm | (combo) | POST `GetAll` | `[SBAuthorize]` | public | Danh sách nhóm KM của đơn vị. | EF | quyền key bị comment |
| 3 | Tạo mới | Lưu | POST `Create` | `[SBAuthorize]` | public | `Create(model)`. | EF | quyền key bị comment |
| 4 | Cập nhật | Lưu | POST `Update` | `[SBAuthorize]` | public | `Update(model)`. | EF | quyền key bị comment |
| 5 | Xóa | Xóa | POST `Delete/{id}` | `[SBAuthorize]` | public | `Delete(id)`. | EF | quyền key bị comment |
| 6 | Chi tiết nhóm | Mở edit | POST `GetOne/{id}` | `[SBAuthorize]` | public | `GetOne(id)`. | EF | quyền key bị comment |

## Quan hệ
- **Gọi sang:** không.
- **Dữ liệu (entities):** `NhomKhuyenMai` (master nhóm), liên kết `KhuyenMai` (KM thuộc nhóm — nguồn count của GetAllWithCount).
- **Ripple/đồng bộ:** xóa nhóm cần cân nhắc KM đang tham chiếu (FK).

## Lỗi đã biết / lưu ý kỹ thuật
- `NhomKhuyenMaiController.cs:22-89` — **toàn bộ 6 endpoint** để `[SBAuthorize]` rỗng (mọi quyền key thật đã bị comment) → bất kỳ user login nào cũng CRUD được nhóm KM, không phân quyền theo role AD/CN/MCN như comment gốc ngụ ý.
- `Delete`/`GetOne`/`Update`/`Create` nhận id/model qua POST nhưng có check `!ModelState.IsValid` cho action chỉ nhận `Guid` từ route (Delete/GetOne) → check thừa.
