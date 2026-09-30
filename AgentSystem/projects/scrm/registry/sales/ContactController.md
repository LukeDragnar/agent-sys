---
controller: ContactController
module: sales
service: Contact
man_hinh_ui: "Sale > Danh sách contact (sale/danhsach-contact — lưới + chi tiết, đặt lịch, chuyển tác nghiệp, trùng số) · Sale > Danh sách contact chờ duyệt lịch (danhsach-contact-datlich) · Marketing/Landing > Hồ sơ khách hàng (landing-page/ho-so-khach-hang — tạo/sửa/import contact MKT) · ghi chú & blacklist trong popup chi tiết contact"
muc_dich: "CRUD contact/lead + tác nghiệp sale-MKT (nhận data, cập nhật kết quả/nhãn/chuyển tác nghiệp, đặt lịch & duyệt lịch), phân bổ data sale/MKT, trùng số/khách cũ, ghi chú nội bộ, blacklist; nhiều endpoint private đồng bộ cross-service (OrderLogistic, Booking/lịch hẹn, TMDT, AutoCall, partner sandbox)"
alias: [Contact, Lead, "Danh sách contact", "data sale", "data MKT"]
auth: mixed
state_machine: [DatLich_ChoDuyet, DatLich_DaDuyet, DatLich_HuyDuyet]
deprecated: false
entities: [Contact, ContactMessage, ContactNote, ContactPhoneBlacklist]
stored_procs: ["Đa số list/trùng-số đi qua IContactStoreService (Dapper/SP) — TimTheoDieuKien, TimTheoNhan, TimTheoCuocHoiThoai, GetTrungSoKhachCu, GetDSContactByPhone, LayTacNghiepCan"]
depends: [IContactService, IContactStoreService, IDataReceiveService, IKhoSoService, IHoSoKhachHangService]
cross_module_in: [OrderLogistic, Booking/LichHen, TMDT/ecommerce, AutoCall, Hotline/OmiCall, AuthenPartner/Sandbox]
cross_module_out: [OrderLogistic, Booking/LichHen, KhoSo/ChiaSo, KhachHang, CSKH]
---

# ContactController — Quản lý contact / lead (tác nghiệp sale-MKT)

> Controller lớn (~50 endpoint, 928 dòng). Bảng **Hành động** chỉ liệt kê endpoint **có UI** (màn hình sale dùng trực tiếp).
> Endpoint private/cross-service (OrderLogistic, lịch hẹn, TMDT, AutoCall...) gom ở mục **Nhóm endpoint phụ**.

## Hành động (endpoint chính — có UI)

| # | Hành động | Nhãn UI/nút | Endpoint | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Danh sách contact (lọc) | Lưới "Danh sách contact" | `POST TimTheoDieuKienAsync` | ContactController_TimTheoDieuKienAsync | public | — | Trả list contact theo bộ lọc + scope đơn vị | StoreService (SP) | — |
| 2 | Tìm theo cuộc hội thoại / nhãn | Tab lọc theo nhãn/hội thoại | `POST TimTheoCuocHoiThoai` · `POST TimTheoNhan` · `POST TimTheoNhan_TotalRecord` | ContactController_TimTheoCuocHoiThoai / _TimTheoNhan / _TimTheoNhan_TotalRecord | public | — | List + total cho paging theo nhãn | StoreService (SP) | — |
| 3 | Chi tiết contact | Mở popup chi tiết | `GET ChiTietContact/{id}` · `GET ChiTietContactNoLog/{id}` · `GET ChiTietContactToView/{id}` | ContactController_ChiTietContact / _ChiTietContactNoLog / _ChiTietContactToView | public | — | `ChiTietContact(id, ghiLog)`; bản NoLog không ghi lịch sử xem; ToView read-only | EF | 3 biến thể gần trùng (log/noLog/view) — dễ nhầm khi gọi |
| 4 | Cập nhật kết quả tác nghiệp | Form "Kết quả tác nghiệp" | `POST CapNhatKetQuaTacNghiep` | ContactController_CapNhatKetQuaTacNghiep | public | — | Ghi kết quả tác nghiệp cho contact | EF | — |
| 5 | Nhận data (chia xoay vòng) | Nút "Nhận data" | `POST NhanData/{idContact}` | `[SBAuthorize]` (mã/role bị comment) | public | sale chưa nhận contact | Bỏ ẩn thông tin contact, bắt đầu tác nghiệp | EF | Quyền nới lỏng — chỉ yêu cầu đăng nhập (dòng key+role bị comment, `:154`) |
| 6 | Cập nhật nhãn KH (đơn / hàng loạt) | Gắn nhãn / bulk gắn nhãn | `POST CapNhatNhanKhachHang` · `POST CapNhatNhanKhachHangHangLoat` | ContactController_CapNhatNhanKhachHang / _CapNhatNhanKhachHangHangLoat | public | — | Set nhãn cho 1 hoặc nhiều contact | EF | Danh sách role rất rộng (kho/kế toán/sale) — kiểm soát kỹ |
| 7 | Chuyển tác nghiệp | Nút "Chuyển tác nghiệp" | `POST CapNhatChuyenTacNghiep` | ContactController_CapNhatChuyenTacNghiep | public | — | `IdDonVi` gán từ CurrentUser → `CapNhatChuyenTacNghiep(reqDto, false)` | EF | IdDonVi override từ controller, bỏ giá trị client |
| 8 | Chuyển tác nghiệp tiếp (v1/v2) | Nút "Tác nghiệp tiếp" | `POST ChuyenTacNghiepTiep` · `POST ChuyenTacNghiepTiepV2` | ContactController_ChuyenTacNghiepTiep (DÙNG CHUNG cho cả v2) | public | — | v2 là bản logic mới; **(07/2026) khi số phút chờ ≤ 0 → ép min 1 phút** (`NgayTacNghiepTiep = DateTime.Now.AddSeconds(60)`, `ContactService.cs:8194`) chặn set mốc về quá khứ | EF | v1 và v2 **dùng chung mã quyền** → không audit riêng (`:670`,`:683`) |
| 9 | Cập nhật thông tin contact | Nút "Lưu" (sửa) | `POST CapNhatContact/{id}` | ContactController_CapNhatContact | public | — | `CapNhatContact(reqDto, CurrentUser)` — id route không dùng, lấy từ body | EF | id trên route bị bỏ qua (chỉ dùng reqDto) |
| 10 | Phân bổ lại data (sale/nhóm/MKT) | Nút "Phân bổ" (phanbo-data, phanbo-data-nhom) | `POST PhanBoLaiData` · `POST PhanBoLaiDataNhom` · `POST PhanBoDataNguonLanding` | ContactController_PhanBoLaiData / _PhanBoLaiDataNhom / _PhanBoLaiDataLandingMkt | public | — | Gán lại contact cho sale/nhóm sale/MKT landing. **(07/2026) Bỏ chặn contact có đơn offline đã chốt** — check `if (IdDonHang.HasValue) continue` đã comment lại (`ContactService.cs:3908`,`:4340`); chốt đơn còn kiểm qua cờ `isAcceptPBDHDaChot` (`:3914`) | EF | Trước đây contact có `IdDonHang` (đơn offline) bị bỏ qua khi phân bổ → nay cho phép |
| 11 | Thu hồi sale theo bộ lọc | Nút "Thu hồi sale" | `POST ThuHoiSaleContact` | ContactController_ThuHoiSaleContact | public | — | `ThuHoiBoLoc(reqDto, CurrentUser)` — gỡ sale khỏi contact khớp filter | StoreService | — |
| 12 | Đặt lịch từ contact | Nút "Đặt lịch" | `POST DatLichContact` | ContactController_DatLichContact | public | — | Tạo lịch hẹn từ contact → ripple Booking/LichHen | EF + Booking | — |
| 13 | Duyệt / hủy duyệt lịch (sale leader) | Tab "Chờ duyệt lịch" (danhsach-contact-datlich) | `POST GetListContactDatLichChoDuyet` · `POST DuyetContactDeTaoLich` · `POST HuyDuyetContactTaoLich` | GetListContactDatLichChoDuyet_TimTheoDieuKienAsync / DuyetContactDeTaoLich_TimTheoDieuKienAsync / ContactController_HuyDuyetContactTaoLich | public | state lịch = ChoDuyet | Leader duyệt → tạo lịch; hủy → trả về chờ duyệt | EF | Tên mã quyền không theo prefix Controller (đặt theo action) |
| 14 | Tạo contact thủ công (sale) | Nút "Thêm contact" | `POST InsertContactAsync` | ContactController_InsertContactAsync | public | — | `InsertContactAsync(reqDto, CurrentUser)` | EF | Mã quyền mô tả "Duyệt contact tạo lịch" (copy nhầm), `:322` |
| 15 | Marketing tạo / sửa contact | (Hồ sơ KH — MKT) | `POST Marketing_InsertContact` · `POST Marketing_UpdateContact` | ContactController_Marketing_InsertContact / _Marketing_UpdateContact | public | — | Tạo/sửa contact phía MKT. **(07/2026) thêm field `NgaySinh`** (`Contact.KhachHangNgaySinh`, DTO Mkt) → set `entity.KhachHangNgaySinh` (`ContactService.cs:3174`,`:6166`,`:12027`) + **propagate sang KhachHang** (event `EventUpdateCustomerFromContactMessage.NgaySinh`, `UpdateCustomerFromContactHelperService.cs:44`; backfill không override) | EF + RabbitMQ | — |
| 16 | Import contact (file) | Nút "Import", "Tải file mẫu", "Tải file lỗi" | `POST ImportContact` (IFormFile) · `GET DownloadFileTemplateImportContact` · `GET DownloadDataWrongContact/{id}` | `[SBAuthorize()]` (chỉ đăng nhập) | public | — | Map cột Excel index cứng (data từ row 7) → import; xem ContactImportController cho bản đầy đủ | EF/GemBox | Quyền chỉ `[SBAuthorize()]` không mã/role; import logic trùng với ContactImportController |
| 17 | Trùng số / khách cũ | Tab "Trùng số", "Khách cũ" | `POST GetDanhSachTrungSo` · `GetTrungSoKhachCu` · `GetDSContactByPhone` · `GetDanhSachTrungSo_TongTien` · `GetDanhSachKhachCu` · `GetDanhSachKhachCu_TongTien` · `GetDsTrungSo` · `CheckContactTrung` | ContactController_GetDanhSachTrungSo / _GetTrungSoKhachCu / _GetDSContactByPhone / _GetDanhSachTrungSo_TongTien / _GetDanhSachKhachCu(...) ; GetDsTrungSo & CheckContactTrung chỉ `[SBAuthorize]` | public | — | Phát hiện contact/đơn trùng SĐT, tính tổng tiền SP trùng; phần lớn forward `IHoSoKhachHangService` | StoreService + HoSoKH SP | GetDanhSachTrung* delegate sang HoSoKhachHangService (chéo logic) |
| 18 | Trùng sản phẩm | "Danh sách contact/đơn trùng SP" | `POST GetDanhSachTrungSanPham` | `[SBAuthorize()]` (mã/role bị comment) | public | — | `GetDonHangTrungSanPham` (HoSoKH) | SP | Quyền nới lỏng — dòng mã/role bị comment `:807` |
| 19 | Ghi chú / tin nhắn nội bộ | Tab "Ghi chú", link Zalo | `POST SaveMessage` · `SaveNote` · `GetListMessageByIdContact` · `GetListMessageByPhone(TimeLine)` · `SaveLinkZalo` · `GetLinkZalo/{id}` | Contact_SaveMessage / _SaveNote / _GetListMessageByIdContact / _GetListMessageByPhone(TimeLine) / _SaveLinkZalo / _GetLinkZalo | public | — | CRUD ghi chú/tin nhắn nội bộ + link nhóm Zalo theo contact | EF | — |
| 20 | Blacklist theo contact | Nút "Thêm/Xóa blacklist" (lịch sử Blacklist) | `POST InsertOrUpdateBlackList` · `POST DeleteBlackList` | Contact_InsertOrUpdateBlackList / Contact_DeleteBlackList | public | — | Thêm/xóa SĐT contact vào blacklist (xem ContactPhoneBlacklistController) | EF | — |
| 21 | Cập nhật ngày muốn nhận hàng | Form giao hàng | `POST UpdateNgayMuonNhanHang` | Contact_UpdateNgayMuonNhanHang | public | — | Set ngày muốn nhận hàng cho contact | EF | — |
| 22 | Tạo đơn hàng khách cũ | Nút "Tạo đơn khách cũ" | `POST TaoDonHangKhachCu` | ContactController_TaoDonHangKhachCu | public | đơn cũ đã chốt | Tạo contact mới từ contact cũ để lên đơn lại | EF | — |
| 23 | Xóa contact (đơn / nhiều / trùng) | Nút "Xóa" / bulk | `POST DeleteContact/{id}` · `POST DeleteMultipleContact` · `POST DeleteMultipleContactSoTrung` | ContactController_DeleteContact / Contact_DeleteMultipleContact (chỉ DV-AD-TONG) / Contact_DeleteMultipleContactSoTrung | public | — | Xóa contact; bulk delete siết quyền chỉ AD-TONG | EF | — |
| 24 | Cập nhật thời gian/ngày gọi tác nghiệp | (auto khi gọi tổng đài) | `POST UpdateThoiGianTacNghiep` · `GET CapNhatNgayGoiDauTien/{id}` | `[SBAuthorize()]` (chỉ đăng nhập) | public | — | Ghi mốc thời gian tác nghiệp / ngày gọi đầu | EF | — |
| 25 | Kiểm tra quyền truy cập / sửa SĐT | (gating UI) | `POST CheckAccessContact/{id}` · `GET CheckEditPhoneNumberByIdContact/{id}` (private) | `[SBAuthorize]` / `[PrivateSBAuthorize]` | mixed | — | Trả true/false để ẩn-hiện thao tác trên contact | EF | — |

## Nhóm endpoint phụ (private / cross-service — KHÔNG có UI trực tiếp)

> Đa số `[PrivateSBAuthorize]` (service-to-service) hoặc `[AllowAnonymous]`. Gom theo mục đích:

- **OrderLogistic ⇄ Contact (đồng bộ đơn):** `CapNhatListIdNhanKhachHangFromOrderLogistic` · `UpdateDonHangContact` · `CapNhatDhLogisticContact` · `DongBoContactXoaDonHang` · `ContactTacNghiepChuyenHuongChotDonKo/{id}` — đơn logistic đổi trạng thái/xóa → cập nhật ngược contact.
- **Lịch hẹn (Booking):** `DeleteContactMountedLichPrivate/{id}` · `CapNhapTrangThaiLichChoContact` · `SyncContactLichHenToLichHen` — đồng bộ trạng thái lịch ↔ contact.
- **CSKH / đếm ngược:** `CreateContactCSKH` · `UpdateContactCSKHCancel` · `DemNguocCskh` · `HuyDemNguocCskh` (CreateContactCSKH có cả `[SBAuthorize(key)]` lẫn `[PrivateSBAuthorize]` chồng nhau).
- **TMDT / ecommerce (AllowAnonymous):** `TMDT_InsertContactAsync` (tạo Contact + KhachHang) · `TMDT_DeleteContactAsync` · `InsertContactWebsitePrivateAsync` (AllowAnonymous, từ website/landing).
- **Tổng đài / AutoCall / Hotline:** `CreateContactFromHotline` · `CapNhatContactKhiAcceptAutoCall` (chỉ update khi tạo phiên cuộc gọi).
- **Sale/MKT gán (đồng bộ KhachHang):** `GetSaleMktMostRecent` · `CapNhapContactNhanKhachHang`.
- **Partner sandbox:** `UpdateKetQuaTacNghiepContactPartner` (RequestSandboxContactDto từ AuthenPartner).
- **Bulk private:** `DeleteMultipleContactWithoutDonHang`.
- **Đọc nội bộ:** `GetContactsByIds` · `GetInfoBasicContact` · `ChiTietLichHenSale/{id}` · `LayTacNghiepCan`.
- **Debug (vô hiệu):** `FixBugContact` — **luôn return rỗng** (`return ResponseResult(new MethodResult<object>())` ngay đầu), code thật sau đó là dead code.

## Quy tắc nghiệp vụ (gating / state lịch)

1. Đặt lịch (`DatLichContact`) → contact vào trạng thái **ChoDuyet** nếu cấu hình yêu cầu duyệt.
2. Sale leader `GetListContactDatLichChoDuyet` → `DuyetContactDeTaoLich` (true) → lịch tạo thật / `HuyDuyetContactTaoLich` → quay lại ChoDuyet.
3. "Nhận data" (CH1 chia xoay vòng): contact ẩn thông tin → sale bấm `NhanData` → bỏ ẩn + bắt đầu đếm thời gian tác nghiệp.

## Quan hệ
- **Gọi sang:** `IContactService` (CRUD + tác nghiệp) · `IContactStoreService` (list/trùng-số qua SP) · `IDataReceiveService` (CSKH) · `IHoSoKhachHangService` (trùng số/khách cũ) · `IKhoSoService` (chỉ inject, dùng trong FixBug).
- **Dữ liệu (entities):** `Contact` (gốc), `ContactMessage`/`ContactNote` (ghi chú), `ContactPhoneBlacklist`.
- **Ripple/đồng bộ:**
  - **OrderLogistic** đổi/xóa đơn → đồng bộ ngược contact (nhiều endpoint private).
  - **Booking/LichHen** ↔ contact (đặt lịch, sync trạng thái lịch).
  - **KhachHang** — gán sale/MKT, tạo KH khi TMDT tạo contact.
  - **KhoSo/ChiaSo** — chia data xoay vòng (NhanData).

## Luồng xuyên module
1. **TMDT tạo contact:** `TMDT_InsertContactAsync` (anonymous) → tạo Contact + KhachHang → ripple sang module Customer.
2. **OrderLogistic chốt/xóa đơn:** gọi private `UpdateDonHangContact` / `DongBoContactXoaDonHang` → cập nhật cờ đơn trên contact.
3. **Đặt lịch:** `DatLichContact` → tạo lịch Booking; sale leader duyệt → đồng bộ `SyncContactLichHenToLichHen`.
4. **Chia data xoay vòng (KhoSo):** contact ẩn → `NhanData` bỏ ẩn → bắt đầu tác nghiệp.

## Lỗi đã biết / lưu ý kỹ thuật
- `ContactController.cs:880-924` (`FixBugContact`) — **return rỗng ngay dòng đầu**, toàn bộ phần sau là dead code; debug-only, không nên expose.
- `ContactController.cs:154-155` (`NhanData`) — mã quyền + role bị comment, chỉ còn `[SBAuthorize]` → quyền nới lỏng.
- `ContactController.cs:670` & `:683` (`ChuyenTacNghiepTiep` / `V2`) — **dùng chung mã quyền** `ContactController_ChuyenTacNghiepTiep`.
- `ContactController.cs:218-224` (`CapNhatContact/{id}`) — tham số `id` trên route **không dùng**, chỉ lấy từ `reqDto`.
- `ContactController.cs:196-198` (`CreateContactCSKH`) — chồng `[SBAuthorize(key)]` + `[PrivateSBAuthorize]` (2 attribute auth khác nhau cùng method).
- `ContactController.cs:807-808` (`GetDanhSachTrungSanPham`) & `:475-476` rải rác — nhiều endpoint trùng-số có dòng mã/role bị comment, hạ về `[SBAuthorize()]` chỉ đăng nhập.
- `ContactController.cs:330-336` (`InsertContactWebsitePrivateAsync`), `:750`, `:789` (`TMDT_*`) — `[AllowAnonymous]` tạo/xóa contact: cần validate input chặt vì không auth.
- `ContactController.cs:322` (`InsertContactAsync`) — description mã quyền là "Duyệt contact tạo lịch" (copy nhầm, không khớp action).
