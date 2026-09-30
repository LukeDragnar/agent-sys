# Template — Spec (tiêu chí nghiệm thu / acceptance)

> Copy khuôn này ở **Pha 1 (Spec)** khi nhận 1 task đụng logic, TRƯỚC khi code.
> Lưu vào `memory/local/{feature}.md` (khối Acceptance) hoặc tách `memory/local/tieu-chi-nghiem-thu-{feature}.md`.
> Spec = **"đúng là gì"**, viết từ Ý ĐỊNH/nghiệp vụ, **ĐỘC LẬP với code** (KHÔNG chạy code ra rồi chép). 1 spec dùng cho 3 việc: code làm theo · trace bám vào · test/E2E sinh ra.

---

## Quy trình 5 bước: task thô → spec

```
1. LÀM RÕ      task muốn gì? (input/output, ai dùng, kỳ vọng nghiệp vụ) — mơ hồ = ⚠️ HỎI, không đoán
2. KHẢO SÁT    "đúng" lấy từ đâu? (3 mức tin cậy bên dưới)
3. LIỆT KÊ     các thao tác/nhánh + edge case (thiếu input · không tồn tại/idempotent · quyền ·
               trùng/đụng-cái-khác · giá trị biên)
4. VIẾT        mỗi thao tác → Given/When/Then (kết quả QUAN SÁT ĐƯỢC, đo được)
5. ĐÁNH DẤU    chỗ task không nói rõ → ⚠️ "cần xác nhận" → TRÌNH user duyệt rồi mới code
```

## Nguồn "đúng là gì" (bước 2 — 3 mức tin cậy)

| Mức | Nguồn | Cách dùng |
|---|---|---|
| CAO | `registry/<Controller>.md` (hành vi đúng) + feature đã product-xác-nhận | lấy thẳng làm expected |
| TRUNG | `features/<module>/{summarize,flow}.md` | lấy + ĐỐI CHIẾU code hiện tại (kho là snapshot) |
| THẤP | task im lặng / "Draft / cần xác nhận" | DỪNG, HỎI user — không tự quyết |

> Feature MỚI chưa có trong kho → nguồn "đúng" = ý định của dev → viết ra + ⚠️ để user chốt.

---

## Khung file (copy phần dưới)

```markdown
# Tiêu chí nghiệm thu — <Tên tính năng>

> Phạm vi: <làm cái gì — vd chỉ phần CẤU HÌNH>. Ngoài phạm vi: <cái để sau> → tiêu chí riêng.
> Khoá/định danh: <khóa nghiệp vụ>. Mức spec: <UI | service/API> (quyết "Then" quan sát ở đâu).
> ⚠️ = task không ghi, là ý định dev → cần xác nhận.

## A. <Nhóm thao tác 1 — vd Lấy/Get>
​```gherkin
Scenario: <tình huống thường — happy path>
  Given <tiền điều kiện / trạng thái dữ liệu>
  When  <hành động>
  Then  <kết quả nghiệp vụ QUAN SÁT ĐƯỢC>
  And   <bất biến: dòng khác giữ nguyên, không mất data...>

Scenario: <thiếu input>
  Given <thiếu trường bắt buộc>
  When  <thao tác>
  Then  <báo lỗi, không ghi/không đụng gì>
​```

## B. <Nhóm thao tác 2 — vd Lưu/Xoá/Copy>   (lặp khối Scenario)

## Chỗ ⚠️ cần xác nhận (task không ghi — chỉ dev biết)
1. <điểm ý định #1 — đúng không?>
2. <...>

## Ngoài phạm vi (tiêu chí riêng, chưa làm)
- <auto-fill / phần khác — chốt spec trước rồi mới làm>
```

## Checklist trước khi trình user
```
☐ Mỗi thao tác chính ≥1 Scenario happy path
☐ Quét đủ edge: thiếu input · không tồn tại (idempotent) · quyền · trùng/đụng-cái-khác · biên
☐ "Then" QUAN SÁT ĐƯỢC ở đúng mức (UI: thấy trên màn; API: response/trạng thái)
☐ Bất biến "không phá cái khác" có ghi (And ... GIỮ NGUYÊN)
☐ Chỗ ý định/không-rõ đã ⚠️, KHÔNG đoán
☐ Phạm vi + ngoài-phạm-vi rạch ròi
```

## Hay sai (tránh)
- ❌ Viết spec sau khi code, chép kết quả code ra → enshrine bug.
- ❌ Đoán hành vi khi task im lặng → phải ⚠️ + hỏi.
- ❌ "Then" không quan sát được ở mức đang viết (vd "lưu đúng DB" ở spec mức UI).

---

## Vòng đời spec (nháp session → kho `features/`)

> Spec sống ở `memory/local` lúc làm; khi feature ship + product chốt → graduate vào kho team để lần sau KHÔNG còn thiếu doc. **Không tự động** — chỉ khi user nói "update đi".

```
memory/local/tieu-chi-...md        NHÁP: theo ý định, còn ⚠️
        │
        │  GRADUATE khi đủ 3 điều kiện:
        │   ① feature đã SHIP + product/user XÁC NHẬN đúng
        │   ② các ⚠️ đã được chốt (KHÔNG bê dấu ⚠️ chưa-rõ vào kho)
        │   ③ user nói "update đi" (agent KHÔNG tự đẩy)
        ▼
projects/<mảng>/features/<module>/<feature>/      KHO team (committed)
   ├── summarize.md   hành động làm được + quyền
   ├── flow.md     ⭐ acceptance / case biên / ví dụ số  ← spec đổ chủ yếu vào đây
   ├── faq.md
   └── dev.md
   + cập nhật registry/<Controller>.md nếu behavior đổi
```

- **Không tự động:** đẩy kho phải qua người duyệt (chống rác) — xem `agents/duc-ket-agent.md` + `CONVENTIONS.md` (Quy tắc UPDATE).
- **Không copy-paste:** reshape Gherkin sang format feature (chủ yếu `flow.md`), KHÔNG bê nguyên file spec.
- **Chỉ phần ĐÃ CHỐT mới lên kho:** phần chưa có spec (vd auto-fill chưa chốt) → chờ spec riêng, chưa graduate.
- **Vì sao đợi product xác nhận:** spec nháp = ý định dev (có thể sai); chạy prod + được chấp nhận → "đúng" được bảo chứng → vào kho mới chuẩn. Sau này ticket khách lộ sai → vòng `feedback/` đính chính lại feature doc + spec.

> 1 spec phục vụ 3 việc trọn vòng đời: **code** làm theo · **trace** bám vào · **test/E2E** sinh ra.
