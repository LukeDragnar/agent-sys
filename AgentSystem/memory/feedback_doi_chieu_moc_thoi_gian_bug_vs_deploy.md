---
name: Trước khi kết luận bug còn sống → đối chiếu mốc sự cố với ngày commit fix + ngày deploy
description: Bug từ ticket/khách có thể đã được người khác fix rồi; so thời điểm sự cố (NgayDangDon/log) với `git log -S` trong _SourceForBuild + lịch sử [DEPLOY] trước khi lên plan sửa, kẻo viết lại cái đã có
type: feedback
---

Nhận bug từ ticket/khách → **trước khi lên plan sửa**, đối chiếu **thời điểm sự cố xảy ra** với **thời điểm dòng code đó được sửa** và **thời điểm bản fix lên môi trường**. Bug có thể đã chết từ trước khi mình mở máy.

**Why:** Session 2026-07-28 (B262 · BigSun · Viettelpost). Ticket trễ 6 ngày, đọc như bug đang sống. Timeline thật:

```
21/07 09:52:39  đăng đơn BS00100000036 → gửi SL 20/20/1 → VTP hiện 19.160.000
21/07 15:03:11  commit 746cab8387 (nganlq) sửa đúng lỗi này
21/07 17:40:30  deploy OrderLogistic.API 4.4139 → 4.4140
```

Đơn đăng **trước bản fix 5 tiếng**. Việc thật của ticket không phải viết code mà là **verify + đóng**. Suýt lên plan sửa 2 repo cho một lỗi đã chết.

Thêm: cùng gốc đó sinh **2 mã việc** — B262 (triệu chứng "giá trị đơn sai") và B265 (triệu chứng "số lượng SP sai", cùng khách, cùng ngày, đã `15. Đã Update`). Xem [[feedback_tra_ma_task_anh_em_truoc_khi_sua]].

**How to apply:**
- Lấy mốc sự cố: `NgayDangDon` / `NgayTao` của bản ghi lỗi, hoặc timestamp trong log — **con số cụ thể**, không phải "hôm trước".
- Tìm commit đụng dòng code nghi ngờ, trong repo **build** (`_SourceForBuild`, nhánh `release_product`) chứ không phải folder dev:
  ```bash
  git log -1 --format="%ad %h %s" --date=iso -S "<đoạn code đặc trưng>" -- <path/file.cs>
  git merge-base --is-ancestor <fix> HEAD && echo "fix có trong release_product"
  ```
- Tìm bản deploy chứa fix: `git log --format="%ad %s" --date=short | rg "DEPLOY.*<Service>.API"` → so version với bản đang chạy.
- Sự cố **trước** fix → ticket = verify + đóng, ghi rõ commit/bản deploy làm bằng chứng, **không viết code mới**. Sự cố **sau** fix → hoặc fix chưa đủ, hoặc prod chạy image cũ → đi tiếp theo hướng đó.
- Đây là chiều **ngược** với [[feedback_check_deploy_truoc_khi_debug]]: cái kia là *code của mình chưa lên*, cái này là *fix của người khác đã lên rồi*.
