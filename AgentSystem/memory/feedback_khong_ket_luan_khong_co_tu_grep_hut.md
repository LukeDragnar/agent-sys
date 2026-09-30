---
name: Không kết luận "không có" từ grep hụt — đọc source xác minh
description: Cấm kết luận field/method/file "không tồn tại" chỉ vì 1 lần grep không khớp. Grep miss ≠ không có. Phải đọc thẳng entity/DTO/source để xác nhận trước khi nói "không có".
type: feedback
---

**KHÔNG được kết luận một field/method/file "không có / không tồn tại" chỉ vì một lần grep/search không ra.** Grep hụt ≠ không tồn tại — thường do sai pattern, sai casing, sai folder, field nằm ở entity/DTO khác, hoặc serialize đổi tên (PascalCase entity → camelCase JSON).

**Why:** Sự cố thực tế: hỏi "đơn có idChiNhanh không", tôi grep `dataDonHang.idChiNhanh` trong 1 file FE không ra → kết luận "không có field chi nhánh của đơn" và đề xuất giữ `userLogin.idChiNhanh`. Nhưng thực tế **`DonHangLogistic.cs` có `IdChiNhanh`** rõ ràng. Kết luận ẩu này dẫn tới quyết định sai (auto-fill lấy chi nhánh user thay vì chi nhánh đơn) + làm user mất niềm tin ("đoán à mà bảo không có").

**How to apply:**
- Trước khi nói "không có X": grep **nhiều biến thể** (PascalCase/camelCase, `IdChiNhanh`/`idChiNhanh`/`chiNhanhId`), tìm ở **entity/DTO/model** (không chỉ component đang đọc), và **mở thẳng file định nghĩa** đọc mắt.
- Đặc biệt field DB/entity: mở **entity class** (`*.DataContext/Entity/*.cs`) xác nhận, đừng suy từ việc 1 component FE không tham chiếu nó (component không dùng ≠ response không trả).
- Khi chưa chắc chắn đã đọc đủ: nói **"để tôi đọc source xác minh"** rồi mới kết luận — KHÔNG phán "không có" rồi mới bị bắt lỗi.
- Liên quan [[feedback_ask_spec_before_code]]: code/quyết định phải dựa trên **đọc thật**, không đoán.
