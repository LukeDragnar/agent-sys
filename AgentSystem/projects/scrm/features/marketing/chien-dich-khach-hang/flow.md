# Luồng — Chiến dịch khách hàng (Tập điều kiện)

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc đơn vị/chi nhánh, có key CRUD tập điều kiện (`CustomerConditionFilterController_*`).
- Đã có dữ liệu khách (hạng, đơn hàng, điểm, nghề nghiệp…) để điều kiện lọc có ý nghĩa.
- (Với kiểu "theo điều kiện") đã có danh mục KH / nhãn sản phẩm để chọn làm tiêu chí.

## Luồng dùng (tạo / sửa tập)
1. Mở **Marketing → Tập điều kiện khách hàng** → `GetAll` / `SearchByCondition` trả danh sách tập theo phạm vi (đơn vị → chi nhánh → người tạo).
2. **Thêm mới** (`Insert`): hệ thống **rẽ nhánh theo `TypeFilter`**:
   - `Normal (1)` hoặc null → `Insert` (lọc theo `ListConditions`).
   - `Phone (2)` → `CreateConditionWithTypeFilterPhone` (tạo từ list SĐT truyền vào).
   - Giá trị khác → trả lỗi "Kiểu loại bộ lọc không phù hợp!".
3. Với kiểu Normal: thêm các `ConditionDetail` (mỗi điều kiện có `Type` + `JsonData`), nối bằng `TypeCodition` (1 = AND, 2 = OR).
4. (Tùy chọn) bật `IsAutoDailyScan` → job quét tự động hằng ngày (`JobScanCustomerInfoWithCondition`).
5. **Sửa** (`Update`): rẽ nhánh giống Insert (Normal → `Update`; Phone → `UpdateConditionWithTypeFilterPhone`).
6. **Xóa** (`Delete/{id}`): xóa tập theo id.

## Luồng xuyên module (tập → chiến dịch gửi)
1. Tập điều kiện được lưu trong MongoDB (`CustomerConditionFilter`, gồm `ListConditions`).
2. **Chiến dịch** (Campaign) tham chiếu `IdCustomerConditionFilter` → biết gửi cho nhóm khách nào.
3. **CustomerInfoFilter** dùng tập làm input để **quét ra tệp khách đã lọc** (`CustomerInfoFiltered`).
4. Khi gửi (SMS/Zalo/voucher), chiến dịch lấy danh sách khách từ tệp đã quét.

## Các loại điều kiện (ConditionDetail.Type)
1 = Điều kiện đơn hàng (kèm nhãn sản phẩm `ListIdNhanSanPham`, gộp từ 02/06/2026) · 2 = Ngày sinh · 3 = Địa chỉ · 4 = Tổng tiền mua hàng · 5 = Tổng điểm tích lũy · 6 = Danh sách hạng khách hàng · 7 = Tên khách hàng · 8 = Nghề nghiệp · 9 = Giới tính. (Chi tiết mỗi điều kiện nằm trong `ConditionDetail.JsonData`, deserialize theo Type.)

## Case biên
- **TypeFilter sai:** giá trị khác Normal/Phone → controller trả lỗi ngay ("Kiểu loại bộ lọc không phù hợp!").
- **Nối điều kiện AND vs OR:** `TypeCodition = 1` (AND) thì khách phải thỏa hết; `= 2` (OR) chỉ cần thỏa một → kết quả tập khác hẳn, dễ nhầm.
- **Sửa tập đang dùng:** không có khóa chống sửa → đổi điều kiện ảnh hưởng tập khách của mọi chiến dịch tham chiếu.
- **Xóa tập đang dùng:** không thấy guard chặn xóa khi có chiến dịch tham chiếu `IdCustomerConditionFilter` → chiến dịch có thể trỏ tới tập mồ côi (cần product xác nhận).
- **Thêm loại điều kiện mới:** phải khai báo `Type` vào enum + DTO deserialize tương ứng; nếu thiếu → điều kiện không được hiểu.
- **Quét tự động:** chỉ tập có `IsAutoDailyScan` mới được job quét hằng ngày; tập không bật thì áp điều kiện tại thời điểm chiến dịch dùng.

## Ví dụ cụ thể có số
Marketing muốn gửi voucher sinh nhật cho khách VIP mua nhiều:
- Tạo tập "VIP sinh nhật T6" kiểu Normal, nối **AND**:
  - Điều kiện Hạng (Type 6) = "VIP".
  - Điều kiện Ngày sinh (Type 2) = tháng 6.
  - Điều kiện Tổng tiền mua (Type 4) ≥ 5.000.000.
- Bật quét tự động → mỗi ngày job quét lại. Ngày 01/6 quét ra **120 khách**; ngày 15/6 thêm 3 khách mới đạt mốc → **123 khách**.
- Tạo tập thứ hai "Khách tỉnh A" kiểu Phone: dán 500 SĐT → tập có đúng 500 khách (không thay đổi theo điều kiện).
- Chiến dịch "Voucher sinh nhật" trỏ vào tập "VIP sinh nhật T6" → gửi cho 123 khách hiện tại của tập.
- Sau đó sửa điều kiện tổng tiền mua thành ≥ 3.000.000 → tập tăng lên **190 khách** → chiến dịch (chưa gửi) sẽ gửi cho 190.

## Hậu điều kiện
- Tạo/sửa/xóa tập → ghi/sửa/xóa document `CustomerConditionFilter` trong MongoDB.
- Tập là input cho CustomerInfoFilter (quét tệp) và Campaign (gửi) — sửa tập làm thay đổi tập khách của các chiến dịch tham chiếu.
- Tập bật `IsAutoDailyScan` → được job quét lại mỗi ngày.
