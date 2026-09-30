---
name: CodeMap down — phải confirm trước khi fallback
description: Khi CodeMap server không phản hồi, KHÔNG tự fallback sang grep/glob — phải hỏi user trước và chỉ bỏ qua khi user nói rõ
type: feedback
---

Khi CodeMap server (`http://10.0.161.21:50888`) không phản hồi:

1. **Không tự fallback sang grep/glob.** Phải dừng lại, báo user kết quả check (ping, curl) và hỏi hướng xử lý.
2. Đưa ra các lựa chọn rõ ràng: (a) run CodeMap local, (b) user tự start lại server, (c) fallback grep/glob.
3. **Chỉ bỏ qua CodeMap / dùng grep khi user nói rõ "bỏ qua" / "fallback đi" / tương đương.** Không tự quyết.

**Why:** User muốn kiểm soát việc tìm code — CodeMap là công cụ chính, fallback tự động có thể làm task chậm hoặc sai vì grep kém chính xác hơn. Rule `CLAUDE.md` hiện tại ghi "fallback sang grep/glob" nhưng user đã override: phải confirm trước.

**How to apply:** Mỗi khi check CodeMap (ping/curl) fail → report tình trạng + hỏi + chờ user quyết. Áp dụng ngay cả khi đang giữa task, đừng vì "đỡ mất thời gian" mà tự grep luôn.
