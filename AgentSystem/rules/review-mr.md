# rules/review-mr.md — Skill: Review Merge Request (mọi repo)

> **Trigger:** "review MR <link/#>", "review giúp MR này", "MR này có hỏng gì không", "MR có an toàn merge không".
> **Mục tiêu:** kết luận MR **an toàn merge / có rủi ro X / cần sửa trước** — đúng convention, **KHÔNG phá logic vận hành hiện tại của target** (thường `release_product`), không revert/mất thay đổi mới, cover spec.
> Gắn với: [Definition of DONE (CLAUDE.md)](../CLAUDE.md), [code.md](code.md), "Code Search" (codemap/ripgrep), các lesson `memory/feedback_*` nêu ở mục cuối. KHÔNG chép lại — chỉ trỏ.

## Ràng buộc (đọc trước)
- **KHÔNG tự merge / approve / push** — chỉ review + đưa nhận xét (`feedback_mr_link_only`).
- **KHÔNG đụng `release_product`** (`feedback_no_touch_release_product`).
- **Confirm path repo local mỗi session** (BE/FE ở đâu) nếu cần đọc source — KHÔNG tự đoán. Hoặc review qua `glab`/`gh` diff (không cần local) → nói rõ.

## Quy trình

### B0. Xác định phạm vi
- Repo nào (`scrm` BE / `scrm_frontend` FE / `ecommerce_connect`...), **MR#**, **source → target** (target thường `release_product`).
- Confirm path repo (hoặc dùng CLI diff). Glab/gh có chưa.

### B1. Lấy diff
- `glab mr diff <#>` · `gh pr diff <#>` · hoặc local: `git fetch origin && git diff origin/<target>...origin/<source>`.
- Liệt kê **file đổi + phân loại**: logic (`.cs` Service/Handler/Controller/Domain, `.ts` logic, `.sql`) vs label/`.html` text/`.scss`/docs. (Quyết định có cần Trace ở B5.)

### B2. ⚠️ Base-staleness / REVERT check — RỦI RO #1 (đặc biệt nhánh feature)
Nhánh feature base **CŨ** → merge dễ **revert / ghi đè** thay đổi mới của target.
- So base MR vs target hiện tại: `git merge-base origin/<target> origin/<source>` rồi `git log <base>..origin/<target> -- <file đổi>` → nếu target đã sửa file đó SAU base ⇒ **nghi MR đè mất**.
- **Đọc `feedback_cherrypick_feature_branch_config_gate`**: gate `formControlName` — tập `formControlName`/config của file FE đổi phải **= target** (0 thêm/bớt ngoài ý đồ MR). MR lang/UI base cũ hay làm **MẤT config staging mới**.
- Diff có xóa/đổi dòng **không thuộc ý đồ MR** (config, field, registration) → flag.

### B3. Hiểu luồng hiện có (đừng đoán) — `feedback_study_existing_flow`
Với mỗi file logic đổi → đọc kho project: `patterns.md`/`qa-code.md` + **registry node** controller liên quan (hành vi đúng + ripple `cross_module_out` → đổi X gãy đâu). Dùng **CodeMap/Ripgrep** tìm caller thật nếu cần (CLAUDE.md "Code Search").

### B4. Review convention + spec
Spawn **`review-agent`** trên phần diff logic (qa-code + qa-spec). Lỗi convention/structure → ghi.

### B5. Trace impact (nếu đụng LOGIC) — theo Definition of DONE
Spawn **`trace-agent`**: data giả lập đi qua **nhánh logic MR đổi**, verify (a) không phá hành vi cũ, (b) không phá ripple (B3), (c) cover case/acceptance. Report `memory/local/trace/`.
→ MR **chỉ label/`.html`/`.scss`/docs** → **BỎ trace**, review nhẹ.

### B6. Verdict block (kết luận, KHÔNG tự merge)
```
✅/⚠️/❌ Review MR #<n> (<repo> <source>→<target>):
• Phạm vi đổi: <logic | chỉ label/UI>  (N file)
• Base/revert: <base ok | ⚠️ nghi đè file X target sửa ngày .. | mất config formControlName: ...>
• Convention: <pass | lỗi: path:line>
• Trace:      <case đã thử / kết quả>     ← bỏ nếu không đụng logic
• Ripple:     <đổi X có thể ảnh hưởng Y (registry cross_module)>
• Kết luận:   <AN TOÀN merge | RỦI RO: ... cần sửa trước | cần product xác nhận ...>
```

## Lesson liên quan (đọc khi review MR SCRM/connect)
- `feedback_cherrypick_feature_branch_config_gate` — **mất config/`formControlName`** khi nhánh feature base cũ (bẫy hay gặp nhất).
- `feedback_no_touch_release_product` · `feedback_no_merge_staging_to_feature` · `feedback_mr_product_staging_cherrypick` — workflow nhánh (MR target release_product; staging chỉ cherry-pick).
- `feedback_study_existing_flow` · `feedback_mr_link_only` · `feedback_self_trace_before_done`.
