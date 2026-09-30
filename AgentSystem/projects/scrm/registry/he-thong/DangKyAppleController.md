---
controller: DangKyDatLichController
module: he-thong
service: Core
man_hinh_ui: "nội bộ — không có UI rõ ràng (giả lập đặt lịch cho mobile / đăng ký Apple)"
muc_dich: "CRUD chuẩn trên entity DangKyApple (giả lập đặt lịch cho mobile) + tìm theo điều kiện có filter/grid."
alias: ["Đăng ký Apple", "DangKyApple", "đặt lịch mobile", "DangKyDatLich", "giả lập đặt lịch"]
auth: public
state_machine: []
deprecated: false
entities: [DangKyApple]
stored_procs: []
depends: ["IDangKyAppleService", "SBBaseController<DangKyApple, DangKyAppleDto>"]
cross_module_in: ["[mobile] app gọi giả lập đặt lịch / đăng ký Apple qua CRUD + TimTheoDieuKien"]
cross_module_out: []
---

# DangKyDatLichController — Giả lập đặt lịch cho mobile (DangKyApple)

> ⚠️ **Tên file vs tên class**: file `DangKyAppleController.cs` nhưng class thật là **`DangKyDatLichController`** (`partial`), route thật là **`api/DangKyDatLich`** (không phải `api/DangKyApple`). Node giữ `controller: DangKyDatLichController` theo code thật; đặt tên file `DangKyAppleController.md` theo yêu cầu batch.

Kế thừa `SBBaseController<DangKyApple, DangKyAppleDto>` → có sẵn CRUD chuẩn `[SBAuthorize]`. Controller chỉ thêm 1 action riêng `TimTheoDieuKien` (filter/grid). Toàn bộ nghiệp vụ qua `IDangKyAppleService`. Vì là `partial`, có thể có phần khác của class ở file/khác (cần xác nhận nếu mở rộng).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | CRUD chuẩn (kế thừa SBBaseController) | (chuẩn) | GET `api/DangKyDatLich/TimTheoId/{id:Guid}`, GET `LayTatCa`, POST `ThemMoi`, POST `CapNhatThongTin`, POST `Xoa/{id:Guid}` | — (chỉ `[SBAuthorize]` controller-level) | public | — | Insert/Update/Delete/Get qua `IBaseService<DangKyApple>`; map DangKyApple ↔ DangKyAppleDto. | EF qua service | — |
| 2 | Tìm theo điều kiện | (lưới/tìm kiếm) | POST `api/DangKyDatLich/TimTheoDieuKien` | — (`[SBAuthorize]`) | public | — | Nhận `DangKyAppleFilterDto`, gọi `service.TimTheoDieuKienAsync`, trả danh sách theo filter/grid. | EF qua service | — |

## Quan hệ
- **Gọi sang:** `IDangKyAppleService` (+ `IBaseService<DangKyApple>` qua base).
- **Dữ liệu (entities):** `DangKyApple` ↔ `DangKyAppleDto` / `DangKyAppleFilterDto`.
- **Ripple / đồng bộ:** không thấy ở tầng controller.

## Lỗi đã biết / lưu ý kỹ thuật
- `DangKyAppleController.cs:17` — class tên `DangKyDatLichController` không khớp tên file `DangKyAppleController.cs` → khó tra cứu/route gây nhầm (route thật `api/DangKyDatLich`).
- `DangKyAppleController.cs:17` — class `partial` → cần kiểm tra có file partial khác bổ sung action không trước khi kết luận đầy đủ.
- `DangKyAppleController.cs:19` — controller giữ riêng `_service` (IDangKyAppleService) song song với `_service` của base (IBaseService) → 2 field cùng tên ngữ nghĩa, dễ nhầm khi maintain.
