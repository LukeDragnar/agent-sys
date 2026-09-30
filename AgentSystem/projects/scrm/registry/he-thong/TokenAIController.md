---
controller: TokenAIController
module: he-thong
service: AIChat
man_hinh_ui: "Hệ thống > AI Chat > Token AI (admin khai báo token/khóa gọi model AI dùng cho đơn vị)."
muc_dich: "CRUD token AI (TokenAI) theo đơn vị: tìm danh sách, xem chi tiết, thêm/sửa/xóa. Token là khóa để service gọi model AI sinh câu trả lời."
alias: ["TokenAI", "token ai", "khóa ai", "api key ai", "cấu hình token model"]
auth: public
state_machine: []
deprecated: false
entities: [TokenAI]
stored_procs: []
depends:
  - ITokenAIService (AIChat.Service.Generated.TokenAI)
cross_module_in: ["consumer/Chat dùng TokenAI của đơn vị để gọi model AI"]
cross_module_out: []
---

# TokenAI — Token gọi model AI (per đơn vị)

> CRUD token AI theo `CurrentUser.DonViId`. List/Detail role HT-AD/DV-AD-TONG/HT-CSKH; thêm/sửa/xóa role DV-AD-TONG. **Khác AgentProfile: TokenAI giữ key gọi model còn active** (không comment). `TienIch(..., TI_AI)`. Khuôn CRUD chuẩn TimTheoDieuKien/TimTheoId/ThemMoi/CapNhatThongTin/Xoa.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm danh sách token | Grid token AI | `POST api/TokenAI/TimTheoDieuKien` | `TokenAI_TimTheoDieuKien` (HT-AD,DV-AD-TONG,HT-CSKH) | public | màn danh sách | Trả list TokenAI của đơn vị. | EF | — |
| 2 | Xem chi tiết | Click dòng | `GET api/TokenAI/TimTheoId/{id}` | `TokenAI_TimTheoId` (HT-AD,DV-AD-TONG,HT-CSKH) | public | dòng token | Trả chi tiết theo id. | EF | — |
| 3 | Thêm mới | Nút "Thêm mới" | `POST api/TokenAI/ThemMoi` | `TokenAI_ThemMoi` (DV-AD-TONG) | public | màn danh sách | Tạo TokenAI gắn đơn vị. | EF | — |
| 4 | Cập nhật | Nút "Lưu" | `POST api/TokenAI/CapNhatThongTin` | `TokenAI_CapNhatThongTin` (DV-AD-TONG) | public | dòng token | Cập nhật theo `req` + đơn vị. | EF | — |
| 5 | Xóa | Nút "Xóa" | `POST api/TokenAI/Xoa/{id}` | `TokenAI_Xoa` (DV-AD-TONG) | public | dòng token | Xóa theo id. | EF | — |

## Quan hệ
- **Gọi sang:** `ITokenAIService`.
- **Dữ liệu (entities):** `TokenAI` (khóa gọi model, scope đơn vị).
- **Ripple/đồng bộ:** consumer chat/AgentProfile dùng token để gọi model AI → xóa/sai token làm bot ngừng trả lời.

## Lỗi đã biết / lưu ý kỹ thuật
- Quyền key vẫn active (không comment như các controller PartnerConnect) — giữ nguyên giới hạn role.
- Delete chỉ theo `id`, không re-check đơn vị (`TokenAIController.cs:61`).
