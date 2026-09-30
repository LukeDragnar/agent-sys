---
controller: FBQuickReplyController
module: marketing
service: FacebookChat
man_hinh_ui: "ChatHub > Cấu hình > Trả lời nhanh (quản lý câu trả lời nhanh dùng khi chat fanpage)"
muc_dich: "CRUD câu trả lời nhanh (quick reply) cho ChatHub: cú pháp (syntax) → nội dung/hình ảnh chèn nhanh khi chat, scope theo đơn vị + chi nhánh."
alias: ["quick reply", "trả lời nhanh", "tin nhắn nhanh", "câu trả lời nhanh", "FBQuickReply"]
auth: internal
state_machine: []
deprecated: false
entities: [FBQuickReply]
stored_procs: []
depends:
  - "IFBQuickReplyService (FBQuickReplyService — EF qua IRepository<FBQuickReply>)"
  - "Core/TienIch (MaTienIchSCRM.TI_ChatHub — đơn vị phải mua tiện ích ChatHub)"
cross_module_in: []
cross_module_out: []
---

# FBQuickReplyController — Quản lý câu trả lời nhanh (ChatHub)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy tất cả quick reply | Danh sách trả lời nhanh | GET `api/FBQuickReply/LayTatCa` | `FBQuickReply_FacebookChat_LayTatCa` | internal | Có TI_ChatHub | Trả list quick reply lọc theo `IdDonVi == CurrentUser.DonViId && IdChiNhanh == CurrentUser.IdChiNhanh` (scope đơn vị + chi nhánh). | EF `FBQuickReply` (`TableWithNolock`) | — |
| 2 | Tìm theo Id | (mở chi tiết) | GET `api/FBQuickReply/TimTheoId/{id:Guid}` | `FBQuickReply_FacebookChat_TimTheoId` | internal | Có TI_ChatHub | Trả 1 quick reply theo Id (`GetByIdAsync`). | EF `FBQuickReply` | Không filter đơn vị/chi nhánh ở `GetByIdAsync` → có thể đọc record của đơn vị khác nếu biết Guid. |
| 3 | Thêm mới | Thêm trả lời nhanh | POST `api/FBQuickReply/ThemMoi` | `FBQuickReply_FacebookChat_ThemMoi` | internal | Có TI_ChatHub | Set `IdDonVi/IdChiNhanh` từ CurrentUser → validate ModelState → map `FBQuickReplyDto→FBQuickReply` → `InsertAsync`. | EF `FBQuickReply` | — |
| 4 | Cập nhật | Lưu | POST `api/FBQuickReply/CapNhatThongTin` | `FBQuickReply_FacebookChat_CapNhatThongTin` | internal | Có TI_ChatHub | Validate ModelState → check tồn tại theo Id (NotFound nếu null) → `UpdateAsync` chỉ mark dirty `Syntax, Content, Image, IsPublic`. | EF `FBQuickReply` | `CapNhatThongTin` KHÔNG set lại `IdDonVi/IdChiNhanh` (khác `ThemMoi`); update chỉ cập nhật 4 field — các field khác trong DTO bị bỏ. |
| 5 | Xóa | Xóa | POST `api/FBQuickReply/Xoa/{id:Guid}` | `FBQuickReply_FacebookChat_Xoa` | internal | Có TI_ChatHub | Check tồn tại theo Id (NotFound nếu null) → `DeleteAsync(id)` → trả `true` + MESS_Delete_Success. | EF `FBQuickReply` | — |

## Quy tắc nghiệp vụ (gating)
- **Tiện ích gate:** `[TienIch("FBQuickReplyController", "Quản lý trả lời nhanh", MaTienIchSCRM.TI_ChatHub)]` controller-level → chưa mua ChatHub thì không vào.
- **Phân quyền:** mọi action mở rộng cho cả nhóm MKT lẫn SALE + DV-TRUC-PAGE (AD-TONG/CN/MCN, MKT, MKT-LEAD, QL-MKT, QL-SALE, SALE, SALE-LEAD, TRUC-PAGE).
- **Scope dữ liệu:** đọc/tạo gắn `IdDonVi + IdChiNhanh` (CRUD đa-tenant theo chi nhánh).

## Quan hệ
- **Gọi sang:** `IFBQuickReplyService` (EF thuần qua `IRepository<FBQuickReply>`); không gọi service ngoài.
- **Dữ liệu (entities):** `FBQuickReply` (Syntax, Content, Image, IsPublic, IdDonVi, IdChiNhanh).
- **Ripple / đồng bộ:** không có side-effect cross-service; quick reply chỉ được đọc khi soạn tin trong ChatHub.

## Lỗi đã biết / lưu ý kỹ thuật
- `FBQuickReplyController.cs:35-38` — `GetById` gọi `_fbQuickReplyService.GetByIdAsync(id)` (base, không filter đơn vị/chi nhánh) → rò rỉ ngang tenant nếu đoán được Guid.
- `FBQuickReplyController.cs:57-71` — `CapNhatThongTin` không gán lại `IdDonVi/IdChiNhanh` từ CurrentUser (khác `ThemMoi`).
- `FBQuickReplyService.cs:26-36` — `UpdateAsync` chỉ `MarkDirty` 4 field (`Syntax, Content, Image, IsPublic`); field khác trong DTO sẽ không được lưu.
- `FBQuickReplyService.cs:38-41` — `AddQuickReply` `throw NotImplementedException` (interface thừa, controller không gọi — `ThemMoi` đi qua `InsertAsync` của base).
- Tên action lệch nghĩa: `GetAllTagChat()` ở dòng :27 thực ra trả quick reply (copy-paste từ FBTagChats) — chỉ là tên method nội bộ, endpoint vẫn `LayTatCa`.
