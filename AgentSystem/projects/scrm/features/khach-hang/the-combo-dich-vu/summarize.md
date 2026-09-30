---
alias: ["thẻ dịch vụ", "gói liệu trình", "combo khách hàng", "thẻ tích buổi", "thẻ số lần", "thẻ số tiền", "combo spa"]
module: khach-hang
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: cần product xác nhận
phu_thuoc: [WarehouseProduct (danh mục Thẻ dịch vụ / Combo gốc), Order (đơn hàng trừ thẻ/combo khi sử dụng), Customer (hồ sơ khách hàng), Booking (đặt lịch dùng buổi), Report (báo cáo lịch sử dùng thẻ), Core (chi nhánh / phân quyền)]
link_flow: ./flow.md
---

# Thẻ & combo dịch vụ (của khách hàng)

## Là gì
Tính năng **cấp thẻ dịch vụ và combo/gói liệu trình cho từng khách hàng để khách dùng dần** — mỗi lần đến cửa hàng/spa thì trừ vào thẻ hoặc combo thay vì thanh toán lại từ đầu.

Có **2 loại "thẻ dịch vụ":**
- **Thẻ số lần (thẻ tích buổi):** mua sẵn 1 số **buổi/lần** (ví dụ thẻ 10 buổi massage), mỗi lần làm dịch vụ trừ 1 buổi.
- **Thẻ số tiền (thẻ trả trước):** nạp sẵn 1 **số tiền** vào thẻ (kèm tiền khuyến mãi nếu có), mỗi lần mua hàng/dịch vụ trừ tiền trong thẻ.

Và **combo dịch vụ (gói liệu trình spa):** một gói gồm **nhiều dịch vụ, mỗi dịch vụ có số buổi riêng** (ví dụ gói "Trắng da 12 buổi": 6 buổi soi da + 6 buổi đắp mặt nạ). Mỗi lần làm trừ đúng buổi của dịch vụ đó trong combo.

Các màn hình trong nhóm này:
- **Danh sách thẻ-khách** — toàn bộ thẻ đã cấp cho các khách (lọc theo loại, ngày hết hạn, chi nhánh).
- **Thẻ khách hàng** — cấp thẻ mới cho 1/nhiều khách, xem thẻ của 1 khách, gia hạn, khóa, thu hồi.
- **Lịch sử dùng thẻ** — báo cáo từng lần thẻ bị trừ (buổi/tiền), theo đơn hàng.
- **Danh sách combo-khách** — toàn bộ combo đã bán cho các khách, lọc theo số buổi còn lại.
- **Khách hàng-combo** — bán/xem combo của 1 khách, xem buổi còn lại từng dịch vụ trong gói.
- **Template mẫu** — mẫu giao diện thẻ/combo do hệ thống dựng sẵn, đơn vị copy về dùng.
- **Template đơn vị** — mẫu giao diện thẻ/combo riêng của đơn vị, chọn 1 mẫu làm mặc định.

> Phân biệt: ở đây là **thẻ/combo CỦA KHÁCH** (đã cấp cho khách cụ thể, có số dư riêng). Còn **danh mục Thẻ / Combo gốc** (định nghĩa loại thẻ, giá, số buổi, thời hạn) nằm ở module **Kho**.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Cấp / Sửa thẻ dịch vụ cho khách | ✅ | DV-AD-*, DV-QLDV; số đã-dùng-ban-đầu không được vượt tổng |
| Xóa thẻ | ✅ | DV-AD-*, DV-QLDV (lưu ý: dùng HTTP GET) |
| Khóa / Mở khóa / Thu hồi thẻ | ✅ | tùy quyền; Thu hồi là một chiều, không phục hồi |
| Gia hạn thẻ (hàng loạt) | ✅ | DV-AD-*, DV-QLDV |
| Nạp tiền trực tiếp vào thẻ tiền | ✅ | thẻ tiền đang Sử dụng; đồng bộ dòng tiền sang Order, fail thì rollback |
| Trừ buổi/tiền thẻ khi thanh toán | ✅ | tự động khi lên đơn (logic ở Order), không thao tác tay |
| Bán / Sửa combo cho khách | ✅ | tùy quyền; sửa chặn số còn lại > số ban đầu |
| Xóa combo khách | ✅ | soft-delete |
| Cộng / Trừ buổi combo | ✅ | thu ngân/đặt lịch; ⚠️ check hạn đang lỗi (chặn nhầm combo còn hạn) |
| Import combo khách hàng (3 mẫu + update) | ✅ | qua file Excel theo mẫu |
| Xem lịch sử dùng/giao dịch thẻ | ✅ | read-only, theo từng thẻ |
| Hủy đơn → hoàn lại buổi/tiền | ✅ | tự động khi đơn dùng thẻ/combo bị hủy/rollback |

## Để làm gì / cho ai
- **Cho spa / phòng khám / salon bán liệu trình:** bán trọn gói nhiều buổi, khách trả tiền 1 lần rồi đến dùng dần — giữ chân khách, thu tiền trước.
- **Cho cửa hàng bán thẻ trả trước:** khách nạp tiền vào thẻ thành viên, mua hàng trừ thẳng vào thẻ (thường kèm khuyến mãi khi nạp).
- **Cho lễ tân / thu ngân / kỹ thuật viên:** khi khách đến chỉ cần chọn thẻ/combo của khách, hệ thống tự trừ buổi/tiền, không cần tính lại giá.
- **Cho quản lý:** theo dõi thẻ sắp hết hạn, buổi còn lại, doanh thu trả trước, lịch sử sử dụng.
- **Actor:** Admin tổng / Admin chi nhánh / Quản lý dịch vụ cấp & quản lý thẻ-combo; Sale / Nhân viên / Lễ tân xem và dùng khi lên đơn (theo phân quyền — *danh sách quyền chi tiết cần product xác nhận*).

## Cách dùng
**Cấp thẻ cho khách:**
1. Vào màn **Thẻ khách hàng** → **Thêm mới**.
2. Chọn **loại thẻ** (đã tạo sẵn trong Kho), chọn **một hoặc nhiều khách hàng**.
3. Nhập **ngày cấp**, số lượng thẻ cấp cho mỗi khách, (tùy chọn) số buổi/số tiền đã dùng ban đầu.
4. Lưu → mỗi khách được 1 thẻ riêng, có mã thẻ, số buổi/tiền còn lại, **ngày hết hạn tự tính** theo thời hạn của loại thẻ.

**Bán combo cho khách:** vào **Khách hàng-combo** → chọn khách + combo → lưu; hệ thống ghi nhận từng dịch vụ trong gói với số buổi của nó.

**Dùng thẻ/combo:** khi **lên đơn hàng** (hoặc đặt lịch) cho khách, chọn thanh toán/sử dụng bằng thẻ hoặc combo của khách → hệ thống **tự trừ buổi/tiền** sau khi đơn hoàn tất.

**Theo dõi:** xem **Danh sách thẻ-khách / combo-khách** để lọc thẻ sắp hết hạn, buổi còn lại; xem **Lịch sử dùng thẻ** để biết khách đã dùng những gì.

## Cần chú ý
- **Thẻ trừ dần, không "tiêu là mất hết":** thẻ số lần trừ theo **buổi**, thẻ số tiền trừ theo **tiền**, combo trừ theo **buổi của từng dịch vụ** trong gói. Còn số dư là còn dùng được.
- **Số buổi/tiền đã dùng ban đầu không được lớn hơn thẻ:** khi cấp thẻ mà nhập "đã dùng ban đầu" vượt quá số buổi / số tiền / tiền khuyến mãi của loại thẻ → hệ thống báo lỗi, không cho cấp.
- **Ngày hết hạn tính tự động:** theo thời hạn của loại thẻ (số tháng/ngày), tính **từ ngày cấp** hoặc **từ ngày tạo thẻ** tùy cấu hình loại thẻ. Hết hạn thì thẻ không dùng được nữa dù còn buổi/tiền.
- **Trạng thái thẻ:** Sử dụng / Khóa / Thu hồi / Hoàn thành / Hết hạn. Khóa thì tạm không dùng được; thu hồi là lấy lại thẻ; hết buổi/hết hạn chuyển sang Hoàn thành/Hết hạn.
- **Tiền khuyến mãi của thẻ tách riêng:** thẻ số tiền có thể có thêm "tiền khuyến mãi" (nạp X tặng Y) — phần này được theo dõi và trừ riêng theo cấu hình.
- **Hủy đơn thì hoàn lại buổi/tiền:** nếu đơn dùng thẻ/combo bị hủy/rollback, số buổi/tiền đã trừ được **cộng trả lại** vào thẻ/combo.
- **Phạm vi theo chi nhánh:** một số màn lọc theo chi nhánh của tài khoản đang đăng nhập — đứng ở chi nhánh khác có thể không thấy đủ thẻ/combo (*chi tiết quyền xem theo chi nhánh cần product xác nhận*).
- **Template chỉ là giao diện hiển thị:** Template mẫu / Template đơn vị là **mẫu trình bày thẻ/combo** (ảnh nền, tiêu đề...), không ảnh hưởng số dư hay nghiệp vụ trừ thẻ.

## Ảnh hưởng tới
- **Đơn hàng / Đặt lịch** — thẻ và combo của khách là phương thức thanh toán/sử dụng khi lên đơn; đơn hoàn tất sẽ trừ vào thẻ/combo.
- **Hồ sơ khách hàng** — tab Thẻ dịch vụ / Combo hiển thị thẻ-combo khách đang có; lịch sử tương tác ghi nhận mỗi lần cấp/dùng.
- **Báo cáo** — Lịch sử dùng thẻ, doanh thu trả trước, thẻ sắp hết hạn.
- **Danh mục Kho (Thẻ / Combo gốc)** — là nơi định nghĩa loại thẻ/combo; thẻ-combo của khách "đẻ ra" từ các định nghĩa này.

## Link
→ [flow.md](./flow.md) · [faq.md](./faq.md) · Module: [../README.md](../README.md) · Kỹ thuật: [./dev.md](./dev.md)
