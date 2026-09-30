---
controller: NhanSanPhamController
module: kho
service: WarehouseProduct
man_hinh_ui: "Kho > Quản lý nhãn sản phẩm (route warehouse-product/nhan-san-pham) — lưới nhãn (Tên / STT / Màu nền / Cập nhật) + inline edit + nút Thêm mới / Lưu tất cả / Xóa + popup sửa"
muc_dich: "Quản lý danh mục NHÃN SẢN PHẨM (tag gắn nhãn cho sản phẩm): tên nhãn, thứ tự hiển thị (STT), màu nền (backgroundColor) để phân loại/đánh dấu sản phẩm"
alias: ["nhãn sản phẩm", "nhan san pham", "tag sản phẩm", "nhãn sp", "gán nhãn", "label sản phẩm", "màu nhãn"]
auth: public
state_machine: []
deprecated: false
entities: [NhanSanPham]
stored_procs: []
depends: [Core (User/DonVi — scope theo đơn vị)]
cross_module_in: [SanPham (gán nhãn cho sản phẩm), grid sản phẩm hiển thị màu nền nhãn]
cross_module_out: [SanPham (nhãn được tham chiếu khi lọc/hiển thị sản phẩm)]
---

# NhanSanPhamController — Quản lý nhãn sản phẩm

> Controller QUẢN LÝ DANH MỤC NHÃN (tag) gắn cho sản phẩm: mỗi nhãn có `Ten`, `Stt` (thứ tự hiển thị), `BackgroundColor` (màu nền hiển thị trong lưới sản phẩm). Feature nhỏ — chỉ CRUD danh mục theo đơn vị, không có state machine, không ripple lớn. Màn hình hỗ trợ **2 cách sửa**: inline trong lưới (Thêm mới / Lưu / Lưu tất cả) hoặc popup sửa (GetById → CapNhat).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện (grid chính) | Lưới + ô "Tên nhãn" + nút Tìm kiếm | POST `TimTheoDieuKien` | `NhanSanPham_TimTheoDieuKien` | public | Mặc định màn hình | Lọc nhãn theo đơn vị user + `keyword` (tên) + `kieuLoai`; FE không phân trang server (`nzShowPagination=false`), trả `data` + `totalRecord` | EF | FE gửi `kieuLoai` trong filter nhưng UI list không có control chọn loại → luôn null |
| 2 | Thêm mới nhãn | Nút "Thêm mới" (thêm dòng inline) → "Lưu"; hoặc popup | POST `ThemMoiNhanSanPham` | `NhanSanPham_ThemMoiNhanSanPham` | public | Khi thêm dòng mới (chưa có `id`) | Validate `Ten` bắt buộc (FE chặn "Nhãn chưa nhập tên"); insert nhãn theo đơn vị; trả `id` mới về để gán vào dòng lưới | EF | FE phân biệt thêm/sửa bằng `id == null` (dòng mới chưa có id) |
| 3 | Cập nhật 1 nhãn | Nút "Lưu" trên dòng có id; hoặc popup sửa | POST `CapNhatThongTinNhanSanPham` | `NhanSanPham_CapNhatThongTinNhanSanPham` | public | Dòng `isEdit && id != null` | Cập nhật `Ten`/`Stt`/`BackgroundColor` của nhãn theo id; set người/ngày cập nhật (`nguoiCapNhatUserName`, `ngayCapNhat` hiển thị ở lưới) | EF | - |
| 4 | Lưu tất cả (batch) | Nút "Lưu tất cả" | POST `UpdateMany` | `NhanSanPham_UpdateMany` | public | Có ≥1 dòng `isEdit=true` | Nhận list dòng đang sửa, update hàng loạt trong 1 request; FE reload lưới sau khi thành công; nếu không có dòng nào sửa → FE cảnh báo, không gọi API | EF | FE chỉ gửi các dòng `isEdit=true`; dòng mới chưa save có thể lẫn trong batch |
| 5 | Lấy chi tiết theo ID | (mở popup sửa) | GET `GetById/{id}` | `NhanSanPham_GetById` | public | Khi mở popup sửa | Trả `Ten`, `BackgroundColor`, `Stt` của nhãn để patch vào form popup | EF | - |
| 6 | Lấy tất cả nhãn theo đơn vị | (dropdown / dùng nội bộ FE — gán nhãn cho SP) | GET `GetAllNhanSanPhamByDonVi` | `NhanSanPham_GetAllNhanSanPhamByDonVi` | public | Khi cần list nhãn | Trả toàn bộ nhãn của đơn vị (không phân trang) để build dropdown / hiển thị màu nền ở màn sản phẩm | EF | - |
| 7 | Xóa nhãn | Menu dropdown dòng → "Xóa" (confirm) | POST `XoaNhanSanPham/{id}` | `NhanSanPham_XoaNhanSanPham` | public | Dòng đã có `id` | Xóa nhãn theo id (xóa cứng hoặc soft tùy BE); FE confirm "Bạn có chắc chắn muốn xóa?" rồi reload lưới | EF | Dòng chưa save (`!id`) dùng `onDeleteSoft` chỉ splice khỏi mảng FE, KHÔNG gọi API |

## Quan hệ
- **Gọi sang:** Core (scope theo `DonViId`/user hiện tại — nhãn thuộc đơn vị).
- **Dữ liệu (entities):** `NhanSanPham` (Ten, Stt, BackgroundColor, NguoiCapNhat, NgayCapNhat). Danh mục đơn giản, không bảng quan hệ phụ.
- **Ripple / đồng bộ:** Không có consumer/event. Nhãn được **SanPham tham chiếu** (gán nhãn + hiển thị màu nền trong lưới sản phẩm); xóa nhãn có thể để lại tham chiếu mồ côi ở SP (BE chưa verify có chặn xóa khi đang dùng).

## Lỗi đã biết / lưu ý kỹ thuật
- **BE source chưa checkout trên máy** (`WarehouseProduct` chỉ có build artifact ở `scrm_workflow/DebugLaunchSettings`, không có `.cs` gốc) → bảng dựng từ FE ground truth: endpoint lấy từ `services/warehouseproduct/nhan-san-pham.service.ts` + `base.service.ts` (`TimTheoDieuKien`); permission key theo convention `NhanSanPham_<Method>` (CHƯA đối chiếu `[SBAuthorize]` thật trong BE).
- FE list (`nhan-san-pham.component.ts`) có 2 luồng tạo/sửa song song: inline-grid (onAddNew/onSave/onSaveAll) và popup (`nhan-san-pham-edit`) — cùng gọi `ThemMoiNhanSanPham`/`CapNhatThongTinNhanSanPham`, dễ lệch nếu BE đổi validate.
- `nhan-san-pham.component.ts:91-99` — `onAddNew` chèn dòng mới lên đầu lưới với `stt = gridData.length + 1`; `sortAfterChangeNumber()` sort lại theo `stt` sau 500ms debounce (chỉ ở FE, không persist thứ tự lên server cho tới khi Lưu).
- `filter.kieuLoai` truyền lên `TimTheoDieuKien` nhưng màn list không render control chọn loại → luôn null (có thể là tàn dư từ template controller khác).
