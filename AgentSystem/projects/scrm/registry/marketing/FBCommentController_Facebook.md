---
controller: FBCommentController
module: marketing
service: Facebook
man_hinh_ui: "nội bộ — không có UI (scaffold, toàn bộ action bị comment)"
muc_dich: "Scaffold quản lý bình luận/inbox FB (FBComment). Toàn bộ endpoint bị comment-out → controller không expose action nào. Việc đọc bình luận thực tế đi qua FBMessageController_Facebook.GetByContactId (kéo comment + child từ Graph)."
alias: ["bình luận facebook", "comment FB", "inbox FB", "FBComment"]
auth: internal
state_machine: []
deprecated: true
entities: [FBComment]
stored_procs: []
depends:
  - "IFBCommentService (chỉ inject, không endpoint nào active)"
cross_module_in: []
cross_module_out: []
---

# FBCommentController (Facebook) — Quản lý bình luận/inbox (SCAFFOLD)

> **Scaffold rỗng**: ctor inject `IFBCommentService` nhưng TẤT CẢ action (TimTheoId, ThemMoi, CapNhatThongTin, Xoa, TimTheoDieuKien) bị comment-out (FBCommentController.cs:26-113). Không có endpoint HTTP nào hoạt động. `[TienIch("FBCommentController","Quản lý bình luận, inbox",TI_Facebook)]`.
>
> Đọc bình luận/inbox thực tế nằm ở `FBMessageController_Facebook.GetByContactId` (service kéo comment parent/child + message từ Graph). `FBComment` là entity store của comment đã đồng bộ về.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| — | nội bộ — không có UI | — | (toàn bộ comment-out) | — | internal | Không expose | Không có endpoint active. | — | Scaffold; logic xem comment nằm ở FBMessageController_Facebook. |

## Quan hệ
- **Dữ liệu (entities):** `FBComment` (chính) — được consume ở `FBMessageService.GetByContactId` / `GetByMessageId`.
- **Ripple / đồng bộ:** không (controller inert).

## Lỗi đã biết / lưu ý kỹ thuật
- `FBCommentController.cs:26-113` — toàn bộ method comment-out → controller không hoạt động (deprecated/scaffold).
