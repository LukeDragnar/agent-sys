---
name: ecomm-vs-crm-logic-boundary
description: Phân biệt logic nên nằm ở project ECOMM (adapter) vs CRM (generic business). Generic business logic luôn ở CRM.
metadata:
  type: feedback
---

# Phân biệt logic ECOMM vs CRM/SCRM

**Rule:** Trước khi add field/config/logic, phải tự phân biệt: đây là **partner-specific adapter** hay **generic business logic**.

**Why:** SCRM/CRM phải dùng chung cho **tất cả nguồn đơn** — Shopee, TikTok, Lazada, và đơn không phải sàn (tự ship). Nếu nhồi business logic vào ECOMM hoặc UnitConnect → khi sau làm cho TikTok / Lazada / non-sàn phải copy-paste lại, hoặc tệ hơn UI bị partner-specific.

Quá trình ra quyết định: trước khi commit "add field X vào table Y" — tự hỏi "X dùng cho mọi nguồn hay chỉ Shopee?" Nếu mọi nguồn → CRM-side.

**How to apply:**

| Loại logic / data | Đặt ở | Ví dụ |
|---|---|---|
| **Partner adapter** — call API riêng từng sàn | ECOMM | `ShopeeReturnsService.Dispute()`, `TikTokReturnsService.*`, push code mapping |
| **Webhook receiver** từ sàn | ECOMM | `PushNotificationService`, `OrderReturnShopeeService.ProcessSendOrderReturnToCrm` |
| **Forward DTO** sàn → CRM | ECOMM | `CrmOrderReturnModel` — chỉ data forward, KHÔNG business decision |
| **Generic business config** (auto-create HBBTL, deadline khiếu nại, tính phí…) | **CRM/SCRM** | `DonHangHoan.IdPhieuBanTraLai`, hardcode `AUTO_CREATE_HBBTL` flag trong `Ecommerce.DonHangHoanService` |
| **Generic UI** (3 tab, 5 action types) | **CRM FE** | KHÔNG có wording "Shopee/TikTok" trên màn hình |
| **Cross-source aggregation/reporting** | **CRM** | Dashboard "Tỉ lệ khiếu nại thành công" tính qua mọi nguồn |

**Counter-example đã tránh (session 2026-05-29):**
- Initial plan add `UnitConnect.AutoCreateHBBTL` (ECOMM) → user revert: "config CRM-side generic sau, dùng chung cho mọi loại đơn"
- Đổi sang hardcode `private const bool AUTO_CREATE_HBBTL` trong SCRM `Ecommerce.DonHangHoanService` — tạm thời, để sau migrate sang config CRM-side

**Process check trước khi code:**
1. Field/logic này áp dụng cho nguồn nào? (chỉ Shopee / nhiều sàn / tất cả + non-sàn)
2. Nếu "tất cả + non-sàn" → CRM-side
3. Nếu "chỉ Shopee" → ECOMM
4. Khi nghi ngờ → hỏi user

Link: [[ecomm-no-business-logic]] (chưa viết, có thể tạo khi gặp case khác).
