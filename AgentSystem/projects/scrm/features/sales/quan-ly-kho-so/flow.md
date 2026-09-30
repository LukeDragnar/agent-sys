# Luồng — Quản lý kho số

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc đơn vị (DonViId) và chi nhánh.
- Đơn vị đã có **cấu hình kho số** (`GetKhoSoCfgApplyAsync`): bật `IsSuDungKhoSo`, các tham số cooldown (`SoPhutGiuaCacLanSaleXemSo`, `SoPhutGiuaCacLanSaleLaySo`, `SoPhutGiuaCacLanContactMoXemSoTrongNgay`), giới hạn (`CountMaxSaleLaySoTrongNgay`), phạm vi (`ChiHienThiKhoSoCuaTeam`, `SauXPhutVaoKhoNotSaleNhatViewAllDonVi`), cờ hiển thị (`ChoPhepSuDungIconViewSo`, `ShowLsTacNghiepKhoSo`).
- Để **chọn số**: user có 1 trong role sale `DV-QL-SALE / DV-SALE / DV-SALE-LEAD`.
- Kho số đã có dữ liệu (lead chưa có chủ).

## Luồng dùng (sale)
1. Sale mở **Sale → Kho số**; hệ thống lọc danh sách kho số theo điều kiện + phân quyền team/đơn vị (`KhoSoStoreService.TimTheoDieuKienAsync`).
2. **Xem số**: nếu `ChoPhepSuDungIconViewSo=true`, sale bấm icon xem số → hệ thống kiểm tra cooldown sale + cooldown contact → pass thì insert `KhoSoViewLog` và trả về số điện thoại (`{Id, KhachHangPhone}`).
3. **Chọn số**: sale bấm "Chọn số" → hệ thống chạy chuỗi gate 10 bước (xem Case biên) → pass thì thực hiện trong transaction: xóa khỏi `KhoSo`, insert `KhoSoLog`, update Contact (gán SaleUserId/nhóm, `KhoSoIsDaVaoKho=false`, `PhanBoTuDau=KhoSo`), cập nhật đơn hàng của contact, log tác nghiệp.
4. Sau commit: reindex Elasticsearch (Contact + KhoSo), publish `DataUpdateContactMessage` (Mongo nhóm NV nếu có), publish `KiemTraThayDoiContact` (RabbitMQ).
5. (Tùy chọn) Sale mở **Lịch sử xem số** nếu `ShowLsTacNghiepKhoSo=true`.

## Luồng cấu hình (admin — khởi tạo lại kho)
1. Admin vào cấu hình kho số, **TẮT `IsSuDungKhoSo`** (đơn vị hoặc chi nhánh).
2. Bấm **Khởi tạo lại** (đơn vị → `KhoiTaoLaiKhoSoTheoDonVi`, chi nhánh → `KhoiTaoLaiKhoSoTheoChiNhanh`).
3. Hệ thống loop `StartKhoiDong(...)` reset kho theo lô 200 bản ghi.
4. Bật lại cấu hình dùng kho số khi cần dùng tiếp.

## Case biên (chuỗi gate ChonSo — KhoSoService.cs:1373)
1. Không có cấu hình kho số → "Không tìm thấy cấu hình".
2. `IsSuDungKhoSo == false` → "Đơn vị không cấu hình sử dụng kho số."
3. User không có role sale → "Chỉ có role sale... mới được chọn số!".
4. Cooldown lấy số: `SoPhutGiuaCacLanSaleLaySo != null` và lần lấy gần nhất + X phút > now → chặn, báo giờ mở lại.
5. Giới hạn ngày: `CountMaxSaleLaySoTrongNgay > 0` và số `KhoSoLog` hôm nay ≥ ngưỡng → "Bạn đã vượt quá số lần chọn số trong ngày".
6. Kho số không tồn tại / sai đơn vị → "Không tìm thấy contact!".
7. Lọc nhóm (khi tác nghiệp không `IsMacDinhVaoKhoTong`): nếu `SauXPhutVaoKhoNotSaleNhatViewAllDonVi > 0` thì lead quá X phút HOẶC thuộc nhóm user mới lấy được; nếu `<= 0` thì chỉ lấy được lead thuộc nhóm user → ngược lại "Không đủ điều kiện lấy số này!".
8. Contact phải `KhoSoIsDaVaoKho == true` → ngược lại "Contact không vào kho số...".
9. Contact chưa chốt đơn → ngược lại "Contact đã chốt đơn...".
10. Contact chưa đối soát → ngược lại "Contact đã được đối soát...".
- **Lỗi giữa transaction** → RollbackAsync, trả message lỗi, kho số không bị xóa.
- **Xem số quá tần suất** → chặn theo cooldown sale/contact, không trả số.
- **Khởi tạo lại khi `IsSuDungKhoSo` vẫn bật** → không chạy reset (điều kiện bắt buộc tắt trước).

## Ví dụ cụ thể có số
Shop C bật kho số, cấu hình: `SoPhutGiuaCacLanSaleLaySo = 30` phút, `CountMaxSaleLaySoTrongNgay = 10`, `ChiHienThiKhoSoCuaTeam = true`, `SauXPhutVaoKhoNotSaleNhatViewAllDonVi = 60` phút.
- Sale Minh (role DV-SALE, nhóm A) mở kho lúc 9:00, thấy 50 lead nhóm A + các lead đã vào kho > 60 phút.
- Minh chọn 1 lead nhóm A lúc 9:00 → pass, nhặt thành công (đây là lần thứ 1/10 hôm nay).
- Minh chọn lead tiếp lúc 9:10 → vì chưa đủ 30 phút từ lần trước → chặn, báo mở lại lúc 9:30.
- Minh chọn lead của nhóm B vừa vào kho 10 phút → chưa đủ 60 phút và không thuộc nhóm Minh → "Không đủ điều kiện lấy số này!".
- Lead nhóm B đã ở kho 70 phút → Minh chọn được (vượt mốc 60 phút).
- Đến lần thứ 11 trong ngày → "Bạn đã vượt quá số lần chọn số trong ngày".

## Hậu điều kiện
- **Chọn số thành công**: lead bị xóa khỏi kho, Contact có chủ mới (`PhanBoTuDau=KhoSo`), `KhoSoLog` ghi SaleFrom→SaleTo, đơn hàng của contact cập nhật SaleUserId, ES + Mongo + Rabbit đồng bộ.
- **Xem số**: chỉ ghi `KhoSoViewLog`, không đổi chủ lead.
- **Khởi tạo lại**: kho bị reset theo lô (không transaction tổng — lỗi giữa chừng có thể để kho ở trạng thái nửa reset).
