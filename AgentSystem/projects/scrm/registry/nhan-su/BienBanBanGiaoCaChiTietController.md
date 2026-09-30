---
controller: BienBanBanGiaoCaChiTietController
module: nhan-su
service: HumanResource
man_hinh_ui: "nội bộ — không có UI (skeleton scaffold chưa hoàn thiện)"
muc_dich: "Biên bản bàn giao ca chi tiết — HIỆN LÀ STUB SCAFFOLD chưa implement: file khai báo class placeholder `MethodController` với đúng 1 endpoint mẫu `TimTheoDieuKien`."
alias: [BienBanBanGiaoCaChiTiet, "biên bản bàn giao ca", "bàn giao ca"]
auth: anonymous
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends: [MethodService (IMethodService — service skeleton)]
cross_module_in: []
cross_module_out: []
---

# BienBanBanGiaoCaChiTiet — Biên bản bàn giao ca chi tiết (STUB scaffold)

> ⚠️ File `BienBanBanGiaoCaChiTietController.cs` thực tế khai báo class **`MethodController`** (partial) với attribute mẫu `[TienIch("MethodController", "Đơn hàng post chi tiết", TI_Order)]` — đây là **code skeleton generate sẵn**, chưa đổi tên/đắp nghiệp vụ bàn giao ca. Chỉ có 1 endpoint mẫu, chưa dùng thực tế.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện (stub) | — (không UI) | POST `TimTheoDieuKien` | (không attribute) | anonymous | — | Gọi `IMethodService.TimTheoDieuKienAsync()` không tham số — body skeleton, chưa có logic nghiệp vụ. | EF (chưa rõ — stub) | Class tên `MethodController` lệch tên file; chưa implement; không `[SBAuthorize]` |

## Quan hệ
- **Gọi sang:** `IMethodService` (service skeleton).
- **Dữ liệu (entities):** chưa xác định — file chưa map entity nghiệp vụ.
- **Ripple / đồng bộ:** không có.

## Lỗi đã biết / lưu ý kỹ thuật
- `BienBanBanGiaoCaChiTietController.cs:10` — class khai báo là `public partial class MethodController` (không phải `BienBanBanGiaoCaChiTietController`); attribute `TienIch("MethodController", "Đơn hàng post chi tiết", TI_Order)` còn nguyên text mẫu của generator.
- Là **scaffold chưa hoàn thiện** — không nên coi như controller nghiệp vụ thật cho tới khi được đắp logic. AI nên bỏ qua nhanh khi triage nghiệp vụ HR.
