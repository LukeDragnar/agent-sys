# Dev — Cấu hình Sale (contact / đơn / giảm giá / ship / kho số)

> Feature bó nhiều controller cấu hình. Chỉ dành cho dev. Trace registry:
> - `CauHinh_Contact` → `projects/scrm/registry/sales/CauHinhController.md` (service **Contact**).
> - `CauHinh_Order` → `projects/scrm/registry/kho/CauHinhController_OrderLogistic.md` (service **OrderLogistic**, có `CauHinhDonHangSale`). *(Lưu ý còn bản service Order `cua-hang/CauHinhController.Order.md` — cùng tên class, khác service.)*
> - `DonHangCauHinhGiamGia` → `projects/scrm/registry/marketing/DonHangCauHinhGiamGiaController.md` (service **Promotion**).
> - `CauHinhSuaSo` *(bổ sung Pha 2)* → `projects/scrm/registry/sales/CauHinhSuaSoController.md` (service **Contact**) — ma trận quyền sửa field contact. *(KHÔNG nhầm với `CauHinhDauSoController` — node đó thuộc tổng đài.)*

## Màn hình & route (FE)
| Màn hình | Route (tham chiếu) | Service |
|---|---|---|
| Cấu hình Contact (init/kho số/chia contact/nguồn/CSKH) | Cài đặt > Cấu hình Contact | Contact |
| Cấu hình Order / đơn hàng sale | `config-order` (ConfigOrderComponent) + màn cấu hình OrderLogistic | Order / OrderLogistic |
| Cấu hình SuperAdmin (đơn vị) | `quantri/cauhinh-donvi-super-admin` | Order / Contact |
| Cấu hình giảm giá theo đơn | Marketing > Cấu hình giảm giá theo đơn hàng | Promotion |
| Cấu hình ma trận quyền sửa field contact *(bổ sung Pha 2 — CauHinhSuaSo)* | nội bộ — không UI riêng, gọi từ màn cấu hình contact (TienIch `TI_Marketing`) | Contact |

## Endpoint / Controller (trace registry)
**Contact — `CauHinhController`** (`CauHinh_Contact_*`):
- `SaveCauHinhInit` (`CauHinh_Contact_LuuCauHinhContact`, DV-AD-TONG) · `GetCauHinhInit`/`Get` (`[SBAuthorize]`).
- `LuuCauHinhKhoSo` (`CauHinh_Contact_LuuCauHinhKhoSo`, DV-AD-TONG) · `GetCauHinhKhoSo`/`GetKhoSoCfgApply`.
- `SaveCauHinhChiaContactXoayVong` / `GetCauHinhChiaContactXoayVongDonVi`.
- `SaveQuanLyNguonDuLieu` (`..._SaveQuanLyNguonDuLieu`, DV-AD-TONG) / `GetCauHinhQuanLyNguonDuLieu`.
- `SaveCauHinhMKT` / `GetCauHinhMKT` (`[SBAuthorize]` — key role đã comment).
- `SaveCauHinhSinhBanGhiCSKH` / `CauHinhSinhBanGhiCSKH_GetDetail` / `LichSuThayDoiCauHinh` (`[SBAuthorize]` — key comment).
- `DsCauHinhGiaoHangChuyenCskh` / `SaveCauHinhGiaoHangChuyenCskh` (DV-AD-TONG).
- `Save_SuperAdmin` / `GetCauHinhSuperAdmin` (HT-AD, HT-CSKH).
- `SaveKhoSoCauHinhChiNhanh` / `GetKhoSoCauHinhChiNhanh` (`[SBAuthorize]`) — **side-effect re-sync cấu hình kho số đơn vị**.
- Private: `GetCHPrivate`, `CauHinhCheSoPrivate`, `GetCauHinhChiaContactXoayVong/{idDonVi}`, `GetAllCauHinhSuperAdmin`, `GetCauHinhContact/{idDonVi}`, `DongBoCauHinhContactTuDonHang`.

**OrderLogistic — `CauHinhController`** (`CauHinh_OrderLogistic_*`, TienIch `CauHinh_OrderLogistic_Controller`):
- `Get` (`[SBAuthorize]`) · `GetCauHinhDonHangSale` (`[SBAuthorize]`) · `SaveCauHinhDonHangSale` (`CauHinh_OrderLogistic_SaveCauHinhDonHangSale`, DV-AD-TONG).
- `GetCauHinhInit` / `SaveCauHinhInit` (DV-AD-TONG) · `GetNghiepVuCauHinh/{nghiepVu}` / `SaveNghiepVuCauHinh` (`[SBAuthorize]` — key tạm comment).
- `Save_SuperAdmin` / `GetCauHinhSuperAdmin` (HT-AD/HT-CSKH) · `CheckQuyenXuatExcel` · `CapNhatInvoice`.
- Private: `GetCauHinhOrder/{idDonVi}`, `GetAllCauHinhSuperAdmin`, `GetAllAsyncPrivate/{idDonVi}`, `DongBoCauHinhDonHangTuContact`.

**Promotion — `DonHangCauHinhGiamGiaController`** (`DonHangCauHinhGiamGia_*`, TienIch `TI_KhuyenMai`):
- `TimTheoDieuKien` (list, AD-TONG/CN/MCN, QLDV) · `ThemMoi` · `CapNhatThongTin` · `TimTheoId/{id}` (đọc Redis) · `Xoa/{id}` · `GetConfigOrderByPrice/{price}` (thanh toán) · `LayDsCauHinhGiamGia` (`[SBAuthorize()]` — login).

**Contact — `CauHinhSuaSoController`** *(bổ sung Pha 2 — CauHinhSuaSo)* (controller-level `[SBAuthorize]`, TienIch `CauHinhSuaSoController` thuộc `TI_Marketing`):
- `GET GetCauHinhInit` (`CauHinhSuaSoController_GetCauHinhInit`, **DV-AD-TONG,HT-AD,HT-CSKH**) — lấy ma trận theo đơn vị (cache-aside).
- `POST SaveCauHinhInit(CauHinhSuaSoMatrixReq)` (`CauHinhSuaSoController_SaveCauHinhInit`, **DV-AD-TONG,HT-AD,HT-CSKH**) — upsert ma trận theo đơn vị + clear cache.
- *(Chỉ 2 endpoint; controller 43 dòng. Lưu ý: registry frontmatter ghi `auth: public` nhưng CODE THỰC có key role DV-AD-TONG/HT-AD/HT-CSKH trên cả 2 endpoint — bám theo code.)*

## Entities
- Contact: `CauHinhContact`, `KhoSoCauHinhChiNhanh`, `CauHinhSinhBanGhiCSKH`.
- OrderLogistic: `CauHinh`, `CauHinhSetting` (cờ `IsSuDungKhoTong`, `IsSuDungChiaSoCareDon`, `IsSuDungChiaSoCareDonKhoTong`, `IdChiNhanhTong`…), `CauHinhDonHangSale`, `CauHinhNghiepVu`.
- Promotion: `DonHangCauHinhGiamGia` (scope IdDonVi+IdChiNhanh; `GiaTriTu`, `GiaTriDen`, `GiamGiaKieuLoai`, `GiamGiaGiaTri`, `SuDung`).
- Contact *(bổ sung Pha 2)*: `CauHinhSuaSo` (`IdDonVi`, `UnitCodeId`, `EnumUnitCode`, `Roles`) — mỗi bản ghi = 1 field/cột contact gán danh sách role. `UnitCodeId` map enum `EnumCauHinhSuaSoContact`. DTO: `CauHinhSuaSoMatrixDto` (UnitCodeId, Roles), req `CauHinhSuaSoMatrixReq.CauHinhSuaSoMatrixDtos`.

## Luồng code
- **SaveKhoSoCauHinhChiNhanh** (Contact): lưu chi nhánh rồi gọi `SaveDonViSuDungKhoSo` lấy tất cả chi nhánh active → re-sync cấu hình kho số cấp đơn vị (1 lần lưu chạm 2 phạm vi).
- **Đồng bộ cấu hình đơn hàng**: Contact ↔ OrderLogistic qua `DongBoCauHinhContactTuDonHang` / `DongBoCauHinhDonHangTuContact`; Invoice → OrderLogistic qua `CapNhatInvoice`.
- **Giảm giá** (`DonHangCauHinhGiamGiaService.cs:167` Validate): đủ trường → kiểu loại hợp lệ → `GiaTriTu < GiaTriDen` → PhanTram ≤ 100 → Tien < GiaTriTu; rồi check overlap khoảng giá. CUD → log (LogActivityHelper ADD/UPDATE/DELETE) + ClearCache Redis theo chi nhánh. `GetConfigOrderByPrice`: chọn `SuDung==true` chứa price, order `GiaTriDen` desc.
- **Ma trận sửa số** *(bổ sung Pha 2 — CauHinhSuaSo)* (`CauHinhSuaSoService.cs`):
  - `GetCauHinhInit` (`:46`): đọc cache `GetCache_CauHinhSuaSoSetting_DonVi(DonViId)` trước; miss → `TableWithNolock()` lọc `IdDonVi`, select (UnitCodeId, Roles), order UnitCodeId; rỗng → `ResultWithError("Lỗi lấy dữ liệu")` (`:64`); có data → `SetCache_...` rồi trả.
  - `SaveCauHinhInit` (`:70`): query `settingsSuaSo` theo `IdDonVi` (nolock). Có bản ghi → loop update `Roles` theo UnitCodeId khớp request + `MarkDirty(Roles)` + `UpdateAsync` (`:77-86`). Chưa có → insert mới full ma trận, `EnumUnitCode = ((EnumCauHinhSuaSoContact)UnitCodeId).ToString()` (`:90-103`). Cuối: `ClearCache_CauHinhSuaSoSetting_DonVi` (`:105`), trả `Guid.Empty`. Catch nuốt ex, trả "Lỗi lưu cấu hình" (`:108-110`).

## Bug-prone / lưu ý (KHÔNG sửa ở đây — chỉ ghi; có lỗ hổng auth → để session security)
- **Contact: rất nhiều endpoint `[SBAuthorize]` rỗng** (`CauHinhController.cs:58,67,108,130,213,220,228,252,260,267,274,286,293`): nhiều cái có key role gốc đã bị comment (`SaveCauHinhMKT`, `SaveCauHinhSinhBanGhiCSKH`, `GetCauHinhSinhBanGhiCSKH`) → khe hở phân quyền nếu lộ UI ngoài quản trị.
- **Contact: `SaveKhoSoCauHinhChiNhanh` quyền lỏng + side-effect đơn vị** (`CauHinhController.cs:274-284`): chỉ `[SBAuthorize]` rỗng nhưng re-sync cấu hình kho số cấp **đơn vị** → user login bất kỳ có thể đổi cấu hình đơn vị nếu UI cho phép.
- **OrderLogistic: key permission bị comment "TODO comment for debug"** (`CauHinhController.cs:27,127,136`): `Save`/`GetNghiepVuCauHinh`/`SaveNghiepVuCauHinh` tạm chỉ `[SBAuthorize]` mở → thiếu phân quyền nếu lên prod.
- **OrderLogistic: endpoint `Save` (cấu hình order chung) đã comment toàn bộ** (`CauHinhController.cs:27-33`): không còn endpoint lưu cấu hình order cơ bản, chỉ còn SuperAdmin/sale/init/nghiệp vụ.
- **Trùng tên class `CauHinhController`** giữa Contact / OrderLogistic / Order (PreOrder) → khi trace cần xác định đúng service; registry tách node bằng suffix.
- **Giảm giá: biểu thức overlap thừa/sai** (`DonHangCauHinhGiamGiaService.cs:65-68,104-107`): nhánh `(data.GiaTriDen >= x.GiaTriDen && data.GiaTriDen <= x.GiaTriDen)` so chính nó → overlap không phủ hết trường hợp lồng nhau → có thể tạo khoảng chồng lấn.
- **Giảm giá: `GetById` đọc Redis không EF** (`...Service.cs:157-165`): cache chưa set/clear đúng → chi tiết lệch DB.
- **Giảm giá: `if (price == null)` so double với null** (`...Service.cs:193`): luôn false (dead check).
- **Giảm giá: `LayDsCauHinhGiamGia` quyền lỏng** (`DonHangCauHinhGiamGiaController.cs:76`): chỉ `[SBAuthorize()]` login trong khi CUD cần role AD/QLDV → rò cấu hình.
- **Giảm giá: Delete NRE** (`...Service.cs:144`): clear cache theo `data.IdChiNhanh.Value` → NRE nếu IdChiNhanh null.
- **Ma trận sửa số: đơn vị chưa cấu hình → trả lỗi** *(bổ sung Pha 2)* (`CauHinhSuaSoService.cs:64`): list rỗng → `ResultWithError("Lỗi lấy dữ liệu")` thay vì list rỗng → đơn vị mới chưa cấu hình nhận lỗi, FE cần phân biệt "rỗng" vs "lỗi thật".
- **Ma trận sửa số: field (UnitCode) mới không được insert cho đơn vị đã có cấu hình** *(bổ sung Pha 2)* (`CauHinhSuaSoService.cs:74-86`): nhánh update chỉ update các UnitCode đã tồn tại trong DB; UnitCode mới gửi từ request bị bỏ qua → ma trận thiếu cột nếu enum `EnumCauHinhSuaSoContact` mở rộng sau khi đơn vị đã lưu lần đầu.
- **Ma trận sửa số: catch nuốt exception** *(bổ sung Pha 2)* (`CauHinhSuaSoService.cs:108-110`): trả message tĩnh "Lỗi lưu cấu hình", không log `ex` → khó debug khi lưu fail.
- **Ma trận sửa số: registry frontmatter `auth: public` lệch code** *(bổ sung Pha 2)*: code controller (`CauHinhSuaSoController.cs:25,33`) có key role DV-AD-TONG/HT-AD/HT-CSKH trên cả 2 endpoint → node registry nên cập nhật lại auth (không phải public). Bám theo code.

## File source (path:line)
- Contact: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Contact\...\Controllers\CauHinhController.cs:58/67/108/130/213/220/228/252/260/267/274-284/286/293`.
- OrderLogistic: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\OrderLogistic\...\Controllers\CauHinhController.cs:27-33/127/136`.
- Promotion: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Promotion\...\Controllers\DonHangCauHinhGiamGiaController.cs:76`; service `DonHangCauHinhGiamGiaService.cs:65-68/104-107/144/157-165/167/193`.
- Entities: Contact (`CauHinhContact`, `KhoSoCauHinhChiNhanh`, `CauHinhSinhBanGhiCSKH`), OrderLogistic (`CauHinh`/`CauHinhSetting`/`CauHinhDonHangSale`/`CauHinhNghiepVu`), Promotion (`DonHangCauHinhGiamGia`).
- Ma trận sửa số *(bổ sung Pha 2 — CauHinhSuaSo)*: controller `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Contact\Contact.API\Controllers\CauHinhSuaSoController.cs:25/33`; service `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Contact\Contact.Service\Generated\CauHinhSuaSoService.cs:46/64/70/74-86/90-103/105/108-110`; entity `CauHinhSuaSo` (IdDonVi/UnitCodeId/EnumUnitCode/Roles), enum `EnumCauHinhSuaSoContact`.
