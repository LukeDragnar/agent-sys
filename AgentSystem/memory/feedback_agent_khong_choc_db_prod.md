---
name: feedback_agent_khong_choc_db_prod
description: Cả hệ thống agent TUYỆT ĐỐI không chọc DB production (kể cả SELECT); không lưu SQL/data prod vào repo agent
metadata:
  type: feedback
---

**Rule (user nhấn MẠNH nhiều lần):** Cả hệ thống agent — Claude chính VÀ mọi sub-agent spawn ra — **TUYỆT ĐỐI KHÔNG chọc DB production**: không INSERT/UPDATE/DELETE, và **không cả SELECT trực tiếp** lên prod. Lỡ spawn agent đang query prod → **KILL ngay** (TaskStop).

**Why:** (1) DB prod là bảng partition khổng lồ — SELECT nặng (>5s) có thể treo/chết hệ thống thật. (2) Rủi ro lộ creds/dữ liệu. (3) Vận hành prod là việc user/DBA, agent không đụng.

**How to apply:**
- Cần data prod → **viết SQL (chỉ SELECT / WITH(NOLOCK) / seek theo Id-PK) đưa user/DBA TỰ chạy**, agent KHÔNG tự chạy.
- SELECT nào nghi >5s → DỪNG, báo trước, không chạy mù. Lọc bằng `Id`/PK (seek), tránh lọc `MaDon`/full-scan bảng partition.
- **LƯU SQL script được** (thuần query để handoff/tái chạy) nếu user muốn — NHƯNG **tuyệt đối KHÔNG kèm user/password/creds** trong file, và hạn chế dump raw data prod. Kết quả nghiệp vụ (list đơn) giữ nếu user muốn. Điểm cấm là *chạy query lên prod từ agent*, không phải *lưu câu SQL*.
- Không tự xoá file `.sql`/`.csv` của session/dev khác (chỉ dọn cái mình tạo); thấy file prod cũ của người khác → hỏi, đừng tự xoá.
- Creds prod lỡ xuất hiện trong lệnh shell (transcript) → nhắc user **đổi mật khẩu DB** (xoá file không gỡ được khỏi transcript).

Liên quan [[feedback_prod_query_safety]], [[feedback_db]], [[feedback_no_migration_in_repo]].
