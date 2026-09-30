---
controller: PageAIController
module: he-thong
service: AIChat
man_hinh_ui: "Hệ thống > AI Chat > Cấu hình Page AI (đăng ký/hủy AI cho từng fanpage, đặt phạm vi trả lời OnAll/OffAll/OnlyHoiThoai, bật lại bot bị tạm dừng)."
muc_dich: "Quản lý cấu hình AI theo PAGE: liệt kê page, đăng ký/hủy/kiểm tra dùng AI, đọc/cập nhật cấu hình (phạm vi OnAll/OffAll/OnlyHoiThoai), và bật lại bot cho 1 hội thoại đang bị tạm dừng (clear botpause marker)."
alias: ["PageAI", "cấu hình page ai", "đăng ký ai page", "OnAll OffAll OnlyHoiThoai", "bật lại bot", "botpause"]
auth: public
state_machine: [OnAll, OffAll, OnlyHoiThoai]
deprecated: false
entities: [PageAI]
stored_procs: []
depends:
  - IPageAIService (AIChat.Service.Generated.PageAI)
  - IMemoryRepository (AIChat.Service.Generated.Memory)
  - "SalesChatService.BuildTenantId — dựng khóa tenant"
cross_module_in: ["UI cấu hình AI per-page; consumer chat đọc IsSuDung + phạm vi để quyết bot trả lời"]
cross_module_out: ["BatLaiBot clear marker chat:botpause trong Memory (Redis) → bot trả lời lại ngay"]
---

# PageAI — Cấu hình AI theo fanpage

> Quản lý cờ + cấu hình AI ở mức **page**. Gồm: list page, đăng ký/hủy/kiểm tra dùng AI, đọc cấu hình theo page (IsSuDung + CauHinh), cập nhật phạm vi trả lời (`OnAll`/`OffAll`/`OnlyHoiThoai`), và `BatLaiBot` (bỏ marker `chat:botpause` để bot trả lời lại ngay, không chờ hết 1 ngày). `BatLaiBot` dựng tenantId bằng `SalesChatService.BuildTenantId(DonViId, idChiNhanh, pageId)` rồi `ClearBotPausedAsync`. `TienIch(..., TI_AI)`, tất cả `[SBAuthorize]` trống (key role gốc DV-AD-TONG đã comment).

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Danh sách page | Grid page AI | `GET api/PageAI/GetList` | `[SBAuthorize]` | public | màn cấu hình | Trả list PageAI của đơn vị. | EF | — |
| 2 | Lấy cấu hình theo page | Mở chi tiết page | `GET api/PageAI/GetByPage/{pageId}` | `[SBAuthorize]` | public | dòng page | Trả `IsSuDung` + `CauHinh` của page. | EF | — |
| 3 | Đăng ký / hủy / kiểm tra dùng AI | Toggle "Dùng AI" | `POST .../DangKySuDungAI/{pageId}` · `POST .../HuySuDungAI/{pageId}` · `GET .../KiemTraSuDungAI/{pageId}` | `[SBAuthorize]` (key đã comment) | public | dòng page | Bật/tắt/kiểm tra trạng thái dùng AI của page (scope đơn vị). | EF | Key `PageAI_DangKySuDungAI`/`_HuySuDungAI`/`_KiemTraSuDungAI` (DV-AD-TONG) đã comment (L47,56,65). |
| 4 | Cập nhật phạm vi trả lời | Chọn OnAll/OffAll/OnlyHoiThoai | `POST .../CapNhatCauHinh` | `[SBAuthorize]` | public | dòng page | req null → 400. Cập nhật `CauHinh` (phạm vi) của page. | EF | — |
| 5 | Bật lại bot (bỏ tạm dừng) | Nút "Bật lại bot" | `POST .../BatLaiBot` | `[SBAuthorize]` | public | hội thoại đang botpause | Thiếu pageId/psid → 400. Dựng tenantId + `ClearBotPausedAsync` → bot trả lời lại ngay (không chờ hết 1 ngày). | Memory (Redis) | — |

## Quy tắc nghiệp vụ (gating / tier / ngưỡng)
- Phạm vi AI của page điều khiển consumer chat:
  - `OnAll` → bot trả lời mọi hội thoại của page.
  - `OffAll` → bot không trả lời.
  - `OnlyHoiThoai` → chỉ trả lời hội thoại được bật riêng (xem HoiThoaiAIController).
- `BatLaiBot` chỉ tác động marker tạm dừng (`chat:botpause`), không đổi cấu hình phạm vi.

## Quan hệ
- **Gọi sang:** `IPageAIService` (cấu hình), `IMemoryRepository` (clear botpause), `SalesChatService.BuildTenantId`.
- **Dữ liệu (entities):** `PageAI` (cấu hình per page) + marker botpause trong Memory.
- **Ripple/đồng bộ:** consumer chat đọc IsSuDung + phạm vi để quyết bot trả lời; clear botpause cho phản hồi lại ngay sau handoff.

## Lỗi đã biết / lưu ý kỹ thuật
- `PageAIController.cs:47,56,65,75,91` — key quyền `PageAI_*` (DV-AD-TONG) bị comment, thay `[SBAuthorize]` trống → mất giới hạn role.
- `BatLaiBot` phải dựng tenantId giống hệt lúc chat (`BuildTenantId(DonViId, idChiNhanh, pageId)`), lệch tham số → clear sai key, bot vẫn pause (`PageAIController.cs:100-101`).
