---
name: Commit message bắt đầu bằng TÊN BRANCH đang commit lên + KHÔNG Co-Authored-By
description: Lệ commit của team. Message phải bắt đầu bằng tên branch hiện tại đang commit (commit trên branch `thiennq/xxx` → "thiennq/xxx: <mô tả>"). 1 dòng ngắn gọn. KHÔNG thêm trailer "Co-Authored-By: Claude" trong repo client.
metadata:
  type: feedback
---

**Lệ commit message của team:**
1. **Bắt đầu bằng TÊN BRANCH đang commit lên.** Commit trên branch `thiennq/supership-static-token` → message `thiennq/supership-static-token: <mô tả>`. (Bằng chứng: repo `delivery_connect` commit thẳng branch `product` → message luôn prefix `product: ...`, vd "product: SUpership fix cast response".)
2. **1 dòng ngắn gọn**, không body dài.
3. **KHÔNG thêm `Co-Authored-By: Claude ...`** (hay trailer AI nào) — repo client của team không dùng. (Đây là override lệ mặc định của harness — lệ team/user thắng.)

**Why:** Lần SuperShip token tĩnh: tôi commit trên branch mới `thiennq/supership-static-token` nhưng (a) bê prefix "product:" từ log cũ (sai — đó là tên branch CŨ), và (b) tự thêm `Co-Authored-By: Claude` theo mặc định → cả 2 đều sai lệ. User phải tự bắt. Đây đúng là kiểu "layer-3 tự quyết lẽ ra phải hỏi" ([[feedback_flag_gia_dinh_ngam]]).

**How to apply:**
- Trước khi commit: lấy **tên branch hiện tại** (`git branch --show-current`) làm prefix message.
- **Bỏ** trailer Co-Authored-By / mọi attribution AI.
- Giữ 1 dòng, mô tả gọn.
- Không chắc format của repo khác → xem `git log --oneline` vài commit gần nhất để bắt chước lệ, hoặc hỏi.
- Liên quan [[feedback_branch]] (tên branch `thiennq/{feature}`), [[feedback_git]] (confirm trước khi git, không tự push), [[feedback_flag_gia_dinh_ngam]].
