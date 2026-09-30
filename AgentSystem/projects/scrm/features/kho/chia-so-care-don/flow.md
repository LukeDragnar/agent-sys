# Flow — Chia số care đơn

## Tiền điều kiện
- Đơn vị đã **bật tính năng** chia số care đơn (`CauHinhSetting.IsSuDungChiaSoCareDon = true`).
- Đã có **cấu hình care đơn** cho chi nhánh (hoặc chi nhánh tổng nếu dùng kho tổng).
- Đã cấu hình **trạng thái giao hàng nào thì chia** (mỗi trạng thái có cờ `ChiaSoCareDon` và `SoPhutCauHinhChiaSau`).
- Có **nhóm nhân viên kho** (role `DV-QL-KHO`) và tài khoản care đơn; tài khoản phải đang **bật nhận dữ liệu** (`User.IsCareDon = true`) mới được nhận đơn.
- Nếu dùng **kho tổng**: phải khai báo **chi nhánh tổng** (`IdChiNhanhTong`), nếu không sẽ dừng và ghi log lỗi.
- Người thao tác cấu hình/chia thủ công có quyền tương ứng (xem `quyen` trong summarize).

---

## Luồng dùng — Chia tự động (hệ thống tự giao)
Kích hoạt khi đơn đạt trạng thái giao hàng được cấu hình (đi qua RabbitMQ consumer hoặc job nền).

1. **Kiểm tra đơn:** nạp lại đơn từ DB. Nếu đơn không còn, hoặc **đã có nhóm care** (`IdNhomCareDon`), hoặc **đã có người care** (`UserIdCareDon`) → **bỏ qua** (mỗi đơn chỉ chia một lần).
2. **Kiểm tra cấu hình đơn vị:** chưa bật tính năng / chưa có cấu hình → bỏ qua. Bật kho tổng nhưng thiếu chi nhánh tổng → dừng + ghi log lỗi.
3. **Kiểm tra trạng thái:** lấy danh sách trạng thái có cờ chia. Nếu trạng thái hiện tại của đơn không nằm trong đó → bỏ qua.
4. **Chia ngay hay chia trễ:** nếu trạng thái có `SoPhutCauHinhChiaSau > 0` và lượt này đến từ realtime (không phải job) → tạo bản ghi **QueueDonHangChiaCareDon** với `NgayThucHienChia = now + X phút`, rồi dừng. Job nền quét hàng đợi này tới hạn sẽ gọi lại để chia thật.
5. **Xác định chi nhánh áp dụng cấu hình:** kho tổng → dùng cấu hình chi nhánh tổng và gom user toàn bộ chi nhánh; ngược lại → dùng cấu hình + user của chi nhánh đơn.
6. **Khởi tạo bộ đếm theo ngày** (counter theo đơn vị / chi nhánh / cấu hình / ngày).
7. **Chia theo kiểu cấu hình:**
   - **Kiểu chia về nhóm (Option1):** trước hết chia **về nhóm** care đơn:
     - Nếu nhóm sale của đơn có **liên kết** với nhóm care đơn (và khớp điều kiện sản phẩm) → chọn trong các nhóm liên kết.
     - Ngược lại, nếu bật "chia theo nhóm tự động" → chọn trong các nhóm cấu hình (lọc theo sản phẩm nếu bật chia theo sản phẩm).
     - Trong các ứng viên, chọn nhóm **counter "xa nhất"** (ít đơn nhất / lâu chưa nhận nhất) còn dưới định mức. Gán nhóm lên đơn, tăng counter.
     - Sau khi có nhóm, nếu bật "chia theo user tự động" → tiếp tục **chia về một user** trong nhóm theo cùng nguyên tắc.
   - **Kiểu chia trực tiếp về tài khoản (Option2):** đơn của nhóm sale nào → giao thẳng cho **tài khoản care đơn liên kết** với nhóm sale đó.
8. **Khóa tương tranh:** bước chọn-counter-rồi-tăng được bao bằng **Redis lock** để hai đơn cùng lúc không nhận lệch định mức.
9. **Ghi log chia số** lên đơn (lịch sử "đơn này đã được chia thế nào").

> Nguyên tắc cốt lõi: ưu tiên đối tượng **ít đơn nhất**, tôn trọng **định mức ngày**, tôn trọng **liên kết nhóm sale** và **điều kiện sản phẩm**, và **không chia lại** đơn đã có người/nhóm.

---

## Luồng dùng — Chia thủ công (màn Chia số care đơn thủ công)
1. Mở màn, hệ thống gọi `GetDSAllDonHangLGTChuaPhanBo` → chạy store `SCRM_CauHinhChiaCareDon_GetDSAllDonHangLGTByNhom` trả về **đơn chưa phân bổ** (lọc theo từ khóa, khoảng ngày, trạng thái phân bổ Đã chia / Chưa chia, đơn của sale đã nghỉ việc…). Trả kèm tổng số đơn và tổng số đơn chưa phân bổ.
2. Quản lý chọn đơn cần chia.
3. **Phân về nhóm:** `ChiaNhomCareDon_PhanBoNgay` / `ChiaNhomCareDon_PhanBoVeNhomUser` — gắn nhóm care đơn cho đơn.
4. **Phân về người:** chọn nhóm → `GetDSNhanVienKhoByNhom` lấy danh sách user trong nhóm → `ChiaUserCareDon_PhanBoNgay` / `ChiaUserCareDon_PhanBoVeUser` gắn người care lên đơn.
5. Số điện thoại khách trong danh sách có thể bị **che** tùy cấu hình che số của tài khoản đang xem.

---

## Luồng cấu hình
1. Mở **Cấu hình care đơn** → `GetDetail` (`CauHinhChiaSoCareDon_GetDetail`): nếu chi nhánh chưa có cấu hình, trả **template rỗng** mặc định kiểu Option1; nếu đã có, trả cấu hình kèm số lượng nhân viên mỗi nhóm, danh sách sản phẩm đã chọn, cấu hình liên kết…
2. **Lưu kiểu chia về nhóm:** `Save_Kieu1` — lưu cấu hình nhóm/người, định mức, cờ chia tự động, cờ + danh sách sản phẩm.
3. **Đổi kiểu chia:** `ChangeTypeCHCSCareDon` chuyển giữa Option1 (về nhóm) và Option2 (trực tiếp tài khoản).
4. **Kiểu trực tiếp tài khoản:** `Save_Kieu2_CareDonLienKet` / `Update_Kieu2_CareDonLienKet` thiết lập tài khoản care đơn liên kết với (các) nhóm sale; bật/tắt nhận đơn theo từng liên kết (`BatTatUserCareDonNhomSale`) hoặc tất cả (`BatTatUserCareDonAllNhomSale`).
5. Mọi thay đổi cấu hình ghi vào **lịch sử thay đổi cấu hình** (`LichSuThayDoiCauHinhChiaSo`).

**Bật/tắt nhận dữ liệu (màn riêng):**
1. `CHCS_LayDSUserCareDon` (`ChiaSoCareDonController_CHCS_LayDSUserCareDon`) — liệt kê tài khoản kho care đơn (lọc theo phòng ban/nhóm/user theo quyền, từ khóa, trạng thái nhận dữ liệu), có phân trang.
2. Gạt bật/tắt một tài khoản → `BatTatNhanDuLieuChoTaiKhoanCareDon` cập nhật `User.IsCareDon`, đồng bộ user, ghi **lịch sử tác động**.

---

## Case biên
- **Đơn đã có nhóm/người care:** chia tự động bỏ qua hoàn toàn, không ghi đè.
- **Trạng thái không cấu hình chia:** đơn không được chia dù tính năng đang bật.
- **Đặt chia sau X phút:** đơn vào hàng đợi, nếu trong X phút đó được chia bằng cách khác / đã có người → tới lượt job xử lý sẽ thấy đã có người và bỏ qua.
- **Tất cả nhóm/người đủ định mức ngày:** không còn ứng viên → đơn không được chia tự động cho tới ngày mới (cần chia thủ công).
- **Tài khoản bị tắt nhận dữ liệu:** không nằm trong ứng viên nhận đơn.
- **Bật kho tổng nhưng thiếu chi nhánh tổng:** dừng + ghi log lỗi rõ ràng.
- **Không có quyền thao tác tài khoản** (bật/tắt nhận dữ liệu user ngoài phạm vi quyền): trả lỗi "không có quyền thao tác".
- **Đơn không xác định được chi nhánh:** ghi log lỗi, không chia.
- **Tương tranh hai đơn cùng lúc:** Redis lock đảm bảo counter không bị cộng lệch.

---

## Ví dụ có số
Cấu hình kiểu về nhóm, bật chia tự động theo nhóm, **không** chia theo sản phẩm. Có 2 nhóm kho, định mức mỗi nhóm 50 đơn/ngày. Đầu ngày cả 2 nhóm counter = 0.

- Đơn #1 đạt trạng thái "đã đóng gói" (trạng thái có cờ chia, chia ngay). Cả 2 nhóm = 0 đơn → chọn nhóm "xa nhất" (ví dụ Nhóm A). Gán Nhóm A. Counter A = 1.
- Đơn #2 tới: Nhóm A = 1, Nhóm B = 0 → B ít hơn → gán Nhóm B. Counter B = 1.
- Đơn #3 tới: A = 1, B = 1 → chọn nhóm lâu chưa nhận nhất → Nhóm A. Counter A = 2.
- ... cứ thế luân phiên cho tới khi một nhóm chạm 50 đơn → nhóm đó bị loại, đơn còn lại dồn về nhóm kia tới khi cũng đủ 50.
- Khi cả hai đủ 50 (tổng 100 đơn/ngày), đơn #101 **không được chia tự động** → phải vào màn chia thủ công.

Nếu trạng thái đặt **chia sau 30 phút**: đơn #1 đạt trạng thái lúc 09:00 → tạo hàng đợi `NgayThucHienChia = 09:30`. Job nền quét và chia thật lúc ~09:30 (nếu lúc đó đơn vẫn chưa có người care).

---

## Hậu điều kiện
- Đơn được gắn **nhóm care đơn** và/hoặc **người care đơn**.
- **Bộ đếm theo ngày** của nhóm/người tăng tương ứng.
- **Lịch sử chia số** của đơn được cập nhật.
- Đơn đã chia sẽ **không bị chia lại** ở các lần đổi trạng thái sau.
- Thay đổi cấu hình / bật-tắt nhận dữ liệu được ghi vào **lịch sử thay đổi / lịch sử tác động**.
