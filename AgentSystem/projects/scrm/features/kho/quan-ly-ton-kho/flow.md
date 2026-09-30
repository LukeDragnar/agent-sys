# Luồng — Quản lý tồn kho

> Mô tả luồng dùng + cấu hình cho người vận hành / QA / đào tạo. Số liệu ví dụ là giả lập.

## Tiền điều kiện
- Đã có **chi nhánh** và ít nhất 1 **kho** thuộc chi nhánh đó (xem màn Danh sách kho).
- Đã có **sản phẩm** và **nhóm sản phẩm** trong danh mục.
- Tồn kho đã được hình thành qua phiếu nhập đã xác nhận (nếu chưa nhập gì → tồn = 0, vẫn xem được).
- Người dùng có quyền xem tồn kho (Admin tổng/CN/MCN, Kế toán, Quản lý dịch vụ, Quản kho; Sale chỉ thấy kho không bị ẩn).

---

## Luồng dùng — Xem tồn kho theo kho (màn Quản lý tồn kho)
1. Vào **Kho → Quản lý tồn kho**.
2. Chọn **Kho** (hoặc để trống = tất cả kho được phép) và **Nhóm sản phẩm** (bắt buộc — không chọn thì bảng trống).
3. (Tùy chọn) Gõ từ khóa: tìm theo **tên sản phẩm / mã sản phẩm / mã vạch**.
4. Hệ thống trả bảng phân trang, mỗi dòng 1 sản phẩm trong kho với: Số lượng tồn, Đang chờ xuất, Có thể bán (= tồn − chờ xuất), Giá nhập trung bình (nếu không bị ẩn).
5. (Tùy chọn) Bấm **Xuất Excel** để tải file.

## Luồng dùng — Kiểm tra tồn 1 sản phẩm trên toàn chi nhánh (màn Kiểm tra tồn kho)
1. Vào **Kho → Kiểm tra tồn kho**.
2. Chọn **sản phẩm** (bắt buộc), tùy chọn lọc theo chi nhánh / kho.
3. Hệ thống liệt kê tồn của sản phẩm đó theo từng chi nhánh.
4. Nếu đơn vị bật **giới hạn tồn kho tự động** và chọn xem **dự báo hết hàng** → chọn thêm **kiểu khoảng ngày**, hệ thống tính số ngày còn đủ hàng dựa trên tốc độ bán.

---

## Luồng cấu hình (admin)

### Danh sách kho (thêm/sửa kho)
1. Vào **Kho → Danh sách kho** → **Thêm mới** hoặc **Cập nhật**.
2. Thiết lập: tên kho, **kho mặc định** của chi nhánh, **cho phép chốt âm**, **ẩn kho với sale**, danh sách **quản kho**, kho dùng cho giao hàng...
3. Lưu. Nếu đặt 1 kho làm **mặc định** → các kho khác cùng chi nhánh tự bị bỏ cờ mặc định (chỉ 1 kho mặc định / chi nhánh).

### Cấu hình kho (cấp đơn vị)
- Vào màn **Cấu hình kho**: thiết lập giới hạn tồn kho, ẩn/che giá nhập theo vai trò, bật giới hạn tồn kho tự động (phục vụ dự báo hết hàng), cấu hình quản lý sản phẩm.

### Cấu hình kho chi nhánh
- Vào màn **Cấu hình kho chi nhánh**: tạo/cập nhật cấu hình riêng cho từng chi nhánh (override cấu hình chung khi cần).

---

## Case biên
- **Không chọn nhóm sản phẩm** → bảng trả về rỗng (table1 = [], total = 0), không báo lỗi.
- **Từ khóa có khoảng trắng thừa** → hệ thống tự trim; rỗng → coi như không lọc.
- **Người dùng không cùng chi nhánh tổng** → cột Giá nhập trung bình bị **ẩn**.
- **Vai trò nằm trong danh sách bị che giá nhập** (và không phải admin/CSKH) → ẩn giá nhập.
- **Kho bật chốt âm** → vẫn cho bán khi tồn không đủ, tồn xuống âm.
- **Sản phẩm cấu thành** → giá nhập trung bình của SP cha được tính lại = tổng (giá vốn SP con × định lượng), không lấy trực tiếp.
- **Xuất Excel báo cáo tồn theo đơn vị** → bắt buộc nhập vị trí bắt đầu (OffSet ≥ 0) và số lượng (PageSize > 0, ≤ 10.000), sai → báo lỗi không cho xuất.
- **Dự báo hết hàng nhưng chưa chọn kiểu khoảng ngày** → báo lỗi "Vui lòng chọn kiểu khoảng ngày...".

---

## Ví dụ có số
Sản phẩm "Kem chống nắng A" tại **Kho HCM**:
- Số lượng tồn: **50**
- Đang chờ xuất (đơn chưa hoàn thành): **8**
- → **Có thể bán = 50 − 8 = 42**

Nhập thêm lô 30 cái giá 80.000đ, tồn cũ 50 cái giá vốn 100.000đ:
- Tồn mới = 50 + 30 = **80**
- Giá nhập trung bình mới = (50×100.000 + 30×80.000) / 80 = (5.000.000 + 2.400.000)/80 = **92.500đ**

Kiểm tra tồn toàn chi nhánh "Kem chống nắng A":
- HCM: 80 · HN: 12 · ĐN: 0 → tổng toàn hệ thống 92.

---

## Hậu điều kiện
- Màn tồn kho chỉ **đọc** — không thay đổi dữ liệu kho.
- Con số tồn chỉ thay đổi khi có phiếu nhập/xuất/chuyển/kiểm kho được **xác nhận**, hoặc đơn hàng xuất kho.
- Thay đổi cấu hình kho có hiệu lực ngay cho các thao tác tra cứu/bán hàng sau đó.
