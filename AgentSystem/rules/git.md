# Git Rules

## Nguyên tắc chung

Không tự ý thực hiện bất kỳ git operation nào (checkout, commit, add, push, branch...).

**Why:** User muốn kiểm soát hoàn toàn git — thao tác git sai có thể ảnh hưởng shared repo.

**How to apply:** Khi cần làm git, đề xuất lệnh/kế hoạch trước → chờ user xác nhận lần 1 → hỏi lại "xác nhận thực hiện?" → chờ confirm lần 2 → chạy **1 mạch toàn bộ flow** không dừng giữa chừng.

### Ngoại lệ DUY NHẤT — `deploy-agent` đọc code staging

`agents/deploy-agent.md` được phép chạy **`git fetch`** và **`git pull --ff-only`** (cùng các lệnh chỉ-đọc: `log`, `diff`, `status`, `rev-parse`, `remote get-url`) **trong folder build staging** — sau khi user đã chốt phạm vi ở cổng confirm #1.

- Chỉ 2 lệnh ghi đó, **không** `checkout`/`commit`/`push`/`merge`/`reset`/`stash`/`clean`/`--force`.
- `--ff-only` nên không tạo commit, không viết lại lịch sử; dirty hoặc diverge → **DỪNG hỏi user**.
- Ngoài `deploy-agent` + folder build staging thì ngoại lệ này **không áp dụng**.

---

## Commit message

Format: `{tên branch}: {mô tả}`

Ví dụ: `thiennq/dong-thoi-ncc-kh-202603: fix bug đồng thời NCC/KH`

---

## Tạo branch mới

- User sẽ chỉ định folder/repo cụ thể cho từng task — không tự suy ra repo
- **Trước khi tạo branch: luôn pull `release_product` về mới nhất trước**
- Branch mới **luôn base từ `release_product`**: `git checkout -b {branch} release_product`
- Nếu chưa ở `release_product` thì checkout về đó trước, rồi pull, rồi mới tạo nhánh mới

## Tên branch

Format: `{username}/{feature-name}-{yyyymm}` — kebab-case, tiếng Việt không dấu, năm tháng ở cuối.

Ví dụ: `thiennq/dong-thoi-ncc-kh-202603`

---

## GitLab URLs

- scrm BE: `https://gitlab.com/nhantocuiday/scrm`
- scrm FE: `https://gitlab.com/nhantocuiday/scrm_frontend`
- MR link template FE: `https://gitlab.com/nhantocuiday/scrm_frontend/-/merge_requests/new?merge_request[source_branch]={branch}&merge_request[target_branch]=release_product`

---

## Thứ tự push

1. Push feature branch lên GitLab (`git push origin {branch}`)
2. Sau đó mới cherry-pick vào `release_staging` và push staging

**Why:** Feature branch phải có trên GitLab trước để tạo MR được.

---

## Sau khi push branch → ĐƯA LINK MR BẤM ĐƯỢC (không bắt user tự mò)

**Rule: push xong PHẢI đưa ngay link tạo MR dạng markdown BẤM ĐƯỢC, đã điền sẵn source + target branch.**

**Why:** Dán URL thô ra màn hình = user phải copy, mở tab, tự chọn lại target branch → mất thời gian và dễ chọn nhầm nhánh đích. Đây là lỗi đã bị nhắc nhiều lần.

**How to apply:**
```markdown
**BE (scrm):** [Tạo MR → release_product](https://gitlab.com/nhantocuiday/scrm/-/merge_requests/new?merge_request%5Bsource_branch%5D={BRANCH_URLENCODED}&merge_request%5Btarget_branch%5D={TARGET})
**FE (scrm_frontend):** [Tạo MR → release_product](https://gitlab.com/nhantocuiday/scrm_frontend/-/merge_requests/new?merge_request%5Bsource_branch%5D={BRANCH_URLENCODED}&merge_request%5Btarget_branch%5D={TARGET})
```
- `{BRANCH_URLENCODED}`: `/` phải encode thành `%2F` (vd `thiennq/ticket-thongke` → `thiennq%2Fticket-thongke`).
- `{TARGET}`: hỏi user muốn MR vào đâu (`release_product` / `release_staging`) — KHÔNG tự đoán.
- Repo nào push thì đưa link repo đó (BE + FE = **2 link**, đừng đưa 1).
- Kèm sẵn **mô tả MR** để user dán (nội dung + điều kiện triển khai + kết quả QA).
- Nếu có `glab` + đã `glab auth login` → tạo MR thẳng; không có thì đưa link, KHÔNG im lặng.

---

## Tạo MR THẲNG bằng git push-option — ĐỪNG bảo "không tạo được MR"

**Rule: GitLab tạo được MR NGAY trong lệnh `git push` qua push-option (`-o`), KHÔNG cần API/glab/token.** Đừng phản xạ "tôi chỉ push được nhánh, không tạo được MR" — sai. Kiểm chứng thực tế: 1 lệnh push tạo luôn MR #4678 (BE) + #3401 (FE) nhắm `release_product`.

**How to apply:**
```bash
git push -u origin {branch} \
  -o merge_request.create \
  -o merge_request.target=release_product \
  -o merge_request.title="..."
```
- GitLab nhận push → **tự tạo MR** server-side, in link MR ra trong output push (`remote: View merge request ... /-/merge_requests/{id}`) → lấy link đó đưa user.
- Push-option khác nhau: `merge_request.create` (bật tạo), `.target=` (nhánh đích — **hỏi user**, đừng đoán), `.title=`, `.description=`, `.remove_source_branch=true`, `.merge_when_pipeline_succeeds=true`.
- Nếu nhánh đã có MR mở → push-option **không tạo trùng**, chỉ cập nhật commit; GitLab vẫn in link MR đang mở.

**Khi nào tạo thẳng vs chỉ đưa link:**
- **User bảo "đưa MR để review" / "tạo MR"** → **CONFIRM trước khi tạo** (đây là git op outward-facing = theo luật double-confirm ở đầu file): trình *"sẽ push + tạo MR nhắm {target}, tiêu đề ..., OK tạo?"* → **chờ user gật** → mới chạy push-option. Tạo xong đưa link + báo rõ **"MR do lần push này vừa tạo"**.
- User chỉ nói "push nhánh" → push thường + đưa **link tạo MR bấm được** (mục trên), KHÔNG tự tạo MR.
- Đã lỡ tạo mà user không muốn → **đóng (close) MR** được ngay, giữ nhánh + commit.

> ⚠️ Tạo MR = thao tác outward-facing → **KHÔNG tự tạo dù user bảo "tạo MR"; phải CONFIRM lại một nhịp rồi mới tạo** (bài học: đã có lần tự tạo luôn khi user chỉ bảo "đưa MR để review" → user nhắc "khi tạo nhớ confirm lại"). Tạo xong **nói rõ là mình vừa tạo**, đừng để user tưởng có sẵn.

---

## Cherry-pick sang staging

Sau khi push feature branch xong, **luôn hỏi user confirm pick vào `release_staging`** — không bỏ qua bước này.

Quy trình — thực hiện trong folder được giao, không sang folder khác:
1. `git checkout release_staging`
2. `git pull origin release_staging` — fast-forward
3. `git cherry-pick <commit(s)>` — chỉ cherry-pick, KHÔNG merge feature vào staging
4. `git push origin release_staging`

**Tại sao không merge:** Feature base từ `release_product`, không phải staging — cherry-pick là đủ, merge sẽ kéo theo code thừa.

**KHÔNG pick trước rồi pull sau** — sẽ sinh merge commit thừa hàng trăm dòng.

🔴 **`cherry-pick -n` (không commit) KHÔNG tính là đã pick.** Nó chỉ đổ code vào **cây làm việc** —
build ra image được, tester test được, nhưng **nhánh `release_staging` vẫn RỖNG**. Dọn folder
(`reset --hard` / `checkout` nhánh khác) là mất sạch. Chỉ dùng `-n` khi mục đích là **chạy thử local**;
muốn staging thật sự có code thì phải `cherry-pick` **có commit** + `push`.

> Sự cố 30–31/07/2026 (591-3): suốt task chỉ `cherry-pick -n` để user build staging. Đẩy product xong,
> dọn folder → `release_staging` **không có 591-3** trong khi `release_product` **có** = staging tụt sau
> product; ai build lại staging hoặc tách nhánh mới từ staging là mất tính năng. Phát hiện nhờ user hỏi lại.
> Cách check nhanh (không cần checkout):
> `git cat-file -e origin/release_staging:<đường-dẫn-file-mới-của-feature>` → `KHÔNG` = nhánh chưa có.

## Bê phần code ĐANG SỬA DỞ giữa các nhánh (không commit rác, không lẫn nhánh nguồn)

Tình huống lặp nhiều: đang sửa trên cây `release_staging` (đã `cherry-pick -n` feature vào) rồi cần
đưa **đúng phần mới gõ tay** sang nhánh feature — không được kéo theo commit nào của staging.

```bash
# 1. Patch = diff UNSTAGED (index → worktree) = ĐÚNG phần mới gõ, không thể chứa commit của nhánh nguồn
git diff -- <đúng thư mục cần>  > /tmp/new.patch
grep "^diff --git" /tmp/new.patch          # soi danh sách file: loại config local (environment.*, package-lock)

# 2. Sang nhánh đích. Nếu cây đang BUILD → đừng checkout, dùng worktree tạm:
git worktree add /tmp/wt <feature-branch>

# 3. Áp bằng 3-way: lệch context thì BÁO CONFLICT, không vá lệch
git -C /tmp/wt apply --3way /tmp/new.patch

# 4. Verify KHÔNG lẫn nhánh nguồn — phải chỉ ra commit của mình:
git log --oneline origin/release_product..HEAD

# 5. commit + push, rồi gỡ worktree
git worktree remove /tmp/wt
```

- Dùng `--3way` vì file có thể đã **auto-merge với drift của staging** lúc cherry-pick → context khác nhánh feature.
- `git worktree` để **không đụng cây user đang build** (checkout đổi nhánh giữa lúc build = phá build).
- Luôn **loại `environment.stagingdebug.ts`/`launchSettings.json`** khỏi patch — config trỏ localhost, lọt sang là chết.

---

## Mang hotfix từ product/staging VỀ feature — lấy 1 file, đừng pick cả commit deploy

**Tình huống:** user hotfix thẳng trên `release_product` (vd sửa gate), commit đó **kèm bump version** (`.bat`, csproj) + có thể whitespace toàn cục. Cần đưa đúng phần sửa về nhánh `feature`.

**Cách đúng — checkout đúng file từ nhánh đích:**
```bash
git checkout feature/<branch>
git checkout origin/release_product -- path/to/File.cs   # lấy nguyên file (gồm hotfix), stage luôn
git diff --cached                                          # verify chỉ đúng phần cần
git commit -m "fix: đồng bộ <hotfix> từ release_product"
```

- **Đừng** `git cherry-pick <commit-deploy>`: sẽ kéo theo bump version `.bat`/csproj + noise không thuộc feature.
- Trước khi làm: `git diff feature origin/release_product -- <file>` để chắc file chỉ khác đúng phần hotfix (không lẫn thay đổi product-only khác).
- Nếu đang kẹt cherry-pick dở ở nhánh khác → `git cherry-pick --abort` (nếu chưa commit) hoặc hoàn tất trước, rồi mới `checkout feature`.
- File debug local (`launchSettings.json`, `environment.stagingdebug.ts`) → **stash trước khi đổi nhánh, pop lại sau** (khỏi lẫn/mất).

> Bài học (2026-07-20): user merge feature→product rồi hotfix gate thẳng trên product; mang về feature bằng `checkout origin/release_product -- UserService.cs` (feature khớp product luôn, hết diff nhiễu), tránh pick commit deploy dính version.

---

## Giới hạn folder

**Khi được giao làm việc trong folder nào, chỉ thao tác git trong folder đó — không tự ý sang folder khác.**

**Why:** Các folder khác có thể đang có task khác đang dở — can thiệp vào sẽ hỏng task của người khác.

**How to apply:** Nếu cần thao tác ở folder khác, hỏi user trước, không tự suy và hành động.
