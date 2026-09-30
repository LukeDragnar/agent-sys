# API Workflow (Quản lý công việc) — Tham chiếu DUY NHẤT

> Bản dùng chung cho team. Gộp từ doc session trước (06/2026) + phát hiện mới (07/2026).
> **Kho là bản đồ, code/API live là chân lý** — giá trị id/option có thể đã đổi, verify lại khi cần.

## Thông tin chung
- **Base:** `https://api.sandbox.com.vn/workflow/api` (auth ở `…/auth`, file ở `…/data`).
- **origin/referer:** `https://sandbox.sandbox.com.vn`. **content-type:** `application/json`.
- **Envelope:** `{success, data, status, message, totalRecord, tabIndexError}`. HTTP **luôn 200** kể cả lỗi nghiệp vụ → **đọc `success`**, không dựa HTTP. Lỗi: `success:false` + `error` + `message`.

## 🔑 Xác thực = COOKIE `protoken` (KHÔNG phải Bearer)
- Gọi API: header `cookie: protoken=<JWT>`. Gửi Bearer → **401**.
- **Login (lấy JWT):** `POST /auth/api/Authen/login` body `{"userName":"<userName>","passWord":"..."}` → `data.token` (JWT, `exp` ~7 ngày). Worker lưu `token.txt`, tái dùng.
- **Danh tính người dùng** = claim `id` trong JWT payload (= DEVELOPER id). Worker `me()` decode base64 phần payload để tự biết "tôi là ai" — KHÔNG hardcode.
- Web thật dùng `/auth/api/Authen/login-encrypt` (user/pass mã hóa, token trả qua `Set-Cookie`) — không cần cho worker.
- **401 đột ngột** dù `exp` còn hạn (server vô hiệu session) → login lại. **Đừng login lặp liên tục** (throttle vài phút).

## Danh mục endpoint
| Chức năng | Method | Path |
|---|---|---|
| Danh sách việc (lọc) | POST | `/CongViec/TimTheoDieuKien` |
| Chi tiết việc | POST | `/CongViec/TimTheoId/{id}` (body rỗng) |
| Tạo việc (chỉ tạo mới) | POST | `/CongViec/Save` |
| Cập nhật 1 trường | POST | `/CongViec/CapNhatTruong` |
| Auto-arrange lịch (KHÔNG ghi DB) | POST | `/CongViec/TuDongSapXepThoiGian` |
| Lịch sử thay đổi (audit) | POST | `/CongViec/LayLichSuCongViec/{id}?pageIndex=&pageSize=` |
| Xóa việc (không hoàn tác) | POST | `/CongViec/Xoa/{id}` |
| Danh sách user (id→tên) | POST | `/CongViec/LayDanhSachUser` (body rỗng) |
| Danh sách tab | POST | `/CongViecTab/TimTheoDieuKien` |
| DM ưu tiên | POST | `/DmMucDoUuTien/TimTheoDieuKien` |
| DM trạng thái | POST | `/DmTrangThai/TimTheoDieuKien` |
| DM trường động | POST | `/DmCongViecMauField/TimTheoDieuKien` |
| Config view timeline | POST | `/CauHinhView/TimTheoDieuKien` (body cần `module:"timeline"`) |
| Upload file | POST | `/data/Files/UploadFile` (multipart, field=`file`) |
| Gắn file vào việc | POST | `/CongViecFile/Save` |
| Tải/xem file | GET | `/data/Files/GetFile?id=<fileId>&size=o` (`o`=gốc) |

## CongViec/TimTheoDieuKien (danh sách + lọc)
Body: `{"pageInfo":{"page":1,"pageSize":N},"sorts":[],"chiLayGoc":bool,"tabFilter":{...}}`. `data`=mảng việc, `totalRecord`=tổng khớp (phân trang: lặp `page`).
- **Không filter** → trả TẤT CẢ (~1010). **Lọc server-side** dùng `tabFilter`:
  ```json
  "tabFilter":{"matchMode":"all","conditions":[{"field":"ext_DEVELOPER","operator":"equals","value":"<userId>"}]}
  ```
  - `field`: trường lõi (`ma`,...) hoặc **trường động = `ext_<MA>`** (gạch DƯỚI, vd `ext_DEVELOPER`, `ext_LARK-ID`).
  - `operator`: chỉ **`equals`** đáng tin. `contains` thất thường; `like`/`startsWith` bị bỏ qua.
  - Lọc trường động cần **`chiLayGoc:false`**.
- **Lấy việc con / theo mã:** `chiLayGoc:false` + `tabFilter ma equals <MA>`, rồi **lọc client** `data.find(x=>x.ma===MA)` (data trả cả path cha→con).
- ⚠️ Bản danh sách (07/2026) **không trả `idTrangThai`/`tenTrangThai`** ổn định — trạng thái vòng đời ở trường động **`TRANG-THAI-XU-LY`**.
- **"Việc của tôi"** (worker `fetch_mine`): `tabFilter ext_DEVELOPER equals <me()>` + re-verify client `me() ∈ extFields.DEVELOPER`; bỏ `TRANG-THAI-XU-LY=="15. Đã Update"`.

## Cấu trúc việc (đọc)
`id, ma, tieuDe, moTa, idMucDoUuTien/tenMucDoUuTien, ngayBatDauDuKien, ngayKetThucDuKien, ngayBatDauThucTe, ngayKetThucThucTe, estimate, level, soCongViecCon, idCongViecCha, listUser, extFields{...}, files`.
- **extFields** = trường động, key=`ma` (giá trị string; mảng bị stringify). Đọc ở `extFields`, GHI qua `fields[]`/`ext:` (map qua id).
- Chi tiết (`TimTheoId`) có thêm: `tenMucDoUuTien`, `estimate`, `ngay...DuKien`, `flowNodeStatuses`, `files`.

## CapNhatTruong (GHI 1 trường) ✅
Body: `{"Id":guid,"Field":<tên>,"Value":<string, null=xóa>,"ClientTimestamp":<unix ms>}` → `data:true`.
- **Trường lõi:** `tieuDe, moTa, idTrangThai, idMucDoUuTien, ngayBatDauDuKien, ngayKetThucDuKien, estimate, ngayBatDauThucTe, ngayKetThucThucTe`.
- **Trường động:** `Field="ext:<idDmCongViecMauField>"`, `Value=chuỗi option/id thẳng`.
- **Format Value:** ngày→`"YYYY-MM-DD"` (BE lấy `.Date`); estimate→số chuỗi; select-user single→id; select-user multi→`"[\"id\",...]"`; select-custom→nhãn thẳng.
- `idTrangThai` có **side-effect**: tự set `ngayBatDau/KetThucThucTe` theo mã trạng thái (DANG_XU_LY/DA_HOAN_THANH...).
- **OT conflict (ClientTimestamp) HIỆN TẮT** ở BE (comment) → 2 lần liên tiếp vẫn OK (doc cũ bảo cần ≥3s — không còn đúng). Vẫn truyền `Date.now()` ms.

## TuDongSapXepThoiGian (GỢI Ý lịch — KHÔNG ghi DB) ✅
Body: `{"Tasks":[{"Id","NgayBatDauDuKien","NgayKetThucDuKien","Estimate"}],"From","To","HoursPerDay":8,"Algorithm":"work-cap"}`
→ `data:{Updates:[{TaskId,NewNgayBatDau,NewNgayKetThuc}], SkippedNoFit:[id], TotalConsidered}`.
- Chỉ xét task có ngày kết thúc ≥ From; nhồi theo capacity `HoursPerDay`. **Không lưu DB** → preview an toàn, ghi thật qua `CapNhatTruong`.

## Save (TẠO MỚI task) — ✅ đã verify thật 2026-07-06
`POST workflow/api/CongViec/Save` → `data` = GUID việc mới. DTO `CongViecSaveDto` (camelCase). **KHÔNG kiêm update** — sửa sau tạo dùng `CapNhatTruong`.

**Payload TẠO VIỆC CON (đã chạy, mã tự sinh `{cha}-{n}`, vd 591 → 591-2):**
```json
{ "ma":null, "tieuDe":"...", "moTa":"<p>HTML</p>",
  "estimate":8, "ngayBatDauDuKien":"2026-07-06", "ngayKetThucDuKien":"2026-07-06",
  "congViecMauId":"<template>", "idCongViecCha":"<id việc cha>", "listIdUser":["<userId>"],
  "fields":[ {"idCongViecMauField":"<DEVELOPER-id>","giaTri":"<userId>"},
             {"idCongViecMauField":"<TRANG-THAI-XU-LY-id>","giaTri":"8. Dev tiếp nhận"},
             {"idCongViecMauField":"<PL-TASK-id>","giaTri":"Task"} ] }
```
- `idCongViecCha` = việc lớn → thành **việc con** (mã tự nối `-n`). `congViecMauId` mẫu BA = `c69088f3-57fb-4d34-a4e1-024f92a4e2c1`.
- `estimate`/`ngay...DuKien` là **top-level** (không phải trong fields). Ngày = `"YYYY-MM-DD"`.
- **Gán dev** = fields DEVELOPER (không chỉ listIdUser) — vì board/filter lọc theo ext DEVELOPER.

### 🔴 Field-id ổn định (để tạo + lọc):
| Field | idCongViecMauField |
|---|---|
| DEVELOPER (Dev phụ trách) | `b375dd8e-216b-472e-a4c6-6497c7215c92` |
| TRANG-THAI-XU-LY | `9bb25840-a9a7-4a22-ad58-7838d8c6a354` |
| PL-TASK (Phân loại Task) | `4695c22a-ca1d-4865-b4cd-2e7f6ba1b3c5` |

### ⚠️ ĐỂ TASK HIỆN TRÊN TIMELINE "theo nhân sự", khi Save PHẢI set đủ 4 thứ (khớp 4 đk filter):
1. **DEVELOPER** = user (∈ list dev của view) · 2. **TRANG-THAI-XU-LY** ∈ [8,9,11] · 3. **estimate** ≠ 0 · 4. **PL-TASK** ∈ [Task, Bug].
> Thiếu bất kỳ cái nào → task tạo được (hiện ở danh sách) nhưng **ẩn trên timeline**. (Lỗi đã gặp: quên PL-TASK → 4 việc 591-x ẩn.)
- Cập nhật SAU tạo mỗi trường → `CapNhatTruong` (ext:`{id}` cho field động).

## Master data
- **DM chung:** `POST /<Dm>/TimTheoDieuKien` body `{pageInfo:{page:1,pageSize:1000},sorts:[],keyword:""}`.
- 🔴 **Tìm việc theo từ khóa: `CongViec/TimTheoDieuKien` BỎ QUA `keyword`** (truyền vào vẫn trả nguyên trang đầu) → phải kéo hết (paging 300/lần, ~2.4k việc) rồi **lọc client-side** theo `tieuDe` + `extFields.DEVELOPER`. Dùng để tra **mã việc anh em** cùng khách/cùng vùng nghiệp vụ trước khi sửa bug.
- 🔴 **Lệnh `raw` của worker CẮT output ở ~4000 ký tự** → JSON vỡ, `json.load` chết. Cần data lớn thì `import sb_work` rồi gọi thẳng `sb_work.api(path, body)` (nhớ `PYTHONIOENCODING=utf-8` trên Windows, không thì `UnicodeEncodeError` cp1252 khi in tiếng Việt).
- **DmCongViecMauField** (trường động): mỗi mục `{id, ma, ten, dataType, options(JSON: selectMode/source/items/default), isSystem}`. **Ghi theo `id` (ổn định), `ma` có thể đổi.**
- **Trạng thái xử lý** (`TRANG-THAI-XU-LY`, id `9bb25840-a9a7-4a22-ad58-7838d8c6a354`, select single) — vòng đời, giá trị LIVE 07/2026: `1. Task cần duyệt`→`2. Task đã duyệt`→`3/4/4.1/5/6 BA-*`→`7. CTO đã phân tích`/`7.1 CTO tạm bỏ qua`→`8. Dev tiếp nhận`→`9. Đang dev`→`10/10.1/10.2 Dev*`→`11. Dev ReOpen`→`12. Đang Test`→`13. Test hoàn thành`→`15. Đã Update`→`16. Dev phân tích thêm`/`14. Leader đã review`. (verify live vì đổi thường.)
- **Ưu tiên** (`DmMucDoUuTien`): Cao/Trung bình/Thấp/Gấp-Xử lý ngay. **User** (`LayDanhSachUser`): `{id, ten, userName}` (~66 user) — map id→tên (worker `users`).

## Timeline view "theo nhân sự" (/work/custom-view)
- Data qua `CongViec/LayDanhSachNhomPhanTrang` (group theo user), field ngày = `ngayBatDauDuKien/ngayKetThucDuKien` (chế độ "Dự kiến").
- Mỗi tab = 1 **CauHinhView** (`configJson.filter` + `groupBy`). View "Team BackEnd" filter 4 đk (AND): `Dev phụ trách ∈ list` · `Trạng thái xử lý ∈ [8. Dev tiếp nhận, 9. Đang dev, 11. Dev ReOpen]` · `Ước tính ≠ 0` · `Phân loại ∈ [Task, Bug]`. → việc "7. CTO đã phân tích" bị ẩn tới khi `setxuly` sang "8. Dev tiếp nhận".
- Token cá nhân chỉ thấy view "Mặc định" của mình; view team dùng chung không trả qua API cá nhân.

## Đính kèm ảnh (nếu cần)
1. `POST /data/Files/UploadFile` multipart field `file` → `data:{id,fileName,extention,path,size}`.
2. `POST /CongViecFile/Save` `{congViecId,fileId,fileName,extention(BỎ dấu chấm),path,size}`.
- Tải: `GET /data/Files/GetFile?id=<fileId>&size=o` (o=gốc; s/m/l=thumb).

## Xóa (⚠️ không hoàn tác)
`POST /CongViec/Xoa/{id}` body rỗng → `data:true`. LUÔN xác nhận user trước.
