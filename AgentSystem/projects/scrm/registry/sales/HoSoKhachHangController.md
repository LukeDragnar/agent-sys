---
controller: HoSoKhachHangController
module: sales
service: Contact
man_hinh_ui: "Marketing/Landing > Hồ sơ khách hàng (landing-page/ho-so-khach-hang — báo cáo/lưới hồ sơ KH lead MKT, trùng số) · nút Xuất Excel (DanhSachContact.xlsx)"
muc_dich: "Báo cáo hồ sơ khách hàng (lead) cho Marketing/Sale: search danh sách, đếm trùng số, export Excel 2 kiểu"
alias: ["Hồ sơ khách hàng", "Hồ sơ KH", "Lead MKT", HoSoKhachHang]
auth: public
state_machine: []
deprecated: false
entities: [Contact, HoSoKhachHang]
stored_procs: ["GetHoSoKhachHang_LeadMKT_* đi qua IHoSoKhachHangService (truy vấn tổng hợp/SP)"]
depends: [IHoSoKhachHangService]
cross_module_in: []
cross_module_out: [Contact, KhachHang]
---

# HoSoKhachHangController — Báo cáo hồ sơ khách hàng (lead MKT)

> Controller báo cáo (TI_Marketing). Cũng được `ContactController` tái sử dụng `IHoSoKhachHangService` cho các API trùng số/khách cũ.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Danh sách hồ sơ KH (lead MKT) | Lưới "Hồ sơ khách hàng" | `POST GetHoSoKhachHang_LeadMKT_Search` | HoSoKhachHangController_GetHoSoKhachHang_LeadMKT_Search (AD/MKT/QL-MKT/CHIASO) | public | — | `GetHoSoKhachHang_LeadMKT_Search(payload, RoleCode.SALE_AND_MKT, CurrentUser)` — list theo scope role sale+MKT | SP/EF | — |
| 2 | Đếm hồ sơ KH trùng số | (badge số trùng) | `POST GetHoSoKhachHang_LeadMKT_TrungSo_Count` | HoSoKhachHangController_GetHoSoKhachHang_LeadMKT_TrungSo_Count (AD/MKT/QL-MKT) | public | — | Đếm bản ghi hồ sơ KH trùng số (role DV_MKT) | SP/EF | Endpoint Search-trùng-số gốc đã bị **comment** (`:34-40`) — chỉ còn Count |
| 3 | Export Excel hồ sơ KH (kiểu 1/2) | Nút "Xuất Excel" | `POST ExportExcelHoSoKhachHangKieu1` · `POST ExportExcelHoSoKhachHangKieu2` | `[SBAuthorize()]` (chỉ đăng nhập) | public | — | `result.Success=false` → **BadRequest(message)**; success → trả file `DanhSachContact.xlsx` | GemBox | Cả 2 export chỉ `[SBAuthorize()]` — không mã/role |

## Quan hệ
- **Gọi sang:** `IHoSoKhachHangService` (search + count trùng số + export). Service này còn được `ContactController` gọi cho các API trùng số/khách cũ/trùng SP.
- **Dữ liệu (entities):** tổng hợp từ `Contact` / hồ sơ khách hàng (lead).
- **Ripple/đồng bộ:** chỉ đọc/báo cáo — không ghi; ripple gián tiếp tới Contact/KhachHang qua truy vấn tổng hợp.

## Lỗi đã biết / lưu ý kỹ thuật
- `HoSoKhachHangController.cs:34-40` — endpoint `GetHoSoKhachHang_LeadMKT_TrungSo_Search` (search trùng số) bị **comment toàn bộ**; chỉ còn Count → UI lấy danh sách trùng số qua `ContactController.GetDanhSachTrungSo` (delegate cùng service).
- `HoSoKhachHangController.cs:56-89` (Export) — chỉ `[SBAuthorize()]` không mã quyền; trả `BadRequest` khi lỗi (khác convention ResponseResult của các controller khác).
- `HoSoKhachHangController.cs:30` & `:47` — RoleCode (`SALE_AND_MKT` / `DV_MKT`) **gán cứng trong controller** rồi truyền xuống service → đổi quyền phải sửa code.
