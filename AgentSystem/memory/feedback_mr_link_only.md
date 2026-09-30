---
name: MR chỉ cho link
description: Không tự tạo MR, chỉ đưa link để user tự tạo
type: feedback
---

Khi user yêu cầu MR, chỉ đưa link tạo MR trên GitLab. Tuyệt đối KHÔNG tự chạy lệnh tạo MR (glab mr create, gh pr create, ...).

**Why:** User muốn kiểm soát nội dung MR, tự review trước khi tạo.

**How to apply:** Khi user nói "cho MR" → chỉ output link dạng:
`https://gitlab.com/{repo}/-/merge_requests/new?merge_request[source_branch]={branch}`
