---
alias: ["báo cáo sản phẩm", "báo cáo hàng", "báo cáo combo", "tỉ lệ chốt đơn sản phẩm", "số ngày chưa bán được sản phẩm", "combo sản phẩm", "hiệu suất sản phẩm"]
module: bao-cao
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: "cần product xác nhận — báo cáo public ở mức controller (key BaoCaoOnlineController_ReportTyLeChotDonSanPham, ..._ReportCombo, ..._ReportComboSanPham, ..._ReportThongKeSoNgayChuaBanDuocSanPham). Phạm vi dữ liệu theo chi nhánh."
phu_thuoc: ["Sản phẩm / Combo (danh mục hàng)", "Đơn hàng (nguồn bán)", "Cấu hình đơn vị (giới hạn khoảng ngày, ép chi nhánh)"]
link_flow: ./flow.md
---

# Báo cáo sản phẩm

## Là gì
Đây là **bộ báo cáo phân tích sản phẩm/combo**: bán được bao nhiêu, hiệu suất ra sao, sản phẩm nào lâu chưa bán được. Gồm:
- **Tỉ lệ chốt đơn sản phẩm** — trong số contact/đơn quan tâm SP, bao nhiêu chốt thành công.
- **Báo cáo combo** và **combo sản phẩm** — kết quả bán theo combo và theo SP trong combo.
- **Số ngày chưa bán được sản phẩm** — cảnh báo hàng tồn lâu, chưa phát sinh bán.

Mỗi báo cáo là một **bảng (lưới)** lọc theo chi nhánh + khoảng ngày, phần lớn có nút **Xuất Excel**.

Lưu ý: báo cáo bán sản phẩm theo doanh thu (theo SP, theo ngày) thuộc nhóm **Báo cáo doanh số** — xem feature đó.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Xem tỉ lệ chốt đơn sản phẩm | ✅ | tùy quyền; trong phạm vi chi nhánh |
| Xem báo cáo combo / combo sản phẩm | ✅ | tùy quyền |
| Xem số ngày chưa bán được sản phẩm | ✅ | tùy quyền |
| Lọc theo chi nhánh + khoảng ngày + sản phẩm | ✅ | bị giới hạn ngày nếu đơn vị bật |
| Xuất Excel | ✅ | hầu hết có; **Excel combo** xuất từ dữ liệu lưới hiện có (không chạy lại) |
| **Sửa / Xóa số liệu** | ❌ | báo cáo **chỉ đọc** |

## Để làm gì / cho ai
- **Chủ shop / quản lý:** biết SP nào bán chạy, combo nào hiệu quả, hàng nào tồn lâu chưa bán để có phương án xả/khuyến mãi.
- **Mua hàng / kho:** dùng "số ngày chưa bán được" để cân nhắc nhập thêm hay ngừng nhập.
- **Sale / Marketing:** xem tỉ lệ chốt đơn theo SP để tối ưu kịch bản bán.

## Cách dùng
1. Vào module **Báo cáo** → chọn báo cáo sản phẩm (Tỉ lệ chốt đơn SP, Combo, Combo sản phẩm, Số ngày chưa bán được SP).
2. Chọn **chi nhánh**, **khoảng ngày Từ – Đến**, và **sản phẩm** (nếu báo cáo hỗ trợ).
3. Bấm **Xem / Tìm** → bảng số liệu hiện ra.
4. Bấm **Xuất Excel** nếu cần tải file.

## Cần chú ý
- **Báo cáo chỉ đọc**, không sửa được dữ liệu sản phẩm/đơn từ đây.
- **Excel combo** được tạo từ dữ liệu đang hiển thị trên lưới, không truy vấn lại — nếu lưới chưa tải đủ thì file cũng thiếu (xem dev.md).
- **Giới hạn khoảng ngày** áp dụng như mọi báo cáo.
- **"Số ngày chưa bán được"** tính từ lần bán gần nhất của SP; SP chưa từng bán sẽ có giá trị lớn.

## Ảnh hưởng tới
- **Sản phẩm / Combo** — nguồn danh mục hàng; báo cáo phản ánh kết quả bán của chúng.
- **Đơn hàng** — quyết định SP/combo nào được tính là bán.
- **Báo cáo doanh số** — báo cáo doanh thu theo SP nằm ở nhóm doanh số, bổ trợ cho nhóm này.
- **Mua hàng / kho** — dùng cảnh báo SP tồn lâu chưa bán để ra quyết định nhập hàng.
