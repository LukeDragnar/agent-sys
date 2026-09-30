---
controller: KhachHangDanhGiaController
module: khach-hang
service: Customer
man_hinh_ui: "Customer/danh-gia-chi-tiet-sa (danhgia-chitiet-sa — cấu hình tiêu chí đánh giá cho SA) · danh-gia-chi-tiet-unit (danhgia-chitiet-unit — cấu hình/khởi tạo tiêu chí cho đơn vị) · phan-hoi-dich-vu (danh sách phản hồi dịch vụ + export) · trang công khai KH chấm điểm (danhgia-khachhang / danhgia-thanhcong — AllowAnonymous qua short link)"
muc_dich: "Quản lý đánh giá dịch vụ của khách hàng: tạo link đánh giá (short link), KH chấm điểm hài lòng theo đơn/nhân viên, lưu phản hồi, list/export phản hồi cho đơn vị, và cấu hình tiêu chí đánh giá chi tiết (SA/Unit)"
alias: [KhachHangDanhGia, DanhGiaKhachHang, "Đánh giá khách hàng", "Phản hồi dịch vụ", "Đánh giá dịch vụ"]
auth: mixed
state_machine: []
deprecated: false
entities: [KhachHangDanhGia, KhachHangDanhGiaDetail, KhachHangDanhGiaChildren]
stored_procs: []
depends: [IKhachHangDanhGiaService, "Order (DonHang: GetThongTinDonHang / GetChiTietDonTheoId / LayThongTinCoBanById)", "OrderLogistic (DonHangLogistic: LayThongTinCoBanById)", "Booking (ShortLink: CreateCustomAsync / GetShortLinkByHashCode / TongSoKhachNhanLinkDanhGia)", "Core (ChiNhanh TimTheoIdPrivate, DonVi)", BaseDanhMucController, "RabbitMQ (GuiThongBaoInfo)", "ILogTuongTacKhachHangService"]
cross_module_in: ["Booking/ShortLink (KH bấm link đánh giá → SaveDanhGiaKhachHang)", "Order/OrderLogistic (đơn hàng cung cấp chi tiết dịch vụ + nhân viên)"]
cross_module_out: ["KhachHang (ghi LichSuTuongTac kiểu DanhGia)", "Notification (publish GuiThongBaoInfo khi KH đánh giá xong)"]
---

# KhachHangDanhGia — Đánh giá dịch vụ khách hàng

> Controller `mixed`: action cấu hình/list dùng `[SBAuthorize("KEY",...)]`, còn các endpoint KH-facing (qua short link, không đăng nhập) là `[AllowAnonymous]`.
> Kế thừa `BaseDanhMucController<KhachHangDanhGia, KhachHangDanhGiaDto>` → có sẵn `TimTheoId/{id}`, `LayTatCa`, `ThemMoi`, `CapNhatThongTin` (CRUD danh mục mặc định, `[SBAuthorize]` controller-level).

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tạo link đánh giá cho đơn | (gọi khi gửi đánh giá) | `POST InsertDanhGia` | KhachHangDanhGia_InsertDanhGia (DV-AD-TONG/CN/MCN, DV-QLDV, DV-NV) | public | đơn hàng offline | Gọi Booking `ShortLink/CreateCustomAsync` với `LoaiDoiTuong=DonHangOffline` → trả `TxtShortLink`; tạo link lỗi → "Không tạo được link đánh giá!" | EF + HTTP Booking | Logic cũ (tự gen MaDanhGia + insert) đã **comment**, hiện chỉ tạo short link |
| 2 | Lấy thông tin đơn theo mã đánh giá | (trang KH chấm điểm) | `GET GetThongTinDonHangByMa/{maDanhGia}` | — | anonymous | KH mở link | Tìm `KhachHangDanhGia` theo MaDanhGia → gọi Order `GetChiTietDonTheoId` → ghép chi tiết đánh giá + dropdown tiêu chí; không thấy → "Mã đánh giá không tồn tại." | EF + HTTP Order | — |
| 3 | Lấy/khởi tạo đánh giá theo IdDonHang | (trang KH chấm điểm) | `POST GetThongTinDonHangIdDonHang` | — | anonymous | có IdDonHang+KieuLoai+IdDonVi+IdChiNhanh | Nếu chưa có bản đánh giá → **tự tạo** (gen MaDanhGia unique, lấy info đơn Order/OrderLogistic, tên chi nhánh private) rồi insert; KieuLoai=Order → ghép chi tiết dịch vụ, ngược lại trả Data rỗng | EF + HTTP Order/OrderLogistic/Core | Thiếu **đủ cả 4** field mới báo lỗi (điều kiện AND) — payload thiếu 1 field vẫn lọt |
| 4 | KH gửi điểm đánh giá | Nút "Gửi đánh giá" (trang công khai) | `POST SaveDanhGiaKhachHang` | — | anonymous | `ValueDanhGiaChung > 0` | Tra short link theo MaDanhGia → tìm bản đánh giá theo LoaiDoiTuong (MaDanhGia/DonHangOffline/DonHangOnline); set MucDoHaiLong + ghi/ cập nhật chi tiết theo từng NV trong transaction; ghi LichSuTuongTac + **publish thông báo**; `ValueDanhGiaChung<=0` → "Vui lòng chọn đánh giá." | EF (transaction) + HTTP Booking | Tiêu chí "đã đánh giá rồi thì chặn" bị **comment** → KH gửi lại đè điểm cũ |
| 5 | Kiểm tra đã đánh giá chưa | (trang KH — chặn double) | `GET IsHaveEvaluated/{maDanhGia}` | — | anonymous | — | Trả bool đã đánh giá hay chưa theo mã | EF | — |
| 6 | Danh sách phản hồi dịch vụ | Lưới "Phản hồi dịch vụ" (phan-hoi-dich-vu) | `POST GetListPhanHoiDichVu` | KhachHangDanhGia_GetListPhanHoiDichVu (DV-AD-TONG/CN/MCN, DV-QLDV) | public | — | Lọc theo chi nhánh (non-admin ép `IdChiNhanh` = của user), mức hài lòng, ngày, keyword; gộp chi tiết NV + SĐT KH; trả thống kê Tổng/RấtHàiLòng/HàiLòng/ChưaHàiLòng + tổng số link gửi (Booking) | EF + HTTP Booking | Đếm thống kê và list dùng 2 query riêng → nguy cơ lệch nếu filter khác; phân trang in-memory (`Skip/Take` trên list đã `ToList`) |
| 7 | Export Excel phản hồi dịch vụ | Nút "Xuất Excel" | `POST ExportExcelPhanHoiDichVu` | KhachHangDanhGia_ExportExcelPhanHoiDichVu (DV-AD-TONG/CN/MCN, DV-QLDV) | public | — | Reuse #6 (ép PageSize=5000) → dựng GemBox workbook (bảng tổng hợp + bảng chi tiết) → file `PhanHoiDichVu.xlsx`; lỗi/no-data → **HTTP 500** (`Code=EXPORT_ERROR`) | EF + GemBox | Trả **HTTP raw 500** (không qua ResponseResult); cap cứng 5000 dòng — vượt sẽ thiếu |
| 8 | Cập nhật trạng thái xử lý phản hồi | Nút "Đánh dấu đã xử lý" | `POST UpdatePhanHoiDichVu` | `[SBAuthorize()]` (key gốc bị comment) | public | — | `UpdatePhanHoiDichVu(model)` (set IsProcessed) | EF | Quyền **nới lỏng**: key `KhachHangDanhGia_UpdatePhanHoiDichVu` bị comment, chỉ còn `[SBAuthorize()]` |
| 9 | List tiêu chí đánh giá chi tiết (Unit) | Lưới (danhgia-chitiet-unit) | `POST TimTheoDieuKienDsDetailForUnit` | KhachHangDanhGia_TimTheoDieuKienDsDetailForUnit (DV-AD-TONG) | public | chỉ AD-TONG | List tiêu chí đánh giá theo điều kiện, scope `CurrentUser` | EF | Toàn bộ nhóm cấu hình chi tiết Unit chỉ mở cho **DV-AD-TONG** |
| 10 | Thêm tiêu chí chi tiết (Unit) | Nút "Thêm" (danhgia-chitiet-unit-edit) | `POST InsertKhDanhGiaDetailForUnit` | KhachHangDanhGia_InsertKhDanhGiaDetailForUnit (DV-AD-TONG) | public | chỉ AD-TONG | Insert `KhachHangDanhGiaChildren` (tiêu chí) | EF | — |
| 11 | Sửa tiêu chí chi tiết (Unit) | Nút "Lưu" | `POST UpdateKhDanhGiaDetailForUnit` | KhachHangDanhGia_UpdateKhDanhGiaDetailForUnit (DV-AD-TONG) | public | chỉ AD-TONG | Update tiêu chí | EF | — |
| 12 | Xem chi tiết 1 tiêu chí (Unit) | (mở chi tiết) | `GET GetKhDanhGiaChiTietForUnit/{id:Guid}` | KhachHangDanhGia_GetKhDanhGiaChiTietForUnit (DV-AD-TONG) | public | chỉ AD-TONG | `GetKhDanhGiaChiTiet(id)` | EF | — |
| 13 | Xóa tiêu chí chi tiết (Unit) | Nút "Xóa" | `POST DeleteKhDanhGiaChiTietForUnit/{id:Guid}` | KhachHangDanhGia_DeleteKhDanhGiaChiTietForUnit (DV-AD-TONG) | public | chỉ AD-TONG | `DeleteKhDanhGiaChiTiet(id)` | EF | — |
| 14 | Khởi tạo data tiêu chí cho đơn vị | Nút "Khởi tạo" (danhgia-chitiet-unit-init) | `POST InitDataForUnit` | KhachHangDanhGia_InitDataForUnit (DV-AD-TONG) | public | chỉ AD-TONG | Seed bộ tiêu chí mặc định cho đơn vị | EF | — |
| 15 | Dropdown tiêu chí theo kiểu loại | (dropdown trên trang chấm điểm) | `POST GetDropDownListTheoKieuLoai` | — | anonymous | — | Trả danh sách tiêu chí theo KieuLoai để KH chọn | EF | — |

> **Lưu ý:** Code controller chỉ thấy nhóm `...ForUnit`. Nhóm `...ForSA` tồn tại trong service interface (TimTheoDieuKienDsDetailForSA / InsertKhDanhGiaDetailForSA / UpdateKhDanhGiaDetailForSA) và có UI `danhgia-chitiet-sa` nhưng **không có action trong controller này** → có thể nằm ở controller/endpoint khác (cần xác nhận).

## Quy tắc nghiệp vụ (scoring / mức hài lòng)

> Thang điểm `MucDoHaiLong` (enum `EnumMucDoHaiLong`), suy ra từ mapping export & validation:
> - **1 = Rất hài lòng**
> - **2 = Hài lòng**
> - **3 = Chưa hài lòng** (label export là "Chưa hài lòng")

Luồng tính điểm trong `SaveDanhGiaKhachHang`:
1. `ValueDanhGiaChung` (điểm tổng KH chọn) **phải > 0** → nếu null/<=0 → trả lỗi "Vui lòng chọn đánh giá.".
2. Set `danhGia.MucDoHaiLong = ValueDanhGiaChung` (điểm parent).
3. Với mỗi nhân viên trong từng dịch vụ:
   - Nếu NV **chưa được chấm riêng** (`DanhGiaNv` null/<=0) → **kế thừa điểm tổng** `ValueDanhGiaChung` cho chi tiết.
   - Nếu NV **được chấm riêng** (`DanhGiaNv > 0`) → dùng đúng điểm riêng đó (đè điểm kế thừa).
4. Nếu đã có bản chi tiết trong DB (`lstDgChiTietDb > 0`) → **update** (chỉ đổi điểm khi `DanhGiaNv > 0`) + cập nhật phản hồi; ngược lại → **insert** mới.
5. Thống kê (GetListPhanHoiDichVu / export): đếm theo từng mức (RatHaiLong/HaiLong/KhongHaiLong); `TongDanhGia = tổng 3 mức`.

> **DanhGiaDichVu (service, không expose qua controller này):** chỉ chấp nhận `MucDoHaiLong` = RatHaiLong hoặc HaiLong (các mức BinhThuong/KhongHaiLong/RatKhongHaiLong bị comment) → nếu sau này mở lại endpoint sẽ chặn điểm thấp (cần xác nhận quy tắc mong muốn).

## Quan hệ
- **Gọi sang:** Order `DonHang/GetThongTinDonHang`, `GetChiTietDonTheoId`, `LayThongTinCoBanById` · OrderLogistic `DonHangLogistic/LayThongTinCoBanById` · Booking `ShortLink/CreateCustomAsync`, `GetShortLinkByHashCode`, `TongSoKhachNhanLinkDanhGia` · Core `ChiNhanh/TimTheoIdPrivate` + DonVi (redis cache + CommonService).
- **Dữ liệu (entities):** `KhachHangDanhGia` (parent), `KhachHangDanhGiaDetail` (điểm theo NV/dịch vụ), `KhachHangDanhGiaChildren` (tiêu chí cấu hình).
- **Ripple/đồng bộ:**
  - Ghi `KhachHangLichSuTuongTac` kiểu `DanhGia` cho KH (qua `ILogTuongTacKhachHangService`).
  - **Publish `GuiThongBaoInfo`** (RabbitMQ) khi KH đánh giá xong → thông báo về đơn vị/chi nhánh (`MauThongBaoCode.KhachHangDanhGiaDv`).

## Luồng xuyên module (đánh giá đầu-cuối)
1. Nhân viên/đơn vị gửi link đánh giá → `InsertDanhGia` tạo short link (Booking) gắn `DonHangOffline`.
2. KH mở link công khai → `GetThongTinDonHangIdDonHang` / `GetThongTinDonHangByMa` lấy chi tiết đơn (Order/OrderLogistic) + dropdown tiêu chí.
3. KH chấm điểm → `SaveDanhGiaKhachHang`: lưu điểm parent + từng NV (transaction) → ghi LichSuTuongTac KH → **publish thông báo** về chi nhánh.
4. Đơn vị xem `GetListPhanHoiDichVu` / export Excel → đánh dấu `UpdatePhanHoiDichVu` (đã xử lý).

## Lỗi đã biết / lưu ý kỹ thuật
- `KhachHangDanhGiaController.cs:99-106` (`UpdatePhanHoiDichVu`) — key quyền gốc bị comment, chỉ còn `[SBAuthorize()]` → ai đăng nhập cũng cập nhật được trạng thái phản hồi.
- `KhachHangDanhGiaController.cs:82-97` (`ExportExcelPhanHoiDichVu`) — trả **HTTP raw 500** (`Code=EXPORT_ERROR`) khi lỗi/no-data, không theo ResponseResult; FE phải bắt riêng.
- `KhachHangDanhGiaService.cs:938-943` (`ExportExcelPhanHoiDichVu`) — cap cứng `PageSize=5000`; dữ liệu lớn hơn sẽ bị cắt khi export.
- `KhachHangDanhGiaService.cs:592-595` (`SaveDanhGiaKhachHang`) — check "đã đánh giá rồi thì chặn" bị **comment** → KH có thể gửi lại đè điểm cũ.
- `KhachHangDanhGiaService.cs:366-369` (`GetThongTinDonHangIdDonHang`) — điều kiện thiếu-thông-tin dùng **AND** cả 4 field → thiếu 1 field không bị chặn.
- `KhachHangDanhGiaService.cs:781-915` (`GetListPhanHoiDichVu`) — list (`ToListAsync` rồi `Skip/Take`) và thống kê là **2 query tách biệt**, phân trang in-memory; filter lệch nhỏ giữa 2 nhánh dễ gây số liệu không khớp.
- Nhóm `...ForSA` (service interface có, UI `danhgia-chitiet-sa` có) **không có action trong controller này** → endpoint SA nằm nơi khác (cần xác nhận).
