# SETUP — Bắt đầu với skill "Quản lý công việc" (5 bước)

Worker chung cho cả team. Bạn chỉ cần khai báo tài khoản của MÌNH — worker tự biết "bạn là ai"
qua token (không hardcode). Làm 1 lần, dùng mãi.

## Bước 1 — Có sẵn code
Bạn đã có repo → thư mục `skills/qlcv/` chứa `sb_work.py`, `SKILL.md`, `API.md`, file này.
Cần `python 3` (chỉ dùng thư viện chuẩn — không phải cài gì thêm).

> 📁 **Tách CODE vs DATA:**
> - **Code chung** (worker + docs + template) ở `skills/qlcv/` — commit.
> - **Data cá nhân** (creds/token/cache/worklog) ở **`memory/local/qlcv/`** — gitignore, KHÔNG commit.
> - Worker tự tạo `memory/local/qlcv/` khi chạy. Muốn để chỗ khác → set env `QLCV_DATA_DIR` = thư mục bạn chọn.

## Bước 2 — Tạo `creds.json` từ template
Copy `skills/qlcv/creds.json.template` → **`memory/local/qlcv/creds.json`** (đây là thư mục DATA, KHÔNG phải `skills/qlcv/`), điền tài khoản SandBox của bạn:
```json
{ "user": "<userName của bạn>", "pass": "<mật khẩu của bạn>" }
```
> 🔒 `memory/local/qlcv/` đã gitignore → creds/token KHÔNG bị commit. Đừng dán mật khẩu vào file nào khác.
> Cách khác (không lưu file): đặt biến môi trường `SB_USER` và `SB_PASS` trước khi chạy.
> Đổi chỗ DATA: `export QLCV_DATA_DIR=/path/khac` (Windows: `set QLCV_DATA_DIR=...`).

## Bước 3 — Chạy thử: "việc của tôi"
```bash
cd skills/qlcv
export PYTHONIOENCODING=utf-8      # Windows: set PYTHONIOENCODING=utf-8
python sb_work.py mytasks
```
Lần đầu worker sẽ **tự login** bằng creds → lưu `token.txt` (sống ~7 ngày, tự login lại khi hết hạn).
Nó **tự biết bạn là ai** bằng cách đọc claim `id` trong token → chỉ hiện việc được giao cho bạn.
Nếu ra danh sách việc của bạn = xong, mọi thứ chạy đúng.

## Bước 4 — Học lệnh (cheat-sheet ngắn)
```bash
python sb_work.py mytasks              # việc của tôi (bỏ 'Đã Update')
python sb_work.py mytasks --all        # gồm cả 'Đã Update'
python sb_work.py detail <Mã>          # chi tiết 1 việc (mô tả, estimate, người liên quan)
python sb_work.py timeline             # ngày dự kiến + estimate
python sb_work.py suggest <from-iso> <to-iso> [hours]   # GỢI Ý sắp lịch (KHÔNG ghi DB)
python sb_work.py users [keyword]      # tra id→tên user
python sb_work.py apply   <Mã> <field> <value>          # GHI 1 trường lịch (WRITE)
python sb_work.py setxuly <Mã> "<giá trị>"              # GHI trạng thái xử lý (WRITE)
python sb_work.py assign  <Mã> <userName|uid> [dev|test|ba|theodoi] [--yes]  # gán người (WRITE khi --yes)
python sb_work.py create  <parentId> "<tiêu đề>" <est> <bd> <kt> [status] [PL]  # tạo việc con (WRITE)
```
- Lệnh ĐỌC (mytasks/detail/timeline/suggest/users/tabs) + `assign` không `--yes` (PREVIEW) → chạy tự do.
- (Tuỳ chọn) Muốn `assign` bằng userName người ngoài roster ví dụ → tạo `team.json` local, xem README.

## Bước 5 — Đọc rule AN TOÀN trước khi GHI
🔴 Mọi lệnh **GHI lên hệ thống** (`apply` / `setxuly` / `assign --yes` / `create`) là **HARD GATE — CONFIRM 2 LẦN**:
1. Trình tóm tắt rõ **Mã | tiêu đề | trường/cột | giá trị CŨ → MỚI** → chờ OK lần 1.
2. Nhắc lại đúng danh sách + số lượng → chờ OK lần 2 → mới thực thi.
- Câu chung chung "làm đi / ghi đi / xếp cho tôi" **KHÔNG tính OK** — vẫn phải qua đủ 2 lần.
- 🔴 **KHÔNG đổi Estimate khi đã bắt đầu/đang làm** việc (est gốc là mốc đo KPI). Lúc doing chỉ đổi trạng thái/ngày.
- Chi tiết ranh giới + 17 lessons: đọc `SKILL.md`.
