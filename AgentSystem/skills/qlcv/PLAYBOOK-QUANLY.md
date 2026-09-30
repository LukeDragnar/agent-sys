# 📘 PLAYBOOK Quản lý công việc — chuẩn thế giới, lọc cho dev-lead team nhỏ (SCRM)

> **Mục đích:** Claude dùng để tư vấn/soi khi quản task; đồng thời giúp **user (thiennq) tự nâng kỹ năng quản lý**. Không bê nguyên lý thuyết — chỉ giữ cái **áp được cho 1 lead + team nhỏ làm feature/bug qua bảng qlcv**, và **neo vào sự cố thật** tuần 07/2026.
> **Cặp đôi:** file này = *nguyên lý (tại sao)*; `CHECKLIST-QUANLY-TASK.md` = *thao tác (làm gì từng bước)*.
> **Calibrate:** team nhỏ → đừng nghi thức hóa. Lấy tinh thần, bỏ bộ máy nặng (không cần story point ceremony, burndown chart cầu kỳ…).

---

## 0. Tư duy gốc — 2 câu
1. **Bảng task = mô hình của thực tế.** Trạng thái sai/cũ còn tệ hơn không có, vì ra quyết định trên bản đồ sai. → mọi thay đổi phải phản chiếu ngay.
2. **Tối ưu DÒNG CHẢY (flow), không tối ưu độ-bận (utilization).** Người bận 100% mà việc không xong-nhanh thì vô nghĩa. Đo bằng **việc XONG**, không bằng **việc đang mở**.

---

## 1. WIP & Flow — đòn bẩy số 1 (Kanban / Little's Law)
- **Little's Law:** `Thời gian hoàn thành 1 việc = Số việc đang mở (WIP) / Tốc độ xong`. Càng nhiều việc mở song song → mỗi việc càng **lâu xong**.
- **Giới hạn WIP:** 1 người nên có **1–2 việc "đang dev"**, không phải 6. Nhiều việc doing = nhảy qua lại, mất context, không cái nào xong.
- **Kéo (pull), không đẩy (push):** xong 1 mới kéo cái kế, thay vì mở sẵn 5 cái "cho có".
- 🔗 **Vấp thật:** cụm ticket có 591-3/591-5/591-6/591-7/591-8/926 cùng mở → user *"tôi hơi rối rồi"*. Đó chính là WIP tràn. → **Chọn 1–2 cái làm dứt điểm rồi mới mở tiếp.**

## 1b. San tải theo NĂNG LỰC — mỗi ngày đủ ~8h + luôn có việc tương lai (Capacity leveling)
> 🔴 **User (thiennq) chốt 15/07/2026 — nguyên tắc quản lịch CỐT LÕI của bạn ấy:**
- **Timeline = kế hoạch NĂNG LỰC, không phải "xong sớm nhất".** Mỗi **ngày làm việc ≈ 8h** tổng các task active ngày đó: **không để ngày trống** (idle), **không nhồi >8h** (quá tải ảo).
- **Luôn giữ việc ở TƯƠNG LAI** — KHÔNG dồn hết về hiện tại rồi để tương lai trống. Có việc xếp phía trước = **runway** rõ ràng (team còn việc để làm, báo cáo lên trên minh bạch).
- ⚠️ **Vì vậy: ĐỪNG "nén lịch cho gọn về sớm"** (đó là tư duy sai tôi từng đề xuất). Mục tiêu là **rải đều 8h/ngày, kéo liên tục về trước**, không tạo khoảng trống giữa chừng.
- **Cách xếp:** duyệt task theo thứ tự ưu tiên → đổ vào từng ngày làm (bỏ T7/CN) cho tới khi ngày đó đủ 8h → tràn sang ngày kế. 1 task N giờ → chiếm `ceil(N/8)` ngày (hoặc chia sẻ ngày với task khác để lấp cho đủ 8h).
- **Khi rà lịch, tự soi:** (1) có ngày nào <8h (hụt tải, lấp thêm) hay >8h (quá tải, giãn bớt)? (2) có **khoảng trống** giữa các task không (kéo việc sau lấp vào)? (3) tương lai gần còn việc xếp sẵn không (nếu cạn → cảnh báo "sắp hết việc, cần kéo task mới vào")?
- Task **blocked** (`16. Dev phân tích thêm`, chờ API/BA) → **không tính vào 8h/ngày** (nó đang chờ, không phải đang làm); để riêng, đừng để nó "giả vờ" lấp ngày.

## 2. Đường găng & Ripple — (Critical Path / dependency)
- **Timeline là chuỗi có ràng buộc.** Việc A xong mới tới B → A trượt thì B (và mọi việc sau) trượt theo. Chuỗi dài nhất quyết ngày kết dự án = **đường găng**.
- **Slack (dự trữ):** việc KHÔNG nằm trên đường găng có thể xê dịch mà không ảnh hưởng ngày kết; việc TRÊN đường găng thì không → ưu tiên bảo vệ.
- **Quy tắc dời:** giãn 1 việc ⇒ **đẩy lùi cả chuỗi phía sau**, không để chồng ngày. Trình bảng `mã: ngày cũ → ngày mới` cho từng việc bị ảnh hưởng.
- 🔗 **User đã dạy 15/07:** *"giãn timeline thì phải lùi các việc tương lai, không để im được"* — đây đúng là critical-path thinking. Đã thành Bước 3 trong checklist.
- **Phụ thuộc dữ liệu cũng là đường găng:** 591-7 (time-in-status) cấp data cho 591-6 (thống kê) → 591-7 phải **trước/song song**, làm ngược thì phải sửa lại.

## 3. Ước lượng (Estimation) — đừng lowball
- **3 điểm / PERT:** ước = `(Lạc quan + 4×Thường + Bi quan) / 6`. Buộc nghĩ tới trường hợp xấu → bớt lạc quan quá đà.
- **Reference class:** so với **việc TƯƠNG TỰ đã làm** mất bao lâu, đừng ước từ số 0. "Đơn hàng" luôn lâu hơn tưởng.
- **Buffer đặt CUỐI chuỗi, không nhồi từng việc** (Critical Chain / Goldratt): nhồi mỗi việc → **Parkinson** (việc phình cho đầy thời gian) + **student syndrome** (để nước tới chân). Gom dự trữ ở cuối, minh bạch.
- **Est gốc = mốc KPI → KHÔNG sửa khi đang doing.** Giãn thì đổi **ngày**, giữ nguyên estimate.
- 🔗 **Vấp thật:** B176 tôi ước 4h, user *"nó phải 8h chứ, đơn hàng có đơn giản đâu"* = tôi thiếu reference-class. (lesson #19)

## 4. Ưu tiên (Prioritization)
- **Value × Effort (2×2):** làm trước ô *giá trị cao – công thấp*; né *giá trị thấp – công cao*.
- **WSJF gọn** (giá trị + gấp) / công → xếp việc "ngắn mà đáng" lên trước để giải phóng dòng chảy.
- **Bug triage = Mức độ nghiêm trọng × Tần suất:** bug chặn nhiều người/mỗi ngày > bug hiếm gặp cosmetic. (cụm hóa đơn: B138/B191 đang chờ triage vậy.)
- **Eisenhower:** Gấp≠Quan trọng. Việc gấp-mà-không-quan-trọng hay nuốt mất thời gian của việc quan-trọng-không-gấp (vd nợ kỹ thuật, tài liệu).

## 4b. Thứ tự trong hàng — bug/việc-gọn TRƯỚC, feature-to/tối-ưu SAU (thiennq chốt 15/07/2026)
- **Bug + việc nhỏ (quick win) xếp TRƯỚC** — gọn, xong nhanh, giải phóng dòng chảy, khách hết đau sớm.
- **Feature to / tối ưu / thêm chức năng mới (vd 591-3 "duyệt yêu cầu", 591-5 "phân quyền ticket") đẩy RA SAU** — vì **khó, mất nhiều time**, dễ trượt; để cuối hàng khỏi chặn mấy việc nhanh.
- **Giữ việc ĐANG chạy ở đầu** dù nó là feature (đang dở thì làm cho xong, đừng bỏ giữa chừng).
- Trong cùng mức ưu tiên: **việc nhỏ trước việc to** (shortest-job-first — xong nhiều đầu việc, đỡ dồn cục).
- ⚠️ Ưu tiên field trên qlcv hay **"phẳng"** (đa số `5. Cao`) → đừng chỉ dựa field; dùng thêm tiêu chí bug-vs-feature + size như trên.

## 5. Definition of Ready / Done (cổng vào & cổng ra)
- **DoR — trước khi kéo vào dev:** spec rõ, phụ thuộc đã biết, path xác nhận, tiêu chí nghiệm thu có. → tránh việc "vào dev rồi mới phát hiện mơ hồ". (591-6/591-8 hiện **spec chưa chốt** = chưa Ready.)
- **DoD — trước khi đóng:** code + review + trace (đụng logic) + **bước DB/config tay** + verify + report cập nhật. Deploy = code **+** DB script (SCRM không có migration versioned).

## 6. Nhịp (Cadence) — team nhỏ chỉ cần 3
- **Đầu ngày (standup 1 người):** hôm qua xong gì · hôm nay làm gì · **vướng gì (blocker)**. → user đã có **report ngày**, chính là artifact này.
- **Đầu tuần (planning nhẹ):** tuần này đường găng là gì, WIP đặt bao nhiêu, việc nào phải xong.
- **Cuối việc lớn (retro/đúc kết):** vấp ở đâu → ghi kho (đã có `duc-ket-agent`). Tuần này ta đang làm đúng cái này.

## 7. Blocker & Rủi ro
- **Blocker phải NỔI, không ngồi im:** việc bị chặn (chờ người khác, chờ duyệt, chờ API) → gắn cờ + nêu, đừng để nó lặng lẽ trôi hạn. (CV3551-2 est rỗng + quá hạn 1 tuần = blocker ngầm.)
- **Nhận rủi ro sớm:** deploy-2-service (B176), cột DB tay (thu ngân) — nêu **trước khi** làm, không phải khi gãy. Rủi ro đã biết → viết vào plan/CHO-UPDATE.

## 8. Aging / Time-in-status — "việc để lâu là mùi hôi"
- Việc **nằm quá lâu ở 1 trạng thái** (chờ test, chờ duyệt, chờ update) là tín hiệu tắc — cần soi.
- 🔗 Đây **đúng là tính năng 591-7** user đang xây (lưu + hiển thị thời-gian-ở-mỗi-trạng-thái). → làm xong nó, đội có ngay chỉ số aging để tự quản. Dùng chính sản phẩm mình build để quản lý.

---

## 🎯 Để BẠN (thiennq) nâng skill — 5 thói quen, ít mà chắc
1. **Giới hạn WIP của chính mình:** tối đa 1–2 việc "đang dev". Muốn mở cái mới → hỏi "cái đang dở đã dứt chưa?".
2. **Nghĩ theo chuỗi, không theo ô:** mỗi lần đổi 1 việc, hỏi ngay *"cái này đẩy/đè cái nào?"*.
3. **Ước theo việc-tương-tự-đã-làm**, và luôn thêm buffer cho "đơn hàng / tích hợp / kho / tiền".
4. **Bảng luôn khớp thật:** cuối ngày 2 phút đồng bộ trạng thái → report tự đúng, họp không phải nhớ.
5. **Blocker nói to sớm:** việc chờ >1–2 ngày mà đứng im → escalate, đừng ôm.

## 🤝 Claude sẽ hỗ trợ bạn thế nào (chủ động, không đợi nhắc)
- **Đầu session/ngày:** tự kéo timeline + việc đang doing → tóm *"hôm nay bạn có X,Y,Z; WIP=n; đường găng là…; việc nào sắp trễ"*.
- **Mỗi thao tác task:** chạy `CHECKLIST-QUANLY-TASK.md` → tự phát hiện đè/ripple → **trình bảng dời + confirm**, không sửa lẻ.
- **Khi bạn ước lượng:** nếu thấy lowball so với việc tương tự → **nói thẳng** (kèm dẫn chứng việc cũ), không im.
- **Khi WIP phình / có blocker ngầm / việc aging lâu:** chủ động cảnh báo 1 dòng.
- **Cuối việc lớn:** rủ đúc kết → ghi kho.

> Liên quan: `CHECKLIST-QUANLY-TASK.md` · `SKILL.md` (lessons #16/#18/#19/#20) · `report-format.md` · `../../rules/quy-trinh-theo-level.md`.
