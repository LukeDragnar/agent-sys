# Skill: Quản lý công việc (qlcv) — điểm vào

## Skill này là gì
Bộ công cụ để **quản lý công việc cá nhân trên hệ SandBox WorkFlow** ngay trong Claude:
xem việc được giao, chi tiết task, timeline, gợi ý sắp lịch, và (có gate) ghi lịch/trạng thái/gán người/tạo việc con.
Lõi là `sb_work.py` gọi thẳng API WorkFlow (`api.sandbox.com.vn`), auth bằng cookie `protoken`.

## Ai dùng
Mọi thành viên team dev SandBox. **Một bộ worker dùng chung** — không cần bản riêng cho từng người:
worker tự suy "bạn là ai" từ token (claim `id` = DEVELOPER id), nên mỗi người login bằng tài khoản của mình
là "việc của tôi" ra đúng việc người đó.

## Đọc file nào
| Bạn muốn | Đọc |
|---|---|
| Bắt đầu lần đầu (5 bước) | **`SETUP.md`** |
| Vận hành hằng ngày: khung làm việc, quy trình estimate, bản đồ source, an toàn, 17 lessons | **`SKILL.md`** |
| Chi tiết endpoint/DTO khi cần gọi API mới | **`API.md`** |
| Cheat-sheet lệnh | `SETUP.md` (bước 4) hoặc phần "Chạy worker" trong `SKILL.md` |

## Ranh giới CHUNG vs LOCAL (quan trọng)
| Loại | Ví dụ | Commit? | Ở đâu |
|---|---|---|---|
| **Dùng chung (tri thức + code)** | `sb_work.py`, `SKILL.md`, `API.md`, `SETUP.md`, `README.md`, `creds.json.template` | ✅ commit | `skills/qlcv/` |
| **Riêng mỗi người (secret + cache + nhật ký)** | `creds.json`, `token.txt`, `team.json`, `users.json`, `mytasks.json`, `timeline.json`, `WORKLOG.md`, plan `{Mã}.md` | ❌ KHÔNG commit | `memory/local/qlcv/` (gitignore) |

- 🔒 **KHÔNG BAO GIỜ commit** mật khẩu/token. `creds.json` + `token.txt` là bí mật cá nhân.
- 📓 Worklog/plan/report của bạn là cá nhân → để `memory/local/qlcv/`.
- 🧩 Muốn `assign` bằng userName cho người ngoài roster mẫu trong `sb_work.py` (TEAM) → tạo file local
  `team.json` ở thư mục DATA `memory/local/qlcv/` (không phải cạnh worker) để **đè** roster:
  ```json
  { "userabc": ["Nguyễn Văn A", "<uid-guid>"], "userxyz": ["Trần Thị B", "<uid-guid>"] }
  ```
  `team.json` đã gitignore. (Không có nó thì dùng roster mẫu, hoặc truyền thẳng `uid` guid vào lệnh assign.)

## An toàn (nhắc lại — chi tiết ở SKILL.md)
- Mặc định **CHỈ ĐỌC**. Mọi lệnh GHI (`apply`/`setxuly`/`assign --yes`/`create`) = **HARD GATE, confirm 2 lần**.
- **KHÔNG đổi Estimate** khi việc đã bắt đầu/đang làm (est gốc = mốc đo KPI).
