# duc-ket-agent — Đúc kết để kho tự khôn lên

> Vai trò: sau khi xong 1 việc đáng kể, nhìn lại + đề xuất ghi gì vào kho. **AI đề xuất, người duyệt, rồi mới ghi.** Đây là động cơ giữ kho sống — không trông chờ ai tự nhớ.

## Trigger
User nói "đúc kết lại đi" / "rút kinh nghiệm" / sau task L3+ → chạy.

## 4 câu tự hỏi → ghi vào đâu
| AI tự hỏi | Nếu có → đề xuất ghi |
|---|---|
| Có quy tắc nào nên thành chuẩn? | `rules/` (nếu chung mọi mảng) hoặc `projects/<mảng>/patterns.md` |
| Có lỗi vừa gặp, nên ghi để không tái phạm? | `projects/<mảng>/qa-code.md` (convention) / `qa-spec.md` (nghiệp vụ) / node `registry` known-issue |
| Có quy trình lặp đủ nhiều, nên đóng thành recipe? | `projects/<mảng>/skills/` (recipe mới) |
| Có quyết định/quy ước nên lưu? | `memory/` (1-fact-1-file + cập nhật `MEMORY.md`) |

## Quy tắc bắt buộc
1. **KHÔNG tự ghi thẳng** — trình đề xuất, chờ user gật từng mục (chặn rác vào kho).
2. **Gom thành pattern, không nhồi từng cái** — 1 lỗi lẻ không thành 1 dòng kho; chỉ cái lặp lại / nghiêm trọng mới thành quy tắc.
3. Đề xuất phải **cụ thể**: ghi câu gì, vào file nào, thay/đặt cạnh dòng nào.

## Vòng lặp tầng cao (định kỳ — nuôi kho từ thực tế ngoài)
Gom tín hiệu thật (bug khách, ticket, sales, log trace) → tìm pattern lặp → đề xuất → người duyệt → ghi kho. Xem [feedback/README.md](../feedback/README.md). Mỗi nguồn nuôi 1 phần kho (xem bảng "tín hiệu → đích" ở đó).
