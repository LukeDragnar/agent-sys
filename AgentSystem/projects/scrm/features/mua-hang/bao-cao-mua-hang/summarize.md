---
alias: ["báo cáo mua hàng", "bao cao mua hang", "thống kê mua hàng", "báo cáo đặt cọc", "báo cáo tồn kho", "report mua hàng"]
module: mua-hang
trang_thai: "cần product xác nhận — CHƯA có 1 màn 'Báo cáo mua hàng' tổng hợp/dedicated. Hiện chỉ có các báo cáo rời thuộc các tính năng con của Mua hàng (xem dưới)."
goi: cần product xác nhận
quyen: "cần product xác nhận — Báo cáo đặt cọc yêu cầu role DV-AD-TONG, DV-QL-KHO-TONG. Báo cáo tồn kho đơn vị: role kho/kế toán/quản lý; xuất Excel chỉ admin."
phu_thuoc: ["Quản lý đặt cọc (nguồn dữ liệu báo cáo đặt cọc)", "Quản lý tồn (dự báo) (báo cáo tồn kho đơn vị)", "Quản lý đơn đặt hàng (theo dõi đơn mua)"]
link_flow: ./flow.md
---

# Báo cáo mua hàng

> ⚠️ **Trạng thái khung:** Trong khung tính năng (INDEX) mục "Báo cáo mua hàng" đang ghi **"(chưa gate)"** — nghĩa là **chưa có một màn/controller "Báo cáo mua hàng" tổng hợp riêng**. Tài liệu này mô tả **những báo cáo rời đang có thật** trong mảng Mua hàng, và đánh dấu rõ phần chưa tồn tại để tránh hiểu nhầm. **Cần product xác nhận** phạm vi mong muốn của một báo cáo mua hàng hợp nhất.

## Là gì
"Báo cáo mua hàng" là nhóm các báo cáo phục vụ hoạt động mua/nhập hàng. Hiện tại **không có một màn tổng hợp duy nhất**; thay vào đó dữ liệu báo cáo nằm rải ở các tính năng con:

- **Báo cáo đặt cọc** (đã có, có gate): thống kê dữ liệu đặt cọc theo chi nhánh × sản phẩm (số lượng cọc / đã thanh toán / chưa thanh toán / đã giao / hoàn cọc...), kèm dòng tổng và xuất Excel. Có cơ chế giới hạn lượt truy cập chống quá tải.
- **Báo cáo tồn kho đơn vị / Dự báo hết hàng** (đã có, thuộc feature "Quản lý tồn (dự báo)"): tồn kho và sản lượng bán theo sản phẩm/chi nhánh, cảnh báo sắp hết.
- **Theo dõi đơn đặt hàng** (qua lưới Quản lý đơn đặt hàng + lọc trạng thái): hiện chưa thấy một báo cáo thống kê đơn mua hàng dedicated — **cần product xác nhận**.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Xem báo cáo đặt cọc (theo chi nhánh × SP) | ✅ | role kho tổng; có giới hạn lượt truy cập |
| Xem dòng tổng báo cáo đặt cọc | ✅ | role kho tổng |
| Xuất Excel báo cáo đặt cọc | ✅ | role kho tổng |
| Xem báo cáo tồn kho đơn vị / dự báo hết hàng | ✅ | xem ở feature "Quản lý tồn (dự báo)" |
| Xuất Excel báo cáo tồn kho đơn vị | ⚠️ | chỉ admin; ≤10.000 dòng |
| Báo cáo thống kê đơn đặt hàng (PO) dedicated | ⚠️ | chưa xác định — cần product xác nhận |
| Màn "Báo cáo mua hàng" tổng hợp riêng | ❌ | chưa có (mục INDEX ghi "chưa gate") |

## Để làm gì / cho ai
- **Quản lý kho tổng / chủ shop:** nắm tình hình đặt cọc (bao nhiêu khách đã nộp, đã giao, cần hoàn) và tình hình tồn kho để ra quyết định mua hàng.
- **Kế toán:** đối chiếu số liệu cọc và tồn.

## Cách dùng
**Báo cáo đặt cọc:**
1. Vào **Đặt cọc → Báo cáo đặt cọc**.
2. Lọc theo chi nhánh nhận, sản phẩm, đợt trả cọc, khoảng ngày; chọn gộp theo chi nhánh nếu muốn.
3. Xem lưới + dòng tổng; bấm **Xuất Excel**.

**Báo cáo tồn kho đơn vị / dự báo:** xem feature [Quản lý tồn (dự báo)](../quan-ly-ton-du-bao/summarize.md).

## Cần chú ý
- **Chưa có một màn "Báo cáo mua hàng" hợp nhất.** Nếu khách hỏi "báo cáo mua hàng ở đâu" → hướng tới Báo cáo đặt cọc và Báo cáo tồn kho đơn vị; còn báo cáo thống kê đơn đặt hàng tổng hợp thì **cần product xác nhận** đã có hay chưa.
- **Báo cáo đặt cọc có giới hạn lượt truy cập đồng thời** — khi đông người mở cùng lúc có thể báo "Chưa đến lượt truy cập" (statusCode 250), thử lại sau.
- **Số tiền hiển thị trên một số báo cáo cọc cần đối chiếu** do có lỗi tính trung bình đã biết ở phần phiếu cọc (xem feature Quản lý đặt cọc / dev).

## Ảnh hưởng tới
- Báo cáo đọc dữ liệu từ Quản lý đặt cọc và Quản lý tồn (dự báo); là báo cáo thuần đọc, không thay đổi dữ liệu nghiệp vụ.
