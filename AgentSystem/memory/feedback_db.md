---
name: DB changes — script only
description: Mọi thay đổi DB chỉ gen script, không tự chạy trừ khi được yêu cầu rõ ràng
type: feedback
---

Không tự thực thi bất kỳ lệnh nào thay đổi DB (INSERT, UPDATE, DELETE, DDL).

**Why:** User muốn review script trước khi áp dụng, tránh thay đổi ngoài ý muốn.

**How to apply:** Khi cần thay đổi DB — chỉ gen script SQL và trình bày để user xem xét. Chỉ chạy trực tiếp vào DB khi user nói rõ: "chạy đi", "apply đi", "làm vào db đi", hoặc tương đương.

**TUYỆT ĐỐI KHÔNG** đụng vào DB production dưới bất kỳ hình thức nào — kể cả SELECT nếu có thể tránh được. Chỉ dùng DB dev (`dev.pushsale.vn.db`) để query/test.
