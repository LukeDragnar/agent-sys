---
controller: DMTacNghiepSauBaoLauController
module: sales
service: Contact
man_hinh_ui: "Sale > Thiết lập tác nghiệp — danh mục 'sau bao lâu' (nội bộ trong màn cấu hình tác nghiệp; danh mục mốc thời gian follow-up)"
muc_dich: "CRUD danh mục mốc thời gian 'sau bao lâu' (SLA/timing) dùng cho luật follow-up: vế 'THÌ làm tác nghiệp tiếp SAU thời gian T'. Mỗi bản ghi = số lượng + loại thời gian (phút/giờ/ngày)."
alias: [DMTacNghiepSauBaoLau, "sau bao lâu", "mốc thời gian tác nghiệp", "SLA follow-up", "timing tác nghiệp"]
auth: public
state_machine: []
deprecated: false
entities: [DMTacNghiepSauBaoLau, DMTacNghiepCauHinh]
stored_procs: []
depends:
  - "DMTacNghiepSauBaoLauService (Insert/Update/TimTheoDieuKien/Delete/CheckExist; bản HeThong*)"
  - "GetCauHinhInit (IsEnableTacNghiepTheoChiNhanh — danh mục theo chi nhánh hay đơn vị)"
cross_module_in:
  - "DMTacNghiepCauHinh tham chiếu IdSauBaoLau khi cấu hình luật 'làm tác nghiệp tiếp sau T'"
cross_module_out:
  - "Xóa 1 mốc 'sau bao lâu' → CASCADE xóa các DMTacNghiepCauHinh đang dùng mốc đó (xem lưu ý)"
  - "Cache DMTacNghiepSauBaoLau + DMTacNghiepCauHinh theo đơn vị/chi nhánh"
---

# DMTacNghiepSauBaoLau — Danh mục mốc thời gian "sau bao lâu" (timing follow-up)

> CRUD danh mục (64 dòng controller / 381 dòng service) cho entity `DMTacNghiepSauBaoLau`: mỗi bản ghi = `SoThoiGian` + `LoaiThoiGian` (1=phút, 2=giờ, 3=ngày → quy ra `SoPhut`/`Ten`). Mang **rule timing** cho luật follow-up: validate giá trị hợp lệ + chống trùng; xóa có ripple cascade sang cấu hình.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Thêm mới | Lưu (thêm) | POST `InsertOneAsync` | `DMTacNghiepSauBaoLau_InsertOneAsyncc` (AD-TONG/CN/MCN) | public | Nút Thêm | Validate LoaiThoiGian∈[1..3] + SoThoiGian>0 → set Ten/SoPhut → CheckExist → insert → clear cache. | EF | Key có typo "Asyncc" (xem lỗi) |
| 2 | Cập nhật | Lưu (sửa) | POST `UpdateOneAsync` | `DMTacNghiepSauBaoLau_UpdateOneAsync` (AD-TONG/CN/MCN) | public | Form edit | Validate giống Insert; tìm theo Id + đơn vị (IsDelete=false); update. | EF | — |
| 3 | Tìm theo điều kiện (grid) | Danh sách / Lọc | GET `TimTheoDieuKienAsync` | `DMTacNghiepSauBaoLau_TimTheoDieuKienAsync` (AD-TONG/CN/MCN) | public | Mở danh mục | Cache-first theo đơn vị+chi nhánh; trả list mốc thời gian. | EF (cache) | — |
| 4 | Xóa | Xóa | POST `DeleteOneItemAsync/{id:guid}` | `DMTacNghiepSauBaoLau_DeleteOneItemAsync` (AD-TONG/CN/MCN) | public | Nút Xóa | Xóa mốc + **cascade xóa các DMTacNghiepCauHinh có IdSauBaoLau=id**; clear cả 2 cache. | EF | Cascade delete cấu hình (xem lưu ý) |
| 5 | Tìm theo ngành nghề (hệ thống) | — | POST `TimTheoDieuKienByNganhNghe` | `DMTacNghiepSauBaoLau_TimTheoDieuKienByNganhNghe` (AD-TONG/CN/MCN) | public | — | `HeThongTimTheoDieuKienAsync(model, CurrentUser)`. | EF | — |

## Quy tắc nghiệp vụ (validate / ripple)
1. **Loại thời gian hợp lệ** — `LoaiThoiGian` phải ∈ {1,2,3} (phút/giờ/ngày); ngoài khoảng → lỗi "Loại thời gian sai". `DMTacNghiepSauBaoLauService.cs:87,164`.
2. **Số thời gian dương** — `SoThoiGian` phải > 0; ≤0 → lỗi "Thời gian nhập sai". `DMTacNghiepSauBaoLauService.cs:90,167`.
3. **Tên/SoPhut tự sinh** — `SetTenThoiGianByModelSauBaoLau` quy đổi (SoThoiGian + LoaiThoiGian) ra `Ten` hiển thị và `SoPhut` chuẩn hóa. `DMTacNghiepSauBaoLauService.cs:93`.
4. **Gắn chi nhánh theo cấu hình** — `IsEnableTacNghiepTheoChiNhanh=true` → set `IdChiNhanh`, ngược lại null (dùng chung đơn vị).
5. **Chống trùng** — CheckExist trước insert; trùng → "Sau bao lâu nghiệp đã tồn tại, không thể lưu."
6. **Xóa kéo theo cấu hình** — khi xóa 1 mốc, mọi `DMTacNghiepCauHinh` đang dùng `IdSauBaoLau=id` **bị xóa luôn** (cascade chủ động trong service). `DMTacNghiepSauBaoLauService.cs:71-79`.

## Quan hệ
- **Gọi sang:** `IDMTacNghiepSauBaoLauService`, `GetCauHinhInit`, `ICommonService.GetDMTacNghiepCauHinhByIdDonViChiNhanh` (để cascade).
- **Dữ liệu (entities):** `DMTacNghiepSauBaoLau` (SoThoiGian, LoaiThoiGian, SoPhut, Ten); ripple sang `DMTacNghiepCauHinh`.
- **Ripple / đồng bộ:** Insert/Update/Delete clear cache SauBaoLau; Delete còn clear cache CauHinh và xóa cấu hình liên quan.

## Lỗi đã biết / lưu ý kỹ thuật
- Có **bản sinh đôi Base** `Areas/SanboxArea/DMTacNghiepSauBaoLauController.cs` route `api/QuanTriArea/DMTacNghiepSauBaoLau` (TienIch `TI_HeThong`, isAdmin), role `HT-AD, HT-CSKH`, gọi `HeThong*` — khác route/role, cùng permission key.
- `DMTacNghiepSauBaoLauController.cs:24` — permission key `DMTacNghiepSauBaoLau_InsertOneAsyncc` có **typo "Asyncc"** (3 chữ c) → key này phải khớp chính xác khi gán quyền/seed; sai 1 ký tự là mất quyền.
- `DMTacNghiepSauBaoLauService.cs:71-79` — Delete **cascade xóa DMTacNghiepCauHinh** dùng mốc này mà không cảnh báo user → xóa 1 mốc thời gian có thể âm thầm phá nhiều luật follow-up đã cấu hình. Cần xác nhận đây là ý đồ.
- `DMTacNghiepSauBaoLauService.cs:60,71` — đọc bằng `TableWithNolock` (dirty read) khi xác định bản ghi xóa và danh sách cấu hình liên quan → dưới race có thể bỏ sót cấu hình cần cascade.
- TienIch controller = `TI_OrderLogistic` (khác `DMTacNghiep` = `TI_Marketing`) dù cùng họ tác nghiệp → kiểm tra phân quyền tiện ích nếu đơn vị chỉ mua 1 trong 2.
