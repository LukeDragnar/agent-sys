# Phân loại & đánh nhãn khách hàng — Luồng

> Tóm tắt: [summarize.md](./summarize.md) · Hỏi đáp: [faq.md](./faq.md) · Module: [../README.md](../README.md)

## Tiền điều kiện
- Đã đăng nhập, có đơn vị (IdDonVi) — mọi nhãn/nhóm/hạng đều theo từng đơn vị.
- Có sẵn hồ sơ khách hàng để gắn nhãn / gán nhóm.
- (Để gán nhóm có điều kiện / xét nâng hạng) khách phải có **điểm tích lũy / điểm hiện tại** từ phần Điểm thưởng.
- (Khai báo danh mục nhóm/hạng) cần quyền **admin tổng (DV-AD-TONG)**.

## Luồng dùng (nhân viên)

### A. Gắn nhãn khách hàng
1. Vào màn **Nhãn khách hàng** → thêm nhãn: nhập **Tên** + chọn **màu nền**, đặt **thứ tự (Stt)** để sắp hiển thị.
2. Vào hồ sơ khách → dán một hoặc nhiều nhãn cho khách.
3. Danh sách nhãn sắp theo **Stt** tăng dần; mỗi dòng có cờ **IsEdit** — nếu không phải admin tổng và không phải người tạo nhãn thì không sửa/xóa được nhãn đó.
- **Kết quả mong đợi:** khách có các nhãn màu hiển thị trên hồ sơ; lọc/nhận diện nhanh.

### B. Gán nhóm cho khách
1. Ở hồ sơ khách → chọn **gán nhóm** (CreateSingle) → chọn nhóm từ danh mục.
2. Hệ thống kiểm tra: khách **đã có nhóm chưa** → nếu rồi, chặn tạo, chỉ cho **sửa (Update)** sang nhóm khác.
3. Nếu là **nhóm có điều kiện** → so `GiaTri` (điểm yêu cầu) với `SoDiemHienTai` của khách; thiếu điểm → chặn.
4. Lưu xong → ghi **lịch sử nhóm (Mongo)** + **lịch sử tương tác khách hàng** (đổi nhóm).
- **Kết quả mong đợi:** khách thuộc đúng 1 nhóm; mức giảm giá của nhóm sẵn sàng áp khi lên đơn.

### C. Xem danh sách nhóm / hạng
1. **Danh sách nhóm khách hàng** (`KhachHangNhomKhachHang_TimTheoDieuKien`): lọc theo **từ khóa** (tên/mã/SĐT khách) + **IdNhomKhachHang** + chi nhánh → trả về khách kèm tên nhóm.
2. **Danh sách hạng khách hàng** (`KhachHang_TimKiemHangKhachHangTheoDieuKien`): chỉ liệt kê khách **đã có hạng** (`IdHangKhachHang != null`), lọc theo từ khóa + **IdHangKhachHang** + chi nhánh.

## Luồng cấu hình (admin tổng)

### Danh mục nhóm KH
1. **Khởi tạo** (`KhoiTaoMacDinh`): clone các nhóm mẫu từ template hệ thống (IdDonVi = Guid.Empty, IsSuDung = true) về đơn vị; **bỏ qua nhóm trùng tên** đã có.
2. **Thêm mới** (`DMNhomKhachHang_ThemMoi_DV`): nhập Tên + Đơn vị giảm giá + Giá trị giảm giá + Kiểu loại (có/không điều kiện).
   - Validate: tên không trống, không trùng (so không phân biệt hoa thường), đơn vị giảm giá hợp lệ, giảm theo % thì ≤ 100.
   - Nếu **không điều kiện** → bỏ trống GiaTri/LoaiDieuKien; nếu **có điều kiện** → LoaiDieuKien = TheoGiaTri.
   - Mặc định: LevelUuTien = 1, IsChoPhepTuDongXetNangNhom = false.
3. **Bật/tắt sử dụng** (`ChangeSuDung_DV`), **bật/tắt tự xét nâng nhóm khi thanh toán** (`ChangeChoPhepXetNangNhom_DV`).

### Danh mục hạng KH
1. **Khởi tạo** (`KhoiTao_DV`): clone hạng mẫu từ template (IdDonVi = Guid.Empty) về đơn vị; bỏ qua hạng trùng tên.
2. **Thêm mới** (`DMHangKhachHang_ThemMoi_DV`): nhập Tên + Điểm tích lũy (ngưỡng) + Đơn vị giảm (Type) + Giá trị giảm (PriceDiscount) + Màu (Color).
   - Validate lần lượt: Tên, PriceTichLuy, Type, PriceDiscount, Color đều bắt buộc; tên không trùng trong đơn vị.
   - Cơ chế xếp hạng ép mặc định = **DiemTichLuy**.
3. Lưu xong → xóa cache hạng theo đơn vị.

## Case biên
| Tình huống | Kết quả |
|---|---|
| Xóa nhãn không phải mình tạo (không phải admin tổng) | "Bạn không có quyền xóa nhãn này" |
| Xóa nhãn không tồn tại | "Bạn không tìm thấy bản ghi" |
| Gán nhóm cho khách đã có nhóm | "Khách hàng này đã có nhóm khách hàng chỉ có thể sửa, không thể tạo!" |
| Gán nhóm có điều kiện nhưng khách thiếu điểm | "Khách hàng chưa đủ điều kiện vào nhóm này!" |
| Thêm nhóm trùng tên | "Tên nhóm khách hàng đã tồn tại!" |
| Thêm nhóm giảm % > 100 | "Giá trị giảm giá không được vượt quá 100%" |
| Thêm hạng thiếu điểm tích lũy / màu / giá trị giảm | Báo lỗi tương ứng từng trường ("... không được để trống") |
| Thêm hạng trùng tên | "Tên đã tồn tại trong hệ thống" |
| Danh sách hạng: khách chưa có hạng | Không xuất hiện (chỉ lấy khách có `IdHangKhachHang`) |

## Ví dụ cụ thể có số
- **Nhóm có điều kiện:** nhóm "Khách thân thiết" đặt điều kiện ≥ **500 điểm**. Khách A có `SoDiemHienTai = 320` → gán nhóm bị chặn "chưa đủ điều kiện". Khách B có `SoDiemHienTai = 600` → gán thành công, ghi lịch sử đổi nhóm.
- **Hạng:** khai báo 3 hạng — Đồng (ngưỡng 0 điểm, giảm 0%), Bạc (1.000 điểm, giảm 3%), Vàng (5.000 điểm, giảm 5%). Khách tích lũy đạt **1.200 điểm** → hệ thống xét lên **Bạc**; lên đơn áp chiết khấu 3% theo hạng.
- **Nhãn:** tạo nhãn "VIP" màu `#FFD700` Stt = 1, nhãn "Hay hủy" màu `#FF0000` Stt = 2 → danh sách hiển thị VIP trước, Hay hủy sau.

## Hậu điều kiện
- Nhãn đã dán → hiển thị trên hồ sơ khách; xóa cache nhãn theo đơn vị sau khi thêm/xóa nhãn.
- Nhóm đã gán → khách thuộc đúng 1 nhóm, có bản ghi lịch sử nhóm (Mongo) + lịch sử tương tác; mức giảm của nhóm sẵn sàng áp đơn.
- Danh mục nhóm/hạng đã lưu → dùng để gán/xét cho khách toàn đơn vị; cache hạng theo đơn vị được làm mới.
