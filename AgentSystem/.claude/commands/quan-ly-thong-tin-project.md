Skill "Quản lý thông tin project" — SỔ HẠ TẦNG SCRM (server · IP · port · DB · Redis/Rabbit/ES · Portainer · version deploy · domain). Đọc theo thứ tự:
1. `skills/thong-tin-project/SKILL.md` — vận hành: tra cứu / bổ sung / verify + convention (đọc TRƯỚC).
2. `memory/local/infra-registry.md` — DỮ LIỆU sổ hạ tầng (LOCAL, gitignore — KHÔNG push, KHÔNG password thô).

Dùng khi user hỏi: *"X ở server/DB/port nào"*, *"con nào chạy Y"*, *"version deploy bao nhiêu"*, *"IP/DB của service Z"*, *"thông tin hạ tầng/project"*, hoặc gõ `/quan-ly-thong-tin-project`.

🔴 **GHI (bổ sung/sửa sổ) = cân nhắc + CONFIRM với user trước.** KHÔNG lưu password thô (chỉ trỏ chỗ giữ creds). Số/version/topology phải **verify từ config (appsettings) / build path thật** — đừng tin lời suông (bài học B296). Subnet: `10.0.63.x`=prod · `10.0.161.x`=staging · `10.0.61.x`=APM.

Script tra nhanh: `bash skills/thong-tin-project/infra.sh <từ khóa>`.

Task/yêu cầu: $ARGUMENTS
