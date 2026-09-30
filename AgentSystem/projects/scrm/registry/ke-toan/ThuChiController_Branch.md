---
controller: ThuChiController
module: ke-toan
service: Branch
man_hinh_ui: "Quỹ / Thu chi (service Branch): tạo/sửa/xóa phiếu thu - phiếu chi - chuyển tiền nội bộ, gửi yêu cầu chi sang ngân hàng, import phiếu thu/chi từ Excel, đồng bộ lại phiếu sang Kế toán. Khác với ThuChiController.md của service Accounting (chỉ đọc danh sách + xuất Excel)."
muc_dich: "Controller GHI thực sự của thu-chi phía Branch: lưu/xóa phiếu thu-chi, lưu phiếu chi/thu theo đối tượng (nháp → yêu cầu chi → hoàn thành), chuyển tiền nội bộ, gửi yêu cầu chi qua ngân hàng, import phiếu thu/chi Excel, đồng bộ lại sang Kế toán; cộng nhiều endpoint nội bộ để Order/Accounting tạo phiếu thu-chi theo đơn hàng/phiếu mua hàng và đồng bộ/rollback PTTT."
alias: ["thu chi", "phiếu thu", "phiếu chi", "chuyển tiền nội bộ", "yêu cầu chi", "gửi yêu cầu chi ngân hàng", "import phiếu thu", "import phiếu chi", "thu chi branch", "tạo phiếu thu chi đơn hàng"]
auth: mixed
state_machine: [Nhap, DangXuLy, DaXuLy]
deprecated: false
entities: [ThuChi, ThuChiDonHang, ThuChiBankLog, SyncThuChiKeToan]
stored_procs: []
depends: [Customer (xếp hạng/hoa hồng KH khi hoàn thành phiếu thu), WarehouseProduct/NhaCungCapGhiNo (trả nợ NCC), Accounting (lấy TK ngân hàng NCC + đồng bộ phiếu), Payment/ZengiBank (gửi yêu cầu chi/chuyển khoản), GemBox.Spreadsheet (import/template Excel), AutoMapper, Redis lock (chống double-submit), MassTransit IPublishEndpointDonHang]
cross_module_in: [Order/CommonService (TaoPhieuThuChiKhiThanhToanDonHang), Order/BranchService (TaoPhieuThuChiTienCoc, UpdatePttt_ThuChiByDonHang, RollbackPttt_ThuChiByDonHang), Accounting/CommonService (TaoPhieuChiMuaHang, HuyPhieuChiTheoListIdsPrivate, TaoPhieuThuBanHang), Payment (CapNhatTrangThaiThanhToan callback)]
cross_module_out: [Accounting (SyncThuChiKeToan outbox — đồng bộ phiếu thu-chi sang kế toán), Customer (cập nhật hạng/hoa hồng KH khi hoàn thành phiếu thu), WarehouseProduct/NhaCungCapGhiNo (trả nợ NCC), Payment (gửi yêu cầu chi/chuyển khoản)]
---

# ThuChiController (Branch) — Quỹ / Thu chi: phiếu thu, phiếu chi, chuyển tiền nội bộ

> Đây là controller **GHI** thu-chi phía service **Branch** (đừng nhầm với `ThuChiController.md` của Accounting — bản Accounting chỉ ĐỌC danh sách + xuất Excel + số dư ban đầu). `auth: mixed`: các thao tác UI dùng key hoặc `[SBAuthorize()]` trống (chỉ login); một loạt endpoint tạo phiếu theo đơn hàng / mua hàng / đồng bộ PTTT là `internal` (`[PrivateSBAuthorize]`). State phiếu: `Nhap → DangXuLy → DaXuLy`; khi đạt `DaXuLy` mới đẩy outbox `SyncThuChiKeToan` sang Accounting. Phần import dùng `ThuChiImportService.cs`, phần đồng bộ/rollback PTTT dùng `ThuChiUpdatePtttService.cs`, còn lại `ThuChiService.cs` (5145 dòng).

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lưu thu chi | Lưu | `POST api/ThuChi/Luu` | `ThuChi_Luu` | public | màn Quỹ/Thu chi | Validate + insert/update `ThuChi` + `ThuChiDonHang`. Nếu trạng thái=DaXuLy → đẩy outbox đồng bộ Accounting; loại=trả nợ NCC → gọi Warehouse `NhaCungCapGhiNo`. | **EF** `ThuChi`,`ThuChiDonHang` | `ThuChiService.cs:181` |
| 2 | Xóa thu chi | Xóa | `GET api/ThuChi/Xoa/{id}` | `ThuChi_Xoa` | public | mỗi phiếu | Soft-delete; **chặn** nếu phiếu tự tạo / từ phiếu mua hàng / chuyển nội bộ. Hard-delete chi tiết; gọi Accounting xóa bản ghi tương ứng. | **EF** | `ThuChiService.cs:1065` — chi tiết hard-delete. |
| 3 | Chi tiết phiếu | (mở chi tiết) | `GET api/ThuChi/ChiTiet/{id}` | `ThuChi_ChiTiet` | public | click phiếu | Trả DTO phiếu + list `ThuChiDonHang`. | **EF** | `ThuChiService.cs:680` |
| 4 | Danh sách thu chi | Lưới list | `POST api/ThuChi/DanhSach` | `ThuChi_DanhSach` | public | màn Quỹ | Lọc loại/quỹ/lý do/khoảng ngày + `IsDelete=false`; enrich KH qua Customer; tính tổng quan + phân trang. | **EF** | `ThuChiService.cs:755` |
| 5 | Đồng bộ lại sang kế toán | (nút đồng bộ lại) | `POST api/ThuChi/DongBoLaiSangKeToanTheoId/{id}` | `ThuChi_DongBoLai` | public | phiếu DaXuLy | Yêu cầu trạng thái=DaXuLy; tạo lại outbox `SyncThuChiKeToan` (Pending) → Job đẩy sang Accounting. | **EF** `SyncThuChiKeToan` | `ThuChiService.cs:5044`,`5067` |
| 6 | Lưu phiếu chi đối tượng (nháp) | Lưu phiếu chi | `POST api/ThuChi/SavePhieuChiDoiTuong` | `SBAuthorize()` (login) | public | màn phiếu chi | Redis lock; gen mã `PC*`; validate; insert/update phiếu + chi tiết. **Chặn** nếu từ chuyển nội bộ / đã DangXuLy/DaXuLy (trừ IsFromHoanThanh). | **EF** | `ThuChiService.cs:1857` |
| 7 | Lưu & yêu cầu chi | Yêu cầu chi | `POST api/ThuChi/SaveAndYeuCauChi` | `SBAuthorize()` (login) | public | phiếu chi | Gọi SavePhieuChiDoiTuong; tiền mặt/ngân hàng-offline → đánh DangXuLy; quỹ ngân hàng-online → queue GuiYeuCauChi. | **EF** | `ThuChiService.cs:2191` |
| 8 | Lưu phiếu chi hoàn thành | Hoàn thành | `POST api/ThuChi/SavePhieuChiHoanThanh` | `SBAuthorize()` (login) | public | phiếu chi | Lưu → tùy chọn gọi Warehouse trả nợ; loại GuiTienVaoNganHang sinh phiếu thu liên kết `PT_`; đánh DaXuLy + đồng bộ Accounting + socket. | **EF** + Warehouse | `ThuChiService.cs:2274` |
| 9 | Gửi yêu cầu chi (ngân hàng) | Gửi NH | `POST api/ThuChi/GuiYeuCauChi` | `SBAuthorize()` (login) | public | quỹ ngân hàng | Redis lock; chống gửi trùng (đọc `ThuChiBankLog`, chỉ gửi lại nếu data đổi); gọi Payment; insert `ThuChiBankLog`; đánh DangXuLy. | **EF** `ThuChiBankLog` + Payment | `ThuChiService.cs:4373` — no transaction xuyên service. |
| 10 | Lưu phiếu thu đối tượng (nháp) | Lưu phiếu thu | `POST api/ThuChi/SavePhieuThuDoiTuong` | `SBAuthorize()` (login) | public | màn phiếu thu | Redis lock; gen mã `PT*`; validate; insert/update. **Chặn** nếu có IdPhieuGoc / từ chuyển nội bộ / đã DangXuLy. | **EF** | `ThuChiService.cs:2792` — msg lỗi sai field L2827. |
| 11 | Lưu phiếu thu hoàn thành | Hoàn thành | `POST api/ThuChi/SavePhieuThuHoanThanh` | `SBAuthorize()` (login) | public | phiếu thu | Lưu → tùy chọn gọi Customer (hạng/hoa hồng KH); loại RutTienGuiNhapQuy sinh phiếu chi liên kết `PC_`; đánh DaXuLy + đồng bộ + socket. | **EF** + Customer | `ThuChiService.cs:3081` |
| 12 | Chuyển tiền nội bộ (nháp) | Lưu chuyển NB | `POST api/ThuChi/SavePhieuChuyenTienNoiBo` | `SBAuthorize()` (login) | public | màn chuyển NB | Validate TK/CN/số tiền; gen mã `CTNB*`; tạo 3 phiếu (chuyển + thu `PT_` + chi `PC_`) trong 1 transaction; verify TK qua Payment. | **EF** | `ThuChiService.cs:3520` |
| 13 | Hoàn thành chuyển nội bộ | Hoàn thành | `POST api/ThuChi/SaveHoanThanhPhieuNoiBo` | `SBAuthorize()` (login) | public | phiếu chuyển NB | Gọi SavePhieuChuyenTienNoiBo; đánh DaXuLy cả 3 phiếu + đồng bộ Accounting. | **EF** | `ThuChiService.cs:4154` |
| 14 | Xem chi tiết phiếu chi | (mở chi tiết) | `GET api/ThuChi/GetDetailPhieuChi?id=` | `SBAuthorize()` (login) | public | click phiếu | Trả `ReqThuChiDoiTuong` + mã phiếu liên kết (nếu ChuyenTienNoiBo lấy mã thu/chi liên kết; loại khác lấy mã phiếu chuyển). | **EF** | `ThuChiService.cs:4243`,`4336` |
| 15 | Tìm phiếu chi theo IdPhieuChiDonHang | — | `GET api/ThuChi/GetPhieuChiByIdPhieuChiDonHang/{idChiTiet}` | `SBAuthorize()` (login) | public | — | Tổng `SoTienDaThanhToan` từ phiếu liên kết theo trạng thái bank/xử lý. | **EF** | `ThuChiService.cs:4737` |
| 16 | Tìm Id theo mã đơn / Id phiếu bán hàng | — | `GET api/ThuChi/TimIdTheoMaDon/{maDon}` · `.../TimIdTheoIdPhieuBanHang/{id}` | `SBAuthorize()` (login) | public | — | Trả list Id `ThuChi` theo mã hóa đơn / Id phiếu bán hàng. | **EF** | `ThuChiService.cs:4961`,`5107` |
| 17 | Lấy DS thu chi theo Ids | — | `POST api/ThuChi/GetDanhSachThuChiByIds` | `SBAuthorize()` (login) | public | — | Lọc theo mảng Id, trả các field cơ bản (LoaiThuChi/LoaiQuy/KieuLoaiChiTiet). | **EF** | `ThuChiService.cs:4973` |
| 18 | Import phiếu thu (Excel) | Tải mẫu / Import / Hoàn tất | `GET .../DownloadTemplatePhieuThuAsync` · `POST .../ImportTemplatePhieuThu` (IFormFile) · `.../HoanTatImportPhieuThu` · `.../GetDanhSachPhieuThuImport` · `GET .../DeleteTmpPhieuThuImport` | `SBAuthorize()` (login) | public | màn import phiếu thu | Tải file mẫu `phieu-thu-tien.xlsx`; đọc Excel → validate → lưu bảng tạm; hoàn tất ghi bảng chính theo batch; list/xóa bảng tạm theo trạng thái. | GemBox + **EF** | `ThuChiImportService.cs` |
| 19 | Import phiếu chi (Excel) | Tải mẫu / Import / Hoàn tất | `GET .../DownloadTemplatePhieuChiAsync` · `POST .../ImportTemplatePhieuChi` (IFormFile) · `.../HoanTatImportPhieuChi` · `.../GetDanhSachPhieuChiImport` · `GET .../DeleteTmpPhieuChiImport` | `SBAuthorize()` (login) | public | màn import phiếu chi | Tương tự #18 cho phiếu chi (`phieu-chi-tien.xlsx`). | GemBox + **EF** | `ThuChiImportService.cs` |
| 20 | Cập nhật trạng thái thanh toán (callback) | — | `POST api/ThuChi/CapNhatTrangThaiThanhToan` | (none) `[AllowAnonymous]` | anonymous | callback Payment | Publish message `LogRequestChiHoThuChiDto` qua MassTransit (xử lý async ở consumer). | MassTransit | `ThuChiController.cs:341` — AllowAnonymous, không auth. |

## Endpoint nội bộ (private / service-to-service)
- **[internal] `POST .../TaoPhieuThuChiKhiThanhToanDonHang`** (`[PrivateSBAuthorize]`) — **Order** (`Order.Service/Generated/Common/CommonService.cs:399`) gọi khi thanh toán đơn hàng → batch tạo phiếu thu-chi từ đơn (có thể lấy TK NH NCC từ Accounting), trả Id phiếu của đơn hiện tại. `ThuChiService.cs:1455`.
- **[internal] `POST .../TaoPhieuThuChiTienCoc`** (`[PrivateSBAuthorize]`) — **Order** (`Order.Service/Generated/OtherService/BranchService.cs:245`) gọi tạo/cập nhật phiếu tiền cọc; số tiền ≤ 0 → xóa phiếu. `ThuChiService.cs:1692`.
- **[internal] `POST .../UpdatePttt_ThuChiByDonHang`** + **`.../RollbackPttt_ThuChiByDonHang`** (`[PrivateSBAuthorize]`) — **Order** (`BranchService.cs:265`,`284`; cũng có ở Order `AccountingService.cs:131`,`150`) gọi khi đổi PTTT đơn hàng: tái khớp phiếu thu-chi theo `IdLoaiQuy` (xóa/insert/update theo LoaiQuy), và rollback khi luồng lỗi. `ThuChiUpdatePtttService.cs:41` (UpdatePttt).
- **[internal] `POST .../TaoPhieuChiMuaHang`** + **`.../HuyPhieuChiTheoListIdsPrivate`** + **`.../TaoPhieuThuBanHang`** (`[PrivateSBAuthorize]`) — **Accounting** (`Accounting.Service/Generated/Common/CommonService.cs:569`,`578`,`647`) gọi từ phiếu mua hàng/bán hàng để tạo phiếu chi/thu, hủy phiếu chi liên kết (chặn nếu phiếu đã DaXuLy). `ThuChiService.cs:2602`,`2742`,`3405`.
- **[internal] `POST .../TaoPhieuLuong`**, **`.../TaoPhieuThuChiTraNoDonHang`**, **`GET .../GetThuChiByKhachHang/{id}`**, **`.../SavePhieuChiHoanThanh_NoAuthen`** (`[PrivateSBAuthorize]`) — tạo phiếu lương, phiếu trả nợ đơn hàng, tổng thu theo KH, hoàn thành phiếu chi không cần authen (service khác gọi). `ThuChiService.cs:1220`,`1295`,`1845`.
- **[note] `GET .../XoaTheoIdTransaction/{id}`** — key `ThuChi_Xoa` đã bị comment (dòng 71) → hiện chỉ yêu cầu login mặc định controller, soft-delete toàn bộ phiếu theo IdTransaction. `ThuChiService.cs:1398`.

## Quan hệ
- **Gọi sang:** Customer (hạng/hoa hồng KH khi hoàn thành phiếu thu), Warehouse `NhaCungCapGhiNo` (trả nợ NCC), Accounting (lấy TK NH NCC + GetCauHinh), Payment/ZengiBank (gửi yêu cầu chi/chuyển khoản).
- **Dữ liệu (entities):** `ThuChi` (phiếu, mang trạng thái Nhap/DangXuLy/DaXuLy), `ThuChiDonHang` (chi tiết theo đơn), `ThuChiBankLog` (audit gửi NH), `SyncThuChiKeToan` (outbox đồng bộ kế toán).
- **Ripple / đồng bộ:** Phiếu đạt DaXuLy → outbox đẩy sang Accounting. Phiếu chuyển nội bộ sinh kèm phiếu thu `PT_` + phiếu chi `PC_`. Callback Payment cập nhật trạng thái + đẩy đồng bộ.

## Luồng xuyên module
- **Order thanh toán đơn** → `TaoPhieuThuChiKhiThanhToanDonHang` → tạo phiếu thu-chi theo đơn → (DaXuLy) đẩy outbox `SyncThuChiKeToan` → Accounting.
- **Order đổi PTTT** → `UpdatePttt_ThuChiByDonHang` → so khớp DB↔request theo `IdLoaiQuy`: thiếu→xóa, thừa→insert (clone mẫu, gen `MaThuChi`), trùng→update SoTien.
- **Accounting phiếu mua hàng** → `TaoPhieuChiMuaHang` (tạo phiếu chi), hủy phiếu mua → `HuyPhieuChiTheoListIdsPrivate` (chặn nếu đã DaXuLy).

## Lỗi đã biết / lưu ý kỹ thuật
- `ThuChiController.cs:341` — `CapNhatTrangThaiThanhToan` `[AllowAnonymous]`: endpoint public hoàn toàn (chỉ publish message, không xử lý DB trực tiếp).
- `ThuChiController.cs:71` — `XoaTheoIdTransaction` đã comment key `ThuChi_Xoa` → chỉ còn auth mặc định controller.
- `ThuChiService.cs:2827` — message lỗi `SavePhieuThuDoiTuong` tham chiếu sai field (`MaDoiTuong` thay vì tên phiếu nguồn).
- `ThuChiService.cs:4336` — `GetDetailPhieuChi` dùng `IdDoiTac` thay vì `MaDoiTac` (cần xác nhận đúng field hiển thị mã đối tác).
- `ThuChiService.cs:3164,3815,3860` — nối chuỗi `MaThuChi` có thể ra `"PT_null"`/`"PC_null"` nếu không validate trước.
- `ThuChiService.cs:4373` (`GuiYeuCauChi`) — gọi Payment + ghi DB không cùng transaction xuyên service; Redis lock TTL 30s → rủi ro race nếu thao tác > 30s.
- `ThuChiService.cs:1065` (`Xoa`) — chi tiết phiếu là hard-delete (phiếu chính soft-delete).
- Import: methods nằm ở `ThuChiImportService.cs` (partial), không ở `ThuChiService.cs`.
