---
controller: AIUrlMapController
module: he-thong
service: SandBoxAI
man_hinh_ui: "Cấu hình AI (SandBox) > Quản lý URL map (danh sách + thêm/sửa/xóa)"
muc_dich: "Quản lý CRUD bảng ánh xạ URL (mã/tên/url/mô tả) dùng cho cấu hình AI trong SandBox — danh mục dùng chung, không scope theo đơn vị."
alias: ["AIUrlMap", "url map AI", "ánh xạ url", "cấu hình url SandBox"]
auth: public
state_machine: []
deprecated: false
entities: [AIUrlMap]
stored_procs: []
depends: ["SandBoxAIDataContext (EF Core)", "AutoMapper"]
cross_module_in: ["FE màn cấu hình AI SandBox → CRUD"]
cross_module_out: []
---

# AIUrlMapController — Quản lý ánh xạ URL cho AI

CRUD chuẩn bảng `AIUrlMap` (mã, tên, url, mô tả). Khác `AIDonViPrompt`: **không scope theo `IdDonVi`** — đây là danh mục dùng chung (entity không có cột IdDonVi), bất kỳ user login nào cũng đọc/sửa/xóa toàn bộ. Method `TimTheoId` (xem chi tiết) thậm chí **không gắn `[SBAuthorize]`** → anonymous gọi được. Các method ghi gắn `[SBAuthorize]` (login). Permission key chỉ ở comment.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Xem chi tiết | (click 1 dòng) | GET `api/AIUrlMap/TimTheoId/{id?}` | — | anonymous | — | GetById trả về thẳng (không check đơn vị, không check login). | EF `AIUrlMap` | **Thiếu `[SBAuthorize]`** → endpoint public hoàn toàn |
| 2 | Thêm mới | "Thêm mới" | POST `api/AIUrlMap/ThemMoi` | (gợi ý: AIUrlMap_ThemMoi) | public (login) | — | Map DTO→entity, Insert (không gán IdDonVi). ModelState sai → lỗi validate. | EF insert | Permission key chỉ ở comment |
| 3 | Cập nhật | "Lưu" | POST `api/AIUrlMap/CapNhatThongTin` | (gợi ý: AIUrlMap_CapNhatThongTin) | public (login) | — | GetById theo Id; null → NotFound; ngược lại Update. | EF update | Không kiểm tra đơn vị (danh mục dùng chung) |
| 4 | Xóa | "Xóa" | POST `api/AIUrlMap/Xoa/{id}` | (gợi ý: AIUrlMap_Xoa) | public (login) | — | GetById; null → NotFound; ngược lại Delete (soft). | EF delete | Bất kỳ user login nào xóa được mọi bản ghi |
| 5 | Tìm theo điều kiện (phân trang) | Lưới danh sách | POST `api/AIUrlMap/TimTheoDieuKien` | — | public (login) | Mở màn hình | Lọc/sort/phân trang theo `AIUrlMapFilterDto` (không scope đơn vị), trả danh sách + total. | EF query | — |

## Quan hệ
- **Gọi sang:** chỉ EF nội bộ (SandBoxAIDataContext).
- **Dữ liệu (entities):** `AIUrlMap` (Ma, Ten, Url, MoTa) — kế thừa BaseEntity. **Không có IdDonVi** → danh mục toàn hệ thống.
- **Ripple / đồng bộ:** không có cross-service; bảng map URL được tính năng AI trong SandBox tra cứu.

## Lỗi đã biết / lưu ý kỹ thuật
- `AIUrlMapController.cs:32-36` `TimTheoIdAsync` — **không có `[SBAuthorize]`** (khác hẳn AIDonViPrompt) → đọc chi tiết không cần đăng nhập.
- Toàn bộ controller không scope `IdDonVi` (entity không có cột này) → mọi đơn vị thấy/sửa chung 1 danh mục; cân nhắc nếu dữ liệu cần tách theo đơn vị.
- Permission key chỉ nằm trong comment; runtime các method ghi chỉ yêu cầu login.
