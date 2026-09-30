---
controller: CongViecExternalController
module: cong-viec
service: WorkFlow
man_hinh_ui: "nội bộ — không có UI (endpoint công khai cho hệ thống ngoài đẩy công việc vào SCRM)"
muc_dich: "Endpoint công khai cho hệ thống NGOÀI lưu công việc vào WorkFlow — không xác thực user, bảo vệ bằng 'key thời gian' (yyMMddHHmm, dung sai ±5 phút), gắn đơn vị cố định."
alias: [CongViecExternal, "công việc từ ngoài", external task, "đẩy công việc từ hệ thống khác", webhook công việc]
auth: anonymous
state_machine: []
deprecated: false
entities: [CongViec]
stored_procs: []
depends: [ICongViecService]
cross_module_in: ["hệ thống ngoài (external-system, đơn vị cố định b8f8d798-...)"]
cross_module_out: [CongViec (LuuAsync), ESExtended (publish CongViecChanged qua CongViecService)]
---

# CongViecExternal — Lưu công việc từ hệ thống ngoài (anonymous, key thời gian)

> **KHÔNG** `[SBAuthorize]`, **KHÔNG** `[TienIch]`/`[KiemTraTienIch]` → không cần Account trong HttpContext. Bảo vệ bằng "key" = giờ server định dạng `yyMMddHHmm`, hợp lệ nếu lệch giờ server ≤ **5 phút** (`KeyToleranceMinutes`). `CurrentUser` dựng cứng: `external-system`, `DonViId = b8f8d798-2349-474c-a913-317d8865f0af` (cố định), IdChiNhanh/Roles rỗng. Dùng chung `ICongViecService.LuuAsync` với màn UI.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lưu công việc từ ngoài | (gọi API, không UI) | `POST api/CongViecExternal/Save?key={yyMMddHHmm}` | (không quyền — anonymous) | anonymous | key hợp lệ ±5 phút | Key sai/hết hạn → `Ok(ResultWithError("Key không hợp lệ hoặc đã hết hạn.", code 401))`. Hợp lệ → `LuuAsync(dto, BuildFixedUser())` (đơn vị cố định). Cùng logic tạo/sửa như CongViec.Save. | EF (qua ICongViecService) | (a) Đơn vị **hardcode** Guid — chỉ phục vụ 1 tenant; tenant khác không dùng được endpoint này. (b) Key chỉ là timestamp ±5 phút → bất kỳ client biết format đều giả mạo được (không HMAC/secret). |

## Quan hệ
- **Gọi sang:** `ICongViecService.LuuAsync` (chung với CongViecController).
- **Dữ liệu (entities):** `CongViec` (tạo/sửa).
- **Ripple/đồng bộ:** qua CongViecService → publish `CongViecChanged` (ESExtended) + ghi CongViecLichSu (Mongo) như luồng UI.

## Lỗi đã biết / lưu ý kỹ thuật
- `CongViecExternalController.cs:24` (`FixedDonViId`) — đơn vị cố định hardcode → endpoint chỉ đúng cho 1 đơn vị.
- `CongViecExternalController.cs:59-66` (`IsValidKey`) — xác thực chỉ bằng timestamp `yyMMddHHmm` ±5 phút, không secret/chữ ký → mức bảo mật thấp (chống bot ngẫu nhiên, không chống tấn công có chủ đích).
- Trả lỗi key bằng `Ok(...)` (HTTP 200) kèm code 401 trong body — không phải HTTP 401 thật → client phải đọc body để biết bị từ chối.
