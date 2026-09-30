# Hướng dẫn Team — Dùng AgentSystem (Claude Code + kho tri thức SCRM)

> Đối tượng: dev trong team dùng Claude Code để làm task SCRM (BE/FE/DB).
> Mục tiêu: làm task **đúng convention, đúng nghiệp vụ, có QA** — agent dẫn, bạn duyệt.

---

## 1. Đây là gì

`AgentSystem/` là **kho tri thức + quy trình** để Claude Code hiểu hệ thống SCRM và làm việc an toàn:
- `CLAUDE.md` — luật làm việc (quy trình, QA, convention gốc). Claude nạp file này **mỗi lần mở session**.
- `projects/scrm/` — bản đồ hệ thống: architecture, registry (746 controller), features, skills, qa-code/qa-spec.
- `agents/` — các skill QA: `review-agent` (check convention+spec), `trace-agent` (mô phỏng data chạy qua code).

Claude **không tự đoán** hệ thống làm gì — nó tra registry/feature/codemap rồi mới làm.

---

## 2. Chuẩn bị (làm 1 lần, hoặc khi đổi máy)

1. **Pull bản mới nhất:**
   ```
   git checkout release_product && git pull
   ```
2. *(Optional)* **Giấy nhớ path cá nhân:** copy `memory/local.template/paths.md.template` → `memory/local/paths.md`, điền path BE/FE máy bạn.
   - Chỉ để **bạn copy-paste cho nhanh** khi Claude hỏi path. Claude **KHÔNG tự đọc** file này (mỗi session nó vẫn hỏi lại, vì path có thể đã đổi). Không thích thì bỏ qua, gõ tay cũng được.
   - Folder `memory/local/` đã **gitignore** → không lên git.
3. **Codemap chạy được** (search BE + dependency). Xem `tools/README.md`. Search nhanh FE/pattern thì dùng **ripgrep** (`tools/ripgrep/`).

> ⚠️ **Quan trọng:** `CLAUDE.md` chỉ nạp lúc **mở session**. Nếu bạn `git pull` **giữa lúc đang chat**, luật mới **chưa** có hiệu lực — phải **mở session mới** sau khi pull.

---

## 3. Quy trình làm 1 task — GATED 6 pha

**Nguyên tắc:** Claude **DẪN** từng pha (tự đánh giá "có vẻ xong" → trình kết quả → mời bạn xác nhận → **chờ**). Bạn chỉ **gật / lắc** ở mỗi mốc. Claude **không tự nhảy pha**, cũng **không tự bỏ pha**.

| Pha | Claude làm | Mốc bạn quyết |
|---|---|---|
| **1. Spec** | Hỏi lại yêu cầu cho rõ, chốt phạm vi | Xác nhận đúng ý |
| **2. Plan** | Khảo code (codemap + registry + feature) → trình plan (scope, việc, acceptance, QA) | Duyệt / chỉnh plan |
| **3. Code** | Sửa code → **dừng**, báo "đã code gì + còn việc gì" | Chốt "code đủ chưa" |
| **4. Review** | Spawn `review-agent` → sửa lỗi → báo | OK để qua trace |
| **5. Trace** | Spawn `trace-agent` (mô phỏng data số cụ thể) → báo | OK |
| **6. Confirm** | Báo cáo đóng task (đã làm gì + trạng thái + còn lại) | **Bạn** quyết commit |

### Hội thoại mẫu

**Pha 1–2 (Spec → Plan):**
```
Bạn:    Thêm trường "ghi chú nội bộ" cho voucher
Claude: Để rõ scope: trường này hiện ở list + form sửa, lưu DB,
        có cần hiện khi nhân bản voucher không? BE+FE+DB đúng chứ?
Bạn:    Đúng. Nhân bản thì KHÔNG kéo theo ghi chú.
Claude: [tra registry VoucherController + feature voucher + codemap]
        Plan: (1) BE thêm field VoucherOption... (2) FE form+list...
        (3) SQL ALTER... (4) QA: review + trace case nhân bản.
        Duyệt plan này không?
Bạn:    OK code đi
```

**Pha 3 (Code) — Claude DỪNG, không tự nhảy review:**
```
Claude: Đã sửa: VoucherOption.cs (+field), DTO, AutoMapper, form FE, list FE.
        SQL gen ở memory/local/sql/voucher-ghi-chu.sql (bạn tự chạy).
        Đã rà scope: hết việc code. → Code đủ chưa, OK qua review?
Bạn:    ok
```

**Pha 4–5 (Review → Trace) — dừng giữa 2 pha:**
```
Claude: review-agent: pass, đã sửa 1 lỗi (thiếu [Column TypeName]).
        → OK chạy trace không?
Bạn:    ok
Claude: trace-agent: case "nhân bản voucher có ghi chú" → field KHÔNG
        carry-over (đúng plan). Case sửa/lưu → giá trị đúng.
        ✅ Confirm: review pass, trace pass. Còn lại: bạn chạy SQL + quyết commit.
```

---

## 4. Cách ra lệnh

| Bạn muốn | Nói |
|---|---|
| **Đi nhanh, khỏi hỏi từng mốc** | *"làm hết đi"* / *"code + QA luôn"* → tính là OK gộp, Claude chạy thẳng |
| **Gọi QA bất kỳ lúc nào** | *"tự check"* / *"review + trace"* / *"trace lại đi"* |
| **Chỉ làm 1 phần** | Nói rõ *"chỉ làm BE thôi"* (Claude sẽ không tự thu hẹp scope nếu bạn không bảo) |
| **Đổi path code** | Mỗi session Claude hỏi BE/FE ở đâu → trả lời path **working** (đừng path backup) |

---

## 5. Nguyên tắc bắt buộc

- ✅ **Review áp cho MỌI thay đổi** — kể cả sửa text/label/đa ngôn ngữ (sai chữ = sai nghĩa, lệch ngữ cảnh, sai key dịch, label không khớp chức năng). **Trace (mô phỏng data qua code)** cộng thêm khi đụng **logic** (field/mapping/save/DB/business rule); sửa text thuần thì trace N/A (báo rõ N/A, không phải "bỏ qua"). **Không có** "task nhỏ nên khỏi QA".
- ✅ **Commit/push là việc của bạn.** Claude không tự commit (trừ khi bạn chủ động bảo). Nó cũng **không mời** "commit không?".
- ⛔ **Không commit `memory/local/`** (plan/sql/trace cá nhân) — đã gitignore sẵn.
- ⛔ **SQL/migration:** Claude gen ra `memory/local/sql/`, **bạn tự chạy thủ công** trên DB (repo không chứa migration; không FK/INDEX trong script).
- ⛔ **Staging chỉ READ-ONLY** — chỉ SELECT khi thật cần, không thêm/sửa/xóa data.
- ⛔ **Không sửa vào folder backup** mà Claude tự đoán — luôn xác nhận path working với nó.

---

## 6. Bản đồ kho — tìm gì ở đâu

| Cần gì | Đọc |
|---|---|
| Bản đồ ~40 service, quan hệ | `projects/scrm/architecture.md` |
| "Controller X làm gì / quyền / lỗi đã biết" | `projects/scrm/registry/<module>/<Controller>.md` |
| Cẩm nang tính năng (cho cả khách/sale/dev) | `projects/scrm/features/<module>/<feature>/` |
| Recipe tác vụ (paging, export Excel, upload, search, multi-lang) | `projects/scrm/skills/` |
| Convention code BE/FE | `projects/scrm/patterns.md` + `convention/` |
| Checklist review / test | `projects/scrm/qa-code.md` / `qa-spec.md` |
| Bug-sự-cố đã gặp | `projects/scrm/lessons/` |

---

## 7. FAQ — lỗi hay gặp & cách xử

**Q: Agent code xong tự chạy QA / tự nhảy sang trace, không hỏi.**
→ Nó đang chạy CLAUDE.md **bản cũ**. `git pull` + **mở session mới**. Nếu vẫn vậy: nhắc thẳng *"chưa xác nhận mà đã nhảy bước, dừng lại báo cáo đã"*.

**Q: Agent bảo "task đơn giản, bỏ qua trace".**
→ Sai. "Thêm 1 field" vẫn đụng logic (mapping+save+DB) → phải trace. Nói *"đụng logic thì phải trace, không được bỏ"*.

**Q: Agent tự code ngay sau khi lên plan, chưa cho mình duyệt.**
→ Nhắc *"trình plan chờ tôi duyệt rồi mới code"*. (Luật đã có, nhưng bản cũ hay lách.)

**Q: Agent ghi "đã review + trace" nhưng không thấy spawn agent.**
→ Hỏi thẳng *"đã spawn review-agent / trace-agent thật chưa?"*. Overclaim QA là lỗi nặng — nó phải khai thật.

**Q: Agent định sửa vào path lạ / folder backup.**
→ Dừng, chỉ lại path working đúng. Mỗi session nó phải hỏi path, đừng để nó tự đoán.

**Q: Mình pull rồi mà agent vẫn theo luật cũ.**
→ Bạn pull **giữa session**. Mở **session mới** sau khi pull.

---

## 8. Cập nhật kho tri thức

Claude **không tự** sửa kho sau task. Chỉ khi bạn bảo:
- *"update docs đi"* → cập nhật `patterns.md` / `qa-code.md` / `features/` nếu phát hiện cái mới.
- *"đúc kết lại đi"* → chạy `agents/duc-ket-agent.md`: tự rút kinh nghiệm → **đề xuất** ghi vào kho → **chờ bạn duyệt** rồi mới ghi (chống rác).

> Tri thức team (file `.md` ở `memory/` root + `projects/`) → commit lên `release_product`.
> Ghi chú cá nhân (`memory/local/`) → gitignore, không lên git.
