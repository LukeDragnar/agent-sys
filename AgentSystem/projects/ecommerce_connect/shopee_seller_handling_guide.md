# Shopee Seller — Hướng dẫn xử lý Trả hàng / Hoàn tiền / Hủy

> Góc nhìn seller: thao tác cụ thể + deadlines + action types từ docs hướng dẫn Shopee (Trung tâm trợ giúp Shopee Uni).
>
> Khác với [SHOPEE_RETURN_STANDARD_FLOW.md](SHOPEE_RETURN_STANDARD_FLOW.md) (spec API/state machine) và [STATUS_ACTION_GUIDE.md](STATUS_ACTION_GUIDE.md) (mapping enum → ý nghĩa), file này tổng hợp **WORKFLOW thực tế seller** cần làm trên Seller Center.

---

## 1. Bố cục trang quản lý Kênh Người Bán

Path: **Kênh Người Bán > Quản lý đơn hàng > Trả hàng/Hoàn tiền/Hủy**

Trang gồm 5 vùng:
1. **Thông báo** — bảng update từ Shopee
2. **Tab quản lý đơn** — 3 tab:
   - Đơn Trả hàng/Hoàn tiền
   - Đơn Hủy
   - Đơn Giao hàng không thành công
3. **Bộ lọc nhanh** — theo từng giai đoạn + **Hành động quan trọng**
4. **Bộ lọc tùy chọn** — tìm theo trạng thái / mã / ngày / phương án / vận chuyển
5. **Danh sách yêu cầu** — kèm cột Thao tác (action cụ thể + deadline)

---

## 2. Action Types — "Hành động quan trọng" (5 loại)

Đây là **categorization quan trọng** của Shopee cho seller — ECOMM nên forward + SCRM nên track.

| Action Type | Tab áp dụng | Nghĩa | Trigger field/enum |
|-------------|-------------|-------|---------------------|
| **Cung cấp bằng chứng** | Đơn Trả hàng/Hoàn tiền | Shopee yêu cầu seller upload evidence | `seller_proof_status = PENDING` |
| **Thương lượng với Người mua** | Đơn Trả hàng/Hoàn tiền + Đơn Hủy | Buyer raise request, chờ seller phản hồi | `negotiation_status = PENDING_RESPOND` hoặc `return_status = REQUESTED` |
| **Kiểm tra hàng hoàn** | Đơn Trả hàng/Hoàn tiền + Đơn Giao hàng không thành công | Buyer đã gửi hàng / hàng đang về | `reverse_logistic_status ∈ {LOGISTICS_PICKUP_DONE, LOGISTICS_DELIVERY_DONE}` |
| **Giữ lại kiện hàng** | Đơn Hủy | Buyer hủy trước khi bàn giao ĐVVC (SPX Express) — KHÔNG bàn giao | Không có public field — derived từ context |
| **Phản hồi quyết định hoàn tiền của Shopee** | Đơn Trả hàng/Hoàn tiền | Shopee đã quyết hoàn tiền không cần trả hàng | `return_solution = REFUND` + `return_status = ACCEPTED` |

---

## 3. Tab Đơn Trả hàng/Hoàn tiền — Quy trình

### 3.1. Khi Shopee yêu cầu bằng chứng (`seller_proof_status = PENDING`)

**Thao tác seller** (Kênh Người Bán):
1. Vào tab Tất cả → mục "Cần cung cấp bằng chứng"
2. Chọn yêu cầu → bấm "Đăng tải Bằng chứng"
3. Thêm ảnh/video + bấm Hoàn thành

⚠️ Quá hạn → đơn không hiển thị nữa, không thể upload, kết quả xử lý có thể bất lợi.

### 3.2. Khi buyer đã gửi hàng — Kiểm tra hàng hoàn

**Khi nào trigger:**
- `reverse_logistic_status = LOGISTICS_PICKUP_DONE` (đang về)
- `reverse_logistic_status = LOGISTICS_DELIVERY_DONE` (đã về kho)

**Deadlines khiếu nại:**

| Loại đơn | Mốc bắt đầu khiếu nại | Hạn khiếu nại |
|----------|------------------------|----------------|
| Đơn Trả hàng/Hoàn tiền **thường** | Từ khi đơn cập nhật trả hàng thành công HOẶC sau ngày nhận hoàn dự kiến | **2 ngày** |
| Đơn Trả hàng/Hoàn tiền **sau đồng kiểm** (từ 24/07/2025) | Sau **2 ngày** kể từ đơn cập nhật chuyển hoàn | **2 ngày** |
| **Tự sắp xếp** (buyer tự gửi không qua ĐVVC Shopee) | Từ khi buyer xác nhận đã gửi hàng | **2 ngày** |

**Bằng chứng cần khi khiếu nại:**
- Video/hình quá trình **đóng gói** (mã đơn, đóng đúng đủ, sản phẩm nguyên vẹn)
- Video quá trình **mở hàng hoàn** (mã đơn, 6 mặt kiện, tình trạng + số lượng sản phẩm bên trong, chi tiết lý do)
- Biên bản đồng kiểm với bưu tá (nếu kiện không nguyên đai)
- "**Video là bằng chứng mạnh nhất**"

Shopee xét: **3-5 ngày làm việc**.

### 3.3. Khi Shopee quyết hoàn tiền không cần trả hàng (Refund Only)

**Field state:**
- `return_solution = REFUND`
- `return_status = ACCEPTED`

**Quyền seller:** Khiếu nại trong **2 ngày** từ khi Shopee gửi thông báo Hoàn tiền Ngay.

**Special case:** Buyer khiếu nại "Chưa nhận hàng":
- Shopee chủ động kiểm tra với ĐVVC
- Nếu đơn thực tế không giao được:
  - Buyer được hoàn tiền
  - Hàng vận chuyển hoàn về seller, HOẶC
  - ĐVVC đền bù cho seller (nếu mất hàng)

→ **SCRM logic**: Khi `return_solution = REFUND` + `return_status = ACCEPTED`:
- KHÔNG cần HBBTL nhập kho (hàng không về)
- Hiển thị badge "Hoàn tiền không trả hàng — kiểm tra trong 2 ngày để khiếu nại"

---

## 4. Tab Đơn Hủy — 2 sub-flow

### 4.1. Hủy TRƯỚC khi bàn giao ĐVVC (SPX Express, từ 21/10/2024)

**Điều kiện:**
- ✅ Đơn trong nước, vận chuyển SPX Express
- ✅ Đơn chưa bàn giao ĐVVC
- ❌ KHÔNG áp dụng: Hỏa Tốc, Voucher-Dịch vụ, ĐVVC khác SPX

**Quy trình:**
- Shopee **tự động duyệt** yêu cầu hủy
- Action type cho seller: **"Giữ lại kiện hàng"** (KHÔNG bàn giao)
- Notification: "Vui lòng không bàn giao kiện hàng này"
- Khi quá Hạn chót bàn giao → notification biến mất, đơn auto-cancel

**Impact:**
- ✅ KHÔNG tính NFR (Non-Fulfillment Rate)
- ✅ KHÔNG tốn phí seller

→ **SCRM logic**: Đơn này KHÔNG cần tạo HBBTL — hàng vẫn ở seller, chỉ cần đánh dấu cancel + revert tồn kho nếu đã trừ.

### 4.2. Hủy SAU khi đã giao cho ĐVVC (đang vận chuyển)

**Điều kiện (từ 20/06/2024):**
- ✅ Đơn trong nước SPX Express
- ✅ Đơn chưa tới trạm giao hàng
- ❌ KHÔNG áp dụng: Thực phẩm tươi sống/đông lạnh, Voucher & Dịch vụ

**5 lý do buyer hủy:**
- Thay đổi sản phẩm (size, màu, số lượng)
- Cập nhật địa chỉ/SĐT
- Không có nhu cầu mua
- Seller xác nhận gửi sai
- Thời gian giao hàng dài

**Kết quả phụ thuộc ĐVVC:**
| ĐVVC | Kết quả |
|------|---------|
| ĐVVC chấp nhận | Hoàn tiền buyer + hàng hoàn về seller. **Seller chịu phí vận chuyển trả hàng** |
| ĐVVC từ chối | Tiếp tục giao đến buyer |

**Lưu ý:** Buyer chỉ được yêu cầu Hủy đơn **01 lần** trong suốt quá trình vận chuyển.

**Bồi thường:** Nếu yêu cầu hủy được chấp nhận nhưng kiện vẫn giao đến buyer → seller được bồi thường.

→ **SCRM logic**: Đơn này hiển thị ở tab **Đơn Trả hàng/Hoàn tiền** (không phải tab Đơn Hủy). Khi hàng về kho, áp dụng flow HBBTL bình thường nhưng cần tính thêm phí vận chuyển trả hàng vào chi phí.

---

## 5. Tab Đơn Giao hàng không thành công

### 5.1. Định nghĩa (2 sub-cases)

| Sub-case | Trigger |
|----------|---------|
| **Đơn giao không thành công** | Buyer từ chối nhận / địa chỉ sai / nhiều lần liên hệ giao fail |
| **Đơn hủy trên đường giao** | Đã cover ở §4.2 — buyer hủy khi đang vận chuyển |

⚠️ **Sub-case "Đơn giao không thành công" tương đương Shopee Case C** (`return_refund_request_type = 2` Return-on-the-Spot).

### 5.2. E-receipt mechanism (SPX Express)

- Khi shipper hoàn hàng → seller **ký xác nhận trên App** sau kiểm hàng
- Biên bản hoàn hàng điện tử (e-receipt) gửi về email seller
- Yêu cầu: seller phải cập nhật đúng email trên Kênh Người Bán

### 5.3. Quy trình xử lý (2 trường hợp)

#### 5.3.1. Đã nhận hàng hoàn

| Tình trạng | Hành động seller | Deadline |
|------------|-------------------|----------|
| Nguyên vẹn | Bấm "Nhận hàng trong tình trạng nguyên vẹn" | – |
| Hư hỏng/sai/thiếu | Khiếu nại + bằng chứng | **7 ngày** từ khi nhận hàng |
| **Hỏa Tốc — Ưu Tiên + thay đổi tính chất do nhiệt** (trứng/rau/thịt/đông lạnh) | Khiếu nại + bằng chứng có timestamp | **24 giờ** từ khi nhận hàng |

Hệ thống cập nhật trong **24h** từ khi seller nhận hàng. Quá thời gian này không update → liên hệ CSKH.

#### 5.3.2. Chưa nhận hàng hoàn

| Tình huống | Hành động | Deadline |
|------------|-----------|----------|
| Hệ thống cập nhật đã hoàn thành công nhưng seller chưa nhận | Khiếu nại | **7 ngày** từ khi hệ thống cập nhật |
| **Hoàn fail** (không có người nhận / địa chỉ sai / nhiều lần fail) | Liên hệ ĐVVC lên lịch nhận lại | **30 ngày** (Hỏa Tốc: **21 ngày**). Sau hạn → kiện bị tiêu hủy |
| **Hàng thất lạc khi hoàn** | Theo dõi bồi thường từ Shopee → vào Số dư TK Shopee | – |

Hàng sẽ về trong **4 ngày làm việc** từ khi ĐVVC ghi nhận bắt đầu hoàn. Quá → liên hệ CSKH.

---

## 6. Tổng kết Deadlines (cần SCRM cảnh báo trước hết hạn)

| Action | Deadline | Trigger từ |
|--------|----------|-----------|
| Upload bằng chứng dispute | Theo `seller_evidence_deadline` field | `seller_proof_status = PENDING` |
| Khiếu nại Refund Only | **2 ngày** | Shopee fire quyết định hoàn tiền |
| Khiếu nại hàng hoàn (thường) | **2 ngày** | Đơn cập nhật trả hàng thành công |
| Khiếu nại hàng hoàn (đồng kiểm) | **2 ngày** sau 2 ngày từ chuyển hoàn | Đơn cập nhật chuyển hoàn (sau đồng kiểm) |
| Khiếu nại Đơn giao không thành công (hư hỏng) | **7 ngày** | Seller nhận hàng |
| Khiếu nại Đơn giao không thành công (chưa nhận) | **7 ngày** | Hệ thống cập nhật đã hoàn thành công |
| Khiếu nại đơn Hỏa Tốc đông lạnh | **24 giờ** | Seller nhận hàng |
| Liên hệ ĐVVC lấy lại kiện hoàn fail | **30 ngày** (Hỏa Tốc 21 ngày) | ĐVVC báo hoàn fail |
| Shopee xét khiếu nại | **3-5 ngày làm việc** | Sau seller bấm khiếu nại |

→ **SCRM design**: thêm field `DeadlineKhieuNai` trên `DonHangHoan` + job cảnh báo "sắp hết hạn khiếu nại" 24h trước deadline.

---

## 7. Mẹo Shopee dành cho seller

1. Dùng bộ lọc nhanh **"Hành động quan trọng"** để lọc các đơn cần xử lý ưu tiên
2. Chọn tab **"Tất cả"** + filter cụ thể "Hành động" → tránh sót yêu cầu
3. Đơn có deadline gần nhất hiển thị trên top → xử lý từ trên xuống
4. Khi nhận hàng hoàn:
   - Mở hàng ở khu vực có camera
   - Quay video mở hàng (luôn luôn)
   - Lập biên bản đồng kiểm với bưu tá
   - Tạo quy trình kiểm tra hàng hoàn hằng ngày
5. Khuyến khích buyer chọn hình thức "ĐVVC đến lấy" hoặc "Trả tại bưu cục" để dễ track

---

## 8. 🎯 SCRM Operations Center — Equivalent của Seller Center

**Scope NEW (xác nhận user 2026-05-29)**: SCRM phải cho seller làm gần như tất cả thao tác Seller Center từ ERP — để seller dùng ERP "sướng" không cần switch sang Shopee.

### 🔑 CRITICAL CONSTRAINT — Generic UI cho TẤT CẢ sàn + non-sàn

UI SCRM **dùng CHUNG cho tất cả nguồn**: Shopee, TikTok, Lazada, **và đơn giao vận KHÔNG phải sàn** (vd: tự ship, GHN/GHTK trực tiếp).

→ **KHÔNG cá nhân hóa cho 1 sàn nào riêng.**

**Hệ quả kiến trúc:**

| Layer | Trách nhiệm |
|-------|-------------|
| **SCRM UI** | Hiển thị generic action buttons + form. Không có wording "Shopee/TikTok/Lazada" trên màn hình. |
| **SCRM Backend** | Nhận generic action request từ FE → forward sang ECOMM với `EcommerceId` (Shopee/TikTok/Lazada) hoặc null (non-sàn) |
| **ECOMM Adapter** | Resolve action → call API riêng từng sàn (`shopeeReturnsService.Dispute()` / `tiktokReturnsService.Dispute()` / ...). Non-sàn: skip Shopee API, chỉ update SCRM state. |
| **Partner-specific code** | Nằm hoàn toàn ở ECOMM module riêng (Shopee/, Tiktok/, Lazada/) — KHÔNG leak ra SCRM |

**Naming generic** (KHÔNG dùng từ Shopee-specific):

| Concept | KHÔNG dùng | Dùng |
|---------|-----------|------|
| Khiếu nại | "Dispute" | "Khiếu nại" / "Raise Complaint" |
| Yêu cầu hoàn | "Return Refund Request" | "Đơn hoàn" / "Return Order" |
| Bằng chứng | "Seller Proof" | "Bằng chứng" / "Evidence" |
| Thương lượng | "Negotiation/Offer" | "Đề xuất" / "Counter Proposal" |
| Logistics hoàn ngược | "Reverse Logistics" | "Vận chuyển hoàn về" |
| return_sn | "Return SN" | "Mã yêu cầu hoàn" |

**Đối với non-sàn (tự ship)**: UI vẫn dùng được — chỉ thiếu các action call ra partner (UploadProof/Dispute/Offer/etc.) sẽ disabled/hidden. Các action local (Confirm hàng nguyên vẹn, ghi nhận khiếu nại nội bộ) vẫn fire bình thường.

### 8.1. Functional Parity Map (Generic — adapter per partner)

| SCRM Generic Action | SCRM UI behavior | ECOMM Adapter — Shopee API | TikTok / Lazada adapter | Non-sàn |
|---------------------|-------------------|----------------------------|--------------------------|---------|
| Xem list "Trả hàng/Hoàn tiền/Hủy" 3 tab | Tab + filter Hành động | `get_return_list` | Partner equivalent | DB query local |
| Cung cấp bằng chứng | Upload ảnh/video form | `upload_proof` + `convert_image` + `query_proof` | Partner equivalent | Local only |
| Thương lượng / Đề xuất | Form propose solution + refund | `offer` | Partner equivalent | Hide button |
| Chấp nhận yêu cầu hoàn | Button "Chấp nhận" | `confirm` hoặc `accept_offer` | Partner equivalent | Local only |
| Từ chối | Button "Từ chối" + lý do | `confirm` with reject | Partner equivalent | Local only |
| Khiếu nại | Form chọn lý do + upload evidence | `dispute` + `get_return_dispute_reason` | Partner equivalent | Local log only |
| Hủy khiếu nại | Button "Hủy khiếu nại" | `cancel_dispute` | Partner equivalent | Local revert |
| Confirm nhận hàng nguyên vẹn | Button | `confirm` (status=ACCEPTED) | Partner equivalent | Local only |
| Theo dõi vận chuyển hoàn về | Timeline component | `get_reverse_tracking_info` | Partner equivalent | DB cache hoặc external API |
| Giữ lại kiện hàng | Notification banner | – (display only) | – | – |
| Phản hồi quyết định hoàn tiền | Form khiếu nại Refund Only | `dispute` (constraint) | Partner equivalent | – |

→ **Shopee adapter: 13/13 API ready** (sau Sprint vừa rồi). TikTok adapter cần verify state riêng. Non-sàn dùng subset local-only.

### 8.2. Phase rollout mở rộng (cho parity với Seller Center)

**Phase 5 — SCRM Operations Center Backend**:
- SCRM endpoints proxy 6 action: UploadProof, Confirm, Offer, AcceptOffer, Dispute, CancelDispute (mỗi endpoint nhận input + call ECOMM)
- ECOMM endpoints proxy → Shopee API (đã có service methods, chỉ thêm Controller mapping)
- Auth/authorization: SCRM user → check quyền theo DonVi/Shop

**Phase 6 — SCRM Operations Center Frontend**:
- Trang `DonHangHoan` redesign: 3 tab Shopee-style (Trả hàng/Hoàn tiền, Đơn Hủy, Đơn Giao hàng không thành công)
- Sidebar "Hành động quan trọng" 5 options (filter quick)
- Form modal cho mỗi action (Upload Evidence, Dispute, Offer, etc.)
- Reverse tracking timeline component (tương tự shipping tracking)
- Badge deadline countdown ("Còn 2 ngày để khiếu nại")

**Phase 7 — Notifications & Reminders**:
- Job cron daily: check `DeadlineKhieuNai` sắp hết hạn → push notification + email
- Webhook integration: Shopee push 29 → SCRM realtime UI update (signalr/websocket)

**Phase 8 — Audit & Reporting**:
- Log mọi action seller bấm từ SCRM → audit trail (ai bấm Dispute đơn nào lúc nào, kết quả Shopee response)
- Dashboard "Tỉ lệ khiếu nại thành công", "Số đơn pending action", "Refund Only count"

### 8.3. Risk + Constraint

- **Authorization**: 1 seller có thể nhiều shop Shopee — SCRM cần ràng buộc action chỉ apply cho shop user có quyền (`UnitConnectId` mapping)
- **Idempotency**: Seller có thể bấm 2 lần (network slow) → ECOMM forward cần idempotency check trước khi call Shopee
- **Sync state**: Sau khi seller bấm action ở SCRM → Shopee xử lý → push 29 update DB SCRM. UI phải reload state thật trước khi cho action tiếp
- **API rate limit Shopee**: 1000 req/h/shop — cảnh báo nếu seller spam action
- **Constraint Shopee** đã biết:
  - `dispute` chỉ accept status ∈ {REQUESTED, PROCESSING}
  - `cancel_dispute` chỉ apply cho compensation dispute

### 8.4. UI Reference từ Seller Center

Khi build SCRM UI, theo design Shopee:
- 5 vùng layout (Thông báo / Tab / Bộ lọc nhanh / Bộ lọc tùy chọn / Danh sách)
- Cột table: Sản phẩm | Số tiền | Lý do | Phương án cho Người mua | Trạng thái | Vận chuyển đến Người Mua | Vận chuyển hàng hoàn | Thao tác
- Sắp xếp đơn theo deadline ưu tiên tăng dần (sắp hết hạn lên đầu)

---

## 9. Impact vào SCRM design (insights cũ, vẫn giữ)

### 8.1. Cần track Action Type (5 loại) trên DonHangHoan

Hiện DonHangHoan chỉ có status raw → cần derive thêm **HanhDongQuanTrong** từ tổ hợp `return_status + seller_proof_status + reverse_logistic_status + return_solution + negotiation_status`.

Đề xuất helper:
```csharp
public enum HanhDongQuanTrong
{
    None,
    CungCapBangChung,           // seller_proof_status = PENDING
    ThuongLuongVoiNguoiMua,     // negotiation = PENDING_RESPOND
    KiemTraHangHoan,            // reverse_logistic = PICKUP_DONE/DELIVERY_DONE
    GiuLaiKienHang,             // hủy trước bàn giao
    PhanHoiQuyetDinhHoanTien    // return_solution = REFUND + status = ACCEPTED
}
```

### 8.2. Cần track Deadline

Add field `DeadlineKhieuNai` (DATETIME?) + job notification.

### 8.3. Sub-case "Giữ lại kiện hàng"

Đơn hủy trước bàn giao SPX Express → KHÔNG tạo HBBTL — hàng vẫn ở seller. Chỉ:
- Revert tồn kho nếu đã trừ
- Đánh dấu `TrangThai = CANCELLED`

### 8.4. Phí vận chuyển trả hàng (hủy đang giao)

Khi sub-case "Hủy đang giao" về kho → tính thêm chi phí logistics vào HBBTL hoặc tách phiếu chi riêng.

### 8.5. Special "Refund Only — buyer chưa nhận hàng"

Field combo:
- `return_solution = REFUND` (hoàn tiền không trả hàng)
- + buyer reason "chưa nhận hàng"
- → Shopee có thể auto-route: hoàn về seller (Case C) HOẶC ĐVVC đền bù

SCRM cần phân biệt 2 sub-path:
- Path 1: Hàng về kho → HBBTL bình thường
- Path 2: ĐVVC đền bù → ghi nhận thu nhập khác, không HBBTL

---

## Reference

- Hướng dẫn gốc Shopee Uni: https://help.shopee.vn/portal/4/article/137988 (tổng quan)
- State machine + API: [SHOPEE_RETURN_STANDARD_FLOW.md](SHOPEE_RETURN_STANDARD_FLOW.md)
- Status → enum mapping: [STATUS_ACTION_GUIDE.md](STATUS_ACTION_GUIDE.md)
- HBBTL trigger design: [HBBTL_TRIGGER_FLOW.md](HBBTL_TRIGGER_FLOW.md)
