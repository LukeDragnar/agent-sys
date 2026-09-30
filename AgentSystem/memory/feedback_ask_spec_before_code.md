---
name: Hỏi spec trước khi code (chỉ khi gọi Claude API)
description: Chỉ hỏi user viết/update spec trước khi code khi đang làm việc qua Claude API (Anthropic SDK). Khi chạy trong Claude Code CLI thì KHÔNG hỏi.
type: feedback
---

Trước khi implement task, chỉ hỏi:
> "Anh có muốn viết spec hoặc update spec trước không?"
**khi và chỉ khi** đang được gọi qua **Claude API (Anthropic SDK / `/v1/messages`)**.

**Why:** Khi chạy qua API, không có vòng lặp tương tác chặt như CLI nên cần spec rõ ràng trước để output đúng ý. Khi chạy trong Claude Code CLI, user tương tác trực tiếp realtime nên không cần bước hỏi spec — code thẳng.

**How to apply:**
- ✅ Áp dụng: khi runtime là Claude API call (Anthropic SDK, agent custom build trên API).
- ❌ KHÔNG áp dụng: khi đang chạy trong Claude Code CLI (môi trường hiện tại) — bỏ qua bước hỏi spec, vào việc luôn.
