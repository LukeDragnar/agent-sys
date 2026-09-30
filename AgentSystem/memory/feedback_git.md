---
name: Git — chỉ thực hiện khi confirm 2 lần
description: Không tự động thực hiện bất kỳ git operation nào, kể cả commit/branch/stage, trừ khi user đã confirm rõ ràng 2 lần
type: feedback
---

Không tự ý thực hiện bất kỳ git operation nào (checkout, commit, add, push, branch...).

**Why:** User muốn kiểm soát hoàn toàn git — thao tác git sai có thể ảnh hưởng shared repo.

**How to apply:** Khi cần làm git, đề xuất lệnh/kế hoạch trước → chờ user xác nhận lần 1 → hỏi lại "xác nhận thực hiện?" → chờ confirm lần 2 → chạy **1 mạch toàn bộ flow** không dừng giữa chừng.

**TUYỆT ĐỐI KHÔNG chạy `git push`** dù user nói "ok" hay đồng ý chung chung — phải hiển thị rõ lệnh + remote target + hỏi confirm lần 2 trước khi thực thi. Push sai remote có thể ảnh hưởng cấu hình remote hoặc gây side effect ngoài ý muốn (user đã cảnh báo "cấm làm điều tương tự").

## Commit message

Format: `{tên branch}: {mô tả}`

Ví dụ: `thiennq/dong-thoi-ncc-kh-202603: fix bug đồng thời NCC/KH`

---

## Tạo branch mới

- User sẽ chỉ định folder/repo cụ thể cho từng task — không tự suy ra repo
- **Trước khi tạo branch: luôn pull `release_product` về mới nhất trước**
- Branch mới **luôn base từ `release_product`**: `git checkout -b {branch} release_product`
- Nếu chưa ở `release_product` thì checkout về đó trước, rồi pull, rồi mới tạo nhánh mới

## GitLab URLs

- scrm_frontend: `https://gitlab.com/nhantocuiday/scrm_frontend`
- MR link template: `https://gitlab.com/nhantocuiday/scrm_frontend/-/merge_requests/new?merge_request[source_branch]={branch}&merge_request[target_branch]=release_product`

## Thứ tự push

1. Push feature branch lên GitLab (`git push origin {branch}` từ repo gốc của dev — path mỗi máy khác nhau, confirm với user đầu session)
2. Sau đó mới cherry-pick vào `release_staging` và push staging

**Why:** Feature branch phải có trên GitLab trước để tạo MR được. Push lên local repo trung gian không đủ.

## Cherry-pick sang staging

Sau khi push feature branch xong, **luôn hỏi user confirm pick vào `release_staging`** — không bỏ qua bước này.

Quy trình — thực hiện trong folder được giao, không sang folder khác:
1. `git checkout release_staging`
2. `git pull https://gitlab.com/nhantocuiday/scrm_frontend.git release_staging` — pull GitLab, fast-forward
3. `git cherry-pick <commit(s)>` — chỉ cherry-pick, KHÔNG merge feature vào staging
4. `git push https://gitlab.com/nhantocuiday/scrm_frontend.git release_staging`

**Tại sao không merge:** Feature base từ `release_product`, không phải staging — cherry-pick là đủ, merge sẽ kéo theo code thừa.

**KHÔNG pick trước rồi pull sau** — sẽ sinh merge commit thừa hàng trăm dòng.

## Giới hạn folder

**Khi được giao làm việc trong folder nào, chỉ thao tác git trong folder đó — không tự ý sang folder khác.**

**Why:** Các folder khác (SourceForBuild, scrm_frontend gốc, ...) có thể đang có task khác đang dở — can thiệp vào sẽ hỏng task của người khác.

**How to apply:** Nếu cần thao tác ở folder khác, hỏi user trước, không tự suy và hành động.
