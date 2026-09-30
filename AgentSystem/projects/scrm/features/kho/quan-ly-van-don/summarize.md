---
alias: ["quản lý vận đơn", "đăng đơn giao hàng", "thủ kho tác nghiệp", "đẩy đơn sang hãng vận chuyển", "gom hàng đi giao"]
module: kho
trang_thai: đang dùng
goi: cần product xác nhận
quyen: "Đăng đơn/tác nghiệp: quản lý kho, admin chi nhánh, CSKH. Cấu hình vận đơn/trạng thái/mẫu đóng hàng: chủ shop/admin tổng. (chi tiết cần product xác nhận)"
phu_thuoc: [Đơn giao hàng (OrderLogistic), Kho & tồn kho (WarehouseProduct), Kết nối đơn vị vận chuyển]
link_flow: ./flow.md
---

# Quản lý vận đơn

## Là gì
Nơi nhân viên kho **xử lý đơn giao hàng từ lúc có đơn đến lúc bàn giao cho hãng vận chuyển**: gom đơn, soạn hàng, đăng đơn sang hãng (GHN, GHTK, Viettel Post...), in phiếu giao và theo dõi trạng thái. Đi kèm là vài màn cấu hình để cài đặt cách hệ thống chạy cho phù hợp với shop.

Gồm 5 màn hình:
- **Đăng đơn (Thủ kho tác nghiệp):** màn làm việc chính của kho — danh sách đơn chờ giao, soạn hàng, đẩy đơn sang hãng vận chuyển, đổi trạng thái, in phiếu.
- **Cấu hình vận đơn:** cài đặt mặc định cho vận đơn (in phiếu, quy định nghiệp vụ, giới hạn gom hàng...).
- **Cấu hình trạng thái giao hàng:** đặt **thứ tự hiển thị** và **màu sắc** cho từng trạng thái giao hàng để nhìn cho dễ.
- **Mẫu kích thước đóng hàng:** lưu sẵn các khổ đóng hàng hay dùng (dài × rộng × cao × cân nặng) để khi đăng đơn chọn nhanh, khỏi gõ tay.
- **Danh sách lượt nhặt:** danh sách các "lượt gom hàng" — mỗi lượt gộp nhiều đơn lại để nhân viên đi nhặt hàng trong kho 1 lần cho nhanh, có phiếu nhặt in ra.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Đăng đơn sang hãng vận chuyển | ✅ | phải kết nối hãng vận chuyển trước; quyền kho/admin CN/CSKH |
| In phiếu giao | ✅ | sau khi đăng đơn có mã vận đơn |
| Đổi trạng thái đơn / tác nghiệp | ✅ | quyền kho/admin CN/CSKH |
| Hủy chờ đối tác + hoàn kho (về "chưa đăng") | ✅ | đúng vai trò được cấp quyền |
| Tạo lượt nhặt (gom đơn) | ⚠️ | cần cấu hình "số SKU tối đa mỗi phiếu nhặt" trước; mỗi đơn chỉ vào 1 lượt nhặt |
| In phiếu nhặt | ✅ | sau khi tạo lượt nhặt |
| Thêm / Sửa mẫu kích thước đóng hàng | ✅ | tên không trùng trong cùng chi nhánh |
| Cấu hình vận đơn | ✅ | chủ shop / admin tổng |
| Cấu hình thứ tự + màu trạng thái giao hàng | ✅ | chủ shop / admin tổng; chỉ đổi hiển thị, không đổi bản chất trạng thái |
| Xuất Excel (lưới đăng đơn) | ✅ | dùng bản *_V2 |

## Để làm gì / cho ai
- **Cho nhân viên kho / thủ kho:** xử lý đơn nhanh, gom nhiều đơn đi nhặt 1 lượt, không sót đơn, không nhặt nhầm.
- **Cho chủ shop / quản lý:** cài đặt một lần để cả team làm theo (mẫu đóng hàng chuẩn, màu trạng thái dễ nhìn, quy định vận đơn).
- **Actor:** nhân viên kho, quản lý kho, CSKH; phần cấu hình do chủ shop/admin làm.

## Cách dùng (bước chính)
1. Vào màn **Đăng đơn** → xem danh sách đơn chờ giao, lọc theo trạng thái / kho / ngày.
2. Soạn hàng cho đơn → **đăng đơn** để đẩy sang hãng vận chuyển → hệ thống trả về **mã vận đơn**.
3. In phiếu giao, dán lên kiện hàng, bàn giao cho shipper.
4. Khi cần gom nhiều đơn đi nhặt 1 lần: tạo **lượt nhặt**, in **phiếu nhặt**, nhân viên cầm đi gom hàng theo vị trí trong kho.
5. (Cài đặt một lần, do quản lý) vào **Cấu hình vận đơn / trạng thái / mẫu đóng hàng** để chỉnh cho đúng cách shop làm.

## Cần chú ý (cạm bẫy)
- **Muốn đăng đơn được thì phải kết nối hãng vận chuyển trước** (xem "Kết nối đơn vị vận chuyển"). Chưa kết nối thì không đẩy đơn đi được.
- **Mỗi đơn chỉ được tạo lượt nhặt 1 lần.** Đơn đã nằm trong 1 lượt nhặt rồi thì chọn lại sẽ bị chặn — phải xóa lượt cũ mới gom lại được.
- **Tạo lượt nhặt cần cấu hình "số SKU tối đa mỗi phiếu nhặt" trước.** Chưa cấu hình thì hệ thống báo lỗi, không tạo được lượt.
- **Mẫu đóng hàng không được trùng tên** trong cùng chi nhánh — đặt tên rõ ràng (vd "Hộp nhỏ", "Hộp lớn").
- **Cấu hình trạng thái giao hàng chỉ đổi thứ tự + màu hiển thị**, không đổi bản chất trạng thái đơn.
- Đăng đơn rồi gặp lỗi/đợi hãng quá lâu: có thao tác **hủy chờ đối tác + hoàn kho** để đưa đơn về "chưa đăng" — dùng đúng vai trò được cấp quyền.

## Ảnh hưởng tới
- **Đơn giao hàng (OrderLogistic):** đây là nơi vận hành các đơn này; đăng đơn xong sẽ cập nhật trạng thái đơn.
- **Kho & tồn kho:** soạn hàng / nhặt hàng đụng tới tồn kho; hoàn kho khi hủy đăng đơn.
- **Hãng vận chuyển:** đăng đơn = đẩy thông tin sang hãng và nhận mã vận đơn về.
- **Kế toán / đối soát COD:** trạng thái giao hàng là cơ sở để đối soát tiền thu hộ sau này.

## Link
→ [flow.md](./flow.md) · [faq.md](./faq.md) · Module: [../README.md](../README.md)
