---
controller: TheDichVuController
module: kho
service: WarehouseProduct
man_hinh_ui: "Kho > Thẻ dịch vụ (route warehouse-product/thedichvu) — danh sách thẻ + nút Thêm/Sửa/Xóa + toggle Sử dụng"
muc_dich: "Quản lý danh mục THẺ DỊCH VỤ (cấu hình mẫu thẻ): thẻ số lần (trừ buổi dần), thẻ số tiền (trừ tiền dần), thẻ lịch; gán thẻ cho chi nhánh"
alias: ["thẻ dịch vụ", "thẻ số lần", "thẻ số tiền", "thẻ buổi", "thẻ trừ dần", "thẻ tiền", "the dich vu"]
auth: mixed
state_machine: []
deprecated: false
entities: [TheDichVu, TheDichVuDoiTuongApDung]
stored_procs: []
depends: [Core/ChiNhanh, CommonService(cache + thông báo)]
cross_module_in: [Website (private endpoints), Order/Sale (tính hoa hồng, dùng thẻ), Booking (thẻ lịch)]
cross_module_out: [TheDichVuKhachHang (thẻ đã bán cho KH), cache TheDichVu, thông báo gán/gỡ/sửa thẻ tới chi nhánh]
---

# TheDichVuController — Quản lý thẻ dịch vụ (danh mục mẫu thẻ)

> Đây là controller QUẢN LÝ DANH MỤC MẪU THẺ (định nghĩa loại thẻ + giá + thời hạn + chi nhánh áp dụng). Việc "trừ buổi/tiền dần" của KH xảy ra ở instance đã bán — entity `TheDichVuKhachHang` (controller riêng), không ở đây. `LoaiThe`: 1=TheSoLan (trừ buổi dần), 2=TheSoTien (trừ tiền dần), 3=TheLich (mỗi đơn vị chỉ 1 thẻ lịch).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện (grid chính) | Lưới + ô tìm kiếm + filter loại thẻ | POST `TimTheoDieuKien` | `TheDichVu_TimTheoDieuKien` | public | Mặc định màn hình | Lọc theo DonViId + chi nhánh áp dụng (join `TheDichVuDoiTuongApDung`); non-admin ép `IdChiNhanh=CurrentUser.IdChiNhanh`; phân trang, set `LoaiTheText` | EF (TableWithNolock + join) | Non-admin luôn ép theo chi nhánh user, bỏ qua filter.IdChiNhanh client gửi |
| 2 | Thêm mới thẻ | Nút "Thêm mới thẻ dịch vụ" → popup | POST `ThemMoi` | `TheDichVu_ThemMoi` | public | Luôn | Validate theo LoaiThe (xem Quy tắc nghiệp vụ), check trùng tên, gen Ma 8 ký tự unique, tính NgayHetHan nếu TuNgayTaoThe, insert thẻ + bản ghi chi nhánh áp dụng trong transaction, xóa cache, gửi thông báo gán thẻ | EF (transaction) | Trùng tên check 2 lần (Ten == Trim + Replace-space-upper) |
| 3 | Cập nhật thẻ | Nút Sửa → popup | POST `CapNhat` | `TheDichVu_CapNhat` | public | Luôn | Validate như Create; tính lại danh sách chi nhánh thêm/sửa/xóa; xóa hết bản ghi áp dụng cũ rồi insert lại; gửi thông báo gỡ/sửa/gán theo diff; xóa cache | EF (transaction) | Message trả "Thêm mới thành công!" dù là update (copy-paste) |
| 4 | Xóa thẻ | Nút Xóa (confirm) | GET `Xoa/{id}` | `TheDichVu_Xoa` | public | Luôn | Chặn xóa nếu thẻ đã bán cho KH (`TheDichVuKhachHang` tồn tại); xóa bản ghi áp dụng + thẻ trong transaction; gửi thông báo gỡ; xóa cache | EF (transaction) | Dùng `GuiThongBaoInfoGoComboChiNhanh` (tên combo) cho thẻ dịch vụ |
| 5 | Lấy chi tiết theo ID | (mở popup sửa) | GET `TimTheoId/{id}`, GET `TimTheoId_CN/{id}` | `TheDichVu_TimTheoId`, `TheDichVu_TimTheoId_CN` | public | Khi sửa | Map DTO + load ListChiNhanh; bản `_CN` thêm check thẻ có thuộc chi nhánh user không | EF | - |
| 6 | Bật/tắt sử dụng thẻ | Toggle cột "Trạng thái sử dụng" | GET `ChangeSuDungThe/{id}` | `TheDichVu_ChangeSuDungCombo` | public | Mỗi dòng | Đảo `IsSuDung` (null→true); xóa cache | EF | Permission key tên `...Combo` nhưng là thẻ dịch vụ |
| 7 | Lấy tất cả thẻ (theo chi nhánh / đơn vị) | (dropdown / dùng nội bộ FE) | GET `LayTatCa/{isSuDung}`, GET `LayTatCa_DV/{isSuDung}` | `TheDichVu_LayTatCa`, `TheDichVu_LayTatCa_DV` | public | Khi cần dropdown | Lấy qua CommonService (có cache); `LayTatCa`=theo chi nhánh, `_DV`=toàn đơn vị | EF + cache (CommonService) | - |
| 8 | Lấy danh sách phân trang | (lưới/dropdown phân trang) | POST `LayTatCaPt`, GET `GetTheDichVuPaging`, GET `GetTheDichVu_DonVi_Paging` | `TheDichVu_LayTatCaPt`, `TheDichVu_GetTheDichVuPaging`, `TheDichVu_GetTheDichVu_DonVi_Paging` | public | Khi cần | Phân trang theo keyword/chi nhánh, sắp xếp theo IdNhom | EF | - |
| 9 | Lấy thẻ để tính hoa hồng | (dùng cho cấu hình hoa hồng) | GET `GetTheDichVuTinhHoaHong/{id}` | `TheDichVu_GetTheDichVuTinhHoaHong` | public | Khi cấu hình HH | Non-admin ép id=IdChiNhanh; trả thẻ theo chi nhánh | EF | - |
| 10 | Lấy ds thẻ theo đơn vị / chi nhánh | (dropdown) | GET `GetDsTheTheoDonVi`, GET `GetDsTheTheoChiNhanh` | (không key, `[SBAuthorize()]`) | public | Khi cần | Trả `ResBaseTheoDonVi` gom theo đơn vị/chi nhánh | EF | - |
| 11 | Lấy thẻ theo IDs | (nội bộ FE) | POST `GetTheDichVuByIds` | `TheDichVu_GetTheDichVuByIds` | public | Khi cần | Lấy list thẻ theo ids trong cùng đơn vị | EF | - |

## Endpoint nội bộ (private / service-to-service)
<!-- caller → endpoint → ripple -->
- [internal] Service Website gọi `LayTatCa_Private` (POST, `LayTatCaPrivateFilterDto`) để hiển thị thẻ dịch vụ lên web bán hàng.
- [internal] Service Website gọi `TimTheDichVuTheoId_Private` / `TimTheDichVuTheoIds_Private` lấy chi tiết 1 hoặc nhiều thẻ theo Id.
- [internal] `TimTheoDieuKien_Private` (POST, `TheDichVuFilterDtoAPI` có `IdDonVi`/`IdChiNhanh`) — tìm theo điều kiện cho caller ngoài (không qua token user).
- [internal] `PrivateGetTheByIds` (POST, `PrivateGetItemFilterDto`) → trả `List<DropdownCommon>` cho service khác build dropdown.
- Tất cả endpoint trên gắn `[PrivateSBAuthorize]` (auth = internal).

## Quy tắc nghiệp vụ (gating / tier / ngưỡng)
<!-- blueprint cho trace-agent — validate Create/Update theo LoaiThe -->
- **LoaiThe = TheSoLan (1) — thẻ trừ buổi dần:**
  - `GiaBan` bắt buộc (≠ null) → false: "Giá bán không được bỏ trống!"
  - `SoLanSuDung` bắt buộc > 0 → false: "Số lần sử dụng không được bỏ trống và phải lớn hơn 0!"
  - Bắt buộc `ThoiHanTheBatDau` ≠ null và (`ThoiHanThang` > 0 OR `ThoiHanNgay` > 0).
- **LoaiThe = TheSoTien (2) — thẻ trừ tiền dần:**
  - `GiaBan` bắt buộc; `GiaTri` (số tiền trong thẻ) bắt buộc ≠ null → false: "Số tiền trong thẻ không được bỏ trống!"
  - Nếu `SoTienKhuyenMai` > 0 thì `CauHinhTienKhuyenMai` bắt buộc → false: "Cấu hình tiền khuyến mãi không được bỏ trống!"
  - Bắt buộc `ThoiHanTheBatDau` ≠ null và (ThoiHanThang>0 OR ThoiHanNgay>0).
- **LoaiThe = TheLich (3):**
  - Mỗi đơn vị **chỉ 1 thẻ lịch** — Create check tồn tại → "Thẻ lịch đã tồn tại, không thể tạo thẻ thứ 2 cùng loại." (lưu ý: query existTheLich KHÔNG lọc IdDonVi → kiểm tra toàn hệ thống, xem Lỗi đã biết).
  - Không chọn chi nhánh → tự set `ListChiNhanh = [Guid.Empty]` (all chi nhánh).
  - Force: `IsKhongGioiHan=true`, `ThoiHanThang=0`, `ThoiHanNgay=0` (không cần thời hạn).
- **Chung (mọi loại):**
  - `Ten` bắt buộc; trùng tên (Replace-space + ToUpper, cùng đơn vị) → "Tên đã tồn tại trong hệ thống".
  - `ListChiNhanh` rỗng (với TheSoLan/TheSoTien) → "Cần chọn ít nhất 1 chi nhánh!".
  - `Guid.Empty` trong ListChiNhanh = áp dụng TẤT CẢ chi nhánh (gọi Core lấy full list).
  - `IsAdjustAmount20=true` → `TyLeDoanhThu` bắt buộc ∈ {0,1,2,3,5}; else ép `TyLeDoanhThu=null`.
- **ThoiHanTheBatDau = TuNgayTaoThe (2):** `NgayHetHan = NgayTao + ThoiHanThang(tháng) + ThoiHanNgay(ngày)`. Nếu TuNgayCapThe (1): hết hạn tính khi cấp thẻ cho KH (không set ở đây).

## Quan hệ
- **Gọi sang:** Core `ChiNhanh/GetChiNhanhTheoDonVi`, `ChiNhanh/GetListChiNhanhByIds` (validate + expand chi nhánh); `CommonService` (cache + gửi thông báo).
- **Dữ liệu (entities):** `TheDichVu` (mẫu thẻ) + `TheDichVuDoiTuongApDung` (n chi nhánh áp dụng, KieuLoai=ChiNhanh). Mỗi thẻ áp dụng nhiều chi nhánh hoặc Guid.Empty=tất cả.
- **Ripple / đồng bộ:** Sau Create/Update/Delete/ChangeSuDung → `XoaCacheTheDichVu` + `XoaCachePrefixTheDichVuChiNhanh`; gửi thông báo `GanThe`/`GoThe`/`SuaThe` tới các chi nhánh bị ảnh hưởng. Xóa bị chặn nếu đã bán cho KH (`TheDichVuKhachHang`).

## Luồng xuyên module
<!-- trigger → bước → side-effect cross-module -->
- **Tạo/sửa thẻ → đẩy về chi nhánh:** Admin đơn vị tạo thẻ → chọn chi nhánh (hoặc all) → BE validate qua Core ChiNhanh → insert + gửi thông báo info tới user các chi nhánh được gán/gỡ → các chi nhánh thấy thẻ trong dropdown khi bán.
- **Website đồng bộ:** Website service kéo thẻ qua `*_Private` để hiển thị/bán online (không qua token user, dùng IdDonVi).

## Lỗi đã biết / lưu ý kỹ thuật
- `TheDichVuService.cs:294` — check tồn tại TheLich (`existTheLich`) KHÔNG lọc `IdDonVi` → có thể chặn nhầm giữa các đơn vị (rule comment "mỗi đơn vị 1 thẻ lịch" nhưng query là toàn bảng).
- `TheDichVuService.cs:724` & `:787` — message trả về "Thêm mới thành công!" ở cả nhánh Update và Delete (copy-paste, sai ngữ cảnh).
- `TheDichVuController.cs:120` — permission key `TheDichVu_ChangeSuDungCombo` / `:783` thông báo `GuiThongBaoInfoGoComboChiNhanh` dùng từ "Combo" cho thẻ dịch vụ (đặt tên nhầm theo combo, hành vi vẫn đúng).
- `TheDichVuController.cs:150-160` — `TimTheoDieuKien_CN` đã comment khóa cứng (không còn endpoint chi nhánh riêng); grid non-admin dùng chung `TimTheoDieuKien` (ép IdChiNhanh).
- `TheDichVuService.cs:202` — `TimTheoDieuKienAsync` join chi nhánh luôn dùng `userLogin.IdChiNhanh` (không phải `gridInfo.IdChiNhanh`) → filter chi nhánh client gửi không có tác dụng cho non-admin.
