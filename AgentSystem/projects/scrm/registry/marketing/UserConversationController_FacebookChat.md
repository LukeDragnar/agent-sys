---
controller: UserConversationController
module: marketing
service: FacebookChat
man_hinh_ui: "ChatHub > Hội thoại > Chia hội thoại cho nhân viên (gán/bỏ gán 1 nhân viên chat phụ trách 1 cuộc trò chuyện)"
muc_dich: "Chia (phân công) quyền xử lý 1 cuộc hội thoại Facebook cho nhân viên chat: thêm/xóa bản ghi UserConversation gắn nhân viên với conversation, kiểm tra nhân viên có cờ IsNhanVienChatFacebook và thuộc đúng đơn vị/chi nhánh."
alias: ["chia hội thoại", "phân công chat", "gán nhân viên chat", "chia chat", "UserConversation", "phân hội thoại cho sale"]
auth: public
state_machine: []
deprecated: false
entities: [UserConversation, Conversation, User]
stored_procs: []
depends:
  - "IFBUserConversationService (FacebookChat.Service.Generated.FBUserConversationService)"
  - "IMongoDbClient — đọc Conversation + đọc/ghi UserConversation (Mongo collections)"
  - "IRepository<User> — kiểm tra nhân viên (IsNhanVienChatFacebook) theo đơn vị/chi nhánh"
cross_module_in:
  - "ChatHub UI — chia / bỏ chia hội thoại cho nhân viên"
cross_module_out:
  - "Mongo (UserConversation) — quyền hàng phân công, ảnh hưởng danh sách hội thoại nhân viên thấy"
---

# UserConversationController — Chia hội thoại Facebook cho nhân viên (ChatHub)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Chia hội thoại cho nhân viên | Chia hội thoại | POST `api/UserConversation/AddPermissionConversation` (UserConversationReqDto) | (không key — `[SBAuthorize]` chỉ check đăng nhập) | public | Đã chọn conversation + nhân viên | Tìm `Conversation` theo `IdConversationMG` + đúng đơn vị/chi nhánh của user → không có → "Không tìm được cuộc trò chuyện". Check đã chia chưa (UserConversation theo conv+SaleUserId) → đã có → "Người này đã quản lý cuộc trò chuyện này". Check `User.IsNhanVienChatFacebook==true` đúng đơn vị/chi nhánh → không → "Người này không phải nhân viên chat". Insert `UserConversation` (copy thông tin conv, set TypeConversation theo IsMessage/IsComment, IsAuto=false). | Mongo Conversation/UserConversation + EF User | Không key quyền (chỉ đăng nhập) → ai đăng nhập cũng chia được; `TypeConversation` chỉ set khi IsMessage **hoặc** IsComment true (cả hai false → 0/mặc định). |
| 2 | Bỏ chia hội thoại | Bỏ chia | POST `api/UserConversation/DeletePermissionConversation` (UserConversationReqDto) | (không key — `[SBAuthorize]` chỉ check đăng nhập) | public | Hội thoại đã chia cho nhân viên đó | Tìm Conversation (đúng đơn vị/chi nhánh) → không có → "Không tìm được cuộc trò chuyện". Tìm UserConversation theo conv+SaleUserId → không có → "Người này chưa quản lý cuộc trò chuyện này". Có → `DeleteOneAsync` bản ghi UserConversation. | Mongo Conversation/UserConversation | Không key quyền; xóa cứng (không soft-delete/log). |

## Quan hệ
- **Gọi sang:** Mongo (Conversation, UserConversation); EF (User) để xác thực nhân viên chat.
- **Dữ liệu (entities):** `UserConversation` (bản ghi phân công), `Conversation` (nguồn), `User` (nhân viên).
- **Ripple / đồng bộ:** thêm/xóa UserConversation → thay đổi danh sách hội thoại mà nhân viên đó nhìn thấy/được route trong ChatHub.

## Lỗi đã biết / lưu ý kỹ thuật
- `FBUserConversationService.cs:33-82` (`AddPermissionConversation`) & `:84-103` (`DeletePermissionConversation`) — cả 2 endpoint chỉ `[SBAuthorize]` không key → không phân quyền theo vai trò, mọi user đăng nhập thực hiện được.
- `FBUserConversationService.cs:66-75` — `TypeConversation` set rời theo IsMessage/IsComment; nếu cả hai false → bỏ trống (mặc định 0).
- So filter dùng `IdDonVi`/`IdChiNhanh` lúc string lúc Guid (UserConversation lưu string, Conversation/User dùng Guid) — cần khớp kiểu khi query Mongo (`:43`, `:94`).
