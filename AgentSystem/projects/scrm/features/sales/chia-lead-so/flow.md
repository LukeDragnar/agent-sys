# Luồng — Chia Lead / số

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc đơn vị + chi nhánh.
- Có sẵn dữ liệu nền: nguồn dữ liệu (landing), phòng ban / nhóm nhân viên / nhân viên sale-cskh, sản phẩm, ca làm việc (nếu dùng làm điều kiện chia).
- Có lead/contact **chưa phân bổ** để chia.
- Quyền: phần lớn thao tác cần cụm role admin/quản lý; "Bật/tắt giới hạn check trùng số" chỉ HT-CSKH.

## Luồng cấu hình (admin)
**A. Chia theo nguồn (CSTN):**
1. Mở tab **Chia số theo nguồn**, load setting hiện có (`CHCS_CSTN_CauHinhChiaSoSetting_Get`).
2. Chọn nguồn dữ liệu, gán kiểu chia mặc định theo chi nhánh → **Lưu** (`CHCS_CSTN_CauHinhChiaSoSetting_Save`).

**B. Chia tự động (CHCS):**
1. Mở tab **Chia số tự động**, list cấu hình (`CHCS_TimTheoDieuKien`).
2. Thêm/sửa: đặt **Tên**, chọn nhóm sale/cskh → nhân viên, đặt **định mức** (`DinhMucChiaSo`) mỗi node, chọn **sản phẩm** (cụ thể / tất cả / không chọn) và **ca làm việc** → **Lưu** (`CauHinhChiaSo_Save`).
3. Hệ thống validate (tên không rỗng, không trùng tên trong đơn vị+chi nhánh) → tạo master `CauHinhChiaSo` + cây `CauHinhChiaSoDoiTuong`/`CauHinhChiaSoLienKet` → set lại cache.
4. Xóa cấu hình (`CHCS_Xoa/{idCHCS}`) khi không dùng.

## Luồng dùng (phân bổ thủ công + bật/tắt nhận lead)
**C. Chia thủ công (CSTC):**
1. Mở tab **Chia số thủ công**; load danh sách contact / nguồn **chưa phân bổ**.
2. Chọn đích (nhóm hoặc nhân viên) → **Áp dụng phân bổ** (theo định mức) hoặc **Phân bổ ngay** (gán lead cụ thể).
3. Hệ thống update `Contact.SaleUserId/IdNhomNhanVien` (`PhanBoTuDau`), ghi log tác nghiệp.

**D. Bật/tắt nhận lead:**
1. Tab Bật/tắt: list sales (`CHCS_LayDSSales`) hoặc nhóm sale-cskh (`CHCS_GetDSNhomSaleNhanData`).
2. Toggle nhận data cho 1/nhiều tài khoản (`BatTatNhanDuLieuChoTaiKhoan` / `...ChoNhieuTaiKhoan` / `...Username`) hoặc cả nhóm (`CHCS_BatTatNhomSaleNhanData`).

**E. (HT-CSKH) Bật/tắt giới hạn check trùng số:**
1. Chọn danh sách CHCS → set `ApDungCheckGioiHanSo` → remove + set lại cache từng cấu hình.

## Case biên
- **Lưu cấu hình tự động tên rỗng** → "Tên không được để trống!".
- **Trùng tên cấu hình** trong cùng đơn vị+chi nhánh (khác Id) → lỗi `MESS_Model_EXISTS`.
- **`req.Id == null`** → tạo mới; có Id → cập nhật.
- **Chọn sản phẩm**: có `IdSanPham` → `ChonSanPhamCuThe`; rỗng + `IsChonTatCa` → `ChonTatCa`; ngược lại → `KhongChon`.
- **Bật/tắt giới hạn check trùng với danh sách CHCS rỗng/không thuộc đơn vị** → "Không tìm thấy cấu hình chia số nào để cập nhật".
- **Phân bổ thủ công khi không còn contact chưa phân bổ** → không có gì để chia.
- **Sale tắt nhận data hoặc đủ định mức** → không nằm trong tập đích nhận lead khi chia tự động.

## Ví dụ cụ thể có số
Đơn vị D, chi nhánh HCM. Nhóm Sale1 có 3 NV (An, Bình, Châu).
- Admin tạo cấu hình chia tự động "Lead Facebook HCM": nhóm Sale1, định mức mỗi NV = 20 lead/ngày, sản phẩm = Chọn tất cả, ca sáng. Lưu OK.
- Admin tạo cấu hình thứ 2 cùng tên "Lead Facebook HCM" → trùng tên trong cùng đơn vị+chi nhánh → lỗi `MESS_Model_EXISTS`.
- Có 50 lead Facebook chưa chia. Admin vào tab thủ công, chọn nguồn Facebook, đích nhóm Sale1, bấm "Áp dụng phân bổ" → hệ thống chia theo định mức 20/người: An 20, Bình 20, Châu 10 (do hết lead) → 50 lead có chủ.
- Châu tạm nghỉ → admin tắt nhận data cho Châu → lượt chia tự động sau chỉ chia cho An + Bình.

## Hậu điều kiện
- **Lưu cấu hình** → DB cập nhật + cache CHCS set lại (đồng bộ DB).
- **Phân bổ lead** → Contact đổi chủ (`PhanBoTuDau`), log tác nghiệp; ảnh hưởng kho số + đơn hàng của contact.
- **Bật/tắt nhận data** → cờ nhận lead của tài khoản/nhóm thay đổi → ảnh hưởng lượt chia tự động về sau.
- Runtime auto-chia (`ChiaSoV2Service`/`ChiaSoTheoNhomService`/`ChiaSoPUSHSALEService`) đọc cấu hình mới khi có lead vào.
