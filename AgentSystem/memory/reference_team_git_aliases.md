---
name: reference-team-git-aliases
description: Mapping git author alias → tên thật + email cho team SCRM, dùng khi trace git log
metadata:
  type: reference
---

# Git Author Alias → Tên thật (team SCRM)

Khi user nhắc tên người, dùng bảng này để build `git log --author=...` đúng. Một người có thể có nhiều alias (HaiChieu / Chiều Nguyễn Hải) vì cấu hình `user.name` khác nhau giữa máy.

## Repo: `scrm_frontend` (Group1)

| Tên thường gọi | Git author name(s) | Email | Ghi chú |
|---|---|---|---|
| Nguyễn Hải Chiều (chieu, chieunh) | `HaiChieu`, `Chiều Nguyễn Hải` | `chieunh@pushsale.vn` | Branch prefix: `chieunh/...`. Owner module QLNS (Quản Lý Nhân Sự) — `caidat-hethong/nhan-vien-custom/`, `quan-ly-tuyen-dung/`, `quan-ly-van-ban-quy-dinh/`. Đã vào staging 2025-03-21 (merge `chieunh/QLNS_commit`). |

## Cách trace cho an toàn

- Dùng email thay vì tên: `git log --all --author="chieunh@pushsale.vn"` — bắt được mọi alias.
- Nếu chỉ có tên Việt → grep cả branch prefix: `git log --all --grep="chieunh/"`.

Thêm người mới khi gặp — đừng để session sau phải dò lại.
