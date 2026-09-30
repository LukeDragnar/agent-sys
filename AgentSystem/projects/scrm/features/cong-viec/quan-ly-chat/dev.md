# Dev — Quản lý Chat (Chat Hub)

> Service BE: **FacebookChat** (chat/báo cáo) + **ChiaSoChat** (chia chat). FE: module **Chatbox** (`pages/chatbox`) + **ChiaChat** (`pages/chia-chat`). Tất cả gate qua tiện ích `MaTienIchSCRM.TI_ChatHub`. Chỉ dành cho dev.

## Màn hình & route (FE)
| Màn hình | Route | Component / vị trí |
|---|---|---|
| Hộp thoại ChatHub (danh sách hội thoại 1 Fanpage) | `/chathub/:idFBFanpage` (+ `/chathubmkt`, `/chathubsale` — cùng module) | `MessageComponent` (`pages/chatbox/chatbox-routing.module.ts:9`) |
| Danh sách Fanpage | `/chathub` | `FanPageComponent` (`:10`) |
| Cài đặt Fanpage | `/chathub/setting-fanpage` | `CaiDatFanpageComponent` (`:11`) |
| Kết nối Fanpage FB | `/chathub/connect/connect-fb` | `ConnectFanpageFbComponent` (`:13`) |
| Phân quyền Fanpage (user↔fanpage) | trong chatbox | `phanquyenfanpage.component.ts` |
| Phân quyền / chia care hội thoại | trong chatbox | `phan-quyen-conversation/*` (chia-nhom-care, chia-user-care) |
| Chia chat tự động (cấu hình kiểu chia) | `/chia-chat/chia-tu-dong` | `ChiaSoChatTuDongComponent` (`chia-chat-routing.module.ts:10`) |
| Báo cáo tag chat | `/chia-chat/bao-cao-tag-chat` | `ThongKeTagChatComponent` (`:14`) |
| Báo cáo theo trang | `/chia-chat/bao-cao-trang-chat` | `ThongKeTrangChatComponent` (`:18`) |
| Báo cáo theo nhân viên | `/chia-chat/bao-cao-nhan-vien-chat` | `ThongKeNhanvienChatComponent` (`:22`) |
| Log chia chat | trong chatbox | `pages/chatbox/log-chia-chat/log-chia-chat.component.ts` |

- App route prefix: `app-routing.module.ts:188` (`chathub`), `:194` (`chia-chat`), `:200`/`:206` (`chathubmkt`/`chathubsale`).

## Endpoint / Controller (trace registry)
**Hộp thoại + phân quyền Fanpage** — `FBUserFanpagesController` (registry `marketing/FBUserFanpagesController.md`, key `FBUserFanpages_FacebookChat_*`):
| Hành động | Endpoint | Quyền |
|---|---|---|
| Page của tôi | `GET api/FBUserFanpages/LayTatCaFanpageCuaUser` | `FBUserFanpages_FacebookChat_LayTatCaFanpageCuaUser` |
| User trong 1 page | `GET .../LayTatCaUserTrongFanpage/{IdFanPage}` | `..._LayTatCaUserTrongFanpage` |
| Lọc fanpage theo role (V2) | `POST .../TimTheoDieuKienAsync` | `..._TimTheoDieuKienAsync` (rẽ 4 SP theo role) |
| Thêm phân quyền | `POST .../ThemMoi` | `..._ThemMoi` (chỉ admin/admin CN ở service) |
| Cập nhật 9 cờ quyền | `POST .../CapNhatThongTin` | `..._CapNhatThongTin` |
| Xóa (rời page) | `POST .../Xoa/{id}` (id = FanPageId long) | `..._Xoa` |
| Xóa 1 bản ghi phân quyền | `POST .../XoaUF/{id}` | `[SBAuthorize]` trống (⚠ thiếu key) |

**Chia hội thoại (1 NV ↔ 1 conversation)** — `UserConversationController` (registry `marketing/UserConversationController_FacebookChat.md`):
- `POST api/UserConversation/AddPermissionConversation` / `DeletePermissionConversation` — đều `[SBAuthorize]` trống (⚠ không key).

**Chia chat tự động (cấu hình)** — `CauHinhChiaChatController` (registry `cong-viec/CauHinhChiaChatController.md`):
- `POST api/CauHinhChiaChat/TimTheoDieuKien` (`CauHinhChiaChat_TimTheoDieuKien`), `.../CauHinhChiaChat_Save`, `GET .../CHCSC_Xoa/{id}` (⚠ xóa dùng GET), `_GetById_Private` (consumer chia chat).

**Chia chat thủ công** — `CauHinhChiaChatThuCongController` (registry `cong-viec/CauHinhChiaChatThuCongController.md`):
- `POST .../GetDSPhongBanChat`, `.../GetDSNhomNVChatByPhongBan`, `.../GetDSNhanVienChatByNhom`, `.../GetListConversationChiaChat`, `.../ChiaChatNhomNgay`, `.../ChiaChatUserNgay` — toàn bộ `[SBAuthorize]` trống.

**Báo cáo chat** — `BaoCaoChatSBController` (registry `marketing/BaoCaoChatSBController.md`, key `BaoCaoChatSB_*`):
- `POST api/BaoCaoChatSB/BaoCaoTagChat` · `BaoCaoTagChatByKeyWord` · `BaoCaoTongQuanChat` · `BaoCaoTongQuanChartChat` · `BaoCaoTop5NhanVien` · `BaoCaoTyLePhanHoi` · `BaoCaoChiTietNhanVienChat`.

## Entities
- **`FBUserFanpage`** — bản ghi phân quyền user↔fanpage (9 cờ quyền + IdUser/IdFBFanpage/FanpageId/IdPhongBanFanpage/IdNhomNhanVienFanpage). `FBFanpage` (FanPageId, Ten, AccessToken, PageToken). `User`.
- **`CauHinhChiaSoChat`** — quy tắc chia tự động, scope đơn vị.
- **`UserConversation`** (Mongo) — bản ghi phân công 1 NV ↔ 1 hội thoại; `Conversation` (Mongo — TagChats, IdKhachHang, IsMessage/IsComment, LastTime); `MessageChatBox` (Mongo — IsComming, MsId, CommentId, IsHavePhone, ThoiGianPhanHoi, NguoiTaoUserId, UserIdMangSdtVe).
- **`FBTagChat`** (SQL — Description/Color), **`FBCustomer`** (SQL — PSId/IsKhachTuongTac/NgayTao).

## Luồng code
- **Lọc fanpage V2** chạy hoàn toàn qua 4 stored proc `PRC_SCRM_Facebook_Chat_FilterFanpage_{Admin|Manager|Leader|User}` — rẽ nhánh theo role (admin → toàn page đơn vị; QL → kèm phòng/nhóm; lead → kèm nhóm; user thuần → page được gán). LINQ cũ đã comment.
- **Chia chat tự động:** consumer khi hội thoại mới đến gọi `CauHinhChiaChat_GetById_Private` (validate đủ IdDonVi+IdChiNhanh+IdCSC) → áp quy tắc gán.
- **Báo cáo:** `IBaoCaoChatService` đọc trộn Mongo (`Conversation`/`MessageChatBox`) + EF (`FBTagChat`/`FBCustomer`); filter Mongo gắn cứng `IdDonVi`+`IdChiNhanh` từ `CurrentUser`.

## Bug-prone / lưu ý (path:line) — KHÔNG sửa ở session này
- **`UserConversationController` thiếu permission key**: `FBUserConversationService.cs:33-82` (Add) & `:84-103` (Delete) chỉ `[SBAuthorize]` → mọi user đăng nhập đều chia/bỏ chia hội thoại được. Xóa cứng, không log.
- **`CauHinhChiaChatThuCong` toàn bộ `[SBAuthorize]` trống** → chia tay đổi quyền hội thoại nhưng chỉ cần đăng nhập (không siết role).
- **`FBUserFanpagesController.XoaUF` (`FBUserFanpagesController.cs:130-143`)** — `[SBAuthorize]` trần, `DeleteAsync(id)` không verify chủ sở hữu/đơn vị → ai có TI_ChatHub xóa được bản ghi phân quyền bất kỳ.
- **`FBUserFanpagesController.TimTheoId`** — `GetByIdAsync` không lọc đơn vị/chi nhánh → rò rỉ ngang tenant.
- **`Xoa` admin (`FBUserFanpagesController.cs:118-122`)** — `AdminXoaFanpage` bị comment (`:121`), chỉ trả message "Liên hệ kỹ thuật"; `id` là FanPageId (long), không phải Guid record.
- **`FilterUserManagerFanpage` / `LocUserManagerPage`** — `Guid.Parse(fanpageId)` không try-parse (`FBUserFanpageService.cs:428`) → 500 nếu fanpageId không phải Guid.
- **`PhanQuyenTuongTacFanpage` (`FBUserFanpageService.cs:527-553`)** — `userRes` không null-check trước khi đọc IdPhongBan/IdNhomNhanVien → NullRef nếu IdUser không tồn tại.
- **`CapNhatThongTin`** — chỉ mark dirty 9 cờ quyền; đổi field khác (IdUser, IdFBFanpage) không lưu.
- **Báo cáo gate 31 ngày + NRE**: `BaoCaoChatService.cs:180` & `:323` deref `DenNgay.Value - TuNgay.Value` trực tiếp → NullReferenceException nếu FE không gửi TuNgay/DenNgay (trước cả khi tới gate 31 ngày).
- **Overflow int**: `BaoCaoChatService.cs:405,502` cast `(int)Average(ThoiGianPhanHoi)` → tràn nếu thời gian phản hồi (ms) lớn.
- **`BaoCaoChiTietNhanVienChat`**: `ListUserId` chỉ áp cho nhánh tin đến (`:471-472`), không áp nhánh "mang SĐT về" (`:475-487`) → cột SĐT mang về không lọc theo user chọn.
- **`CauHinhChiaChatController.cs:38`** — `CHCSC_Xoa` dùng `[HttpGet]` cho thao tác xóa (không idempotent-safe). `:22` — role có space thừa `"HT -CSKH"`.

## File source (path:line)
- BE registry nodes: `projects/scrm/registry/marketing/FBUserFanpagesController.md`, `.../UserConversationController_FacebookChat.md`, `.../BaoCaoChatSBController.md`, `projects/scrm/registry/cong-viec/CauHinhChiaChatController.md`, `.../CauHinhChiaChatThuCongController.md`.
- BE: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\` — service FacebookChat (`FBUserFanpageService.cs`, `FBUserConversationService.cs`, `BaoCaoChatService.cs`), service ChiaSoChat (`ICauHinhChiaSoChatService`, `ICauHinhChiaChatThuCongService`).
- FE chatbox: `D:\PROJECT\Git\_SourceForCode\Group2\scrm_frontend\src\app\pages\chatbox\` (routing `chatbox-routing.module.ts`).
- FE chia-chat: `...\src\app\pages\chia-chat\` (routing `chia-chat-routing.module.ts`).
