---
name: CodeMap — SCRM bắt buộc dùng
description: Khi hỏi về SCRM phải chạy qua codemap.sh trước, bắt buộc không ngoại lệ
type: feedback
---

Khi user hỏi bất kỳ câu gì liên quan SCRM code → BẮT BUỘC chạy qua codemap.sh trước.

**Why:** User yêu cầu rõ ràng. CodeMap tiết kiệm ~90% tokens, chính xác hơn grep.

**How to apply:**
1. Tự check backend running
2. Tự start backend nếu chưa chạy
3. Dùng `CODEMAP_PROJECT_ID="ac6f2d6d-67ef-49f0-82c7-e273cf2a4bd6" bash codemap.sh <command>`
4. Không bao giờ grep/glob SCRM code trước khi chạy codemap
5. Chỉ fallback grep khi codemap trả rỗng
