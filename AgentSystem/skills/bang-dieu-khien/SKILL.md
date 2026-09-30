# Skill: BẢNG ĐIỀU KHIỂN (dashboard ngày)

> Sinh **1 file HTML/ngày** gom mọi thứ cần nhìn vào 1 màn — để không phải hỏi lại Claude mỗi lần,
> và không bị trôi khi context reset. Mỗi ngày 1 file, xem lại được theo ngày.

## Kích hoạt

🔴 **NGÀY MỚI = TỰ ĐỘNG RA BẢNG, KHÔNG CHỜ USER HỎI.** Câu chào đầu tiên của một ngày mới
(*"chào ngày mới"* · *"nay 31/7 nhé"* · *"sáng rồi"* · bất kỳ câu nào cho thấy đã sang ngày khác) →
**việc đầu tiên: chạy radar → spawn agent render bảng của NGÀY HÔM NAY → đưa link**, rồi mới bàn việc.
thiennq đã phải nhắc (*"chưa tạo bảng điều khiển à"* · *"cứ phải nhắc vậy"*) — **bị nhắc là đã hỏng**:
bảng sinh ra để anh ấy KHỎI phải nhớ, mà lại bắt anh ấy nhớ nhắc thì mất sạch tác dụng.

Ngoài ra chạy lại khi: user nói *"mở bảng"* · *"bảng điều khiển"* · *"làm mới bảng"* · *"dashboard"* ·
*"cho tôi cái link bảng"* · hoặc **sau khi chốt/đóng bất kỳ việc gì** (đóng task, dọn folder, phát hiện
rủi ro) → **chạy lại + đưa link full**.

## 🔴 AI chạy: **SPAWN AGENT**, đừng làm inline
thiennq chốt 29/07: *"sau cần update cái bảng điều khiển bạn spawn agent mà làm nhé"*.

**Why:** cập nhật bảng là việc **cơ học và tốn thời gian** — sửa `dashboard-notes.md`, chạy script ~2 phút
(quét 36 repo), verify HTML. Làm inline thì phiên chính đứng chờ và ngốn context, trong khi session chính
đang lo việc quyết định. Tách ra agent con: phiên chính chỉ nhận lại **link + tóm tắt cái gì đã đổi**.

**Prompt mẫu cho agent con:**
```
Đọc skills/bang-dieu-khien/SKILL.md rồi làm:
1. Cập nhật memory/local/qlcv/dashboard-notes.md — thay đổi cần ghi: <liệt kê cụ thể>
   (giữ nguyên các mục khác, đúng cú pháp mục ## BÁO CÁO)
2. Nếu đụng folder/ô cấp phát → cập nhật memory/local/qlcv/FOLDER-ALLOCATION.md cho khớp
3. Chạy: python skills/bang-dieu-khien/dashboard.py   (timeout ≥ 5 phút)
4. Verify: không còn {{PLACEHOLDER}}, đủ 8 ô, ô FOLDER không có dòng "QUÉT LỖI"
5. Báo lại: full path file:///D:/... + tóm tắt ĐÚNG những gì đã đổi (không tô hồng)
CHỈ ĐỌC qlcv/git — KHÔNG setxuly/assign/commit/pull/checkout.
```
Việc **quyết định nội dung** (đóng task nào, rủi ro gì) vẫn là của phiên chính — agent con chỉ **chép vào
và render**. Đừng giao cho nó tự phán trạng thái việc.

## Chạy
```bash
python skills/bang-dieu-khien/dashboard.py
```
- Ra: `memory/local/qlcv/dashboard/YYYY-MM-DD.html` + `index.html` (danh sách theo ngày, có bảng xu hướng).
- Chạy lại trong ngày = **ghi đè đúng file ngày đó**, không đẻ trùng.
- Mất ~1.5–2 phút (quét git 36 repo). Đừng đặt timeout ngắn hơn 5 phút.
- 🔴 **Đưa link phải là full `file:///D:/...`** — copy dán vào browser mở được ngay. Link tương đối chỉ mở trong VSCode.

```
file:///D:/PROJECT/Git/scrm_claude/AgentSystem/memory/local/qlcv/dashboard/index.html
```

## 8 ô
| Ô | Nguồn | Ghi chú |
|---|---|---|
| 🧾 **THUẾ / HĐĐT** | qlcv, tự lọc | Cụm đang căng. Chỉ hiện **việc của mình đang trong luồng dev**; người khác + hàng chờ BA gom xuống chân ô |
| 🔴 **ĐANG LÀM** | ghi tay | Việc đang cầm — kể cả **chưa có mã task** (đó mới là loại hay trôi) |
| ⚠️ **RỦI RO** | ghi tay | Thứ sẽ cắn nếu quên: code chưa commit, base cũ, đụng việc người khác |
| ⏳ **CHỜ QUYẾT** | ghi tay | Việc đang chờ user gật |
| 🐛 **BUG TỒN** / 📋 **TASK TỒN** | qlcv | Sắp theo trễ nhiều nhất trên đầu |
| 🗂️ **FOLDER** | sổ cấp phát **+ quét git** | Xem mục "Ô Folder" bên dưới |
| 📋 **BÁO CÁO NGÀY** | ghi tay | Bấm **Copy** → dán thẳng vào sheet team |

Danh tính lấy từ **token** (`sb_work.me()`) → ai chạy ra bảng người đó, KHÔNG hardcode tên.

## File nuôi tay: `memory/local/qlcv/dashboard-notes.md`
4 mục, cú pháp `## TÊN Ô` rồi mỗi dòng `- nội dung`. Hỗ trợ `**đậm**`, `` `code` ``, `~~gạch~~`, ` | ` = dấu chấm ngăn cột.

```
## ĐANG LÀM      ## RỦI RO      ## CHỜ QUYẾT      ## BÁO CÁO
```

🔴 **Claude phải cập nhật file này MỖI LẦN chốt gì** (đóng task, phát hiện rủi ro, user quyết một việc) rồi
chạy lại script. Ghi chú cũ mà không sửa thì bảng nói dối — nguy hơn không có bảng.

### Mục `## BÁO CÁO` — cú pháp
```
- <mô tả cột C> | <SB-Bucket> | <%> | <Trạng thái>
```
- Cột C: `<Mã> (<Khách>): <việc> (<ticket>) — <phạm vi>, <mốc>`
- Bucket: `SB-Logistics` (CÓ "s") · `SB-Ticket` · `SB-Invoice` · `SB-Warehouse` · `SB-Marketing`
- Trạng thái đúng enum: `Đã Xong` · `Đang Làm` (L HOA) · `Chưa làm`
- **CHỈ việc có MÃ TASK.** Tooling / dọn folder / dựng skill / quản lý → **KHÔNG** vào sheet.
- Script tự chèn cột Ngày làm (`d/m/yyyy`, không zero-pad) + Người làm (từ token) .
- Luật đầy đủ + ví dụ thật: **`memory/local/qlcv/report-format.md`** (đã sai 3 lần, đọc trước khi viết).

## Ô Folder — ghép 2 nguồn, cố ý
Sổ cấp phát `memory/local/qlcv/FOLDER-ALLOCATION.md` cho biết **ô nào giữ việc gì** (kể cả git sạch mà
vẫn đang giữ chỗ); quét git cho biết **thực tế đang bẩn cái gì**. Ô hiện:

- việc đang giữ (theo sổ) → `repo@nhánh` → `N sửa + M file mới` → 3 file vừa đụng → **chạm lần cuối**
- ⚠️ **bẩn trên nhánh base** (`product`/`release_product`) = chưa tách nhánh → không MR được, `git clean` là mất
- 🔴 **IM x NGÀY** = WIP bỏ quên (đã có ca im 34 và 50 ngày)
- **lệch sổ**: sổ ghi TRỐNG mà git bẩn → cảnh báo; git sạch mà sổ ghi giữ chỗ → vẫn BẬN
- Folder ngoài hệ Group (bảng thứ 2 trong sổ) cũng được quét

## 🔴 Ba cái bẫy đã dính thật — đừng lặp lại

**1. Nuốt lỗi git thành "sạch" (29/07).** `git()` trả `None` khi lỗi/timeout, **không** trả chuỗi rỗng.
Trước đó timeout 12s + nuốt lỗi → repo có 21 file dirty hiện ra là **trống** → suýt giao việc đè lên code
đang làm. Ô folder giờ in thẳng **"⚠ QUÉT LỖI — không kết luận được sạch hay bẩn"**.

**2. Clone single-branch báo nhầm "chưa push".** Vài repo có
`remote.origin.fetch = +refs/heads/product:refs/remotes/origin/product` → git chỉ biết 1 nhánh, mọi thứ
ngoài nhánh đó bị đếm là chưa push (có ca báo **163 commit**, kiểm lại thì có đủ trên server).
→ **Trước khi hô "commit chỉ có trên máy này": `git config --get remote.origin.fetch` + `git ls-remote --heads origin`.**

**3. Quét tuần tự quá chậm.** 36 repo → dùng `ThreadPoolExecutor(max_workers=6)`. Nhiều worker hơn thì
tranh ổ đĩa, git status bắt đầu timeout.

## Ràng buộc
- **Chỉ ĐỌC** qlcv + git. Script này **không ghi** gì lên hệ thống, không `pull`/`checkout`/`commit`.
- Toàn bộ output nằm trong `memory/local/` (gitignore) → **không lên git**.
- Engine auth/API dùng chung `skills/qlcv/sb_work.py` (cookie `protoken`). Skill này chỉ mượn, không sửa.
