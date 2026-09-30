---
controller: FBCommentController
module: marketing
service: FacebookChat
man_hinh_ui: "nội bộ — không có UI (toàn bộ action đã comment; chỉ là scaffold CRUD bình luận FB)"
muc_dich: "Controller scaffold cho FBComment (bình luận/inbox Facebook). HIỆN TẠI rỗng: tất cả action CRUD đã bị comment, không expose endpoint nào. Logic bình luận thực tế nằm ở FBCommentService (dùng bởi webhook/Conversation), không qua controller này."
alias: ["bình luận FB", "FBComment", "comment Facebook", "inbox comment"]
auth: internal
state_machine: []
deprecated: true
entities: [FBComment, FBFanpage]
stored_procs: []
depends:
  - "IFBCommentService (BaseService<FBComment>) — TimTheoDieuKienAsync, GetByMessageId (dùng nội bộ, không qua controller)"
  - "ICommonService.GetFbFanpageByIdDonVi — lấy danh sách fanpage theo đơn vị"
  - "EF: IRepository<FBComment>"
cross_module_in:
  - "FBDataWebhookService — gọi IFBCommentService.GetByMessageId / cập nhật comment khi xử lý webhook bình luận"
cross_module_out: []
---

# FBCommentController — Bình luận Facebook (scaffold, chưa active)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | (Tất cả CRUD) Xem chi tiết / Thêm / Cập nhật / Xóa / Tìm theo điều kiện | — | (không có — đều comment) | `FBComment_*` (chỉ còn trong comment) | internal | — | **Không hoạt động** — mọi action `TimTheoId`/`ThemMoi`/`CapNhatThongTin`/`Xoa`/`TimTheoDieuKien` đã bị comment block (`FBCommentController.cs:26-113`). Controller chỉ có `[TienIch(TI_ChatHub)]` + ctor inject service. | — | Controller rỗng: 0 endpoint active. |

## Endpoint nội bộ (private / service-to-service)
- Controller không có endpoint active. Logic thật của `IFBCommentService` được **gọi trực tiếp từ service khác** (chủ yếu `FBDataWebhookService` khi xử lý bình luận webhook):
  - `GetByMessageId(messageId, IdDonVi)` — tìm FBComment theo CommentId trong các fanpage của đơn vị (lọc qua cache `GetFbFanpageByIdDonVi`).
  - `TimTheoDieuKienAsync(FBCommentFilterDto)` — phân trang FBComment (order theo NgayTao).
  - `UpdateAsync` override — MarkDirty toàn bộ field trước khi update (pattern chuẩn để EF cập nhật mọi cột).

## Quan hệ
- **Gọi sang:** `IFBCommentService` → EF `FBComment`; `ICommonService.GetFbFanpageByIdDonVi` (cache fanpage).
- **Dữ liệu (entities):** `FBComment` (CommentId, PostId, Content, SenderId, IdFBFanpage, IdLanding, IsCoSoDienThoai...).
- **Ripple / đồng bộ:** không có (controller rỗng). Service được webhook gọi để lưu/đối chiếu bình luận.

## Lỗi đã biết / lưu ý kỹ thuật
- `FBCommentController.cs:26-113` — toàn bộ action đã comment → controller không expose API. Đánh dấu `deprecated: true`: AI bỏ qua nhanh khi tra endpoint.
- Vẫn giữ `[TienIch("FBCommentController", ..., TI_ChatHub)]` ở class — nếu sau này un-comment, các action sẽ chịu gate tiện ích ChatHub.
- `IFBCommentService` vẫn active và được service khác dùng — đừng nhầm "controller rỗng" với "service chết".
