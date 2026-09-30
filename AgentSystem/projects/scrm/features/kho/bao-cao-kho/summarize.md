---
alias: ["báo cáo kho", "báo cáo nhập xuất kho", "báo cáo tồn kho", "báo cáo sản phẩm bán", "báo cáo quay vòng sản phẩm"]
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: "cần product xác nhận — code yêu cầu 1 trong các role: DV-AD-TONG, DV-AD-CN, DV-AD-MCN, DV-KT, DV-KT-TONG, HT-AD, HT-CSKH, DV-QLDV, DV-QL-KHO, DV-QL-KHO-TONG, DV-TP-QL-KHO, DV-TN-QL-KHO"
phu_thuoc: [WarehouseProduct (Kho, KhoChiTiet, SanPham, nhóm/nhãn sản phẩm, đơn vị tính/quy đổi), Order (đơn hàng — cho báo cáo sản phẩm bán), Report service (kiểm soát giới hạn truy cập + cấu hình khoảng thời gian)]
link_flow: ./flow.md
module: kho
---

# Báo cáo kho

## Là gì
Báo cáo kho là **nhóm các báo cáo thống kê hoạt động của kho hàng**: nhập bao nhiêu, xuất bao nhiêu, còn tồn bao nhiêu, sản phẩm nào bán chạy, sản phẩm nào "nằm kho" lâu. Mỗi loại báo cáo là một màn hình riêng, nhưng cách dùng giống nhau: **chọn điều kiện lọc (kho, chi nhánh, nhóm sản phẩm, khoảng thời gian…) → bấm xem → ra bảng số liệu → xuất Excel nếu cần.**

Tất cả số liệu được tính tại thời điểm bạn bấm xem, dựa trên dữ liệu nhập/xuất/bán thực tế trong khoảng thời gian đã chọn.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Xem báo cáo (nhập / xuất / nhập-xuất / tồn / sản phẩm bán / quay vòng…) | ✅ | cần 1 trong các role kho/kế toán; chỉ-đọc |
| Lọc theo chi nhánh / kho / nhóm – sản phẩm / nhãn / từ khóa | ✅ | non-admin thường chỉ thấy dữ liệu chi nhánh của mình |
| Lọc theo khoảng thời gian | ⚠️ | giới hạn tối đa N ngày theo cấu hình đơn vị; vượt khoảng → báo lỗi |
| Chọn cách gộp (theo sản phẩm / theo kho–sản phẩm) | ✅ | với báo cáo hỗ trợ gộp |
| Xuất Excel | ✅ | với các báo cáo có nút xuất |
| Sửa / tạo phiếu / trừ tồn từ màn báo cáo | ❌ | báo cáo chỉ đọc, không thay đổi dữ liệu kho |
| Chạy báo cáo nặng khi hệ thống quá tải | ⚠️ | có cơ chế xếp hàng; báo "Chưa đến lượt truy cập" thì chờ thử lại |

## Để làm gì / cho ai
- **Cho chủ shop / quản lý:** nắm được dòng tiền hàng hóa — nhập nhiều hay ít, hàng tồn đọng, sản phẩm bán chạy để quyết định nhập thêm hay ngừng nhập.
- **Cho kế toán:** đối chiếu giá trị nhập/xuất/tồn theo kỳ.
- **Cho quản lý kho:** theo dõi tồn kho từng kho/chi nhánh, biết món nào quay vòng chậm.
- **Actor:** chủ đơn vị, kế toán, quản lý kho, CSKH (xem hỗ trợ). Mỗi loại báo cáo yêu cầu quyền tương ứng.

## Cách dùng
1. Vào module **Báo cáo** (nhóm báo cáo kho) và chọn loại báo cáo muốn xem.
2. Đặt **bộ lọc**: chi nhánh, kho, nhóm sản phẩm / sản phẩm cha / sản phẩm cụ thể, nhãn sản phẩm, từ khóa, và **khoảng thời gian** (với báo cáo tồn kho thì xem tại thời điểm, không bắt buộc khoảng thời gian).
3. Chọn **cách gộp** (theo sản phẩm hoặc theo kho–sản phẩm) nếu báo cáo hỗ trợ.
4. Bấm xem → bảng số liệu hiện ra kèm dòng **tổng cộng**, có **phân trang** và **sắp xếp** theo cột.
5. Bấm **Xuất Excel** để tải file về (các báo cáo có nút xuất).

> Lưu ý: hệ thống giới hạn khoảng thời gian được phép truy vấn và có cơ chế "xếp hàng" khi nhiều người cùng chạy báo cáo nặng (xem mục Cần chú ý).

## Các loại báo cáo
1. **Báo cáo nhập kho** — tổng số lượng/giá trị hàng đã nhập theo sản phẩm trong kỳ.
2. **Báo cáo nhập–xuất kho** — nhập, xuất, tồn đầu kỳ/cuối kỳ trên cùng một bảng; dùng để nhìn toàn cảnh biến động kho.
3. **Báo cáo tồn kho** — số lượng và giá trị hàng **đang còn** trong kho tại thời điểm xem (không theo khoảng thời gian).
4. **Báo cáo xuất kho** (và biến thể **xuất kho theo nhân viên**) — hàng đã xuất ra, có thể xem gộp theo nhân viên thực hiện xuất.
5. **Báo cáo sản phẩm bán** — doanh số/số lượng bán của từng sản phẩm (lấy từ đơn hàng).
6. **Báo cáo sản phẩm bán theo ngày** — số lượng bán tách theo từng ngày, dạng bảng ngày × sản phẩm.
7. **Báo cáo quay vòng sản phẩm** — tốc độ luân chuyển hàng (vòng quay, số ngày tồn trung bình), phân loại bán chạy / trung bình / bán kém.

## Cần chú ý
- **Giới hạn khoảng thời gian:** đơn vị có cấu hình "chỉ được xem dữ liệu trong khoảng N ngày". Chọn khoảng dài hơn sẽ báo lỗi và không cho xem.
- **Xếp hàng truy cập:** báo cáo kho nặng có giới hạn số người chạy đồng thời. Nếu đang quá tải, hệ thống báo **"Chưa đến lượt truy cập"** — chờ rồi thử lại.
- **Phân quyền theo chi nhánh:** người dùng không phải admin/super-user thường chỉ thấy dữ liệu chi nhánh của mình, dù chọn chi nhánh khác.
- **Đơn vị tính:** số liệu hiển thị theo đơn vị tính hiện tại của sản phẩm (có quy đổi); cùng một sản phẩm nhập/bán bằng đơn vị khác nhau vẫn được quy về để so sánh.
- **Số liệu là ảnh chụp tại thời điểm xem** — chạy lại lúc khác có thể khác nếu có phát sinh nhập/xuất/bán mới.

## Ảnh hưởng tới
Báo cáo kho **chỉ đọc, không thay đổi dữ liệu** — không tạo phiếu, không trừ tồn. Nó **phản ánh** kết quả của các nghiệp vụ khác:
- **Nhập kho / xuất kho / kiểm kho / chuyển kho** → ảnh hưởng số nhập, xuất, tồn.
- **Đơn hàng (bán hàng)** → ảnh hưởng báo cáo sản phẩm bán, sản phẩm bán theo ngày, và phần xuất do bán.
- **Cấu hình đơn vị tính / nhóm / nhãn sản phẩm** → ảnh hưởng cách gộp và hiển thị.

Nếu số liệu báo cáo sai, gốc rễ thường nằm ở các nghiệp vụ nhập/xuất/bán ở trên, không phải ở màn báo cáo.
