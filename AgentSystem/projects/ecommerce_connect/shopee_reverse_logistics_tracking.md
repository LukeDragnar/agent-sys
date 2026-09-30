# Shopee Return — Reverse Logistics & Tracking (spec + data thật)

> Nguồn: Shopee Open Platform — Return Refund Management + bản update API mới
> (`get_reverse_tracking_info`, thêm `return_refund_request_type` / `validation_type`).
> Kèm 2 bộ data thật (sandbox sandbox.com.vn) để đối chiếu.
> Liên quan: [shopee_return_refund_flow.md](shopee_return_refund_flow.md) (state machine 13 transitions).

---

## 1. Hai discriminator cốt lõi (Shopee chính thức)

Phân loại đơn hoàn KHÔNG dựa "field webhook nào có value" mà dựa 2 field từ `get_return_detail` / `get_reverse_tracking_info`:

### `return_refund_request_type` (int) — quyết FORMAT + độ đầy đủ
| Value | Loại | Khi nào raise | Reverse logistics |
|-------|------|---------------|-------------------|
| 0 | Normal RR | Buyer raise SAU khi nhận hàng / quá hạn giao | status = **CODE** `LOGISTICS_*` + **FULL** tracking (tracking_number, ePOP/ePOD, est. date) |
| 1 | In-transit RR | Buyer raise KHI hàng đang in-transit tới buyer | status = **DISPLAY NAME** + update_time, **KHÔNG** tracking |
| 2 | Return-on-the-Spot | Driver raise khi buyer từ chối nhận tại cửa | status = **DISPLAY NAME** + update_time, **KHÔNG** tracking |

### `validation_type` (string) — quyết SEGMENT
| Value | Số segment | Field lấy status |
|-------|-----------|------------------|
| `seller_validation` | 1 (buyer → seller) | `reverse_logistics_status` (+ tracking_number, tracking_info) |
| `warehouse_validation` | 2 (buyer → kho → seller) | seg1: `reverse_logistics_status`; seg2: `post_return_logistics_status` (+ rts_tracking_number, post_return_logistics_tracking_info) |

> **Local Return (VN)**: nếu có segment 2, API CHỈ trả segment 2 (post_return). Cross-border trả cả 2.

### `return_refund_type` (RRBOC / RRAOC) — CHỈ là timing, KHÔNG dùng map logistics
- RRBOC = Return/Refund raised **Before** Order Complete
- RRAOC = raised **After** Order Complete
- ⚠️ Đây là trục thời điểm độc lập — KHÔNG suy ra được format/field logistics (một Normal RR vẫn có thể là RRBOC).

---

## 2. Data Definition (enum)

### ReturnStatus (state machine — xem shopee_return_refund_flow.md §1)
`REQUESTED · ACCEPTED · CANCELLED · JUDGING · CLOSED · PROCESSING · SELLER_DISPUTE`

### ReturnSolution
- `RETURN_REFUND` (int 0) — trả hàng + hoàn tiền
- `REFUND` (int 1) — chỉ hoàn tiền, buyer KHÔNG trả hàng
- ⚠️ get_return_detail trả **int** (0/1); webhook updated_values trả **string**. Map: **0=RETURN_REFUND, 1=REFUND**.
- ⚠️ Solution CÓ THỂ ĐỔI theo thời gian (data thật: RETURN_REFUND→REFUND) → CRM phải update mỗi webhook.

### Reverse Logistics Status
**[Normal Return] — CODE canonical:**
```
LOGISTICS_PENDING_ARRANGE   chờ buyer chọn ĐVVC
LOGISTICS_READY             đã chọn, chờ tạo request (chưa có tracking)
LOGISTICS_REQUEST_CREATED   đã tạo request — TRACKING NUMBER có từ đây
LOGISTICS_PICKUP_RETRY      3PL thử lấy lại
LOGISTICS_PICKUP_FAILED     3PL lấy thất bại
LOGISTICS_PICKUP_DONE       đã pickup (integrated) / buyer nhập shipping proof (non-integrated)
LOGISTICS_DELIVERY_FAILED   giao về seller thất bại
LOGISTICS_LOST              mất hàng
LOGISTICS_DELIVERY_DONE     ĐÃ GIAO VỀ SELLER (đủ điều kiện HBBTL)
```

**[In-transit RR] & [Return-on-the-Spot] — DISPLAY NAME (theo spec official):**
```
Preparing · Delivered · Delivery Failed · Lost
```
⚠️ **Data thật còn bắn "Pickup done"** (đơn 1) — NGOÀI spec official. Tức Shopee thực tế gửi nhiều hơn 4 value spec liệt kê. Cần map defensive.

**Post Return Logistics Status (segment 2, chỉ warehouse_validation):**
```
POST_RETURN_LOGISTICS_REQUEST_CREATED / REQUEST_CANCELED / PICKUP_FAILED /
PICKUP_RETRY / PICKUP_DONE / DELIVERY_FAILED / DELIVERY_DONE / LOST
```

### SellerProofStatus
`NOT_NEEDED · PENDING · UPLOADED · OVERDUE`

### return_refund_request_type / validation_type — xem §1.

---

## 3. API mới: `v2.returns.get_reverse_tracking_info`

Request: `return_sn`.

Response chính:
| Field | Ghi chú |
|-------|---------|
| `return_refund_request_type`, `validation_type` | 2 discriminator |
| `reverse_logistics_status` | latest reverse status (buyer→validation point) |
| `reverse_logistics_update_time` | mọi loại RR |
| `tracking_number` | **chỉ Normal RR + integrated** |
| `tracking_info[]` | **chỉ Normal RR + integrated**: `update_time`, `tracking_description`, `epop_image_list` (ảnh lấy hàng), `epod_image_list` (ảnh giao hàng) |
| `estimated_delivery_date_min/max` | chỉ Normal RR + integrated |
| `post_return_logistics_status` | chỉ: request_type=0 + solution=0 + warehouse_validation + integrated |
| `post_return_logistics_update_time`, `rts_tracking_number`, `post_return_logistics_tracking_info[]` | segment 2 (RTS = Return To Seller) |

### Note quan trọng (Additional Notes)
1. **Theo request_type**: Normal RR → latest status + FULL tracking. In-transit/RoS → CHỈ latest status + update_time (no tracking). Value reverse_logistics_status In-transit/RoS KHÁC Normal.
2. **Theo validation_type**: seller_validation = 1 segment (reverse_*). warehouse_validation + integrated = 2 segment (seg1 reverse_*, seg2 post_return_*). Local chỉ trả seg2.
3. 🔴 **Webhook update**: `return_updates_push` GIỜ bắn `logistics_status` cho In-transit/RoS (value display name, khác Normal). → **field name không còn phân biệt được type** → BẮT BUỘC dùng request_type.

---

## 4. API Call Flow (outbound — seller actions)

```
get_return_list → get_return_detail → get_available_solutions → [action]
```
| Action API | Generic CRM | Constraint | Kết quả |
|---|---|---|---|
| `confirm` | Confirm | **chỉ Full Refund** (refund only, không trả hàng) | → ACCEPTED |
| `offer` | Offer | sau get_available_solutions | seller đưa plan |
| `accept_offer` | AcceptOffer | khi buyer đề xuất | seller chấp nhận |
| `dispute` | Dispute | **chỉ REQUESTED / PROCESSING** | → SELLER_DISPUTE |
| `convert_image`→`upload_proof`→`query_proof` | UploadProof | sau dispute | upload ảnh (KHÔNG video) |

---

## 5. Hai đơn data thật (đối chiếu)

| | Đơn 1 — `26050900MS4BU49` | Đơn 2 — `2605090W1P2SM56` |
|---|---|---|
| return_refund_type | RRBOC | RRAOC |
| request_type | 1 (In-transit) | 0 (Normal) |
| return_solution (detail int) | 1 (=REFUND) | 0 (=RETURN_REFUND) |
| return_solution (webhook) | RETURN_REFUND→**REFUND** | (không bắn) |
| needs_logistics | false | true |
| validation_type | seller_validation | seller_validation |
| tracking_number | "" | `SPXVN063161424395` |
| integrated | false | true |
| refund_amount | 0 | 11.232.000 |
| Đường state machine | REQUESTED ─④→ ACCEPTED (thẳng) | JUDGING ─⑤→ PROCESSING ─⑥→ ACCEPTED |
| Webhook field VC hoàn | `reverse_logistics_status` | `logistics_status` |
| Value VC hoàn | "Pickup done"→"Delivered" (display) | LOGISTICS_PENDING_ARRANGE→…→PICKUP_DONE (code) |

### Insight nghiệp vụ từ data
1. **Refund-only nhảy thẳng ACCEPTED, không qua logistics**; return-refund qua PROCESSING + chuỗi logistics đầy đủ.
2. **`needs_logistics=false` ≠ "không có hàng về"**: đơn 1 (In-transit) buyer không ship nhưng hàng vẫn redirect ngược về seller ("Delivered"). `needs_logistics` = "buyer có phải ship không", KHÔNG phải "có hàng về kho không".
3. **In-transit refund-only ≈ hủy đơn** (hàng chưa giao, quay về, refund 0) — khác bản chất Normal return-refund (hàng đã bán, trả lại, refund tiền thật). CRM cần phân biệt nghiệp vụ kế toán.
4. **tracking_number chỉ có ở Normal + integrated** (qua LOGISTICS_REQUEST_CREATED).
5. **"Hàng về seller" khác theo validation_type**: seller_validation → reverse Delivered = về seller; warehouse_validation → reverse Delivered = mới về KHO Shopee, phải đợi post_return DELIVERY_DONE mới về seller.

---

## 6. Gap với code hiện tại (ECOMM + SCRM) — CHƯA fix

| # | Gap | Gốc |
|---|-----|-----|
| A | `LogisticsType` gán theo "field webhook có value" → Normal RR (logistics_status) bị gán **FORWARD** thay vì REVERSE → `IsHangVeKho` miss → KHÔNG trigger HBBTL cho đơn return-refund phổ biến | Phân loại sai discriminator |
| B | `Shopee_MapReturnLogisticsStatus` chỉ hiểu code `LOGISTICS_*`, không hiểu display name ("Pickup done"/"Delivered") → In-transit/RoS map ra null | Thiếu map display name |
| C | Detect refund-only chỉ dựa `GiaiPhapHoan`; `return_solution` int từ detail không map (0/1) | Bỏ qua int + needs_logistics |
| D | `IsHangVeKho` không xét validation_type → warehouse_validation có thể tưởng nhầm hàng về seller khi mới về kho Shopee | Thiếu phân biệt segment |
| E | ~~ECOMM chưa gọi `get_reverse_tracking_info`~~ → **mã vận đơn hoàn ĐÃ fix (2026-06)**: cả manual sync `SyncOneReturnToCrm` lẫn webhook job `ProcessSendOrderReturnToCrm` gọi `GetReverseTrackingInfo` lấy `tracking_number`(REVERSE)/`rts_tracking_number`(POST_RETURN) → `OrderReturnCode3Pl` → SCRM map `DonHangHoan.LogisticsMaVanDon` (CREATE+UPDATE). **CÒN thiếu**: ePOP/ePOD images + `tracking_info[]` entries chưa persist | tracking_number DONE; ePOP/ePOD chưa |

### Thiết kế map ĐÚNG (theo spec — chưa code)
```
1. Lấy request_type + validation_type (get_return_detail / get_reverse_tracking_info).
2. Đọc trạng thái VC hoàn:
   - request_type 0  → logistics_status, value = CODE
   - request_type 1,2 → logistics_status HOẶC reverse_logistics_status (defensive), value = DISPLAY NAME → normalize về code
3. LogisticsLoai: seller_validation→REVERSE (1 seg); warehouse_validation→REVERSE(seg1)+POST_RETURN(seg2). KHÔNG có FORWARD.
4. Hàng-về-seller (gate HBBTL):
   - seller_validation: reverse = Delivered / LOGISTICS_DELIVERY_DONE
   - warehouse_validation: post_return = POST_RETURN_LOGISTICS_DELIVERY_DONE
5. Mã vận đơn + ePOP/ePOD: get_reverse_tracking_info.
```

---

## 7. LoaiHoan canonical (trục kế toán) + gate HBBTL — ĐÃ implement

`return_refund_type` (RRBOC/RRAOC) là **trục KẾ TOÁN**, KHÁC `request_type` (giao vận). Đã thành canonical CRM riêng (xem memory `reference_don_hoan_loaihoan_vs_thoidiem`).

- **Mapping** `EnumShopee.MapLoaiHoanToCrm`: `"RRBOC"→0 TruocHoanThanhDon`, `"RRAOC"→1 SauHoanThanhDon`, else `9 KhongXacDinh` (uppercase+trim). Raw giữ ở `DonHangHoan.LoaiHoanDoiTac`; canonical ở `DonHangHoan.LoaiHoan`(int). Enum mirror 2 đầu: `EnumDonHangHoan_LoaiHoan` (SCRM) ↔ `EnumCrmLoaiHoan` (ECOMM), int phải khớp.
- **Gate HBBTL** (`TryAutoCreateHBBTLAsync`): `LoaiHoan==TruocHoanThanhDon` (RRBOC) → **return (chặn cứng)**, vì chưa ghi nhận doanh thu → không đảo. RRAOC/null → qua gate kế toán rồi check gate vật lý (`NoiKiemHoan`: NguoiBan→REVERSE+LOGISTICS_DELIVERY_DONE; KhoTrungGian→POST_RETURN+POST_RETURN_LOGISTICS_DELIVERY_DONE). ⚠️ Hiện `AUTO_CREATE_HBBTL=false` (stub) — gate logic sẵn nhưng auto-create để phase sau (cần plumb UserLogin qua contract ECOMM→CRM).
- **return_solution**: detail trả INT (0=RETURN_REFUND, 1=REFUND), webhook trả string → chuẩn hoá về string khi sync từ detail.

## 8. Đồng bộ thủ công 2 nút + điểm refresh detail — ĐÃ implement

- **`SyncOneReturnToCrm(unitConnect, returnSn, allowCreate, processName)`** (ECOMM, dùng chung): get_return_detail → (lookup OrderReturnData, null→create skeleton nếu allowCreate) → lookup OrderData (CrmOrderId, bắt buộc cho CREATE) → sync detail → get_reverse_tracking_info → BuildReturnDataFull(needFullPayload=isCreate) → SendOrderReturnDataToCrmAsync(CREATE/UPDATE_DETAIL) → persist.
  - **Nút 1 "Lấy đơn hoàn mới"** `GetReturnFromEcomm`: get_return_list (chunk 15 ngày) → sn chưa có trong OrderReturnData → `SyncOneReturnToCrm(allowCreate:true)`. Tạo **thẳng vào CRM**, KHÔNG qua temp/job.
  - **Nút 2 "Đồng bộ trạng thái"** `RefreshReturnStatus`: loop `SyncOneReturnToCrm(allowCreate:false)` (đơn lạ bỏ qua).
  - ⚠️ **Defer Insert**: CREATE chỉ `InsertAsync` SAU khi push CRM thành công — tránh row mồ côi `Status=null`. Xem memory `feedback_defer_persist_until_external_success`.
- **Webhook job `ProcessSendOrderReturnToCrm`**: GIỮ NGUYÊN (temp pipeline + timestamp + retry). Force gọi lại `get_return_detail` (qua `GetOrLoadPartnerReturnDetail` forceRefresh) khi: đổi `return_solution`, HOẶC chuyển sang **status quan trọng** `EnumShopee.IsImportantReturnStatusForDetailRefresh` = {PROCESSING, ACCEPTED, REFUND_PAID, SELLER_DISPUTE, CLOSED, CANCELLED}. REQUESTED/JUDGING + update logistics/proof → dùng cache (tiết kiệm API). Đây là cơ chế giữ refund/lý do/solution realtime theo status, KHÔNG dùng polling job.
  - **(2026-06) Reverse tracking trong webhook job**: webhook `return_updates_push` KHÔNG kèm mã vận đơn hoàn (chỉ `logistics_status`). Khi `hasLogisticsUpdate` đổi sang mốc có tracking (`EnumShopee.IsReverseTrackingAvailable` = REQUEST_CREATED trở đi, REVERSE+POST_RETURN) mà `OrderReturnCode3Pl` còn trống + `NeedsLogistics != false` → gọi `GetReverseTrackingInfo` bù. Gate trong `hasLogisticsUpdate` (1 lần/lần đổi status, tránh gọi lặp với non-integrated không bao giờ có tracking). Race tracking gán trễ → backfill ở logistics transition kế hoặc nút "Đồng bộ trạng thái". Trước đây chỉ manual sync gọi `GetReverseTrackingInfo` → webhook-driven đơn (đa số) bị NULL `LogisticsMaVanDon`.
