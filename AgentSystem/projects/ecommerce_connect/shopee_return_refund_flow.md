# Shopee Return Refund Management — Luồng tổng thể

> Tổng hợp từ Shopee Open Platform: https://open.shopee.com/developer-guide/227
> Data Definition: https://open.shopee.com/developer-guide/31
> Push Mechanism overview: https://open.shopee.com/developer-guide/18
>
> Để dev xem chi tiết spec/code, vào file docs trong ECOMM repo:
> - `EcommerceConnect.Service/Generated/Shopee/Docs/SHOPEE_RETURN_STANDARD_FLOW.md`
> - `EcommerceConnect.Service/Generated/Shopee/Docs/IMPLEMENTATION_COVERAGE.md`
> - `EcommerceConnect.Service/Generated/Shopee/Docs/RETURNS_FLOW.md`

---

## 1. Return Status Flow — state machine chính thức

**7 states:** REQUESTED, PROCESSING, JUDGING, SELLER_DISPUTE, CLOSED, CANCELLED, ACCEPTED.

⚠️ **REFUND_PAID KHÔNG có trong diagram chính thức + V2.0 Data Definition.** Code current giữ làm legacy alias — cần verify khi nhận webhook thực.

### 13 transitions

| # | From | To | Trigger |
|---|------|-----|---------|
| 1 | REQUESTED | CANCELLED | buyer cancels request |
| 2 | REQUESTED | PROCESSING | buyer+seller agree return / auto-accept latest offer / "return and refund" no response by due date |
| 3 | REQUESTED | JUDGING | buyer or seller raises dispute |
| 4 | REQUESTED | ACCEPTED | buyer+seller agree refund / auto refunds / auto-accept latest offer / "refund only" no response by due date |
| 5 | JUDGING | PROCESSING | agent agrees to proceed return |
| 6 | PROCESSING | ACCEPTED | seller clicks refund / auto-accelerate after buyer ships / auto-refund (no response after buyer shipped) |
| 7 | PROCESSING | CANCELLED | buyer cancels before ship / buyer didn't ship by due date |
| 8 | PROCESSING | SELLER_DISPUTE | seller raises dispute after buyer ship |
| 9 | JUDGING | ACCEPTED | agent agrees to proceed refund |
| 10 | JUDGING | CANCELLED | agent cancels (buyer can request again) |
| 11 | JUDGING | CLOSED | agent closes (buyer cannot request anymore) |
| 12 | SELLER_DISPUTE | CLOSED | agent closes refund |
| 13 | SELLER_DISPUTE | ACCEPTED | agent agrees to refund |

---

## 2. Return API Call Flow

### 2.1 Read APIs (lấy info)
- `v2.returns.get_return_list` — list returns + filter (status, negotiation, evidence, compensation)
- `v2.returns.get_return_detail` — detail by `return_sn`
- `v2.returns.get_available_solutions` — solutions khả dụng

### 2.2 Refund Only flow
`get_return_list → get_return_detail → get_available_solutions → confirm` → status `ACCEPTED`, buyer KHÔNG cần gửi hàng.

### 2.3 Return & Refund (No Dispute)
```
get_return_list → get_return_detail → get_available_solutions → offer
  ├── Buyer Accept → get_return_detail → End
  └── Buyer counter-propose
        ├── Seller Accept → accept_offer → get_return_detail → End
        └── Seller reject → offer (loop)
```

### 2.4 Return & Refund (Dispute)
2 nhánh từ `get_available_solutions`:
- **Buyer dispute path**: `offer` → buyer raise dispute → `get_return_detail` → End
- **Seller dispute path**: doesn't accept → `dispute` → `convert_image` → `upload_proof` → `query_proof` → `get_return_detail` → End

⚠️ **Constraint**: `dispute` API chỉ accept khi status ∈ {REQUESTED, PROCESSING}.

### API list

| API | Code current state |
|-----|---------------------|
| `get_return_list` | ✅ Có |
| `get_return_detail` | ✅ Có |
| `get_available_solutions` | ✅ Có |
| `get_reverse_tracking_info` | ❌ Chưa có |
| `confirm` | ✅ Có |
| `offer` | ✅ Có |
| `accept_offer` | ✅ Có |
| `dispute` | ✅ Có (đã dùng `image_list` bản mới) |
| `convert_image` | ✅ Có (multipart TODO) |
| `upload_proof` | ✅ Có |
| **`query_proof`** | ❌ **Chưa có** (mới phát hiện trong dispute flow) |
| `get_return_dispute_reason` | ❌ Chưa có |
| `cancel_dispute` | ❌ Chưa có (release 2024-12-31) |

---

## 3. Phân loại case business — `return_refund_request_type`

Field này KHÔNG có trong push webhook 29 → phải call `get_return_detail` để biết.

| Value | Case business | Người khởi xướng | Thời điểm | Tab Seller Channel VN |
|-------|---------------|------------------|-----------|------------------------|
| `0` | Normal RR (Case A) | Buyer | Sau delivery done / past est. delivery date | "Đơn Trả hàng Hoàn tiền" |
| `1` | In-transit RR (Case A') | Buyer | Khi item đang in-transit | "Đơn Trả hàng Hoàn tiền" |
| `2` | **Return-on-the-Spot (Case C)** | **Driver** | **Buyer reject parcel TẠI CỬA khi giao** | **"Đơn Giao hàng không thành công"** |

Plus `validation_type`:
- `seller_validation` — 1 segment reverse logistics (buyer → seller)
- `warehouse_validation` — 2 segment (buyer → warehouse → seller), dùng `post_return_logistics_*` cho segment 2

---

## 4. Data Definition refs

Tất cả enum/status đã add vào `EnumShopee.cs` (developer-guide/31):
- `ReturnStatus` (state machine 13 transitions trong XML doc)
- `ReturnRefundRequestType` (0/1/2)
- `ValidationType` (seller/warehouse)
- `ReverseLogisticsStatus` (NormalReturn 9 / InTransitRR 4 / ReturnOnTheSpot 4)
- `PostReturnLogisticsStatus` (8 values)
- `NegotiationStatus` (3 values)
- `SellerCompensationStatus` (9 values)
- `SellerProofStatus` (4 values V2.0 + legacy mapping)
- `ReturnDisputeReasonId` (Dictionary 36 entries)
- `ReturnReason` (33 values với aliases)

---

## 5. Webhook — push code 29

**`return_updates_push`**: notify khi 4 field thay đổi:
- `return_status`
- `return_solution`
- `seller_proof_status`
- `logistics_status` (⚠️ legacy field, chỉ cover Normal RR — Case A. In-transit RR / Return-on-the-Spot dùng `reverse_logistic_status` mới nhưng push 29 KHÔNG fire field này)

**Spec**: timeout 3s, sequence NOT guaranteed, can repeated, retry 5min/30min/3h.

**Implication Case C**: Push 29 KHÔNG chứa `return_refund_request_type` → phải call `get_return_detail` để biết Case 2. Push 29 cũng KHÔNG chứa `reverse_logistic_status` → call `get_reverse_tracking_info` để get latest status.

---

## 6. Workflow rule cho file docs Shopee

Khi note docs/spec partner (Shopee/TikTok/Lazada) vào code:
- API endpoint URL → XML `<summary>` của class/method consuming
- Push code / webhook → XML `<summary>` const trong `EnumShopee.PushMechanismCode`
- Enum / Data Definition → XML `<summary>` enum class
- Property/field cụ thể → XML doc của property
- Overview / developer guide (concept-level) → file `.md` trong `Shopee/Docs/`

Chi tiết: xem [patterns.md](patterns.md) section "Docs Notation".
