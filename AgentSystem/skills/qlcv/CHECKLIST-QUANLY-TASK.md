# ✅ CHECKLIST — Quản lý task (bắt buộc chạy mỗi khi user "bảo làm việc gì")

> **Nguồn:** user (thiennq) chốt 15/07/2026. Timeline **KHÔNG phải các ô rời rạc** — nó là **lịch có ràng buộc**: kéo/giãn 1 việc thì việc khác phải dịch theo. Trước đây Claude sửa từng task cô lập (kéo B176 về hôm nay, tạo 591-8 vào hôm nay) mà **không rà đè/đẩy** → sai.
>
> **Nguyên tắc gốc:** user "bảo làm X" ⇒ (1) X phải **hiện đúng trên timeline**, (2) mọi **ripple lịch** (đè hôm nay / đẩy tương lai) phải được **nêu + confirm**, (3) trạng thái luôn **khớp thực tế** để report ngày chuẩn.

---

## 🔔 CHỦ ĐỘNG nhắc "việc đang treo" — mỗi đầu ngày + mỗi báo cáo (chốt 20/07/2026)

> User (thiennq) chốt: **"sau nhớ nhắc việc đang treo nhé"** → **KHÔNG đợi user hỏi**. Mỗi khi user mở đầu ngày ("ngày mới" / "chào") hoặc xin báo cáo/tổng quan → **tự điểm danh việc treo TRƯỚC**, coi như bước mặc định của briefing.

**"Việc đang treo" = gộp các nhóm (dựa `mytasks` + `timeline`, mốc = hôm nay):**
1. **Đang dev quá hạn** — TT xử lý ∈ {`9. Đang dev`, `11. ReOpen`} mà `end < hôm nay` → hỏi *xong / trượt*.
2. **Xong-chưa-sync** — user đã báo xong nhưng status vẫn đang dev → nhắc **đổi trạng thái + ghi mốc deploy**.
3. **Bug/việc Cao trễ hạn chưa bắt đầu** — `8. Dev tiếp nhận` / `Chưa bắt đầu` mà `end ≤ hôm nay`.
4. **Việc to treo lâu** — est lớn, quá hạn nhiều ngày, chưa đụng.
5. **Started-chưa-sync** — đã bắt đầu code nhưng vẫn `8. Dev tiếp nhận` → gợi ý chuyển `9. Đang dev`.

→ Trình **gọn theo nhóm** (mã + tiêu đề ngắn) rồi hỏi *"cái nào xong để đóng status, cái nào trượt để kéo lịch"*. Không chờ user gõ mới rà.

---

## 🛰️ NHỊP NGÀY tự động — RADAR (sáng) · Watcher (ngầm) · EOD (tối) (chốt 22/07/2026)

> **Vá lỗ hổng "để việc trôi":** trước chỉ quét git/INBOX, **BỎ QUÊN quét qlcv** (việc mới + quá hạn + bug chưa gán) → sếp phải tự nhắc. Nay có công cụ deterministic + **sơ đồ team**. User (thiennq) chốt: *"hiểu bug là cực kỳ quan trọng, phải có cơ chế TÌM RA và NHẮC"* + *"việc chưa gán nhưng thuộc nghiệp vụ team cũng phải báo"* + *"cấu trúc team ai care nghiệp vụ nào phải có file riêng, đẩy git ai team nào tự ăn theo sơ đồ"*.

**Sơ đồ team — `skills/qlcv/team-map.json` (COMMIT, team-shared):**
- teams → members (userName→uid) + `domains` (keyword nghiệp vụ team ôm). Worker tự nhận team theo token→uid rồi scope đúng team đó.
- Team 2 (ThienNQ) = thiennq, nganlq, tiendv, nghiatd. Team HaiHN / DuyDV = stub (họ tự điền roster+domain khi dùng).
- `domains` = **CONFIG SỐNG, bổ sung dần qua đúc-kết** (thêm mảng mới / bỏ keyword nhiễu).

**ĐẦU NGÀY** (user "chào" / "ngày mới") → tự chạy, KHÔNG đợi hỏi:
1. `python sb_work.py radar` — quét **CẢ TEAM (roster)** + việc **CHƯA GÁN thuộc domain**:
   - **VIỆC TEAM (đã gán) = PHẢI XỬ LÝ** → quá hạn / hôm nay / mới / sắp; **bug + Cao đẩy lên đầu**.
   - **CHƯA GÁN domain = TRIAGE** → chỉ nêu MỚI ≤3 ngày (cần gán) + **đếm tồn** (không dump cả trăm). Ghi `RADAR.md`.
2. Đọc `memory/local/freshness-alert.txt` (nếu có) → kho `features/` lệch code product → đề xuất scan bù.
3. Đọc `memory/local/qlcv/FOLLOWUP.md` — **việc hẹn check ngày sau** (user dặn "mai theo dõi lại"). Có mục tới hạn hôm nay → làm + đánh `[x]`.
4. Đọc INBOX (báo cáo code) + điểm việc treo (mục 🔔 trên).

**GIỮA NGÀY (ngầm 2-3h, khi session mở):** watcher chạy `python sb_work.py radaralert` → chỉ báo **bug Cao MỚI** xuất hiện (diff `radar-seen.json`), im lặng nếu không có gì → nhắc user gán/triage.

**CUỐI NGÀY (sau khi trao đổi report với user):** `python sb_work.py eod` — đối chiếu timeline việc team vs thực tế → **ĐỀ XUẤT chỉnh** (dời ngày) cho việc quá hạn / đến-hạn-mà-chưa-làm / lịch-bắt-đầu-đã-qua. Ghi `EOD-proposals.md`.
- 🔴 **Chỉ ĐỀ XUẤT — user duyệt TỪNG cái → mới `apply <mã> <field> <YYYY-MM-DD>`** (WRITE gate, confirm). **KHÔNG đổi estimate việc đang doing** (est gốc = căn cứ KPI).
- Phân loại done theo **SỐ trạng thái** (10–15 trừ 11 = xong; <10 hoặc 11/16 = active) — KHÔNG match chuỗi cứng (biến thể dấu cách/số lẻ).

**Freshness (⚠️ LOCAL máy thiennq, KHÔNG commit):** task Windows `SCRM-FreshnessCheck` chạy `memory/local/freshness-check.ps1` nightly (02:00) → có commit `release_product` mới (Group2 BE/FE) → ghi `freshness-alert.txt`. Chết 17–18/07 (task biến mất), tạo lại 22/07. Máy tắt/nghỉ → không alert. Muốn dùng chung team = redesign riêng (script committed + mỗi dev tự đăng ký task).

---

## 📨 TRẠM ĐIỀU PHỐI — nhận báo cáo từ session CODE qua INBOX (chốt 21/07/2026)

> User muốn: **session quản lý (đây) nhận báo cáo từ các session code khác mà user KHÔNG phải relay tay.** Cơ chế: file chung + watcher.

**Hạ tầng:**
- `memory/local/qlcv/INBOX.md` — session CODE **append** báo cáo (block `## [ ] <ngày giờ> · <mã> · <Group>` + Loại DONE/BLOCKED/CẦN-QUYẾT/HỎI/PROGRESS).
- `memory/local/qlcv/OUTBOX.md` — quản lý ghi chỉ đạo/answer ngược lại; code đọc.
- **Watcher (Monitor nền)** đếm block `^## \[` trong INBOX; thêm block mới → **đánh thức session quản lý**.

**Quy trình khi watcher báo "📨 INBOX có báo cáo mới":**
1. Đọc `INBOX.md`, lấy block `[ ]` (chưa đọc).
2. Xử lý theo Loại: **DONE** → sync qlcv (setxuly, confirm mã+value) + báo user; **BLOCKED/CẦN-QUYẾT/HỎI** → escalate cho user (quản lý không tự quyết thay user việc rủi ro); **PROGRESS** → ghi nhận, kéo lịch nếu lệch.
3. Đánh dấu block `[ ]` → `[x]` (KHÔNG xoá — để nguyên số dòng, watcher đếm block không lệch).
4. Cần trả lời code → **append OUTBOX.md**.

**Giới hạn (nói thật với user):** watcher chỉ chạy khi **session này còn mở** + tốn token mỗi lần thức; latency ~20s (nhịp poll trong Monitor). Session đóng → báo cáo vẫn nằm INBOX, mở lại đọc bù. **KHÔNG đóng vai user quyết** — chỉ gom + escalate.

**Session CODE muốn báo → user dán 1 lần cho nó:** *"Có gì cần báo quản lý (xong/blocked/cần quyết) → APPEND 1 block vào `memory/local/qlcv/INBOX.md` theo format đầu file, đừng xoá dòng cũ."*

---

## 🗂️ CẤP PHÁT FOLDER — nguồn sự thật, KHÔNG re-derive từ git (chốt 21/07/2026)

> **Sai đã mắc:** giao 1046→Group6, sau đó thấy Group6 git-sạch lại đề xuất B251 vào Group6 → tự mâu thuẫn. Gốc: coi "git sạch = trống".
>
> 🔴 **git sạch ≠ TRỐNG.** Folder đã giao 1 task (kể cả mới Pha 1, chưa commit) = **RESERVED**. Đừng nhìn working tree clean rồi tưởng free.
>
> **Quy tắc:** mọi lần giao/thu hồi folder → **cập nhật `memory/local/qlcv/FOLDER-ALLOCATION.md`** (sổ cấp phát). Trước khi giao mới → **ĐỌC sổ** (không chỉ `git status`), chỉ giao ô 🟢 TRỐNG, ghi ngay thành 🔒. Task xong/hủy mới trả 🟢.
>
> 🔴 **"Làm sạch folder" = ĐỦ 3 bước, KHÔNG chỉ discard:**
> **(1) VERIFY 2 TẦNG trước khi đụng gì:**
>   - **a) Commit:** `git log origin/release_product..HEAD` = **0 commit lạc** (mọi thứ đã ở release_product → không mất code đã commit).
>   - **b) 🆕 File dirty:** **`git diff <file>` TỪNG file thay đổi + TRÌNH user xem RỒI MỚI discard** — **CẤM xoá mù theo TÊN file.** *(Chốt 23/07: đã có lần discard `appsettings.StagingDebug.json` / `environment.stagingdebug.ts` chỉ nhìn tên, không mở diff. `git checkout -- .` / `git restore .` **KHÔNG khôi phục được** — lỡ có gì quan trọng lẫn trong đó là mất luôn.)* Config debug local (StagingDebug / connection / URL) discard được, **nhưng phải xem diff xác nhận ĐÚNG là config** đã.
> **(2)** discard (`git restore .`) — sau khi đã xem diff ở bước 1b.
> **(3)** **`checkout release_product` + `merge --ff-only origin/release_product`** (về product + latest).
> Chỉ discard mà để nguyên nhánh staging/feature = **CHƯA sạch** (user chốt 21/07). File untracked của người khác (`_UnitTests/`…) KHÔNG tự xoá — hỏi trước.

---

## Khi user nói "làm việc <X>" / "kéo <X> về hôm nay" / "bắt đầu <X>" → chạy 4 bước:

### Bước 1 — TỒN TẠI & định vị
- [ ] Task **X đã có** trên qlcv chưa? → `mytasks` / `detail X` / quét chống trùng (`TimTheoDieuKien`, so tiêu đề+mô tả). Xem lesson #20 (chống trùng).
- [ ] Nếu **chưa có** → phải **tạo** (bake đủ 4 field, xem Bước 2) — KHÔNG để user "làm chay" một việc không có task.
- [ ] Nếu **đã có** → đọc: `start / end / estimate / trạng thái / dev / cha`.

### Bước 2 — HIỆN TRÊN TIMELINE (view "theo nhân sự")
Timeline chỉ vẽ khi khớp **đủ 4 điều kiện AND**:
- [ ] **DEVELOPER** = đúng người (user, hoặc người user chỉ định)
- [ ] **Trạng thái** ∈ [`8. Dev tiếp nhận`, `9. Đang dev`, `11. Dev ReOpen`]
- [ ] **estimate ≠ 0**
- [ ] **PL-TASK** ∈ [`Task`, `Bug`]  *(≠ `Module`/`Feature` → ẩn)*
- [ ] Thiếu bất kỳ cái nào → task có nhưng **KHÔNG hiện** → **nêu rõ + đề xuất chỉnh** (assign / setxuly / estimate / PL) → confirm rồi ghi.
- [ ] Muốn **cố ý ẩn** 1 việc khỏi timeline (backlog/chờ duyệt) → hạ `Trạng thái = 1. Task cần duyệt` (đừng set estimate=0 mất KPI). Ghi rõ "cố ý ẩn".

> ⚠️ **2 FIELD TRẠNG THÁI DỄ LẪN** (chốt 15/07/2026):
> - **Trạng thái xử lý** (`extFields.TRANG-THAI-XU-LY`, vd `8. Dev tiếp nhận`/`9. Đang dev`/`15. Đã Update`) — field NGHIỆP VỤ chính, `setxuly` ghi field này.
> - **TT / "Chưa bắt đầu"** (`tenTrangThai`, top-level) = tiến độ thực hiện, khác field trên.
>
> 🔴 **ĐÍNH CHÍNH 15/07/2026 — filter timeline KHÔNG phải chỉ {8,9,11}:** trước ghi "timeline lọc Trạng thái xử lý ∈ {8,9,11}, Đã Update bị ẩn" — **SAI**. Bằng chứng thực tế: task `15. Đã Update` (vd CV3536-2) **VẪN hiện** trên view "Team BackEnd". → **Đừng khẳng định task ẩn/hiện timeline theo status khi chưa verify config view thật.** Nếu task không thấy trên Gantt: kiểm **NGÀY** (nằm ngoài vùng đang xem / bị panel chi tiết che) + **refresh view** TRƯỚC, đừng đổ cho status. Config filter của view team KHÔNG trả qua API cá nhân → không tự tin đoán được, phải hỏi/kiểm UI.

### Bước 3 — RIPPLE (đây là bước HAY BỎ NHẤT — bắt buộc)
Trước khi ghi ngày, tính **va chạm**:
- [ ] **Kéo X về HÔM NAY** → liệt kê **các việc khác cũng đang ở hôm nay** của cùng người. Hôm nay gánh thêm X có **quá tải / chồng giờ** không? → **nêu danh sách việc hôm nay + confirm** (user chấp nhận chồng, hay dời bớt).
- [ ] **X giãn ra** (estimate tăng, hoặc làm lâu hơn dự kiến, hoặc start lùi) → **các việc TƯƠNG LAI phía sau phải bị đẩy lùi tương ứng** — **KHÔNG để im cho chồng ngày**. Tính chuỗi: X chiếm tới ngày N ⇒ việc kế bắt đầu ≥ N+1, và mọi việc sau nó dịch theo.
- [ ] Trình **bảng dời** rõ: `<mã> <ngày cũ → ngày mới>` cho TỪNG việc bị ảnh hưởng → **confirm** → mới `apply` từng cái.
- [ ] ⚠️ **KHÔNG đổi estimate khi task đang `doing`** (est gốc = KPI). Giãn lịch = đổi **ngày**, không đụng estimate.
- [ ] Cuối tuần / ngày nghỉ (T7/CN) → **không xếp** (bài học: "mai t7 nghỉ mà").

### Bước 4 — GHI & REPORT-READY
- [ ] Mỗi thay đổi ngày/trạng thái/assign = **1 WRITE** → confirm đích danh **mã + giá trị** trước khi ghi (lesson #16). WRITE lên hệ thống = **confirm 2 lần**.
- [ ] Sau khi ghi → trạng thái + ngày **khớp thực tế** để **report ngày theo format** chạy đúng (xem `report-format.md`). Task đụng tới trong ngày → phải nằm trong report.
- [ ] Việc vào `13. Test hoàn thành` → ghi ngay **sổ chờ-update** (`CHO-UPDATE.md`) — deploy gồm code **+ bước DB/config tay**.

---

## Tự hỏi trước khi báo "xong sắp xếp":
1. Việc user bảo làm **đã hiện trên timeline** của đúng người chưa?
2. Kéo về hôm nay có **đè** việc hôm nay nào không — đã confirm chưa?
3. Có việc nào **giãn** làm **đẩy tương lai** — đã dời hết chuỗi phía sau chưa, hay còn để chồng?
4. Trạng thái đã **khớp thực tế** để report đúng chưa?

> Bốn câu này = định-nghĩa-DONE của một thao tác quản lý task. Thiếu 1 câu = chưa xong.

---

## 🗣️ Quy ước TRÌNH BÀY cho user (chốt 15/07/2026)
- **LUÔN nhắc mã KÈM tiêu đề ngắn** — `591-8 "bắt buộc ảnh + tiêu đề 8 từ"`, KHÔNG quăng mã trần `591-8`. User không nhớ mã, mã trần = phải tra ngược = khó chịu.
- Áp cho **mọi** chỗ nói tới task: bảng, gạch đầu dòng, cảnh báo, câu hỏi confirm, bảng dời ngày. Bảng dời: `<mã> "<tiêu đề ngắn>": <ngày cũ → ngày mới>`.

Liên quan: `SKILL.md` lesson #16 (confirm WRITE) · #18 (cha-con) · #19 (đừng lowball estimate đơn hàng) · #20 (chủ động kéo việc hôm nay + status map) · `report-format.md` · `CHO-UPDATE.md`.
