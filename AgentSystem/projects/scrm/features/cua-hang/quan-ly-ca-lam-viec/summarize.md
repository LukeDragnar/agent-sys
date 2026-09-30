---
alias: ["biên bản bàn giao ca", "bàn giao ca", "giao ca", "đầu ca", "cuối ca", "xác nhận ca", "tiền thiếu cuối ca", "biên bản ca", "quản lý ca làm việc"]
module: cua-hang
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: "cần product xác nhận — code gắn role cho phép thao tác biên bản gồm: DV-AD-TONG, DV-AD-CN, HT-KT, DV-KT, DV-KT-TONG, DV-QLDV. Thêm/Xóa dòng chi tiết qua màn quản trị chỉ super user (HT-AD)."
phu_thuoc: ["Tài khoản nhân viên (người nhận đầu/cuối ca)", "Đơn vị / chi nhánh (mỗi biên bản gắn 1 đơn vị)", "Sản phẩm / tiền mặt trong ca (tổng đầu ca, cuối ca)"]
link_flow: ./flow.md
---

# Quản lý ca làm việc (Biên bản bàn giao ca)

## Là gì
Đây là nơi **lập biên bản bàn giao ca làm việc** tại cửa hàng: ghi nhận ai nhận ca, tổng sản phẩm và tiền mặt **đầu ca**, rồi đến cuối ca ghi lại tổng cuối ca và **tiền thiếu** (nếu có). Mỗi biên bản đi qua 2 mốc xác nhận: **xác nhận đầu ca** và **xác nhận cuối ca**. Khi đã xác nhận thì biên bản bị khóa, không xóa được — giúp cửa hàng đối chiếu quỹ và trách nhiệm giữa các ca rõ ràng.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Thêm biên bản giao ca | ✅ | tùy quyền; hệ thống ghi lại người tạo |
| Sửa thông tin biên bản | ✅ | tùy quyền; phần đã xác nhận thì bị khóa |
| Nhập thông tin **đầu ca** (người nhận, tổng SP/tiền) | ✅ | tùy quyền |
| Nhập thông tin **cuối ca** (người nhận, tiền thiếu) | ✅ | sau khi đã có đầu ca |
| Xác nhận đầu ca | ✅ | khi biên bản còn ở trạng thái Nháp |
| Xác nhận cuối ca | ✅ | sau khi đã xác nhận đầu ca |
| **Xóa biên bản** | ⚠️ | **chỉ khi chưa xác nhận**, và **chỉ người tạo** (hoặc super user) mới xóa được |
| Xem chi tiết biên bản | ✅ | tùy quyền |
| Thêm/Xóa từng dòng chi tiết qua màn quản trị | ⚠️ | **chỉ super user (HT-AD)** mới thao tác trực tiếp; người dùng cửa hàng thêm chi tiết ngay trong biên bản |

## Để làm gì / cho ai
- **Thu ngân / nhân viên trực ca:** ghi nhận mình nhận ca với bao nhiêu tiền/hàng đầu ca, cuối ca giao lại bao nhiêu, thiếu bao nhiêu.
- **Quản lý cửa hàng / kế toán:** đối chiếu quỹ giữa các ca, biết ai chịu trách nhiệm ca nào, phát hiện chênh lệch tiền.
- **Chủ shop:** kiểm soát dòng tiền mặt tại quầy theo từng ca.

## Cách dùng
1. Vào màn **Biên bản bàn giao ca**.
2. Bấm **Thêm mới** để lập biên bản cho ca sắp tới.
3. **Đầu ca:** chọn người nhận ca, nhập tổng sản phẩm và tiền mặt nhận đầu ca, rồi bấm **Xác nhận đầu ca**. Sau bước này phần đầu ca bị khóa.
4. **Cuối ca:** khi hết ca, mở lại biên bản, nhập người nhận cuối ca và **tiền thiếu cuối ca** (nếu có), rồi bấm **Xác nhận cuối ca** để đóng ca.
5. Có thể **xem lại** chi tiết biên bản bất kỳ lúc nào; **xóa** chỉ được nếu biên bản chưa xác nhận và bạn là người tạo.

## Cần chú ý
- **Đã xác nhận thì không xóa được.** Khi biên bản đã xác nhận đầu ca hoặc cuối ca, hệ thống chặn xóa với thông báo "Không thể xóa biên bản đã được xác nhận!".
- **Chỉ người tạo (hoặc super user) mới xóa được.** Nếu bạn không phải người lập biên bản, hệ thống báo "Người tạo phiếu mới có thể xóa!".
- **Thứ tự xác nhận cố định:** đầu ca trước → cuối ca sau. Không thể xác nhận cuối ca khi chưa làm đầu ca.
- **Mỗi biên bản chỉ thuộc 1 đơn vị**; bạn không thao tác được biên bản của đơn vị khác.
- Việc **thêm/xóa từng dòng chi tiết qua màn quản trị riêng** chỉ dành cho super user hệ thống; người dùng cửa hàng làm việc với chi tiết ngay trong màn biên bản.

## Ảnh hưởng tới
- **Đối chiếu quỹ tiền mặt / doanh thu ca** — biên bản là căn cứ so tiền giữa các ca.
- *Cần product xác nhận:* biên bản có tự đối chiếu với doanh thu/đơn hàng phát sinh trong ca hay không (hiện chưa thấy liên kết tự động trong code).
