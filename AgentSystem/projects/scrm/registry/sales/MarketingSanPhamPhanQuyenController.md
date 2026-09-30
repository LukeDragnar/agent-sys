---
controller: MarketingSanPhamPhanQuyenController
module: sales
service: Contact
man_hinh_ui: "Cài đặt hệ thống > Cấu hình contact MKT (config-contact-mkt.component) — phân quyền sản phẩm cho marketing"
muc_dich: "CRUD phân quyền sản phẩm cho marketing (gán user/nhóm MKT được phép thao tác trên sản phẩm nào), và cung cấp dữ liệu phân quyền MKT theo chi nhánh từ cache cho service khác."
alias: [MarketingSanPhamPhanQuyen, "phân quyền sản phẩm MKT", "phân quyền MKT"]
auth: public
state_machine: []
deprecated: false
entities: [MarketingSanPhamPhanQuyen]
stored_procs: []
depends: [MarketingSanPhamPhanQuyenService, "Redis/cache (GetDataPhanQuyenAllMarketingChiNhanhFromCache)"]
cross_module_in: ["service nội bộ (GetDataPhanQuyenAllMarketingChiNhanhFromCache)"]
cross_module_out: []
---

# MarketingSanPhamPhanQuyen — Phân quyền sản phẩm cho marketing

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Hành vi đúng (expected) | Nguồn | Known-issue |
|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện | Danh sách / Lọc | POST `TimTheoDieuKien` | `MarketingSanPhamPhanQuyenController_TimTheoDieuKien` (DV-AD-TONG, DV-AD-CN) | public | màn cấu hình MKT | ModelState invalid → lỗi; hợp lệ → trả danh sách phân quyền theo filter. | EF | — |
| 2 | Tạo hoặc cập nhật | Lưu | POST `CreateOrUpdate` | `MarketingSanPhamPhanQuyenController_CreateOrUpdate` (DV-AD-TONG, DV-AD-CN) | public | form phân quyền | Upsert bản ghi phân quyền sản phẩm MKT. | EF | — |
| 3 | Xóa | Xóa | POST `Delete/{id}` | `MarketingSanPhamPhanQuyenController_Delete` (DV-AD-TONG, DV-AD-CN) | public | dòng phân quyền | Xóa phân quyền theo id (POST — idempotent-safe). | EF | — |
| 4 | Chi tiết theo id | Mở edit | POST `TimTheoId/{id}` | `MarketingSanPhamPhanQuyenController_TimTheoId` (DV-AD-TONG, DV-AD-CN) | public | mở dòng | Trả chi tiết phân quyền theo id. | EF | — |

## Endpoint nội bộ (private / service-to-service)
- `[PrivateSBAuthorize]` `GetDataPhanQuyenAllMarketingChiNhanhFromCache` — service khác lấy toàn bộ dữ liệu phân quyền MKT theo chi nhánh từ cache (dùng khi chia số / lọc landing theo quyền MKT).

## Quan hệ
- **Gọi sang:** `MarketingSanPhamPhanQuyenService`, cache phân quyền MKT theo chi nhánh.
- **Dữ liệu (entities):** MarketingSanPhamPhanQuyen.
- **Ripple / đồng bộ:** dữ liệu phân quyền được service khác đọc qua cache → nếu CreateOrUpdate/Delete không làm mới cache, kết quả phân quyền có thể lệch (cần verify cache invalidation ở service).

## Lỗi đã biết / lưu ý kỹ thuật
- `MarketingSanPhamPhanQuyenController.cs` — 4 endpoint CRUD đều check `ModelState.IsValid` rồi forward service (convention chuẩn, không phát hiện lỗi).
