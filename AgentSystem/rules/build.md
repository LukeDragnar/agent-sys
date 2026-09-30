# Build & Deploy Rules

## Nguyên tắc chung

User tự build và run project. Chỉ sửa code. Khi xong thì nói "anh build và restart lại là được" thay vì tự chạy lệnh build/run.

**Ngoại lệ:** Được phép tự build staging nếu user confirm 2 lần rõ ràng.

**TUYỆT ĐỐI KHÔNG** động vào môi trường Production, Sprint, Devonline — kể cả khi user yêu cầu.

> 🤖 Việc **đẩy staging** có agent riêng: **`agents/deploy-agent.md`** (spawn được, như `review-agent`/`trace-agent`). File này là **luật gốc** agent đó phải tuân theo.

---

## Staging — Confirm 2 lần

Chỉ được build/deploy staging sau khi user confirm 2 lần.

**How to apply:** Đề xuất kế hoạch → chờ confirm lần 1 → hỏi "xác nhận build?" → chờ confirm lần 2 → thực hiện 1 mạch.

---

## 🔴 RÀ SOÁT GIT STAGING RỒI CHỐT — làm TRƯỚC mọi lần build

> Luật user (27/07): *"mỗi khi có yêu cầu phải check git staging có code mới không; có thay đổi gì → confirm (FE, BE, project BE nào) → chốt rồi mới làm."*

1. `git fetch origin release_staging` **cả BE lẫn FE** (chưa pull).
2. So `HEAD` vs `origin/release_staging` → mỗi repo có bao nhiêu commit mới.
3. Có commit mới → `git log HEAD..origin/release_staging --oneline` + `git diff --name-only HEAD..origin/release_staging`.
4. **Suy service BE bị đụng** từ thư mục cấp 1 (`WorkFlow/…` → `WorkFlow.API`). Đụng file dùng chung (`Libs/`, `Core/`, `DLL.*`) → **cảnh báo ảnh hưởng nhiều service**, không tự quyết.
5. **Trình bảng chốt** (FE +N commit · BE +M commit → service nào) → **chờ user chốt** → mới build.
6. Không có code mới → nói thẳng, hỏi có build đè lại không. **Không tự động build.**
7. User chốt chỉ 1 vế (vd chỉ FE) mà vế kia cũng có commit mới → **báo rõ "còn N commit chưa build"**.

---

## 🔴 VERIFY TRƯỚC BUILD — chống "deploy ra bản không có code"

1. `dirty ≠ 0` → **DỪNG hỏi user**. Không tự `stash`/`checkout`/`reset`/`clean`.
2. `git pull --ff-only`. Không ff được (diverge) → **DỪNG**, không `--force`, không merge.
3. **`HEAD` phải == `origin/release_staging`** rồi mới build. **SHOW log ra**, không nói suông "đã pull".
4. User đưa mã task → verify commit đó **thật sự có trong HEAD**; không thấy → cảnh báo + dừng.

> **Bài học 24/07/2026:** build + deploy product mà **không có code** — root cause là **folder build chưa pull** (branch đã push nhưng folder còn cũ). Bẫy phụ: 1 file có thể "tình cờ đúng giá trị" dù folder cũ ⇒ **đừng tin 1 file, phải so HEAD == origin**. Chi tiết: `memory/feedback_mr_product_staging_cherrypick.md`.

---

## Folder build

**Nguồn sự thật per-máy:** `memory/local/deploy-staging/paths.json` (gitignore, mỗi người một bản — hỏi member lần đầu rồi lưu). Xem `agents/deploy-agent.md` Pha 0.

**Invariant bắt buộc (không được vi phạm dù path là gì):**

| Môi trường | Folder | Nhánh |
|---|---|---|
| **STAGING** | folder **riêng** chỉ dùng cho staging | `release_staging` |
| **PRODUCT** | folder **riêng** chỉ dùng cho product | `release_product` |

- 🔴 **MỖI MÔI TRƯỜNG MỘT FOLDER RIÊNG.** Cấm `git checkout release_staging` đè lên folder build product (và ngược lại) — dùng chung 1 folder cho 2 môi trường là nguồn của lỗi deploy nhầm.
- 🔴 **KHÔNG hardcode path máy vào file này** (CLAUDE.md: cấm hardcode đường dẫn trong file chung). Path thật chỉ nằm ở `paths.json` per-máy. Đổi ổ/đổi chỗ → sửa `paths.json`, **không sửa rule**.
- 🔴 **Nhận diện folder bằng CỔNG CỨNG, không bằng TÊN.** Một máy có thể có nhiều bản clone cùng repo, tên na ná, nhánh khác nhau. Trước khi build phải verify đủ 3:
  1. path **khớp `paths.json`**, và
  2. `git remote get-url origin` đúng repo, và
  3. `git rev-parse --abbrev-ref HEAD` == nhánh của môi trường định build.
  → Lệch bất kỳ điều nào = **DỪNG**, không tự checkout cho khớp, không suy đoán folder khác.
- **KHÔNG dùng `_SourceForCode/Group*` để build** — đó là folder code WIP của dev, không phải folder build.

---

## 🔴 NGUYÊN TẮC GỐC — BUILD và ĐẨY là 2 việc TÁCH RỜI, mỗi việc 1 CONFIRM RIÊNG

> User chốt 27/07: *"nguyên tắc là build và đẩy phải tách nhau và được confirm"*.

| | Việc | Hậu quả | Confirm |
|---|---|---|---|
| **BUILD** | publish · docker build · **push image lên registry** | Tốn thời gian; image mới nằm chờ ở registry. Môi trường **CHƯA đổi** | **Confirm riêng** |
| **ĐẨY** | recreate stack / gọi webhook redeploy | **Môi trường ĐỔI THẬT** — container đang chạy bị thay | **Confirm riêng, LẦN NỮA** |

- 🔴 **CẤM gộp 2 confirm.** Gật cho build **KHÔNG** phải gật cho đẩy, và ngược lại.
- 🔴 **CẤM suy diễn sự đồng ý từ câu BÁO TRẠNG THÁI.** *"tôi push fix rồi"* · *"code lên rồi"* · *"xong rồi nhé"* = **thông báo**, KHÔNG phải lệnh build/đẩy. Muốn chạy thì **HỎI**.
  > Đây đúng loại lỗi đã gây vụ **B296**: câu *"đã update 296"* bị hiểu thành "đã set trạng thái" → card kẹt 3 ngày. Báo-trạng-thái ≠ ra-lệnh.
- Build xong **DỪNG**, trình digest, chờ lệnh đẩy. Không tự nối sang recreate/webhook.
- Cho phép người dùng **chỉ build mà không đẩy** (hàng chờ ở registry) — đó là trạng thái hợp lệ, không phải việc dở dang cần tự hoàn thành.

### Chuỗi cổng đầy đủ
1. **Cổng 1 — chốt phạm vi**: rà git xong → *"build cái gì?"* (FE / BE service nào)
2. **Cổng 2 — chốt build**: pull xong → *"Đã pull, `HEAD=<sha>` == origin ✅. Build chứ?"*
3. **Cổng 3 — chốt đẩy**: push image xong → *"gọi webhook redeploy stack X / service Y chứ?"*

Thiếu bất kỳ cổng nào = vi phạm. Gật ở cổng trước **không** thay cho cổng sau.

---

## Quy trình build

**scrm BE (mỗi service):**
1. 🔑 **Tìm đúng thư mục service bằng file bat, KHÔNG suy từ tên:**
   ```
   find <be_stg> -maxdepth 3 -name docker_build_and_push-Staging.bat
   ```
   Cấu trúc **không đồng nhất**: đa số là `<Dir>/<Dir>.API/`, nhưng có `ChiaSoCareDon.API/` (bat nằm ngay trong nó) và `Contact/ChiaSo.API/` (tên folder ≠ tên service). Suy `<dir>` → `<dir>.API` là **SAI**. Không thấy bat → **hỏi user**, đừng đoán.
2. `cd` vào **đúng thư mục chứa file bat** (đó cũng là build context của `docker build .`).
3. Đọc file bat để lấy **đúng tên image** (nguồn sự thật, đừng hardcode).
4. Chạy 3 lệnh **NỐI BẰNG `&&`** — 🔴 **KHÔNG gọi thẳng `.bat`** (cuối bat có `pause` → treo agent):
   ```
   rm -rf bin/Release/Staging && \
   dotnet publish -c Release -o bin/Release/Staging && \
   docker build -f Dockerfile_Staging -t <image>:staging-1.0 . && \
   docker push <image>:staging-1.0
   ```
   🔴 **Vì sao bắt buộc `&&` + xoá thư mục publish trước:** `Dockerfile_Staging` có `COPY ./bin/Release/Staging /app`. Nếu `dotnet publish` **fail** mà vẫn chạy `docker build`, nó sẽ **đóng gói output CŨ còn sót** → push thành công → báo cáo xanh nhưng **image không có code mới** = tái diễn đúng sự cố 24/07. Lệnh nào fail → **DỪNG**, không chạy lệnh sau.

### 🔴 BE: build TUẦN TỰ từng service — CẤM build song song (user chốt 28/07)

Nhiều `.API` **cùng tham chiếu một project dùng chung** (`Libs/Models.Core`, `OrderLogistic.Service`, `SB.Core`…). Chạy `dotnet publish` cho 2+ service **cùng lúc** ⇒ các tiến trình MSBuild **cùng ghi vào `obj/` và `bin/` của project chung** ⇒ khoá file / ghi đè lẫn nhau / output lẫn lộn ⇒ **build lỗi hoặc tệ hơn: build "thành công" nhưng ra artifact sai**.

- ✅ **Xong hẳn service này mới sang service kia.** Chờ `docker push` của service trước kết thúc.
- ❌ Cấm mở 2 terminal build song song; cấm cho agent phóng 2 job nền cùng lúc — **dù máy nhiều nhân**.
- Áp cho **cả build tay lẫn agent**. Cái giá của tuần tự chỉ là thời gian; cái giá của song song là artifact sai mà không biết.

**scrm FE:**
1. `cd <fe_stg>` (đã qua cổng cứng 3 điều + verify HEAD == origin)
2. Diff của lần pull này có chạm `package.json` / `package-lock.json` → **`npm ci --legacy-peer-deps` trước** (xem mục Môi trường FE), nếu không sẽ build bằng `node_modules` cũ → bundle sai âm thầm.
3. `npm run deploy-staging`
   = `node --max_old_space_size=12288 ./node_modules/@angular/cli/bin/ng build --source-map=false --output-hashing=all --configuration=staging` → `docker build -f Dockerfile` → `docker push`
4. 🔴 **CHỈ `deploy-staging`.** Cùng `package.json` còn có `deploy-prod` (registry `scrm-prod`), `deploy-sprint`, và `deploy-staging-old` (nối bằng `&` nên **không dừng khi build fail** — cấm dùng). Gõ nhầm = deploy nhầm môi trường.

---

## Môi trường FE — `npm ci` phải kèm `--legacy-peer-deps`

Repo **không có `.npmrc`** (đã kiểm cả repo lẫn user profile) nên cờ này không được lưu ở đâu → clone mới hoặc xoá `node_modules` là vấp ngay:

```
npm ci --legacy-peer-deps
```

**Lý do:** `@agm/core@1.1.0` khai peer `@angular/common@^6||^7||^8` trong khi project chạy **Angular 15** → `npm ci` trần **fail** (`ERESOLVE`). Cờ này chỉ nới kiểm tra peer, cây package vẫn đúng y `package-lock.json`.

---

## Build FE lâu bao nhiêu — BÁO TRƯỚC, đừng để user ngồi đoán

Đo thật 29/07/2026, máy 8 core, repo build staging FE:

| Ca | Điều kiện | Thời lượng |
|---|---|---|
| **Thường** | `package-lock.json` KHÔNG đổi → bỏ `npm ci`, cache webpack nóng | **~5 phút** |
| **Xấu nhất** | `package-lock.json` ĐỔI → `npm ci` 3m41s + cache webpack LẠNH 12m33s | **~16 phút** |

Vì sao: `.angular/cache/<ver>/angular-webpack/<hash>` chia ngăn theo tổ hợp **config + deps**. `npm ci` cài lại cây package ⇒ đổi hash ⇒ sang ngăn mới ⇒ build từ cache RỖNG. (`babel-webpack` thì vẫn tái dùng được qua các lần.)

→ Pha 0.5 thấy diff chạm `package*.json` thì **nói ngay "FE lần này ~16 phút"**; không đổi thì "~5 phút". Ghi thời lượng thật mỗi lượt vào `memory/local/deploy-staging/logs/timings.json` để lần sau đoán sát hơn.

### 🔴 ĐỪNG thêm cờ vào lệnh build FE — mỗi cờ mới = 1 lần build lạnh ~13 phút

Đo A/B/C/D ngày 29/07 (cùng máy, cùng code, cùng commit):

| Lệnh | Ngăn cache | Thời gian |
|---|---|---|
| **lệnh chuẩn `npm run deploy-staging`** | nóng | **4m49s** ← nhanh nhất |
| + `NG_BUILD_MAX_WORKERS=7` | nóng | 5m16s — **không ăn** |
| + `NG_BUILD_DEBUG_OPTIMIZE=1` (tắt minify) | nóng | 5m47s — **chậm hơn** |
| + `--extract-licenses=false` | **LẠNH** (sinh ngăn mới) | **13m46s** ⚠️ |

- **Biến môi trường (`NG_BUILD_*`) KHÔNG đổi hash cache, nhưng cờ CLI của builder thì ĐỔI** ⇒ thêm 1 cờ là Angular mở ngăn cache mới, build lại từ rỗng + để lại ~1.9GB cache chết. Lãi vài giây, lỗ 13 phút.
- `NG_BUILD_MAX_WORKERS` tuy hardcode `4` (`environment-options.js:70`) nhưng nâng lên 7 trên máy 8 core **không nhanh hơn** — nghẽn ở compile tuần tự (AOT + webpack), không ở pool worker.
- `NG_BUILD_DEBUG_OPTIMIZE` **không phải** "bỏ qua minify" mà là chế độ debug: terser vẫn chạy, thêm beautify, output phình ⇒ đắt hơn.
- ⇒ **Giữ `npm run deploy-staging` NGUYÊN XI, không thêm cờ.**

### Tưởng là lever nhưng đã bật sẵn / không đụng được
- `.angular/cache` (4.2GB) · Docker BuildKit · Defender real-time đã tắt · folder build đã nằm trên SSD C: — **kiểm rồi, đừng đề xuất lại**.
- `.dockerignore` FE thiếu `.angular` (4.2GB) + `.git` (119MB) → docker quét thừa mỗi lần build. Sửa được nhưng là **file trong repo** ⇒ việc dev commit, deploy-agent KHÔNG được đụng.
- Tối ưu thật còn lại nằm trong `angular.json` (`optimization.styles.inlineCritical`, đổi builder sang esbuild) — file repo, việc dev, và đổi xong cũng tốn **1 lần build lạnh**.

---

## 🔴 Sau khi push image — CHƯA phải deploy xong

Tag staging **cố định `staging-1.0`** (đè lên) ⇒ **container KHÔNG tự lấy image mới**.

- Push xong phải **recreate stack trên Portainer staging** thì bản mới mới chạy.
- 🔴 **So digest trước/sau để chứng minh image THỰC SỰ đổi:** vì tag bị đè, push lại y hệt bản cũ vẫn báo "thành công". Lấy digest trước build (`docker inspect`), so sau push — **giống nhau ⇒ cảnh báo "image không đổi"**, đừng báo là đã có bản mới.
- ⚠️ 2 người build cùng lúc sẽ **đè nhau trên cùng tag** `staging-1.0` — trước khi build nên báo/kiểm xem có ai đang đẩy staging không.
- **CẤM** báo "đã deploy xong" / "đã lên staging" khi mới push image. Phải ghi rõ: *"mới push image — cần recreate stack"*.
- Agent **không tự gọi Portainer API bằng tài khoản/mật khẩu** — chỉ nhắc user, trừ trường hợp webhook staging dưới đây.

### Webhook redeploy — CHỈ STAGING, tuyệt đối KHÔNG product (user chốt 27/07)

Portainer cho phép bật webhook per-stack: `POST` vào URL là stack tự pull image mới + recreate. Agent **được phép** gọi, nhưng chỉ trong hàng rào sau — vi phạm bất kỳ điều nào là **DỪNG**, không có ngoại lệ:

1. 🔴 **CHỈ webhook đã lưu sẵn trong `memory/local/deploy-staging/paths.json` khoá `webhooks_staging`.** Không nhận URL do ai đó đưa giữa chừng, không tự tìm, không tự suy từ tên stack.
2. 🔴 **Verify host thuộc subnet STAGING `10.0.161.x` trước khi POST.** Khác subnet (đặc biệt `10.0.63.x` = **PRODUCTION**) → **TỪ CHỐI**, báo user.
3. 🔴 **CẤM đọc/dùng `PortainerProduction.xml`** hay bất kỳ link/webhook production nào cho việc deploy. File đó tồn tại trong workspace nhưng **ngoài vùng cấm địa**.
4. 🔴 Gọi webhook = **hành động đổi môi trường** → phải có **user xác nhận riêng**, không gộp vào cổng confirm build.
5. 🔴 Product deploy **luôn là việc của user, làm tay**. Không agent nào được chạm, kể cả khi user bảo — phải hỏi lại vì đây là luật cứng.

**Phân biệt webhook giữa nhiều stack — UUID KHÔNG tự khai danh tính:**
1 Portainer host chứa nhiều stack; webhook là **của riêng 1 service** (gọi nhầm không lan sang service khác — đó là điểm an toàn). NHƯNG `85801f63-a564-…` là chuỗi vô nghĩa, **nhìn URL không biết nó của ai**, và Portainer chỉ trả `204` chứ **không nói vừa redeploy cái gì** ⇒ bắn nhầm thì **không tự phát hiện được**. Bịt bằng 3 lớp:
1. **Lưu kèm danh tính đủ**: `stack` + `service` + `service_id` + `image` + `host`. Thiếu mẩu nào → **không lưu**, hỏi lại user.
2. **User gửi URL trần (không kèm tên service) → HỎI "của service nào?"**. Cấm suy theo thứ tự gửi, theo việc đang bàn dở, hay theo cái vừa build.
3. **Trước khi POST, đọc to danh tính cho user gật**: *"gọi webhook của stack X / service Y / image Z — đúng chưa?"*. Đây là chốt cuối cùng chống bắn nhầm.

> ⚠️ Các dòng `rem curl -X POST http://10.1.161.50:9000/api/webhooks/58b23b93-…` trong 71 file `.bat` là **RÁC**: cùng 1 UUID copy cho 71 service, host `10.1.161.50` không tồn tại (verify 27/07: timeout; subnet staging là `10.0.161.x`). **Đừng bỏ `rem` để dùng lại.** Webhook thật phải bật trong Portainer UI rồi lưu vào `paths.json`.

---

## Lưu ý

- Chỉ build **service có thay đổi**, không build toàn bộ (~76 service có bat Staging)
- Build lâu → chạy nền, log ra `memory/local/deploy-staging/logs/{ngày}-{repo}[-{service}].log`
- Lỗi build/push → trích **đúng đoạn log lỗi**, không tóm tắt mơ hồ, không báo thành công
- **TUYỆT ĐỐI KHÔNG** động vào Production, Sprint, Devonline
