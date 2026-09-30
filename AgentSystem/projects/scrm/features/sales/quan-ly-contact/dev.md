# Dev — Quản lý Contact

> Service: **Contact** (BE .NET) · FE module Sale. Chỉ dành cho dev.
> Trace registry: `registry/sales/ContactController.md`, `registry/sales/ContactPhoneBlacklistController.md` (xem thêm `ContactImportController.md` cho import đầy đủ).

## Màn hình & route (FE)
| Màn hình | Route (tham chiếu) | Ghi chú |
|---|---|---|
| Danh sách contact | `sale/danhsach-contact` | lưới + chi tiết, đặt lịch, chuyển tác nghiệp, trùng số |
| Danh sách contact chờ duyệt lịch | `sale/danhsach-contact-datlich` | tab leader duyệt lịch |
| Hồ sơ khách hàng (MKT/landing) | `landing-page/ho-so-khach-hang` | tạo/sửa/import contact MKT |
| Danh sách black list | `sale/danh-sach-black-list` | lưới + popup chi tiết blacklist |

## Endpoint / Controller (trace registry)
**ContactController** (Service `Contact`, ~50 endpoint, ~928 dòng) — CRUD contact + tác nghiệp sale-MKT. Nhóm hành động chính (xem bảng đầy đủ trong registry node):
| Nhóm | Endpoint chính | Quyền (key) |
|---|---|---|
| Danh sách / lọc | `TimTheoDieuKienAsync`, `TimTheoCuocHoiThoai`, `TimTheoNhan(+_TotalRecord)` | `ContactController_*` |
| Chi tiết | `ChiTietContact/{id}`, `ChiTietContactNoLog/{id}`, `ChiTietContactToView/{id}` | `ContactController_ChiTiet*` |
| Nhận data | `NhanData/{idContact}` | `[SBAuthorize]` (key+role bị comment) |
| Kết quả tác nghiệp / nhãn | `CapNhatKetQuaTacNghiep`, `CapNhatNhanKhachHang(+HangLoat)` | `ContactController_*` |
| Chuyển tác nghiệp | `CapNhatChuyenTacNghiep`, `ChuyenTacNghiepTiep(+V2)` | v1 & v2 **dùng chung** `ContactController_ChuyenTacNghiepTiep` |
| Phân bổ / thu hồi | `PhanBoLaiData(+Nhom)`, `PhanBoDataNguonLanding`, `ThuHoiSaleContact` | `ContactController_*` |
| Đặt lịch / duyệt lịch | `DatLichContact`, `GetListContactDatLichChoDuyet`, `DuyetContactDeTaoLich`, `HuyDuyetContactTaoLich` | `GetListContactDatLichChoDuyet_TimTheoDieuKienAsync`, `DuyetContactDeTaoLich_TimTheoDieuKienAsync`, `ContactController_HuyDuyetContactTaoLich` |
| Tạo / sửa / xóa contact | `InsertContactAsync`, `Marketing_InsertContact/_UpdateContact`, `CapNhatContact/{id}`, `DeleteContact/{id}`, `DeleteMultipleContact` (chỉ DV-AD-TONG), `DeleteMultipleContactSoTrung` | `ContactController_*` / `Contact_DeleteMultipleContact` |
| Import | `ImportContact` (IFormFile), `DownloadFileTemplateImportContact`, `DownloadDataWrongContact/{id}` | `[SBAuthorize()]` (chỉ login) |
| Trùng số / khách cũ / SP | `GetDanhSachTrungSo`, `GetTrungSoKhachCu`, `GetDSContactByPhone`, `GetDanhSachKhachCu`, `CheckContactTrung`, `GetDanhSachTrungSanPham` | nhiều cái forward `IHoSoKhachHangService` |
| Ghi chú / Zalo | `SaveMessage`, `SaveNote`, `GetListMessageByIdContact`, `SaveLinkZalo`, `GetLinkZalo/{id}` | `Contact_*` |
| Blacklist theo contact | `InsertOrUpdateBlackList`, `DeleteBlackList` | `Contact_*` |

Endpoint private / cross-service (PrivateSBAuthorize / AllowAnonymous) gom theo mục đích: OrderLogistic ⇄ Contact (đồng bộ đơn), Booking (sync trạng thái lịch), CSKH (đếm ngược), TMĐT (`TMDT_InsertContactAsync` tạo Contact + KhachHang — AllowAnonymous), Tổng đài/AutoCall, Partner sandbox. Xem registry node.

**ContactPhoneBlacklistController** (Service `Contact`) — quản lý blacklist số điện thoại:
| Nhóm | Endpoint | Quyền |
|---|---|---|
| Danh sách | `TimTheoDieuKien` | `ContactPhoneBlacklistController_TimTheoDieuKienAsync` (DV-AD-CN/MCN/TONG) |
| Chi tiết / CRUD | `GetDataById`, `InsertAsync`, `UpdateAsync`, `InsertOrUpdateAsync`, `DeleteDataById/{id}` | `[SBAuthorize()]` (chỉ login) |
| Gắn theo đơn / contact | `GetDataByIdOrderLogistic`, `GetDataByIdContact`, `DeleteData*` | `[SBAuthorize()]` |
| Tra cứu SĐT | `GetBlackListByPhone/{phone}`, `CheckPhoneBlackList/{phone}`, `GetListPhoneBlackListByPhone` | `[SBAuthorize()]` |
| Private (cho OrderLogistic) | `IsBlackListByPhonePrivate`, `InsertOrUpdatePrivateAsync` | `[PrivateSBAuthorize]` |

## Entities
- **`Contact`** — entity gốc của lead/contact (Service Contact).
- **`ContactMessage` / `ContactNote`** — tin nhắn / ghi chú nội bộ theo contact.
- **`ContactPhoneBlacklist`** — bản ghi blacklist (gắn theo phone, có thể liên kết `idContact` / `idOrderLogistic`).
- Phần lớn list / trùng-số đi qua `IContactStoreService` (Dapper / stored proc): `TimTheoDieuKien`, `TimTheoNhan`, `TimTheoCuocHoiThoai`, `GetTrungSoKhachCu`, `GetDSContactByPhone`, `LayTacNghiepCan`.

## Luồng code
- **Nhận data:** `NhanData/{idContact}` bỏ ẩn thông tin contact + bắt đầu đếm thời gian tác nghiệp (CH1 chia xoay vòng từ KhoSo).
- **Đặt lịch + duyệt:** `DatLichContact` → state **ChoDuyet** nếu cấu hình yêu cầu duyệt → leader `GetListContactDatLichChoDuyet` → `DuyetContactDeTaoLich(true)` tạo lịch thật / `HuyDuyetContactTaoLich` quay lại ChoDuyet. Đồng bộ Booking qua `SyncContactLichHenToLichHen`.
- **Trùng số / khách cũ:** phần lớn delegate sang `IHoSoKhachHangService` (logic chéo service).
- **Blacklist check khi tạo đơn:** OrderLogistic gọi `IsBlackListByPhonePrivate` (PrivateSBAuthorize) → trả cờ blacklist.

## Field ngày sinh + địa chỉ import, phân bổ, tác nghiệp (delta 07/2026)
> Nhánh `nganlq/2026/task/contact-ngaysinh-diachi` + hotfix haihnPS. Code hiện tại đã verify (path:line theo `scrm_build` @ `4c4f92260a`).

- **Field `NgaySinh` trên contact (`Contact.KhachHangNgaySinh`):**
  - DTO thêm `NgaySinh` vào `MktInsertContactReqDto` / `MktUpdateContactReqDto` (`ReqEditContactDto.cs`) và `ReqEditContactDto`.
  - `ContactService.Marketing_InsertContact`/`_UpdateContact` set `entity.KhachHangNgaySinh = reqDto.NgaySinh` (`ContactService.cs:3174`, `:6166`, `:12027`).
  - Khi contact tạo/đổi ngày sinh → **propagate sang KhachHang**: `UpdateCustomerFromContactHelperService.EventUpdateCustomer` publish `EventUpdateCustomerFromContactMessage` với `NgaySinh = contact.KhachHangNgaySinh` (`Common/UpdateCustomerFromContactHelperService.cs:44`). Field mới `NgaySinh` thêm vào message DTO `EventUpdateCustomerFromContactMessage.cs`.
  - Luồng manual: khi tạo KH private từ contact, payload `createKhachHangPrivateDto` set `NgaySinh = reqDto.NgaySinh` (`ContactService.cs:11423`).
  - Xử lý phía Customer: xem `khach-hang/quan-ly-thong-tin-kh/dev.md` (set khi tạo mới + **backfill không override** nếu KH đã có ngày sinh).

- **Import contact — cột ngày sinh + địa chỉ (`ContactImportService.cs`):**
  - `ImportContactDto` thêm `NgaySinh` (đặt ngay sau "SĐT Khách hàng", trước Email) + 4 cột địa chỉ append CUỐI sau "Quốc gia": `Tinh`, `Huyen`, `Xa`, `DiaChi` (tất cả optional, đọc theo header).
  - `ContactImport` entity thêm `KhachHangNgaySinh` (đặt tên trùng `Contact.KhachHangNgaySinh` để SP map thẳng khi đẩy import→Contact).
  - **Ô ví dụ template dịch cột (`genShiftNgaySinh`)** (`ContactImportService.cs:1095-1266`): quét 6 dòng header đầu, nếu thấy chuỗi "ngày sinh" → `genShiftNgaySinh=1` → các ô ví dụ (sale/tác nghiệp/SP/DV/combo/nhãn...) index +1. Đây là cột shift MỚI, chồng thêm lên cơ chế `shiftEmail`/`shift` cũ.
  - **Resolve tên→mã tỉnh/huyện/xã theo phân cấp** (`:1283` `ChuanHoaTenDiaChi` normalize bỏ tiền tố "tỉnh/tp/quận/huyện/phường/xã"; precompute `dicTinh`/`dicHuyen`/`dicXa` key `"{maCha}|{tenChuan}"`, lookup O(1) trong phạm vi cha `:1762-1774`, `:2043+`). `DiaChi` (số nhà/đường) lưu text thẳng, luôn giữ.
  - **Địa chỉ sai KHÔNG loại dòng** (Option 2): cấp sai → bỏ mã cấp đó + `rowCoLoiDiaChi=true` → tăng `countDiaChiWarning` (`:2072`), cảnh báo gộp; contact vẫn được import. KHÁC với cột bắt buộc (nguồn/tên/SĐT) — sai thì loại dòng.
  - **Validate ngày sinh import** (`:2105`): `KhachHangNgaySinh` không được > `DateTime.Now.Date` → thêm message lỗi `"Ngày sinh không được vượt quá hiện tại"` (nhất quán luồng nhập tay); sai định dạng → `"Ngày sinh nhật không hợp lệ"` (`:2114`).

- **Thời gian tác nghiệp tiếp min 1 phút** (`ContactService.cs:8194-8195`): khi `soPhut <= 0` (tác nghiệp tiếp ngay) → ép `soPhut = 1`, `reqData.NgayTacNghiepTiep = DateTime.Now.AddSeconds(60)` (trước đây set `= DateTime.Now`). Mục đích **chặn update mốc tác nghiệp về quá khứ/ngay lập tức** — đẩy tối thiểu +1 phút.

- **Phân bổ data đã chốt đơn — bỏ chặn đơn offline** (`ContactService.cs:3908`, `:4340`, haihnPS): 2 khối `if (itemContact.IdDonHang.HasValue) { errorDonHangOffline++; continue; }` (bỏ qua contact có đơn offline đã chốt) đã bị **comment lại** → cho phép phân bổ contact đã chốt đơn (không còn check "đã tạo đơn hàng offline"). Điều kiện chốt đơn còn lại kiểm qua `isChotDon` + cờ `isAcceptPBDHDaChot` (`:3914`), không còn chặn cứng theo `IdDonHang`.

## Bug-prone / lưu ý (path:line)
> Chỉ ghi nhận — KHÔNG sửa code ở session này. Để session security xử lý.
- **`ContactController.cs:880-924` (`FixBugContact`)** — return rỗng ngay dòng đầu, toàn bộ phần sau là dead code; debug-only, không nên expose.
- **`ContactController.cs:154-155` (`NhanData`)** — mã quyền + role bị comment, chỉ còn `[SBAuthorize]` → quyền nới lỏng (ai đăng nhập đều nhận data được).
- **`ContactController.cs:670` & `:683` (`ChuyenTacNghiepTiep` / `V2`)** — dùng chung mã quyền → không audit riêng v1/v2.
- **`ContactController.cs:218-224` (`CapNhatContact/{id}`)** — tham số `id` trên route không dùng, chỉ lấy từ `reqDto`.
- **`ContactController.cs:196-198` (`CreateContactCSKH`)** — chồng `[SBAuthorize(key)]` + `[PrivateSBAuthorize]` (2 attribute auth khác nhau cùng method).
- **`ContactController.cs:807-808` (`GetDanhSachTrungSanPham`)** & rải rác `:475-476` — nhiều endpoint trùng-số bị comment mã/role, hạ về `[SBAuthorize()]` chỉ đăng nhập.
- **`ContactController.cs:330-336` (`InsertContactWebsitePrivateAsync`), `:750`, `:789` (`TMDT_*`)** — `[AllowAnonymous]` tạo/xóa contact → cần validate input chặt vì không auth.
- **`ContactController.cs:322` (`InsertContactAsync`)** — description mã quyền là "Duyệt contact tạo lịch" (copy nhầm, không khớp action).
- **Mã quyền duyệt lịch không theo prefix Controller** (`GetListContactDatLichChoDuyet_*`, `DuyetContactDeTaoLich_*`) — đặt theo action, khác convention.
- **`ContactPhoneBlacklistController.cs:41-134`** — gần như toàn bộ CRUD + tra cứu blacklist chỉ `[SBAuthorize()]` (không mã quyền/role); chỉ `TimTheoDieuKien` (`:33`) có mã + role → blacklist là dữ liệu nhạy cảm nhưng phân quyền lỏng.
- **`ContactPhoneBlacklistController.cs:136-149`** — `IsBlackListByPhonePrivate` / `InsertOrUpdatePrivateAsync` dùng cho OrderLogistic; đổi DTO phải sync 2 service.

## File source
- BE controller: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Contact\...\Controllers\ContactController.cs`, `ContactPhoneBlacklistController.cs`.
- Registry nodes: `projects/scrm/registry/sales/ContactController.md`, `projects/scrm/registry/sales/ContactPhoneBlacklistController.md`, `projects/scrm/registry/sales/ContactImportController.md`.
