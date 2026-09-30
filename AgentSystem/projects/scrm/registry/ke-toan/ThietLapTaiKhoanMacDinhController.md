---
controller: ThietLapTaiKhoanMacDinhController
module: ke-toan
service: Accounting
man_hinh_ui: "Kế toán > Thiết lập > 'Thiết lập tài khoản mặc định': lưới theo (Loại đối tượng × chi nhánh) gắn 1 số TK mặc định. Nút: Thêm mới, Sửa, Xóa, 'Áp dụng theo đơn vị' (reset chi nhánh = copy bộ thiết lập cấp đơn vị). Lọc: Chi nhánh, Loại đối tượng, Keyword."
muc_dich: "Map mỗi loại đối tượng (Tiền mặt / Ngân hàng / Ví điện tử / Khách hàng / Nhà cung cấp...) sang 1 số tài khoản kế toán mặc định, theo đơn vị + chi nhánh. Dùng để gợi ý/điền sẵn TK khi lập phiếu & hỗ trợ định khoản."
alias: ["thiết lập tài khoản mặc định", "tài khoản mặc định theo đối tượng", "thiết lập TK mặc định", "ThietLapTaiKhoanMacDinh", "tk mặc định chi nhánh", "map đối tượng - tài khoản"]
auth: public
state_machine: []
deprecated: false
entities: [ThietLapTaiKhoanMacDinh]
stored_procs: []
depends: [IRepository<ThietLapTaiKhoanMacDinh>, IClientHelper, ServiceSettings]
cross_module_in: []
cross_module_out: []
---

# ThietLapTaiKhoanMacDinhController — Thiết lập tài khoản mặc định theo (loại đối tượng × chi nhánh)

> CRUD thuần: 1 bản ghi = 1 (`IdDonVi`, `IdChiNhanh`, `LoaiDoiTuong`) → `MaTaiKhoanMacDinh`. Chống trùng theo bộ ba đó. Toàn bộ endpoint `[SBAuthorize()]` trống (chỉ cần login, không gắn permission key). Mọi truy vấn ép `IdDonVi == userLogin.DonViId`. `ResetTheoDonViAsync` = "áp dụng theo đơn vị": xóa thiết lập chi nhánh hiện tại của user rồi clone từ bộ thiết lập cấp đơn vị (IdChiNhanh null/Empty).

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Load lưới | Lưới + lọc (Chi nhánh, Loại đối tượng, Keyword) | `POST api/ThietLapTaiKhoanMacDinh/TimTheoDieuKienAsync` | — | public | màn thiết lập | Lọc `IdDonVi == DonViId`; thêm filter IdChiNhanh / LoaiDoiTuong / keyword (theo LoaiDoiTuong). Paging, `OrderBy NgayCapNhat DESC`. | EF `ThietLapTaiKhoanMacDinh` | Keyword chỉ match `LoaiDoiTuong`, không match MaDoiTuong/TenDoiTuong/MaTaiKhoan. |
| 2 | Thêm mới | Popup / Lưu | `POST api/ThietLapTaiKhoanMacDinh/InsertAsync` | — | public | nút Thêm mới | Validate LoaiDoiTuong + MaTaiKhoanMacDinh bắt buộc. Chặn trùng (`DonViId`, `IdChiNhanh`, `LoaiDoiTuong`) → "Thiết lập đã tồn tại. Vui lòng chỉnh sửa...". Insert gán IdDonVi = DonViId. | EF Insert | — |
| 3 | Sửa | Popup sửa / Lưu | `POST api/ThietLapTaiKhoanMacDinh/UpdateAsync` | — | public | chọn 1 dòng | Bắt buộc Id; validate như #2. `GetByIdAsync(req.Id)` (không lọc đơn vị). Chặn trùng (DonViId, IdChiNhanh, LoaiDoiTuong, Id != self). Update + MarkDirty. | EF Update | `GetByIdAsync` không lọc `IdDonVi` → biết Id sửa được bản ghi đơn vị khác (chỉ check trùng theo DonViId của *người sửa*, không verify bản ghi thuộc đơn vị đó). |
| 4 | Xóa | Nút Xóa / confirm | `POST api/ThietLapTaiKhoanMacDinh/DeleteAsync?id=` | — | public | mỗi dòng | `GetByIdAsync(id)` → `DeleteAsync` (xóa cứng). | EF Delete (hard) | Không lọc IdDonVi khi tìm → xóa chéo đơn vị nếu biết Id. |
| 5 | Lấy chi tiết | (load form sửa) | `POST api/ThietLapTaiKhoanMacDinh/GetById?id=` | — | public | trước khi mở sửa | `GetByIdAsync(id)` → map DTO. | EF | Không lọc IdDonVi. |
| 6 | Áp dụng theo đơn vị | Nút "Áp dụng theo đơn vị" / "Reset chi nhánh" | `POST api/ThietLapTaiKhoanMacDinh/ResetTheoDonViAsync` | — | public | đang ở 1 chi nhánh | Lấy bộ mặc định cấp đơn vị (`IdChiNhanh` null/Empty). Chưa có → lỗi "Đơn vị chưa có thiết lập mặc định để kế thừa." Có → **xóa** thiết lập của `userLogin.IdChiNhanh` hiện tại rồi **clone** bộ đơn vị xuống chi nhánh đó. | EF Delete + Insert | Thao tác xóa-rồi-clone không transaction tường minh; nếu Insert lỗi sau khi Delete thành công → chi nhánh mất thiết lập cũ. |

## Quan hệ
- **Gọi sang:** chỉ repo nội bộ (`IClientHelper`/`ServiceSettings` inject nhưng không dùng trong các method hiện tại).
- **Dữ liệu (entities):** `ThietLapTaiKhoanMacDinh` (Id, IdDonVi, IdChiNhanh, LoaiDoiTuong, MaDoiTuong, TenDoiTuong, MaTaiKhoanMacDinh, GhiChu).
- **Ripple / đồng bộ:** không bắn message. Bảng tra cứu: `MaTaiKhoanMacDinh` tham chiếu `DmTaiKhoanKeToan.SoTaiKhoan`; dùng để gợi ý/điền TK mặc định theo đối tượng khi lập phiếu (không phải input trực tiếp của engine ButToan như TaiKhoanNgamDinh).

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Accounting/Accounting.Service/Generated/ThietLapTaiKhoanMacDinhService.cs`
  - `UpdateAsync` L89 / `DeleteAsync` L129 / `GetById` L139 dùng `GetByIdAsync` **không lọc `IdDonVi`** → thao tác chéo đơn vị nếu biết Id.
  - `ResetTheoDonViAsync` (L197-232): Delete chi nhánh hiện tại rồi Insert clone — không bọc transaction; rủi ro mất dữ liệu nếu Insert fail giữa chừng.
  - `TimTheoDieuKienAsync` keyword chỉ lọc `LoaiDoiTuong` (L166).
- Controller `Accounting/Accounting.API/Controllers/ThietLapTaiKhoanMacDinhController.cs`: mọi endpoint `[SBAuthorize()]` trống.
- Entity `Accounting/Accounting.DataContext/Entity/ThietLapTaiKhoanMacDinh.cs`.
