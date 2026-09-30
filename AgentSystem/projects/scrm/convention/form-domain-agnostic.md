---
name: accounting-form-domain-agnostic
description: Form accounting (HBBTL, Phiếu chi, ...) phải domain-agnostic. KHÔNG nhồi logic source-specific (đơn hoàn ecomm, retur receipt, manual...) vào form. Caller compute, form patch generic.
metadata:
  type: feedback
---

# Form accounting phải domain-agnostic — caller compute, form patch generic

**Rule:** Form accounting (HBBTL, Phiếu thu/chi, Phiếu nhập/xuất...) chỉ nhận DTO prefill có shape generic (boolean/string/number). KHÔNG được biết về domain nguồn (đơn hoàn ecomm code REFUND_ONLY, retur receipt status, manual flag...).

**Why:** 1 form HBBTL dùng cho nhiều source: đơn hoàn ecomm (Shopee/TikTok/Lazada), tạo manual, từ đơn bán gốc, từ retur receipt, v.v. Mỗi source có business logic riêng. Nếu form xử lý logic của 1 source → phải copy-paste khi thêm source khác, hoặc form leak ecomm code (REFUND_ONLY) vào accounting domain. Confirmed bởi user 2026-06-02 sau session refactor HBBTL prefill từ đơn hoàn.

**How to apply:**

| Logic | Caller (BE source) | Form (FE accounting) |
|---|---|---|
| Compute `isKiemNhapKho` từ `giaiPhapHoan` ecomm | ✓ Compute boolean ở `DonHangHoanService.HBBTL_PrefillFromDonHangHoan` | ✗ Form KHÔNG đọc `giaiPhapHoan` |
| Decide `tongTien` per line khi prefill thiếu thuế/CK | ✓ BE compute default = thanhTien | ✗ Form KHÔNG biết "đây là REFUND_ONLY nên skip thuế" |
| Map status sang display label | ✓ BE đẩy text-ready field | ✗ Form KHÔNG switch case theo enum source |

**DTO design:**
- BE compute → DTO chứa **flag generic**: `bool? IsKiemNhapKho`, `bool? IsAutoCalculate`, `string? Title`, ...
- Form patch blindly: `if (data.isKiemNhapKho != null) form.isKiemNhapKho = data.isKiemNhapKho`
- KHÔNG: `if (data.giaiPhapHoan === 'REFUND_ONLY') form.isKiemNhapKho = false` ← leak ecomm code vào accounting

**Counter-example đã tránh (session 2026-06-02):**
- Initial: BE prefill DTO trả `GiaiPhapHoan: string?` từ entity đơn hoàn → FE form check `giaiPhap === 'REFUND_ONLY'` set `isKiemNhapKho = false`.
- User revert: "không được cho logic đơn hoàn vào phiếu đâu" + "nó còn nhiều trường hợp đâu phải mỗi đơn hoàn đâu".
- Fix: replace `GiaiPhapHoan` bằng `IsKiemNhapKho: bool?` trong DTO. BE compute trong service. FE patch boolean.

**Process check trước khi code:**
1. Field này thuộc domain nào (ecomm / accounting / warehouse / shared)?
2. Form đang sửa thuộc domain nào?
3. Nếu khác → caller compute, expose generic flag. Form không know-how source.

Liên quan: [[ecomm-vs-crm-logic-boundary]]
