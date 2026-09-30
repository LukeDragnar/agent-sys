---
controller: PhanQuyenMobileController
module: he-thong
service: Core
man_hinh_ui: "nội bộ — không có UI; mỗi endpoint là 1 'cổng quyền' (permission gate) để app mobile kiểm tra user có role tương ứng hay không"
muc_dich: "Tập hợp endpoint 'kiểm tra quyền theo role' cho mobile: mỗi action chỉ gắn 1 SBAuthorize role và trả 'OK' nếu user có role đó → client mobile dùng để dò user thuộc role nào (admin chi nhánh, ad tổng, kế toán, sale, CSKH...)."
alias: ["phân quyền mobile", "kiểm tra role mobile", "permission gate mobile", "PhanQuyenMobile"]
auth: public
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends: ["IPageService", "IRedisCacheService", "BaseController"]
cross_module_in: ["App mobile gọi từng endpoint Quyen* để xác định user có thuộc role tương ứng không"]
cross_module_out: []
---

# PhanQuyenMobileController — Cổng kiểm tra quyền theo role (mobile)

Kế thừa `BaseController`, controller-level `[SBAuthorize]` + `[TienIch(TI_Chung)]`. **Mỗi endpoint chỉ là 1 permission gate**: thân hàm luôn trả `MethodResult{ Success=true, Data="OK" }`; ý nghĩa nằm ở `[SBAuthorize("PhanQuyen_*", role)]` — nếu user KHÔNG có role → middleware chặn 403, có role → trả OK. Service inject (IPageService/Redis) KHÔNG được dùng. Route `api/[controller]` (không phải QuanTriArea dù namespace SanboxArea).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Gate quyền đơn vị | — | GET `api/PhanQuyenMobile/QuyenAdminChiNhanh` / `QuyenAdminNhieuChiNhanh` / `QuyenAdminTong` / `QuyenThuNgan` / `QuyenQuanLyLichHen` / `QuyenSale` | `PhanQuyen_QuyenAdminChiNhanh` (DV-AD-CN), `_QuyenAdminNhieuChiNhanh` (DV-AD-MCN), `_QuyenAdminTong` (DV-AD-TONG), `_QuyenThuNgan` (DV-QLDV), `_QuyenQuanLyLichHen` (DV-QLLH), ... | public | user phải có role tương ứng | Có role → trả "OK"; không có → 403 từ middleware. | — (không truy vấn) | — |
| 2 | Gate quyền kế toán/MKT/NV | — | GET `QuyenKeToan` / `QuyenMarketing` / `QuyenNhanVien` | `PhanQuyen_QuyenKeToan` (DV-KT, DV-KT-TONG), `_QuyenMarketing` (DV-MKT), `_QuyenNhanVien` (DV-NV) | public | role tương ứng | Trả "OK" nếu có role. | — | — |
| 3 | Gate quyền hệ thống | — | GET `QuyenHeThongAdmin` / `QuyenHeThongCSKH` / `QuyenHeThongKeToan` / `QuyenHeThongNguoiGioiThieu` / `PhanQuyenQuanLySale` / `PhanQuyenSaleLead` | `PhanQuyen_QuyenHeThongAdmin` (HT-AD), `_QuyenHeThongCSKH` (HT-CSKH), `_QuyenHeThongKeToan` (HT-KT), `_QuyenHeThongNguoiGioiThieu` (HT-NGT), `_QuyenQuanLySale` (DV-QL-SALE), `_QuyenSaleLead` (DV-SALE-LEAD) | public | role tương ứng | Trả "OK" nếu có role. | — | — |

## Quan hệ
- **Gọi sang:** không (service inject nhưng không dùng).
- **Dữ liệu (entities):** không truy vấn entity.
- **Ripple / đồng bộ:** không.

## Lỗi đã biết / lưu ý kỹ thuật
- Endpoint `QuyenSale` (dòng 157) dùng nhầm key `PhanQuyen_QuyenQuanLyLichHen` (trùng key với `QuyenQuanLyLichHen` dòng 142) thay vì 1 key riêng cho sale → cấu hình quyền sale có thể bị lệch.
- `QuyenHeThongNguoiGioiThieu` (dòng 217) mô tả nhãn vẫn ghi "Quyền Hệ thống - kế toán" (copy-paste sai nhãn), role thực tế HT-NGT.
- `IPageService`/`IRedisCacheService` được inject nhưng không dùng trong bất kỳ action nào.
