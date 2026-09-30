---
controller: ChienDichController
module: sales
service: Contact
man_hinh_ui: "nội bộ — không có UI (endpoint nhận dữ liệu chiến dịch TMDT từ kênh thương mại điện tử)"
muc_dich: "Nhận dữ liệu chiến dịch (campaign) từ sàn thương mại điện tử (TMDT) đẩy sang để lưu/đối soát trong Contact."
alias: [ChienDich, "chiến dịch", campaign, "chiến dịch TMDT"]
auth: anonymous
state_machine: []
deprecated: false
entities: [ChienDich]
stored_procs: []
depends: [ChienDichService]
cross_module_in: ["Ecommerce / sàn TMDT (NhanDuLieuChienDichTMDT)"]
cross_module_out: []
---

# ChienDich — Nhận dữ liệu chiến dịch TMDT

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Hành vi đúng (expected) | Nguồn | Known-issue |
|---|---|---|---|---|---|---|---|---|
| 1 | Nhận dữ liệu chiến dịch TMDT | — | POST `NhanDuLieuChienDichTMDT` | không có attribute auth | anonymous | Nhận payload chiến dịch từ sàn TMDT → forward `ChienDichService.NhanDuLieuChienDichTMDT`. | EF | Không auth + trả `Ok()` thô |

## Quan hệ
- **Gọi sang:** `ChienDichService`.
- **Dữ liệu (entities):** ChienDich.
- **Ripple / đồng bộ:** điểm nhận dữ liệu chiến dịch từ kênh TMDT (Ecommerce) vào Contact.

## Lỗi đã biết / lưu ý kỹ thuật
- `ChienDichController.cs:12` — controller-level `[SBAuthorize]` đã bị **comment** và endpoint không có attribute auth nào → **không xác thực** (cần đảm bảo có cơ chế bảo vệ khác ở gateway / chữ ký).
- `ChienDichController.cs:29` — trả `Ok(rs)` thô thay vì `ResponseResult(...)` → response shape khác convention chung của Contact API.
