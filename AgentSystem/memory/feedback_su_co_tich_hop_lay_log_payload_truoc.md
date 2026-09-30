---
name: Sự cố "số sang đối tác sai" → LẤY LOG PAYLOAD trước, code+SQL chỉ là giả thuyết
description: Bug tích hợp (ĐVVC/sàn/đối tác) báo sai số → xin log request/response thật + hỏi rõ khách đang nhìn con số nào trên màn nào, TRƯỚC khi đọc code suy luận; đọc code + query DB chỉ dựng giả thuyết, không phải bằng chứng
type: feedback
---

Bug tích hợp dạng **"gửi sang đối tác bị sai số"** (giá trị đơn, số lượng, cân nặng, COD, phí) → việc **ĐẦU TIÊN** là lấy **log request/response thật** đã gửi sang đối tác, và hỏi rõ **khách đang nhìn con số nào, trên màn nào**. Đọc code + query DB chỉ cho ra **giả thuyết**; payload thật mới là **bằng chứng**.

**Why:** Session 2026-07-28 (B262, BigSun, Viettelpost). Chỉ với code + SQL, đã kết luận sai **2 lần**:

1. **Loại nhầm giả thuyết đúng.** User nghi số lượng quy đổi. Đọc source Group6 thấy `GenerateProduct` lấy `SoLuongBanTheoDonViQuyDoi` → tuyên bố "quy đổi chạy đúng, bị loại". Nhưng bản chạy lúc đơn được đăng là bản **cũ hơn** — log thật gửi `x20` (đơn vị lẻ) thay vì `x2`. Nghi ngờ của user đúng ngay từ đầu.
2. **Bám nhầm field.** Chăm chăm `PRODUCT_PRICE` ở header payload (giá trị bảo hiểm) và dựng cả kế hoạch sửa quanh nó, trong khi con số khách khiếu nại là **"Tổng giá trị" trên màn VTP** — do VTP tự cộng từ `LIST_ITEM` (giá × số lượng), một đường hoàn toàn khác.

Chỉ khi user đưa **log request** (`[content][{...}]`) và **ảnh màn VTP** (`Tổng giá trị: 19.160.000`) thì mới ráp được: `590.000×20 + 368.000×20 = 19.160.000` — khớp tuyệt đối, hết chỗ đoán.

**How to apply:**
- Nhận bug "số sang đối tác sai" → hỏi ngay: **log payload gửi đi có không?** + **ảnh màn bên đối tác?** Có 2 thứ đó thì thường ra nguyên nhân trong 1 lượt.
- **Hỏi khách/user con số nào là con số sai** — vận đơn của đối tác thường có nhiều ô tiền (giá trị hàng, giá trị bảo hiểm, COD, tổng cước, tổng giá trị); sửa nhầm ô = fix xong bug vẫn còn.
- Ráp số: lấy từng dòng trong payload nhân tay ra tổng, so với số trên màn. Khớp = đúng đường; không khớp = còn tầng nữa chưa thấy.
- SQL vẫn cần, nhưng vai trò là **đối chiếu** (đơn đó trong DB là bao nhiêu) chứ không phải kết luận. Mô phỏng lại công thức code bằng SQL là **ước tính**, phải nói rõ là ước tính.
- Chốt nguyên nhân bằng code mà chưa có payload → nói rõ "đây là giả thuyết", đừng trình như kết luận. Liên quan [[feedback_bao_su_that_doc_code_minh_truoc]], [[feedback_doi_chieu_moc_thoi_gian_bug_vs_deploy]].
