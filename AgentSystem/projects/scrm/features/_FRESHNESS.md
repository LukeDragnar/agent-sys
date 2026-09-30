# Kho Feature — Freshness / Watermark (theo dõi độ tươi vs code)

> **Vấn đề:** Cẩm nang `features/` được build từ 1 **ảnh chụp code** (BE/FE nhánh `release_product` tại 1 thời điểm). Code thay đổi tiếp → docs cũ dần. File này **chốt watermark** (commit mới nhất lúc quét) + định nghĩa 2 cơ chế giữ docs tươi.

---

## 📌 WATERMARK hiện tại

> Cẩm nang feature phản ánh code BE/FE `release_product` tại các commit dưới đây. **Cập nhật mỗi khi quét code bổ sung/sửa feature.**

| Repo | Branch | Commit | Commit date (ISO) | Ngày quét (ghi docs) |
|------|--------|--------|-------------------|----------------------|
| **BE** (scrm) | release_product | `72ac442a74` | `2026-06-18T10:19:14Z` | 2026-06-20 |
| **FE** (scrm_frontend) | release_product | `e531064955` | `2026-06-18T10:18:59Z` | 2026-06-20 |

*(Ghi `short hash` + `commit_date`; đối chiếu drift dùng `commit_date`.)*

> 🔴 **Watermark = RANH GIỚI DELTA, không phải trang trí.** Lần quét kế chỉ đọc `watermark..origin/release_product` — **không bao giờ quét lại phần cũ**. Nếu KHÔNG dời watermark sau khi quét → session sau mất dấu → **quét lại từ đầu (ốm người)**. Đây là hạ tầng chống-quét-lại, giữ nó ĐÚNG là bắt buộc.

### 📊 Watermark theo MODULE (cho quét LẺ / incremental)

> Global ở trên = baseline mọi module (đều quét tại BE `72ac442a` / FE `e531064` @ 2026-06-18). Khi quét LẺ 1 module (không full) → **KHÔNG dời global** (sẽ đánh dấu tươi khống các module khác) → thay vào đó **thêm/cập nhật 1 dòng module** dưới đây. Drift-check tính delta **theo dòng module nếu có, không thì fallback global**.

| Module (feature folder) | BE commit đã quét | FE commit đã quét | Ngày quét |
|---|---|---|---|
| *(baseline: mọi module = global watermark)* | `72ac442a` | `e531064` | 2026-06-20 |
| `hoa-don-yeu-cau-xuat-logistic` (YeuCauXuatHoaDon) | `4c4f92260a` | `b37f90703` | 2026-07-13 |
| `sales/quan-ly-don-hang` (Order) | `77ae14985f` | `b37f90703` | 2026-07-13 |
| `sales/quan-ly-contact` + `khach-hang/quan-ly-thong-tin-kh` (Contact ngày sinh/địa chỉ) | `4c4f92260a` | `b37f90703` | 2026-07-13 |
| `ke-toan/phan-bo-chi-phi-mua-hang` + `PhieuMuaHang` (thông báo kho/in phiếu) | `4c4f92260a` | `b37f90703` | 2026-07-13 |
| `kho/ra-soat-reset-ton-kho` (RaSoatTonKho tồn/giá vốn) | `4c4f92260a` | `b37f90703` | 2026-07-13 |
| `kho` — OrderLogistic (DonHangCauHinhThuocTinhGiaoVan) + WarehouseProduct (GiaVonTheoKy) | `e617abcbbf` | ⚠️ chưa quét FE | 2026-07-15 |
| `ke-toan` — Accounting (gộp chiết khấu giá bán MISA) | `e617abcbbf` | ⚠️ chưa quét FE | 2026-07-15 |
| `bao-hanh` — Warranty (module MỚI: BaoHanh + 5 controller) | `e617abcbbf` | ⚠️ chưa quét FE | 2026-07-15 |
| `he-thong` — ESDocument (DonViElasticSearch) | `e617abcbbf` | ⚠️ chưa quét FE | 2026-07-15 |
| `facebook` — chat lại từ contact | `e617abcbbf` | ⚠️ chưa quét FE | 2026-07-15 |
| `cong-viec` — WorkflowNoti (noti công việc) | `e617abcbbf` | ⚠️ chưa quét FE | 2026-07-15 |
| `sale-dms` — SaleDMS (module MỚI: 11 controller, 19 entity) | `e617abcbbf` | ⚠️ chưa quét FE | 2026-07-15 |

> 🔴 **NỢ — ticket kho (`cong-viec/ticket-ho-tro`):** viết 2026-07-15 từ **Group4 (dev), TRƯỚC khi** nhánh `thiennq/ticket-thongke` merge product. Nhánh đó (thu hồi tin `ThuHoiTin`+`DaThuHoi`, độ ưu tiên `MucDoUuTienMa/Ten`, gửi ảnh chat, `SBTeamPhanTich`, `DanhSachTheoO`, CapNhat khóa-quyền-SB) **đã lên product 15/07 nhưng kho ticket CHƯA cập nhật**. Lần quét sau đọc delta `WorkFlow/`/`cong-viec` phải bổ sung. *(Watermark row "cong-viec — WorkflowNoti" chỉ là WorkflowNoti, KHÔNG bao gồm ticket.)*
>
> ⚠️ **Đợt quét 2026-07-15 chỉ BE** (`_SourceForBuild/scrm_build`, scan tới `e617abcbbf`, commit-date **2026-07-15 07:47 +00**). Trong lúc scan chạy, origin BE nhích thêm **+7 commit** → HEAD hiện `e7f46f1f4c` (7 commit này **CHƯA scan**, để đợt sau). **FE (`build_scrm_frontend` HEAD `49cda8f8`, commit-date 2026-07-15 09:50, drift 486) CHƯA quét** — 8 module trên còn nợ FE. Đợt sau quét FE + 7 commit BE tail thì mới điền tiếp.

→ Chỉ dời **global** khi đã quét lại **TOÀN BỘ** tới commit mới. Quét lẻ → dùng bảng module.

---

## ⚙️ Cơ chế 1 — GHI watermark sau mỗi lần quét (record) — **BẮT BUỘC, không bỏ**

**Khi nào:** sau 1 session quét code để **thêm/sửa feature** trong `features/` (Pha 2 hoặc bổ sung lẻ).

> 🔴 **Quét mà KHÔNG ghi lại latest = coi như CHƯA quét** (session sau lặp lại toàn bộ). Ghi latest là bước ĐÓNG BẮT BUỘC của mọi session quét, không phải tuỳ chọn.

**Làm:**
1. Lấy commit mới nhất `release_product` của BE & FE (path do dev chỉ định / `memory/local/paths.md` — **KHÔNG hardcode path ở file này**):
   ```bash
   git -C "<WORKING_BE>"  log -1 --format="%h %cI %s" origin/release_product
   git -C "<WORKING_FE>"  log -1 --format="%h %cI %s" origin/release_product
   ```
   *(Nếu không có `origin/release_product` thì fetch trước, hoặc fallback `release_product`/`HEAD`.)*
2. Ghi latest **đúng phạm vi đã quét**:
   - Quét **FULL** (mọi module tới commit mới) → cập nhật bảng **WATERMARK global**: `Commit`, `Commit date`, `Ngày quét` = hôm nay.
   - Quét **LẺ** (một/vài module) → thêm/cập nhật **dòng module** trong bảng "Watermark theo MODULE" (đừng đụng global).
3. Ghi 1 dòng vào "Lịch sử quét" cuối file (phạm vi + commit range đã xử).

> Watermark = "docs này đúng với code tới commit X". Ai đọc docs biết baseline; cơ chế 2 dựa vào đây để báo drift **chỉ phần chưa quét**.

---

## ⚙️ Cơ chế 2 — CHECK hằng ngày, gợi ý session mới (drift watch) — **LOCAL ONLY**

> Chạy **trên máy dev**, 1 lần/ngày (cron). Tạm thời cá nhân, chưa team-wide.

**Làm:**
1. `git -C "<WORKING_BE>" fetch origin release_product` + tương tự FE (chỉ fetch, không đụng working tree).
2. Lấy `commit_date` mới nhất `origin/release_product` của BE & FE.
3. **So với WATERMARK** trong file này:
   - **Mới hơn** (BE hoặc FE có commit sau watermark) → **báo user**: số commit mới + ngày + subject; **gợi ý "tạo session mới quét code bổ sung/cập nhật feature"** rồi cập nhật watermark.
   - **Không đổi** → im lặng (không làm phiền).
4. Không tự sửa docs; chỉ **gợi ý**. Việc quét cập nhật là session riêng do user khởi động.

**Trigger thủ công:** user nói "check freshness" / "code có mới không" → chạy ngay cơ chế 2.

**Cron (local):** đặt lịch chạy daily — xem `CronList`. Cron là máy-local, không commit.

### 📝 Log drift-check (LOCAL, state cho cron) — `memory/local/freshness-check.log`

> Mỗi lần chạy drift-check (cron hoặc tay) **ghi 1 dòng append** vào `memory/local/freshness-check.log` (gitignore, không commit). Mục đích: (a) **không báo lặp** — nếu latest commit không đổi so với dòng log trước thì im; (b) biết **cron còn sống** (dòng log mới mỗi ngày); (c) đối chiếu nhanh lần cuối thấy gì.

Format 1 dòng:
```
<ISO timestamp> | BE <latest_hash> drift=<n_commit_sau_watermark> | FE <latest_hash> drift=<n> | notified=<yes/no>
```
Ví dụ:
```
2026-07-10T17:00+07 | BE a670302 drift=560 | FE b37f907 drift=411 | notified=yes
```

**Logic dùng log:** đọc dòng cuối → nếu `latest_hash` BE&FE **giống hệt** dòng trước → đã báo rồi, **im** (chỉ ghi log `notified=no`). Khác → có commit mới hơn cả lần báo trước → **báo user** + ghi `notified=yes`. → drift-check **có state**, không spam mỗi ngày cùng một drift.

---

## 🧭 Lưu ý
- **KHÔNG hardcode path** trong file này (committed). Path BE/FE: hỏi dev / `memory/local/paths.md` mỗi session.
- Drift nhỏ (vài commit lẻ) → có thể gom, không cần session ngay; drift lớn / đụng module đã có feature → nên quét lại sớm.
- Khi quét lại: ưu tiên module có commit chạm tới (đọc subject/diff để biết feature nào ảnh hưởng), không cần quét full mỗi lần.

## Lịch sử quét
- **2026-06-20** — Pha 2: build ~128 feature × 4 file. Baseline BE `72ac442a74` / FE `e531064955` (release_product @ 2026-06-18).
- **2026-07-13** — Quét incremental (fan-out 4 lượt song song, range BE `72ac442a74..~4c4f9226`, FE `e531064955..b37f90703`):
  - **#2 YeuCauXuatHoaDon**: cập nhật `hoa-don-yeu-cau-xuat-logistic` (summarize/flow/dev) + registry ViettelInvoiceController + CauHinhController_OrderLogistic. LỚN NHẤT: **gỡ gate auto-publish** (cờ không còn chặn xuất HĐ, chỉ quyết nguồn TT người mua), Config B thêm `MotPhanMacDinh`, ecom set cờ=false lúc tạo, Viettel buyerCode, phí ship theo loại đơn.
  - **#3 Order**: `sales/quan-ly-don-hang` (dev/flow) + registry DonHangLogisticController. KM bội số/waterfall, lock 2 lớp hủy chốt⟷đăng đơn, hủy chốt không giới hạn 30 ngày (snapshot).
  - **#4 Contact/KH**: `sales/quan-ly-contact` + `khach-hang/quan-ly-thong-tin-kh` + registry ContactController/ContactImportController. Field ngày sinh + 4 cột địa chỉ import, propagate sang KH (backfill no-override), validate, tác nghiệp min 1', bỏ chặn phân bổ đơn đã chốt.
  - **#5 Kế toán**: TẠO mới registry ChungTuMuaDichVuController + feature `ke-toan/phan-bo-chi-phi-mua-hang` (4 file) + cập nhật INDEX/README/PhieuMuaHang. Phân bổ chi phí mua hàng (landed cost). 🔴 known-issue: migration SQL đã bị xóa khỏi repo → môi trường mới phải chạy DB tay.
  - **#1 Tồn kho/giá vốn**: TẠO mới registry `RaSoatTonKhoController` + feature `kho/ra-soat-reset-ton-kho` (summarize/flow/dev). RaSoatTonKho = rà soát/tái tạo tồn + giá vốn (4 loại phiên, 2 hướng Anchor/Stream — **Anchor đang hardcode**, 13+ stage rebuild, Redis lock, retry 3, temp-then-swap). Verify 8 path:line @ BE `4c4f9226`, sửa 4 điểm lệch (quyền per-action, HuongReset 2 path, +3 stage).
- **2026-07-15** — Quét incremental **CHỈ BE** (workflow fan-out 8 module, range BE `72ac442a..e617abcbbf`; git-log-delta per module đã verify; file mới verify tồn tại thật). **FE `49cda8f8` drift 486 CHƯA quét — còn nợ.** 8 doc mới:
  - **OrderLogistic**: TẠO `registry/kho/DonHangCauHinhThuocTinhGiaoVanController` (7 endpoint auto-gán thuộc tính giao vận theo SP, 3 đọc-mở/4 ghi-admin, `TI_OrderLogistic`). *(Còn nợ ghi: ApplyGiaVonOnDonHangLogisticConsumer Phase-5 giá vốn cross-service, DonHangLogisticChotDonSnapshot = hủy chốt đơn KHÔNG giới hạn 30 ngày, Common 7 endpoint nội bộ rà-soát-tồn-kho, TimDonCuaToi KPI cá nhân, Lazada ngang Shopee/Tiktok — đã ghi trong summary analysis, chưa tách node.)*
  - **WarehouseProduct**: TẠO `registry/kho/GiaVonTheoKyController` (tính lại giá vốn theo kỳ — SAGA 7 phase MassTransit, JobScanGiaVonTheoKyStuck, 12 endpoint).
  - **Accounting**: TẠO `features/ke-toan/gop-chiet-khau-gia-ban-misa/summarize`.
  - **Warranty** (module MỚI): TẠO `registry/bao-hanh/BaoHanhController` (BaoHanh + 5 controller: InDonConfig/LanXuLy/CauHinh/DMBaoHanhMoRong/Common).
  - **ESDocument**: TẠO `registry/he-thong/DonViElasticSearchController`.
  - **Facebook**: TẠO `features/facebook/chat-lai-tu-contact/dev`.
  - **WorkflowNoti**: TẠO `features/cong-viec/noti-workflownoti/summarize` (7 controller noti).
  - **SaleDMS** (module MỚI): TẠO `registry/sale-dms/README` (11 controller, 19 entity — DMS đại lý/tuyến/KPI).
