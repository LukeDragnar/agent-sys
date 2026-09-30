---
name: Pre-commit / pre-"xong" workflow — tránh miss cross-reference
description: Quy trình bắt buộc trước khi báo xong hoặc commit, tránh miss stale cross-reference giữa các file
type: feedback
---

Trước khi báo user "xong" hoặc chạy `git commit`, **bắt buộc** đi qua checklist dưới. Không skip vì "thấy chắc rồi".

**Why:** Session 2026-04-10 Claude tự check code trước commit, báo clean. Nhưng MEMORY.md vẫn còn 3 chỗ stale (`{BUILD_BE}`, "3 loại folder") vì chỉ check global (hardcoded path) mà không cross-check giữa file vừa sửa và file reference nó. User phải tự soi và chỉ ra.

Gốc rễ: check không scope đúng chỗ đã thay đổi + quá tự tin vào "đã tự check".

## Checklist bắt buộc

### 1. Đọc `git diff --cached` từng hunk
- KHÔNG chỉ xem `--stat` (số dòng). Phải đọc **content** của từng thay đổi.
- Hunk nào không chắc → dừng lại, hỏi lại bản thân "thay đổi này có phá gì khác không?"

### 2. Dependency-aware re-check
- List file đã edit trong session
- Với mỗi file A, tự hỏi: "file nào reference A?" (index, rule mention, import, link, description)
- Re-read các file reference đó, verify mô tả/link còn đúng sau khi A đổi
- Case điển hình: edit `feedback_X.md` → phải check entry tương ứng trong `MEMORY.md` description có stale không

### 3. Commit nhỏ theo logical change
- KHÔNG gom 10 thứ khác nhau vào 1 commit "refactor + add feature + update doc + fix rule"
- Chia theo chủ đề, mỗi commit review riêng
- Exception: thay đổi đan xen không tách được (vd: rename + update all references) → commit 1 cục nhưng message phải liệt kê rõ

### 4. Plan → Execute → Verify tách biệt
- **Trước khi edit**: list rõ "sẽ edit file nào, vì sao, kỳ vọng thay đổi gì"
- **Sau khi edit**: re-read từng file đã sửa (không chỉ dựa vào Edit tool success)
- KHÔNG đi thẳng edit → stage → commit bỏ bước verify

### 5. Pair-of-eyes khi thay đổi lớn
- Thay đổi >5 file hoặc cross-cut nhiều module → spawn review-agent context sạch
- Review-agent không có bias "đã viết là đúng", nhìn diff như người ngoài
- Đừng bỏ qua vì "chỉ là doc/config" — meta-change cũng có cross-reference

## Anti-pattern

- ❌ "Em đã self-check, OK commit" mà không show cụ thể check gì
- ❌ Chỉ show `--stat` rồi hỏi "OK push?"
- ❌ Tự check code mình vừa viết → luôn có bias "biết nó đúng"
- ❌ Gom nhiều logical change vào 1 commit rồi review chung
- ❌ Trust Edit tool success = nội dung đúng (tool chỉ verify text replace, không verify logic)

## Cách áp dụng thực tế

Trước khi gõ `git commit`:
1. `git diff --cached` — đọc hết
2. List mental các file edited + file có thể reference chúng
3. Re-read các file reference đó
4. Nếu >5 file hoặc rule quan trọng → spawn review-agent
5. Chỉ commit sau khi 4 bước trên clean
