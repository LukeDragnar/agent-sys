---
controller: CommonController
module: marketing
service: MarketingCampaign
man_hinh_ui: "nội bộ — không có UI (controller debug/fix-bug, hiện không hoạt động)"
muc_dich: "Controller dùng chung của MarketingCampaign — hiện chỉ chứa 1 endpoint FixBug (debug thủ công), đã bị vô hiệu bằng return sớm."
alias: ["common marketing campaign", "fixbug marketing"]
auth: anonymous
state_machine: []
deprecated: true
entities: []
stored_procs: []
depends: [CommonService]
cross_module_in: []
cross_module_out: []
---

# CommonController (MarketingCampaign) — Controller chung (debug)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | FixBug (debug) | (không có UI) | POST `api/Common/FixBug` | (none) | anonymous | - | **VÔ HIỆU**: `return ResponseResult(new MethodResult<object>())` ngay đầu hàm; phần dưới (gọi GetDonHangLogisticSanPham/Combo theo id hardcode) là dead code | - | `[AllowAnonymous]` + dead code; id đơn vị/đơn hàng hardcode (CommonController.cs:31-38) |

## Quan hệ
- **Gọi sang:** `ICommonService` (GetDonHangLogisticSanPhamByIdsDonHang / GetDonHangLogisticComboByIdsDonHang) — chỉ ở phần dead code.
- **Dữ liệu (entities):** không (debug).
- **Ripple / đồng bộ:** không.

## Lỗi đã biết / lưu ý kỹ thuật
- Endpoint duy nhất `FixBug` đã bị khóa bằng `return` sớm — toàn bộ logic phía dưới (region "HaiHN Fix Bug") là dead code, chứa GUID hardcode. `[AllowAnonymous]` → nếu logic được mở lại sẽ là endpoint không auth. Đánh dấu `deprecated: true`.
