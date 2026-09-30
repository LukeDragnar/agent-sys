---
controller: PromotionUpdateWithBillController
module: marketing
service: Promotion
man_hinh_ui: "nội bộ — không có UI (service-to-service, gọi từ Order/OrderLogistic lúc thanh toán)"
muc_dich: "Áp KM + voucher + hoa hồng giới thiệu khi thanh toán đơn hàng (trừ lượt, đánh dấu đã dùng, sinh hoa hồng), rollback toàn bộ khi hủy đơn, và validate trước thanh toán. Biến thể riêng cho đơn OrderLogistic."
alias: ["áp KM khi thanh toán", "PromotionUpdateWithBill", "cập nhật khuyến mãi hóa đơn", "rollback khuyến mãi"]
auth: internal
state_machine: []
deprecated: false
entities: [KhuyenMai, KhuyenMaiKhachHang, KhuyenMaiKhachHangLichSu, VoucherOption, VoucherMa, VoucherLichSu, HoaHongGioiThieu, CauHinhHoaHongGioiThieu, NguonGioiThieu]
stored_procs: []
depends:
  - "Core/TienIch (kiểm tra đơn vị đã mua TI_KhuyenMai / TI_Voucher / TI_HoaHongGioiThieu — không có thì bỏ qua nhánh)"
  - "Customer/KhachHang (TimTheoId — số đơn đã TT thành công, IsKhachCu, hạng/nhóm KH cho validate)"
  - "Redis (GetAllKhuyenMaiFromRedis / GetAllKhuyenMaiKhachHangFromRedis; XoaRedisKhuyenMai sau commit)"
  - "MongoDB (IMongoDbClient — voucher option / collection promotion)"
  - "RabbitMQ (IPublishEndpoint — publish message voucher Deposit sau commit)"
cross_module_in:
  - "Order / OrderProcess (OrderRollBackService.PromotionUpdate / PromotionRollback — thu ngân)"
  - "OrderLogistic (PromotionUpdate_Logistic / PromotionRollback_Logistic — đơn logistic)"
cross_module_out:
  - "Redis KM cache (XoaRedisKhuyenMai sau mỗi áp/hủy)"
  - "LogTuongTacKhachHang (ghi KieuLoai=TangKhuyenMai khi áp KM)"
  - "RabbitMQ (message voucher Deposit)"
---

# PromotionUpdateWithBill — Áp khuyến mãi/voucher/hoa hồng khi thanh toán

## Hành động
> Toàn bộ controller là **internal** (`[PrivateSBAuthorize]`) — không có UI, chỉ service Order/OrderLogistic gọi. `[TienIchAttribute]` bị comment ⇒ không gắn check tiện ích ở controller (check tiện ích nằm trong service).

| # | Hành động | Endpoint (HTTP path) | Auth | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|
| 1 | Áp KM/voucher/hoa hồng khi thanh toán | POST `ThanhToanDonHang` | internal | `PromotionUpdate(payload)` — trong 1 transaction (ExecutionStrategy+retry): trừ lượt KM (`SoLuongDaDung`/`SoLuotDaApDung`), đánh dấu voucher đã dùng, insert hoa hồng giới thiệu. Sau commit: XoaRedisKhuyenMai + publish Deposit. | EF (transaction) | Toàn bộ KM/voucher/hoa hồng chung 1 transaction → lỗi 1 phần rollback tất cả |
| 2 | Rollback khi hủy đơn | POST `RollBackThanhToanDonHang` | internal | `PromotionRollback(OrderRollBackBody)` theo `IdDonHang` — hoàn lượt KM (`SoLuotDaApDung-1` guard `>0`), reset KhuyenMaiKhachHang (IsSuDung=false), xóa lịch sử, hoàn mã voucher, **xóa hoa hồng** theo IdDonHang. | EF (transaction) | Cần idempotency ở tầng gọi để không rollback 2 lần |
| 3 | Áp KM khi thanh toán (logistic) | POST `ThanhToanDonHang_Logistic` | internal | `PromotionUpdate_Logistic(payload)` — cùng cơ chế, payload OrderLogistic. | EF | — |
| 4 | Rollback (logistic) | POST `RollBackThanhToanDonHang_Logistic` | internal | `PromotionRollback_Logistic(OrderLogisticRollBackBody)`. | EF | — |
| 5 | Validate trước thanh toán | POST `ValidateThanhToanDonHang` | internal | `ValidateThanhToanDonHang(payload)` — kiểm tra KM chọn có hợp lệ trước khi cho thanh toán (KHÔNG ghi DB). | EF + Redis + Customer | — |

## Quy tắc nghiệp vụ (gating ValidateThanhToanDonHang — blueprint trace)
`ValidateThanhToanDonHang(payload)` (`:2250`) chạy chuỗi điều kiện, fail bất kỳ → trả message lỗi:
1. `IdKhachHang` rỗng → "Cần truyền Id Khách hàng!".
2. Gọi Customer `KhachHang/TimTheoId/{id}`; null/Success=false → "Không tìm thấy khách hàng!". Lấy `soDonHang = (SoDonHangThanhToanThanhCong ?? 0) + 1`, `isKhachCu`, hạng/nhóm KH.
3. KM đơn vị (Redis): lọc `TuNgay <= now <= DenNgay` & `IsSuDung` & (`IsApDungSoLuot != true` HOẶC `SoLuotDaApDung < SoLuotApDung`) & nằm trong list KM chọn.
4. Số lần/KH: join `KhuyenMaiKhachHangLichSu` đếm `SoLanDaDung`; giữ KM nếu `IsApDungSoLanSuDungDichVu == true` HOẶC `SoLanSuDungDichVu > SoLanDaDung`.
5. So sánh count KM hợp lệ vs count KM chọn — lệch → "Khuyến mãi đã chọn không phù hợp!".
6. Lọc tiếp theo KM được tặng (`GetAllKhuyenMaiKhachHangFromRedis`): chỉ giữ KM nằm trong list được tặng.
7. **Loại KH cũ/mới**: `LoaiKhachHangCuMoi != TatCa` & (`!= isKhachCu` & `!= KhachVangLai`) → lỗi.
8. **Áp theo lần mua**: `IsApDungVaoLanMua == true` & list lần mua không chứa `soDonHang` → lỗi.

## Quan hệ
- **Gọi sang:** Customer (TimTheoId), Redis (KM đơn vị + KM khách hàng), MongoDB (voucher option), CommonService.
- **Dữ liệu (entities):** KhuyenMai/KhuyenMaiKhachHang/...LichSu (lượt KM), VoucherOption/VoucherMa/VoucherLichSu (voucher), HoaHongGioiThieu/CauHinhHoaHongGioiThieu/NguonGioiThieu (hoa hồng).
- **Ripple/đồng bộ:** sau commit phải XoaRedisKhuyenMai; publish message voucher Deposit; ghi LogTuongTacKhachHang (KieuLoai=TangKhuyenMai).

## Luồng xuyên module (thanh toán → áp 3 nhánh → rollback khi hủy)
1. **Thanh toán** `PromotionUpdate` (`:115+`): check tiện ích đơn vị từng nhánh (TI_KhuyenMai/TI_Voucher/TI_HoaHongGioiThieu) — không mua thì bỏ nhánh đó. Khách vãng lai: chỉ voucher `IsKhongGioiHan`. Trong transaction: KM (lịch sử + tăng `SoLuongDaDung`/`SoLuotDaApDung`), voucher (đánh dấu mã), hoa hồng (`HoaHongGioiThieuUpdate` insert nếu IdNguonGioiThieu + có cấu hình + ListDoiTuong). Catch → RollbackAsync. Sau commit: XoaRedisKhuyenMai + publish Deposit.
2. **Hủy đơn** `PromotionRollback` (`:377+`): theo IdDonHang — hoàn lượt KM, reset KhuyenMaiKhachHang, xóa lịch sử, hoàn mã voucher, **DeleteAsync hoa hồng** (`:458-460`).
3. **Logistic**: cùng cơ chế, payload khác.
> Số lượt **stateful 2 chiều** (áp khi TT, hoàn khi hủy); rollback không chạy = lượt khóa vĩnh viễn; chạy 2 lần = trừ âm (guard `>0` cho SoLuotDaApDung, không phải mọi nhánh).

## Lỗi đã biết / lưu ý kỹ thuật
- `PromotionUpdateWithBillService.cs:217-296` — KM + voucher + hoa hồng chung **1 transaction** → lỗi 1 phần rollback tất cả (đúng ý đồ nhưng coupling cao, khó debug cục bộ).
- `PromotionUpdateWithBillService.cs:355-358` — rollback `SoLuotDaApDung` guard `>0`; nhưng reset cờ KhuyenMaiKhachHang không kiểm tra trạng thái cũ → idempotency phải đảm bảo ở OrderRollBackService.
- `PromotionUpdateWithBillService.cs:2241` — công thức hoa hồng: `DonViTinh==Tien ? GiaTri*SoLuong : Math.Floor(ThanhTien*GiaTri/100)` — phần trăm bị **floor** (mất phần thập phân, có thể lệch tổng vài đồng).
- `PromotionUpdateWithBillController.cs:11` — `[TienIchAttribute]` bị comment ⇒ controller không tự chặn theo tiện ích; phụ thuộc hoàn toàn check tiện ích trong service.
- Tất cả endpoint `[PrivateSBAuthorize]` ⇒ chỉ gọi nội bộ; lộ ra ngoài = trừ lượt/đánh dấu mã sai dữ liệu.
