# CONVENTIONS — Quy ước kho tri thức

> Đọc khi thêm/sửa file trong kho. Mục tiêu: chống "loạn cào cào" — ai cũng đặt tên/để file đúng chỗ, AI tự theo.

## Đặt tên
- **File kho:** tiếng Anh-Việt thường, gạch ngang. Vd `qa-spec.md`, `export-excel.md` — KHÔNG `QA_Spec.md`/`kiemtra.md`.
- **Node registry:** `<Controller>.md`; trùng tên khác service → `<Controller>_<Service>.md`.
- **Không viết tắt tối nghĩa:** `tongTienKhuyenMai` > `ttkm`.
- **Nội dung:** tiếng Việt, ít viết tắt — non-tech đọc cũng hiểu (trừ `dev.md`/`registry` được phép kỹ thuật).

## File nào để đâu (xem CLAUDE.md "File Organization")
| Loại | Nơi |
|---|---|
| Rule chung mọi mảng | `rules/` |
| Vai trò AI | `agents/` |
| Recipe tác vụ | `projects/<mảng>/skills/` (hoặc `skills/` global nếu dùng ≥2 mảng) |
| Backbone hệ thống | `projects/<mảng>/registry/<module>/` |
| Cẩm nang tính năng (người/chatbot) | `projects/<mảng>/features/<module>/<feature>/` |
| Trí nhớ tích lũy | `memory/` (1-fact-1-file + `MEMORY.md` index) |
| Plan/trace session | `memory/local/`, `memory/local/trace/` (gitignore) |
| Khuôn thêm mới | `templates/` |
| Tín hiệu ngoài (bug khách...) | `feedback/` |

## Tách 3 loại nội dung — KHÔNG trộn
- **Mô tả** (architecture/domain/features) · **Quy tắc** (rules/patterns/qa) · **Trí nhớ** (memory).
- Lớp người-đọc (summarize/faq) tách khỏi lớp kỹ-thuật (dev/registry). Chatbot khách chỉ ăn lớp người-đọc.

## Lằn ranh rules chung vs patterns riêng
Dùng ≥2 mảng → `rules/`. Chỉ 1 mảng → `projects/<mảng>/patterns.md`. Quy tắc riêng lặp sang mảng thứ 2 → nâng lên `rules/`.

## Quy tắc UPDATE
- **KHÔNG tự cập nhật docs** sau task — chỉ khi user xác nhận ("update đi"). Xem CLAUDE.md.
- **Đổi/dời file → grep references → sửa hết → 0 link chết.** Cấm `mv` trần.
- Thêm vào kho phải qua **người duyệt** (chống rác) — xem `agents/duc-ket-agent.md`.
- File nhỏ trỏ nhau qua README, KHÔNG gộp file khổng lồ.
