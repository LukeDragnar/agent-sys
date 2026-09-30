---
alias: ["báo cáo doanh thu", "báo cáo thu ngân", "báo cáo bán hàng", "doanh số thu ngân", "hoa hồng nhân viên", "báo cáo doanh số nhân viên", "dashboard doanh số", "báo cáo cửa hàng", "thống kê bán hàng"]
module: cua-hang
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: "cần product xác nhận — nhóm báo cáo doanh số/thu ngân gắn tiện ích báo cáo chung và đa số có phân quyền riêng. Nhóm báo cáo chi nhánh/nhân viên gắn tiện ích chi nhánh + yêu cầu đăng nhập; một vài báo cáo chỉ cần đăng nhập (chưa gắn quyền riêng — xem dev.md)."
phu_thuoc: ["Đơn hàng đã thanh toán (nguồn dữ liệu doanh thu)", "Chi nhánh & cấu hình giới hạn khoảng thời gian tìm kiếm", "Nhân viên / nhóm nhân viên + xếp nhân viên (hoa hồng)", "Khung giờ làm việc (báo cáo chấm công)", "Dữ liệu báo cáo tổng hợp từ DB"]
link_flow: ./flow.md
---

# Báo cáo & thống kê (cửa hàng)

## Là gì
Đây là **bộ báo cáo và dashboard cho hoạt động bán hàng tại cửa hàng**: doanh thu theo ngày/chi nhánh/khách hàng/thu ngân/sản phẩm/nhân viên, báo cáo bán sản phẩm, thống kê khách dùng sản phẩm/thẻ, các widget tổng quan (tổng doanh thu, doanh số, thu-chi, tỉ lệ lấp đầy, lượng khách), cùng **báo cáo doanh số nhân viên** và **hoa hồng nhân viên áp dụng**. Tất cả đều là báo cáo **chỉ xem** — không thay đổi dữ liệu.

Hai nhóm chính:
- **Báo cáo đơn hàng / doanh thu** (gồm báo cáo thu ngân): doanh thu, bán hàng, dashboard.
- **Báo cáo chi nhánh / nhân viên** (gồm hoa hồng nhân viên, doanh số nhân viên, chấm công).

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Xem báo cáo doanh thu (ngày/chi nhánh/KH/thu ngân/SP/NV) | ✅ | quyền báo cáo; trong khoảng thời gian cho phép |
| Xem báo cáo bán sản phẩm (chi tiết / gom nhóm) | ✅ | quyền báo cáo |
| Xem báo cáo doanh thu **thu ngân** | ✅ | quyền báo cáo (có phân quyền riêng cho báo cáo thu ngân) |
| Xem dashboard tổng quan (doanh số, thu-chi, tỉ lệ lấp đầy, lượng khách) | ✅ | quyền báo cáo |
| Xem **hoa hồng nhân viên áp dụng** | ✅ | đăng nhập (báo cáo chi nhánh) |
| Xem **báo cáo doanh số nhân viên** + chi tiết 1 NV | ✅ | quyền báo cáo / đăng nhập |
| Xem báo cáo chấm công (theo chi nhánh / của chính mình) | ✅ | nhân viên xem được của chính mình |
| Lọc theo chi nhánh / khoảng ngày / SP / nhân viên | ✅ | bị giới hạn khoảng ngày theo cấu hình đơn vị |
| Sửa số liệu báo cáo | ❌ | báo cáo chỉ đọc, không chỉnh được số |

## Để làm gì / cho ai
- **Chủ shop / quản lý:** nắm doanh thu theo ngày/chi nhánh/sản phẩm, so sánh hiệu suất, theo dõi dashboard tổng quan.
- **Quản lý chi nhánh:** xem doanh số và hoa hồng của nhân viên, đánh giá hiệu quả bán.
- **Thu ngân:** xem doanh thu theo ca/thu ngân để đối chiếu.
- **Nhân viên:** xem doanh số/chấm công của chính mình.

## Cách dùng
1. Vào **Báo cáo → (Doanh thu / Cửa hàng / Chi nhánh / Nhân sự)** tùy báo cáo cần xem.
2. Chọn **bộ lọc**: chi nhánh, khoảng ngày (Từ/Đến), và tùy báo cáo còn có sản phẩm/dịch vụ, nhân viên, nhóm nhân viên, khung giờ.
3. Bấm xem → lưới số liệu/biểu đồ hiện ra (có phân trang ở các báo cáo dạng lưới).
4. Với báo cáo doanh số nhân viên: bấm vào một nhân viên để xem **chi tiết** doanh số người đó.

## Cần chú ý
- **Có giới hạn khoảng thời gian xem dữ liệu.** Nếu đơn vị đặt giới hạn (vd chỉ xem trong N ngày), chọn khoảng rộng hơn sẽ bị chặn với thông báo "Chỉ được xem dữ liệu trong khoảng N ngày".
- **Người dùng thường bị ép theo chi nhánh của mình.** Nếu không phải admin/quản trị và không chọn chi nhánh, hệ thống mặc định lấy chi nhánh của bạn.
- **Doanh thu chỉ tính đơn đã thanh toán.** Báo cáo doanh số/hoa hồng cố định lọc trạng thái đơn "Đã thanh toán".
- **Báo cáo chấm công** cần có khung giờ làm việc và nhóm nhân viên đã khai báo; thiếu sẽ báo "Không tìm thấy khung giờ/nhóm nhân viên".
- **Nhân viên chỉ xem được chấm công của chính mình** (báo cáo chấm công bản user tự ép về NV + chi nhánh của user).
- Một biểu đồ tổng quan (biểu đồ dịch vụ) hiện đang bị lỗi khi mở — đây là lỗi đã ghi nhận (xem dev.md).

## Ảnh hưởng tới
- Không ghi/sửa dữ liệu — chỉ **đọc** và tổng hợp từ đơn hàng, thu chi, nhân viên, chấm công.
- Số liệu phụ thuộc trực tiếp vào **đơn hàng đã thanh toán** và việc **xếp nhân viên** (cho hoa hồng/doanh số NV) ở màn bán hàng.
