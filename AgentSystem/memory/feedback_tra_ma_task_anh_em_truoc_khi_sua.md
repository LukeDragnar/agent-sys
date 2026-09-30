---
name: Một gốc → nhiều mã việc: tra mã "anh em" theo khách + ngày trước khi sửa
description: Cùng 1 bug thường sinh nhiều ticket vì khách mô tả các triệu chứng khác nhau; tra danh sách việc theo khách + ngày ±3 + từ khóa nghiệp vụ trước khi code, tránh fix trùng hoặc fix chồng lên nhau
type: feedback
---

Nhận bug từ khách → **tra mã việc "anh em"** (cùng khách, quanh cùng thời điểm, cùng vùng nghiệp vụ) **trước khi** lên plan sửa. Một lỗi code thường sinh **nhiều ticket** vì mỗi người mô tả một triệu chứng khác nhau.

**Why:** Session 2026-07-28, cùng một dòng mapping số lượng đơn giao vận sinh ra **3 mã**:

| Mã | Tiêu đề | Triệu chứng khách thấy | Trạng thái |
|---|---|---|---|
| **B265** | Sai số lượng SP | SL trên SB là 1, trên VTP là 10 | 15. Đã Update (nganlq fix) |
| **B262** | Báo lỗi giá trị đơn hàng Viettelpost | Giá trị đơn 19.160.000 thay vì 1.180.000 | 9. Đang dev |
| **1136** | Thay đổi giá trị bảo hiểm hàng sau chiết khấu | Giá trị khai lấy trước CK | treo |

B262 và B265 cùng khách **BigSun**, cùng tạo **21/07**, cùng gốc — B265 đã fix xong thì B262 tự hết. Không tra thì hai người sửa hai lần cùng một chỗ, hoặc sửa chồng lên nhau.

**How to apply:**
- Trước khi code: tra việc theo **tên khách** + **ngày tạo ±3** + **từ khóa nghiệp vụ** (số lượng / giá trị / đơn vị / giao vận...). Có mã anh em → đọc mô tả + trạng thái + commit của nó trước.
- Mã anh em đã `Đã Update` → nhiều khả năng việc của mình chỉ còn **verify + đóng**, không phải code. Đối chiếu mốc thời gian theo [[feedback_doi_chieu_moc_thoi_gian_bug_vs_deploy]].
- Mã anh em **đang treo** → hỏi user gộp hay tách trước khi sửa, vì sửa lệch tầng dễ làm hỏng cái kia (vd đổi field `Insurance` không cứu được hãng đọc field `Price`).
- Tra bằng skill qlcv — lưu ý `CongViec/TimTheoDieuKien` **bỏ qua tham số `keyword`**, phải kéo hết rồi lọc client-side theo `tieuDe` + `extFields.DEVELOPER`. Chi tiết trong `skills/qlcv/API.md`.
- Ghi mã anh em vào plan/confirm để người sau không phải dò lại.
