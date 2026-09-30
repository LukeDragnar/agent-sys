---
controller: TheDichVuChiNhanhTaoController
module: kho
service: WarehouseProduct
man_hinh_ui: "Quản lý kho > Thẻ dịch vụ chi nhánh tạo (route thedichvu-chinhanhtao, thedichvu-chinhanhtao.component) — list thẻ dịch vụ do CHI NHÁNH tự tạo; nút Thêm mới / Sửa / Xóa / nz-switch Sử dụng. Edit popup thedichvu-chinhanhtao-edit. Phân biệt với màn thẻ dịch vụ cấp đơn vị (TheDichVuController)."
muc_dich: "Quản lý thẻ dịch vụ (gift/prepaid card: thẻ số tiền hoặc thẻ số lần) do CHI NHÁNH tự tạo — bản song song của thẻ dịch vụ cấp đơn vị nhưng phạm vi và phân quyền chi nhánh: CRUD, gán đối tượng áp dụng (chi nhánh), bật/tắt sử dụng, lấy thẻ để tính hoa hồng."
alias: ["thẻ dịch vụ chi nhánh tạo", "thẻ chi nhánh tạo", "thẻ dịch vụ chi nhánh", "gift card chi nhánh", "thẻ số tiền", "thẻ số lần", "the dich vu", "tạo thẻ tại chi nhánh"]
auth: public
state_machine: []
deprecated: false
entities: [TheDichVu, TheDichVuDoiTuongApDung, TheDichVuKhachHang]
stored_procs: []
depends: [WarehouseProduct, Core/ChiNhanh (GetChiNhanhTheoDonVi / GetListChiNhanhByIds), CommonService (cache thẻ + gửi thông báo gán/gỡ/sửa thẻ chi nhánh)]
cross_module_in: [Order/Thu ngân (PrivateGetTheByIds — chọn thẻ để bán/áp), Hoa hồng (GetTheDichVuTinhHoaHong)]
cross_module_out: [TheDichVuKhachHang (thẻ bán cho khách), Core/ChiNhanh (đồng bộ đối tượng áp dụng), Notification (thông báo gán/gỡ/sửa thẻ tới chi nhánh)]
---

# TheDichVuChiNhanhTaoController — Thẻ dịch vụ do chi nhánh tạo

> Thẻ dịch vụ = thẻ trả trước/quà tặng. `LoaiThe`: TheSoTien (thẻ số tiền) hoặc TheSoLan (thẻ số lần). Bản "ChiNhanhTao" gán cứng `IdChiNhanhTao = userLogin.IdChiNhanh` khi tạo và lọc theo chi nhánh hiện tại; đối tượng áp dụng lưu ở `TheDichVuDoiTuongApDung` (IdDoiTuong = Guid.Empty nghĩa là "tất cả chi nhánh").

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm thẻ theo điều kiện | "Tìm kiếm" lưới thẻ | POST `api/TheDichVuChiNhanhTao/TimTheoDieuKien` | `TheDichVuChiNhanhTao_TimTheoDieuKien` | public | màn list | Lọc thẻ theo `IdDonVi + IdChiNhanhTao == IdChiNhanh` user; keyword (theo Ten), LoaiThe; join `TheDichVuDoiTuongApDung` lấy danh sách chi nhánh áp dụng; phân trang. Non-admin: controller ép `filter.IdChiNhanh = CurrentUser.IdChiNhanh` | EF | - |
| 2 | Thêm mới thẻ | "Thêm mới" → `ThemMoi` | POST `api/TheDichVuChiNhanhTao/ThemMoi` | `TheDichVuChiNhanhTao_ThemMoi` | public | popup thêm | Validate (xem Quy tắc nghiệp vụ). Gán cứng `ListChiNhanh = [userLogin.IdChiNhanh]`, IdChiNhanhTao/IdChiNhanh = chi nhánh hiện tại, sinh `Ma` random 8 ký tự (unique trong đơn vị), tính NgayHetHan nếu thời hạn tính từ ngày tạo. Insert thẻ + đối tượng áp dụng trong transaction; sau đó xóa cache + gửi thông báo gán thẻ | EF | Validate gọi Core/ChiNhanh qua HTTP — chi nhánh service lỗi → "Kiểm tra chi nhánh thất bại!" |
| 3 | Cập nhật thẻ | "Lưu" → `CapNhat` | POST `api/TheDichVuChiNhanhTao/CapNhat` | `TheDichVuChiNhanhTao_CapNhat` | public | popup sửa | Check thẻ tồn tại trong đơn vị (→ "Không tồn tài thẻ dịch vụ cần cập nhật!"), validate như thêm; tính lại đối tượng áp dụng (delete cũ + insert mới), MarkDirty nhiều field, recompute NgayHetHan; trong transaction; gửi thông báo gán/gỡ/sửa theo diff chi nhánh + xóa cache | EF | **Message thành công sai**: trả "Thêm mới thành công!" cho cả Update (Service:575) |
| 4 | Xóa thẻ | "Xóa" → `Xoa` | GET `api/TheDichVuChiNhanhTao/Xoa/{id}` | `TheDichVuChiNhanhTao_Xoa` | public | per-row, confirm | Check tồn tại (→ "Không tồn tài thẻ dịch vụ cần xóa!"); **chặn nếu thẻ đã bán cho khách** (`TheDichVuKhachHang` tồn tại) → "Thẻ này đã bán cho khách hàng, không thể xóa!"; xóa thẻ + đối tượng áp dụng trong transaction; gửi thông báo gỡ + xóa cache | EF | **Message thành công sai**: trả "Thêm mới thành công!" cho Delete (Service:638) |
| 5 | Bật/tắt sử dụng thẻ | nz-switch "Sử dụng" → `changeSuDungThe` | GET `api/TheDichVuChiNhanhTao/ChangeSuDungThe/{id}` | `TheDichVuChiNhanhTao_ChangeSuDungCombo` | public | per-row toggle | Đảo cờ `IsSuDung` (null → true); xóa cache thẻ. Lỗi → FE revert switch | EF | **Permission key lệch tên**: key là `..._ChangeSuDungCombo` cho action đổi sử dụng THẺ (nhãn "ChangeSuDungThe") — dễ map nhầm (Controller:106) |
| 6 | Tìm thẻ theo Id | (mở popup sửa) | GET `api/TheDichVuChiNhanhTao/TimTheoId/{id}` | `TheDichVuChiNhanhTao_TimTheoId` | public | per-row | Lấy thẻ theo id + đơn vị (→ "Không tồn tài thẻ dịch vụ này!"); kèm ListChiNhanh áp dụng | EF | - |
| 7 | Lấy nhiều thẻ theo Ids | (load chi tiết nhiều thẻ) | POST `api/TheDichVuChiNhanhTao/GetTheDichVuByIds` | `TheDichVuChiNhanhTao_GetTheDichVuByIds` | public | - | Trả list `TheDichVu` theo danh sách id trong đơn vị | EF | - |
| 8 | Lấy tất cả thẻ (sử dụng) | (dropdown chọn thẻ) | GET `api/TheDichVuChiNhanhTao/LayTatCa/{isSuDung}` | `TheDichVuChiNhanhTao_LayTatCa` | public | - | Admin → `LayTatCaThe`; non-admin → `LayTatCaTheChiNhanh` (qua CommonService, có cache) | EF/cache | - |
| 9 | Lấy thẻ để tính hoa hồng | (luồng tính hoa hồng) | GET `api/TheDichVuChiNhanhTao/GetTheDichVuTinhHoaHong/{id}` | `TheDichVuChiNhanhTao_GetTheDichVuTinhHoaHong` | public | - | Non-admin: controller ép `id = CurrentUser.IdChiNhanh`. Join đối tượng áp dụng theo chi nhánh, trả list thẻ phục vụ tính hoa hồng | EF | Param `id` thực chất là IdChiNhanh chứ không phải IdThe — đặt tên gây hiểu nhầm (Controller:86) |
| 10 | CRUD/list biến thể `_CN` | (region chi nhánh) | GET `TimTheoId_CN/{id}` · GET `LayTatCa_CN` | `TheDichVuChiNhanhTao_TimTheoId_CN` / `_LayTatCa_CN` | public | - | `TimTheoId_CN`: lấy thẻ + check thẻ thuộc chi nhánh user (Guid.Empty hoặc khớp IdChiNhanh) → ko thuộc → "Combo này không thuộc chi nhánh!"; `LayTatCa_CN`: lấy tất cả thẻ chi nhánh | EF | Message "Combo này không thuộc chi nhánh!" dùng từ "Combo" cho THẺ — copy nhầm context (Service:744) |

## Endpoint nội bộ (private / service-to-service — `[PrivateSBAuthorize]`)
- [internal] **PrivateGetTheByIds** (POST, `PrivateGetItemFilterDto`) — service ngoài (Order/Thu ngân) lấy dropdown thẻ theo `IdDonVi` + filter keyword/ListId; rỗng cả keyword lẫn ListId → trả list rỗng. Dùng khi bán/áp thẻ dịch vụ trên đơn hàng.

## Quy tắc nghiệp vụ (validate tạo/sửa thẻ — blueprint để trace)
1. **Tên bắt buộc + không trùng** (Create): rỗng → "Tên thẻ không được để trống!"; trùng `Ten` trong đơn vị → "Tên thẻ dịch vụ đã tồn tại!". (Service:179-182)
2. **Giá bán bắt buộc**: `GiaBan == null` → "Giá bán không được bỏ trống!". (Service:183-184,385-386)
3. **Thẻ số lần** (`LoaiThe == TheSoLan`): `SoLanSuDung` null/0 → "Số lần sử dụng không được bỏ trống và phải lớn hơn 0!". (Service:185-188,387-390)
4. **Thẻ số tiền** (`LoaiThe == TheSoTien`): `GiaTri == null` → "Số tiền trong thẻ không được bỏ trống!"; nếu `SoTienKhuyenMai > 0` mà thiếu `CauHinhTienKhuyenMai` → "Cấu hình tiền khuyến mãi không được bỏ trống!". (Service:192-208,394-410)
5. **Thời hạn**: `ThoiHanTheBatDau == null` → "Thời hạn thẻ bắt đầu không được bỏ trống!"; cả `ThoiHanThang` lẫn `ThoiHanNgay` đều null/0 → "Thời hạn không được bỏ trống!"; nếu bắt đầu = TuNgayTaoThe → tính `NgayHetHan = NgayTao + ngày + tháng`. (Service:209-221,270-275)
6. **Chi nhánh áp dụng**: Create ép `ListChiNhanh = [chi nhánh hiện tại]`; nếu chứa Guid.Empty → áp dụng "tất cả chi nhánh" (gọi GetChiNhanhTheoDonVi); ngược lại verify từng id qua GetListChiNhanhByIds, thiếu → "Chi nhánh không tồn tại!". (Service:225-267,430-459)
7. **Mã thẻ** unique: sinh random 8 ký tự, loop đến khi không trùng trong đơn vị. (Service:280-285)
8. **Chặn xóa khi đã bán**: thẻ có bản ghi `TheDichVuKhachHang` → không cho xóa. (Service:584-585)

## Quan hệ
- **Gọi sang:** Core `ChiNhanh/GetChiNhanhTheoDonVi`, `ChiNhanh/GetListChiNhanhByIds` (verify + lấy danh sách chi nhánh); `CommonService` (LayTatCaThe/LayTatCaTheChiNhanh có cache, XoaCacheTheDichVu, XoaCachePrefixTheDichVuChiNhanh, GuiThongBaoInfoGanThe/GoThe/SuaThe ChiNhanh).
- **Dữ liệu (entities):** `TheDichVu` (đầu thẻ: Ten/Ma/LoaiThe/GiaBan/GiaTri/SoLanSuDung/ThoiHan.../SoTienKhuyenMai/IdChiNhanhTao/IsSuDung), `TheDichVuDoiTuongApDung` (đối tượng áp dụng theo chi nhánh), `TheDichVuKhachHang` (thẻ đã bán cho khách — dùng để chặn xóa).
- **Ripple / đồng bộ:** tạo/sửa/xóa thẻ → đẩy thông báo (Notification) tới các chi nhánh được gán/gỡ/sửa; xóa cache thẻ dùng chung; thẻ là master để bán cho khách (TheDichVuKhachHang) và để tính hoa hồng.

## Lỗi đã biết / lưu ý kỹ thuật
- **Message thành công copy nhầm**: `Update` (Service:575) và `Delete` (Service:638) đều trả "Thêm mới thành công!" thay vì "Cập nhật/Xóa thành công".
- **Permission key lệch tên**: `ChangeSuDungThe` dùng key `TheDichVuChiNhanhTao_ChangeSuDungCombo` (chữ "Combo" trong action của THẺ) — `Controller:106`.
- **Từ "Combo" dùng nhầm cho thẻ**: `GetById_CN` trả lỗi "Combo này không thuộc chi nhánh!" trong service của THẺ dịch vụ — `Service:744`.
- **Param đặt tên gây hiểu nhầm**: `GetTheDichVuTinhHoaHong(Guid? id)` — `id` thực ra là IdChiNhanh (non-admin bị ép = CurrentUser.IdChiNhanh) chứ không phải id thẻ — `Controller:84-95`.
- **Code chết / validate comment**: cả Create/Update còn block validate hoa hồng + method `ValidationCreate` bị comment toàn bộ (Service:323-371) → đừng tưởng còn hiệu lực.
- **Phụ thuộc HTTP đồng bộ chi nhánh**: mọi thao tác tạo/sửa/xóa gọi Core/ChiNhanh; Core lỗi → "Kiểm tra chi nhánh thất bại!" và chặn toàn bộ thao tác.
- **`[SBAuthorize()]` rỗng ở controller-level** (Controller:10) + `[TienIch(..., TI_Kho)]` — yêu cầu đăng nhập + tiện ích Kho; quyền chi tiết khai báo per-method.
- **Phân biệt với `TheDichVuController`** (thẻ cấp đơn vị) — controller này là bản chi-nhánh-tạo (gán cứng chi nhánh hiện tại); sửa logic nhớ đối chiếu bản đơn vị tránh drift.
