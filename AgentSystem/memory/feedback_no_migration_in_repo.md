---
name: Không push migration/store SQL vào repo
description: SQL migration + stored procedure chỉ để gen và chạy thủ công, KHÔNG commit vào repo SCRM
type: feedback
---

SCRM quản lý DB theo mô hình "code-in-repo, schema-in-DB". Migration scripts + stored procedure:
- Gen ra để user chạy thủ công trên từng môi trường (staging/prod)
- Lưu trong `memory/` hoặc gửi user trực tiếp
- **KHÔNG add vào git commit, không push lên remote**

**Why:** User đã nêu rõ rule này 2026-04-15 — "đống migrate đừng bao giờ đẩy lên". Repo dùng để deploy code, DB schema migrate manual bởi DBA/team; mix SQL vào commit gây nhầm lẫn CI/CD pipeline và khó trace version.

**How to apply:**
- Khi tạo file `.sql` (migration/store/procedure) cho SCRM → giữ ngoài tracking, add `.gitignore` nếu cần
- Trước khi `git add` → check staged files, exclude tất cả `*.sql` liên quan schema/store
- Nếu lỡ commit → revert bằng `git rm --cached` + commit mới
- File SQL có thể để trong working tree (local) hoặc lưu vào `memory/local/sql/` của agent để session sau tham khảo
