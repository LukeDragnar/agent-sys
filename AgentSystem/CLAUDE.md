# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## ⚠️ CRITICAL: Session Path Confirmation — KHÔNG Cache Path Across Sessions

**Rule: Mỗi session confirm path từ user, chỉ dùng cho session đó, KHÔNG lưu file chung.**

**Why:** Developers khác hoặc same dev (session khác) có thể đã thay đổi path → path lưu trong file chung sẽ sai.

### Quy tắc bắt buộc mỗi session task liên quan đến project:

Khi user nói task liên quan đến SCRM BE/FE hoặc bất kỳ project:

1. **Prompt user confirm paths:**
   ```
   Path code ở đâu?
   - WORKING_BE: ?
   - WORKING_FE: ?
   - Hay path khác?
   ```

2. **User trả lời → lưu vào SESSION CONTEXT ONLY**
   - KHÔNG lưu vào `memory/local/paths.md`
   - KHÔNG hardcode vào file chung
   - Chỉ dùng cho session hiện tại

3. **Session sau → ask lại**
   - Không dùng path từ session trước
   - User có thể đã thay đổi folder structure

> 🔴 **CHỈ sửa vào path user ĐÃ xác nhận — KHÔNG tự ĐOÁN path.** Dev muốn làm ở folder nào là **quyền của họ** (kể cả folder tên `_SourceForBak`/`*_bak`/`*backup*` — user bảo làm ở đó thì cứ làm, đừng cãi). Điều cấm là **tự chọn/đoán path rồi sửa** khi user CHƯA chỉ — vì workspace có thể chứa nhiều bản gần giống nhau, vớ nhầm thì sửa lạc chỗ mà tưởng đã xong. → Chưa rõ làm ở đâu = **HỎI**, đừng đoán.
> 🔴 **Tín hiệu "cứ code" / "làm đi" / "code đi" KHÔNG miễn confirm path + trình plan**: cho phép *làm*, nhưng vẫn phải có **path user đã xác nhận** + **trình plan/scope ngắn** trước khi sửa file. Câu nói qua loa ≠ duyệt plan đã trình.

### Template Message:

```
Bạn cho tôi biết code ở đâu để tôi thao tác:

WORKING_BE (Backend source code):
  - Path hiện tại: ?
  - Hay path khác?

WORKING_FE (Frontend source code):
  - Path hiện tại: ?
  - Hay path khác?
```

---

## ⚠️ CRITICAL: Project Paths — KHÔNG hardcode

**KHÔNG BAO GIỜ hardcode đường dẫn** trong bất kỳ file chung nào (CLAUDE.md, project.json, ...).

### Quy tắc khi task liên quan đến project

Path cá nhân của dev lưu trong `memory/local/paths.md` (folder `memory/local/` đã gitignore). Template gốc: `memory/local.template/paths.md.template`.

1. **Mỗi session task project → Bắt buộc hỏi user confirm paths**
   - KHÔNG đọc từ `memory/local/paths.md` (có thể đã cũ)
   - KHÔNG assume path từ session trước
   - **Hỏi user:** "BE code ở đâu? FE code ở đâu?"
   - User trả lời → **Lưu SESSION CONTEXT ONLY** (không lưu file)
   - Dùng path đó cho task hiện tại

2. **Path có thể thay đổi theo session/dev**
   - Dev A session 1: `/path/a/`
   - Dev A session 2: `/path/b/` (thay đổi structure)
   - Dev B: `/path/c/`
   - → **SESSION CONTEXT** làm source of truth, không file chung

3. **KHÔNG hardcode path trong file chung** (CLAUDE.md, agents/, projects/, memory/ ngoài `local/`). Dùng placeholder `{WORKING_BE}` / `{WORKING_FE}` hoặc mô tả chung ("folder working do dev chỉ định").

4. **Template: Session Confirmation**
   ```
   "Bạn cho tôi path code ở đâu?
   - WORKING_BE: ? (hoặc path khác)
   - WORKING_FE: ? (hoặc path khác)"
   ```
   → User trả lời → dùng cho session này only

5. **`memory/local/paths.md` là REFERENCE ONLY**
   - Dev có thể tự maintain nó (optional)
   - Claude KHÔNG dùng nó để skip confirmation
   - Mỗi session vẫn phải ask user

## ⚠️ Definition of DONE — Quy trình task code GATED theo pha: Claude DẪN từng bước, DỪNG chờ user OK mỗi mốc

**Rule: Claude CHỦ ĐỘNG đề xuất & dẫn từng pha; user duyệt ở MỖI mốc. Claude KHÔNG tự phóng một mạch, cũng KHÔNG tự bỏ qua pha nào.**
> - **"Claude tự dẫn"** = user không cần NHỚ/nhắc quy trình (kể cả gõ "tự check") — Claude chủ động đưa ra pha kế. KHÔNG phải Claude tự ý chạy hết không hỏi.
> - **"Dừng chờ OK" ≠ "được skip pha".** Hard-gate là để user duyệt, KHÔNG phải đường thoát để bỏ review/trace.
> - **Cách DẪN ở mỗi mốc (chủ động, không thụ động):** Claude **tự đánh giá** *"bước này có vẻ đã xong"* → **TRÌNH kết quả/bằng chứng** (đã làm gì, còn gì) → **MỜI user xác nhận** (*"đúng chưa / OK qua bước sau nhé?"*) → **CHỜ**. KHÔNG im lặng đợi user tự hỏi "xong chưa"; KHÔNG auto nhảy sang bước sau khi user chưa xác nhận. Claude là người **dẫn**, user **gật/lắc**.

### Pipeline — mỗi pha xong → DỪNG, báo, chờ user OK rồi mới qua pha sau
| Pha | Claude làm | Mốc dừng |
|---|---|---|
| 1. **Spec/Mô tả** | Hỏi "yêu cầu/mô tả đủ rõ chưa?", làm rõ điểm mơ hồ | → **chờ user OK spec** |
| 2. **Khảo sát + Plan** | OK spec rồi MỚI: định vị code (codemap/registry/feature) + lập plan `memory/local/{task}.md` | → **TRÌNH plan + trao đổi xác nhận** → chờ user OK |
| 3. **Code** | User OK plan + bảo "code" → mới sửa code | → **DỪNG: báo đã code gì (file/SQL) + tự rà "còn việc nào trong plan/scope chưa làm?" + chờ user chốt "code đủ chưa"** rồi mới qua Review |
| 4. **Review** | DẪN "giờ review nhé" → user OK → spawn `review-agent` → sửa lỗi → báo | → chờ user OK qua Trace |
| 5. **Trace/QA** | Review sạch → DẪN "giờ trace nhé" → user OK → spawn `trace-agent` → báo | → chờ user OK |
| 5b. **UT (gợi ý)** | Task đụng logic → sau Trace, DẪN *"viết/cập nhật Unit Test nhé?"* → user OK → **đọc test cũ trước** (quyết SỬA hay THÊM) → viết/sửa UT **bám Tiêu chí nghiệm thu (KHÔNG chép code)** → `dotnet test` xanh → báo. Xem `rules/unit-test.md` + `unit-test-agent`. | → chờ user OK (gợi ý, chưa hard-gate) |
| 6. **Confirm** | Đóng task: đã làm gì + trạng thái + việc còn lại | (commit là việc user) |

> 🔴 **Hard-gate KHÔNG có nghĩa được bỏ pha.** Mỗi pha Claude **PHẢI chủ động đề xuất** (kể cả review/trace) — **cấm tự dán nhãn "task đơn giản → bỏ qua"**. User OK → chạy; user chưa OK → **dừng đợi**, KHÔNG tự phóng. Bỏ pha = sai; phóng-không-hỏi = sai.
> 💡 Nếu user **đã ra lệnh gộp** ("làm hết đi" / "code + QA luôn" / "tự check") → câu đó **tính là OK** cho các pha nó bao trùm, không cần hỏi lại từng mốc. Hard-gate chỉ áp khi user **chưa** cho phép pha kế.
> 🔴 **Mỗi pha xong PHẢI DỪNG + báo + chờ user OK — CẤM tự nối sang pha sau.** Hai mốc hay vi phạm nhất (đều là lỗi):
> - **Code → Review:** code xong → DỪNG, báo đã sửa gì (file/SQL) + **tự rà còn việc nào trong scope/plan chưa xong** + để user chốt *"code đủ chưa"*. CẤM tự phán *"xong toàn bộ"* rồi nhảy thẳng review.
> - **Review → Trace:** review xong → DỪNG, báo **kết quả review** (pass / đã sửa N lỗi) + chờ user OK. CẤM *"review sạch, giờ chạy nốt trace"* tự động nối.
> → Tự nối pha = **vi phạm hard-gate** (dù QA vẫn phải làm đủ). "Dừng chờ OK" áp cho **từng boundary**, không phải chỉ trước Code.

> **Thuật ngữ — Review và Trace là 2 SKILL ĐỘC LẬP, mỗi cái BẮT BUỘC RIÊNG:**
> - **Skill 1 — Review** (`agents/review-agent.md`): check convention + spec (đọc code đã sửa).
> - **Skill 2 — Trace** (`agents/trace-agent.md`): mô phỏng data số cụ thể CHẠY QUA code, bám plan.
>
> Thứ tự: **Review → Trace → Confirm**. **BÁO CÁO TÁCH RIÊNG từng skill** — KHÔNG gộp "đã review+trace" (gộp = che được việc bỏ trace). Xong Review **≠** xong QA: **thiếu Trace là QA chưa xong**. "Confirm" = báo cáo cả 2 skill đã chạy thật.

Sau khi code xong (chi tiết **pha 4–6** của Pipeline trên), Claude DẪN theo thứ tự, **DỪNG ở mỗi mốc chờ user OK** (KHÔNG tự phóng, KHÔNG tự bỏ qua — trừ khi user đã ra lệnh gộp):

4. **Review** — đề xuất *"giờ review nhé"* → **user OK** → spawn `review-agent` (convention + spec theo `qa-code.md` / `qa-spec.md`). Lỗi tìm thấy → **sửa ngay**, báo "đã sửa N lỗi". → DỪNG chờ OK qua Trace.
5. **Trace** — review sạch → đề xuất *"giờ trace nhé"* → **user OK** → spawn `trace-agent`. Trace **bám BẢN PLAN của session** (`memory/local/{task}.md`) làm gốc "expected": verify code KHỚP plan + cover các **case/acceptance đã plan**; cộng **invariants** từ `qa-spec.md`/`domain.md` (không được phá). Mô phỏng data giả lập số cụ thể, report `memory/local/trace/`. → Vì vậy task logic **nên có plan rõ trước khi code** (plan ghi luôn scope + acceptance + edge case → chính là blueprint để trace).
6. **Confirm block (cuối) — ĐÓNG MỌI task, BẮT BUỘC** (kể cả task không đụng logic): kết thúc task nào cũng phải có block báo **đã làm gì + trạng thái + việc còn lại**. Task đụng logic → kèm **kết quả review+trace**; task chỉ đụng text → kèm **kết quả review** (trace N/A) — đều là bằng chứng đã QA. **TUYỆT ĐỐI KHÔNG kết thúc bằng menu/liệt-kê-các-bước rồi hỏi "làm bước nào tiếp"** (= đùn việc). Đây là **BÁO CÁO**, KHÔNG phải hỏi-lại-quyền-commit:
   - User **ĐÃ bảo commit/push** → đính block rồi **commit luôn**, TUYỆT ĐỐI không hỏi lại "OK commit chưa" (hỏi lại = thừa, gây bực).
   - User **CHƯA nói gì** về commit → trình block, **không tự commit** nhưng **TUYỆT ĐỐI KHÔNG hỏi/đề nghị commit**: *"muốn tôi commit không?"* / *"commit hay làm tiếp FE?"* = **SAI**. Commit là **việc của USER**, chỉ làm khi user **CHỦ ĐỘNG yêu cầu**. Confirm kết thúc bằng **trạng thái + việc còn lại**, KHÔNG bằng câu mời commit.
   - **Task CHƯA xong hết** (mới làm 1 phần — vd xong BE, thiếu FE) → ghi rõ "**Còn lại: ...**" + hỏi *có làm tiếp không*; **chưa xong thì KHÔNG nhắc commit** (chưa tới lượt).
   - Chỉ **DỪNG hỏi** khi QA phát hiện **vấn đề đáng để user quyết** (rủi ro / đổi behavior ngoài ý muốn).
   ```
   ✅ Confirm:
   • Phạm vi: <task đụng tới gì — theo plan>
   • Review (review-agent): <✅ ĐÃ SPAWN, pass | đã sửa N lỗi: ... | ❌ CHƯA chạy>   ← ghi RÕ đã spawn agent hay chưa
   • Trace  (trace-agent):  <✅ ĐÃ SPAWN, case ... → pass | ❌ CHƯA spawn>            ← chưa spawn mà code đụng logic = KHÔNG được ghi pass (phạm vi trace quyết định theo plan LÚC làm task)
   • Kết luận: <còn lại: ... (nếu chưa xong) | chờ bạn quyết vì có vấn đề: ...>   ← KHÔNG mời commit (chỉ commit khi user CHỦ ĐỘNG bảo)
   ```

> 🔴 **Review/Trace = SPAWN agent THẬT** (`review-agent` / `trace-agent`, context RIÊNG để khách quan) — **KHÔNG phải tự đọc lại code mình vừa viết (inline)**. Tự review/trace inline = thiên vị, **KHÔNG TÍNH** là QA.
> 🔴 **Confirm block phải ĐÚNG SỰ THẬT**: chỉ ghi *"đã review+trace"* khi đã **thực sự spawn** 2 agent. Nếu mới inline → **ghi rõ "mới inline, chưa spawn agent"**, đừng tô là đã QA. **Overclaim QA (ghi đã làm mà chưa) = lỗi NẶNG NHẤT** — báo cáo sai còn tệ hơn không QA. Khi user hỏi "đã review chưa" → trả lời thật ngay từ đầu, đừng để bị hỏi mới khai.
> 🔴 **`trace-agent` là bước HAY BỊ BỎ/DODGE NHẤT** (vì nặng — phải mô phỏng data số cụ thể). **Tự tay viết file `memory/local/trace/*.md` mà KHÔNG spawn `trace-agent` = GIẢ MẠO artifact**, không tính. Làm `review-agent` rồi **bỏ `trace-agent`** = QA mới một nửa. Đụng logic mà thiếu `trace-agent` thật → **QA CHƯA XONG**, **cấm** ghi confirm "đã trace".

**QA = Review + Trace, nhưng 2 cái có phạm vi KHÁC nhau (đừng gộp "chỉ logic mới QA"):**
- **REVIEW áp cho MỌI thay đổi** — kể cả sửa **text/label/đa ngôn ngữ/message/nội dung user thấy**. Vì **sai chữ = sai nghĩa**: lệch ngữ cảnh, sai key đa ngôn ngữ, label không khớp chức năng, dịch sai → vẫn hỏng. KHÔNG có "task text nhỏ nên khỏi review".
- **TRACE** = mô phỏng data số chạy QUA code → chỉ có **data để chạy khi đụng LOGIC** (BE `.cs` Service/Handler/Consumer/Repository/Controller/Domain, `.sql`/migration, FE `.ts` logic, **field/mapping/DTO/save/cột DB**, business rule/tính toán). Sửa text thuần → không có data để mô phỏng → trace N/A (nhưng **report rõ "N/A vì chỉ đụng text"**, không phải "bỏ qua").
- **Đụng logic** → **BẮT BUỘC đủ review → trace → confirm** (spawn agent thật). "Thêm/sửa 1 field" **VẪN đụng logic** (mapping + save + cột DB + list) → trace.
- ⚠️ **Phạm vi/độ sâu quyết định LÚC LÀM TASK theo PLAN đụng tới cái gì** (mục Scope / Việc-cần-làm), **KHÔNG phải agent tự dán nhãn "đơn giản" để né**. Rule KHÔNG viết sẵn bất kỳ đường "khỏi QA/review/trace" nào.

User chỉ trả lời **OK / không** — đây là backstop chính, không dùng git hook/server.

## Self-QA pipeline — câu user CŨNG có thể gọi QA (KHÔNG phải điều kiện để QA chạy)

> 🔴 **QA (review→trace) là pha BẮT BUỘC sau mọi code đụng logic** — Claude **CHỦ ĐỘNG đề xuất** từng pha, **dừng chờ user OK** (xem Pipeline ở mục "Definition of DONE"), KHÔNG đợi user gõ "tự check" mới nhớ ra. Mục này CHỈ liệt kê các câu user **CŨNG có thể** dùng để **chủ động gọi lại / gọi thêm** QA (vd việc đã làm trước đó, hoặc muốn chạy lại) — câu đó tính là **OK gộp**, chạy luôn không hỏi lại. **TUYỆT ĐỐI KHÔNG hiểu ngược là "user không nói mấy câu này thì khỏi QA"** — Claude vẫn phải tự đề xuất review/trace ở mỗi task đụng logic.

Khi user nói 1 trong các câu sau (hoặc biến thể tương đương) → chạy **TUẦN TỰ `review-agent` → `trace-agent`** (review trước + sửa lỗi, rồi mới trace), không hỏi lại:

- "tự check" / "tự check lại" / "tự check lại đi"
- "tự qa" / "tự qa lại"
- "tự review" / "review + trace"
- "tự trace" / "trace lại đi" / "trace full case"
- "tự test lại" / "tự verify lại" / "tự kiểm tra lại"
- "self check" / "self qa" / "double check đi"
- "rà lại case edge" / "soi lại"
- "chắc chưa" / "chắc chưa, check đi"

Phân vai:
- `review-agent` — check convention + spec theo `qa-code.md` / `qa-spec.md`
- `trace-agent` — trace case edge bằng data giả lập, report ra `memory/local/trace/`

Nếu context mơ hồ (không biết check cái gì) → hỏi 1 câu xác nhận rồi mới spawn.

Chi tiết từng agent: `agents/review-agent.md`, `agents/trace-agent.md`.

### Đúc kết + nuôi kho (v11)
- User nói **"đúc kết lại đi" / "rút kinh nghiệm"** (hoặc sau task L3+) → chạy `agents/duc-ket-agent.md`: tự hỏi 4 câu → **đề xuất** ghi vào `rules/`/`qa-code`/`qa-spec`/`skills/`/`memory`/`registry known-issue` → **chờ user duyệt** rồi mới ghi (chống rác).
- **Mức độ task** (việc dễ đi nhanh, việc đụng tiền/kho/multi-tenant/schema/tích hợp/RBAC đi đủ phanh): theo `rules/quy-trinh-theo-level.md` (L1–L5 + cờ leo thang).
- **Tín hiệu ngoài** (bug khách/ticket/sales/log) → `feedback/` (inbox → gom pattern → người duyệt → ghi kho). Xem `feedback/README.md`.
- Quy ước đặt tên/để file/update: `CONVENTIONS.md`. Khuôn thêm mới: `templates/`.

## Review MR — trigger
User nói **"review MR <link/#>" / "review giúp MR này" / "MR này có hỏng gì không" / "MR có an toàn merge không"** (hoặc dán link MR + nhờ review) → đọc + làm theo **`rules/review-mr.md`**:
- Lấy diff → **check base-cũ/REVERT** (nhánh feature base cũ dễ đè mất config `release_product`) → đọc **registry node** ripple → **review-agent → trace-agent** (đụng logic) → **verdict block, KHÔNG tự merge/approve**.
- Nhớ lesson **`feedback_cherrypick_feature_branch_config_gate`** (gate `formControlName`, bẫy mất-config hay gặp nhất).

## Đẩy STAGING — trigger

User nói **"đẩy staging" / "đẩy `<mã>` lên staging" / "build staging (FE|BE|`<service>`)" / "build lại image staging" / "lên staging cho tester test"** → **spawn `agents/deploy-agent.md`**, KHÔNG tự gõ lệnh build theo bản năng.

- Agent làm: rà git 2 repo staging → suy service bị đụng từ diff → **trình bảng chốt** (FE/BE/service nào) → pull `--ff-only` + verify `HEAD == origin` → **cổng confirm #2** → build → push → báo digest + **nhắc recreate stack** (tag đè `staging-1.0` nên container không tự đổi).
- Luật gốc: **`rules/build.md`**. Path build per-máy: `memory/local/deploy-staging/paths.json` (lần đầu hỏi user rồi lưu).
- 🔴 **CONFIRM 2 LẦN** · cấm gọi thẳng `.bat` (dính `pause`) · **TUYỆT ĐỐI KHÔNG** Production / Sprint / Devonline.

## Agent Learning — Tự học hỏi + tự QA

**Rule:** Khi spawn agents cho bulk fixes, **bắt buộc include:**

1. **Kinh nghiệm từ memory** — agents phải đọc relevant memory files trước khi làm việc:
   - Ví dụ: fix đa ngôn ngữ issues → agent phải đọc `pattern_angular_di_lifecycle_issue.md`
   - Ví dụ: refactor → agent phải đọc `qa-code.md` + relevant pattern files

2. **Checklist verify bắt buộc** — agents phải auto-verify trước khi báo xong:
   ```
   Checklist sau mỗi fix:
   ☐ Code structure đúng (indentation, method in class không ngoài)
   ☐ Translation keys hợp lệ (không typo)
   ☐ ngOnInit() gọi method initialize đúng
   ☐ Không có lỗi syntax
   ☐ Pattern áp dụng đúng quy tắc đã ghi nhớ
   ```

3. **Auto-review + fix** — agents tự review output trước khi báo done:
   - Nếu phát hiện lỗi → fix ngay, không chờ user chỉ ra
   - Report: lỗi tìm thấy + fixed, không phải report sau

4. **Update memory sau khi học** — nếu phát hiện pattern mới:
   - Ghi vào `memory/` ngay
   - Update `MEMORY.md` index
   - Chia sẻ với user: "Tôi đã ghi nhớ pattern X vào memory cho future work"

**Ví dụ spawn agent WITH learning:**
```
Spawn agent với prompt:
"Trước khi fix, đọc: memory/pattern_angular_di_lifecycle_issue.md
Áp dụng strict checklist:
☐ Method private, nằm TRONG class (check indentation)
☐ ngOnInit() gọi method này
☐ Không có code ngoài class boundary
After each file: tự verify, nếu sai → fix ngay"
```

## Code Search — Priority & Tools

**Tools organized in `tools/` folder** — shared for team use. See `tools/README.md` for full guide.

### Priority Order:
1. **Ripgrep** (primary) — Pattern search, file search, regex
2. **CodeMap** (secondary) — Dependencies, symbol graph
3. **Grep** (fallback) — When ripgrep unavailable

---

### 1. Ripgrep (PRIMARY)

**Location:** `tools/ripgrep/`

**Use for:**
- File pattern search: `rg --files -g "*translate*"`
- Code pattern search: `rg "this\.tsb\.t\(" --type ts`
- Regex patterns: `rg "^\s+\w+.*=.*pattern"`
- Comments: `rg "TODO.*translate"`

```bash
# From AgentSystem root
rg --config-path tools/ripgrep/.ripgreprc <pattern>

# Examples
rg --config-path tools/ripgrep/.ripgreprc "TranslateSBService" --type ts
rg --config-path tools/ripgrep/.ripgreprc --files -g "*translate*"
rg --config-path tools/ripgrep/.ripgreprc "this\.tsb\.t\(" --type ts -A 2
```

**Benefits:**
- ✅ 10-100x faster than CodeMap (local vs HTTP)
- ✅ 0 token cost
- ✅ PCRE2 regex support
- ✅ Real-time (no scan delay)

See `RIPGREP.md` for quick start, `RIPGREP_GUIDE.md` for 30+ examples.

---

### 2. CodeMap (SECONDARY)

**Location:** `tools/codemap/`

**Use for:**
- Find class/method by name: `bash tools/codemap/codemap.sh symbol "TranslateSBService"`
- Find dependencies: `bash tools/codemap/codemap.sh deps "OrderService"`
- Find endpoints: `bash tools/codemap/codemap.sh endpoint "order"`

```bash
# From AgentSystem root
bash tools/codemap/codemap.sh search <keyword>
bash tools/codemap/codemap.sh symbol <name> Class
bash tools/codemap/codemap.sh deps <ClassName>
bash tools/codemap/codemap.sh endpoint <route>
```

**When CodeMap is down:**
```bash
# Check backend status
curl -s http://10.0.161.21:50888/swagger/index.html -o /dev/null -w "%{http_code}"

# If returns 200 → use normally
# If no response → fallback to Ripgrep or Grep
```

---

### 3. Grep (FALLBACK)

Use only when Ripgrep unavailable:
```bash
grep -r "pattern" . --include="*.ts"
find . -name "*translate*"
```

---

### Test Results (verified 2026-04-24)

| Task | Ripgrep | CodeMap | Grep |
|------|---------|---------|------|
| Find class | 0.27s ✅ | ❌ (not found) | - |
| Find files | 0.27s ✅ | ❌ (no API) | 4.03s |
| Find pattern | 0.23s ✅ | ❌ (no regex) | 0.62s |
| Find refs (BE) | 52.5s | 0.83s ✅ | 35.01s |

**Recommendation:** Use Ripgrep for FE/pattern search, CodeMap for BE/dependencies.

---

**Full tool guide:** `tools/README.md`

## Multi-lang Automation (SCRM) — Always Ask First

**Trigger keywords:**
- "bổ sung multi-lang" / "bổ sung đa ngôn ngữ"
- "làm multi-lang all" / "wrap all"
- "làm multi-lang file X" / "wrap file Y"
- "bổ sung multi-lang file X" / "update file Y"

**Flow:**
1. User nói trigger keywords ↓
2. Claude dùng AskUserQuestion để ask:
   - Mode: Baseline / All / New File / Update File / Other
   - Scope (if needed): FE / BE / Both
   - File path (nếu file-specific mode)
3. User chọn → Claude spawn multi-lang-agent
4. Agent updates `projects/scrm/skills/multi-lang/baseline.md` + keys-log.md

**Rules:**
- KHÔNG tự động chạy, phải ask user trước
- Luôn đọc `projects/scrm/skills/multi-lang/README.md` khi trigger
- Baseline: Đọc từ `projects/scrm/skills/multi-lang/baseline.md`
- Output: SQL + Trace + Summary + Auto-review

Chi tiết: [projects/scrm/skills/multi-lang/](projects/scrm/skills/multi-lang/)

## Build & Run Commands

```bash
# Build the entire solution
dotnet build AgentSystem.sln

# Run the API service
dotnet run --project backend/AgentService/AgentService.csproj

# Run with HTTPS (port 7168) or HTTP (port 5137)
# Swagger UI opens automatically in development
```

## Architecture

This is a .NET 8 ASP.NET Core Web API structured in three layers:

- **AgentService** — API entry point and HTTP layer. References AgentCore.
- **AgentCore** — Core business logic. References AgentInfrastructure.
- **AgentInfrastructure** — Data access and external integrations. No project references.

Dependency direction: `AgentService → AgentCore → AgentInfrastructure`

The project is in early scaffolding stage. `AgentCore` and `AgentInfrastructure` contain only empty placeholder classes — all meaningful logic goes there as the system is built out.

## Key Configuration

- API runs at `http://localhost:5137` (HTTP) and `https://localhost:7168` (HTTPS)
- Swagger UI is enabled in development and launches by default
- `backend/AgentService/AgentService.http` contains HTTP test requests for VS Code REST Client

## Shared Memory

**QUAN TRỌNG: Toàn bộ memory phải lưu trong folder `memory/` của repo này.** KHÔNG sử dụng thư mục `.claude/` mặc định trên máy local. Mục đích: memory được đẩy lên git để cả team cùng dùng chung.

- **Đọc**: Khi bắt đầu conversation, đọc `memory/MEMORY.md` để biết danh sách memory, sau đó đọc file liên quan đến task.
- **Ghi**: Khi cần lưu memory mới hoặc cập nhật, luôn ghi vào `memory/` trong repo này.
- **Index**: Cập nhật `memory/MEMORY.md` sau mỗi lần thêm/sửa/xóa memory file.

## Skill "Quản lý công việc" (qlcv) + Đội Agent — team dùng chung

**Skill qlcv** (`skills/qlcv/`): quản lý công việc SCRM qua API (xem việc/timeline, đánh giá estimate, xếp lịch, assign, báo cáo ngày). Kích hoạt khi user nói *"việc của tôi"*, *"hôm nay làm gì"*, *"timeline/sắp lịch"*, *"báo cáo ngày"*... hoặc `/quan-ly-cong-viec`.
- Đọc `skills/qlcv/SKILL.md` (vận hành: khung ngày + estimate + bản đồ source + an toàn) → chạy worker `skills/qlcv/sb_work.py`. Onboarding người mới: `skills/qlcv/SETUP.md`.
- **Worker tự nhận diện người dùng qua token** — ai login nấy ra việc của mình (KHÔNG hardcode ai). **DATA cá nhân** (creds/token/cache/worklog) ở `memory/local/qlcv/` (gitignore, per-người); lần đầu chưa có creds → hỏi user user/pass SCRM để tạo `memory/local/qlcv/creds.json` (không commit).
- 🔴 WRITE lên hệ thống (`apply`/`setxuly`/`assign`/`create`) = **CONFIRM 2 LẦN**; **KHÔNG đổi estimate khi đang doing** (est gốc = căn cứ KPI). Chi tiết trong SKILL.md.

**Đội Agent** (`agents/`): điểm vào `agents/README.md` · sơ đồ tổ chức `agents/TEAM.md` (tổ Lab R&D · QA · Thực thi) · nhật ký phát triển `agents/CHANGELOG.md`. Lab R&D theo triết lý **hấp thụ→tự xây** (không bê tool theo hype, có cổng cô lập — không ghép vào hệ chính khi chưa qua sandbox + duyệt).

## File Organization — Nơi lưu file mỗi loại

**Rule: Mỗi loại file có location riêng. KHÔNG sinh file lộn xộn khắp nơi.**

> **Nguyên tắc gốc:** MỌI thứ **SINH RA khi làm task** → để dưới **`memory/local/`** (1 ô duy nhất): plan/progress/summary để trực tiếp; `trace/`, `sql/`, `tmp/` là **subfolder bên trong `local/`**. **`memory/*/` đã gitignore → cả `local/` + mọi thứ bên trong tự động KHÔNG lọt git**, không thành rác. *(Folder tự tạo khi ghi — Write/mkdir auto-create, không cần tạo sẵn.)* `scripts/` **chỉ** chứa script **reusable đã commit**, KHÔNG để scratch.

### Location Mapping

> **2 cõi rạch ròi trong `memory/`:**
> ```
> memory/
> ├── *.md (root)          COMMITTED — tri thức team (đã đúc kết, push lên git)
> ├── local.template/      COMMITTED — mẫu cho local/
> └── local/               ⛔ LOCAL — gitignore, KHÔNG push   ← MỌI thứ sinh khi task
>     ├── {feature}.md         plan / progress / summary
>     ├── trace/               trace report (trace-agent ghi vào đây)
>     ├── sql/                 sql scratch
>     └── tmp/                 code-gen / scratch
> ```
> → **FILE `.md` ở root = team (git)** · **FOLDER `local/` (+ mọi thứ bên trong) = local (ignore)**.

**Nhóm 1 — Ghi chú session** → `memory/local/` (gitignore)
| Loại file | Nơi lưu |
|-----------|---------|
| **Plan/Kế hoạch** | `memory/local/{feature}.md` |
| **Progress report** | `memory/local/{feature}-progress.md` |
| **Report/Summary** | `memory/local/{feature}-summary.txt` |

**Nhóm 2 — Artifact theo loại** (subfolder **dưới `memory/local/`**, đều ignore)
| Loại file | Nơi lưu |
|-----------|---------|
| **Trace/Debug log** | `memory/local/trace/{feature}.md` |
| **SQL script** | `memory/local/sql/` (hoặc thư mục user chỉ định) |
| **Code generation** | `memory/local/tmp/` |
| **Batch script (.sh/.py)** | one-shot → `memory/local/tmp/`; **reusable → `scripts/`** (commit) |

**Nhóm 3 — Tri thức team** (file `.md` ở `memory/` ROOT, **COMMITTED**, không ignore)
| Loại file | Nơi lưu |
|-----------|---------|
| **Feedback/Rule team-wide** | `memory/{feedback\|reference\|pattern}*.md` |
| **Project status** | `memory/project_{name}.md` (xóa khi DONE) |

### Quy tắc BẮT BUỘC

1. **Session-specific files** (plan, progress, trace):
   - Lưu vào `memory/local/` → **gitignore tự động**
   - Không commit vào repo
   - Xóa sau task xong (hoặc keep 1-2 session nếu cần reference)

2. **Code/SQL artifacts**:
   - **one-shot** (generate rồi run xong) → `memory/local/tmp/` (hoặc `memory/local/sql/`), **XÓA sau dùng xong**
   - **dùng lại nhiều lần** → commit vào `scripts/`

3. **Root folder CLEAN**:
   - **KHÔNG** sinh file `.sql`, `.txt`, `.sh`, `.py` trong root
   - Root chỉ chứa: `CLAUDE.md`, `README.md`, config files, `scripts/`, `memory/`
   - Artifacts / generated files → `memory/local/tmp/`, xóa sau xong

4. **Cleanup sau task**:
   - Trước khi báo xong: grep root folder tìm `.txt`, `.sql`, `.sh`, `.py` không thuộc source
   - Xóa artifacts
   - Xóa session files từ `memory/local/` nếu không cần lưu lại

### Example: Session One-shot Task

```
Task: Generate đa ngôn ngữ SQL keys từ HTML

❌ SAI:
- Sinh gen_multilang_sql.sh, multilang_report.txt ở root → rác thải

✅ ĐÚNG:
1. Sinh scripts vào memory/local/tmp/
2. Run script, output vào memory/local/sql/multilang_keys.sql
3. User review + run SQL
4. Xóa memory/local/tmp/ → clean
5. Lưu kết quả summary vào memory/local/multilang-batch-summary.md (session-only)
```

---

## Plans & Local Session Context — `memory/local/`

**Rule: Khi user yêu cầu lên kế hoạch → lưu vào `memory/local/{task-name}.md` (trong repo root), KHÔNG ở C:\**

**CRITICAL:**
- ❌ KHÔNG dùng `.claude/plans/` (ở C:\Users\..., local machine)
- ❌ KHÔNG lưu gì ở ổ C:\
- ✅ **LỰA DỌN dùng repo paths** (folder `memory/local/` trong repo root)

**Why:** 
- C:\ drive là local-only → không shared với team, không persistent
- Repo (D:\) là shared, persistent, backed up
- Member cần access plans trong session sau hoặc team members khác
- Gitignored nhưng accessible trong repo

### Quy tắc:
1. **Khi user nói**: "lên kế hoạch", "plan đi", "hãy plan", v.v.
   - Lưu vào: `D:\PROJECT\Git\scrm_claude\AgentSystem\memory\local\`
   - Tên file: `{tính-năng}.md` (ví dụ: `return_order_from_shopee.md`, `multi_lang_batch_3.md`)
   - **KHÔNG commit** lên git (folder `memory/local/` đã gitignore)

2. **Plan này là member-specific, persistent**
   - Lưu vào repo (gitignored `memory/local/`)
   - Có thể reuse giữa sessions (member xem lại plan cũ)
   - Nếu task thay đổi → update file, hoặc create file mới
   - Sau task DONE → xóa hoặc lưu lại nếu muốn reference

3. **Nội dung plan** (task đụng logic — đủ các khối):
   - 🔴 **Nguồn đã NẮM (Pha 1 — bắt buộc CITE để chứng minh đã đọc kho, KHÔNG nhảy thẳng vào source):**
     - **CodeMap** định vị code (SCRM **bắt buộc** dùng codemap, down → **hỏi user** menu scan/grep/skip — KHÔNG tự grep);
     - **registry node** của controller bị đụng (`registry/<module>/<Controller>.md` → hành vi đúng + **ripple `cross_module_out`** + known-issue);
     - **feature doc** (`features/<module>/<A>/` summarize/dev) cho nghiệp vụ + cạm bẫy.
     - ⚠️ **Kho là SNAPSHOT** (watermark, xem `features/_FRESHNESS.md`): đọc để **ĐỊNH HƯỚNG** (ripple/behavior/known-issue), NHƯNG **phải VERIFY lại với code HIỆN TẠI** (mở source qua codemap/ripgrep) — `path:line` có thể lệch, behavior/field có thể đã đổi từ lúc quét. **Code là chân lý, kho là bản đồ** — đọc kho ≠ tin mù; chốt số/behavior phải từ source thật.
     → Plan task-logic mà **KHÔNG cite được registry node / feature / codemap = Pha 1 bị BỎ → làm lại** (đụng code mà không biết ripple/behavior đúng là nguy hiểm).
   - **Scope** rõ. ⚠️ Nếu định **THU HẸP scope** so với yêu cầu (vd task FE+BE+DB mà chỉ định làm BE) → **PHẢI HỎI user trước**, KHÔNG tự cắt.
   - Thứ tự công việc + **paths ĐÃ confirm với user** (KHÔNG đoán — workspace có thể có nhiều bản gần giống, hỏi user path nào là chỗ làm, đừng tự chọn).
   - **Acceptance + edge case** (blueprint cho trace).
   - **QA** (1 phần CỦA plan, không bỏ): ghi rõ sẽ chạy `review-agent` → `trace-agent` (case nào) → confirm.
   - Errors/blockers.

4. **Lên plan xong → TRÌNH plan + CHỜ user duyệt rồi MỚI code** (task đụng logic / có thu hẹp scope). KHÔNG tự code ngay sau plan, KHÔNG tự quyết scope. *(Đây là pha 2→3 của Pipeline GATED ở mục "Definition of DONE": Spec → Plan → Code → Review → Trace, mỗi mốc DỪNG chờ user OK. QA sau code cũng gated — Claude đề xuất review/trace + chờ OK, KHÔNG tự phóng, KHÔNG tự bỏ.)* Liên quan `feedback_ask_spec_before_code`.

### Ví dụ:

```
Session 1 (Member A):
User: "lên kế hoạch return order từ shopee"
→ Claude creates: memory/local/return_order_from_shopee.md
→ Member làm theo plan

Session 2 (Member A, continue):
User: "tiếp tục return order"
→ Claude reads memory/local/return_order_from_shopee.md
→ Update plan nếu cần, tiếp tục task
```

---

## Project Knowledge Base

Folder `projects/` chứa context về các dự án mà agent system hỗ trợ.

### Quy tắc ĐỌC — bắt buộc trước khi làm bất kỳ task nào

Khi user đề cập đến project (hoặc path code thuộc project đó), đọc theo thứ tự:
1. `architecture.md` — bản đồ hệ thống, đọc TRƯỚC TIÊN
2. `patterns.md` — convention viết code (core/tổng quan, lean)
3. `qa-code.md` — checklist convention để tránh lỗi lặp lại
4. File liên quan trực tiếp đến task (`domain.md`, `features/`, v.v.)
   - **`convention/<chủ-đề>.md`** — **load-on-demand**: convention chi tiết tầng 2 (canonical+raw pair, form domain-agnostic, webhook nội bộ...). `patterns.md` nêu tổng quan → khi task chạm đúng chủ đề thì đọc file chi tiết. KHÔNG đọc cả folder.
5. **Registry node** (`registry/<module>/<Controller>.md`) — **BẮT BUỘC khi sửa LOGIC của controller đó** (load-on-demand đúng node): đọc TRƯỚC khi sửa để biết **chính xác** nó làm gì (endpoint/quyền/hành vi đúng), **ripple** (`cross_module_out` → đi tiếp node liên quan để biết đổi X gãy đâu), và **lỗi/lỗ hổng đã biết** (path:line). Đây là nguồn "what does X do" chuẩn nhất. **Sửa code mà KHÔNG đọc node = dễ phá ripple / sai behavior** → cấm nhảy thẳng vào source. KHÔNG đọc cả folder — chỉ node liên quan.
6. **Recipe theo tác vụ** (`skills/`) — **load-on-demand**: nếu task chạm tới 1 tác vụ tái sử dụng (xuất Excel, phân trang, upload file, search/filter...), đọc đúng file recipe trong `projects/{project}/skills/`. Xem `skills/README.md` để biết có recipe nào. KHÔNG đọc hết folder — chỉ đọc file đúng tác vụ, để giữ context nhẹ.

Nhận biết project qua tên hoặc context user cung cấp:
- `scrm` → đọc `projects/scrm/`
- `ps_delivery` → đọc `projects/ps_delivery/`
- `ecommerce_connect` → đọc `projects/ecommerce_connect/`
- `delivery_connect` → đọc `projects/delivery_connect/`
- `delivery_receive` → đọc `projects/delivery_receive/`
- `delivery_status` → đọc `projects/delivery_status/`

### Quy tắc CẬP NHẬT — chỉ khi được xác nhận

**KHÔNG tự động cập nhật sau task.** Chỉ cập nhật khi user nói rõ: "update docs đi", "update md đi", "commit đi", hoặc tương đương.

Khi được xác nhận, cập nhật nếu phát hiện điều mới:

| Phát hiện | Cập nhật file |
|-----------|--------------|
| Convention/pattern mới chưa có trong `patterns.md` | `patterns.md` |
| Lỗi code hay vi phạm convention mới | `qa-code.md` |
| Rule test nghiệp vụ mới, edge case mới | `qa-spec.md` |
| Tính năng/luồng mới đã implement | `features/` hoặc `features.md` |

Không cập nhật nếu nội dung đã có hoặc quá ephemeral (chỉ đúng 1 lần).

---

### projects/scrm/
- `project.json`  — paths BE/FE, tech stack, danh sách modules
- `architecture.md` — **bản đồ toàn hệ thống**: ~40 service, nhóm domain, quan hệ cross-service
- `domain.md`     — entities, relationships, business rules
- `patterns.md`   — code conventions BE (.NET 6) + FE (Angular 15 + ng-zorro)
- `frontend.md`   — Angular patterns, ng-zorro templates, màn hình hiện có
- `registry/`     — 🆕 **knowledge-graph 746 node** (mỗi node = 1 controller): endpoint/quyền/**hành vi đúng**/quan hệ (depends/ripple)/lỗi đã biết. Backbone "hệ thống làm được gì" — load-on-demand, xem `registry/README.md` + `registry/_SCHEMA.md`
- `features/`     — cẩm nang prose theo tính năng: mỗi feature 1 folder `summarize.md`/`faq.md`/`flow.md`/`dev.md` (Pha 2, đắp dần lên registry). `features.md` cũ = mô tả WorkFlow
- `qa-code.md`    — checklist review code: convention, structure, common mistakes
- `qa-spec.md`    — checklist test nghiệp vụ: behavior, edge cases, luồng chính
- `convention/`   — 🆕 **convention chi tiết riêng mảng** (tầng 2, ngang `lessons/`): canonical+raw pair, form domain-agnostic, ngzorro... `patterns.md` = core/tổng quan, `convention/` = chi tiết per-chủ-đề **load-on-demand**. Xem `convention/README.md`
- `lessons/`      — 🆕 **CHỈ bug-sự-cố thật** (reactive "lần đó bị X vì Y"). Convention đã tách sang `convention/`; đơn-hoàn detail dời `memory/local`. Xem `lessons/README.md`
- `skills/`       — recipe theo tác vụ tái sử dụng (paging, export Excel, upload, search) + `skills/multi-lang/` (multi-lang automation) — **load-on-demand**, xem `skills/README.md`

Paths thực tế: hỏi user khi bắt đầu task

### projects/ps_delivery/
- `project.json` — tech stack, danh sách 2 services + danh sách đối tác vận chuyển
- `domain.md`    — entities, luồng xử lý, RabbitMQ messages, services
- `patterns.md`  — cấu trúc solution, controller groups, naming conventions, MassTransit pattern

Paths thực tế: hỏi user khi bắt đầu task `ps_delivery`
- PsWebhook (webhook receiver, .NET Core 2.1): `./Service/` (relative to root)
- DeliveryReceive (message consumer, .NET 6): `./ps_delivery_receive/` (relative to root)

### projects/ecommerce_connect/
- `project.json`    — tech stack, ecommerce partners
- `architecture.md` — luồng, cấu trúc solution, RabbitMQ queues
- `domain.md`       — entities, endpoints
- `patterns.md`     — naming, MassTransit, logging pattern, pattern persist-parent-trước-SP
- `qa-code.md`      — checklist review code: persistence, retry job, external API filter
- `qa-spec.md`      — checklist test nghiệp vụ: invoice flow, order flow, edge cases
- `convention/`     — 🆕 **convention chi tiết riêng mảng** (tầng 2, ngang `lessons/`): webhook nội bộ no-auth, ranh giới ecomm vs CRM, defer persist, deleteasync persist dirty. `patterns.md` = core, `convention/` = chi tiết **load-on-demand**. Xem `convention/README.md`
- `lessons/`        — 🆕 **CHỈ bug-sự-cố thật** (reactive). Convention đã tách sang `convention/`. Xem `lessons/README.md`

Paths thực tế: hỏi user khi bắt đầu task `ecommerce_connect`

### projects/delivery_connect/
- `project.json`    — tech stack
- `architecture.md` — luồng, cấu trúc solution, RabbitMQ
- `domain.md`       — entities, endpoints
- `patterns.md`     — naming, MassTransit, logging pattern

Paths thực tế: hỏi user khi bắt đầu task `delivery_connect`

### projects/delivery_receive/
- `project.json`    — tech stack
- `architecture.md` — luồng, cấu trúc solution, RabbitMQ
- `domain.md`       — entities, endpoints, DataReceive fields
- `patterns.md`     — naming, MassTransit, logging pattern

Paths thực tế: hỏi user khi bắt đầu task `delivery_receive`

### projects/delivery_status/
- `project.json`    — tech stack
- `architecture.md` — luồng, cấu trúc solution, RabbitMQ
- `domain.md`       — entities, endpoints, consumers
- `patterns.md`     — naming, MassTransit, logging pattern

Paths thực tế: hỏi user khi bắt đầu task `delivery_status`
