---
controller: DMKetQuaTacNghiepController
module: sales
service: Contact
man_hinh_ui: "Sale > Thiết lập tác nghiệp — danh mục kết quả tác nghiệp (nội bộ trong màn cấu hình tác nghiệp; cũng có ở Quản trị > Tác nghiệp)"
muc_dich: "CRUD danh mục kết quả tác nghiệp (outcome của 1 bước tác nghiệp: thành công/từ chối/hẹn lại...): tên, màu, cờ IsChotDon, IsDatLich. Là vế 'KẾT QUẢ' trong luật follow-up DMTacNghiepCauHinh."
alias: [DMKetQuaTacNghiep, "kết quả tác nghiệp", "outcome tác nghiệp", "danh mục kết quả"]
auth: public
state_machine: []
deprecated: false
entities: [DMKetQuaTacNghiep]
stored_procs: []
depends:
  - "DMKetQuaTacNghiepSerive (Insert/Update/TimTheoDieuKien/Delete; bản HeThong*) [sic: tên interface 'Serive']"
  - "GetCauHinhInit (IsEnableTacNghiepTheoChiNhanh — danh mục theo chi nhánh hay đơn vị)"
cross_module_in:
  - "DMTacNghiepCauHinh tham chiếu IdKetQua làm vế 'KẾT QUẢ' của luật follow-up"
cross_module_out:
  - "Cache DMKetQuaTacNghiep theo đơn vị/chi nhánh"
---

# DMKetQuaTacNghiep — Danh mục kết quả tác nghiệp (outcome)

> CRUD danh mục (73 dòng) cho entity `DMKetQuaTacNghiep` = kết quả/outcome của 1 bước tác nghiệp. Mỗi bản ghi có `Ten`, `Color`, cờ `IsChotDon` (kết quả này tính là chốt đơn), `IsDatLich` (đặt lịch). Catalog thuần; là 1 trong 3 vế (cùng DMTacNghiep + DMTacNghiepSauBaoLau) tạo nên luật follow-up.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Thêm mới | Lưu (thêm) | POST `InsertOneAsync` | `DMKetQuaTacNghiep_InsertOneAsync` (AD-TONG/CN/MCN) | public | Nút Thêm | `InsertOneAsync(model)` — insert kết quả theo đơn vị. | EF | — |
| 2 | Cập nhật | Lưu (sửa) | POST `UpdateOneAsync` | `DMKetQuaTacNghiep_UpdateOneAsync` (AD-TONG/CN/MCN) | public | Form edit | `UpdateOneAsync(model, CurrentUser)`. | EF | — |
| 3 | Tìm theo điều kiện (grid) | Danh sách / Lọc | GET `TimTheoDieuKienAsync` | `DMKetQuaTacNghiep_TimTheoDieuKienAsync` (AD-TONG/CN/MCN) | public | Mở danh mục | `TimTheoDieuKienAsync(CurrentUser)` — list theo đơn vị/chi nhánh. | EF | — |
| 4 | Tìm theo chi nhánh | (load combo) | GET `TimTheoDieuKienChiNhanhAsync?idChiNhanh=` | `DMKetQuaTacNghiep_TimTheoDieuKienChiNhanhAsync` (AD-TONG/CN/MCN) | public | — | `TimTheoDieuKienChiNhanhAsync(idChiNhanh, CurrentUser)`. | EF | — |
| 5 | Xóa | Xóa | POST `DeleteOneItemAsync/{id:guid}` | `DMKetQuaTacNghiep_DeleteOneItemAsync` (AD-TONG/CN/MCN) | public | Nút Xóa | `DeleteOneItemAsync(id, CurrentUser)`. | EF | Không kiểm tra kết quả còn bị luật cấu hình tham chiếu hay không |
| 6 | Tìm theo ngành nghề (hệ thống) | — | POST `TimTheoDieuKienByNganhNghe` | `DMKetQuaTacNghiep_TimTheoDieuKienByNganhNghe` (AD-TONG/CN/MCN) | public | — | `HeThongTimTheoDieuKienAsync(model, CurrentUser)`. | EF | — |

## Quan hệ
- **Gọi sang:** `IDMKetQuaTacNghiepSerive`, `GetCauHinhInit`.
- **Dữ liệu (entities):** `DMKetQuaTacNghiep` (Ten, Color, IsChotDon, IsDatLich).
- **Ripple / đồng bộ:** cache theo đơn vị/chi nhánh; `DMTacNghiepCauHinh` tham chiếu `IdKetQua` (list cấu hình gắn "(Deleted)" nếu kết quả bị xóa).

## Lỗi đã biết / lưu ý kỹ thuật
- Có **bản sinh đôi Base** `Areas/SanboxArea/DMKetQuaTacNghiepController.cs` route `api/QuanTriArea/DMKetQuaTacNghiep` (TienIch `TI_HeThong`, isAdmin), role `HT-AD, HT-CSKH`, gọi `HeThong*` — khác route/role, cùng permission key.
- `DMKetQuaTacNghiepController.cs:17` — tên interface service viết sai chính tả `IDMKetQuaTacNghiepSerive` (thiếu/đảo "Serive" thay vì "Service") → lưu ý khi search/inject; nhất quán trong codebase nên không gây lỗi nhưng dễ nhầm.
- Xóa kết quả không guard tham chiếu: khác `DMTacNghiepSauBaoLau` (cascade xóa cấu hình), ở đây `DeleteOneItemAsync` không xóa/khóa các `DMTacNghiepCauHinh` đang dùng `IdKetQua` → luật cũ trở thành tham chiếu mồ côi, list cấu hình hiển thị "(Deleted)".
- TienIch controller = `TI_OrderLogistic` (khác `DMTacNghiep` = `TI_Marketing`) → kiểm tra phân quyền tiện ích nếu đơn vị chỉ mua 1 trong 2.
