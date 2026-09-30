# Quản lý sản phẩm — Luồng

> Tóm tắt: [summarize.md](./summarize.md) · FAQ: [faq.md](./faq.md) · Module: [../README.md](../README.md)

## Tiền điều kiện
- Đã đăng nhập, có quyền thao tác (thêm/sửa/xóa cần quyền quản trị tổng — nhóm `DV-AD-TONG`).
- Đã có ít nhất **1 chi nhánh (khu vực kho hàng)**.
- Đã tạo **Danh mục / Nhóm** sản phẩm (hoặc nhóm dịch vụ) — vì khi thêm SP/DV bắt buộc chọn nhóm.

---

## Luồng dùng

### A. Tạo sản phẩm (hàng hóa)
1. Vào **Kho → Quản lý sản phẩm** → bấm **Thêm mới**.
2. Nhập **Tên**, chọn **Ảnh**, chọn **Nhóm sản phẩm**, nhập **Giá bán** (và VAT nếu có).
3. Chọn **Khu vực kho hàng (chi nhánh)** áp dụng.
4. (Tùy chọn) Khai báo: Barcode, giá vốn, **đơn vị tính + bảng quy đổi**, thuộc tính (màu/size → sinh ra SP con), IMEI **hoặc** Lô-date, sản phẩm cấu thành.
5. Bấm **Lưu** → hệ thống kiểm tra trùng Mã/Tên/Barcode → tạo sản phẩm + đánh chỉ mục tìm kiếm + thông báo tới chi nhánh.
- **Kết quả:** sản phẩm xuất hiện trong danh mục, sẵn sàng để nhập kho & bán (sau khi bật "Sử dụng").

### B. Tạo dịch vụ (spa/salon)
1. Vào **Kho → Quản lý dịch vụ** → **Thêm mới**.
2. Nhập **Tên**, **Ảnh**, **Nhóm dịch vụ**, **Giá dịch vụ**, **Thời gian phục vụ (phút)**, chọn **chi nhánh**.
3. (Tùy chọn spa) Bật **Tính giờ** / **Tính theo block** (nhập số phút block + giá block); đặt **giá theo khung giờ** theo ngày trong tuần; gắn sản phẩm tiêu hao kèm dịch vụ.
4. **Lưu** → kiểm tra trùng Tên → tạo dịch vụ + giá theo chi nhánh.

### C. Tạo combo
1. Vào **Kho → Quản lý Combo** → **Thêm mới** (`combo/them/...`).
2. Nhập tên, ảnh; chọn **các dịch vụ và/hoặc sản phẩm** thành phần.
3. Chọn **chi nhánh** áp dụng — hệ thống kiểm tra mọi SP/DV trong combo **phải thuộc các chi nhánh đó**.
4. Đặt **giá gói / chiết khấu / VAT**; (tùy chọn) **giới hạn số lượng bán**, **hạn sử dụng**.
5. **Lưu** → kiểm tra trùng tên → tạo combo + danh sách DV/SP + gán chi nhánh.

### D. Tạo thẻ dịch vụ
1. Vào **Kho → Quản lý thẻ** → **Thêm mới**.
2. Chọn **Loại thẻ**:
   - **Thẻ số tiền:** nhập **giá bán** + **giá trị tiền trong thẻ** + thời hạn.
   - **Thẻ số lần:** nhập **giá bán** + **số lần sử dụng (> 0)** + thời hạn.
   - **Thẻ lịch:** loại đặc biệt, mỗi đơn vị chỉ 1 thẻ — tự set không giới hạn & bỏ thời hạn.
3. Chọn **chi nhánh** áp dụng (>= 1; thẻ lịch không chọn = áp tất cả).
4. **Lưu**.

### E. Danh mục / Nhãn
- **Danh mục sản phẩm:** thêm/sửa/xóa nhóm để gom SP/DV. Tên nhóm không trùng.
- **Nhãn sản phẩm:** thêm tag + màu, sắp xếp theo thứ tự (Stt); thường chỉ người tạo (hoặc admin tổng) sửa/xóa được.

---

## Case biên
| Tình huống | Kết quả |
|---|---|
| Thiếu Tên / Ảnh / Giá / Nhóm / Chi nhánh | Chặn lưu, báo lỗi trường thiếu |
| Mã / Tên / Barcode trùng | Chặn lưu: "đã tồn tại trong hệ thống" |
| Tick cả IMEI và Lô-date | Chặn: "chỉ được chọn là sản phẩm imei hoặc sản phẩm lo date" |
| Đổi đơn vị tính khi SP đã có tồn / đã bán | Chặn: "đang có tồn kho / đã được bán... không thể thay đổi đơn vị tính" |
| Xóa SP đang dùng trong combo / DV / SP cấu thành | Chặn, liệt kê nơi đang dùng |
| Combo: SP/DV không thuộc chi nhánh đã chọn | Chặn: "có chi nhánh không cung cấp sản phẩm/dịch vụ đã chọn" |
| Xóa combo đã bán cho khách | Chặn: "combo này đã bán cho khách hàng, không thể xóa" |
| Thẻ số lần nhập số lần = 0/trống | Chặn: "số lần sử dụng... phải lớn hơn 0" |
| Tạo thẻ lịch thứ 2 | Chặn: "thẻ lịch đã tồn tại, không thể tạo thẻ thứ 2 cùng loại" |
| Dịch vụ: 2 khung giờ đè nhau | Chặn: "thời gian dòng X đè lên thời gian dòng Y" |

---

## Ví dụ cụ thể có số
- **Sản phẩm:** "Serum HA 30ml", giá **350.000đ**, VAT **8%** → hệ thống tự tính **giá sau VAT = 378.000đ**, gán nhóm "Mỹ phẩm", chi nhánh "CN Quận 1". Bật Sử dụng → bán được sau khi nhập kho.
- **Combo:** gói gồm DV "Chăm sóc da" + SP "Mặt nạ" thành phần tổng **800.000đ**, đặt **giá gói 650.000đ**, giới hạn **100 suất**, hạn dùng 90 ngày.
- **Thẻ số lần:** "Thẻ Massage 10 buổi", giá bán **3.000.000đ**, **số lần = 10**, thời hạn **6 tháng** → khách mua, mỗi buổi trừ 1 lần.

---

## Hậu điều kiện
- SP/DV/Combo/Thẻ mới nằm trong danh mục, có thể nhập kho (SP) và bán (đơn quầy / online / đặt lịch).
- Dữ liệu được đánh chỉ mục tìm kiếm; cache theo đơn vị/chi nhánh được làm mới; chi nhánh nhận thông báo có hàng/dịch vụ mới.
- Tồn kho thực tế vẫn = 0 cho tới khi có **phiếu nhập** (xem [../quan-ly-ton-kho/](../quan-ly-ton-kho/)).
