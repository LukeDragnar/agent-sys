---
controller: NhomHoaHongCTVChienDichController
module: nhan-su
service: CommissionFee
man_hinh_ui: "Hoa hồng CTV > Nhóm hoa hồng > tab Hoa hồng theo chiến dịch (gán chiến dịch vào nhóm)"
muc_dich: "Gắn/đổi cấu hình hoa hồng theo chiến dịch cho 1 nhóm hoa hồng CTV: thêm mới gán chiến dịch, thay đổi gán, tìm kiếm cấu hình chiến dịch của nhóm."
alias: [NhomHoaHongCTVChienDich, "hoa hồng chiến dịch của nhóm", "gán chiến dịch vào nhóm"]
auth: public
state_machine: []
deprecated: false
entities: [NhomHoaHongCTVChienDich, ChienDichHoaHongCTV, NhomHoaHongCTV]
stored_procs: []
depends: [NhomHoaHongCTV, ChienDichHoaHongCTV]
cross_module_in: []
cross_module_out: [CommissionFeeUpdateOrder (sinh DonHangChiTietChienDich)]
---

# NhomHoaHongCTVChienDich — Cấu hình hoa hồng chiến dịch của nhóm

## Hành động
| # | Hành động | Nhãn UI/nút | Endpoint | Quyền (key) | Auth | Hành vi đúng (expected) | Nguồn | Known-issue |
|---|---|---|---|---|---|---|---|---|
| 1 | Thay đổi gán chiến dịch ↔ nhóm | Lưu | POST `ThayDoiChienDichNhomHoaHong` | `NhomHoaHongCTVChienDich_ThayDoiChienDichNhomHoaHong` (AD-TONG/CN/MCN) | public | Cập nhật gán chiến dịch hoa hồng cho nhóm. | EF | — |
| 2 | Thêm mới gán chiến dịch | Lưu | POST `ThemMoiNhomHoaHongChienDich` | `NhomHoaHongCTVChienDich_ThemMoiNhomHoaHongChienDich` | public | Gán chiến dịch hoa hồng vào nhóm. | EF | — |
| 3 | Tìm cấu hình chiến dịch của nhóm | Lưới/Lọc | POST `TimKiemHoaHongChienDichTheoDieuKien` | `NhomHoaHongCTVChienDich_TimKiemHoaHongChienDichTheoDieuKien` | public | Trả cấu hình chiến dịch gắn với nhóm theo điều kiện. | EF | — |

## Quan hệ
- **Gọi sang:** EF.
- **Dữ liệu (entities):** liên kết `NhomHoaHongCTV` ↔ `ChienDichHoaHongCTV`. Cấu hình % chi tiết theo SP nằm ở `ChienDichHoaHongCTVSanPham`.
- **Ripple/đồng bộ:** chỉ phát huy tác dụng khi nhóm có cờ `IsSuDungHoaHongChienDich=true` (xem NhomHoaHongCTV) — khi đó `CommissionFeeUpdateOrder` mới tra cấu hình chiến dịch (SP `PRC_CommissionFee_GetCauHinh_ChienDich`) để sinh `DonHangChiTietChienDich`.

## Lỗi đã biết / lưu ý kỹ thuật
- `ThayDoiChienDichNhomHoaHong` và `ThemMoiNhomHoaHongChienDich` nhận cùng DTO `HoaHongChienDichThayDoi` → phân biệt thêm/sửa nằm trong service; cần đảm bảo không tạo trùng gán cùng (nhóm, chiến dịch).
- Cấu hình ở đây vô hiệu nếu nhóm tắt cờ `IsSuDungHoaHongChienDich` — bug "đã gán chiến dịch nhưng không phát sinh hoa hồng" thường do cờ này.
