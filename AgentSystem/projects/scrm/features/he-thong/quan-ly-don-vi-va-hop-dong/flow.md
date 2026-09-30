# Luồng — Quản lý đơn vị & hợp đồng

## Tiền điều kiện
- Quản trị toàn hệ thống: **HT-AD** (chính), **HT-KT**, **HT-CSKH** (một số thao tác).
- Đăng ký đơn vị mới (bản app `ThemMoi`): **AllowAnonymous** — không cần đăng nhập.
- Xem/sửa thông tin đơn vị của chính mình: user đã đăng nhập, thuộc đơn vị đó.

## Luồng dùng (đơn vị tự thao tác — bản app, route api/DonVi)
1. Lấy thông tin: `GetDonViById` / `GetDonViByIdForEdit` (bản edit KHÔNG qua cache) / `GetInfoDonViBasic/{id}` / `GetInfoBasicByIds` (non-super/non-kế-toán-hệ-thống bị lọc chỉ còn đơn vị của mình).
2. **Đăng ký mới** `api/DonVi/ThemMoi` (`[AllowAnonymous]`) → đi qua `IDonViKhoiTaoServiceService.ThemMoiAsync` (khởi tạo tenant).
3. **Cập nhật** `CapNhatThongTin` (`DonVi_CapNhatThongTin`); **GenCode**; lấy lĩnh vực (`GetLinhVucByUser`), đơn vị theo role (`GetUnitByRole`).

## Luồng dùng (admin — quản trị đơn vị, route api/QuanTriArea/DonVi)
1. `DonVi/TimTheoDieuKien` / `LayTatca` → danh sách đơn vị toàn hệ thống (HT-AD/HT-KT/HT-CSKH).
2. `DonVi/TimTheoId/{id}` → `TimTheoIdAsyncNoCache` (sửa không lấy cache).
3. **Thêm/Sửa/Xóa** (`DonVi_ThemMoi`/`_CapNhatThongTin`/`_Xoa`); ThemMoi khởi tạo tenant.
4. **Xuất Excel** `DonVi/ExportExcel` → `DanhSachDonVi.xlsx` (chỉ `[SBAuthorize]`, không key).
5. **Gen / Duyệt mã** `GenCode`, `ApproveCode`.
6. **Gói & merchant**: `GetGoiVaUserByDonVi/{id}`, `YeuCauThayDoiGoiSuDungDonVi`, `GetDonViMerchant/{id}`, `UpdateDonViMerchant`.
7. **Search nhanh** `SearchByKeyword` (ModelState valid).

## Luồng dùng (hợp đồng & tiện ích)
1. **Hợp đồng** (`api/QuanTriArea/HopDong`): CRUD chuẩn (kế thừa SBBaseController) + `HopDong_TimTheoDieuKien` (HT-AD/HT-KT).
2. **Đơn vị tiện ích** (`api/QuanTriArea/DonViTienIch`): `TimTheoDieuKien` (HT-AD/HT-KT) → tiện ích nào gắn đơn vị nào.
3. **Hợp đồng tiện ích** (`api/QuanTriArea/HopDongTienIch`):
   - `TimTheoDieuKien` → danh sách yêu cầu mua.
   - **Duyệt** `DuyetHopDong/{ma}` (theo **mã**) → kích hoạt tiện ích cho đơn vị (chỉ khi đang chờ duyệt).
   - **Hủy duyệt** `HuyDuyetHopDong/{id}` (đã duyệt → hủy).
   - **Chi tiết** `ChiTietHopDong/{id}`; **Xóa** `Xoa/{id}` (check tồn tại trước).

## Case biên
- **Đăng ký đơn vị mới (AllowAnonymous)** → ai cũng gọi được → rủi ro đăng ký rác (known-issue).
- **GetInfoBasicByIds** với user thường (non-super/non-kế-toán-HT) → bị lọc chỉ trả đơn vị của chính user (không xem được đơn vị khác).
- **Xuất Excel đơn vị** → chỉ cần đăng nhập (không key) → ai login gọi endpoint cũng xuất được.
- **Hợp đồng tiện ích Duyệt theo {ma}, các thao tác khác theo {id}** → truyền nhầm loại tham số sẽ thất bại (known-issue không đồng nhất).
- **Duyệt hợp đồng tiện ích khi không ở trạng thái chờ duyệt** → không hợp lệ; Hủy duyệt chỉ với hợp đồng đã duyệt.
- **Xóa hợp đồng/hợp đồng tiện ích không tồn tại** → trả `MESS_NotFound`.

## Ví dụ cụ thể có số
Nhà cung cấp có 350 đơn vị đang hoạt động.
- Admin lọc đơn vị "tỉnh = Hà Nội, gói = Pro" còn 40 đơn vị → **Xuất Excel** → file `DanhSachDonVi.xlsx` 40 dòng.
- Shop mới "Cafe Sữa" đăng ký đơn vị → hệ thống khởi tạo tenant; admin **Sinh mã** rồi **Duyệt mã** đơn vị.
- Đơn vị "Cafe Sữa" gửi yêu cầu mua tiện ích "ChatHub" → tạo hợp đồng tiện ích (mã HD-00123, trạng thái Chờ duyệt) → admin/kế toán mở **Hợp đồng tiện ích**, bấm **Duyệt** theo mã HD-00123 → tiện ích ChatHub kích hoạt cho "Cafe Sữa". Nếu sau đó nhân viên vẫn không vào ChatHub được → kiểm tra role đã được cấp **quyền** ChatHub chưa.
- Admin tra **Đơn vị tiện ích** với filter tiện ích = ChatHub → thấy "Cafe Sữa" trong danh sách đơn vị đang dùng.

## Hậu điều kiện
- Thêm đơn vị → khởi tạo dữ liệu tenant (qua `IDonViKhoiTaoServiceService`).
- Duyệt hợp đồng tiện ích → bật tiện ích cho đơn vị (các controller có `[TienIch("<code>")]` cho phép truy cập).
- Yêu cầu/duyệt đổi gói → cập nhật gói áp dụng cho đơn vị.
- CRUD hợp đồng / đơn vị tiện ích → cập nhật DB (không bắn event rõ rệt).
