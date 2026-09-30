---
alias:
  - "kết nối đơn vị vận chuyển"
  - "kết nối giao hàng"
  - "kết nối GHN / Viettel Post / J&T"
  - "đăng đơn vận chuyển"
  - "tích hợp hãng vận chuyển"
module: kho
trang_thai: dang_su_dung
goi: "cần product xác nhận (gói nào được bật tích hợp giao vận)"
quyen: "cần product xác nhận — thao tác kết nối yêu cầu quyền cấp Tổng / Chi nhánh / Quản lý chi nhánh (DV-AD-TONG, DV-AD-CN, DV-AD-MCN)"
phu_thuoc:
  - "Quản lý vận đơn (đăng đơn dùng tài khoản đã kết nối ở đây)"
  - "Quản lý đơn hàng (đơn phải có người nhận, sản phẩm, COD hợp lệ trước khi đẩy sang hãng)"
  - "Quản lý kho / chi nhánh (mỗi tài khoản giao vận gắn với 1 kho lấy hàng)"
  - "Lịch sử nghiệp vụ logistics (ghi nhận sự kiện đăng đơn / hủy / cập nhật trạng thái)"
---

# Kết nối đơn vị vận chuyển

## Là gì

Là nơi bạn **liên kết phần mềm SCRM với các hãng vận chuyển** (GHN, Viettel Post, J&T...) bằng tài khoản giao vận của shop. Sau khi kết nối, bạn không cần vào web/app của từng hãng nữa: đơn hàng chốt xong có thể **đẩy thẳng (đăng đơn) lên hãng ngay trong SCRM**, lấy mã vận đơn, tính phí ship và theo dõi trạng thái giao hàng tự động.

Mỗi hãng là một "đối tác giao vận". Bạn có thể kết nối **nhiều hãng cùng lúc** và chọn hãng phù hợp cho từng đơn.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Kết nối hãng vận chuyển (nhập token / tài khoản, chọn kho lấy hàng) | ✅ | quyền cấp Tổng / Chi nhánh / QL chi nhánh (DV-AD-TONG, DV-AD-CN, DV-AD-MCN) |
| Kết nối nhiều hãng cùng lúc | ✅ | hỗ trợ 12 đơn vị (gồm "Giao hàng tùy chỉnh" tự khai báo) |
| Nhiều tài khoản giao vận trên 1 hãng | ✅ | mỗi tài khoản gắn 1 kho lấy hàng |
| Tính phí ship trước khi đăng | ✅ | tùy chọn, xem trước phí |
| Đăng đơn lên hãng (lấy mã vận đơn) | ✅ | đơn phải có người nhận, sản phẩm, COD hợp lệ; COD lệch → bị chặn đăng |
| Hủy đăng đơn | ✅ | đơn quay về trạng thái chờ; cộng/trừ lại tồn tương ứng |
| Theo dõi trạng thái giao tự động | ✅ | cập nhật theo dữ liệu hãng trả về |
| In phiếu gửi hàng (vận đơn) hàng loạt | ✅ | |
| Đăng đơn khi token sai/hết hạn | ❌ | báo lỗi; phải nhập token mới ở màn kết nối |

## Để làm gì / cho ai

Dành cho **chủ shop, quản lý kho, nhân viên đóng gói / chốt đơn** — những người cần gửi hàng qua đơn vị vận chuyển.

Giúp bạn:
- Đăng đơn lên hãng ngay trong SCRM, không phải nhập tay lại trên web của hãng.
- Tự lấy **mã vận đơn**, **phí ship**, **tiền thu hộ (COD)** về đơn hàng.
- Theo dõi trạng thái giao hàng (đã lấy, đang giao, giao thành công, hoàn...) cập nhật tự động.
- In phiếu gửi hàng (vận đơn) hàng loạt.

## Các hãng hỗ trợ

Hiện hỗ trợ 12 đơn vị:

| # | Hãng | Ghi chú |
|---|------|---------|
| 1 | Viettel Post | |
| 2 | Giao Hàng Nhanh (GHN) | |
| 3 | J&T Express | |
| 4 | VietNam Post (VNPost) | |
| 5 | SuperShip | |
| 6 | EMS | |
| 7 | Ahamove | Giao nhanh nội thành (2 bánh / xe) |
| 8 | BEST Express | |
| 9 | Ninja Van | |
| 10 | SPX Express | |
| 11 | Giao Hàng Tiết Kiệm (GHTK) | |
| 12 | Giao hàng tùy chỉnh | Tự khai báo hãng riêng / shipper nội bộ |

> Mỗi hãng có cách lấy thông tin kết nối (token / tài khoản) khác nhau — xem mục **Cách dùng** và phần Hướng dẫn kết nối hiển thị ngay trên màn hình của từng hãng.

## Cách dùng

### 1. Kết nối hãng (làm 1 lần)
1. Vào **Kho → Kết nối đơn vị vận chuyển**, chọn hãng muốn kết nối (vd GHN).
2. Nhập thông tin kết nối hãng yêu cầu — thường là **Token / API key** hoặc **tài khoản + mật khẩu** do hãng cấp (lấy trong trang quản trị của hãng). Một số hãng (vd Ninja Van) bấm nút để **đăng nhập ủy quyền** trên trang của hãng.
3. Chọn **Kho lấy hàng / cửa hàng** (điểm hãng đến lấy hàng), phương thức lấy hàng, dịch vụ giao...
4. Bấm **Kết nối** để kiểm tra tài khoản hợp lệ, hoặc **Lưu** để lưu cấu hình.

### 2. Đăng đơn lên hãng (mỗi đơn)
1. Ở đơn hàng đã chốt, chọn **hãng** + **tài khoản giao vận** đã kết nối.
2. (Tùy chọn) Bấm **tính phí** để xem trước phí ship.
3. Bấm **Đăng đơn** → SCRM đẩy đơn sang hãng. Khi hãng nhận, đơn được gắn **mã vận đơn**, chuyển trạng thái **Đã đăng**.
4. Cần hủy thì bấm **Hủy đăng đơn** (đơn quay lại trạng thái chờ).

## Cần chú ý

- **Token/tài khoản sai hoặc hết hạn** → kết nối / đăng đơn báo lỗi. Vào lại màn hình kết nối nhập token mới.
- **Tiền thu hộ (COD)** phải khớp (tiền cuối − đặt cọc). Lệch là hệ thống chặn đăng đơn để tránh thu sai tiền khách.
- Mỗi đơn vị có thể có **nhiều tài khoản giao vận** (vd nhiều cửa hàng GHN); khi đăng đơn nhớ chọn đúng tài khoản gắn đúng kho lấy hàng.
- **Phí ship** có thể được hãng cập nhật lại sau khi đăng đơn — hệ thống tự bù chênh lệch vào phần hỗ trợ ship, không đổi tiền thu khách đã chốt.
- Hủy đăng đơn / đăng đơn thất bại sẽ **cộng/trừ lại tồn kho** tương ứng.

## Ảnh hưởng tới

- **Quản lý vận đơn / đơn hàng**: trạng thái đơn, mã vận đơn, phí ship được cập nhật.
- **Kho**: đăng đơn trừ hàng chờ xuất; hủy/thất bại cộng lại.
- **Kế toán**: đăng đơn thành công sinh phiếu bán hàng cho đơn online.
- **Lịch sử nghiệp vụ logistics**: ghi log đăng đơn / hủy / lỗi.
- **Báo cáo kho / doanh thu**: số liệu đơn đã giao phụ thuộc trạng thái hãng trả về.
