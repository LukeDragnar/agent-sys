---
controller: ChoNgoiController
module: cua-hang
service: Booking
man_hinh_ui: "Cửa hàng / Booking-POS — màn 'Quản lý chỗ ngồi / bàn' (list bàn trong khu vực; nút Thêm/Sửa/Xóa bàn; gán nhân viên phục vụ, giải phóng bàn). Bàn dùng cho sơ đồ POS, bi-a; có endpoint public cho màn đặt online."
muc_dich: "Quản lý chỗ ngồi (bàn) theo Đơn vị + Chi nhánh + Khu vực: thêm/sửa/xóa bàn, gán/giải phóng nhân viên phục vụ, lấy danh sách bàn cho dropdown/sơ đồ POS và màn đặt lịch online (AllowAnonymous)."
alias: ["chỗ ngồi", "bàn", "quản lý bàn", "gán nhân viên cho bàn", "giải phóng bàn", "dropdown bàn", "bàn bi-a"]
auth: mixed
state_machine: []
deprecated: false
entities: [ChoNgoi, KhuVuc, User]
stored_procs: []
depends: [Booking (ChoNgoi/KhuVuc), Redis/Cache (CHO_NGOI), Booking.CommonService (GetDonViById — check số bàn bi-a)]
cross_module_in: [POS/thu ngân — UpdateNhanVienChoNgoi/GiaiPhongChoNgoi/DropdownBanTheoDv dựng sơ đồ + gán phục vụ; màn đặt online — GetAllChoNgoiByKhuVucAllow/GetAllChoNgoiAllow (AllowAnonymous)]
cross_module_out: [Cache CHO_NGOI theo ĐơnVị+ChiNhánh (set khi Insert/Update, remove khi Delete; cache key có thêm IdKhuVuc+Id ở Insert/Update nhưng Delete remove key khác → xem known-issue)]
---

# ChoNgoiController — Quản lý chỗ ngồi / bàn (Booking / POS)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện (list bàn) | "Tìm kiếm" / load grid | POST `api/ChoNgoi/TimTheoDieuKien` | `ChoNgoi_TimTheoDieuKien` | public | màn list | List chỗ ngồi, sort theo `Sorts` hoặc theo `ThuTu` (null/<=0 → 9990); phân trang; map DTO | EF | **Không filter theo DonVi/ChiNhánh** — `ChoNgoiService.cs:50-52` query toàn bảng ChoNgoi (cross-tenant) trước khi phân trang |
| 2 | Thêm mới chỗ ngồi | "Thêm mới" (modal) | POST `api/ChoNgoi/ThemMoi` (override base) | `ChoNgoi_ThemMoi` | public | nút Thêm | Set IdDonVi/IdChiNhanh từ user; nếu ngành nghề BI_A → check tổng số bàn hiện có `>= TongSoUser` của đơn vị (gọi `GetDonViById`) thì chặn; insert (không chặn trùng tên ở nhánh này — xem note) | EF + HTTP Core (DonVi) | Nhánh `ThemMoi(model, user)` (Service:171) KHÔNG check trùng tên; còn `InsertAsync` base mới có check trùng → controller dùng `ThemMoi` nên bỏ qua chặn trùng |
| 3 | Cập nhật chỗ ngồi | "Sửa" (modal) | POST `api/ChoNgoi/CapNhatThongTin` (base) | `ChoNgoi_CapNhatThongTin` | public | nút Sửa | `UpdateAsync`: chặn trùng tên (cùng DonVi+ChiNhánh+KhuVuc, khác Id); MarkDirty Ten/GhiChu/SoKhach/SuDung/ThuTu/TenQuangCao; update + set cache CHO_NGOI (key có IdKhuVuc+Id) | EF | - |
| 4 | Xóa chỗ ngồi | "Xóa" (confirm) | POST `api/ChoNgoi/Xoa/{id}` (override base) | `ChoNgoi_Xoa` | public | nút Xóa | Lấy entity theo Id; null → "Không tìm thấy"; remove cache + `DeleteAsync` | EF | - |
| 5 | Gán nhân viên cho bàn | "Gán nhân viên" | POST `api/ChoNgoi/UpdateNhanVienChoNgoi` | `[SBAuthorize()]` rỗng (chỉ login) | public | thao tác POS | Validate `IdChoNgois` không rỗng + bàn tồn tại (DonVi+ChiNhánh); nếu set nhân viên mà 1 trong các bàn đã có nhân viên → chặn "đang có nhân viên làm. Không được cập nhật"; update `IdNhanVienPhucVu` | EF | - |
| 6 | Giải phóng bàn | "Giải phóng" | POST `api/ChoNgoi/GiaiPhongChoNgoi` | `ChoNgoi_GiaiPhongChoNgoi` | public | thao tác POS | Set `IdNhanVienPhucVu = null` cho các bàn trong `IdChoNgois` (theo DonVi+ChiNhánh); rỗng → trả true (no-op) | EF | - |
| 7 | Dropdown bàn theo đơn vị | dropdown chọn bàn | GET `api/ChoNgoi/DropdownBanTheoDv` | `[SBAuthorize()]` rỗng | public | - | List bàn `SuDung=true` theo DonVi+ChiNhánh | EF | Map sang DTO `rs` nhưng trả về `list` (entity gốc, không map) — Service:130-131 |

## Endpoint nội bộ (private / service-to-service)
- [public — `[SBAuthorize()]` rỗng] **`GetAllChoNgoiByKhuVuc/{idkhuvuc}`** (GET) / **`GetAllChoNgoi`** (GET) — bàn `SuDung=true` theo khu vực / toàn chi nhánh, đọc qua cache CHO_NGOI (miss → query DB + set cache), sort ThuTu, map DTO. POS gọi dựng dropdown bàn.
- [anonymous — `[AllowAnonymous]`] **`GetAllChoNgoiByKhuVucAllow`** (POST, body có IdDonVi/IdChiNhanh/IdKhuVuc) / **`GetAllChoNgoiAllow`** (POST, body IdDonVi/IdChiNhanh) — bàn `SuDung=true` theo đơn vị/chi nhánh truyền vào; nếu có `TenQuangCao` thì hiển thị tên đó thay `Ten`. Dùng cho màn đặt lịch online / view bi-a (không token).

## Quy tắc nghiệp vụ (gating — ThemMoi)
- Ngành nghề `BI_A`: trước khi thêm bàn → gọi `GetDonViById` lấy `TongSoUser`; nếu null đơn vị → "Không tìm thấy đơn vị"; `TongSoUser == null` → "Đơn vị chưa có setting tổng số user"; `countChoNgoi (theo IdDonVi) >= TongSoUser` → chặn "Bạn chỉ được tạo tối đa : N user". Ngành khác: bỏ qua check, insert luôn.

## Quan hệ
- **Gọi sang:** `Booking.CommonService.GetDonViById` → Core `GET /api/DonVi/GetDonViById/{id}` (lấy TongSoUser cho check bi-a). Cache `CacheKey.CHO_NGOI`.
- **Dữ liệu (entities):** `ChoNgoi` (Ten, GhiChu, SoKhach, SuDung, ThuTu, TenQuangCao, IdDevice, IdNhanVienPhucVu, IdKhuVuc, IdDonVi, IdChiNhanh), `KhuVuc`, `User`.
- **Ripple / đồng bộ:** Insert/Update set cache CHO_NGOI; Delete remove cache → POS/đặt online lấy lại danh sách bàn.

## Lỗi đã biết / lưu ý kỹ thuật
- **`TimTheoDieuKienAsync` không lọc tenant** — `ChoNgoiService.cs:50-63` query `_repos.TableWithNolock()` toàn bảng, không filter IdDonVi/IdChiNhanh → list bàn có thể lẫn dữ liệu đơn vị khác.
- **Insert/Update vs Delete dùng cache key KHÁC nhau** — Insert/Update set key 4 tham số `CHO_NGOI(DonVi,ChiNhánh,IdKhuVuc,Id)` (Service:72,88) trong khi `GetAllChoNgoi*` đọc key 2 tham số `CHO_NGOI(DonVi,ChiNhánh)` (Service:203,225,247,278) → cache list KHÔNG được làm mới khi thêm/sửa 1 bàn; chỉ Delete remove đúng key 4-tham-số (Service:165) nhưng cũng không khớp key list. Hệ quả: danh sách bàn cache có thể cũ tới khi key 2-tham-số được set lại ở chỗ khác (vd KhuVucService.LoadCache).
- **`ThemMoi(model, user)` bỏ qua chặn trùng tên** — `ChoNgoiService.cs:171-196` không gọi `InsertAsync` override (vốn có check trùng tên Service:66-69) mà gọi `_repos.InsertAsync` trực tiếp → tạo được bàn trùng tên trong cùng khu vực.
- **`DropdownBanTheoDv` trả entity thô** — `ChoNgoiService.cs:130-131` map ra `rs` (DTO) nhưng `ResultWithData(list)` trả entity ChoNgoi gốc, bỏ phí biến `rs`.
- **`count` bàn bi-a chỉ theo `IdDonVi`** — `ChoNgoiService.cs:187-188` đếm theo `_currentUser.DonViId` (không theo chi nhánh) → ngưỡng `TongSoUser` áp cho toàn đơn vị, không tách chi nhánh.
- Code thật: BE controller `Booking/Booking.API/Controllers/ChoNgoiController.cs`, service `Booking/Booking.Service/Generated/ChoNgoiService.cs`.
