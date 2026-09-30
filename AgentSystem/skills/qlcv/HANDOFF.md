# 📦 HANDOFF — Đóng gói 1 việc để giao session code khác (recipe qlcv)

> **Kích hoạt** khi user nói: *"đưa việc X cho session khác"* · *"handoff X"* · *"gom 1 cục để copy"* · *"giao việc X đi làm"* · *"tạo brief/handoff cho task X"* · *"cho tôi mô tả để session mới làm X"*.
> **Mục tiêu:** 1 cục **copy-ready** (self-contained) + lưu file, để user paste sang session code là chạy được — **không phải nhắc lại quy trình mỗi lần**.

## Quy trình (Claude TỰ làm, không đợi nhắc)
1. **Lấy detail task** từ qlcv: `python skills/qlcv/sb_work.py detail <mã>` → tiêu đề · est · ticket/issue · khách · mô tả · dev.
2. **Khoanh Pha 1** (task đụng code): grep product source (`_SourceForBuild`) tìm entity/field/service/consumer liên quan → cho **lead path:line**. Ghi rõ *"kho là bản đồ — verify với code Group hiện tại"*.
3. **Folder**: **HỎI user** hoặc dùng folder user chỉ; check group trống (`git status` các Group, phân biệt config-noise vs code thật). **KHÔNG tự đoán path.**
4. **Kéo timeline + trạng thái** nếu user muốn: `apply <mã> ngayBatDauDuKien/ngayKetThucDuKien` + `setxuly <mã> "9. Đang dev"`. WRITE gate: confirm mã+value.
5. **Sinh handoff theo TEMPLATE** → lưu `memory/local/qlcv/<mã>.md` + trình **1 CỤC copy-ready**.

## TEMPLATE handoff (các khối bắt buộc)
1. **Định danh** — mã · tiêu đề · est · ticket/issue · khách · dev · trạng thái.
2. **Folder + nhánh** — path Group (user xác nhận) + tên nhánh gợi ý `thiennq/<mã>-<slug>`.
3. **Vấn đề** — hiện trạng sai (ngắn, cụ thể).
4. **Mong muốn** — hành vi đúng.
5. **Pha 1 (đọc trước)** — entity/field/service/consumer + lead path + *"verify với code hiện tại"* + codemap.
6. **Hướng fix** — giả thuyết để verify (KHÔNG chốt mò).
7. **Hỏi trước khi code** — các điểm mơ hồ cần user chốt.
8. **Acceptance** — case số cụ thể (blueprint để trace).
9. **Quy trình bắt buộc** — confirm path → plan → **review-agent** → **trace-agent** (đụng logic/DB/field/ES) → confirm. **Báo tiến độ append `memory/local/qlcv/INBOX.md`.**

## Nguyên tắc
- **1 cục copy-ready = self-contained**: user copy trọn không cần cắt. Ghi chú riêng cho user (capacity, ripple, cảnh báo) để **NGOÀI** cục copy.
- **KHÔNG hardcode/đoán path** — user xác nhận folder (workspace nhiều bản gần giống).
- Task đụng logic → handoff **phải có acceptance** (cho trace) + nhắc review/trace.
- Handoff luôn nhắc session code **báo INBOX** → khép vòng điều phối (xem `CHECKLIST-QUANLY-TASK.md` mục Trạm điều phối).
- Lưu file `memory/local/qlcv/<mã>.md` để tái dùng/cập nhật giữa các session.

## Ví dụ đã làm
- `memory/local/qlcv/591-5.md` (sale phụ trách) · `memory/local/qlcv/1046.md` (ngày muốn nhận bị ghi đè).
