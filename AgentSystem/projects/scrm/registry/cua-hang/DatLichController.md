---
controller: DatLichController
module: cua-hang
service: Booking
man_hinh_ui: "Cửa hàng / Đặt lịch hẹn (route `dat-lich-ad/dat-lich-view`, lịch view theo thời gian/vị trí/nhân viên/ngày) + form Tạo/Sửa lịch hẹn (modal) + popup Payment + màn telesale (contact) gọi các endpoint ViewSale_*; ngoài ra trang đặt lịch end-user công khai (TaoMoi)."
muc_dich: "Quản lý vòng đời lịch hẹn (đặt lịch trước) tại cửa hàng: tạo (admin/nhân viên/end-user/hàng loạt), xem theo nhiều lát cắt, cập nhật thông tin/trạng thái, đồng bộ trạng thái khi tạo/hủy đơn, import lịch hẹn, gắn nhãn khách hàng, gửi SMS/Zalo nhắc lịch."
alias: ["đặt lịch", "lịch hẹn", "booking", "đặt lịch trước", "lịch hẹn cửa hàng", "tạo lịch hẹn", "đặt lịch hàng loạt", "lịch telesale", "đặt lịch online"]
auth: mixed
state_machine: [ChuaXacNhan, DaXacNhan, ChoPhucVu, DangPhucVu, HoanThanh, Huy, KhongDen, DaDenNhungChuaMua]
deprecated: false
entities: [DatLich, DatLichChiTiet, DatLichChiTietSpDvComboDaMua, KhachHangDatLich, NguonDatLich, ChoNgoi]
stored_procs: []
depends: [Customer (KhachHang FindOrCreated/CapNhat), WarehouseProduct (DichVu/Combo/ComboKhachHang), Core (DonVi/ChiNhanh/User), SMS (AccountSms/Pattern/SmsSend), Contact (đồng bộ trạng thái + nhãn), Order (GetDetailDonHang), ShortLink, CauHinhService, MassTransit (GuiThongBaoInfo / UpdateThoiGianDenCuaHangMessage / GuiNotification...)]
cross_module_in: [Order — UpdateKhiThanhToanDonHang & CapNhatThoiGianPhucVu (PrivateSBAuthorize, khi thanh toán/đổi thời gian đơn); Order/thungan — CapNhatTrangThaiPrivate, CapNhatThongTinPayment; AuthenCustomer — KhachHangTimTheoDieuKienPrivate / KhachHangGetChiTietLichHenPrivate / GetLichHenById_Private (app khách hàng); Website end-user — TaoMoi (AllonAnonymous); Contact/Telesale — ViewSale_* + ContactDatLich_CapNhatThongTinPrivate]
cross_module_out: [Contact — đồng bộ trạng thái lịch (CapNhapTrangThaiLichChoContact) + nhãn KH (CapNhapContactNhanKhachHang) khi lịch có IdContact; Customer — FindOrCreated/CapNhat khách + UpdateThoiGianDenCuaHangMessage (publish) + SaveLogTuongTacKhachHang; SMS/Zalo — gửi nhắc lịch & link check-in; Notification — GuiThongBaoInfo + GuiNotificationDungChungTrangThaiLichHen (publish); ShortLink — tạo link đánh giá/check-in]
---

# DatLichController — Đặt lịch hẹn (booking) cửa hàng

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Xem lịch theo thời gian/vị trí/nhân viên/ngày | tab view lịch | POST `DsDatLichTheoThoiGian` / `DsDatLichTheoViTri` / `DsDatLichTheoNhanVien` / `DsDatLichTheoNgay` | `DatLich_DsDatLichTheoThoiGian` / `...TheoViTri` / `...TheoNhanVien` / `DatLich_DsDatLichTheoNgay` | public | màn lịch | List lịch theo DonVi+ChiNhánh của CurrentUser; nhân viên non-multi-role chỉ thấy lịch của mình (`GetLichByNhanVien`); che SĐT theo cấu hình che số | EF + HTTP enrich (KH/DV/Combo/CN) | - |
| 2 | Xem lịch (bản telesale) | view sale | POST `ViewSale_DsDatLichTheoThoiGian` / `ViewSale_DsDatLichTheoViTri` / `ViewSale_DsDatLichTheoNhanVien` / `ViewSale_GetDsDatLichTheoNgay` | `DatLich_ViewSale_DsDatLichTheoThoiGian` / `...TheoViTri` / `...TheoNhanVien` / `DatLich_ViewSale_GetDsDatLichTheoNgay` | public | màn telesale | Gọi cùng service GetDs* như #1 nhưng key quyền dành cho role sale/CSKH | EF | Endpoint sale gọi y hệt service view thường (cùng `GetDsDatLich*`), chỉ khác permission key — lọc role nằm bên trong service theo `_currentUser` |
| 3 | Đếm số lịch trong ngày | badge/đếm | POST `SoDatLichTheoNgay` | `[SBAuthorize()]` (chỉ cần login) | public | - | Đếm số lịch (sau check QLDV được phép xem POS) | EF | - |
| 4 | Tìm theo điều kiện (grid) | "Tìm kiếm" | POST `TimTheoDieuKien` | `DatLich_TimTheoDieuKien` | public | màn grid | List phân trang theo DonVi+ChiNhánh; filter Keyword (Ma/Ten/Sdt + giải mã hashKey→Id+date), ngày, trạng thái, chỗ ngồi, nguồn; QLDV phải được phép POS; che số SĐT | EF + HTTP enrich | Filter `ChoNgoiId`/`NhanVienId` dùng `KhachHang.Contains(id)` (quét chuỗi JSON cột `KhachHang`) thay vì join DatLichChiTiet (Service:1242,1251) |
| 5 | Lấy mã lịch hẹn | (auto khi mở form) | GET `GetMaDatLich` | `[SBAuthorize()]` | public | - | Sinh mã đặt lịch ngẫu nhiên (`GenerateMaDatLich`) — không chạm DB | (in-memory) | - |
| 6 | Tạo lịch (end-user web) | trang đặt lịch online | POST `TaoMoi` | (không key) | anonymous | trang public | Set nguồn=WEBSITE, Loai=DatLichTruoc, nguồn tạo KH=KhachHangDatLichTuWeb; `InsertAsync`: tách khách đính kèm → nhiều DatLich, tạo/tìm KH, tạo DatLichChiTiet, shortlink, gửi SMS/Zalo + notification, transaction | EF (transaction) + nhiều HTTP | `InsertAsync` return lỗi nếu `cauHinh==null` ("Không tìm thấy cấu hình!") → ngắt tạo lịch chỉ vì thiếu cấu hình (Service:2056-2057, có comment tự nhận) |
| 7 | Tạo lịch (nhân viên đơn vị) | "Lưu" (modal tạo) | POST `ThemMoi` | `DatLich_ThemMoi` | public | nút Lưu | Set nguồn=NhanVienDatLich; `InsertAsync` như #6; QLDV phải được phép POS; nếu SoLuongKhach>MaxCustomerInGroup → lỗi; chọn gửi SMS theo cấu hình `EnableSendSMSManualBooking`+`IsGuiSms` | EF (transaction) + HTTP | Như #6 (chung `InsertAsync`) |
| 8 | Tạo lịch hàng loạt | "Đặt hàng loạt" | POST `DatLichHangLoat` | `DatLich_DatLichHangLoat` | public | - | Lặp `ListReqKhungGio` → mỗi khung giờ gọi `InsertAsync` 1 lần; lỗi 1 khung giờ → dừng & trả lỗi | EF + HTTP | Không transaction bao toàn batch: lỗi ở khung giờ thứ N thì các lịch trước đó đã insert (mỗi `InsertAsync` commit riêng) → tạo lịch một phần (Service:4824-4837) |
| 9 | Xem chi tiết lịch | (mở modal) | GET `TimTheoId/{id}` | `DatLich_TimTheoId` | public | nút xem | Trả `DatLichDetail`; QLDV phải được phép POS; deserialize `KhachHang` JSON → enrich tên DV/Combo/chỗ ngồi/nhân viên (HTTP) | EF + HTTP | - |
| 10 | Cập nhật thông tin | "Lưu" (modal sửa) | POST `CapNhatThongTin` | `DatLich_CapNhatThongTin` | public | nút Lưu | Nếu thiếu SĐT → lấy lại từ DB; `UpdateAsync`: check quyền sale (QL sale/SaleLead/Sale chỉ sửa lịch sale mình), tách khách, mark-dirty các field, đồng bộ Contact nếu IdContact | EF | - |
| 11 | Cập nhật từ popup payment | popup Payment | POST `CapNhatThongTinPayment` | `DatLich_CapNhatThongTinPayment` | public | popup payment | Load DB rồi chỉ update LstKhachDinhKem/GhiChu/ThoiGian/SoLuongKhach/KhachHang qua `UpdateAsync` | EF | - |
| 12 | Cập nhật trạng thái | đổi trạng thái lịch | POST `CapNhatTrangThai` | `DatLich_CapNhatTrangThai` | public | - | Load DB; lưu `TrangThaiTruoc`; check quyền sale + QLDV được phép POS; update; đồng bộ Contact; publish UpdateThoiGianDenCuaHang + notification trạng thái | EF + publish | - |
| 13 | Chuyển trạng thái tạo lịch | (khi tạo đơn từ lịch) | POST `ChuyenTrangThaiTaoLich` | `DatLich_ChuyenTrangThaiTaoLich` | public | - | Giải mã Keyword→Id; chặn nếu lịch đã `HoanThanh`; set trạng thái = `cauHinh.UpdateTrangThaiTaoLich` (chỉ các giá trị hợp lệ); đồng bộ Contact | EF | - |
| 14 | Cập nhật trạng thái khi hủy đơn | (khi hủy đơn) | POST `CapNhatTrangThaiKhiHuyDon/{id}` | `DatLich_CapNhatTrangThaiKhiHuyDon` | public | - | Load DB; gán `TrangThai = TrangThaiTruoc`; QLDV được phép POS; update; đồng bộ Contact + publish notification | EF + publish | - |
| 15 | Xóa lịch hẹn | "Xóa" (confirm) | POST `XoaLichHen/{id}` | `DatLich_XoaLichHen` | public | nút Xóa | `DeleteAsync`: chặn xóa lịch online nếu cấu hình Contact `IsNotAllowedAdminRemoveLichHenOnl` (trừ admin/CSKH/super); check quyền sale; QLDV được phép POS | EF + HTTP cấu hình Contact | - |
| 16 | Danh sách trạng thái lịch hẹn | filter trạng thái | POST `ListTrangThaiLichHen` | `DatLich_ListTrangThaiLichHen` | public | - | Trả danh sách trạng thái lịch theo điều kiện | EF | - |
| 17 | Gắn nhãn khách hàng cho lịch | gán nhãn KH | POST `CapNhapLichHenNhanKhachHang` | `[SBAuthorize]` (login, key bị comment) | public | - | Update `ListIdNhanKhachHang` (join `;`); nếu IdContact → đồng bộ nhãn sang Contact | EF + HTTP | Key quyền cũ `DatLich_CapNhapLichHenNhanKhachHang` bị comment (Controller:583) → chỉ còn yêu cầu đăng nhập |
| 18 | Validate giờ đặt lịch / theo nhân viên | cảnh báo trùng giờ | POST `ValidateGioDatLich` / `ValidateGioDatLichNhanVien` | `[SBAuthorize()]` | public | khi chọn giờ | Đếm số lịch đã đăng ký trùng khung giờ (chỉ trạng thái DaXacNhan/ChoPhucVu/DangPhucVu); trả cảnh báo | EF | - |
| 19 | Cập nhật thời gian phục vụ | (từ đơn hàng) | POST `CapNhatThoiGianPhucVu` | `[SBAuthorize()]` | public | - | Update ThoiGian/ThoiGianDiChuyen/ThoiGianTong/GioKetThuc; lịch đã xóa → vẫn trả `true` (không lỗi) | EF | - |
| 20 | Lấy thời gian dịch vụ | - | GET `GetThoiGianDichVu/{idLichHen}` | `[SBAuthorize()]` | public | - | Trả thoiGianPhucVu/DiChuyen/Tong theo id | EF | - |
| 21 | Sắp nhân viên rảnh theo giờ | gợi ý nhân viên | POST `GetListNhanVienOrderByTimeRanh` / `GetListSaleOrNhanVienOrderByTimeRanh` / `GetLstNhanVienRanhTheoLichMuilti` | `[SBAuthorize()]` | public | form chọn NV | Trả danh sách nhân viên/sale sắp theo thời gian rảnh | EF + HTTP | - |
| 22 | Import lịch hẹn | "Tải file mẫu" / "Import" / "Tải lỗi" | POST `DownloadFileTemplateImportLichHen` / POST `ImportLichHen` / GET `DownloadDataWrongLichHen/{id}` | `[SBAuthorize]` (login, key cụ thể bị comment) | public | màn import | Xuất template `.xlsm`; import crawl + validate → tạo lịch; tải file dòng lỗi | GemBox Excel + EF | Các key quyền import (`DatLich_ImportLichHen`...) bị comment → chỉ cần đăng nhập (Controller:521-543) |
| 23 | Danh sách endpoint đặt lịch | (cấu hình landing) | GET `LayDatLichEndPointDanhSach` | (không key) | anonymous | - | Trả dict `{DatLich, DatLichNangCao}` (enum description) | (in-memory) | - |

## Endpoint nội bộ (private / service-to-service)
- [internal — `[PrivateSBAuthorize]`] **`TimTheoIdAsync_Private/{id}`** & **`GetLichHenById_Private/{id}`** — service khác lấy chi tiết / thông tin gọn của lịch hẹn (GetLichHenById_Private chỉ trả Id/DonVi/ChiNhánh/TenKH/GioBatDau/Ngay).
- [internal] **`KhachHangTimTheoDieuKienPrivate`** & **`KhachHangGetChiTietLichHenPrivate`** — app khách hàng (AuthenCustomer) list/chi tiết lịch theo IdKhachHang (UserLogin dựng từ `gridInfo.IdDonVi`).
- [internal] **`CapNhatTrangThaiPrivate`** — service khác đổi trạng thái + IsTaoHoaDon, đồng bộ Contact + publish UpdateThoiGianDenCuaHang.
- [internal] **`UpdateKhiThanhToanDonHang`** — Order gọi khi thanh toán đơn: set TrangThaiTruoc/TrangThai + TamTinhTruocLamTron + TienCoc; check quyền sale + QLDV được phép POS; đồng bộ Contact + notification.
- [internal] **`GetDsDatLichTheoNhanVien` (POST body `List<Guid> idNVs`)** — lấy lịch theo nhiều id nhân viên (không có SBAuthorize key, chỉ controller-level).

## Quy tắc nghiệp vụ (gating / trạng thái)
1. **QLDV (quản lý đơn vị) phải được phép xem POS**: hầu hết thao tác (Insert/Update/Delete/CapNhatTrangThai/TimTheoDieuKien/SoDatLichTheoNgay...) gọi `IsAllowAppointmentViewForCashier(_currentUser, configSV)`; false → "Đơn vị của bạn không cho phép sử dụng tính năng này!".
2. **`cauHinh == null` chặn cứng**: `InsertAsync` trả "Không tìm thấy cấu hình!" nếu CauHinhService null (Service:2056) → không tạo được lịch.
3. **Giới hạn nhóm khách**: `SoLuongKhach > MaxCustomerInGroup` (và Max>0) → lỗi giới hạn (Service:2059).
4. **Quyền sale theo phân cấp**: QL Sale / SaleLead / Sale chỉ thao tác lịch có `SaleId` thuộc danh sách user được phép (qua `CheckPermissionViewrReportContactWithMultiUser`); Sale thường qua `IsActionSaleAllowed`.
5. **ChuyenTrangThaiTaoLich**: chặn nếu lịch `HoanThanh`; chỉ áp khi `cauHinh.UpdateTrangThaiTaoLich` ∈ {DaXacNhan, ChoPhucVu, DangPhucVu, Huy, KhongDen}; nếu = HoanThanh → từ chối.
6. **Khách đính kèm**: khách thứ 2+ phải có Tên + SĐT (thiếu → lỗi); khách trùng IdKhachHang → "Các khách hàng không được trùng nhau!"; khách 2+ có thể nhận trạng thái riêng `TrangThaiKhiTaoThemKhachDatLich`.
7. **Đồng bộ Contact**: chỉ khi lịch có `IdContact` mới gọi sang Contact (đổi trạng thái / nhãn).

## Quan hệ
- **Gọi sang:** Customer (KhachHang FindOrCreated/ThemMoi_V2/CapNhat), WarehouseProduct (DichVu/ComboSpa), Core (ChiNhanh/DonVi/User thu ngân/role), SMS (AccountSmsAgency + SmsSend), Contact (CauHinh + CapNhapTrangThai/Nhan), Order (GetDetailDonHang), ShortLink (CreateCustomAsync).
- **Dữ liệu (entities):** `DatLich` (1 lịch/khách, cột `KhachHang` lưu JSON `List<KhachHangDatLich>`), `DatLichChiTiet` (DV/Combo/ComboKhachHang × NhanVien × ChoNgoi), `DatLichChiTietSpDvComboDaMua` (SP/DV trong combo đã mua).
- **Ripple / đồng bộ:** publish `GuiThongBaoInfo` (thông báo tạo lịch tới thu ngân/QLLH/admin), `GuiNotificationDungChungTrangThaiLichHen` (đổi trạng thái), `UpdateThoiGianDenCuaHangMessage` (Customer); `SaveLogTuongTacKhachHang`.

## Luồng xuyên module (tạo lịch hẹn)
```
ThemMoi/TaoMoi → InsertAsync
  → (nếu IdDonHang) check đơn đã có lịch chưa (Order)
  → tách LstKhachDinhKem → N entity DatLich + DatLichChiTiet
  → tìm/tạo KhachHang (Customer) → tạo ShortLink → gửi SMS/Zalo (SMS) theo cấu hình
  → publish GuiThongBaoInfo (thu ngân/QLLH/admin) → transaction insert
  → SaveLogTuongTacKhachHang (Customer)
Sau đó: CapNhatTrangThai/ChuyenTrangThaiTaoLich → đồng bộ Contact + publish notification
       Order thanh toán → UpdateKhiThanhToanDonHang (private) → đồng bộ Contact
```

## Lỗi đã biết / lưu ý kỹ thuật
- **`DatLichHangLoat` không transaction bao batch** (Service:4824-4837) — lỗi ở khung giờ thứ N → các lịch trước đã commit, tạo lịch một phần, không rollback toàn cục.
- **`InsertAsync` chặn cứng khi `cauHinh==null`** (Service:2056-2057) — comment trong code tự nhận "logic này gây luồng bị ngắt việc tạo lịch chỉ vì cấu hình null => cần xử lý lại".
- **Filter grid theo NhanVien/ChoNgoi dùng `KhachHang.Contains(id)`** (Service:1242,1251 và 5724,5733) — quét substring trong cột JSON `KhachHang` thay vì join `DatLichChiTiet` → có thể khớp nhầm (id xuất hiện ở field khác trong JSON).
- **`GetDatLichById_Private` viết `.FirstOrDefaultAsync(...).Select(...)`** (Service:7202) — gọi `.Select` sau `FirstOrDefaultAsync` (đối tượng đã materialize, không phải IQueryable). Cần xác nhận compile được; nếu build OK thì là extension lạ — đáng soi lại.
- **`KhachHangTimTheoDieuKienPrivate` (PrivateSBAuthorize) vẫn dùng `_currentUser.DonViId`** (Service:5793,5797 build reqCombo) — endpoint private có thể có `_currentUser == null` → rủi ro NullReferenceException; phần lấy KH/DV thì đã chuyển sang `gridInfo.IdDonVi`.
- **Nhiều key quyền bị comment** → giảm về chỉ-đăng-nhập: import (`ImportLichHen`/`DownloadFileTemplateImportLichHen`/`DownloadDataWrongLichHen`, Controller:521-543) và gắn nhãn (`CapNhapLichHenNhanKhachHang`, Controller:583). Cần xác nhận có chủ ý.
- **`TaoMoi` là `[AllowAnonymous]`** (Controller:165) — end-user web tạo lịch không cần token; service nhánh `_currentUser==null` lấy DonVi từ `model.IdDonVi`.
- **`ContactDatLich_CapNhatThongTinPrivate` map đè DTO** (Controller:268-309) — load entity từ DB rồi map lại + ghi đè ~20 field từ request; field không ghi đè sẽ giữ giá trị DB (tránh mất dữ liệu khi telesale sửa).
- **`SendSmsTruocNgayDatLich` (job nhắc lịch)** — quét batch 50 lịch/đơn vị có `Ngay = hôm nay + numberOfDays`, gửi SMS+Zalo, set `NgayGui` để không gửi lại; endpoint test job đã bị comment (Controller:409-416).
- Code thật: BE controller `Booking/Booking.API/Controllers/DatLichController.cs`; service `Booking/Booking.Service/Generated/DatLichService.cs` (~7200 dòng); interface/impl trong namespace `Booking.Services.Generated`.
