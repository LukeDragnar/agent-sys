---
alias: ["đơn hàng", "order", "đơn sale", "đơn bán", "tạo đơn", "chốt đơn", "đơn hàng logistic", "đơn giao vận"]
module: sales
trang_thai: Core
goi: cần product xác nhận
quyen: "cần product xác nhận — Tạo đơn: DV-SALE, DV-SALE-LEAD, DV-AD-TONG, DV-AD-CN, DV-CHIASO. Chốt/Hủy chốt/Xóa: DV-AD-TONG, DV-AD-CN, DV-SALE, DV-SALE-LEAD, DV-QL-SALE. Xóa hàng loạt: chỉ DV-AD-TONG. Nhiều thao tác nghiệp vụ (đăng đơn, thanh toán, phát hành HĐĐT, quét IMEI/lô date, in đơn) chỉ gắn `[SBAuthorize()]` (mở) + gate runtime theo cấu hình quản trị / tiện ích."
phu_thuoc: ["Contact / Lead (đơn tạo từ contact, đồng bộ ngược nhãn KH / cờ đơn)", "Kho & sản phẩm (quét IMEI / lô date, hoàn thành SP → trừ tồn)", "Kết nối đơn vị vận chuyển (đăng đơn Ahamove / GHN / GHTK…)", "Kế toán & Hóa đơn điện tử (phiếu bán, phát hành HĐĐT Misa/Viettel)", "Khuyến mãi / Voucher (tính thành tiền sau KM)", "Cấu hình quản trị (gate quyền cập nhật trạng thái hàng loạt)"]
link_flow: ./flow.md
---

# Quản lý đơn hàng

## Là gì
Đây là **màn hình trung tâm của đơn bán (đơn hàng logistic)** — nơi sale và đội kho/kế toán quản lý toàn bộ vòng đời một đơn hàng: từ **tạo đơn nháp → chốt đơn → xuất kho (quét IMEI/lô date) → đăng đơn cho đơn vị vận chuyển → cập nhật trạng thái giao hàng → đối soát → phát hành hóa đơn**. Một đơn đi qua các trạng thái: Nháp → Đã chốt → Đã đăng → Đã lấy hàng → Đã giao hàng (hoặc Hủy).

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Tạo đơn (nháp) | ✅ | sale: DV-SALE/-LEAD, DV-AD-TONG/-CN, DV-CHIASO |
| Chốt đơn | ✅ | đơn đang nháp; AD/SALE/SALE-LEAD/QL-SALE |
| Hủy chốt đơn | ✅ | đơn đã chốt; cùng nhóm quyền chốt |
| Sửa đơn | ✅ | tùy quyền (nhiều role gồm kho/kế toán) |
| Xóa 1 đơn | ✅ | AD/SALE/SALE-LEAD/QL-SALE |
| **Xóa nhiều đơn theo bộ lọc** | ⚠️ | chỉ **admin tổng đơn vị (DV-AD-TONG)** |
| Tính phí vận chuyển | ✅ | tùy quyền |
| Đăng đơn / hủy đơn Ahamove | ✅ | đơn đủ điều kiện (quyền mở — chỉ cần đăng nhập) |
| Cập nhật trạng thái giao hàng (hàng loạt) | ⚠️ | phải được cấp quyền trong **cấu hình quản trị** (không qua quyền chuẩn) |
| Quét IMEI / lô date vào đơn | ✅ | đơn đang xuất kho |
| Hoàn thành sản phẩm (xuất kho) | ✅ | tùy quyền (ripple trừ tồn) |
| Phát hành hóa đơn điện tử | ⚠️ | phải có **tiện ích HĐĐT** (Misa hoặc Viettel) |
| Tính khuyến mãi / voucher | ✅ | tùy quyền |
| Import đơn từ Excel | ✅ | tùy quyền (SALE/AD/CHIASO/kho) |
| Export Excel đơn (kiểu 1 / 2) | ✅ | quyền mở; lỗi service bận → báo 503 |
| In đơn / phiếu giao | ✅ | chọn đơn (in thực do trình duyệt) |
| Ghi chú nội bộ / gắn nhãn KH | ✅ | tùy quyền |
| Xem lịch sử mua hàng theo SĐT | ✅ | tùy quyền |

## Để làm gì / cho ai
- **Sale:** tạo đơn cho khách, chốt đơn, tính phí ship, áp khuyến mãi, theo dõi đơn.
- **Kho:** quét IMEI / lô date khi xuất hàng, hoàn thành sản phẩm (trừ tồn), đăng đơn cho đơn vị vận chuyển.
- **Kế toán:** đối soát đơn, phát hành hóa đơn điện tử, phương thức thanh toán.
- **Quản lý / admin:** xóa đơn hàng loạt, cập nhật trạng thái theo mã, cấu hình quyền cập nhật.

## Cách dùng
**Tạo và chốt đơn (Sale):**
1. Vào **Đơn hàng logistic → Danh sách đơn hàng**, bấm **Tạo đơn**.
2. Chọn khách / contact, thêm sản phẩm, **Tính phí VC**, áp **khuyến mãi/voucher** nếu có → lưu (đơn nháp).
3. Bấm **Chốt đơn** khi đã xác nhận với khách.

**Xuất kho & giao hàng (Kho):**
1. Quét **IMEI** / **lô date** cho sản phẩm trên đơn.
2. Bấm **Hoàn thành sản phẩm** → hệ thống trừ tồn kho.
3. **Đăng đơn** cho đơn vị vận chuyển (Ahamove ngay tại đây; GHN/GHTK/VTP/J&T ở màn riêng).
4. Cập nhật **trạng thái giao hàng** theo mã (hàng loạt) khi đơn đã đăng / đã lấy / đã giao.

**Hóa đơn & đối soát (Kế toán):**
1. Khi đơn đạt trạng thái cấu hình → phiếu bán sinh sang kế toán.
2. **Phát hành hóa đơn điện tử** (cần tiện ích Misa hoặc Viettel).

## Cần chú ý
- **Chống tạo đơn trùng:** khi tạo / chốt đơn cho cùng một contact (SĐT), hệ thống khóa tạm 60 giây để chặn tạo trùng. Nếu bấm hai lần nhanh, lần thứ hai có thể bị chặn — đợi vài giây rồi thử lại.
- **Cập nhật trạng thái hàng loạt cần quyền riêng:** quyền này không nằm trong phân quyền vai trò thông thường mà nằm trong **cấu hình quản trị**. Nếu báo "Bạn không có quyền" dù bạn là admin, kiểm tra cấu hình quản trị cho user đó.
- **Phát hành hóa đơn cần tiện ích:** nếu đơn vị chưa mua tiện ích hóa đơn điện tử (Misa / Viettel) thì không phát hành được.
- **Xóa nhiều đơn rất nguy hiểm** — chỉ admin tổng đơn vị làm được và có ghi log.
- **Export bị "chưa xuất được":** khi dịch vụ xuất đang bận, hệ thống báo lỗi tạm (503) — thử lại sau.
- Một số thao tác nhạy cảm (đăng đơn, thanh toán, quét IMEI, in đơn) **để quyền mở** (chỉ cần đăng nhập), kiểm soát qua cấu hình / tiện ích runtime — *cần product xác nhận* có chủ ý không.

## Ảnh hưởng tới
- **Kho / tồn:** hoàn thành sản phẩm → sinh phiếu xuất, trừ tồn kho.
- **Đơn vị vận chuyển:** đăng đơn Ahamove → đẩy sang hệ thống delivery_connect.
- **Kế toán & HĐĐT:** đơn đạt trạng thái → sinh phiếu bán; phát hành HĐĐT → kế toán callback trạng thái.
- **Contact:** mọi thay đổi đơn (nhãn KH, tin nhắn, xóa) đồng bộ ngược về contact.
- **Khuyến mãi / Voucher:** áp KM ảnh hưởng thành tiền đơn.
