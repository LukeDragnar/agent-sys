# Dev — Phê duyệt giao dịch tổng đài (super-admin)

> Service: **Switchboard** (BE .NET). Controller trong `SanboxArea` (`api/SanboxArea/...`), `TI_HeThong`, chỉ role hệ thống **HT-AD, HT-KT** → super-admin duyệt nạp tiền tổng đài cho đơn vị. Feature thuộc module **Hệ thống** (registry node nằm ở `registry/sales/` do chung service Switchboard). Chỉ dành cho dev.

## Màn hình & route (FE)
| Màn hình | Vai trò | Ghi chú |
|---|---|---|
| Giao dịch tổng đài IP đơn vị (SanboxArea) | Super-admin duyệt/hủy thanh toán nạp tiền, sửa hạn giao dịch | route FE: cần xác nhận khi cần (registry chưa ghi route FE) |

## Endpoint / Controller (trace registry)
**TongDaiIpDonViSAController** — `api/SanboxArea/...`, `TI_HeThong`, role HT-AD/HT-KT → internal. (registry: `projects/scrm/registry/sales/TongDaiIpDonViSAController.md`)

| MaTinhNang (key) | Route (HTTP) | Mục đích | State |
|---|---|---|---|
| `TongDaiIpDonViSAController_GetLichSuThanhToanSA` | POST `GetLichSuThanhToanSA` | Lịch sử giao dịch nạp tiền theo `idDonVi` | read-only |
| `TongDaiIpDonViSAController_DuyetThanhToanNapTienTongDaiSA` | POST `DuyetThanhToanNapTienTongDaiSA` | Duyệt giao dịch → `DaThanhToan`, cộng số dư đơn vị | ChoThanhToan → DaThanhToan |
| `TongDaiIpDonViSAController_HuyDuyetThanhToanToanTongDaiSA` | POST `HuyDuyetThanhToanToanTongDaiSA` | Hủy duyệt giao dịch → `HuyThanhToan` | DaThanhToan → HuyThanhToan |
| `TongDaiIpDonViSAController_UpdateThongTinGiaoDichDonViSA` | POST `UpdateThongTinGiaoDichDonViSA` | Cập nhật ngày hết hạn chi tiết giao dịch | — |

## Entities
- `GiaoDichTongDai` — giao dịch nạp tiền tổng đài (bản ghi cần duyệt). State: `GiaoDich_ChoThanhToan`, `GiaoDich_DaThanhToan`, `GiaoDich_HuyThanhToan`.
- `TongDaiIpDonVi` — tổng đài IP đơn vị; nơi số dư đơn vị được cộng khi duyệt.

## Luồng code
- `GetLichSuThanhToanSA` → `ITongDaiIpDonViSAService` → query lịch sử theo `idDonVi` (EF). Read-only.
- `DuyetThanhToanNapTienTongDaiSA` → validate `idDonVi` rỗng → lỗi "Id đơn vị không được để trống" → hợp lệ → service duyệt → state `DaThanhToan` + cộng số dư đơn vị.
- `HuyDuyetThanhToanToanTongDaiSA` → service hủy duyệt → state `HuyThanhToan`. **Không validate `idDonVi`/`idGiaoDich` rỗng.**
- `UpdateThongTinGiaoDichDonViSA` → cập nhật ngày hết hạn chi tiết giao dịch (EF).

## Bug-prone / lưu ý (path:line)
- **`TongDaiIpDonViSAController.cs:54-57` (Duyệt) validate `idDonVi` rỗng, nhưng `:71` (`HuyDuyetThanhToanToanTongDaiSA`) KHÔNG validate `idDonVi`/`idGiaoDich` rỗng** → có thể gọi hủy duyệt với tham số null → hành vi không xác định / lỗi không rõ ràng. Nên thêm validate giống endpoint duyệt.
- **Duyệt cộng số dư thật:** duyệt → cộng số dư tổng đài đơn vị; **hủy duyệt** cần đảm bảo **trừ lại số dư** tương ứng để khớp — kiểm tra service có rollback số dư khi hủy không (tránh số dư phình do duyệt rồi hủy).
- **Thao tác ngược trạng thái:** duyệt giao dịch không ở `ChoThanhToan` hoặc hủy duyệt giao dịch chưa `DaThanhToan` → cần service chặn theo state-machine để tránh lệch số dư.

> KHÔNG sửa code ở session này — chỉ ghi nhận; để session security xử lý.

## File source (path:line)
- BE controller: `TongDaiIpDonViSAController.cs:54-57` (Duyệt validate idDonVi), `:71` (HuyDuyet không validate).
- Service: `ITongDaiIpDonViSAService` (duyệt / hủy / cập nhật hạn).
- Registry: `projects/scrm/registry/sales/TongDaiIpDonViSAController.md`.
- Liên quan state-machine: `TongDaiIpDonViController` (luồng nạp tiền/thanh toán của đơn vị).
