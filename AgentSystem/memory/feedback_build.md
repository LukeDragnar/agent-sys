---
name: User builds and runs themselves
description: User prefers to build and run the project themselves, not have Claude do it
type: feedback
---

User tự build và run project. **TUYỆT ĐỐI KHÔNG** động vào môi trường Production, Sprint, Devonline — kể cả khi user yêu cầu.

🔴 **KHÔNG BAO GIỜ tự chạy build để "verify compile"** — kể cả `dotnet build` (dù build ra output riêng/temp) lẫn `ng build` (nhất là `--max_old_space_size=16384`). **Build làm QUÁ TẢI RAM máy user.** Chỉ build khi user **CHỦ ĐỘNG cho phép** ("build đi" / "build kiểm giúp") hoặc mình **HỎI trước và user OK**.

**Why:** User nói "k cần build, tôi tự build, bạn nhớ điều này nhé" + về sau nhấn mạnh "đừng bao giờ tự build mà không có tôi cho phép, nó khiến RAM tôi quá tải". Angular build full app tốn nhiều GB RAM + 10-15 phút; dotnet build cũng nặng → tự động build = lạm dụng máy user, gây bực.
**How to apply:** Chỉ sửa code. Verify bằng **đọc lại code + `grep` sót/khớp + IDE diagnostics** (language server tự báo lỗi TS/template), KHÔNG dựa vào build. Khi xong nói "bạn build và restart lại là được" thay vì tự chạy. Cần build để chắc → **HỎI**, chờ OK.

Quy trình build/deploy staging xem ở `feedback_staging_build.md`.
