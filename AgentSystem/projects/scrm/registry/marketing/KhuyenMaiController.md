---
controller: KhuyenMaiController
module: marketing
service: Promotion
man_hinh_ui: "Marketing > Khuyến mãi (list: khuyenmai.component; edit: khuyenmai-edit-v1)"
muc_dich: "CRUD chương trình khuyến mãi (KM sản phẩm/đơn hàng), cấu hình điều kiện áp dụng (ngày, khung giờ vàng, số lượt, số lần/khách, ngưỡng SL/giá trị), và phục vụ tra cứu KM khả dụng khi thanh toán đơn hàng."
alias: [KhuyenMai, KM, Promotion, "chương trình khuyến mãi"]
phu_thuoc:
  - "Core/TienIch (GetDanhSachTienIchDaMua — đơn vị có mua tiện ích TI_KhuyenMai/TI_Voucher chưa)"
  - "Customer/KhachHang (TimTheoId — hạng KH, nhóm KH để lọc KM)"
  - "WarehouseProduct/SanPham (GetListSanPhamTangByIdsPrivate — quy đổi đơn vị tính khi so SL)"
  - "Redis (GetAllKhuyenMaiFromRedis / XoaRedisKhuyenMai — cache KM theo đơn vị)"
  - "PromotionUpdateWithBillService (áp KM khi thanh toán + rollback)"
ripple:
  - "Order / OrderProcess (OrderRollBackService) — gọi áp KM lúc thanh toán & rollback lúc hủy đơn"
  - "OrderLogistic (đơn logistic — PromotionUpdate_Logistic / PromotionRollback_Logistic)"
  - "Redis KM cache — phải xóa sau mỗi Create/Update/Delete và sau khi trừ lượt"
  - "LogTuongTacKhachHang (ghi lịch sử tương tác KH khi áp KM)"
---

# KhuyenMai — Quản lý chương trình khuyến mãi

## Hành động
| # | Hành động | Nhãn UI/nút | Endpoint | Quyền | Hành vi đúng (expected) | Known-issue |
|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện (grid) | Danh sách / Lọc | POST `TimTheoDieuKien` | `KhuyenMai_TimTheoDieuKien` (AD-TONG/CN/MCN, QLDV) | Non-admin bị ép `IdChiNhanh = CurrentUser.IdChiNhanh` (chỉ thấy KM chi nhánh mình). ModelState invalid → trả lỗi. | — |
| 2 | Lấy tất cả KM còn hạn/chưa kích hoạt | (load combo) | POST `LayTatCa` | `KhuyenMai_LayTatCa` (AD-TONG/CN/MCN) | Trả KM còn hạn hoặc chưa kích hoạt theo đơn vị. | — |
| 3 | Thêm mới | Lưu | POST `ThemMoi` | `KhuyenMai_ThemMoi` (AD-TONG) | `Create(payload)`. Validate điều kiện rồi insert. Xóa Redis KM. | — |
| 4 | Thêm mới V2 | Lưu (form v2) | POST `ThemMoiV2` | `[SBAuthorize()]` (chỉ cần login) | `Create(payload, isVer2=true)`. **Authorize rỗng** → mọi user login gọi được (xem lưu ý). | Quyền lỏng hơn ThemMoi |
| 5 | Chi tiết theo Id | Mở edit | GET `TimTheoId/{id}` | `KhuyenMai_TimTheoId` (AD-TONG/CN/MCN, QLDV) | `GetById(id)`. | — |
| 6 | Chi tiết KM cho đơn | — | POST `GetChiTietKhuyenMai` | `[SBAuthorize()]` | Trả chi tiết KM + check điều kiện áp dụng (SL/giá trị/đối tượng) theo dòng đơn. | — |
| 7 | KM cấp bậc theo Id | — | POST `KhuyenMaiCapBacById` / `GetKhuyenMaiCapBacById` | `[SBAuthorize()]` | KM nhiều cấp bậc (ngưỡng/tier). | — |
| 8 | Cập nhật | Lưu | POST `CapNhat` | `KhuyenMai_CapNhat` (AD-TONG) | `Update(model)`. Xóa Redis KM. | — |
| 9 | Cập nhật V2 | Lưu (v2) | POST `CapNhatV2` | `[SBAuthorize()]` | `Update(model, isVer2=true)`. **Authorize rỗng.** | Quyền lỏng hơn CapNhat |
| 10 | Xóa | Xóa | GET `Xoa/{id}` | `KhuyenMai_Xoa` (AD-TONG) | `Delete(id)`. Xóa Redis KM. | GET cho hành động xóa (không idempotent-safe) |
| 11 | KM khả dụng theo KH | — | POST `GetKhuyenMaiByIdKhachHang` (+ `_V2`, `_DonHang`) | `KhuyenMai_GetKhuyenMaiByIdKhachHang*` (nhiều role) | Lọc KM theo hạng/nhóm KH + còn hạn + còn lượt. V2 mở rộng nhiều role (KT, kho...). | 3 biến thể song song (drift risk) |
| 12 | KM cho đơn (thu ngân/logistic) | — | POST `GetKhuyenMai_DonHang_V2` / `GetKhuyenMai_DonHang` | `[SBAuthorize()]` | Trả KM áp được cho giỏ đơn; quy đổi ĐVT qua Warehouse. | V2 và V1 cùng tồn tại |
| 13 | Tìm cấp bậc KM theo đơn | — | POST `TimCapBacKhuyenMaiTheoDon` | `[SBAuthorize()]` | Xác định tier KM khớp tổng đơn. | — |
| 14 | **Kiểm tra giới hạn áp dụng** | — | POST `KiemTraGioiHanApDungTheoDon` | `[SBAuthorize()]` | Gate nhiều nhánh: tồn tại → IsSuDung → trong hạn → lượt global → số lần/KH. Trả `null`=OK, ngược lại trả message. | — |
| 15 | Batch check KM sản phẩm | — | POST `KiemTraKMSanPhamBatch` | `[SBAuthorize()]` | Redis-first; mỗi SP trả `IsXoaKmSanPham + LyDoXoa` (hết tồn/tạm ngừng/hết hạn/ngoài khung giờ vàng...). | — |
| 16 | Lấy KM web booking | — | POST `GetListKhuyenMaiShowOnWebBooking` | `[AllowAnonymous]` | Public — KM hiển thị trên web booking. | Anonymous, không filter đơn vị qua token |
| 17 | API private (service-to-service) | — | POST `GetByIds` / `GetKhuyenMaiByIds` / `LayTatCa_Private` / `GetById_Private` / `GetPromotionPrivate` | `[PrivateSBAuthorize]` (1 cái `[SBAuthorize()]`) | Service khác gọi để lấy snapshot KM. | `GetKhuyenMaiByIds` trả `Ok()` thô (không ResponseResult wrapper) |
| 18 | Import combo KM (Excel) | Tải template / Import / Tải lỗi | POST `DownloadFileTemplateImportKhuyenMai`, POST `ImportKhuyenMai`, GET `DownloadDataWrongKhuyenMai/{id}` | `KhuyenMai_*ImportKhuyenMai*` (AD-TONG) | Dùng `IKhuyenMaiImportService` (tách khỏi service chính). | — |

## Quan hệ
- **Gọi sang:** Core/TienIch (check tiện ích đã mua), Customer/KhachHang (hạng/nhóm KH), WarehouseProduct/SanPham (quy đổi ĐVT), Redis (cache KM).
- **Dữ liệu (entities):** `KhuyenMai` (master + điều kiện), `KhuyenMaiKhachHang` (KM đã gán/đã dùng theo KH+đơn), `KhuyenMaiKhachHangLichSu` (lịch sử mỗi lần áp — nguồn đếm số-lần/KH), liên quan `Voucher*`, `HoaHongGioiThieu` (xử lý chung trong PromotionUpdateWithBill).
- **Ripple/đồng bộ:** mỗi Create/Update/Delete và mỗi lần trừ lượt khi thanh toán → **phải XoaRedisKhuyenMai(donViId)** nếu không cache lệch. Áp KM khi thanh toán còn ghi LogTuongTacKhachHang (KieuLoai=TangKhuyenMai).

## Luồng xuyên module (áp KM khi thanh toán → trừ lượt → rollback khi hủy đơn)
Controller này CRUD/tra cứu; việc **trừ lượt** không nằm ở đây mà ở `PromotionUpdateWithBillService` (controller `PromotionUpdateWithBillController`), do OrderProcess/Order gọi:

1. **Thanh toán** — `OrderRollBackService.PromotionUpdate` → POST `/api/PromotionUpdateWithBill/ThanhToanDonHang` → `PromotionUpdate(payload)`:
   - Check tiện ích đơn vị (TI_KhuyenMai / TI_Voucher / TI_HoaHongGioiThieu) — không có thì bỏ qua nhánh đó.
   - Khách vãng lai (IdKhachHang rỗng): chỉ voucher `IsKhongGioiHan` mới áp.
   - Trong **1 transaction (ExecutionStrategy + retry)**: insert `KhuyenMaiKhachHangLichSu`, update `KhuyenMaiKhachHang` (IsSuDung=true, gán IdDonHang/NgaySuDung), **tăng `SoLuongDaDung`** (KM/KH) và **`SoLuotDaApDung`** (global); voucher & hoa hồng tương tự. Catch → RollbackAsync, trả lỗi.
   - Sau commit: XoaRedisKhuyenMai; publish message voucher Deposit nếu có.
2. **Hủy/rollback đơn** — `PromotionRollback(OrderRollBackBody)` theo `IdDonHang`:
   - Lấy `KhuyenMaiKhachHangLichSu` của đơn → KM có `IsApDungSoLuot=true` thì **`SoLuotDaApDung -= 1`** (chỉ khi >0).
   - `KhuyenMaiKhachHang` của đơn: set `IsSuDung=false`, `IdDonHang=null`, `NgaySuDung=null`.
   - Xóa các bản ghi lịch sử; voucher hoàn (NhieuMa→IsDaDung=false, MotMa→SoLanSuDung-1, option SoLuongDaDung-=n). Tất cả trong transaction rollback-safe.
3. **Biến thể logistic** — `PromotionUpdate_Logistic` / `PromotionRollback_Logistic` cho đơn OrderLogistic (cùng cơ chế, payload khác).

> Hệ quả: số lượt là **stateful 2 chiều** — áp khi thanh toán, hoàn khi hủy. Bug điển hình nếu rollback không chạy = lượt bị "khóa" vĩnh viễn; nếu rollback chạy 2 lần (idempotency) = trừ âm (đã guard `> 0` cho SoLuotDaApDung nhưng SoLuongDaDung của KhuyenMaiKhachHang giảm không guard ở vài nhánh — xem path).

## Lỗi đã biết / lưu ý kỹ thuật (path:line)
- `KhuyenMaiController.cs:65,126,178,238` — nhiều endpoint nhạy cảm (ThemMoiV2, CapNhatV2, GetKhuyenMai_DonHang...) dùng `[SBAuthorize()]` **rỗng** (chỉ cần login) trong khi bản V1 yêu cầu role AD-TONG → khe hở phân quyền nếu V2 lộ ra UI.
- `KhuyenMaiController.cs:196` — `GetListKhuyenMaiShowOnWebBooking` `[AllowAnonymous]`: không lọc đơn vị qua token, dựa hoàn toàn vào `gridInfo` body.
- `KhuyenMaiController.cs:220` — `GetKhuyenMaiByIds` trả `Ok(result)` thay vì `ResponseResult(result)` → response shape khác các endpoint còn lại (consumer phải biết).
- `KhuyenMaiController.cs:139` — `Xoa` dùng HTTP GET cho hành động xóa (dễ trigger ngoài ý muốn, không CSRF-safe).
- `KhuyenMaiService.cs:1415` — global limit check dùng `(SoLuotDaApDung ?? 0) >= (SoLuotApDung ?? 0)`: nếu `SoLuotApDung` null (không cấu hình) thì `0 >= 0` = hết lượt ngay → cần đảm bảo IsApDungSoLuot chỉ true khi có cấu hình SoLuotApDung.
- `KhuyenMaiService.cs:1425` — per-customer limit: `SoLanSuDungDichVu == null` → coi như đã quá số lần (chặn). Đếm bằng `CountAsync` trên `KhuyenMaiKhachHangLichSu` (đọc nolock — có thể đếm thiếu dưới race khi thanh toán đồng thời).
- `PromotionUpdateWithBillService.cs:217-296` — áp KM trong ExecutionStrategy+transaction; **toàn bộ KM/voucher/hoa hồng chung 1 transaction** → lỗi 1 phần rollback tất cả (đúng ý đồ, nhưng coupling cao).
- `PromotionUpdateWithBillService.cs:355-358` — rollback `SoLuotDaApDung` có guard `> 0`; nhưng `KhuyenMaiKhachHang.IsSuDung=false` reset không kiểm tra trạng thái → gọi rollback lặp an toàn cho cờ nhưng cần idempotency ở tầng gọi (OrderRollBackService) để không trừ lượt 2 lần.
- `KhuyenMaiService.cs` ~6696 dòng — service "god class": Create/Update + ~5 biến thể GetKhuyenMaiByIdKhachHang (`_V1/_V2/_DonHang/OLD`) + GetKhuyenMai_DonHang(`_V2`) cùng tồn tại → drift logic giữa các bản, sửa rule phải đồng bộ nhiều nhánh.
- Entity `KhuyenMai.cs`: `LoaiKhuyenMai` default `1` (KM sản phẩm) — phân nhánh chính KM sản phẩm vs đơn hàng nằm ở field này; nhiều rule (`SanPhamIsApDungSoLuong`, `DichVuIsApDungSoLuong`, ngưỡng SL Tu/Den) chỉ áp theo loại.
