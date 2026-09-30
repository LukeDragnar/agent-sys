# TikTok Shop — Return, Refund, Cancel API Overview

> Tóm tắt docs chính thức của TikTok Shop Partner Center về Cancel / Refund / Return / Replacement API.
> Dùng làm reference cho dev khi tích hợp luồng after-sales giữa EcommerceConnect ↔ TikTok Shop.

---

## TL;DR — Đọc trong 1 phút

**4 loại after-sales:** Cancel (trước ship) · Refund (không trả hàng) · Return (trả hàng + hoàn) · Replacement (đổi hàng).

**Subscribe 5 webhook:**
- `Cancellation Status Change` — đơn hủy
- `Return Status Change` (type 12) — luồng return cũ, 1-webhook gánh hết
- `Aftersales Request Status Update` (type 64) — stage 1: seller approve/reject request
- `RMA Status Update` (type 65) — stage 2: seller review package, SLA 2 business days
- `Refund Success` (type 67) — refund hoàn tất, có amount + line items

**Khi nhận webhook → call API:**
- Lifecycle history → `GET /return_refund/202309/returns/{return_id}/records`
- Current detail + line items → `POST /return_refund/202309/returns/search` (filter `return_id`)
- Order gốc → `Get Order Detail`
- Approve / Reject → `POST /return_refund/202309/returns/{return_id}/approve` hoặc `/reject`

**SLA cần tuân thủ:**
- 48h seller response cho request — quá hạn TikTok auto-approve.
- 2 business days seller review package (RMA stage 2).
- UK: dùng `ON_HOLD` làm buyer remorse window.
- US: thị trường duy nhất cho phép seller initiate return.

**Idempotency:** Dùng `tts_notification_id` làm dedup key.
**Base URL:** `https://open-api.tiktokglobalshop.com` · **Version:** `202309`.

---

## 1. Khái niệm chung

Có **4 loại** "post-transaction" (after-sales) — khởi tạo bởi **buyer** hoặc **seller**:

| Loại | Mô tả |
|------|------|
| **Cancel** | Hủy đơn trước khi ship |
| **Refund** | Hoàn tiền (không trả hàng) |
| **Return** | Trả hàng + hoàn tiền |
| **Replacement** | Đổi hàng (chỉ buyer initiate) |

**Initiator:**
- `BUYER` — Người mua đặt đơn
- `SELLER` — Người bán (TikTok Shop seller)
- `SYSTEM` — TikTok Shop tự xử lý theo policy (vd: package lost trong transit > 7 ngày)
- `OPERATOR` — Customer Service của TikTok Shop

**2 cách triển khai search:**
1. **Subscribe webhook** (khuyến nghị):
   - `Cancellation Status Change`
   - `Return Status Change`
2. **Poll** order list / cancel-return list định kỳ.

---

## 2. Ma trận khả dụng theo Order Status

| Action | Initiator | UNPAID | ON_HOLD | AWAITING_SHIPMENT | AWAITING_COLLECTION | IN_TRANSIT | DELIVERED | COMPLETED |
|--------|-----------|--------|---------|-------------------|---------------------|------------|-----------|-----------|
| Cancel | Buyer  | Y | Y (auto, UK) | Y | N | N | N | N |
| Cancel | Seller | N | Y | Y* | Y | N | N | N |
| Refund | Buyer  | N | N | N | Y | Y* | Y | N |
| Refund | Seller | N | N | N | Y | Y* | Y* | N |
| Return | Buyer  | N | N | N | N | N | Y | N |
| Return | Seller | N | N | N | N | N | Y* (US) | N |

`*` = tùy market policy. Tra Seller Academy của market tương ứng.

---

## 3. Cancel Flow

### Cancel Types
- **`BUYER_CANCEL`** — Buyer khởi tạo, cần seller review. Riêng đơn `ON_HOLD` thì TikTok auto-approve thay seller.
- **`CANCEL`** — Seller/System/Operator direct cancel.

### Status flow
```
REQUEST_APPLYING ──► CANCEL_SUCCESS ──► COMPLETE
                ╰──► CLOSED
```

### Business case
- **Buyer flow:** Reverse webhook → Search Cancellation API → Approve / Reject Cancellation API.
- **Seller flow:** Get Order Detail → (Get Aftersale Eligibility, optional) → Cancel Order API.

### SLA quan trọng
- **Trước 2 business-day shipping SLA:** Seller có 24h để xử lý. Quá hạn → TikTok auto-approve và refund.
- **Sau SLA shipping nhưng trước "In transit":** TikTok auto-approve và refund.
- Seller có 2 cách giải quyết cancellation request: **Upload tracking number** hoặc **Approve cancellation**.

### Reasons
Tra danh sách official cancel reasons trên TikTok Partner Center docs (mục "our list of cancel reasons").

---

## 4. Refund Flow (không trả hàng)

### Status flow
```
RETURN_OR_REFUND_REQUEST_PENDING
 ├─(Step 1)→ REQUEST_SUCCESS ──(Step 2)→ RETURN_OR_REFUND_REQUEST_COMPLETE
 ├─(Step 3)→ REQUEST_REJECTED ──(Step 5)→ RETURN_OR_REFUND_CANCEL
 │                            ──(Step 6)→ REQUEST_SUCCESS (arbitration buyer thắng)
 └─(Step 4)→ RETURN_OR_REFUND_CANCEL (buyer cancel)
```

### Status table
| Status | Mô tả |
|--------|------|
| `RETURN_OR_REFUND_REQUEST_PENDING` | Buyer khởi tạo refund, chờ seller/platform approve |
| `REQUEST_SUCCESS` | Refund approved, buyer sẽ được hoàn tiền |
| `REQUEST_REJECTED` | Seller từ chối refund |
| `RETURN_OR_REFUND_CANCEL` | Request đã bị hủy (bởi buyer hoặc system) |
| `RETURN_OR_REFUND_REQUEST_COMPLETE` | Refund đã hoàn thành, tiền đã chuyển |

### Step triggers
| Step | From → To | Trigger | Initiator |
|------|-----------|---------|-----------|
| 1 | PENDING → REQUEST_SUCCESS | Refund được approve | Seller/System |
| 2 | REQUEST_SUCCESS → COMPLETE | Tiền đã được hoàn | System |
| 3 | PENDING → REJECTED | Seller từ chối | Seller |
| 4 | PENDING → CANCEL | Buyer hủy request | Buyer |
| 5 | REJECTED → CANCEL | Hết hạn arbitration / arbitration thắng seller | System |
| 6 | REJECTED → REQUEST_SUCCESS | Buyer kháng nghị thắng | System |

### Business case
- **Buyer flow:** Reverse webhook → Get Search Returns API → Get Approve Return API (`decision = APPROVE_REFUND`) / Get Reject Return API (`decision = REJECT_REFUND`).
- **Seller flow:** Get Order Detail → (Get Aftersale Eligibility, optional) → Call Create Return API.

---

## 5. Return Flow (trả hàng + hoàn tiền)

### Status flow
```
RETURN_OR_REFUND_REQUEST_PENDING
 ├─(1)→ AWAITING_BUYER_SHIP ──(2)→ BUYER_SHIPPED_ITEM
 │       ↑(10) arbitration thắng buyer            ├─(3)→ REQUEST_SUCCESS ──(4)→ COMPLETE
 │                                                 └─(5)→ RECEIVE_REJECTED
 │                                                         ├─(6)→ REQUEST_SUCCESS (arbitration thắng buyer)
 │                                                         └─(11)→ RETURN_OR_REFUND_CANCEL
 ├─(7)→ REQUEST_REJECTED ──(9)→ RETURN_OR_REFUND_CANCEL
 │                       ──(10)→ AWAITING_BUYER_SHIP
 └─(8)→ RETURN_OR_REFUND_CANCEL (buyer cancel)
```

### Status mở rộng so với Refund
| Status | Mô tả |
|--------|------|
| `AWAITING_BUYER_SHIP` | Chờ buyer gửi hàng về seller. Quá deadline → TikTok đóng request |
| `BUYER_SHIPPED_ITEM` | Buyer đã ship hàng |
| `RECEIVE_REJECTED` | Seller từ chối package (không pass quality check) |

### Return-less Refund
- Seller có thể **cho buyer giữ hàng** mà vẫn refund.
- Cách: gọi Approve Return API với option "buyer keep item".

### Business case
- **Buyer flow:**
  ```
  Reverse webhook → Get Search Returns API → quyết định:
    ├─ Decline → Get Reject Return API (decision = REJECT_RETURN)
    └─ Accept → Get Approve Return API (decision = APPROVE_RETURN)
                 ↓
              khi nhận hàng → check quality:
                ├─ Pass → Get Approve Return API (decision = APPROVE_RECEIVED_PACKAGE)
                └─ Fail → Get Reject Return API (decision = REJECT_RECEIVED_PACKAGE)
  ```
- **Seller flow:** Get Order Detail → (Get Aftersale Eligibility, optional) → Get Create Returns API → Approve/Reject Received Package theo quality check.

### Step triggers (rút gọn)
| Step | From → To | Trigger | Initiator |
|------|-----------|---------|-----------|
| 1 | PENDING → AWAITING_BUYER_SHIP | Return được approve | Seller/System |
| 2 | AWAITING_BUYER_SHIP → BUYER_SHIPPED_ITEM | Buyer ship hàng (platform shipping auto, self-arrange thì buyer upload tracking) | System / Buyer |
| 3 | BUYER_SHIPPED_ITEM → REQUEST_SUCCESS | Seller quality check OK | Seller/System |
| 4 | REQUEST_SUCCESS → COMPLETE | Refund hoàn thành | System |
| 5 | BUYER_SHIPPED_ITEM → RECEIVE_REJECTED | Seller từ chối package | Seller |
| 6 | RECEIVE_REJECTED → REQUEST_SUCCESS | Arbitration thắng buyer | System |
| 7 | PENDING → REJECTED | Seller reject return | Seller |
| 8 | PENDING → CANCEL | Buyer cancel | Buyer |
| 9 | REJECTED → CANCEL | Hết arbitration window / arbitration thắng seller | System |
| 10 | REJECTED → AWAITING_BUYER_SHIP | Arbitration thắng buyer | System |
| 11 | RECEIVE_REJECTED → CANCEL | Hết arbitration / arbitration thắng seller | System |

---

## 6. Replacement Flow (chỉ buyer initiate)

### Status flow
```
REPLACEMENT_REQUEST_PENDING
 ├─(Case 1)→ REPLACEMENT_REQUEST_REJECT
 │           └─(Case 4)→ REPLACEMENT_REQUEST_COMPLETE (sau dispute)
 ├─(Case 2)→ REPLACEMENT_REQUEST_COMPLETE
 ├─(Case 3)→ REPLACEMENT_REQUEST_REFUND_SUCCESS (không đủ kho → refund)
 └─(Case 4 buyer-side)→ REPLACEMENT_REQUEST_CANCEL
```

### Case triggers
| Case | From → To | Trigger | Initiator |
|------|-----------|---------|-----------|
| 1 | PENDING → REJECT | Seller từ chối, buyer có thể dispute | Seller |
| 2 | PENDING → COMPLETE | Seller hoặc system auto-accept → tạo đơn mới fulfill | Seller/System |
| 3 | PENDING → REFUND_SUCCESS | Seller refund thay vì replace (hết kho) | Seller |
| 4 | REJECT → COMPLETE | Sau dispute, TikTok quyết định seller phải replace | TikTok |

### Business case
```
Call Search Return API → Accept / Reject / Refund decision:
  ├─ Accept → Approve Return API (decision = APPROVE_REPLACEMENT)
  │           → New order generated → Get Order Detail → Fulfillment API (arrange shipment)
  ├─ Reject → Reject Return API (decision = REJECT_REPLACEMENT)
  └─ Refund instead → Approve Return API (decision = ISSUE_REPLACEMENT_REFUND)
```

**Đơn replacement mới sinh ra phải fulfill như đơn bình thường.**

---

## 7. TikTok Shop Marketplace Policies

### Policy for Responding to Buyer Requests
- **Seller phải phản hồi trong 48h**, không thì TikTok auto-approve.
- Áp dụng cho: Cancel, Refund, Return (cả request lẫn package nhận về sau khi return).

### Policy for Initiating Cancellations

**Buyers:**
- Tự do hủy đơn `UNPAID`.
- **UK:** TikTok dùng `ON_HOLD` làm buyer remorse window → auto-approve cancel khi đang `ON_HOLD`.
- **Ngoài UK:** Remorse window = từ khi sang `AWAITING_SHIPMENT` + 1h.
- Hết remorse window → cancel phải qua seller review.
- Sang `AWAITING_COLLECTION` trở đi → buyer không cancel được.

**Sellers:**
- KHÔNG cancel được đơn UNPAID.
- Cancel được khi status: `ON_HOLD`, `AWAITING_SHIPMENT`, `AWAITING_COLLECTION`.

### Policy for Initiating Refunds
- **Buyer:** Khi order ở `AWAITING_COLLECTION`, `IN_TRANSIT`, `DELIVERED`.
- **Seller:** Khi order ở `DELIVERED`.

### Policy for Initiating Returns
- **Buyer:** Khi order ở `DELIVERED`.
- **Seller initiate return on behalf:** **CHỈ ÁP DỤNG MARKET US** (status `IN_TRANSIT`, `DELIVERED`). Các market khác seller không được initiate return thay buyer.

---

## 8. FAQ chính

| Câu hỏi | Trả lời |
|---------|--------|
| Seller có thể partial refund không? | Có, nhưng tùy market policy |
| Cancel khác Refund thế nào? | Cancel = trước khi ship. Sau khi ship → phải dùng Refund |
| Seller initiate refund có cần thêm approval? | Không. TikTok auto refund cho buyer |

---

## 9. Appendix — Policy links theo market

Mỗi market có policy riêng tại Seller Academy:
- **US:** `https://seller-us.tiktok.com/university/essay?identity=1&role=1&knowledge_id=3253210454181634&from=policy`
- **GB (UK):**
  - Cancellation: `https://seller-uk.tiktok.com/university/essay?identity=1&role=1&knowledge_id=7753822474782466&from=policy`
  - Refund & Return: `https://seller-uk.tiktok.com/university/essay?identity=1&role=1&knowledge_id=7753847099623170&from=policy`
- **ID:** `https://seller-id.tiktok.com/university/essay?identity=1&role=1&knowledge_id=6837727601690370&from=policy`
- **TH:** `https://seller-th.tiktok.com/university/essay?identity=1&role=1&knowledge_id=6837799362217730&from=policy`
- **MY:** `https://seller-my.tiktok.com/university/essay?identity=1&role=1&knowledge_id=7753775054259970&from=policy`
- **VN:** `https://seller-vn.tiktok.com/university/essay?identity=1&role=1&knowledge_id=6837773789234946&from=policy`
- **PH:** `https://seller-ph.tiktok.com/university/essay?identity=1&role=1&knowledge_id=7654203686340353&from=policy`
- **SG:** `https://seller-sg.tiktok.com/university/essay?identity=1&role=1&knowledge_id=7654182014830338&from=policy`

---

## 10. Webhook: Return Status Change — Spec chi tiết

> Webhook TikTok bắn về khi `return_status` của 1 order thay đổi.
> Đây là input chính của luồng after-sales trong `EcommerceReceive.API` (TikTok partner).

### 10.1. Trigger scenarios (khi nào webhook bắn?)

#### Return / Refund request
| # | Scenario | `return_status` bắn về |
|---|----------|------------------------|
| 1 | Buyer khởi tạo return/refund, chờ seller review | `RETURN_OR_REFUND_REQUEST_PENDING` |
| 2 | Seller từ chối request của buyer | `REFUND_OR_RETURN_REQUEST_REJECT` |
| 3 | Return được approved, chờ buyer ship hàng về (có deadline — quá hạn auto-close) | `AWAITING_BUYER_SHIP` |
| 4 | Buyer drop-off package thành công, hoặc upload tracking number | `BUYER_SHIPPED_ITEM` |
| 5 | Seller từ chối refund cho package đã nhận (quality check fail) | `REJECT_RECEIVE_PACKAGE` |
| 6 | Seller chấp nhận refund / phát hành refund cho return | `RETURN_OR_REFUND_REQUEST_SUCCESS` |
| 7 | Buyer hoặc system đóng request | `RETURN_OR_REFUND_REQUEST_CANCELLED` |
| 8 | Return/refund hoàn tất, đã chuyển tiền | `RETURN_OR_REFUND_REQUEST_COMPLETE` |

#### Replacement request
| # | Scenario | `return_status` bắn về |
|---|----------|------------------------|
| 9 | Buyer khởi tạo replacement, chờ seller review | `REPLACEMENT_REQUEST_PENDING` |
| 10 | Seller từ chối replacement | `REPLACEMENT_REQUEST_REJECT` |
| 11 | Seller quyết định refund thay vì replace | `REPLACEMENT_REQUEST_REFUND_SUCCESS` |
| 12 | Buyer hủy replacement | `REPLACEMENT_REQUEST_CANCEL` |
| 13 | Seller approve replacement (tạo đơn mới ship) | `REPLACEMENT_REQUEST_COMPLETE` |

> ⚠️ **Lưu ý naming inconsistency** trong docs TikTok:
> - Trigger list có `REFUND_OR_RETURN_REQUEST_REJECT` và `RETURN_OR_REFUND__REQUEST_CANCELLED` (2 dấu `_`).
> - Data spec dùng `RETURN_OR_REFUND_REQUEST_CANCEL` (1 dấu `_`, không có `LED`).
> - **Khi implement: dùng tên trong data spec làm chuẩn**, nhưng code parser nên defensive (handle cả 2 dạng nếu TikTok thực sự bắn về).

### 10.2. Payload spec

| Parameter | Mô tả | Sample |
|-----------|------|--------|
| `order_id` | ID của TikTok Shop order | `577087614418520388` |
| `return_role` | Ai khởi tạo: `BUYER` / `SELLER` / `SYSTEM` | `BUYER` |
| `return_type` | Loại request: `REFUND` / `REPLACEMENT` / `RETURN_AND_REFUND` | `REFUND` |
| `return_status` | Status hiện tại (xem bảng đầy đủ bên dưới) | `RETURN_OR_REFUND_REQUEST_PENDING` |
| `return_id` | ID của return request cụ thể | `4035318504086604100` |
| `create_time` | Unix timestamp (seconds) — lúc tạo request | `1627587600` |
| `update_time` | Unix timestamp (seconds) — lúc update status | `1627587600` |

### 10.3. Full status enum (cho mapping)

```
AWAITING_BUYER_SHIP
BUYER_SHIPPED_ITEM
REFUND_OR_RETURN_REQUEST_REJECT
REJECT_RECEIVE_PACKAGE
REPLACEMENT_REQUEST_CANCEL
REPLACEMENT_REQUEST_COMPLETE
REPLACEMENT_REQUEST_PENDING
REPLACEMENT_REQUEST_REFUND_SUCCESS
REPLACEMENT_REQUEST_REJECT
RETURN_OR_REFUND_REQUEST_CANCEL
RETURN_OR_REFUND_REQUEST_COMPLETE
RETURN_OR_REFUND_REQUEST_PENDING
RETURN_OR_REFUND_REQUEST_SUCCESS
```

### 10.4. Event payload example

```json
{
  "type": 12,
  "tts_notification_id": "7327112393057371910",
  "shop_id": "7494049642642441621",
  "timestamp": 1644412885,
  "data": {
    "order_id": "576486316948490001",
    "return_role": "BUYER",
    "return_type": "REFUND",
    "return_status": "RETURN_OR_REFUND_REQUEST_PENDING",
    "return_id": "576486316948490001",
    "create_time": 1627587600,
    "update_time": 1644412885
  }
}
```

**Notes:**
- `type: 12` = Return Status Change event type (cần verify với docs TikTok webhook type list).
- `tts_notification_id` — unique notification ID, dùng để idempotency check (tránh xử lý trùng).
- `shop_id` — shop nhận webhook → dùng để lookup `UnitConnect` trong EcommerceConnect.
- `timestamp` — thời điểm webhook fire ở TikTok side.
- `data.update_time` — thời điểm status thực sự đổi (có thể khác `timestamp` nếu webhook bị delay).

### 10.5. Suy ra hành động xử lý (theo `return_status` × `return_type`)

| Status | Action gợi ý phía CRM (SCRM) |
|--------|------------------------------|
| `RETURN_OR_REFUND_REQUEST_PENDING` | Tạo task "Pending review" cho seller, set deadline 48h |
| `AWAITING_BUYER_SHIP` | Update order status, hiển thị "Chờ buyer ship hàng" |
| `BUYER_SHIPPED_ITEM` | Notify seller chuẩn bị nhận hàng + quality check |
| `REJECT_RECEIVE_PACKAGE` | Tạo case dispute nội bộ, theo dõi arbitration |
| `RETURN_OR_REFUND_REQUEST_SUCCESS` | Update order: đã chấp nhận refund, chờ chuyển tiền |
| `RETURN_OR_REFUND_REQUEST_COMPLETE` | Đánh dấu order đã refund/return xong, sync kế toán |
| `RETURN_OR_REFUND_REQUEST_CANCEL` | Đóng case, log lý do |
| `REFUND_OR_RETURN_REQUEST_REJECT` | Notify buyer, mở khả năng dispute |
| `REPLACEMENT_REQUEST_PENDING` | Tạo task seller review + check tồn kho |
| `REPLACEMENT_REQUEST_COMPLETE` | Tạo đơn replacement mới, sync vào pipeline fulfillment |
| `REPLACEMENT_REQUEST_REFUND_SUCCESS` | Xử lý như refund thay vì replace |
| `REPLACEMENT_REQUEST_REJECT` / `REPLACEMENT_REQUEST_CANCEL` | Đóng case |

---

## 11. Webhook (type 64): Aftersales Request Status Update

> **Stage 1 — First-review** của luồng return mới (kiến trúc 2-stage).
> Bắn khi buyer submit return request và khi seller approve/reject request đó.

### 11.1. Trigger scenarios
1. Buyer submit return request từ TikTok user app.
2. Seller approve hoặc reject request đó.

**Lifecycle:** Đây là **first-review stage**. Nếu request được approve → chuyển sang **second-review stage** (RMA — xem webhook type 65) để chờ seller review hàng đã nhận.

### 11.2. Payload spec

| Property | Type | Description | Example |
|----------|------|------------|--------|
| `shop_id` | String | TikTok Shop identifier | `7494049642642441621` |
| `timestamp` | Int | Unix timestamp khi webhook fire | `1732526400` |
| `tts_notification_id` | String | Notification ID — dùng dedup/tracing | `7327112393057371910` |
| `type` | Int | Event type — luôn = `64` | `64` |
| `data.aftersales_request_id` | String | ID của aftersales request | `576486316948490001` |
| `data.return_role` | String | Ai initiate: `BUYER` / `SELLER` | `BUYER` |
| `data.aftersales_request_status` | String | `PENDING_REQUEST_REVIEW` hoặc `REQUEST_REVIEW_COMPLETED` | `PENDING_REQUEST_REVIEW` |
| `data.aftersales_request_create_time` | Int | Unix seconds — lúc tạo request | `1627587600` |
| `data.aftersales_request_update_time` | Int | Unix seconds — lúc update status | `1644412885` |

### 11.3. Status enum
- `PENDING_REQUEST_REVIEW` — Chờ seller duyệt request.
- `REQUEST_REVIEW_COMPLETED` — Seller đã quyết định (approve hoặc reject). **Lưu ý:** webhook này KHÔNG nói rõ approve hay reject — phải dùng API Get Aftersales Request để lấy decision, hoặc đợi webhook RMA (type 65) bắn về (nếu RMA được tạo nghĩa là approved).

### 11.4. Event example
```json
{
  "type": 64,
  "tts_notification_id": "1111111111",
  "shop_id": "1234567890",
  "timestamp": 1644412885,
  "data": {
    "aftersales_request_id": "576486316948490001",
    "return_role": "BUYER",
    "aftersales_request_status": "PENDING_REQUEST_REVIEW",
    "aftersales_request_create_time": 1627587600,
    "aftersales_request_update_time": 1644412885
  }
}
```

---

## 12. Webhook (type 65): RMA Status Update

> **Stage 2 — Second-review** của luồng return mới.
> Track package-level lifecycle cho RMA object được sinh ra từ items đã approve ở stage 1.

### 12.1. Trigger scenarios
1. RMA được tạo (sau khi Aftersales Request được approve).
2. RMA package đã delivered, chờ seller review items để quyết định refund.
3. Items review và refund decision hoàn tất.

**Note:** RMA tự động được tạo ngay khi Aftersales Request được approve.

### 12.2. Payload spec

| Property | Type | Description | Example |
|----------|------|------------|--------|
| `shop_id` | String | TikTok Shop identifier | `7494049642642441621` |
| `timestamp` | Int | Unix timestamp khi webhook fire | `1732526400` |
| `tts_notification_id` | String | Notification ID | `7327112393057371910` |
| `type` | Int | Event type — luôn = `65` | `65` |
| `data.rma_id` | String | ID của RMA | `576486316948490002` |
| `data.aftersales_request_id` | String | Parent aftersales request ID | `576486316948490001` |
| `data.return_role` | String | `BUYER` / `SELLER` | `BUYER` |
| `data.rma_status` | String | Xem enum bên dưới | `RMA_CREATED` |
| `data.rma_create_time` (`rma_request_create_time` in spec) | Int | Unix seconds | `1627587600` |
| `data.rma_update_time` (`rma_request_update_time` in spec) | Int | Unix seconds | `1644412885` |

> ⚠️ Docs có sự không nhất quán giữa tên field trong table (`rma_request_create_time` / `rma_request_update_time`) và payload example (`rma_create_time` / `rma_update_time`). **Implement parser defensive cả 2 dạng.**

### 12.3. Status enum
| Status | Mô tả |
|--------|------|
| `RMA_CREATED` | RMA đã được tạo |
| `PENDING_PACKAGE_REVIEW` | Return package đã delivered, chờ seller review. **SLA = 2 business days** — quá hạn TikTok auto-decide |
| `PACKAGE_REVIEW_COMPLETED` | Package review hoàn tất (seller đã quyết accept hay reject items) |

### 12.4. Event example
```json
{
  "type": 65,
  "tts_notification_id": "1111111113",
  "shop_id": "1234567890",
  "timestamp": 1644412889,
  "data": {
    "rma_id": "576486316948490002",
    "aftersales_request_id": "576486316948490001",
    "return_role": "BUYER",
    "rma_status": "RMA_CREATED",
    "rma_create_time": 1627587601,
    "rma_update_time": 1644412889
  }
}
```

---

## 13. Webhook (type 67): Refund Success

> **Stage cuối — kết quả refund.**
> Bắn khi refund của 1 RMA hoàn tất thành công, kèm chi tiết từng line-item được refund + số tiền.

### 13.1. Trigger scenario
Khi refund của RMA hoàn tất thành công.

### 13.2. Payload spec

| Property | Type | Description | Example |
|----------|------|------------|--------|
| `shop_id` | String | TikTok Shop identifier | `7494049642642441621` |
| `timestamp` | Int | Unix timestamp khi webhook fire | `1732526400` |
| `tts_notification_id` | String | Notification ID | `7327112393057371910` |
| `type` | Int | Event type — luôn = `67` | `67` |
| `data.aftersales_request_id` | String | Parent aftersales request ID | `576486316948490001` |
| `data.rma_id` | String | RMA ID liên quan | `576486316948490002` |
| `data.refund_currency` | String | Currency code | `USD` |
| `data.refund_status` | String | Kết quả refund | `REFUND_SUCCESS` |
| `data.refund_timestamp` | Int | Unix seconds — lúc refund hoàn tất | `1776102211` |
| `data.refund_total` | String | Tổng số tiền đã refund | `"1.25"` |
| `data.line_items[]` | Array | Chi tiết line-item được refund | — |
| `data.line_items[].main_order_id` | String | ID đơn forward gốc | `576486316948490003` |
| `data.line_items[].return_line_item_id` | String | ID line-item của return | `4035312491762651201` |
| `data.line_items[].sku_id` | String | SKU ID | `1732309230575784932` |
| `data.line_items[].sku_return_request_id` | String | ID return request ở mức SKU | `4035312491762585666` |
| `data.line_items[].sub_return_line_item_id` | String | Child line-item ID (chỉ áp dụng cho **Virtual Bundle returns**) | `4035312491762651202` |

> ⚠️ `refund_total` là **String**, không phải Decimal/Float. Parser phải convert cẩn thận để tránh mất precision (đặc biệt với currency 0-decimal như VND, JPY).

### 13.3. Event example
```json
{
  "type": 67,
  "tts_notification_id": "7628321701399676688",
  "shop_id": "123456",
  "timestamp": 1776107052,
  "data": {
    "aftersales_request_id": "111111111",
    "line_items": [
      {
        "main_order_id": "789456123",
        "return_line_item_id": "999999",
        "sku_id": "147852",
        "sku_return_request_id": "321654987",
        "sub_return_line_item_id": "888888"
      },
      {
        "main_order_id": "789456123",
        "return_line_item_id": "999999",
        "sku_id": "147853",
        "sku_return_request_id": "321654987",
        "sub_return_line_item_id": "777777"
      },
      {
        "main_order_id": "789456123",
        "return_line_item_id": "666666",
        "sku_id": "147853",
        "sku_return_request_id": "321654987"
      }
    ],
    "refund_currency": "USD",
    "refund_status": "REFUND_SUCCESS",
    "refund_timestamp": 1776102211,
    "refund_total": "1.25",
    "rma_id": "222222222"
  }
}
```

---

## 14. So sánh: Webhook cũ (type 12) vs Webhook mới (64+65+67)

TikTok đang **migrate** từ kiến trúc 1-webhook sang **2-stage review** + **separate refund event**.

### 14.1. Bảng so sánh

| Aspect | Type 12 (Return Status Change) | Type 64 + 65 + 67 (Aftersales + RMA + Refund) |
|--------|--------------------------------|-----------------------------------------------|
| **Kiến trúc** | 1 webhook gánh toàn bộ vòng đời | 3 webhook tách riêng theo stage |
| **Stage tracking** | Tất cả status trong 1 enum | Tách thành 2-stage: Request review + Package review |
| **Refund detail** | Chỉ status, không có amount/line-items | Có `refund_total` + `line_items[]` ở type 67 |
| **Line-item level** | Order-level only | Có chi tiết SKU/sub-line-item (Virtual Bundle) |
| **Identifier** | `return_id` | `aftersales_request_id` + `rma_id` |
| **Status granularity** | 13 statuses pha trộn cả 2 stage | Stage 1: 2 statuses. Stage 2: 3 statuses |
| **SLA visibility** | Implicit | Explicit: stage 2 có **2 business days** SLA |
| **Replacement** | Bao gồm trong cùng webhook | **Chưa rõ** docs có cover replacement không (cần verify) |

### 14.2. Mapping giữa 2 hệ webhook (tham khảo)

| Type 12 status | Tương đương trong type 64/65/67 |
|----------------|----------------------------------|
| `RETURN_OR_REFUND_REQUEST_PENDING` | Type 64: `PENDING_REQUEST_REVIEW` |
| `REFUND_OR_RETURN_REQUEST_REJECT` | Type 64: `REQUEST_REVIEW_COMPLETED` (decision = reject — phải call API lấy) |
| `AWAITING_BUYER_SHIP` | Type 64: `REQUEST_REVIEW_COMPLETED` + Type 65: `RMA_CREATED` |
| `BUYER_SHIPPED_ITEM` | Type 65: (chờ delivered) |
| (Package delivered) | Type 65: `PENDING_PACKAGE_REVIEW` |
| `REJECT_RECEIVE_PACKAGE` | Type 65: `PACKAGE_REVIEW_COMPLETED` (decision = reject — call API lấy) |
| `RETURN_OR_REFUND_REQUEST_SUCCESS` | Type 65: `PACKAGE_REVIEW_COMPLETED` (decision = approve) |
| `RETURN_OR_REFUND_REQUEST_COMPLETE` | Type 67: `REFUND_SUCCESS` |
| `RETURN_OR_REFUND_REQUEST_CANCEL` | (Cần check — có thể qua Type 64 với status COMPLETED) |

> ⚠️ **Mapping này là phỏng đoán theo semantics**. Khi implement, **verify với TikTok docs mới nhất** và **test thực tế trong sandbox** trước khi rely.

### 14.3. Khuyến nghị chiến lược tích hợp

**Khi tích hợp mới từ đầu (EcommerceConnect cho TikTok):**
- ✅ Subscribe **cả 4 webhook**: `12`, `64`, `65`, `67`.
- ✅ Dùng `64+65+67` làm primary source — granular + có refund detail.
- ✅ Dùng `12` làm fallback / cross-check (phòng khi TikTok deprecate dần).
- ✅ Persist raw payload (`DataReceive`) trước khi parse — replay được khi schema thay đổi.

**Khi đã có code cũ dùng type 12:**
- Giữ type 12 hoạt động.
- Subscribe thêm `64+65+67` song song, log để so sánh consistency.
- Migrate dần khi đã tin tưởng dữ liệu mới đầy đủ.

---

## 15. Checklist tích hợp cho EcommerceConnect

Khi tích hợp luồng after-sales TikTok ↔ EcommerceConnect:

### Webhook subscription
- [ ] Subscribe `Cancellation Status Change`.
- [ ] Subscribe `Return Status Change` (type 12).
- [ ] Subscribe `Aftersales Request Status Update` (type 64).
- [ ] Subscribe `RMA Status Update` (type 65).
- [ ] Subscribe `Refund Success` (type 67).

### Routing & idempotency
- [ ] Webhook → publish vào `EcommerceConnect_DataReceivePartner` → consumer route theo `EcommerceCode = TikTok`.
- [ ] **Idempotency:** Dùng `tts_notification_id` làm dedup key, lưu Redis/DB để tránh xử lý duplicate.
- [ ] Persist full raw payload vào `DataReceive` (kèm `type`) trước khi parse → debug + replay được khi có lỗi schema.

### Business logic
- [ ] Phân biệt rõ initiator (`BUYER` / `SELLER` / `SYSTEM` / `OPERATOR`) → API call và logic xử lý khác nhau.
- [ ] Tôn trọng **48h SLA** seller response (request stage) — quá hạn TikTok auto-approve.
- [ ] Tôn trọng **2 business days SLA** seller package review (RMA stage).
- [ ] Replacement: phải tạo đơn mới fulfillment khi seller approve.
- [ ] Mapping status TikTok → CRM order status (cần align với domain model của SCRM).
- [ ] Mapping 2-stage architecture (type 64+65) ↔ legacy 1-stage (type 12) để consistent với data cũ.

### Market & data quirks
- [ ] Lưu ý market-specific policy:
  - UK dùng `ON_HOLD` cho remorse window.
  - US là market duy nhất cho seller initiate return.
- [ ] **Defensive parsing:**
  - Handle cả `RETURN_OR_REFUND_REQUEST_CANCEL` và `RETURN_OR_REFUND_REQUEST_CANCELLED` (type 12 — docs naming inconsistent).
  - Handle cả `rma_create_time` / `rma_request_create_time` (type 65 — docs naming inconsistent).
- [ ] **Time fields:** Tất cả timestamp là Unix seconds — convert sang DateTime cẩn thận timezone (lưu UTC trong DB).
- [ ] **Refund amount:** `refund_total` (type 67) là **String** — parse cẩn thận precision, xét currency 0-decimal (VND/JPY).
- [ ] **Virtual Bundle:** `sub_return_line_item_id` chỉ có khi item là Virtual Bundle — không assume luôn tồn tại.

---

## 16. Return & Refund APIs — Endpoint reference

### 16.1. Danh sách API trong nhóm "Return and refund"

| Method | API | Endpoint | Docs URL |
|--------|-----|----------|----------|
| GET | Get Reject Reasons | `/return_refund/202309/reject_reasons` | https://partner.tiktokshop.com/docv2/page/get-reject-reasons-202309 |
| POST | Approve Return | `/return_refund/202309/returns/{return_id}/approve` | https://partner.tiktokshop.com/docv2/page/approve-return-202309 |
| POST | Reject Return | `/return_refund/202309/returns/{return_id}/reject` | https://partner.tiktokshop.com/docv2/page/reject-return-202309 |
| **GET** | **Get Return Records** | `/return_refund/202309/returns/{return_id}/records` | https://partner.tiktokshop.com/docv2/page/get-return-records-202309 |
| POST | Approve Cancellation | `/return_refund/202309/cancellations/{cancel_id}/approve` | https://partner.tiktokshop.com/docv2/page/approve-cancellation-202309 |
| POST | Reject Cancellation | `/return_refund/202309/cancellations/{cancel_id}/reject` | https://partner.tiktokshop.com/docv2/page/reject-cancellation-202309 |
| GET | Get Decision Eligibility | `/return_refund/202309/.../decision_eligibility` | https://partner.tiktokshop.com/docv2/page/get-decision-eligibility-202309 |
| POST | Calculate Refund | `/return_refund/202309/refunds/calculate` | https://partner.tiktokshop.com/docv2/page/calculate-refund-202309 |
| POST | Cancel Order | `/return_refund/202309/cancellations` | https://partner.tiktokshop.com/docv2/page/cancel-order-202309 |
| POST | Create Return | `/return_refund/202309/returns` | https://partner.tiktokshop.com/docv2/page/create-return-202309 |
| POST | Search Cancellations | `/return_refund/202309/cancellations/search` | https://partner.tiktokshop.com/docv2/page/search-cancellations-202309 |
| **POST** | **Search Returns** | `/return_refund/**202602**/returns/search` | https://partner.tiktokshop.com/docv2/page/search-returns-202602 |
| GET | Get Aftersale Eligibility | `/return_refund/202309/orders/{order_id}/aftersale_eligibility` | https://partner.tiktokshop.com/docv2/page/get-aftersale-eligibility-202309 |

> **Base URL:** `https://open-api.tiktokglobalshop.com`
> **API version chủ đạo:** `202309`. **Search Returns version `202602`** (mới hơn).
> Các URL Docs còn lại (ngoài `get-reject-reasons`, `get-return-records`, `search-returns`) là **suy ra theo pattern** — verify khi mở.

### 16.1.1. URLs cho code comment (paste trên method gọi API)

> Dev integrate xong nhớ paste URL doc tương ứng làm comment trên method để dễ trace khi spec thay đổi.

**Webhooks:**
```csharp
// Webhook handlers — TikTok Shop Return/Refund/Cancel
// Overview: https://partner.tiktokshop.com/docv2/page/return-refund-and-cancel-api-overview
// type 12 — Return Status Change:           https://partner.tiktokshop.com/docv2/page/return-status-change
// type 64 — Aftersales Request Status:      https://partner.tiktokshop.com/docv2/page/aftersales-request-status-update
// type 65 — RMA Status Update:              https://partner.tiktokshop.com/docv2/page/rma-status-update
// type 67 — Refund Success:                 https://partner.tiktokshop.com/docv2/page/refund-success
```

**APIs (đã verify URL):**
```csharp
// GET Return Records — lifecycle history của 1 return
// Docs: https://partner.tiktokshop.com/docv2/page/get-return-records-202309
// Endpoint: GET /return_refund/202309/returns/{return_id}/records

// Search Returns — current detail + line items + amount, paginated
// Docs: https://partner.tiktokshop.com/docv2/page/search-returns-202602
// Endpoint: POST /return_refund/202602/returns/search

// Get Reject Reasons — list lý do reject
// Docs: https://partner.tiktokshop.com/docv2/page/get-reject-reasons-202309
// Endpoint: GET /return_refund/202309/reject_reasons
```

> URL các API khác (Approve/Reject/Create/Cancel/...) là suy theo pattern `https://partner.tiktokshop.com/docv2/page/{kebab-case-name}-{version}` — verify khi mở trong browser.

---

### 16.2. `GET Get Return Records` — Detail

**Mục đích:** Retrieve lifecycle event entries của 1 return request cụ thể. Mỗi item trong `data.records` đại diện cho 1 action / lifecycle event của return.

**Required scope:** `seller.return_refund.basic`

**Docs URL:** https://partner.tiktokshop.com/docv2/page/get-return-records-202309

**Endpoint:**
```
GET /return_refund/202309/returns/{return_id}/records
```

#### Path parameters

| Property | Type | Required | Description |
|----------|------|----------|------------|
| `return_id` | string | ✅ | Unique identifier của TikTok Shop return request (từ webhook hoặc Search Returns) |

#### Header parameters

| Property | Type | Required | Description |
|----------|------|----------|------------|
| `content-type` | string | ✅ | `application/json` |
| `x-tts-access-token` | string | ✅ | Seller access token (từ Get Access Token, `user_type = 0`) |

#### Query parameters (common cho mọi API TikTok Shop)

| Property | Type | Required | Description |
|----------|------|----------|------------|
| `app_key` | string | ✅ | App key được TikTok cấp |
| `sign` | string | ✅ | Signature generate theo thuật toán TikTok |
| `timestamp` | int | ✅ | Unix timestamp GMT (UTC+00:00) |
| `shop_cipher` | string | ✅ | Cipher đại diện shop. Lấy qua **Get Authorization Shop** API. Cross-border shop không truyền → trả về sai data |
| `locale` | string | ❌ | BCP-47 locale code, default `en-US`. Dùng để localize `reason_text` |

#### Example cURL

```bash
curl -X GET \
  'https://open-api.tiktokglobalshop.com/return_refund/202309/returns/4035318504086604100/records?app_key=38ab...' \
  -H 'x-tts-access-token: TTP_pwSm2AAAAABmmtFz1xlyKMnwg74T2GJ5s0uQbS8jPjb_GkdFVCxPqzQXSyuyFXdQa0AqyDsea2tYFNVf4' \
  -H 'content-type: application/json'
```

#### Response schema

| Field | Type | Description |
|-------|------|------------|
| `code` | int | Status code (0 = success) |
| `message` | string | "Success" hoặc lý do failure |
| `request_id` | string | Request log ID, dùng để trace khi báo lỗi với TikTok support |
| `data` | object | Specific return information |
| `data.records[]` | array | List các lifecycle event records |
| `data.records[].event` | string | Loại lifecycle event mà record này mô tả (tùy current status + return flow) |
| `data.records[].role` | string | Actor: `BUYER` / `SELLER` / `OPERATOR` (CS thủ công) / `SYSTEM` (auto theo policy) |
| `data.records[].description` | string | Mô tả record |
| `data.records[].reason_text` | string | Reason text đã localize theo `locale` query param |
| `data.records[].note` | string | Note từ seller hoặc buyer (dựa `role` để phân biệt nguồn) |
| `data.records[].images[]` | array | Ảnh đính kèm (seller hoặc buyer upload, phân biệt qua `role`) |
| `data.records[].images[].url` | string | URL ảnh |
| `data.records[].images[].width` / `.height` | int | Kích thước (px) |
| `data.records[].videos[]` | array | Video — **chỉ buyer được upload** |
| `data.records[].videos[].url` | string | URL video |
| `data.records[].videos[].cover` | string | URL thumbnail |
| `data.records[].videos[].width` / `.height` | int | Kích thước (px) |
| `data.records[].videos[].duration_millis` | int | Duration (milliseconds) |
| `data.records[].create_time` | int | Unix timestamp khi record được tạo |

#### Example response (rút gọn)

```json
{
  "code": 0,
  "data": {
    "records": [
      {
        "event": "...",
        "role": "BUYER",
        "description": "...",
        "reason_text": "...",
        "note": "...",
        "images": [
          { "url": "https://...", "width": 200, "height": 200 }
        ],
        "videos": [
          {
            "url": "https://v16m-default.akamaized.net/...",
            "cover": "https://p16-oec-va.ibyteimg.com/...",
            "width": 200,
            "height": 200,
            "duration_millis": 3000
          }
        ],
        "create_time": 1690532213
      }
    ]
  },
  "message": "Success",
  "request_id": "20220307074900010189081028E18C7087"
}
```

#### ⚠️ Điểm cần lưu ý khi dùng API này

1. **Không phải "get return detail" theo nghĩa truyền thống** — API trả về **history of lifecycle events**, không trả full info hiện tại (line items, refund amount, ...). Để lấy:
   - **Line items + amount:** dùng `Search Returns` (POST) với filter `return_id` hoặc các API liên quan.
   - **Order info gốc:** dùng `Get Order Detail`.
2. **`event` enum chưa rõ tất cả values** — docs chỉ mô tả chung "tùy current status và return flow". Test thực tế / call API sandbox để liệt kê đầy đủ.
3. **Locale ảnh hưởng `reason_text`** — nếu cần data ổn định cho mapping, fix `locale=en-US`.
4. **Phân biệt source của `note` / `images`:** dựa vào field `role` trong cùng record (BUYER hay SELLER upload).
5. **Video chỉ buyer upload** — seller record sẽ không có `videos`.
6. **Cross-border shop:** bắt buộc truyền `shop_cipher` đúng, không thì data trả về sai.

#### Luồng dùng webhook → API

```
Webhook type 12 (Return Status Change) ──► nhận return_id
                                            │
                                            ▼
                              GET /return_refund/202309/returns/{return_id}/records
                                            │
                                            ▼
                              records[] ──► persist lifecycle vào CRM
                                            ──► trigger action tương ứng status mới
```

---

### 16.3. `POST Search Returns` — Detail

**Mục đích:** Search/list returns theo nhiều filter (return ID, order ID, buyer, return type, return status, arbitration status, time window). Response **paginated** — page tiếp theo dùng `next_page_token` từ response trước, các filter và sort phải giữ nguyên.

**Required scope:** `seller.return_refund.basic`

**Docs URL:** https://partner.tiktokshop.com/docv2/page/search-returns-202602

**Endpoint:**
```
POST /return_refund/202602/returns/search
```

> ⚠️ Version `202602` — **mới hơn `202309`** của `Get Return Records`. Khi tích hợp lưu ý mỗi API có version khác nhau.

#### Header parameters

| Property | Type | Required | Description |
|----------|------|----------|------------|
| `content-type` | string | ✅ | `application/json` |
| `x-tts-access-token` | string | ✅ | Seller access token |

#### Query parameters

| Property | Type | Required | Description |
|----------|------|----------|------------|
| `app_key` | string | ✅ | App key |
| `sign` | string | ✅ | Signature |
| `timestamp` | int | ✅ | Unix timestamp GMT |
| `shop_cipher` | string | ✅ | Shop cipher (Get Authorization Shop) |
| `sort_field` | string | ❌ | `create_time` (default) hoặc `update_time` |
| `sort_order` | string | ❌ | `ASC` (default) hoặc `DESC` |
| `page_size` | string | ❌ | Default `10`. Valid range **[10–50]** |
| `page_token` | string | ❌ | Token cho page tiếp theo (lấy từ `next_page_token` response trước). **KHÔNG** truyền ở page đầu |

#### Body parameters (JSON, tất cả optional — combine để filter)

| Property | Type | Description |
|----------|------|------------|
| `return_ids` | []string | Filter theo list return IDs |
| `order_ids` | []string | Filter theo list order IDs |
| `buyer_user_ids` | []string | Filter theo list buyer user IDs |
| `return_types` | []string | `REFUND` / `RETURN_AND_REFUND` / `REPLACEMENT` |
| `return_status` | []string | Xem enum bên dưới |
| `seller_proposed_return_type` | []string | Hiện chỉ có `PARTIAL_REFUND` |
| `arbitration_status` | []string | `IN_PROGRESS` / `SUPPORT_BUYER` / `SUPPORT_SELLER` / `CLOSED` |
| `create_time_ge` | int | Unix timestamp — filter `create_time >=` |
| `create_time_lt` | int | Unix timestamp — filter `create_time <` |
| `update_time_ge` | int | Unix timestamp — filter `update_time >=` |
| `update_time_lt` | int | Unix timestamp — filter `update_time <` |
| `locale` | string | BCP-47, default `en` (KHÁC `Get Return Records` default `en-US`) |

#### `return_status` — Enum đầy đủ

| Status | Mô tả |
|--------|------|
| `RETURN_OR_REFUND_REQUEST_PENDING` | Buyer khởi tạo return/refund, chờ seller/system review |
| `REFUND_OR_RETURN_REQUEST_REJECT` | Return/refund bị reject |
| `AWAITING_BUYER_SHIP` | Approved, chờ buyer ship hàng. Quá deadline → platform tự đóng request |
| `BUYER_SHIPPED_ITEM` | Buyer đã ship items về seller |
| `REJECT_RECEIVE_PACKAGE` | Seller inspect package, từ chối return |
| `RETURN_OR_REFUND_REQUEST_SUCCESS` | Request thành công, buyer sẽ được refund |
| `RETURN_OR_REFUND_REQUEST_CANCEL` | Request đã bị buyer hoặc system hủy |
| `RETURN_OR_REFUND_REQUEST_COMPLETE` | Return/refund đã xử lý xong, buyer đã được hoàn |
| `REPLACEMENT_REQUEST_PENDING` | Buyer initiate replacement, chờ seller review |
| `REPLACEMENT_REQUEST_REJECT` | Seller reject replacement |
| `REPLACEMENT_REQUEST_REFUND_SUCCESS` | Hết kho → seller refund thay vì replace |
| `REPLACEMENT_REQUEST_CANCEL` | Buyer cancel replacement |
| `REPLACEMENT_REQUEST_COMPLETE` | Seller approve replacement → tạo đơn mới fulfill |
| **`AWAITING_BUYER_RESPONSE`** | **(MỚI)** Seller đề xuất loại return khác cho buyer (vd partial refund) — chờ buyer phản hồi. Check field `seller_proposed_return_type` |

> ⚠️ Lưu ý:
> - Status `AWAITING_BUYER_RESPONSE` **không có trong webhook type 12 docs cũ** — đây là status mới chỉ Search Returns API expose.
> - Status enum dùng `RETURN_OR_REFUND_REQUEST_CANCEL` (1 chữ `L`) — khác với chỗ webhook type 12 docs ghi `_CANCELLED` (2 chữ `L`). **Search Returns là canonical.**

#### Time filter — cách hoạt động

- `create_time_ge` + `create_time_lt` → filter pair cho create time window.
  - Chỉ truyền `_ge` → `_lt` default = current time.
  - Chỉ truyền `_lt` → `_ge` default = earliest shop time.
- `update_time_ge` + `update_time_lt` → tương tự cho update time.

#### Example cURL

```bash
curl -X POST \
  'https://open-api.tiktokglobalshop.com/return_refund/202602/returns/search?page_size=10&timestamp=1623812664&app_key=38abcd&shop_cipher=GCP_XF90igAAAABh00qsWgtvOiGFNqyubMt3&sort_field=create_time&sort_order=ASC&page_token=aDU2dHIzMlFhME5CUzJKUDhDdVJhTDM1WmJkeFVTVW9LTkRaSnNaZCtuWjJXVU5CSDhlaA==&sign=5361235029d141222525e303d742f9e38aea052d10896d3197ab9d6233730b8c' \
  -H 'x-tts-access-token: TTP_pwSm2AAAAABmmtFz1xlyKMnwg74T2GJ5s0uQbS8jPjb_GkdFVCxPqzQXSyuyfXdQa0AqyDsea2tYFNVf4XeqgZHFfPyv0Vs659QqyLYfsGzanZ5XZAin3_ZkcIxxS0_In6u6XDeU96k' \
  -H 'content-type: application/json' \
  -d '{
    "return_ids": ["4035318504086604100"],
    "order_ids": ["577686530908261117"],
    "buyer_user_ids": ["7493996903972308733"],
    "return_types": ["REFUND"],
    "return_status": ["RETURN_OR_REFUND_REQUEST_PENDING"],
    "seller_proposed_return_type": ["PARTIAL_REFUND"],
    "create_time_ge": 1690340825,
    "create_time_lt": 1690340825,
    "update_time_ge": 1690340825,
    "update_time_lt": 1690340825,
    "arbitration_status": ["IN_PROGRESS"],
    "locale": "en-US"
  }'
```

#### Use case điển hình

| Case | Body |
|------|------|
| **Get detail 1 return cụ thể** (theo webhook) | `{ "return_ids": ["{return_id}"] }` |
| **Tất cả return chưa xử lý của shop** | `{ "return_status": ["RETURN_OR_REFUND_REQUEST_PENDING"] }` |
| **Tất cả return của 1 buyer** | `{ "buyer_user_ids": ["{buyer_id}"] }` |
| **Polling sync gần đây** | `{ "update_time_ge": {last_sync_unix} }` + sort `update_time DESC` |
| **Return đang bị arbitration** | `{ "arbitration_status": ["IN_PROGRESS"] }` |

#### Response schema

##### Cấp top-level
| Field | Type | Description |
|-------|------|------------|
| `code` | int | Status code (0 = success) |
| `message` | string | "Success" hoặc lý do failure |
| `request_id` | string | Request log ID |
| `data` | object | Container kết quả |
| `data.return_orders[]` | array | **Danh sách return objects** |
| `data.total_count` | int | Tổng số returns match filter |
| `data.next_page_token` | string | Pagination token cho page tiếp theo. Rỗng → đã hết data |

##### `return_orders[]` — Mỗi return object
| Field | Type | Description |
|-------|------|------------|
| `order_id` | string | TikTok Shop order ID (chứa nhiều order line items) |
| `return_id` | string | ID của return cụ thể |
| `return_type` | string | `REFUND` / `RETURN_AND_REFUND` / `REPLACEMENT` |
| `return_status` | string | Status (xem enum trong request body) |
| `arbitration_status` | string | `IN_PROGRESS` / `SUPPORT_BUYER` / `SUPPORT_SELLER` / `CLOSED` |
| `role` | string | Initiator: `BUYER` / `SELLER` / `OPERATOR` / `SYSTEM` |
| `return_reason` | string | Reason code |
| `return_reason_text` | string | Reason localized theo `locale` |
| `shipment_type` | string | `PLATFORM` (buyer dùng TikTok shipping — check `handover_method`) / `BUYER_ARRANGE` (buyer tự sắp xếp) |
| `handover_method` | string | `DROP_OFF` (buyer drop tại courier) / `PICKUP` (courier đến pickup) |
| `return_tracking_number` | string | Tracking number của return parcel |
| `return_provider_name` | string | Tên đơn vị vận chuyển |
| `return_provider_id` | string | ID đơn vị vận chuyển |
| `pre_return_id` | string | Return ID gốc nếu đơn này là edit của return cũ |
| `next_return_id` | string | Return ID kế tiếp nếu đơn này đã bị edit |
| `can_buyer_keep_item` | bool | Buyer có được giữ hàng không (return-less refund) |
| `update_time` | int | Unix timestamp last update |
| `create_time` | int | Unix timestamp tạo |
| `seller_next_action_response[]` | array | Action sắp tới seller phải làm + deadline |
| `return_line_items[]` | array | Danh sách items của return (xem schema bên dưới) |
| `discount_amount[]` | array | Chi tiết discount refund (xem schema bên dưới) |
| `shipping_fee_amount[]` | array | Chi tiết shipping fee (xem schema bên dưới) |
| `refund_amount` | object | **Tổng refund cho cả return** (xem schema bên dưới) |
| `return_shipping_document_type` | string | `SHIPPING_LABEL` / `QR_CODE` (Box) / `QR_CODE_NBNL` (No Box No Label) |
| `return_method` | string | `SELLER_SHIPPED` / `BUYER_SHIPPED` / `PLATFORM_SHIPPED` |
| `is_combined_return` | bool | True = nhiều return gộp vào 1 package |
| `combined_return_id` | string | ID combined return (nếu `is_combined_return=true`) |
| `seller_proposed_return_type` | string | `PARTIAL_REFUND` (nếu seller đề xuất partial refund) |
| `partial_refund` | object | Detail partial refund (xem schema bên dưới) |
| `buyer_rejected_partial_refund` | bool | Chỉ trả khi seller initiate partial refund. `TRUE` = buyer reject, `FALSE` = buyer accept hoặc chưa response |
| `return_warehouse_address` | object | Địa chỉ kho nhận return |
| `return_warehouse_address.full_address` | string | Địa chỉ đầy đủ |
| `is_quick_refund` | bool | True = hit quick refund strategy → seller KHÔNG được reject received package |

##### `seller_next_action_response[]`
| Field | Type | Description |
|-------|------|------------|
| `action` | string | `SELLER_RESPOND_REFUND` / `SELLER_RESPOND_RETURN` / `SELLER_RESPOND_RECEIVE_PACKAGE` / `SELLER_RESPOND_REPLACEMENT` |
| `deadline` | int | Unix timestamp deadline seller phải act |

> 💡 Field này rất quan trọng — dùng để **trigger reminder/notification** cho seller trước khi quá hạn SLA.

##### `return_line_items[]`
| Field | Type | Description |
|-------|------|------------|
| `return_line_item_id` | string | Return line item ID |
| `order_line_item_id` | string | Order line item ID gốc |
| `sku_id` | string | SKU ID |
| `sku_name` | string | SKU name |
| `product_name` | string | Product name |
| `seller_sku` | string | SKU name do seller đặt |
| `product_image` | object | `{url, width, height}` |
| `refund_amount` | object | Refund detail cho line item này (cùng schema với `refund_amount` top-level) |
| `return_sub_line_items[]` | array | **Sub items — chỉ có cho Virtual Bundle** (xem schema bên dưới) |

##### `return_sub_line_items[]` (Virtual Bundle only)
| Field | Type | Description |
|-------|------|------------|
| `return_sub_line_item_id` | string | Sub line item ID |
| `return_line_item_id` | string | Parent return line item ID |
| `sub_order_line_item_id` | string | Sub order line item ID |
| `order_line_item_id` | string | Order line item ID |
| `sku_id` / `sku_name` / `product_name` / `seller_sku` | string | Same as line item |
| `product_image` | object | `{url, width, height}` |
| `refund_amount` | object | Refund detail (cùng schema) |

##### `refund_amount` (object)
| Field | Type | Description |
|-------|------|------------|
| `currency` | string | Currency code (USD, VND, ...) |
| `refund_total` | string | Tổng refund |
| `refund_subtotal` | string | Subtotal (giá items return) |
| `refund_shipping_fee` | string | Shipping fee refund |
| `refund_tax` | string | Tax refund |
| `retail_delivery_fee` | string | Retail delivery fee (chỉ US Colorado, GMV > 500K USD) |
| `buyer_service_fee` | string | Buyer service fee refund |

##### `discount_amount[]`
| Field | Type | Description |
|-------|------|------------|
| `currency` | string | Discount currency |
| `product_seller_discount` | string | Seller-funded product discount refund |
| `shipping_fee_platform_discount` | string | Platform-funded shipping discount refund |
| `shipping_fee_seller_discount` | string | Seller-funded shipping discount refund |
| `product_platform_discount` | string | Platform-funded product discount refund |

##### `shipping_fee_amount[]`
| Field | Type | Description |
|-------|------|------------|
| `currency` | string | Currency |
| `seller_paid_return_shipping_fee` | string | Phần seller trả cho return shipping |
| `platform_paid_return_shipping_fee` | string | Phần platform trả |
| `buyer_paid_return_shipping_fee` | string | Phần buyer trả |

##### `partial_refund` (object)
| Field | Type | Description |
|-------|------|------------|
| `currency` | string | Currency partial refund |
| `amount` | string | Số tiền partial refund seller đề xuất. **Chỉ trả khi seller-proposed partial refund** |

#### ⚠️ Điểm quan trọng khi parse Response

1. **Tất cả số tiền là `string`** (`refund_total`, `amount`, ...) — parse cẩn thận precision, đặc biệt với VND/JPY (0-decimal).
2. **`return_sub_line_items` chỉ có với Virtual Bundle** — không assume luôn tồn tại.
3. **`refund_amount` xuất hiện 3 lần** với cùng schema:
   - `return_orders[].refund_amount` — tổng cho cả return
   - `return_orders[].return_line_items[].refund_amount` — cho từng line item
   - `return_orders[].return_line_items[].return_sub_line_items[].refund_amount` — cho từng sub item
4. **`seller_next_action_response[].deadline`** là source-of-truth cho SLA — dùng để alert seller, không tự tính lại.
5. **`is_quick_refund=true`** → seller bị khóa option reject received package. Logic phía CRM phải disable button "Reject package" tương ứng.
6. **`is_combined_return=true`** → 1 package chứa nhiều return. Cần xử lý nhóm theo `combined_return_id`.
7. **`pre_return_id` / `next_return_id`** → return có thể bị edit, tạo chain → cần follow chain để tìm latest.
8. **Pagination:** Luôn pass `next_page_token` từ response trước (KHÔNG đổi filter/sort khi sang page). Rỗng = hết data.
9. **`return_method` example value `SELLER_ARRANGE`** — KHÔNG có trong spec liệt kê (`SELLER_SHIPPED` / `BUYER_SHIPPED` / `PLATFORM_SHIPPED`). **Parser phải defensive** — không enum-strict cứng cho field này.
10. **`partial_refund.currency` có thể khác `refund_amount.currency`** — example show `IDR` vs `USD`. Đừng assume cùng currency.

#### Example response (đầy đủ)

```json
{
  "code": 0,
  "data": {
    "return_orders": [
      {
        "order_id": "577686530908261117",
        "return_id": "4035318504086604100",
        "return_type": "REFUND",
        "return_status": "RETURN_OR_REFUND_REQUEST_PENDING",
        "arbitration_status": "IN_PROGRESS",
        "role": "BUYER",
        "return_reason": "ecom_order_to_ship_canceled_reason_created_by_mistakes",
        "return_reason_text": "Order created by mistake",
        "shipment_type": "PLATFORM",
        "handover_method": "DROP_OFF",
        "return_tracking_number": "213456789098765433456",
        "return_provider_name": "J&T Express",
        "return_provider_id": "TH27014E9R5Q4G",
        "pre_return_id": "4035310341095393452",
        "next_return_id": "4035310341095393463",
        "can_buyer_keep_item": true,
        "update_time": 1690453136,
        "seller_next_action_response": [
          {
            "action": "SELLER_RESPOND_REFUND",
            "deadline": 1690554680
          }
        ],
        "create_time": 1690451136,
        "return_line_items": [
          {
            "return_line_item_id": "4035227657962164811",
            "order_line_item_id": "576473917261451851",
            "sku_id": "2729382476852921560",
            "sku_name": "1#, Standard",
            "product_name": "(SP) [PROMO BUNDLING] NICE Tissue Facial 180s x 5 pcs",
            "seller_sku": "PUTIH 1 TALI",
            "product_image": {
              "url": "https://p16-oec-va.ibyteimg.com/.../image.jpeg",
              "width": 200,
              "height": 200
            },
            "refund_amount": {
              "currency": "USD",
              "refund_total": "1.23",
              "refund_subtotal": "1",
              "refund_shipping_fee": "0.2",
              "refund_tax": "0.03",
              "retail_delivery_fee": "0.1",
              "buyer_service_fee": "0.1"
            },
            "return_sub_line_items": [
              {
                "return_sub_line_item_id": "4035227657962164812",
                "return_line_item_id": "4035227657962164811",
                "sub_order_line_item_id": "576473917261451852",
                "order_line_item_id": "576473917261451851",
                "sku_id": "2729382476852921560",
                "sku_name": "1#, Standard",
                "product_image": { "url": "https://...", "width": 200, "height": 200 },
                "product_name": "(SP) [PROMO BUNDLING] NICE Tissue Facial 180s x 5 pcs",
                "seller_sku": "PUTIH 1 TALI",
                "refund_amount": {
                  "currency": "USD",
                  "refund_total": "1.23",
                  "refund_subtotal": "1",
                  "refund_shipping_fee": "0.2",
                  "refund_tax": "0.03",
                  "retail_delivery_fee": "0.1",
                  "buyer_service_fee": "0.1"
                }
              }
            ]
          }
        ],
        "discount_amount": [
          {
            "currency": "USD",
            "product_seller_discount": "0.1",
            "shipping_fee_platform_discount": "0.1",
            "shipping_fee_seller_discount": "0.1",
            "product_platform_discount": "0.1"
          }
        ],
        "shipping_fee_amount": [
          {
            "currency": "USD",
            "seller_paid_return_shipping_fee": "0.1",
            "platform_paid_return_shipping_fee": "0.1",
            "buyer_paid_return_shipping_fee": "0.1"
          }
        ],
        "refund_amount": {
          "currency": "USD",
          "refund_total": "1.23",
          "refund_subtotal": "1",
          "refund_shipping_fee": "0.2",
          "refund_tax": "0.03",
          "retail_delivery_fee": "0.1",
          "buyer_service_fee": "0.1"
        },
        "return_shipping_document_type": "SHIPPING_LABEL",
        "return_method": "SELLER_ARRANGE",
        "is_combined_return": true,
        "combined_return_id": "4035309948547073951",
        "seller_proposed_return_type": "PARTIAL_REFUND",
        "partial_refund": {
          "currency": "IDR",
          "amount": "10"
        },
        "buyer_rejected_partial_refund": true,
        "return_warehouse_address": {
          "full_address": "1199 Coleman Ave San Jose, CA 95110"
        },
        "is_quick_refund": false
      }
    ],
    "total_count": 100,
    "next_page_token": "aDU2dHIzMlFhME5CUzJKUDhDdVJhTDM1WmJkeFVTVW9LTkRaSnNaZCtuWjJXVU5CSDhlaA=="
  },
  "message": "Success",
  "request_id": "202203070749000101890810281E8C70B7"
}
```

---

## Source
- TikTok Shop Partner Center — Documentation → API Reference → "Return, refund, and cancel API overview"
- TikTok Shop Partner Center — Webhooks → "Return Status Change" (type 12)
- TikTok Shop Partner Center — Webhooks → "Aftersales Request Status Update" (type 64)
- TikTok Shop Partner Center — Webhooks → "RMA Status Update" (type 65)
- TikTok Shop Partner Center — Webhooks → "Refund Success" (type 67)
- TikTok Shop Partner Center — API → "Get Return Records" (`/return_refund/202309/returns/{return_id}/records`)
- TikTok Shop Partner Center — API → "Search Returns" (`/return_refund/202602/returns/search`)
- Snapshot ngày: **2026-05-12**
