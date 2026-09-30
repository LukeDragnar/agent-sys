---
controller: DataReceiveController
module: sales
service: Contact
man_hinh_ui: "nội bộ — không có UI (webhook receiver nhận contact từ landingpage / Facebook / Tiktok)"
muc_dich: "Nhận contact từ kênh ngoài (webhook landingpage, Facebook lead, Tiktok lead) đẩy vào hệ thống Contact"
alias: [DataReceive, "Webhook contact", "Nhận contact", "Lead webhook"]
auth: mixed
state_machine: []
deprecated: false
entities: [DataReceive, Contact]
stored_procs: []
depends: [IDataReceiveService]
cross_module_in: [Landingpage/Webhook, Facebook, Tiktok]
cross_module_out: [Contact]
---

# DataReceiveController — Nhận contact từ webhook (landingpage / Facebook / Tiktok)

> Controller webhook receiver, không có UI. `[TienIch]` bị comment (`:10`); endpoint webhook landingpage **không có attribute auth** (public mở), 2 endpoint mạng xã hội dùng `[PrivateSBAuthorize]`.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Nhận contact từ landingpage | (webhook) | `POST ReceiveContactFromWebhook` | KHÔNG có attribute auth (`[SBAuthorize]` bị comment `:21`) | anonymous | — | `ReceiveContactFromWebhook(contactReceive)` → tạo DataReceive/Contact | EF | **Không attribute auth** → endpoint mở public, cần verify nguồn webhook |
| 2 | Nhận lead Facebook | (webhook private) | `POST ReceiveContactFromFacebook` | `[PrivateSBAuthorize]` | internal | — | `ReceiveContactFromFacebook(contactReceive)` | EF | — |
| 3 | Nhận lead Tiktok | (webhook private) | `POST ReceiveContactFromTiktok` | `[PrivateSBAuthorize]` | internal | — | `ReceiveContactFromTiktok(contactReceive)` | EF | — |

## Endpoint nội bộ (private / service-to-service)
- Toàn bộ 3 endpoint là service-to-service / webhook (không UI). Facebook & Tiktok đi qua gateway nội bộ (`[PrivateSBAuthorize]`); landingpage nhận trực tiếp từ ngoài.
- 2 job test (`TestJobInsertContact`, `TestJobDeleteDataReceive`) đã bị **comment** (`:49-59`) — pipeline DataReceive → Contact chạy ngầm.

## Quan hệ
- **Gọi sang:** `IDataReceiveService` (nhận + đẩy vào Contact). Service này cũng được `ContactController` dùng cho CSKH (`CreateContactCSKH`).
- **Dữ liệu (entities):** `DataReceive` (bản nhận thô) → `Contact`.
- **Ripple/đồng bộ:** webhook ngoài → DataReceive → (job) → **Contact**.

## Luồng xuyên module
1. **Landingpage → Contact:** landingpage POST `ReceiveContactFromWebhook` → tạo bản DataReceive → job đẩy vào Contact (sale tác nghiệp).
2. **Facebook/Tiktok lead → Contact:** gateway nội bộ gọi `ReceiveContactFromFacebook`/`Tiktok` (private) → DataReceive → Contact.

## Lỗi đã biết / lưu ý kỹ thuật
- `DataReceiveController.cs:21-22` (`ReceiveContactFromWebhook`) — `[SBAuthorize(...)]` bị **comment**, endpoint **không có attribute auth** → mở public; cần validate/secret nguồn webhook để tránh spam contact.
- `DataReceiveController.cs:10` — `[TienIchAttribute]` bị comment → controller không khai báo tiện ích (không xuất hiện trong cây phân quyền UI).
- `DataReceiveController.cs:49-59` — 2 endpoint job test đã comment (dead/test only).
