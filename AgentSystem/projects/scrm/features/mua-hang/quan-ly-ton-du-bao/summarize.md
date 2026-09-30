---
alias: ["dự báo hết hàng", "dự kiến thêm hàng", "cảnh báo tồn kho", "báo cáo tồn kho đơn vị", "tồn kho tối thiểu", "tồn tối đa", "ngưỡng tồn kho", "min max tồn kho", "giới hạn tồn kho", "khi nào cần nhập hàng"]
module: mua-hang
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: "cần product xác nhận — Báo cáo tồn kho đơn vị (xem): role AD/KT/QLDV/QL-KHO...; Xuất Excel báo cáo tồn kho đơn vị chỉ admin (DV-AD-TONG, DV-AD-CN, DV-AD-MCN). Cấu hình Giới hạn tồn kho: DV-AD-TONG, DV-AD-CN, DV-MCN (non-admin chỉ thao tác trong chi nhánh mình)."
phu_thuoc: ["Quản lý sản phẩm (sản phẩm, nhóm SP, đơn vị tính)", "Kho / Chi nhánh", "Cấu hình kho (cho phép tính giới hạn tồn kho tự động)", "Đơn đặt hàng (tạo đơn đặt hàng trực tiếp từ màn dự báo)"]
link_flow: ./flow.md
---

# Quản lý tồn (dự báo)

## Là gì
Đây là nơi **theo dõi số lượng hàng đang còn và dự báo khi nào sắp hết** để chủ động đặt thêm hàng. Tính năng gồm hai phần đi đôi với nhau:

- **Báo cáo tồn kho đơn vị / Dự báo hết hàng**: bảng tổng hợp tồn kho theo sản phẩm và chi nhánh, kèm cảnh báo "sắp hết / cần nhập thêm" dựa trên tốc độ bán và ngưỡng đã cài. Từ màn này có thể bấm tạo luôn đơn đặt hàng cho những mặt hàng thiếu.
- **Giới hạn tồn kho (cấu hình ngưỡng)**: nơi khai báo trước cho từng sản phẩm × chi nhánh các mốc **tồn tối thiểu / tối đa / khẩn cấp**, số ngày tồn tương ứng và mức bán trung bình. Đây là "ngưỡng" để hệ thống biết khi nào báo động.

Hiểu đơn giản: phần Giới hạn tồn kho là **đặt mức báo động**, còn phần Báo cáo/Dự báo là **xem đèn báo động đó sáng ở mặt hàng nào** để đi nhập hàng.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Xem báo cáo tồn kho đơn vị | ✅ | tùy quyền xem kho |
| Xem dự báo hết hàng (theo khoảng ngày) | ✅ | cần bật "tính giới hạn tồn kho tự động" và chọn kiểu khoảng ngày |
| Xuất Excel báo cáo tồn kho đơn vị | ⚠️ | chỉ admin (tổng/chi nhánh/multi chi nhánh); tối đa 10.000 dòng/lần |
| Tạo đơn đặt hàng từ màn dự báo | ✅ | tùy quyền tạo đơn đặt hàng |
| Thêm ngưỡng giới hạn tồn (1 hoặc nhiều dòng) | ✅ | tối đa 100 dòng/lần; non-admin chỉ chi nhánh mình |
| Sửa ngưỡng (sửa nội dòng tồn tối thiểu/tối đa) | ⚠️ | xem lưu ý kỹ thuật ở dev — payload sửa nhanh có thể bị từ chối |
| Xóa ngưỡng | ✅ | xóa cứng; chỉ trong đơn vị/chi nhánh của mình |
| Khởi tạo ngưỡng hàng loạt | ✅ | quét toàn bộ SP đơn vị, áp 1 bộ cấu hình |
| Nhập ngưỡng từ Excel | ✅ | tối đa 2.000 dòng/lần |
| Tải file mẫu / file dòng lỗi import | ✅ | tùy quyền |

## Để làm gì / cho ai
- **Chủ shop / quản lý kho / bộ phận mua hàng:** biết mặt hàng nào sắp hết để đặt hàng kịp, tránh đứt hàng; cũng tránh ôm hàng quá mức (tồn tối đa).
- **Admin đơn vị / chi nhánh:** cài mức tồn tối thiểu/tối đa cho từng sản phẩm để hệ thống tự cảnh báo, không phải canh thủ công.
- **Người mua hàng:** từ màn dự báo bấm tạo luôn đơn đặt hàng cho phần thiếu, nối thẳng sang quy trình đặt hàng.

## Cách dùng

**Cài mức báo động (Giới hạn tồn kho):**
1. Vào **Kho → Giới hạn tồn kho**.
2. Bấm **Thêm mới** để nhập từng dòng (chọn sản phẩm, chi nhánh, nhập tồn tối thiểu / tối đa / khẩn cấp, số ngày tồn, mức bán trung bình, khoảng ngày tính trung bình), hoặc bấm **Khởi tạo** để áp một bộ cấu hình cho hàng loạt sản phẩm, hoặc **Nhập Excel** nếu có sẵn file.
3. Có thể sửa nhanh cột Tồn tối thiểu/tối đa ngay trên lưới rồi bấm **Cập nhật**.

**Xem dự báo và đặt hàng (Báo cáo tồn kho đơn vị):**
1. Vào **Kho → Báo cáo tồn kho đơn vị** (hoặc màn Kiểm tra tồn kho → Báo cáo tồn kho đơn vị).
2. Chọn điều kiện (chi nhánh, nhóm SP, khoảng ngày) rồi xem; muốn cảnh báo "dự báo hết hàng" thì bật chế độ dự báo và chọn kiểu khoảng ngày.
3. Xem các mặt hàng đang thiếu / sắp hết; bấm **tạo đơn đặt hàng** cho những mặt hàng cần nhập thêm.
4. Có thể **Xuất Excel** báo cáo (nếu là admin).

## Cần chú ý
- **Phải cài ngưỡng trước thì dự báo mới có ý nghĩa.** Nếu chưa khai báo Giới hạn tồn kho cho sản phẩm, cảnh báo "sắp hết" sẽ không chính xác.
- **Mỗi nhân viên chỉ thấy/cấu hình được tồn trong phạm vi chi nhánh của mình** (trừ admin). Đây là phân quyền, không phải lỗi.
- **Dự báo hết hàng cần bật cấu hình "tính giới hạn tồn kho tự động"** và **bắt buộc chọn kiểu khoảng ngày**; thiếu sẽ báo lỗi yêu cầu chọn.
- **Xuất Excel báo cáo tồn kho đơn vị** chỉ dành cho admin và giới hạn 10.000 dòng/lần — nếu nhiều hơn phải lọc bớt rồi xuất từng phần.
- **Xóa ngưỡng là xóa hẳn** (không khôi phục được).
- Một mặt hàng × một chi nhánh chỉ có **một** dòng ngưỡng; khai trùng sẽ bị chặn.

## Ảnh hưởng tới
- **Đơn đặt hàng:** dự báo là điểm bắt đầu để lập đơn đặt hàng nhập về.
- **Các báo cáo/cảnh báo tồn kho khác:** đều đọc tham chiếu mức ngưỡng đã cài ở đây.
- **Quản lý tồn kho (xem tồn thực tế):** dùng chung dữ liệu tồn; thay đổi cấu hình ngưỡng không làm thay đổi số tồn thực tế, chỉ thay đổi mức cảnh báo.
