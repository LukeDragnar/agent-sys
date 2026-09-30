---
controller: VideoController
module: he-thong
service: GoogleDrive
man_hinh_ui: "nội bộ/đính kèm — quản lý video upload lên Google Drive (thêm/sửa/xóa/xem theo đơn hàng); không có menu quản trị riêng độc lập"
muc_dich: "CRUD metadata video (mã đơn, tiêu đề, đuôi file, thumbnail, thời lượng, link video) lưu cùng tích hợp Google Drive; scope theo đơn vị/chi nhánh của user."
alias: ["Video", "video Google Drive", "quản lý video", "upload video"]
auth: public
state_machine: []
deprecated: false
entities: [Video]
stored_procs: []
depends: ["GoogleDrive DataContext (EF Core)", "Google Drive API (lưu file/link)", "AutoMapper"]
cross_module_in: ["FE màn đính kèm/đơn hàng → CRUD video"]
cross_module_out: ["Google Drive (lưu file vật lý + LinkVideo)"]
---

# VideoController — Quản lý video (Google Drive)

CRUD metadata `Video` (MaDon, TieuDe, DuoiFile, ThumbNail, ThoiLuongVideo, LinkVideo, IdChiNhanh, IdDonVi). Controller-level `[SBAuthorize()]` (cần login). Xóa **scope theo đơn vị** (`IdDonVi != CurrentUser.DonViId` → NotFound); các thao tác còn lại nhận `CurrentUser` để service tự scope. Mọi action mặc định `virtual` (cho phép override). Không có permission key cụ thể.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Thêm mới | "Thêm video" | POST `api/Video/ThemMoi` | — | public (login) | — | `ThemMoiAsync(VideoCreateDto, CurrentUser)` — tạo bản ghi video (gán đơn vị/chi nhánh từ CurrentUser trong service). | EF insert + Google Drive | — |
| 2 | Xem chi tiết | (click 1 video) | GET `api/Video/TimTheoId/{id}` | — | public (login) | — | `ViewChiTietVideo(id, CurrentUser)` — trả chi tiết video. | EF `Video` | — |
| 3 | Tìm theo điều kiện (phân trang) | Lưới/danh sách video | POST `api/Video/TimTheoDieuKienAsync` | — | public (login) | Mở màn hình | `TimTheoDieuKienAsync(VideoFilterDto, CurrentUser)` — lọc/sort/phân trang theo scope user. | EF query | Tên endpoint kèm hậu tố `Async` (khác convention các controller khác) |
| 4 | Cập nhật | "Lưu" | POST `api/Video/CapNhatThongTin` | — | public (login) | — | `CapNhatThongTin(VideoDto, CurrentUser)` — cập nhật metadata video. | EF update | — |
| 5 | Xóa | "Xóa" | POST `api/Video/Xoa/{id}` | — | public (login) | — | GetById; null **hoặc** `IdDonVi != CurrentUser.DonViId` → NotFound; ngược lại Delete (soft). | EF delete | Chỉ xóa metadata; file trên Google Drive xử lý ở service (kiểm tra dọn file) |

## Quan hệ
- **Gọi sang:** Google Drive API (upload/lấy link — thực thi trong `IVideoService`).
- **Dữ liệu (entities):** `Video` (MaDon, TieuDe, DuoiFile, ThumbNail, ThoiLuongVideo, LinkVideo, IdChiNhanh, IdDonVi).
- **Ripple / đồng bộ:** file lưu trên Google Drive; metadata + LinkVideo lưu DB cục bộ.

## Lỗi đã biết / lưu ý kỹ thuật
- `VideoController.cs:35` — endpoint là `TimTheoDieuKienAsync` (giữ hậu tố Async trong route, khác các controller khác dùng `TimTheoDieuKien`) → FE phải gọi đúng path.
- `VideoController.cs:47-55` `XoaTheoId` — chỉ kiểm tra `IdDonVi`, không kiểm `IdChiNhanh`; logic xóa file vật lý trên Drive nằm trong service (cần verify không để file rác).
