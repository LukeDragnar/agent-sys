---
controller: LogChiaChatController
module: cong-viec
service: ChiaSoChat
man_hinh_ui: "Chăm sóc/Chat > Lịch sử chia chat — xem log quá trình chia của 1 hội thoại (ai chia, chia cho ai, khi nào)."
muc_dich: "Xem lịch sử chia chat theo hội thoại: trả toàn bộ log chia của 1 idConversation (đã chia cho nhóm/nhân viên nào, thời điểm)."
alias: ["LogChiaChat", "lịch sử chia chat", "log chia hội thoại", "log conversation chia chat"]
auth: anonymous
state_machine: []
deprecated: false
entities: [LogChiaChat]
stored_procs: []
depends:
  - ILogChiaChatService (ChiaSoChat.Service.Generated)
cross_module_in: ["UI hội thoại mở lịch sử chia"]
cross_module_out: []
---

# LogChiaChat — Lịch sử chia chat theo hội thoại

> Controller chỉ 1 endpoint read-only: trả log chia chat theo `idConversation`. `TienIch("CauHinh_FacebookChat_Controller", ..., TI_ChatHub)` (label trùng tên nhưng phục vụ log chia chat). **Endpoint KHÔNG có `[SBAuthorize]`** → công khai theo định nghĩa controller.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy log chia theo hội thoại | (mở lịch sử chia) | `GET api/LogChiaChat/GetLogChiaChatByIdConversation/{idConversation}` | (không có attribute auth) | anonymous | dòng hội thoại | Trả toàn bộ log chia của hội thoại theo `idConversation`. | EF | **Không gắn `[SBAuthorize]`** (và không scope đơn vị) → ai biết idConversation đều xem được log. |

## Quan hệ
- **Gọi sang:** `ILogChiaChatService`.
- **Dữ liệu (entities):** `LogChiaChat` (bản ghi mỗi lần chia hội thoại).
- **Ripple/đồng bộ:** chỉ đọc; được ghi bởi luồng chia (auto/thủ công).

## Lỗi đã biết / lưu ý kỹ thuật
- `LogChiaChatController.cs:20` — endpoint không có `[SBAuthorize]`, không lọc theo đơn vị → rủi ro lộ log nếu đoán được `idConversation`.
- TienIch label `"CauHinh_FacebookChat_Controller"` không khớp tên nghiệp vụ (log chia chat) — gây nhầm khi map menu/quyền.
