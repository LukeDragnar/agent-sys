---
controller: CauHinhChiaChatThuCongController
module: cong-viec
service: ChiaSoChat
man_hinh_ui: "Chăm sóc/Chat > Chia chat thủ công (nhân viên/quản lý chọn phòng ban → nhóm → nhân viên rồi chia ngay hội thoại trong ngày)."
muc_dich: "Hỗ trợ chia hội thoại THỦ CÔNG: lấy cây phòng ban/nhóm/nhân viên chat, lấy danh sách hội thoại theo nhóm/nhân viên, và thực hiện chia ngay (ChiaChatNhomNgay / ChiaChatUserNgay)."
alias: ["CauHinhChiaChatThuCong", "chia chat thủ công", "chia chat ngay", "chia hội thoại tay", "phòng ban nhóm nhân viên chat"]
auth: internal
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends:
  - ICauHinhChiaChatThuCongService (ChiaSoChat.Service.Generated)
cross_module_in: ["UI chia chat thủ công"]
cross_module_out: ["ChiaChatNhomNgay / ChiaChatUserNgay gán hội thoại FacebookChat cho nhóm/nhân viên ngay trong ngày"]
---

# CauHinhChiaChatThuCong — Chia chat thủ công (chia ngay)

> Phục vụ màn chia hội thoại tay. 3 nhóm chức năng: (1) lấy cây phòng ban → nhóm → nhân viên chat; (2) lấy danh sách hội thoại (theo nhóm/nhân viên); (3) chia ngay (`ChiaChatNhomNgay` cho phòng ban, `ChiaChatUserNgay` cho nhóm nhân viên). Tất cả endpoint `[SBAuthorize]` trống (chỉ cần đăng nhập). `TienIch(..., TI_ChatHub)`.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy phòng ban / nhóm / nhân viên chat | Cây chọn người nhận | `POST .../GetDSPhongBanChat` · `POST .../GetDSNhomNVChatByPhongBan` · `POST .../GetDSNhomNV` · `POST .../GetDSNhanVienChatByNhom` | `[SBAuthorize]` | internal | màn chia tay | Trả danh sách phòng ban → nhóm → nhân viên chat theo filter + đơn vị. | EF | — |
| 2 | DS nhóm/NV của hội thoại | Hiển thị ai đang giữ hội thoại | `POST .../GetDanhSachNhomChiaChatConversation` · `POST .../GetDanhSachNhanVienChiaChatConversation` | `[SBAuthorize]` | internal | dòng hội thoại | Trả nhóm/nhân viên đang gắn hội thoại. | EF | — |
| 3 | DS hội thoại để chia | Grid hội thoại | `POST .../GetListConversationChiaChat` · `POST .../GetListConversationChiaChatByNhomNV` | `[SBAuthorize]` | internal | màn chia tay | Trả list conversation theo filter / theo nhóm nhân viên. | EF | — |
| 4 | Chia ngay (nhóm / user) | Nút "Chia ngay" | `POST .../ChiaChatNhomNgay` · `POST .../ChiaChatUserNgay` | `[SBAuthorize]` | internal | đã chọn người nhận | Gán hội thoại cho phòng ban (`ChiaChatNhomNgay`) hoặc nhóm nhân viên (`ChiaChatUserNgay`) ngay trong ngày. | EF | — |
| 5 | DS user care 1 hội thoại | (xem quyền hội thoại) | `GET .../GetListUserCareConversation/{idConversation}` | `[SBAuthorize]` | internal | dòng hội thoại | Trả list IdUser có quyền với hội thoại. | EF | — |

## Quan hệ
- **Gọi sang:** `ICauHinhChiaChatThuCongService`.
- **Dữ liệu (entities):** phòng ban/nhóm/nhân viên chat + gán hội thoại (FacebookChat conversation).
- **Ripple/đồng bộ:** chia ngay đổi quyền truy cập hội thoại của nhân viên (cross sang module Chat/Inbox).

## Luồng xuyên module
- Chọn phòng ban → nhóm → nhân viên → `ChiaChatNhomNgay`/`ChiaChatUserNgay` → hội thoại được gán → nhân viên đích thấy hội thoại trong inbox (cross-service FacebookChat).

## Lỗi đã biết / lưu ý kỹ thuật
- Toàn bộ endpoint dùng `[SBAuthorize]` trống → không giới hạn role; thao tác chia tay làm đổi quyền hội thoại nhưng chỉ cần đăng nhập.
