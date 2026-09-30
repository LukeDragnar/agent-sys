# Dev — Telesales

> Telesales = **usage layer trên `ContactController`** (service **Contact**, .NET) + tổng đài. KHÔNG có controller "Telesales" riêng. Xem registry node `registry/sales/ContactController.md` là gốc. Chỉ dành cho dev.

## Màn hình & route (FE)
| Màn hình | Route (FE) | Ghi chú |
|---|---|---|
| Danh sách contact (làm việc chính của tele) | `sale/danhsach-contact` | lưới + popup chi tiết, nhận data, gọi, ghi kết quả, nhãn, trùng số |
| Danh sách contact chờ duyệt lịch (leader) | `sale/danhsach-contact-datlich` | duyệt/hủy duyệt lịch hẹn do tele đặt |
| (Tổng đài) lịch sử cuộc gọi / click-to-call | xem feature Tổng đài | tùy phiên bản |

> Lưu ý: FE module sale ở `scrm_frontend/src/app/pages/sale/`. Route chính xác nên xác nhận trong `sale-routing.module.ts`.

## Endpoint / Controller (trace registry)
Gốc: **`ContactController`** (service Contact, ~50 endpoint). Các endpoint telesales dùng nhiều:
| Hành động tele | Endpoint | Quyền (key) |
|---|---|---|
| Danh sách contact | `POST TimTheoDieuKienAsync` | `ContactController_TimTheoDieuKienAsync` |
| Nhận data (chia xoay vòng) | `POST NhanData/{idContact}` | `[SBAuthorize]` (mã/role bị comment — nới lỏng) |
| Cập nhật kết quả tác nghiệp | `POST CapNhatKetQuaTacNghiep` | `ContactController_CapNhatKetQuaTacNghiep` |
| Gắn nhãn (đơn/bulk) | `POST CapNhatNhanKhachHang` / `...HangLoat` | `ContactController_CapNhatNhanKhachHang(...)` |
| Chuyển tác nghiệp / tiếp (v1/v2) | `POST CapNhatChuyenTacNghiep` / `ChuyenTacNghiepTiep(V2)` | `..._CapNhatChuyenTacNghiep` / `..._ChuyenTacNghiepTiep` (v1+v2 dùng chung key) |
| Đặt lịch | `POST DatLichContact` | `ContactController_DatLichContact` |
| Duyệt / hủy duyệt lịch (leader) | `POST GetListContactDatLichChoDuyet` / `DuyetContactDeTaoLich` / `HuyDuyetContactTaoLich` | key đặt theo action (không theo prefix Controller) |
| Phân bổ / thu hồi data | `POST PhanBoLaiData(...)` / `ThuHoiSaleContact` | `..._PhanBoLaiData(...)` / `_ThuHoiSaleContact` |
| Tạo đơn khách cũ (chốt) | `POST TaoDonHangKhachCu` | `ContactController_TaoDonHangKhachCu` |
| Ghi chú/tin nhắn | `POST SaveNote` / `SaveMessage` | `Contact_SaveNote` / `_SaveMessage` |
| Tạo lịch sử cuộc gọi (qua tổng đài) | `POST TongDaiLichSuCuocGoi/TaoLichSuCuocGoi` | `[SBAuthorize]` (key gốc bị comment — nới lỏng) |

State machine lịch hẹn: `DatLich_ChoDuyet → DatLich_DaDuyet` (leader duyệt) / `→ DatLich_HuyDuyet`.

## Entities
- `Contact` (gốc — lead/contact), `ContactMessage`/`ContactNote` (ghi chú), `ContactPhoneBlacklist`.
- Lịch sử cuộc gọi: entity tổng đài (`TongDaiLichSuCuocGoi` / `CallHistory`) — xem feature Tổng đài.

## Luồng code
1. Tele mở danh sách → `TimTheoDieuKienAsync` (StoreService/SP) lọc theo scope đơn vị + data được chia.
2. `NhanData` → bỏ ẩn thông tin contact + bắt đầu đếm thời gian tác nghiệp (chia xoay vòng, liên quan KhoSo/ChiaSo).
3. Gọi → click-to-call qua tổng đài (`MyHub_ClickToCall` hoặc tổng đài IP) → `TaoLichSuCuocGoi` ghi bản ghi.
4. `CapNhatKetQuaTacNghiep` / `CapNhatNhanKhachHang` cập nhật contact (EF).
5. `DatLichContact` → tạo lịch Booking; leader `DuyetContactDeTaoLich` đồng bộ `SyncContactLichHenToLichHen`.
6. Chốt → `TaoDonHangKhachCu` tạo contact/đơn mới → ripple OrderLogistic + gán sale KhachHang.

## Bug-prone / lưu ý (path:line — KHÔNG sửa code ở session này)
- `ContactController.cs:154-155` (`NhanData`) — **mã quyền + role bị comment**, chỉ còn `[SBAuthorize]` → bất kỳ user login nào nhận data được (nới lỏng quyền).
- `ContactController.cs:670,683` (`ChuyenTacNghiepTiep` / `V2`) — **dùng chung 1 mã quyền** → không audit riêng v1/v2.
- `ContactController.cs:218-224` (`CapNhatContact/{id}`) — `id` trên route **không dùng**, chỉ lấy từ body → dễ nhầm khi gọi.
- `ContactController.cs:322` (`InsertContactAsync`) — description mã quyền copy nhầm "Duyệt contact tạo lịch".
- `TongDaiLichSuCuocGoiController.cs:25-26` — `TaoLichSuCuocGoi` key gốc bị comment → chỉ `[SBAuthorize]` rỗng (ai login cũng tạo lịch sử cuộc gọi).
- `ContactController.cs:880-924` (`FixBugContact`) — **return rỗng dòng đầu**, phần sau dead code (debug-only).
- Endpoint `[AllowAnonymous]` tạo contact (`TMDT_*`, `InsertContactWebsitePrivateAsync`) — không auth, validate input chặt (không thuộc luồng tele nhưng cùng controller).

## File source (path:line)
- BE: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Contact\Contact.API\Controllers\ContactController.cs` (~928 dòng; xem các dòng nêu trên).
- BE tổng đài lịch sử: `...\scrm\` service Switchboard, `TongDaiLichSuCuocGoiController.cs:25` (trace feature Tổng đài).
- Registry gốc: `projects/scrm/registry/sales/ContactController.md` (bảng đầy đủ ~50 endpoint + nhóm private/cross-service).
- FE: `D:\PROJECT\Git\_SourceForCode\Group2\scrm_frontend\src\app\pages\sale\` (danhsach-contact, danhsach-contact-datlich).
