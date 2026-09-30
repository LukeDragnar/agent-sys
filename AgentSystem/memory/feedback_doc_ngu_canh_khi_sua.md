---
name: Sửa code phải đọc ngữ cảnh xung quanh, không chèn rời rạc
description: Khi thêm/sửa 1 dòng, phải đọc trọn khối trước-sau và soi xem thay đổi của mình có làm dòng nào dư/mâu thuẫn/đổi nghĩa không. Không chèn rời rạc rồi để lại code thừa.
type: feedback
---

**Khi thêm/sửa code, phải đọc trọn khối xung quanh và đánh giá tác động lên các dòng kế cận — KHÔNG chèn một dòng rời rạc rồi xong.** Thay đổi của mình có thể làm dòng ngay dưới/trên trở nên **dư thừa, mâu thuẫn, hoặc đổi nghĩa**.

**Why:** Sự cố thực tế: thêm dòng log `resString` (body thô) vào `ApiGet` để fix lỗi mất log khi deserialize crash. Tôi chỉ chèn 1 dòng, không đọc kỹ khối → ngay dưới đã có dòng log `resData` (chính là `resString` parse lại) → **thành thừa từ khoảnh khắc tôi thêm**. User phải tự chỉ ra "có thấy nó thừa không?". Đáng lẽ tôi phải thấy ngay khi sửa.

**How to apply:**
- Trước/sau khi sửa: Read trọn khối (cả mấy dòng trước và sau chỗ sửa), tự hỏi *"dòng nào quanh đây giờ dư / lặp / mâu thuẫn với thay đổi của tôi?"*
- Thêm log/biến/nhánh mới → kiểm tra có **trùng** với log/biến/nhánh đã có không (vd log cùng payload 2 lần).
- Đối chiếu với hàm anh em cùng file (vd `ApiGet` vs `ApiPost`) xem có lệch chuẩn không khi mình vừa sửa.
- Liên quan [[feedback_remove_unused_code]] (xóa code dư), [[feedback_study_existing_flow]] (đọc luồng hiện có trước khi viết), [[feedback_code_comments]] (update/xóa comment stale khi sửa).
