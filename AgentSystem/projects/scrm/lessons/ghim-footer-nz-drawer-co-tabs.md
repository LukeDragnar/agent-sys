# Ghim nút Lưu ở đáy `nz-drawer` có `nz-tabs` — 2 cách đều hỏng

**Session 2026-07-30 (591-3, màn `ticket-ho-tro` › drawer chi tiết ticket).**
Yêu cầu tưởng đơn giản: *"cố định nút Lưu ở cuối, đừng cho scroll"*. Thử 2 cách, cả 2 phá layout, tốn nhiều vòng qua lại vì **không tự build được** — mỗi lần sửa phải chờ user chụp màn hình.

## Cách 1 — `position: sticky` trong thân drawer → dải trắng cắt ngang giữa form

```scss
.tkx-edit-foot { position: sticky; bottom: 0; margin: 0 -24px; padding: 10px 24px; background: #fff; }
```
`margin: 0 -24px` là **đoán** padding của `.ant-drawer-body` = 24px để thanh chạy hết bề rộng. Đoán sai → thanh không phủ hết, thành **băng trắng cắt ngang giữa 2 field**.

Ngoài ra sticky neo vào **ancestor gần nhất có khung cuộn**, mà `overflow: hidden` **cũng tính là khung cuộn** (khung không cuộn được ⇒ không bao giờ "dính"). Các lớp bọc `nz-tabs` mặc định clip như vậy → phải mở `overflow: visible` cho `.ant-tabs`, `.ant-tabs-content-holder`, `.ant-tabs-content`, `.ant-tabs-tabpane`, **và** tắt `[nzAnimated]` (hiệu ứng trượt tab CẦN clip; bỏ clip mà giữ animation thì đổi tab lộ pane bên cạnh).

## Cách 2 — `[nzFooter]` của `nz-drawer` → cắt mất phần cuối tab

Footer thật thì không phải đoán padding, nhưng nội dung tab "Thông tin" (form 5 card) **bị cắt cụt phần cuối**, cuộn không tới. Thử `min-height: 0` cho `.ant-drawer-body` (fix flex-item kinh điển) + `padding-bottom` — **không ăn thua**.

## Kết luận đã chốt

**Trả nút Lưu về trong luồng nội dung** (phần tử cuối form), dạt trái. Mất tính năng ghim, đổi lại không cắt / không đè.

Giữ lại 2 thứ **không liên quan việc ghim** nhưng vẫn cần:
- `overflow: visible` cho các lớp `.ant-tabs*` (scope theo `nzWrapClassName`) — form dài trong tab vẫn bị antd clip.
- `[nzAnimated]="false"` đi kèm.

## Rút ra

- **Đừng đoán số đo nội bộ của thư viện** (padding/margin của `.ant-drawer-body`) để dựng layout — sai là lệch nhìn thấy ngay, mà sửa mò rất tốn vòng.
- Muốn ghim footer trong `nz-drawer` **có `nz-tabs`** thì phải xử **gốc chiều cao/clip của tabs** (`.ant-tabs` chạy flex đúng chiều cao, `min-height: 0`, pane tự cuộn) rồi mới đặt footer — không phải việc vá vài dòng CSS.
- Nút dạt **trái**: mép phải app có **bong bóng "Hỗ trợ"** nổi cố định, đè lên nút ở góc dưới-phải.
- Bài học quy trình: sửa UI mà **agent không build được** → mỗi lần đoán sai tốn 1 vòng chờ ảnh. Thử 1 lần không được thì **dừng, hỏi/đổi hướng**, đừng vá tiếp theo phỏng đoán.
