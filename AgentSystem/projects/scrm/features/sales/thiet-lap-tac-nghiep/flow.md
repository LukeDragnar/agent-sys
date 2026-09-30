# Luồng — Thiết lập tác nghiệp

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc đơn vị (DonViId) và chi nhánh (IdChiNhanh).
- Để thêm/sửa/xóa/lọc luật: cần role admin (AD-TONG / AD-CN / AD-MCN).
- Đơn vị đã khai báo sẵn 3 danh mục nguồn để chọn vế trong luật: **Danh mục tác nghiệp** (vế NẾU và vế bước tiếp theo), **Danh mục kết quả tác nghiệp** (vế KẾT QUẢ), **Danh mục sau bao lâu** (mốc thời gian). Nếu danh mục trống thì không có gì để chọn.
- Cấu hình đơn vị `IsEnableTacNghiepTheoChiNhanh` quyết định luật gắn theo chi nhánh hay dùng chung đơn vị.

## Luồng dùng (admin thiết lập)
1. Admin mở **Sale → Thiết lập tác nghiệp**.
2. Hệ thống lọc danh sách luật hiện có theo đơn vị (và chi nhánh nếu bật tác nghiệp theo chi nhánh); những vế tham chiếu tới tác nghiệp/kết quả đã bị xóa được gắn nhãn "(Deleted)".
3. Admin bấm **Thêm mới**, chọn: NẾU tác nghiệp → KẾT QUẢ → (tùy chọn) bước tiếp theo → (đi kèm) sau bao lâu.
4. Trước khi lưu, hệ thống gọi **CheckExist** để chống trùng luật (theo cặp NẾU + KẾT QUẢ).
5. Lưu thành công → hệ thống **xóa cache** bộ luật của đơn vị/chi nhánh để lần đọc sau lấy dữ liệu mới.
6. Admin có thể **sửa** (Id bắt buộc) hoặc **xóa** từng luật; mỗi thao tác đều xóa cache.

## Luồng cấu hình (copy bộ luật mẫu)
1. Admin (đơn vị mới) bấm **Copy** để lấy bộ luật mẫu theo ngành nghề.
2. Hệ thống **ép phạm vi copy = đúng đơn vị đang đăng nhập** (bỏ qua mọi danh sách đơn vị gửi lên) → chỉ copy về đơn vị của chính admin.
3. Sau copy, bộ luật xuất hiện trong danh sách để admin chỉnh sửa lại.

## Case biên
- **Chỉ chọn NẾU, chưa chọn KẾT QUẢ (hoặc ngược lại)** → theo thiết kế phải báo lỗi "chưa chọn"; *(hiện ràng buộc này không có hiệu lực chặn — xem dev.md, luật vẫn lưu được)*.
- **Chọn bước tiếp theo nhưng bỏ trống "sau bao lâu" (hoặc ngược lại)** → theo thiết kế phải báo lỗi cặp phải đi cùng nhau; *(hiện cũng không chặn — xem dev.md)*.
- **Thêm luật trùng** (cùng cặp NẾU + KẾT QUẢ đã tồn tại) → CheckExist trả true → báo "Cấu hình tác nghiệp đã tồn tại, không thể lưu cấu hình.", không lưu.
- **Đơn vị bật `IsEnableTacNghiepTheoChiNhanh`** → luật mới gắn `IdChiNhanh` = chi nhánh của admin → chi nhánh khác không thấy. **Tắt** → `IdChiNhanh` null → cả đơn vị dùng chung.
- **Tác nghiệp/kết quả trong luật bị xóa khỏi danh mục** → khi lọc, vế đó gắn "(Deleted)" thay vì biến mất.
- **Copy với payload danh sách đơn vị khác** → bị override về đơn vị hiện tại; không copy chéo được.
- **Sửa luật không truyền Id** → báo lỗi Id bắt buộc, không cập nhật.

## Ví dụ cụ thể có số
Spa B (1 đơn vị, 3 chi nhánh) bật `IsEnableTacNghiepTheoChiNhanh = true`. Danh mục có: tác nghiệp {Gọi điện, Nhắn tin, Hẹn gặp}, kết quả {Khách bận, Hẹn lại, Chốt đơn}, sau bao lâu {2 giờ, 1 ngày, 3 ngày}.
- Admin chi nhánh Q1 thêm luật: NẾU **Gọi điện** + KẾT QUẢ **Khách bận** → THÌ **Gọi điện** SAU **2 giờ**. Lưu OK, luật chỉ áp cho chi nhánh Q1 (`IdChiNhanh = Q1`).
- Admin Q1 thêm tiếp luật y hệt lần 2 → CheckExist thấy trùng cặp (Gọi điện + Khách bận) → báo "Cấu hình tác nghiệp đã tồn tại", không lưu (vẫn còn 1 luật).
- Chi nhánh Q3 mở màn → **không thấy** luật của Q1 (do gắn chi nhánh) → admin Q3 phải tự cài hoặc Copy mẫu.
- Sau đó danh mục kết quả xóa "Khách bận" → luật trên hiển thị "Gọi điện → (Deleted) → Gọi điện sau 2 giờ", nhắc admin sửa lại.

## Hậu điều kiện
- Thêm/Sửa/Xóa/Copy luật ghi vào DB và **xóa cache** bộ luật theo đơn vị + chi nhánh.
- Module **nhắc việc / notification** đọc bộ luật mới → nội dung nhắc bước tiếp theo cho sale thay đổi theo.
- Việc lọc/đọc luật **không** sửa dữ liệu khách hay đơn hàng (chỉ là cấu hình quy trình).
