---
name: Feature "không chạy" trên staging → check đã commit+push chưa TRƯỚC khi debug
description: User báo tính năng FE/BE không chạy trên môi trường deploy → việc ĐẦU TIÊN là git status/log xem code đã commit+push+deploy chưa, đừng lao vào sửa/đoán code khi bản chạy chưa có code của mình
type: feedback
---

Khi user báo tính năng **"không chạy" / "chưa thấy" / "không kéo được" / "column không hiện"** trên **môi trường ĐÃ DEPLOY** (staging/prod), việc **ĐẦU TIÊN** — TRƯỚC khi đọc lại / viết lại / đoán code — là kiểm tra: **code của mình đã commit + push + deploy lên môi trường đó CHƯA?**

`git -C <repo> status --porcelain` + `git -C <repo> log --oneline -3`. Thay đổi còn **uncommitted trong working tree** thì **KHÔNG có trên staging** (staging build **từ git branch**, không phải từ working tree của mình). Bản user đang test là bản CŨ → mọi triệu chứng "không chạy" là vì **code chưa lên đó**, không phải bug.

**Why:** Session 2026-07-24 (ticket 591-12): user báo drag-scroll "không kéo được" + cột drill "không thấy". Đã đốt **2+ lượt** viết lại directive drag-scroll (đoán sai scroll element `.ant-table-body` vs wrapper), rewrite robust, gợi ý F12... trong khi **root cause thật** là **toàn bộ thay đổi FE đang UNCOMMITTED** — staging (deploy từ git) chạy bản cũ KHÔNG có directive lẫn cột mới. BE session đó đã commit+push đúng, nhưng FE quên → user test trên FE chưa có code của mình. Chỉ cần `git status` FE ngay từ đầu là thấy, khỏi debug lầm.

**How to apply:**
- Triệu chứng "vẫn không chạy / chưa thấy" trên môi trường deploy → **git status + git log NGAY**, trước khi mở lại source đoán bug. Uncommitted = chưa lên → dừng debug, đi commit+push (confirm theo [[feedback_git]]).
- Phân biệt rõ **working tree** (edit local của mình) vs **bản deploy** (git branch staging build ra). "Tôi sửa rồi" ≠ "staging có". Chỉ code **đã push** mới lên staging-build-từ-git.
- **Hỏi user cách deploy** khi chưa chắc: build **từ working tree local** này (thì rebuild là có) hay **deploy từ git release_staging** (thì phải push trước). Đừng giả định môi trường user có sẵn thay đổi uncommitted của mình.
- Áp cho **cả FE lẫn BE** — dễ đẩy 1 phía (BE) mà **quên phía kia (FE)**; mỗi phía là **repo riêng**, phải check/push riêng.
- Screenshot môi trường staging/prod (domain tenant, URL stagingapi...) là tín hiệu: đây là bản deploy, không phải localhost → càng phải verify đã push.

Liên quan: [[feedback_build]] (user tự build), [[feedback_report_touched_paths]] (báo path đã đụng), [[feedback_staging_build]], [[feedback_git]].

**Lỗi đã mắc (2026-07-24):** rewrite directive drag-scroll 2 lần + gợi ý user chạy JS console để dò scroller, trong khi FE chưa commit → staging không hề có code. Đáng ra `git status` FE ngay lượt đầu user báo "không kéo được".
