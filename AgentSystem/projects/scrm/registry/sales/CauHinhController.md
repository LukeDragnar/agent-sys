---
controller: CauHinhController
module: sales
service: Contact
man_hinh_ui: "Cài đặt > Cấu hình Contact (cấu hình init, kho số, chia contact xoay vòng, nguồn dữ liệu, sinh bản ghi CSKH); một phần là cấu hình super-admin nội bộ"
muc_dich: "Trung tâm cấu hình của module Contact: lưu/lấy cấu hình chung của đơn vị (init, kho số, chia contact xoay vòng, nguồn dữ liệu, MKT, sinh bản ghi CSKH, giao hàng chuyển CSKH) và cấu hình kho số theo chi nhánh; expose API private cho service khác đọc cấu hình."
alias: [CauHinh, "cấu hình contact", "cài đặt contact", CauHinhContact]
auth: mixed
state_machine: []
deprecated: false
entities: [CauHinhContact, KhoSoCauHinhChiNhanh, CauHinhSinhBanGhiCSKH]
stored_procs: []
depends: [KhoSoCauHinhChiNhanhService, "OrderLogistic (DongBoCauHinhContactTuDonHang req)", "AutoMapper"]
cross_module_in: ["ChiaContactQuayVong (GetCauHinhChiaContactXoayVong/{idDonVi})", "Order/OrderLogistic (DongBoCauHinhContactTuDonHang)", "service nội bộ (GetCHPrivate, GetCauHinhContact, CauHinhCheSoPrivate, GetAllCauHinhSuperAdmin)"]
cross_module_out: ["KhoSoCauHinhChiNhanh (SaveKhoSoCauHinhChiNhanh kéo theo SaveDonViSuDungKhoSo cập nhật cấu hình kho số cấp đơn vị)"]
---

# CauHinh — Cấu hình chung module Contact

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lưu cấu hình init | Lưu | POST `SaveCauHinhInit` | `CauHinh_Contact_LuuCauHinhContact` (DV-AD-TONG) | public | màn cấu hình contact | ModelState invalid → trả lỗi; hợp lệ → lưu cấu hình init theo đơn vị. | EF | — |
| 2 | Lấy cấu hình init / cấu hình chung | (load màn) | GET `GetCauHinhInit`, GET `Get` | `[SBAuthorize]` (chỉ login) | public | mở màn cấu hình | Trả cấu hình init / cấu hình chung của đơn vị user. | EF | — |
| 3 | Lưu / lấy cấu hình kho số | Lưu kho số | POST `LuuCauHinhKhoSo`, GET `GetCauHinhKhoSo`, GET `GetKhoSoCfgApply` | Lưu: `CauHinh_Contact_LuuCauHinhKhoSo` (DV-AD-TONG); Get: `[SBAuthorize]` | public | tab kho số | Lưu/đọc cấu hình kho số đơn vị; `GetKhoSoCfgApply` trả cấu hình đang áp dụng. | EF | — |
| 4 | Lưu / lấy / đọc đơn vị — chia contact xoay vòng (CH1) | Lưu CH1 | POST `SaveCauHinhChiaContactXoayVong`, GET `GetCauHinhChiaContactXoayVongDonVi` | Lưu: `CauHinh_Contact_LuuCauHinhContact` (DV-AD-TONG); Get: `[SBAuthorize]` | public | tab chia contact | Lưu/đọc cấu hình chia contact xoay vòng của đơn vị user. | EF | — |
| 5 | Cho phép tạo contact khi có cuộc gọi đến | — | POST `GetCauHinhChoPhepTaoContactKhiCuocGoiDen` | `[SBAuthorize()]` (login) | public | — | Trả cờ cho phép tạo contact khi cuộc gọi đến. | EF | — |
| 6 | Cấu hình super-admin (lưu/lấy) | Lưu | POST `Save_SuperAdmin`, POST `GetCauHinhSuperAdmin` | `CauHinh_Contact_LuuCauHinhContactSuperAdmin` / `CauHinh_Contact_GetCauHinhSuperAdmin` (HT-AD, HT-CSKH) | public | màn quản trị hệ thống | Lưu/đọc cấu hình super-admin (cấp hệ thống). | EF | — |
| 7 | Giao hàng chuyển CSKH (DS / lưu) | Lưu | POST `DsCauHinhGiaoHangChuyenCskh`, POST `SaveCauHinhGiaoHangChuyenCskh` | `CauHinh_Contact_DsCauHinhGiaoHangChuyenCskh` / `CauHinh_Contact_SaveCauHinhGiaoHangChuyenCskh` (DV-AD-TONG) | public | tab giao hàng → CSKH | Lấy DS / lưu list mã trạng thái giao vận chuyển sang CSKH. | EF | — |
| 8 | Quản lý nguồn dữ liệu (lưu/lấy) | Lưu | POST `SaveQuanLyNguonDuLieu`, GET `GetCauHinhQuanLyNguonDuLieu` | Lưu: `CauHinh_Contact_SaveQuanLyNguonDuLieu` (DV-AD-TONG); Get: `[SBAuthorize]` | public | tab nguồn dữ liệu | Lưu/đọc cấu hình quản lý nguồn dữ liệu. | EF | — |
| 9 | Cấu hình MKT chia số thủ công (lưu/lấy) | Lưu | POST `SaveCauHinhMKT`, GET `GetCauHinhMKT` | `[SBAuthorize]` (chỉ login) | public | tab MKT | Lưu/đọc cờ cho phép MKT chia số nhập thủ công. | EF | Key role đã comment → chỉ còn `[SBAuthorize]` rỗng (lỏng hơn ý đồ ban đầu) |
| 10 | Check quyền xuất Excel | — | GET `CheckQuyenXuatExcel` | `[SBAuthorize()]` (login) | public | nút xuất Excel | Trả cờ user có quyền xuất Excel. | EF | — |
| 11 | Sinh bản ghi CSKH (chi tiết/lưu/lịch sử) | Lưu | GET `CauHinhSinhBanGhiCSKH_GetDetail`, POST `SaveCauHinhSinhBanGhiCSKH`, POST `LichSuThayDoiCauHinh` | `[SBAuthorize]` (login) | tab sinh bản ghi CSKH | Đọc chi tiết / lưu cấu hình sinh bản ghi CSKH; lịch sử thay đổi cấu hình. | EF | Key role đã comment → chỉ `[SBAuthorize]` rỗng |
| 12 | Kho số cấu hình chi nhánh (lưu/lấy) | Lưu | POST `SaveKhoSoCauHinhChiNhanh`, GET `GetKhoSoCauHinhChiNhanh` | `[SBAuthorize]` (login) | tab kho số theo chi nhánh | Lưu cấu hình kho số chi nhánh → **sau đó tự cập nhật lại cấu hình kho số cấp đơn vị** (gọi `SaveDonViSuDungKhoSo` lấy tất cả chi nhánh active). | EF | — |

## Endpoint nội bộ (private / service-to-service)
- `[PrivateSBAuthorize]` `GetCHPrivate(idDonVi)` — service khác lấy toàn bộ cấu hình theo đơn vị.
- `[PrivateSBAuthorize]` `CauHinhCheSoPrivate(userLogin)` — trả `CauHinhCheSoDto` theo user login.
- `[PrivateSBAuthorize]` `GetCauHinhChiaContactXoayVong/{idDonVi}` — service **ChiaContactQuayVong** đọc cấu hình chia xoay vòng theo đơn vị.
- `[PrivateSBAuthorize]` `GetAllCauHinhSuperAdmin` — lấy toàn bộ cấu hình super-admin.
- `[PrivateSBAuthorize]` `GetCauHinhContact/{idDonVi}` — trả `CauHinh_Contact_Dto` theo đơn vị.
- `[PrivateSBAuthorize]` `DongBoCauHinhContactTuDonHang(req)` — Order/OrderLogistic đẩy sang để đồng bộ cấu hình contact từ đơn hàng.

## Quan hệ
- **Gọi sang:** `KhoSoCauHinhChiNhanhService` (lưu cấu hình kho số theo chi nhánh + đẩy lên đơn vị), AutoMapper (map CauHinhSetting → KhoSoSetting).
- **Dữ liệu (entities):** cấu hình contact của đơn vị (init/kho số/chia xoay vòng/nguồn dữ liệu/MKT/sinh bản ghi CSKH), cấu hình kho số chi nhánh, cấu hình super-admin (cấp hệ thống).
- **Ripple / đồng bộ:** `SaveKhoSoCauHinhChiNhanh` không chỉ lưu chi nhánh mà còn re-sync cấu hình kho số cấp đơn vị → 1 lần lưu chạm 2 phạm vi (chi nhánh + đơn vị).

## Lỗi đã biết / lưu ý kỹ thuật
- `CauHinhController.cs:58,67,108,130,213,220,228,252,260,267,274,286,293` — rất nhiều endpoint dùng `[SBAuthorize]` **rỗng** (chỉ cần login), nhiều cái có key role gốc đã bị comment (`SaveCauHinhMKT`, `SaveCauHinhSinhBanGhiCSKH`, `GetCauHinhSinhBanGhiCSKH`) → khe hở phân quyền nếu lộ ra UI ngoài quản trị.
- `CauHinhController.cs:274-284` — `SaveKhoSoCauHinhChiNhanh` chỉ `[SBAuthorize]` rỗng nhưng side-effect re-sync cấu hình kho số cấp **đơn vị** → user login bất kỳ có thể đổi cấu hình đơn vị nếu UI cho phép.
