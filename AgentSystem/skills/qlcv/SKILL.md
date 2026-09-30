# Skill "Quản lý công việc" — BỘ DÙNG CHUNG (team)

> 🔵 **Khung + tri thức dùng chung — được commit** cho cả team dùng. Ai cũng chạy chung `sb_work.py`.
> 🔒 **Riêng của mỗi người KHÔNG commit:** `creds.json` (mật khẩu), `token.txt` (phiên đăng nhập),
>    `team.json` (roster đè), cache `*.json` (users/mytasks/timeline), và mọi worklog/report/plan cá nhân
>    → để ở `memory/local/qlcv/` (đã gitignore). Xem `SETUP.md` để bắt đầu.
>
> Bộ này gồm: `sb_work.py` (worker) · `SKILL.md` (file này, vận hành) · `API.md` (tham chiếu API) ·
> `SETUP.md` (người mới) · `README.md` (điểm vào) · `creds.json.template` (mẫu). Runtime/secret sinh khi chạy → local.

## Danh tính = suy từ token (KHÔNG hardcode "tôi là ai")
Worker `me()` decode claim `id` trong JWT (`token.txt`) → ra DEVELOPER id của **chính người đang đăng nhập**.
Vì vậy 1 file worker dùng chung cho cả team: ai login bằng creds của mình thì "việc của tôi" ra đúng việc người đó.

## Khi nào chạy
User nói (hoặc biến thể): "việc của tôi", "công việc hôm nay", "hôm nay làm gì", "task tôi được giao",
"chi tiết CV...", "timeline/sắp lịch", "bắt đầu việc ...". Hoặc gõ `/quan-ly-cong-viec`.

## Chạy worker (Claude gọi qua Bash)
```bash
cd skills/qlcv                          # (hoặc thư mục chứa sb_work.py)
export PYTHONIOENCODING=utf-8
python sb_work.py mytasks              # việc của tôi (bỏ 'Đã Update')
python sb_work.py mytasks --all        # gồm cả Đã Update
python sb_work.py detail <Mã>          # chi tiết (mô tả, ưu tiên, estimate, deadline, người liên quan=TÊN)
python sb_work.py timeline             # ngày dự kiến + estimate (như Gantt /work/custom-view)
python sb_work.py suggest <from-iso> <to-iso> [hours]   # GỢI Ý sắp lịch (KHÔNG ghi DB)
python sb_work.py apply <Mã> <field> <value>   # GHI 1 trường lịch — SAU KHI user chốt
python sb_work.py setxuly <Mã> "<giá trị>"     # GHI Trạng thái xử lý — nghiệp vụ, SAU KHI user chốt
python sb_work.py assign <Mã> <userName|uid> [dev|test|ba|theodoi] [--yes]  # GÁN người vào ĐÚNG cột theo vai; KHÔNG --yes = chỉ PREVIEW (không ghi)
python sb_work.py users [keyword]      # danh mục user (id→tên)
python sb_work.py tabs                 # danh sách tab
python sb_work.py raw <path> '<json>'  # gọi API bất kỳ
```
> Mật khẩu: env `SB_PASS` hoặc `creds.json`. userName: `creds.json`(user) hoặc env `SB_USER`. Token `token.txt` sống 7 ngày, worker tự login lại khi 401.

> 🔴 **Mỗi khi user "bảo làm việc gì" / kéo việc về hôm nay / bắt đầu 1 task → BẮT BUỘC chạy `CHECKLIST-QUANLY-TASK.md`** (cùng folder). Timeline là **lịch có ràng buộc**, không phải ô rời rạc: (1) việc phải **hiện đúng trên timeline** (đủ 4 đk filter), (2) kéo về hôm nay → rà **đè việc hôm nay** + confirm, (3) việc **giãn ra → PHẢI đẩy lùi chuỗi việc tương lai**, không để chồng ngày, (4) trạng thái khớp thực tế để report chuẩn. (user chốt 15/07/2026.)

> 📘 **Nguyên lý quản lý (tại sao) + hỗ trợ user nâng skill:** `PLAYBOOK-QUANLY.md` (cùng folder) — WIP/flow, đường găng & ripple, ước lượng 3-điểm/reference-class, ưu tiên value×effort, DoR/DoD, nhịp, blocker, aging. Claude **chủ động** áp: đầu ngày trình bản đồ việc + đường găng + WIP; cảnh báo lowball/blocker/aging; rủ đúc kết cuối việc lớn.

> 📋 **Báo cáo ngày** (user nói *"report hôm nay"* / *"báo cáo ngày"*): xuất theo khuôn sheet team — **đọc `memory/local/qlcv/report-format.md`** (cột: Ngày làm · Công việc `SB-<mảng>` · Mô tả chi tiết `<Mã>: <việc> — <mốc>` · Người làm · Tiến độ % · Trạng thái `Đã Xong/Đang làm/Chưa làm` · Link Lark). Dựng nội dung từ commit/việc thật trong ngày, KHÔNG bịa %.

> 📊 **Bảng điều khiển HTML theo ngày** (user nói *"xem tổng quan"* / *"làm mới bảng điều khiển"* / muốn 1 file mở ra là thấy hết thay vì hỏi lại):
> ```bash
> python skills/bang-dieu-khien/dashboard.py   # skill riêng, xem skills/bang-dieu-khien/SKILL.md
> ```
> → sinh `memory/local/qlcv/dashboard/YYYY-MM-DD.html` + cập nhật `index.html` (danh sách ngày, mới nhất trên cùng, kèm số liệu để thấy **xu hướng**).
> - **Ai chạy ra bảng người đó** (danh tính suy từ token, không hardcode). Chạy lại trong ngày = **ghi đè đúng file ngày đó**, không đẻ trùng.
> - Layout **6 ô lấp đầy 1 màn**, mỗi ô tự cuộn bên trong → không phải scroll cả trang.
> - **3 ô máy tự dựng** (Bug tồn · Task tồn · Folder) lấy từ qlcv + quét git.
> - 🔴 **3 ô ghi tay** (Đang làm · Rủi ro · Chờ quyết) đọc từ **`memory/local/qlcv/dashboard-notes.md`** — vì đây là thứ **không API nào trả về**: việc chưa có mã task, rủi ro tự soi ra, quyết định đang chờ. **Claude cập nhật file này mỗi lần chốt gì với user**, để user chạy script lúc nào cũng ra bảng đầy đủ mà không cần Claude có mặt.
> - Code dùng chung: `dashboard.py` + `dashboard.tpl.html` (commit). Data cá nhân: output + notes ở `memory/local/qlcv/` (gitignore).

> 📦 **Handoff giao việc** (user nói *"đưa việc X cho session khác"* / *"gom 1 cục để copy"* / *"handoff X"* / *"cho mô tả để session mới làm X"*): đóng gói 1 việc thành **1 cục copy-ready** cho session code — **đọc `HANDOFF.md`** (cùng folder): detail task → khoanh Pha 1 (grep field/service) → folder (hỏi user) → kéo timeline/đang dev → sinh theo TEMPLATE → lưu `memory/local/qlcv/<mã>.md`. Handoff nhắc session code **báo tiến độ về `INBOX.md`**.

> 📨 **Trạm điều phối** (nhận báo cáo từ session code khác không phải relay tay): `INBOX.md` (code append báo cáo) + `OUTBOX.md` (quản lý chỉ đạo) + watcher nền. Quy trình xử lý xem `CHECKLIST-QUANLY-TASK.md` mục "Trạm điều phối".

## Xác thực (CHỐT — xem chi tiết API.md)
- Auth = **cookie `protoken`**, KHÔNG phải `Authorization: Bearer` (Bearer → 401).
- Login `POST auth/api/Authen/login` `{userName, passWord}` → JWT. Gọi API kèm `-b protoken=<JWT>`.
- **Login 1 lần, tái dùng token.** Login lặp → throttle vài phút.

## KHUNG LÀM VIỆC HÀNG NGÀY (Claude dẫn từng bước)
1. 🌅 **Điểm việc** — `mytasks`, nhóm theo TRANG-THAI-XU-LY, nổi bật nhóm cần làm ngay (`7. CTO đã phân tích`, `8. Dev tiếp nhận`), gợi ý ưu tiên (Cao/Khách rất cần trước). Kết: hỏi *"làm việc nào?"* — không tự chọn.
2. 🔍 **Chọn 1 việc** → `detail <Mã>` → tóm tắt mục tiêu/giải pháp/khách/estimate/deadline; làm rõ spec.
3. 📐 **Kiểm estimate** (nếu user muốn / trước khi cam kết lịch) → đọc code đánh giá effort (xem "Đánh giá ESTIMATE qua đọc code").
4. 📅 **Sắp lịch** → `suggest` (không ghi) hoặc xếp tay theo estimate đã kiểm → đề xuất cũ→mới → user chốt → `apply` từng việc.
5. 🛠️ **Bắt tay** → nối Pipeline GATED của repo: Spec→Khảo sát+Plan→Code→Review→Trace→Confirm. Plan lưu `memory/local/{Mã}.md`.
6. 🌆 **Cuối ngày** → nối `skill-bao-cao-ngay` (bảng cột sheet). Cập nhật WORKLOG.

## 📅 Timeline + cập nhật lịch (luồng chuẩn)
`mình nắm timeline → gợi ý → user CHỐT → mình call update`. Chi tiết endpoint ở API.md:
- **Gợi ý** = `TuDongSapXepThoiGian` (KHÔNG ghi DB, an toàn preview).
- **Ghi** = `CapNhatTruong` (từng field, sau khi user chốt).
- Xem view "Team BackEnd" (theo nhân sự): chỉ hiện việc status ∈ [8. Dev tiếp nhận, 9. Đang dev, 11. Dev ReOpen] + estimate≠0 + type∈[Task,Bug]. Việc "7. CTO đã phân tích" bị ẩn → cần `setxuly` sang "8. Dev tiếp nhận" khi NHẬN việc.

## 📐 Đánh giá ESTIMATE qua đọc code (trước khi xếp lịch) — QUY TRÌNH CHUẨN
Khi user muốn *"đọc code xem estimate khớp chưa"* / kiểm effort trước khi làm:
1. `detail <Mã>` cho từng việc → lấy mô tả đầy đủ.
2. **Spawn agent SONG SONG — mỗi việc 1 agent** (`general-purpose`), đọc SOURCE thật đánh giá effort. Prompt mỗi agent: mô tả task + estimate gốc + source paths + "đọc code (chỉ đọc), trả về: files `path:line`, scope thay đổi, rủi ro/cạm bẫy, ĐÁNH GIÁ estimate (số giờ + độ tự tin). Đừng viết code."
3. Gộp **bảng: est gốc vs đánh-giá-từ-code** (chênh + lý do) + phân loại **mức sẵn sàng code**.
4. Xếp timeline theo estimate ĐÃ KIỂM (8h/ngày, né T7/CN, việc blocked/spike → cuối).
5. **Ghi tất cả vào WORKLOG** (mã nào đã phân tích, estimate, files, rủi ro, spike cần) → session sau khỏi quét lại.

### 🗺️ Bản đồ source code SCRM (code THẬT ở đâu — hỏi user path mỗi session, mặc định `D:\PROJECT\Git\_SourceForBuild`)
- **BE CRM core:** `scrm_build` (module: OrderLogistic, WarehouseProduct, Order, Invoice, SMS, Report, WorkFlow...).
- **FE:** `build_scrm_frontend`.
- ⚠️ **Ecommerce logic (Shopee/Tiktok/Lazada — sync tồn/đơn/SP, đa kho): microservice RIÊNG `build_ecommerce_connect`** — KHÔNG ở scrm_build (scrm_build chỉ proxy rỗng).
- ⚠️ **Vận chuyển (J&T/SPX/GHTK — tạo đơn, GH1P): `build_delivery_connect`**; nhận trạng thái/cân-lại: `build_delivery_receive` / DeliveryStatus.
- **Nhiều báo cáo = stored procedure trong DB** (không có trong repo) → cần môi trường DB để đọc/sửa.
- **Trigger DB** (`TRG_*`) sống trong DB; repo chỉ đăng ký qua EF `HasTrigger` → body phải trích từ SQL Server.
> Task ecom/vận chuyển/report/trigger thường đụng **ngoài scrm_build** → estimate dễ thiếu nếu chỉ nhìn 1 repo. Luôn nghĩ tới microservice liên quan.

### 🎓 Bài học estimate (đợt 2026-07-04)
- Estimate gốc (CTO/BA) thường **THẤP** khi task "nhỏ" kéo theo: **cross-service / stored-proc / audit rộng / backfill data / rủi ro tiền-kho**. Đọc code trước → chỉnh đúng (đợt vừa rồi +41%: 68h→96h).
- **Phân loại sẵn sàng code:** ✅ code ngay · 🟡 làm rõ spec 30–60' · 🟠 cross-team / spike · ⛔ blocked ngoài. → Ưu tiên mở khoá nhóm 🟡 (rẻ) trước khi nhồi lịch.
- Ngược lại cũng có: task ước cao (vd 32h) có thể **thấp hơn** nếu hạ tầng đã dựng sẵn (chỉ wiring) → đọc code mới biết.

## 📓 WORKLOG — bắt buộc duy trì (mỗi người 1 file local)
`WORKLOG.md` (ở `memory/local/qlcv/`) = **nhật ký + nguồn RESUME**. Mỗi lần đụng việc → cập nhật: giai đoạn (`mới→quét-code→đánh-giá→xếp-lịch→spec→plan→code→review→trace→xong`), đã làm gì, estimate, đã ghi gì lên hệ thống. **Đầu session sau: ĐỌC WORKLOG trước** để biết mã nào đã phân tích tới đâu — đừng quét/đánh-giá lại việc đã có kết quả. (Worklog là cá nhân, KHÔNG commit.)

## Ranh giới / An toàn
- **Chỉ ĐỌC** mặc định (mytasks/detail/timeline/suggest/users/tabs).
- 🔴 **GHI lên hệ thống (`apply`/`setxuly`/`assign --yes`/`create`) = HARD GATE + CONFIRM 2 LẦN.**
  1. **Lần 1 — TRÌNH TÓM TẮT cẩn thận:** mỗi thay đổi ghi rõ **Mã | tiêu đề việc | trường/cột | giá trị CŨ → MỚI** (đủ ngữ cảnh, không viết tắt kiểu "ghi đi"). Hỏi user duyệt.
  2. **Lần 2 — XÁC NHẬN LẠI:** user gật lần 1 → **nhắc lại đúng danh sách sắp ghi + số lượng** ("chốt lần cuối: sẽ ghi N thay đổi sau…") → chờ user OK **lần 2** → MỚI execute.
  - Câu chung chung "xếp cho tôi đi / làm đi / ghi đi" **KHÔNG tính là OK** — vẫn phải qua đủ 2 lần confirm.
  - (Khác với ĐỌC/gợi ý/`assign` PREVIEW: chạy tự do. Chỉ WRITE thật mới cần 2-lần-gate.)
  - Ghi xong: báo `✅` từng dòng + nói user F5 xem. Không ghi hàng loạt ngầm, không đụng việc người khác.
  - *(Bài học: "xếp cho tôi đi" → có lần ghi thẳng 8 việc, bị nhắc "phải đợi OK". Nâng lên: "thay đổi thông tin công việc phải confirm thật cẩn thận 2 lần, tóm tắt nội dung cho cẩn thận".)*
- `setxuly` (đổi trạng thái) = **hành động nghiệp vụ** (team thấy, có thể noti) → chỉ nhận việc thật sự sắp làm.
- KHÔNG commit/chia sẻ secret. `creds.json`/`token.txt`/`team.json`/cache chỉ local (đã gitignore).

## 🔁 Session & tiếp tục việc dở
**1 việc ↔ 1 file `memory/local/{Mã}.md`** (plan + tiến độ). Tiếp việc dở ở BẤT KỲ session nào: đọc file đó + verify trạng thái hệ thống → chạy tiếp. Việc mới: 1 session mới; muốn chạy 1 mạch → user nói "làm hết đi".

## 🎓 Lessons (đúc kết)
1. **Auth = cookie `protoken`, KHÔNG Bearer** (mọi 401 ban đầu do gửi Bearer).
2. **Login 1 lần, tái dùng** — lặp → throttle.
3. **Bundle JS redeploy đổi hash** → đọc SOURCE thật (`_SourceForBuild/build_scrm_frontend` + `scrm_build/WorkFlow`) chính xác hơn; hỏi source sớm.
4. **"Ghi rồi mà không thấy" ≠ "chưa ghi"** — verify audit log (`LayLichSuCongViec`) + `detail`; view có filter riêng ẩn nó.
5. **Ghi ext-field:** `Field="ext:{id}"`, `Value=chuỗi option thẳng`. Trạng thái xử lý id `9bb25840-...`.
6. **`TuDongSapXepThoiGian` KHÔNG ghi DB** → gợi ý an toàn. **OT (ClientTimestamp) đang tắt** ở BE.
7. **Lọc server-side `tabFilter`** (`ext_<ma> equals`, `chiLayGoc:false`) nhanh hơn kéo hết; re-verify client cho chắc.
8. **Tạo task (`Save`) để lên timeline PHẢI set đủ 4 field:** DEVELOPER + TRANG-THAI-XU-LY(∈8/9/11) + estimate + PL-TASK(Task/Bug). Thiếu 1 → task tạo được nhưng ẩn timeline. Xem API.md "Save".
9. **Estimate PHẢI đọc code, ĐỪNG đoán.** User bác estimate cảm tính nhiều lần ("dựa vào đâu?", "đừng đoán, check code đi"). Guess = như estimate gốc lệch +41%. Luôn spawn agent đọc code → path:line + rủi ro.
10. **Baseline estimate quan trọng:** đọc code trên **branch PRODUCT** (`release_product`), KHÔNG phải staging/dev. Cùng 1 việc, baseline product (từ đầu) vs dev-WIP (làm nốt) chênh gấp 2–3 lần → HỎI user baseline nào trước khi chốt số.
11. **git CHỈ thao tác ở `_SourceForBuild`** (build_scrm_frontend/scrm_build...). Repo dev `_SourceForCode/Group*` = chỉ ĐỌC để estimate, KHÔNG git. FE ticket sống ở Group* (dev) nhưng bản chạy = `build_scrm_frontend@release_product`.
12. **Status hệ thống ≠ thực tế** (hay lệch): có việc code đã lên product nhưng task vẫn "Dev tiếp nhận". Trước khi kết luận việc xong/chưa → **spawn agent verify CODE**, đừng tin mỗi status.
13. **WRITE lên hệ thống = HARD GATE chờ OK** (kể cả user nói "làm đi/xếp đi"). Đọc/gợi ý thì tự do; GHI phải trình + chờ "OK".
14. **Vai quản lý KHÔNG code** — spawn session con xử lý, quản lý theo dõi (xem MANAGER — thiết kế mode quản lý, để bản local nếu team dùng).
15. **ASSIGN = mỗi VAI 1 CỘT khác nhau** (dev→`DEVELOPER`, test→`TESTER`, ba→`BA`, theodoi→`NGUOI-THEO-DOI`) — KHÔNG gán chung 1 cột. field-id lấy từ `DmCongViecMauField/TimTheoDieuKien` (mẫu BA), map trong `sb_work.ASSIGN_FIELDS`. Ghi = `CapNhatTruong Field=ext:{fid} Value=userId`; **hệ thống TỰ noti** người được gán → mình không lo thông báo. Lệnh mặc định **PREVIEW**, chỉ ghi khi `--yes` (sau confirm 2 lần). *(Value đang set uid đơn — nếu cột person là multi-select, lần ghi thật đầu tiên verify format `["uid"]`.)*
16. **CONFIRM 2 LẦN cho WRITE**: mọi thay đổi info việc → trình tóm tắt (Mã|việc|cột|cũ→mới) → OK lần 1 → nhắc lại danh sách → OK lần 2 → mới ghi. Xem "Ranh giới / An toàn".
    - 🔴 **TRẢ LỜI CỤT ≠ CONFIRM.** User gõ `"15"` / `"ok"` / `"đúng rồi"` mà **CHƯA nêu MÃ** → **KHÔNG được tự suy ra mã đích rồi ghi**. Phải hỏi lại: *"mã nào?"*. (Vi phạm thật 2026-07-14: user gõ "15", tôi tự đoán là CV3697 rồi `setxuly` — user chưa hề chỉ mã. User nhắc: *"đừng làm gì ngoài tôi yêu cầu, không là nhầm đấy"*.)
    - **Điều kiện đủ để ghi:** user nêu **MÃ** + **giá trị** rõ ràng (hoặc xác nhận đúng bảng `Mã|cột|cũ→mới` tôi vừa trình). Thiếu 1 trong 2 → **hỏi, không đoán**.
17. 🔴 **KHÔNG đổi ESTIMATE khi bắt đầu/đang doing.** Est GỐC là **căn cứ so est-vs-thực-tế** để tính **KPI/performance** — sửa est = phá mốc đo. Khi chuyển sang "9. Đang dev"/làm việc → chỉ đổi TRẠNG THÁI/ngày, **giữ nguyên Estimate**. (Est chỉ chỉnh ở pha estimate TRƯỚC khi nhận việc, không phải lúc đang làm.)
18. **Trạng thái CHA ràng buộc bởi CON:** task cha **KHÔNG thể "Đã Xong"** khi còn task con dang dở → **đừng đề xuất chuyển cha sang done** nếu chưa đóng hết con. Thêm 1 con mới đang làm (vd CV3536-2 Thu ngân) ⇒ cha CV3536 vẫn phải in-progress. (user nhắc 2026-07-10.)
19. **"Đơn hàng không đơn giản":** task đụng luồng đơn (tạo đơn/POS/thu ngân + xuất HĐĐT + mapping người mua + gate config) → **KHÔNG lowball estimate**. Parity "giống cái đã làm" vẫn phải wire lại luồng đơn → tối thiểu ~1 công (8h), đừng đoán 4h. (user bác 4h→8h, 2026-07-10.)
20. 🔴 **VIỆC ĐANG LÀM TRONG NGÀY → TỰ kéo lịch về HÔM NAY + cập nhật TRẠNG THÁI. KHÔNG đợi user nhắc.**
    - **Ngày:** việc user đang làm hôm nay mà `ngayKetThucDuKien` < hôm nay → bar Gantt nằm **tít quá khứ**, không hiện ở cột hôm nay → *"timeline hôm nay có mỗi 1 việc à"*. → `apply <Mã> ngayKetThucDuKien <hôm nay>`.
    - **Trạng thái — MAP MỐC → STATUS (nhớ kỹ, đã sai 1 lần):**
      | Mốc thực tế | Status đúng |
      |---|---|
      | Nhận việc, chưa động | `8. Dev tiếp nhận` |
      | **Đang code** | `9. Đang dev` |
      | **Đã đẩy STAGING** (dev xong, giao tester) | 🔴 **`10.2. Sẵn sàng Test`** ← KHÔNG phải "Đã Update" |
      | Tester đang test | `12.  Đang Test` ← **TESTER tự set, DEV KHÔNG đụng** |
      | Test pass, chờ đẩy product | `13. Test hoàn thành` |
      | **Đã lên PRODUCT + config xong** | `15. Đã Update` |
      *(Sai hay gặp: đẩy staging mà set `15. Đã Update` → user bác 2026-07-13: "lên staging hình như sang sẵn sàng test mà".)*
    - 🔴 **Task vào `13. Test hoàn thành` → BẮT BUỘC ghi 1 mục trong `memory/local/qlcv/CHO-UPDATE.md`**: deploy **service nào** + **config/data gì** + **verify gì**. Deploy code KHÔNG tự làm hết — bài học **B161** (deploy xong nhưng chưa chạy `DongBoQuyen` → MKT vẫn không thấy nút). Không note = **miss lúc update**. (user nhắc 2026-07-13.)
    - **Est GỐC giữ nguyên** (chỉ đổi ngày/trạng thái — xem #17).
    - Quy trình: mỗi khi user nói *"đang làm X"* / *"đang build X"* / *"X lên product rồi"* → **lập tức rà lịch+status của X**, trình `Mã|cột|cũ→mới` rồi ghi. (user nhắc 2026-07-13: *"công việc mà được làm trong ngày thì bạn phải để ý mà sắp xếp, nếu không lại ở tít tận đâu, trạng thái thì lại không update là sai đấy"*.)

## Files trong bộ
- `sb_work.py` — worker (đọc + gợi ý + ghi). Danh tính suy từ token (`me()`).
- `SKILL.md` — file này (vận hành: khung ngày + quy trình estimate + bản đồ source + an toàn + lessons).
- `API.md` — tham chiếu endpoint/DTO đầy đủ (auth, CongViec, CapNhatTruong, TuDongSapXep, master-data, tạo/đính kèm).
- `SETUP.md` — hướng dẫn người mới (5 bước): tạo creds → chạy mytasks → học lệnh → rule an toàn.
- `README.md` — điểm vào: skill là gì, ai dùng, đọc file nào, ranh giới chung-vs-local.
- `creds.json.template` — mẫu creds rỗng (copy thành `creds.json`, điền user/pass, KHÔNG commit).
- (Local, KHÔNG commit) `token.txt`/`creds.json`/`team.json`/`users.json`/`mytasks.json`/`WORKLOG.md` — auth/cache/nhật ký cá nhân.
