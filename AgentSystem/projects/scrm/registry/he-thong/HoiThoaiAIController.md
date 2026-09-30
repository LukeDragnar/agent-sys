---
controller: HoiThoaiAIController
module: he-thong
service: AIChat
man_hinh_ui: "Inbox/Hội thoại — toggle 'AI trả lời tự động' trên từng hội thoại (chỉ áp dụng khi page cấu hình OnlyHoiThoai)."
muc_dich: "Bật/tắt AI trả lời tự động cho TỪNG hội thoại cụ thể (DatTrangThai) và kiểm tra trạng thái AI của hội thoại (KiemTra). Áp dụng khi page ở chế độ OnlyHoiThoai."
alias: ["HoiThoaiAI", "ai theo hội thoại", "bật ai hội thoại", "tắt ai conversation", "OnlyHoiThoai"]
auth: public
state_machine: [BatAITuDong, TatAITuDong]
deprecated: false
entities: []
stored_procs: []
depends:
  - IHoiThoaiAIService (AIChat.Service.Generated.HoiThoaiAI)
cross_module_in: ["UI inbox toggle AI per-conversation; consumer chat đọc trạng thái để quyết bot trả lời hay không"]
cross_module_out: []
---

# HoiThoaiAI — Bật/tắt AI theo từng hội thoại

> Quản lý cờ AI trả lời tự động ở mức **hội thoại** (page/conversation), khác PageAI ở mức page. Chỉ có ý nghĩa khi PageAI cấu hình phạm vi `OnlyHoiThoai`. `DatTrangThai` set cờ theo (DonViId, idChiNhanh, idPage, idConversition, isBatAITuDong); `KiemTra` đọc cờ. `TienIch(..., TI_AI)`, 2 endpoint `[SBAuthorize]` trống.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Bật/tắt AI cho hội thoại | Toggle "AI tự động" trên hội thoại | `POST api/HoiThoaiAI/DatTrangThai` | `[SBAuthorize]` | public | page đang OnlyHoiThoai | req null → 400 "Thiếu dữ liệu". Set cờ AI theo đơn vị+chi nhánh+page+conversation = `IsBatAITuDong`. | EF/Memory store | — |
| 2 | Kiểm tra AI hội thoại | (load trạng thái toggle) | `GET api/HoiThoaiAI/KiemTra?idChiNhanh&idPage&idConversition` | `[SBAuthorize]` | public | page đang OnlyHoiThoai | Trả bool: hội thoại có đang bật AI tự động không. | EF/Memory store | — |

## Quy tắc nghiệp vụ (gating / tier / ngưỡng)
- Trạng thái per-conversation chỉ có hiệu lực khi PageAI của page = `OnlyHoiThoai` (xem PageAI.CapNhatCauHinh). OnAll/OffAll override mức page → cờ hội thoại không quyết định.

## Quan hệ
- **Gọi sang:** `IHoiThoaiAIService`.
- **Dữ liệu (entities):** cờ AI per (đơn vị, chi nhánh, page, conversation).
- **Ripple/đồng bộ:** consumer chat đọc `DangBatAITuDongAsync` để quyết bot có trả lời hội thoại này không.

## Lỗi đã biết / lưu ý kỹ thuật
- Khóa định danh hội thoại = (DonViId + idChiNhanh + idPage + idConversition) — phải khớp đúng khóa lúc chat để KiemTra trả đúng (`HoiThoaiAIController.cs:34,44`).
