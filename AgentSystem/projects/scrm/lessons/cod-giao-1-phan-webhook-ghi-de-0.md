# Lesson: COD giao 1 phần bị webhook AUTO ghi đè về 0 (cross-service delivery_status ↔ CRM)

**Sự cố (2026-07-24, đơn LM001000866 VTP):** đơn giao-1-phần, ô "Số tiền giao hàng một phần" (`DonHangLogistic.GiaoHangMotPhanCOD`) cứ **nhảy 0** mỗi lần webhook DVVC (AUTO) bắn về; nhân viên phải sửa tay lặp đi lặp lại. Hệ quả: nghiệp vụ tự động tính **chi tiêu + công nợ khách THIẾU** (status 35 GiaoHangMotPhan: target = `GiaoHangMotPhanCOD + DonHangDatCoc` — xem `NghiepVuPendingService.TinhSoTienTarget`).

## Gốc rễ (2 tầng)
1. **Bên GỬI (`delivery_status/.../Partner/VtpService.cs`):** nhánh 1 phần (`OrderCodeExternal.EndsWith("1P1")`) set `IsSignPart=true`/`OrderStatusId=35` nhưng **KHÔNG bơm `CodSignPart`** → luôn null. Số thu phần giao nằm sẵn ở **`orderDb.ReturnCod`** ("COD sau sửa", set ở status **202 "Sửa phiếu gửi"** = `MONEY_COLLECTION` mới).
2. **Bên NHẬN (`OrderLogistic/.../DeliveryUnitConnect/OrderService.cs` → `ReceiveStatusOrder`):** ghi đè `GiaoHangMotPhanCOD = ConvertToDouble(reqBody.CodSignPart)` **vô điều kiện** mỗi webhook → `null → 0`. Cùng lớp bug: `GiaoHang1Phan`, `DonHangHoanMaDonGiaoVan/ShipFee/COD` cũng bị đè (webhook đè cả số thủ kho nhập tay → lệch đối soát/kế toán).

Chuỗi đúng của số: `delivery_status OrderStatus.ReturnCod` → DTO **`CodSignPart`** → CRM **`DonHangLogistic.GiaoHangMotPhanCOD`**.

## Cơ chế VTP báo "giao 1 phần" (đừng đoán sai qua status number)
VTP sinh **đơn con mã `...1P1`** (đơn hoàn phần chưa giao, COD của nó = 0) + **sửa COD** đơn cha (status 202) từ tổng → số thu phần giao. Nhận diện bằng **`EndsWith("1P1")`**, KHÔNG phải qua status. Các status di chuyển chặng 1P1 (300/400/500) phần lớn bị guard `[FIX 2026-07-22]` skip (không bắn CRM), chỉ số ít forward → LogData `IsSignPart:true` thưa.

## Fix pattern — quy ước "0 = no-value"
**Đối tác trả `0`/rỗng/null cho các field này = "không có thông tin", KHÔNG phải số 0 thật → SKIP giữ nguyên giá trị đang có (số manual/1P1), không đè.** Đồng nhất mọi đối tác, **CRM phải agnostic — KHÔNG branch theo `ShippingUnitCode`**.
- Field **tiền** (`CodSignPart`/`ReturnFee`/`ReturnCod`): guard `FunctionCommon.ConvertToDouble(x) > 0` (nuốt null + robust mọi dạng zero `"0"`/`"0.0"`/`"00"`).
- Field **mã** (`DonHangHoanMaDonGiaoVan`, chuỗi): guard `!IsNullOrWhiteSpace(x) && x != "0"`.
- Cờ `GiaoHang1Phan`: chỉ set `true` khi `reqBody.IsSignPart` (không đè về false).
- Bên gửi: bơm `reqBodyUpdateStatus.CodSignPart = orderDb.ReturnCod` (nhánh 1P1).
- ⚠️ Đừng "sửa placeholder ở từng sender" (GHTK gửi literal `"0"`) — xử **một chỗ ở CRM guard** là agnostic + phủ hết đối tác.

## Bẫy cherry-pick staging: lệch LOGIC (không chỉ config)
Cherry-pick fix product→`release_staging` **conflict** vì staging đọc `reqBody.ReturnCod` cho `GiaoHangMotPhanCOD` còn product đọc `reqBody.CodSignPart` — 2 nhánh diverge về **LOGIC field**, không chỉ config/i18n. Resolve phải theo **semantic đúng end-to-end** (khớp sender bơm `CodSignPart`) = giữ bản fix, KHÔNG mù quáng "giữ HEAD staging". → mở rộng `feedback_cherrypick_feature_branch_config_gate` từ *config* sang cả *logic*.

## Query debug (DB `DeliveryStatus`; cột base = `CreatedDate`/`IsDeleted`, KHÔNG phải NgayTao)
- **Request gửi CRM:** `LogData.DataInfo` chứa `IsSignPart`/`CodSignPart`/`OrderStatusId` + response. Lọc `DataInfo LIKE '%ReceiveStatusOrder%'`; **mã đơn nằm TRONG DataInfo** (cột `OrderCode` = NULL cho log ClientHelper). Retention ≥ 1 tuần.
- **Webhook thô đối tác:** `DataReceive` (live, update-không-xóa) / `BakDataReceive` (IsDeleted=1). Đơn 1P1: `OrderCodeExternal LIKE '%1P1'`.
- **Số cuối cùng:** CRM `DonHangLogistic.GiaoHangMotPhanCOD` (MaDon = bỏ tiền tố `SBV` của OrderCodeExternal).
- **Gotcha "Không thỏa mãn điều kiện để được pheps update status mới"** = code CŨ, **VÔ HẠI**: VTP gửi cùng lúc `104`+`100` (cùng timestamp), 104 áp trước → 100 bị skip (chống áp trạng thái lùi/trùng), `Success=true`+`IsDeleted=1`. Mỗi đơn mới ~1 dòng skip-100 → volume tỷ lệ lượng đơn, KHÔNG phải lỗi.

## Deploy
2 service độc lập — **phải deploy CẢ 2** (delivery_status bơm `CodSignPart` + OrderLogistic guard) mới đủ. Chỉ 1 → nửa fix (chỉ delivery_status: 1P1 set đúng nhưng webhook thường vẫn đè 0 vì thiếu guard).
