---
controller: CommonController
module: nhan-su
service: Branch
man_hinh_ui: "nội bộ / dùng chung (service Branch): không phải 1 màn riêng — cung cấp dữ liệu nền cho nhiều màn (ngành nghề, trạng thái đặt lịch, ngày trong tuần, danh sách nhân viên theo điều kiện đặt lịch, template hoa hồng, lý do kế toán, loại thu chi). Một số endpoint do service khác (ChiaSoChat, Contact) gọi để lấy cache giờ làm việc."
muc_dich: "Controller helper dùng chung của Branch: lấy ngành nghề/trạng thái đặt lịch/ngày trong tuần, danh sách nhân viên rảnh theo ca đặt lịch, template & data hoa hồng theo nhóm nhân viên, khởi tạo/xóa master data khi đăng ký/xóa đơn vị, cache giờ làm việc nhân viên, lý do kế toán + loại thu chi từ cache."
alias: ["common branch", "ngành nghề", "trạng thái đặt lịch", "nhân viên theo điều kiện đặt lịch", "template hoa hồng", "khởi tạo master data", "cache giờ làm việc", "lý do kế toán", "loại thu chi"]
auth: mixed
state_machine: []
deprecated: false
entities: [HoaHong, ThuChiLyDo, CaLamViecNhanVien, CaLamViecNhanVienNangCao, User, ThuChiKieuLoai]
stored_procs: []
depends: [Core/NganhNghe/LayTatCa, Core/User (cache cơ cấu + user theo chi nhánh), Redis cache (giờ làm việc / cơ cấu / user), CommonCacheService + CommonRetrieveDataForCacheService]
cross_module_in: [ChiaSoChat/CommonService + Contact/CommonService (GetCache_GioLamViec_NhanVien_GroupBy_IdUser_ChiNhanh/{idDonVi}/{idChiNhanh})]
cross_module_out: []
---

# CommonController (Branch) — Helper dùng chung + cache giờ làm việc

> Controller dùng chung của service **Branch**. Class-level `[SBAuthorize]` (mặc định cần login), nhưng nhiều endpoint override: khởi tạo master data + lấy DS nhân viên đặt lịch là `[AllowAnonymous]`; endpoint cache giờ làm việc (không tham số) là `[PrivateSBAuthorize]` (internal). Phục vụ dropdown/master-data cho nhiều màn (đặt lịch, hoa hồng, kế toán) và cung cấp cache giờ làm việc cho service khác.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy ngành nghề | (combo) | `GET api/Common/RequestGetNganhNghe` | `[SBAuthorize]` class (login) | public | màn cấu hình đơn vị | Proxy gọi Core `NganhNghe/LayTatCa`. | Core API | — |
| 2 | Trạng thái đặt lịch / Ngày trong tuần | (combo) | `GET api/Common/GetTrangThaiDatLich` · `.../GetListNgayTrongTuan` | `[SBAuthorize]` class | public | màn đặt lịch | Convert enum `EnumDatLich_TrangThai` / `EnumDayOfWeek` thành list. | enum | — |
| 3 | DS nhân viên theo điều kiện đặt lịch | (chọn KTV/nhân viên) | `POST api/Common/LayDsNhanVienTheoDieuKien` | (none) `[AllowAnonymous]` | anonymous | màn đặt lịch (khách) | Lấy user theo chi nhánh (cache) + ca làm việc (cache, thường + nâng cao) theo ngày/thứ; group theo nhóm nhân viên; enrich tên nhóm từ cơ cấu tổ chức. | Redis cache + Core | `CommonController.cs:62` AllowAnonymous (booking công khai). |
| 4 | Template hoa hồng theo kiểu loại áp dụng | — | `POST api/Common/CurrentUser_LayTemplateMasterDataHoaHongTheoKieuLoaiApDung` | `[SBAuthorize]` class | public | màn cấu hình hoa hồng | Lấy `HoaHong` master (IdNhomNhanVien=Empty) theo DonVi + kiểu loại áp dụng. | **EF** `HoaHong` | — |
| 5 | Data hoa hồng theo nhóm nhân viên | — | `POST api/Common/CurrentUser_LayDataHoaHongTheoNhomNhanVien` | `[SBAuthorize]` class | public | màn cấu hình hoa hồng | Lấy `HoaHong` theo nhóm; nếu trống → clone từ master data + **insert** vào DB. | **EF** `HoaHong` | Auto-insert khi rỗng (side-effect ở endpoint "lấy"). `CommonService.cs:266`. |
| 6 | Lý do kế toán (default) | (combo lý do) | `GET api/Common/GetListLyDoKeToan` | `SBAuthorize()` (login) | public | màn kế toán/thu chi | Lấy `ThuChiLyDo` default (IdDonVi=Empty, có MaKeToan): Id/MaKeToan/TenLyDo. | **EF** `ThuChiLyDo` | — |
| 7 | Tất cả loại thu chi (cache) | (combo loại quỹ) | `GET api/Common/GetAllLoaiThuChiFromCache` | `SBAuthorize()` (login) | public | màn thu chi | Lấy `ThuChiKieuLoai` từ Redis (`Branch_ThuChiKieuLoai_KeyCache`), miss → DB rồi set cache. | **EF** `ThuChiKieuLoai` (cache) | — |

## Endpoint nội bộ (private / service-to-service)
- **[internal] `POST api/Common/GetCache_GioLamViec_NhanVien_GroupBy_IdUser_ChiNhanh`** (không tham số, `[PrivateSBAuthorize]`) — lấy cache giờ làm việc nhân viên group theo IdUser của **chi nhánh đang login** (CurrentUser); miss → retrieve + set cache hết ngày. `CommonService.cs:401`.
- **[public-cross] `POST api/Common/GetCache_GioLamViec_NhanVien_GroupBy_IdUser_ChiNhanh/{idDonVi}/{idChiNhanh}`** (chỉ `[SBAuthorize]` class) — bản theo tham số, được **ChiaSoChat** (`ChiaSoChat.Service/Generated/Common/CommonService.cs:85`) + **Contact** (`Contact.Service/Generated/Common/CommonService.cs:2195`) gọi để lấy cache giờ làm việc chéo service. `CommonService.cs:423`.

## Quy tắc nghiệp vụ (khởi tạo / xóa master data)
- **`POST api/Common/KhoiTaoDuLieuMacDinh`** (`[AllowAnonymous]`) — khởi tạo master data hoa hồng cho đơn vị khi đăng ký (`HoaHongService.KhoiTaoDuLieuMacDinhByDonViAsync`).
- **`POST api/Common/KhoiTaoMasterData`** (`[AllowAnonymous]`) — get-or-khởi-tạo master data hoa hồng dùng chung.
- **`POST api/Common/XoaDuLieuTheoDonVi`** (`[SBAuthorize]` class) — xóa master data hoa hồng khi xóa đơn vị.

## Quan hệ
- **Gọi sang:** Core `NganhNghe/LayTatCa`, Core `User/GetAndSetCoCauCache_TheoDonVi` + `GetAndSetUserCache_TheoChiNhanh` (cache cơ cấu/user); HoaHongService (khởi tạo/xóa master data); CommonCacheService/RetrieveDataForCache (Redis giờ làm việc).
- **Dữ liệu (entities):** `HoaHong` (template + theo nhóm), `ThuChiLyDo` (lý do kế toán default), `ThuChiKieuLoai` (cache loại thu chi), `User`/`CaLamViecNhanVien(+NangCao)` (ca làm việc).
- **Ripple / đồng bộ:** endpoint #5 auto-insert HoaHong khi nhóm chưa có cấu hình.

## Lỗi đã biết / lưu ý kỹ thuật
- `CommonController.cs:62` — `LayDsNhanVienTheoDieuKien` `[AllowAnonymous]` (đặt lịch công khai) — không auth; cẩn thận lộ DS nhân viên theo đơn vị.
- `CommonController.cs:93,106` — `KhoiTaoDuLieuMacDinh` + `KhoiTaoMasterData` `[AllowAnonymous]`: endpoint khởi tạo dữ liệu không auth (gọi từ luồng đăng ký).
- `CommonService.cs:266` (`CurrentUser_LayDataHoaHongTheoNhomNhanVien`) — endpoint "lấy" nhưng tự **insert** HoaHong khi rỗng (side-effect ở GET-style).
- `CommonController.cs:131` — bản endpoint cache theo `{idDonVi}/{idChiNhanh}` chỉ có `[SBAuthorize]` class (không `[PrivateSBAuthorize]`) dù được dùng cross-service → chỉ cần login bất kỳ là gọi được cache chi nhánh khác.
