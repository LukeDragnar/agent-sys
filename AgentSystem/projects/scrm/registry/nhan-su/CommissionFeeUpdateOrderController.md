---
controller: CommissionFeeUpdateOrderController
module: nhan-su
service: CommissionFee
man_hinh_ui: "nội bộ — không có UI (service-to-service: Order/OrderLogistic đẩy đơn sang để tính hoa hồng)"
muc_dich: "Nhận snapshot đơn hàng đã thanh toán, đối chiếu cấu hình nhóm hoa hồng + chiến dịch để sinh dữ liệu hoa hồng (DonHang / DonHangChiTiet / DonHangChiTietChienDich) cho CTV, sale, marketing, thu ngân; và rollback khi hủy đơn."
alias: [CommissionFeeUpdateOrder, "đẩy đơn tính hoa hồng", "rollback hoa hồng"]
auth: internal
state_machine: []
deprecated: false
entities: [DonHang, DonHangChiTiet, DonHangChiTietChienDich, CongTacVien, NhomHoaHongCTV]
stored_procs: [PRC_CommissionFee_GetCauHinh_ChienDich]
depends: [Order, OrderLogistic, CauHinhSetting (CommonCacheService), MongoDB, Redis]
cross_module_in: [Order/OrderProcess, OrderLogistic]
cross_module_out: [CongTacVienController (báo cáo thống kê đọc lại dữ liệu này)]
---

# CommissionFeeUpdateOrder — Đẩy đơn tính hoa hồng / Rollback hoa hồng

## Hành động (service-to-service, không UI)
| # | Hành động | Endpoint | Quyền | Auth | Hành vi đúng (expected) | Nguồn | Known-issue |
|---|---|---|---|---|---|---|---|
| 1 | Tính/ghi hoa hồng cho 1 đơn | POST `CommissionFeeUpdateOrder` | — | internal (`[PrivateSBAuthorize]`) | Validate → nếu cấu hình `IsDongBoThongTinChiKhiCoMaCTV=true` và đơn không có CTV → return true (bỏ qua, không ghi gì). Ngược lại: xóa DonHang/DonHangChiTiet cũ cùng `IdDonHang`, insert lại bản mới theo cấu hình. Tất cả trong 1 transaction (ExecutionStrategy retry). | EF + SP cấu hình chiến dịch | Upsert kiểu delete-then-insert theo `IdDonHang`; gọi lại đè toàn bộ |
| 2 | Rollback hoa hồng khi hủy đơn | POST `CommissionFeeRollBackOrder` | — | internal (`[PrivateSBAuthorize]`) | Theo `IdDonVi + IdTransaction`: xóa DonHang, DonHangChiTiet, DonHangChiTietChienDich của transaction đó trong 1 transaction. Lỗi → rollback, trả lỗi. | EF | Xóa theo `IdTransaction` chứ KHÔNG theo `IdDonHang` (khác key với hàm Update — xem lưu ý) |

## Quy tắc nghiệp vụ (sinh dữ liệu hoa hồng)
Thứ tự xử lý trong `CommissionFeeUpdateOrder`:
1. **Validate** (`ValidateUpdateOrder`): bắt buộc `IdTransaction`, `IdDonVi`, `CommissionFeeOrder`, `IdDonHang`, `IdChiNhanh`, `MaDonHang`, có chi tiết đơn. `KieuLoaiDonHang` chỉ chấp nhận `Order` hoặc `OrderLogistic`. Nếu có `IdCongTacVien` thì **bắt buộc có cả `MaCongTacVien`** (và ngược lại) → thiếu 1 trong 2 = lỗi. Mỗi dòng chi tiết: `IdDoiTuong`, `LoaiDoiTuong` (SanPham..The), `SoLuong > 0`.
2. **Lấy cấu hình đơn vị** (`CauHinhSetting` qua cache): cờ `IsDongBoThongTinChiKhiCoMaCTV`, `IsTaoThongTinDonHangChoSale/Marketing/ThuNgan`. Cache miss → default tất cả = true.
3. **Gate CTV**: nếu `IsDongBoThongTinChiKhiCoMaCTV=true` và đơn KHÔNG có CTV → **dừng, return true** (không ghi).
4. **Gom danh sách "người hưởng hoa hồng"** (`listCongTacVien`): CTV gốc của đơn + nhân viên Sale + Marketing + Thu ngân (chỉ thêm nếu cờ tương ứng bật, id ≠ id CTV, chưa có trong list; thu ngân = `IdQlDichVuHoTro ?? IdQlDichVu`). Nhân viên được khớp với bản ghi CongTacVien có `KieuLoai = NhanVien` theo `IdUser`.
5. **Map sang nhóm hoa hồng**: join CTV → `NhomHoaHongCTV` (theo `IdNhomHoaHongCTV`). Không có nhóm → không sinh dữ liệu.
6. **Sinh 3 bảng**: `AutoMapperDonHang` (header), `AutoMapperDonHangChiTiet` (theo từng dòng × từng người), `AutoMapperDonHangChiTietChienDich` (chỉ nhóm có `IsSuDungHoaHongChienDich=true`, tra SP `PRC_CommissionFee_GetCauHinh_ChienDich` lấy %HoaHongCTV/%HoaHongLienKet theo IdDoiTuong+nhóm+ngày áp dụng).

## Quy tắc tính tiền hoa hồng chiến dịch (`AutoMapperDonHangChiTietChienDich`)
Cho mỗi dòng × mỗi người hưởng (CTV/sale/mkt/thu ngân) khớp `IdNhomHoaHongCTV` của cấu hình chiến dịch:
- `GiaTriHoaHongCTV_TruocVAT_TruocCK = ThanhTien * HoaHongCTV / 100`
- `GiaTriHoaHongCTV_TruocVAT_SauCK  = ThanhTienSauCkTruocVat * HoaHongCTV / 100`
- `GiaTriHoaHongCTV_SauVAT          = ThanhTienSauCkSauVat * HoaHongCTV / 100`
- 3 dòng tương ứng cho `HoaHongLienKet` (liên kết) trên cùng các mốc tiền.
- `HoaHongCTV` / `HoaHongLienKet` (%) lấy từ cấu hình chiến dịch (SP), không có → 0.

> Mỗi người chỉ được sinh **1 bản ghi/đơn** (guard `listIdCTVCreated`): nếu cùng 1 user vừa là CTV vừa là sale thì chỉ ghi 1 lần (ưu tiên theo thứ tự CTV → marketing → sale → thu ngân).

## Quan hệ
- **Gọi sang:** CommonCacheService (CauHinhSetting đơn vị), SP `PRC_CommissionFee_GetCauHinh_ChienDich`.
- **Dữ liệu (entities):** `DonHang`, `DonHangChiTiet`, `DonHangChiTietChienDich` (snapshot hoa hồng theo đơn), đọc `CongTacVien` + `NhomHoaHongCTV`.
- **Ripple/đồng bộ:** dữ liệu sinh ra ở đây là nguồn cho toàn bộ báo cáo thống kê hoa hồng của `CongTacVienController` (GetStats_*).

## Luồng xuyên module (Order thanh toán → tính hoa hồng → hủy đơn rollback)
1. Order/OrderLogistic thanh toán xong → publish/gọi `CommissionFeeUpdateOrder` với snapshot đơn (header + chi tiết + tiền sau CK/VAT đã phân bổ sẵn).
2. CommissionFee ghi DonHang/DonHangChiTiet/DonHangChiTietChienDich (xóa bản cũ cùng IdDonHang trước).
3. Hủy đơn → Order gọi `CommissionFeeRollBackOrder` theo `IdTransaction` để dọn dữ liệu hoa hồng.

## Lỗi đã biết / lưu ý kỹ thuật
- `CommissionFeeUpdateOrderService.cs:142` — `CommissionFeeUpdateOrder` xóa dữ liệu cũ theo **`IdDonHang`**, nhưng `CommissionFeeRollBackOrder` (dòng 80) xóa theo **`IdTransaction`** → 2 hàm dùng key khác nhau; nếu IdTransaction đổi giữa các lần đẩy mà IdDonHang giữ nguyên, rollback có thể không trúng bản ghi đã upsert.
- `CommissionFeeUpdateOrderService.cs:275` — có `return` chết sau khối if/else (unreachable), vô hại nhưng nên dọn.
- `CommissionFeeUpdateOrderService.cs:236-255` — DonHangChiTietChienDich được insert TRƯỚC khi xóa bản chiến dịch cũ (không thấy xóa DonHangChiTietChienDich cũ theo IdDonHang trong nhánh Update) → có thể **tích lũy bản ghi chiến dịch cũ** khi đẩy lại cùng đơn; chỉ rollback theo IdTransaction mới dọn được.
- `CommissionFeeUpdateOrderService.cs:804` — điều kiện check LoaiDoiTuong dùng `< SanPham && > The` (luôn false) → guard loại đối tượng thực chất không chặn được giá trị ngoài range.
- `[PrivateSBAuthorize]` — chỉ service nội bộ gọi được; không có phân quyền theo role người dùng.
