---
alias: ["tự động nhắc khách", "gửi SMS định kỳ", "gửi Zalo tự động", "nhắc lịch hẹn", "SMS chúc sinh nhật", "chiến dịch chăm sóc định kỳ", "nhắc nợ tự động", "nhắc đóng tiền cọc", "auto SMS", "job gửi tin"]
module: marketing
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: "Không thao tác trực tiếp — đây là các tác vụ chạy nền (job). Việc bật/tắt & cấu hình mẫu tin nằm ở các màn cấu hình tương ứng (Cấu hình thông báo SMS/Zalo, Chiến dịch chăm sóc KH, Đặt lịch, Đặt cọc); quyền cấu hình theo từng màn đó — cần product xác nhận chi tiết role."
phu_thuoc: ["Cấu hình SMS/Zalo (tài khoản gửi tin còn hạn, có bật gửi SMS/Zalo)", "Mẫu tin (template + biến chèn động)", "Cấu hình thông báo (bật/tắt theo từng nghiệp vụ)", "Chiến dịch chăm sóc KH (bộ lọc điều kiện khách hàng)", "Đặt lịch hẹn (bật nhắc trước ngày hẹn)", "Đặt cọc (nhắc thanh toán/trả cọc)", "Tiện ích SMS của đơn vị còn hạn"]
link_flow: ./flow.md
---

# Tác vụ tự động nhắc & gửi tin định kỳ (chăm sóc khách hàng)

## Là gì
Đây là **nhóm tác vụ chạy nền** trên hệ thống: cứ đến giờ/đến điều kiện thì hệ thống **tự gửi SMS hoặc Zalo cho khách** mà nhân viên không phải bấm tay — ví dụ nhắc khách trước ngày có lịch hẹn, chạy chiến dịch chăm sóc theo nhóm khách, nhắc khách đóng tiền khi chưa thanh toán/đến đợt trả cọc. Người dùng chỉ **bật và cấu hình** (chọn mẫu tin, đặt điều kiện, hẹn giờ) ở các màn cấu hình; phần gửi do hệ thống tự lo.

Khác với "Gửi SMS & Zalo OA" (soạn tin và bấm gửi ngay), nhóm này **tự kích hoạt theo lịch/điều kiện**, lặp lại định kỳ.

## Hành động làm được
> Đây là tính năng chạy nền, không có nút "gửi tự động" trực tiếp. Bảng dưới là **các loại nhắc/gửi tự động** hệ thống đang hỗ trợ.

| Loại nhắc / gửi tự động | Có? | Điều kiện / quyền |
|---|---|---|
| Nhắc khách trước ngày có **lịch hẹn** (SMS/Zalo) | ✅ | lịch bật cờ "gửi nhắc trước ngày hẹn"; đơn vị có bật cấu hình thông báo nhắc lịch (mã NLHDL) + tài khoản SMS/Zalo còn hiệu lực; gửi trước số phút đã cấu hình |
| Chạy **chiến dịch chăm sóc** theo nhóm khách (SMS/Zalo/voucher), 1 lần hoặc định kỳ | ✅ | tạo chiến dịch, bật sử dụng, gắn bộ lọc điều kiện khách + chọn mẫu/voucher; chọn kiểu gửi 1 lần / định kỳ (hằng ngày/tuần/tháng) |
| Nhắc khách **đóng tiền khi chưa thanh toán** (đơn cọc) | ✅ | đơn vị bật cấu hình "gửi tin khi chưa thanh toán"; chỉ gửi trong khung giờ cho phép |
| Gửi tin theo **đợt trả cọc / đợt tùy chọn** (đặt cọc) | ✅ | đợt đã khóa & có mẫu tin; gửi theo lô, có kiểm tra khung giờ Zalo |
| **SMS chúc sinh nhật khách** tự động | ⚠️ | trong code có sẵn logic nhưng **đang tắt** (đoạn lập lịch gửi sinh nhật bị vô hiệu hóa) — hiện không tự gửi; cần product xác nhận đường gửi sinh nhật thực tế |
| Tự chọn gửi **SMS hay Zalo** | ✅ | theo loại tài khoản/mẫu cấu hình (SMS hoặc Zalo OA); Zalo chỉ gửi trong khung giờ Zalo cho phép |
| Tắt toàn bộ gửi tự động cho 1 nghiệp vụ | ✅ | tắt cấu hình thông báo tương ứng, hoặc tắt tài khoản SMS/Zalo của đơn vị |

## Để làm gì / cho ai
- **Chủ shop / quản lý:** chăm sóc khách đều đặn (nhắc lịch, chúc mừng, gửi ưu đãi, nhắc nợ) mà không tốn người ngồi gửi tay; giảm khách quên lịch, tăng tỷ lệ quay lại và thu hồi công nợ.
- **Marketing / CSKH:** thiết lập chiến dịch theo nhóm khách rồi để hệ thống tự chạy theo lịch.
- **Kế toán / kho (đặt cọc):** tự nhắc khách đóng tiền đúng hạn.
- **Khách hàng:** nhận tin nhắc/chúc/ưu đãi đúng thời điểm.

## Cách dùng
Người dùng **không trực tiếp chạy job** — chỉ thiết lập rồi hệ thống tự gửi:

**Nhắc lịch hẹn trước ngày:**
1. Vào cấu hình thông báo (SMS/Zalo) → bật nhắc lịch hẹn, đặt **gửi trước bao nhiêu phút/ngày** và chọn mẫu tin.
2. Khi tạo lịch hẹn, bật tùy chọn **gửi nhắc trước ngày hẹn**.
3. Hệ thống tự gửi đúng số ngày trước hẹn; mỗi lịch chỉ gửi 1 lần.

**Chiến dịch chăm sóc định kỳ:**
1. Vào **Marketing → Chiến dịch chăm sóc khách hàng → Thêm mới**.
2. Chọn **bộ lọc điều kiện khách**, chọn mẫu SMS/Zalo (và voucher nếu có), chọn **kiểu gửi**: 1 lần hoặc định kỳ (hằng ngày/tuần/tháng + giờ gửi).
3. Bật sử dụng → đến lịch, hệ thống tự quét khách đủ điều kiện và gửi; xem lại ở tab **Lịch sử**.

**Nhắc đóng tiền / đặt cọc:** bật cấu hình "gửi tin khi chưa thanh toán" và/hoặc tạo lịch sử gửi theo đợt trả cọc; hệ thống tự nhắc theo khung giờ.

## Cần chú ý
- **Phải còn tài khoản SMS/Zalo và còn tiền/quota.** Hết quota hoặc tài khoản tắt (IsUse=false) → không có tin nào được gửi, dù đã cấu hình đúng.
- **Mẫu tin phải có sẵn và đúng placeholder.** Thiếu mẫu hoặc thiếu dữ liệu thay vào `{...}` → tin có thể không gửi hoặc dùng nội dung mặc định.
- **Số điện thoại bị che / sai** thì khách không nhận được; khách dùng số rác/không nhận tin từ tổng đài cũng không tới.
- **SMS chúc sinh nhật tự động hiện đang TẮT** trong code (đoạn lập lịch bị vô hiệu hóa) — nếu khách báo "không nhận SMS sinh nhật" thì rất có thể vì đường này chưa bật; cần product xác nhận kênh gửi sinh nhật thực tế.
- **Zalo chỉ gửi trong khung giờ cho phép** (ngoài giờ Zalo sẽ hoãn lại) — nên tin Zalo có thể không tới ngay.
- **Job chạy theo nhịp** (vài giây đến vài phút/lần) và mỗi lượt chỉ xử lý **một lô** (ví dụ 50 bản ghi) → khi tồn nhiều, tin gửi rải ra chứ không tức thời.
- **Nhắc đóng tiền cọc chỉ chạy trong khung giờ ban đêm/sáng sớm** cố định — ngoài khung đó job nghỉ.

## Ảnh hưởng tới
- **Tài khoản SMS/Zalo & số dư:** mỗi tin tự gửi đều trừ tiền/quota như gửi tay.
- **Lịch sử gửi tin (SMS/Zalo) & báo cáo:** mọi tin tự động đều ghi log, hiện trong báo cáo tin nhắn.
- **Chiến dịch & voucher:** chiến dịch định kỳ có thể tự phát voucher cho khách.
- **Lịch hẹn / đặt cọc:** trạng thái "đã gửi nhắc" được đánh dấu để không gửi trùng.
