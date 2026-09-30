---
controller: CauHinhController
module: marketing
service: SMS
man_hinh_ui: "Marketing > Cấu hình gửi tin (đơn vị bật/tắt 'Sử dụng cấu hình nâng cao' = chuyển dùng SMS V2; thiết lập chung gửi tin)."
muc_dich: "Cấu hình service SMS theo đơn vị: lưu/đọc setting gửi tin — quan trọng nhất là cờ `SuDungCauHinhNangCao` quyết định SmsSend đi nhánh V1 hay V2."
alias: ["CauHinh SMS", "cấu hình gửi tin", "cấu hình nâng cao sms", "SuDungCauHinhNangCao", "config sms đơn vị"]
auth: mixed
state_machine: []
deprecated: false
entities: [CauHinhSms]
stored_procs: []
depends:
  - "ICauHinhService (SMS.Services.Generated) — Save/Get setting theo đơn vị"
  - "MaTienIchSCRM.TI_Chung — tiện ích gate controller-level"
cross_module_in:
  - "[internal] SmsSendController gọi `ICauHinhService.GetAsync(idDonVi)` để rẽ V1/V2 khi gửi tin"
cross_module_out: []
---

# CauHinhController — Cấu hình service SMS (đơn vị)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lưu cấu hình | Lưu | POST `api/CauHinh/Save` | `CauHinh_Sms_LuuCauHinhSms` (DV-AD-TONG, HT-CSKH) | public | — | Lưu setting gửi tin của đơn vị (gồm cờ `SuDungCauHinhNangCao`). | EF | — |
| 2 | Lấy cấu hình | (load form) | GET `api/CauHinh/Get` | `CauHinh_Sms_LayCauHinhSms` (DV-AD-TONG, HT-CSKH) | public | — | Đọc setting của đơn vị đang đăng nhập. | EF | — |
| 3 | Lấy cấu hình gửi tin chung | (nội bộ) | GET `api/CauHinh/LayCauHinh_GuiTin_Chung` | (không) | anonymous | — | Đọc cấu hình gửi tin (cùng `GetAsync(CurrentUser)`). | EF | `[AllowAnonymous]` nhưng vẫn truyền `CurrentUser` (có thể null) (`cs:47-52`). |

## Quan hệ
- **Gọi sang:** `ICauHinhService`.
- **Dữ liệu (entities):** `CauHinhSms` (setting đơn vị, cờ `SuDungCauHinhNangCao`).
- **Ripple / đồng bộ:** cờ `SuDungCauHinhNangCao` được `SmsSendController` đọc để chọn nhánh gửi V1 (SMS/Zalo riêng) hay V2 (gộp).

## Lỗi đã biết / lưu ý kỹ thuật
- `LayCauHinh_GuiTin_Chung` `[AllowAnonymous]` (`CauHinhController.cs:47`) nhưng gọi `GetAsync(CurrentUser)` → khi gọi ẩn danh `CurrentUser` null, service phải tự xử lý.
- Đây là cờ điều khiển luồng cốt lõi của SMS V1/V2 — đổi nghiệp vụ ở đây ảnh hưởng trực tiếp SmsSendController.
