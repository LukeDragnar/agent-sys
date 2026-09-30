# Luồng — Cấu hình Sale

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc đơn vị + chi nhánh.
- Quyền: lưu cấu hình chính cần DV-AD-TONG; cấu hình SuperAdmin cần HT-AD/HT-CSKH; cấu hình giảm giá cần AD-TONG/CN/MCN hoặc QLDV.
- Có sẵn dữ liệu nền liên quan (nguồn dữ liệu, chi nhánh active, sản phẩm…) tùy tab.

## Luồng cấu hình (admin)
**A. Cấu hình Contact (service Contact — CauHinhController):**
1. Mở màn Cấu hình Contact, load cấu hình hiện có (`GetCauHinhInit` / `Get`).
2. Chỉnh tab tương ứng và Lưu:
   - Init: `SaveCauHinhInit`.
   - Kho số đơn vị: `LuuCauHinhKhoSo`; kho số chi nhánh: `SaveKhoSoCauHinhChiNhanh` → **tự đồng bộ lại cấu hình kho số cấp đơn vị** (gom chi nhánh active qua `SaveDonViSuDungKhoSo`).
   - Chia contact xoay vòng: `SaveCauHinhChiaContactXoayVong`.
   - Quản lý nguồn dữ liệu: `SaveQuanLyNguonDuLieu`.
   - MKT chia số thủ công: `SaveCauHinhMKT`.
   - Sinh bản ghi CSKH: `SaveCauHinhSinhBanGhiCSKH` (kèm `LichSuThayDoiCauHinh`).
   - Giao hàng → CSKH: `SaveCauHinhGiaoHangChuyenCskh`.
3. SuperAdmin (cấp hệ thống): `Save_SuperAdmin` (HT-AD/HT-CSKH).

**B. Cấu hình đơn hàng sale (service OrderLogistic — CauHinhController):**
1. Mở màn Cấu hình order, load (`GetCauHinhDonHangSale` / `GetCauHinhInit`).
2. Lưu cấu hình đơn hàng sale: `SaveCauHinhDonHangSale` (DV-AD-TONG); init: `SaveCauHinhInit`; nghiệp vụ: `SaveNghiepVuCauHinh`; SuperAdmin: `Save_SuperAdmin` (HT-AD/HT-CSKH).
3. Cấu hình đồng bộ 2 chiều: Contact ↔ OrderLogistic (`DongBoCauHinhDonHangTuContact`), Invoice → OrderLogistic (`CapNhatInvoice`).

**C. Cấu hình giảm giá theo đơn (service Promotion — DonHangCauHinhGiamGia):**
1. Mở màn cấu hình giảm giá, list (`TimTheoDieuKien`).
2. Thêm dòng (`ThemMoi`) / sửa (`CapNhatThongTin`): nhập `GiaTriTu`, `GiaTriDen`, `GiamGiaKieuLoai` (PhanTram/Tien), `GiamGiaGiaTri` → validate + check overlap → ghi log + clear cache Redis theo chi nhánh.
3. Xóa (`Xoa/{id}`). Khi thanh toán, Order gọi `GetConfigOrderByPrice/{price}` để lấy mức giảm.

**D. Cấu hình quyền sửa field contact — ma trận sửa số (service Contact — CauHinhSuaSoController)** *(bổ sung Pha 2 — CauHinhSuaSo):*
1. Mở màn cấu hình ma trận sửa số → load ma trận theo đơn vị: `GetCauHinhInit` (DV-AD-TONG/HT-AD/HT-CSKH). Đọc cache đơn vị trước; miss → query `CauHinhSuaSo` theo `IdDonVi` (UnitCodeId + Roles), order theo UnitCodeId, set cache rồi trả.
2. Mỗi field/cột (UnitCode, map từ enum `EnumCauHinhSuaSoContact`) tích chọn danh sách role được phép sửa.
3. **Lưu**: `SaveCauHinhInit` (DV-AD-TONG/HT-AD/HT-CSKH) — upsert theo đơn vị:
   - Đơn vị **đã có** cấu hình → loop từng bản ghi DB, update `Roles` cho UnitCodeId khớp request (MarkDirty `Roles`); UnitCode mới trong request mà DB chưa có → **bỏ qua, không insert**.
   - Đơn vị **chưa có** cấu hình → insert toàn bộ ma trận (gán `EnumUnitCode = ((EnumCauHinhSuaSoContact)UnitCodeId).ToString()`).
4. Kết thúc: **clear cache** `CauHinhSuaSoSetting` theo đơn vị; trả `Guid.Empty` + "Lưu cấu hình thành công".

## Luồng dùng (gián tiếp khi bán hàng)
- Khách thanh toán → hệ thống tra tổng đơn → `GetConfigOrderByPrice(price)` chọn cấu hình `SuDung==true` chứa price (ưu tiên `GiaTriDen` lớn nhất) → áp mức giảm.
- Kho số / chia số / care đơn hoạt động theo các cờ cấu hình đã lưu.

## Case biên
- **Lưu cấu hình init/contact ModelState invalid** → trả lỗi, không lưu.
- **Cấu hình giảm giá thiếu trường** (`GiaTriTu/GiaTriDen/GiamGiaGiaTri/GiamGiaKieuLoai`) → "Vui lòng nhập đầy đủ thông tin".
- **`GiaTriTu >= GiaTriDen`** → "Giá trị từ phải nhỏ hơn giá trị đến".
- **PhanTram & GiamGiaGiaTri > 100** → "Giảm giá kiểu loại không được > 100%".
- **Tien & GiamGiaGiaTri >= GiaTriTu** → "Giá trị giảm giá phải nhỏ hơn giá trị từ.".
- **Khoảng giá chồng lấn** (giao nhau với cấu hình khác cùng đơn vị+chi nhánh) → "Cấu hình giá trị đã được thiết lập" (Update loại trừ chính nó). *(Lưu ý: kiểm tra overlap hiện không phủ hết trường hợp lồng nhau — xem dev.md.)*
- **Thanh toán với tổng đơn nhỏ hơn mọi khoảng** → `GetConfigOrderByPrice` trả null → không giảm.
- **Lưu kho số chi nhánh** → kéo theo re-sync cấu hình kho số đơn vị (1 thao tác chạm 2 phạm vi).
- **Ma trận sửa số: đơn vị chưa cấu hình** *(bổ sung Pha 2 — CauHinhSuaSo)* → `GetCauHinhInit` trả lỗi "Lỗi lấy dữ liệu" thay vì list rỗng (FE cần phân biệt — xem dev.md).
- **Ma trận sửa số: lưu UnitCode (field) mới cho đơn vị ĐÃ có cấu hình** *(bổ sung Pha 2)* → nhánh update chỉ update UnitCode đã tồn tại; field mới gửi kèm **không được insert** → ma trận thiếu cột nếu enum mở rộng sau khi đơn vị đã lưu (xem dev.md).
- **Ma trận sửa số: lưu lỗi (exception)** *(bổ sung Pha 2)* → catch trả message tĩnh "Lỗi lưu cấu hình", không log chi tiết (xem dev.md).

## Ví dụ cụ thể có số
Đơn vị E, chi nhánh HN. Admin cấu hình giảm giá theo đơn:
- Dòng 1: từ 500.000 đến 1.000.000 → giảm 5% (PhanTram). Lưu OK.
- Dòng 2: từ 1.000.000 đến 2.000.000 → giảm 100.000đ (Tien, 100.000 < 1.000.000 hợp lệ). Lưu OK.
- Thêm dòng 3: từ 800.000 đến 1.200.000 → giao với dòng 1 và 2 → "Cấu hình giá trị đã được thiết lập", không lưu.
- Khách đặt đơn 1.500.000đ → thanh toán → `GetConfigOrderByPrice(1.500.000)` rơi vào dòng 2 → giảm 100.000đ.
- Khách đặt đơn 300.000đ → nhỏ hơn mọi khoảng → không giảm.
- Admin chỉnh kho số cho chi nhánh HN → cấu hình kho số cấp đơn vị E tự cập nhật lại theo các chi nhánh active.

**Ma trận quyền sửa field contact** *(bổ sung Pha 2 — CauHinhSuaSo)* — đơn vị E lần đầu cấu hình:
- Mở màn → `GetCauHinhInit` trả "Lỗi lấy dữ liệu" (đơn vị E chưa có bản ghi nào).
- Admin tích: field "Số điện thoại" (UnitCodeId 1) → role [Sale, QLDV]; field "Nguồn" (UnitCodeId 2) → role [QLDV]. Bấm Lưu → `SaveCauHinhInit` insert mới 2 bản ghi (EnumUnitCode set theo enum), clear cache → "Lưu cấu hình thành công".
- Lần sau mở lại → đọc từ cache, ra đúng 2 dòng trên.
- Đổi field "Số điện thoại" thành role [QLDV] rồi Lưu → đơn vị E đã có cấu hình → nhánh update chỉ đổi `Roles` của UnitCodeId 1 (UnitCodeId 2 giữ nguyên), clear cache.
- Nếu request kèm thêm field mới UnitCodeId 3 (vừa thêm enum) cho đơn vị E đã có cấu hình → field 3 **không được lưu** (nhánh update bỏ qua UnitCode chưa tồn tại trong DB) → ma trận thiếu cột 3.

## Hậu điều kiện
- **Lưu cấu hình** → DB cập nhật theo đơn vị/chi nhánh; cấu hình giảm giá clear cache Redis theo chi nhánh; nhiều cấu hình ghi lịch sử thay đổi.
- **Cấu hình kho số chi nhánh** → đồng bộ lên cấu hình đơn vị.
- **Cấu hình đơn hàng** → đồng bộ chéo Contact/OrderLogistic/Invoice; cờ kho tổng/care đơn được phần phân bổ đơn đọc lại.
- **Cấu hình giảm giá** → áp tự động khi thanh toán đơn về sau.
- **Ma trận quyền sửa field contact** *(bổ sung Pha 2 — CauHinhSuaSo)* → DB cập nhật/insert `CauHinhSuaSo` theo đơn vị; cache `CauHinhSuaSoSetting` đơn vị được clear; lần đọc kế tiếp rebuild cache từ DB.
