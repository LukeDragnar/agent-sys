---
controller: NhomNhanVienController
module: he-thong
service: Core
man_hinh_ui: "Đơn vị > Cơ cấu tổ chức > Nhóm nhân viên: lưới nhóm + form Thêm/Sửa/Xóa nhóm; nguồn dropdown nhóm sale/CSKH cho nhiều màn (giao việc, chia đơn, chat)"
muc_dich: "Quản lý nhóm nhân viên của đơn vị (sale/CSKH/...): CRUD nhóm, lấy nhóm theo đơn vị/role/chi nhánh, lấy nhân viên theo nhóm, khởi tạo nhóm mẫu theo ngành nghề. Là nguồn dữ liệu phân nhóm cho phân công sale/CSKH."
alias: ["nhóm nhân viên", "nhóm sale", "nhóm CSKH", "team nhân viên", "NhomNhanVien"]
auth: mixed
state_machine: []
deprecated: false
entities: [NhomNhanVien, UserNhomNhanVien]
stored_procs: []
depends: ["INhomNhanVienService", "IUserNhomNhanVienService", "SBBaseController<NhomNhanVien, NhomNhanVienDto>", "NganhNghe (init mẫu)"]
cross_module_in: ["FE quản lý nhóm gọi TimTheoDieuKien/ThemMoi-Nhom/CapNhap-Nhom/Xoa", "Màn giao việc/chia đơn/chat gọi GetListByDonVi/LayNhomNhanVienByRole/LayAllNhomCSKH/LayNhanVienByNhom"]
cross_module_out: ["InitStaffGroupSystem/InitStaffGroupOrg sinh nhóm mẫu theo ngành nghề cho đơn vị mới → ảnh hưởng cơ cấu phân nhóm sale/CSKH"]
---

# NhomNhanVienController — Quản lý nhóm nhân viên

Kế thừa `SBBaseController<NhomNhanVien, NhomNhanVienDto>` nhưng override hầu hết + thêm ~15 endpoint nghiệp vụ. Dùng 2 service: `INhomNhanVienService` (nhóm) và `IUserNhomNhanVienService` (nhân viên trong nhóm). Controller-level `[TienIch(TI_Chung)]`; nhiều endpoint chỉ `[SBAuthorize]` rỗng → auth = mixed. Bảng dưới giữ action có ý nghĩa UI; nhóm "Lay*ByRole" gom ở Endpoint nội bộ.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện | (load lưới) | POST `api/NhomNhanVien/TimTheoDieuKien` | `NhomNhanVien_TimTheoDieuKien` (DV-AD-TONG) | public | — | Lọc nhóm theo `NhomNhanVienFilterDto`. | EF | — |
| 2 | Xem chi tiết | (mở form) | GET `api/NhomNhanVien/TimTheoId/{id:Guid}` | — (login) | public | — | `_service.TimTheoIdAsnc(id)` trả nhóm + thành viên. | EF | — |
| 3 | Thêm mới nhóm | Thêm | POST `api/NhomNhanVien/ThemMoi-Nhom` | `NhomNhanVien_ThemMoi` (DV-AD-TONG) | public | — | `ThemMoiAsync(req)` tạo nhóm từ `NhomNhanVienCreateReq` (kèm danh sách user). | EF | Route khác CRUD chuẩn (`ThemMoi-Nhom` thay vì `ThemMoi`) |
| 4 | Cập nhật nhóm | Sửa | POST `api/NhomNhanVien/CapNhap-Nhom` | `NhomNhanVien_CapNhap` (DV-AD-TONG) | public | — | `CapNhapAsync(req)` cập nhật nhóm + thành viên. | EF | Tên route `CapNhap` (thiếu dấu, sai chính tả) → giữ nguyên để FE khớp |
| 5 | Xóa nhóm | Xóa | POST `api/NhomNhanVien/Xoa/{id:Guid}` | `NhomNhanVien_Xoa` (DV-AD-TONG) | public | — | `DeleteAsync(id)` xóa nhóm. | EF | — |
| 6 | Lấy nhóm theo đơn vị | (dropdown nhóm) | GET `api/NhomNhanVien/GetListByDonVi` | — (login) | public | — | Trả nhóm theo đơn vị của CurrentUser (cố ý mở cho mọi role, key đã comment dòng 99). | EF | — |
| 7 | Lấy nhóm CSKH+Sale theo chi nhánh | (dropdown) | GET `api/NhomNhanVien/GetLstNhomCSKHAndSaleByChiNhanh` | — (login) | public | — | Trả nhóm sale/CSKH theo chi nhánh CurrentUser. | EF | — |
| 8 | Lấy tất cả nhóm CSKH | (dropdown CSKH) | GET `api/NhomNhanVien/LayAllNhomCSKH` | — (login) | public | — | Trả toàn bộ nhóm CSKH của đơn vị. | EF | — |
| 9 | Lấy nhân viên theo nhóm | (load thành viên) | GET `api/NhomNhanVien/LayNhanVienByNhom/{idNhom}` (+ `LayNhanVienChatFBByNhom`, `LayNhanVienByNhomIgnoreTruongNhom`) | — (login) | public | — | `IUserNhomNhanVienService` trả nhân viên trong nhóm (biến thể: chat FB, bỏ trưởng nhóm). | EF | — |
| 10 | Khởi tạo nhóm mẫu | (init) | POST `api/NhomNhanVien/InitStaffGroupSystem`, `InitStaffGroupOrg` | — (login) | public | — | Sinh nhóm nhân viên mẫu theo ngành nghề (system) hoặc cho 1 đơn vị (org, body `{IdDonVi}`). | EF | Không gắn key → mọi user login init được dữ liệu mẫu |

## Endpoint nội bộ (private / service-to-service)
- [internal] **GET `LayNhomNhanVienByRole/{roleCode}`**, **`LayNhomNhanVienDonViByRole/{roleCode}`**, **`LayAllNhomNVDonViByRole/{roleCode}`**, **`LayAllNhomNVDonViByRoleForChiaSoCareDon/{roleCode}`** (`[SBAuthorize]` rỗng) — các màn giao việc/chia đơn/care gọi để lấy nhóm theo role (toàn đơn vị hoặc chi nhánh).
- [internal] **GET `GetAllSaleChiNhanh`** — lấy toàn bộ sale theo chi nhánh (phục vụ phân bổ sale).

## Quan hệ
- **Gọi sang:** INhomNhanVienService, IUserNhomNhanVienService (EF).
- **Dữ liệu (entities):** `NhomNhanVien`, `UserNhomNhanVien` (map user↔nhóm).
- **Ripple / đồng bộ:** Init* sinh nhóm mẫu theo ngành nghề; nhóm là đầu vào cho phân công sale/CSKH, chia đơn, chat.

## Lỗi đã biết / lưu ý kỹ thuật
- Route Thêm/Cập nhật không theo chuẩn CRUD (`ThemMoi-Nhom`/`CapNhap-Nhom`) — FE phải gọi đúng tên này, không phải `ThemMoi`/`CapNhatThongTin` kế thừa.
- Nhiều endpoint dropdown/init chỉ `[SBAuthorize]` rỗng hoặc không key → chỉ chặn ở mức "đã login", không gate role.
