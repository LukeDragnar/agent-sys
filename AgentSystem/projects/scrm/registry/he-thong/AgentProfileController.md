---
controller: AgentProfileController
module: he-thong
service: AIChat
man_hinh_ui: "Hệ thống > AI Chat > Hồ sơ Agent (admin đơn vị khai báo persona/cấu hình hồ sơ Agent AI dùng để trả lời tự động)."
muc_dich: "CRUD hồ sơ Agent AI (AgentProfile) theo đơn vị: tìm danh sách, xem chi tiết, thêm/sửa/xóa. Hồ sơ định nghĩa persona/cấu hình cho bot trả lời."
alias: ["AgentProfile", "hồ sơ agent", "agent ai", "persona ai", "cấu hình agent"]
auth: public
state_machine: []
deprecated: false
entities: [AgentProfile]
stored_procs: []
depends:
  - IAgentProfileService (AIChat.Service.Generated.AgentProfile)
cross_module_in: ["PageAI/Chat dùng AgentProfile của đơn vị khi sinh câu trả lời"]
cross_module_out: []
---

# AgentProfile — Hồ sơ Agent AI (per đơn vị)

> CRUD hồ sơ Agent AI theo `CurrentUser.DonViId`. List/Detail mở rộng role (HT-AD, DV-AD-TONG, HT-CSKH); thêm/sửa/xóa giới hạn DV-AD-TONG. `TienIch(..., TI_AI)`. Khuôn CRUD chuẩn TimTheoDieuKien/TimTheoId/ThemMoi/CapNhatThongTin/Xoa.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm danh sách hồ sơ | Grid hồ sơ Agent | `POST api/AgentProfile/TimTheoDieuKien` | `AgentProfile_TimTheoDieuKien` (HT-AD,DV-AD-TONG,HT-CSKH) | public | màn danh sách | Trả list AgentProfile của đơn vị (`CurrentUser.DonViId`). | EF | — |
| 2 | Xem chi tiết | Click dòng | `GET api/AgentProfile/TimTheoId/{id}` | `AgentProfile_TimTheoId` (HT-AD,DV-AD-TONG,HT-CSKH) | public | dòng hồ sơ | Trả chi tiết theo id. | EF | — |
| 3 | Thêm mới | Nút "Thêm mới" | `POST api/AgentProfile/ThemMoi` | `AgentProfile_ThemMoi` (DV-AD-TONG) | public | màn danh sách | Tạo AgentProfile gắn đơn vị. | EF | — |
| 4 | Cập nhật | Nút "Lưu" | `POST api/AgentProfile/CapNhatThongTin` | `AgentProfile_CapNhatThongTin` (DV-AD-TONG) | public | dòng hồ sơ | Cập nhật theo `req` + đơn vị. | EF | — |
| 5 | Xóa | Nút "Xóa" | `POST api/AgentProfile/Xoa/{id}` | `AgentProfile_Xoa` (DV-AD-TONG) | public | dòng hồ sơ | Xóa theo id. | EF | — |

## Quan hệ
- **Gọi sang:** `IAgentProfileService`.
- **Dữ liệu (entities):** `AgentProfile` (persona/cấu hình bot, scope đơn vị).
- **Ripple/đồng bộ:** hồ sơ Agent được Chat/PageAI dùng khi sinh câu trả lời tự động → đổi hồ sơ ảnh hưởng nội dung bot reply.

## Lỗi đã biết / lưu ý kỹ thuật
- Tất cả thao tác scope theo `CurrentUser.DonViId` (Delete chỉ theo `id`, không re-check đơn vị) — `AgentProfileController.cs:63`.
