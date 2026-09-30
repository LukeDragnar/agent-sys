---
controller: TagController
module: website
service: Website
man_hinh_ui: "Quản trị website — màn Quản lý Tag (thẻ/nhãn nội dung): list + Thêm/Sửa/Xóa tag gán cho tin tức/sản phẩm website"
muc_dich: "CRUD Tag (thẻ nội dung) của website theo đơn vị: tìm theo điều kiện, thêm/sửa/xóa, guard cross-tenant theo DonViId."
alias: ["quản lý tag website", "thẻ nội dung website", "tag admin website", "nhãn website"]
auth: public               # [SBAuthorize("key", ...)] scope DV-AD-TONG
state_machine: []
deprecated: false
entities: [Tag]
stored_procs: []
depends: [Website (ITagService)]
cross_module_in: []
cross_module_out: []
---

# TagController — Quản lý Tag website (admin)

> Controller `Website.API/Controllers/Api/TagController.cs`, route `api/Tag`, base `BaseCoreController`, `[ApiController]`, `partial`. Permission scope `DV-AD-TONG`. CRUD chuẩn, guard cross-tenant ở TimTheoId/Xoa.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện (grid) | nút Tìm kiếm | POST `api/Tag/TimTheoDieuKien` | `Tag_TimTheoDieuKien` | public | màn list | Trả list Tag theo filter + CurrentUser | EF | scope `DV-AD-TONG` |
| 2 | Thêm mới Tag | nút Thêm | POST `api/Tag/ThemMoi` | `Tag_ThemMoi` | public | popup Thêm | `ThemMoiAsync(body, user)` | EF | - |
| 3 | Tìm theo id | mở popup Sửa | GET `api/Tag/TimTheoId/{id:Guid}` | `Tag_TimTheoId` | public | per-row | Trả Tag **chỉ khi** `IdDonVi == CurrentUser.DonViId`, ngược lại lỗi (message copy nhầm "Không tìm thấy Q&A!") | EF | message lỗi sai (Q&A thay vì Tag) |
| 4 | Cập nhật Tag | nút Sửa | POST `api/Tag/CapNhatThongTin` | `Tag_CapNhatThongTin` | public | popup Sửa | `CapNhatThongTin(model, user)` | EF | - |
| 5 | Xóa Tag | nút Xóa | POST `api/Tag/Xoa/{id:Guid}` | `Tag_Xoa` | public | per-row | Guard tenant trước (`IdDonVi == DonViId`), rồi `DeleteAsync(id)` | EF | - |

## Quan hệ
- **Gọi sang:** `ITagService` (TimTheoDieuKienAsync, ThemMoiAsync, GetByIdAsync, CapNhatThongTin, DeleteAsync).
- **Dữ liệu (entities):** `Tag` (IdDonVi-scoped; gán cho tin tức/sản phẩm website).
- **Ripple / đồng bộ:** không trực tiếp; tag tham chiếu trong nội dung tin/sản phẩm.

## Lỗi đã biết / lưu ý kỹ thuật
- `TimTheoId` (L51) trả message "Không tìm thấy Q&A!" — copy nhầm từ QuestionAndAnswerController (cosmetic, nên sửa thành "Không tìm thấy Tag!").
- Guard cross-tenant ở TimTheoId & Xoa — invariant, giữ nguyên.
