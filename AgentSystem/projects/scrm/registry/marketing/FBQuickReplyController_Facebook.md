---
controller: FBQuickReplyController
module: marketing
service: Facebook
man_hinh_ui: "ChatHub > Trả lời nhanh (Quick reply) — danh mục cú pháp/nội dung phản hồi mẫu dùng khi chat: list, thêm, sửa, xóa"
muc_dich: "CRUD danh mục Trả lời nhanh (FBQuickReply) theo đơn vị + chi nhánh: cú pháp (Syntax), nội dung (Content), ảnh, cờ công khai (IsPublic). Dùng để chèn nhanh phản hồi mẫu trong màn chat."
alias: ["trả lời nhanh", "quick reply", "câu trả lời mẫu", "tin nhắn mẫu", "FBQuickReply"]
auth: public
state_machine: []
deprecated: false
entities: [FBQuickReply]
stored_procs: []
depends:
  - "IFBQuickReplyService (Facebook.Service.Generated — kế thừa BaseService<FBQuickReply>)"
cross_module_in:
  - "ChatHub UI — chọn/quản lý câu trả lời nhanh"
cross_module_out: []
---

# FBQuickReplyController (Facebook) — Trả lời nhanh (Quick reply)

> CRUD chuẩn. Không có `[TienIch]`. Mọi endpoint dùng `[SBAuthorize]` key rỗng (chỉ check đăng nhập + 6 role MKT/AD).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy tất cả quick reply | (load danh sách) | GET `api/FBQuickReply/LayTatCa` | (key rỗng — 6 role) | public | — | `GetAllQuick`: lọc `IdDonVi == DonViId && IdChiNhanh == IdChiNhanh`, trả toàn bộ (không phân trang). | EF (FBQuickReply) | — |
| 2 | Xem 1 quick reply | (mở chi tiết) | GET `api/FBQuickReply/TimTheoId/{id:Guid}` | (key rỗng — 6 role) | public | — | `GetByIdAsync(id)` (BaseService). | EF | Không lọc đơn vị/chi nhánh ở GetById → ai biết Id đều xem được (cross-tenant read). |
| 3 | Thêm mới | Thêm | POST `api/FBQuickReply/ThemMoi` | (key rỗng — 6 role) | public | ModelState valid | Gán `IdDonVi/IdChiNhanh` từ CurrentUser, map DTO→entity, `InsertAsync`. | EF | — |
| 4 | Cập nhật | Lưu | POST `api/FBQuickReply/CapNhatThongTin` | (key rỗng — 6 role) | public | Bản ghi tồn tại | GetById kiểm tra tồn tại (null → MESS_NotFound); `UpdateAsync` override MarkDirty đúng 4 field: Syntax/Content/Image/IsPublic. | EF | Không kiểm tra entity thuộc đúng đơn vị trước update (chỉ check tồn tại theo Id). |
| 5 | Xóa | Xóa | POST `api/FBQuickReply/Xoa/{id:Guid}` | (key rỗng — 6 role) | public | Bản ghi tồn tại | GetById → null thì MESS_NotFound; `DeleteAsync(id)`, trả MESS_Delete_Success. | EF | Không kiểm tra đơn vị trước xóa (xóa theo Id thuần). |

## Quan hệ
- **Gọi sang:** không.
- **Dữ liệu (entities):** `FBQuickReply` (chính), scope theo `IdDonVi + IdChiNhanh`.
- **Ripple / đồng bộ:** không.

## Lỗi đã biết / lưu ý kỹ thuật
- `AddQuickReply` trong interface là `throw new NotImplementedException()` (FBQuickReplyService.cs:38-41) — không dùng, controller không gọi.
- GetById/Update/Delete đi qua Id thuần, không re-check `IdDonVi/IdChiNhanh` → rủi ro cross-tenant nếu Id bị đoán/lộ.
- Toàn bộ endpoint `[SBAuthorize]` key rỗng → chỉ chặn theo đăng nhập + 6 role, không có permission-key riêng để bật/tắt từng nút.
