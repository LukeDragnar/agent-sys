---
controller: ChienDichHoaHongCTVController
module: nhan-su
service: CommissionFee
man_hinh_ui: "Hoa hồng CTV > Chiến dịch hoa hồng (danh sách + thêm/sửa; toggle Sử dụng)"
muc_dich: "CRUD chiến dịch hoa hồng CTV (master chiến dịch: mã, tên, khoảng ngày áp dụng, cờ sử dụng), bật/tắt sử dụng, autocomplete, và tra nhóm hoa hồng đang dùng chiến dịch."
alias: [ChienDichHoaHongCTV, "chiến dịch hoa hồng", "campaign hoa hồng CTV"]
auth: public
state_machine: [IsSuDung (bật/tắt)]
deprecated: false
entities: [ChienDichHoaHongCTV, ChienDichHoaHongCTVSanPham, NhomHoaHongCTVChienDich]
stored_procs: []
depends: [Redis (cache mã chiến dịch private)]
cross_module_in: []
cross_module_out: [ChienDichHoaHongCTVSanPham (cấu hình % theo SP), NhomHoaHongCTVChienDich (gán vào nhóm)]
---

# ChienDichHoaHongCTV — Quản lý chiến dịch hoa hồng CTV (master)

## Hành động
| # | Hành động | Nhãn UI/nút | Endpoint | Quyền (key) | Auth | Hành vi đúng (expected) | Nguồn | Known-issue |
|---|---|---|---|---|---|---|---|---|
| 1 | Thêm mới chiến dịch | Lưu | POST `ThemMoi` | `ChienDichHoaHongCTV_ThemMoi` (AD-TONG/CN/MCN) | public | Validate + insert (Mã, Tên, TuNgay, DenNgay, IsSuDung). | EF | — |
| 2 | Cập nhật chiến dịch | Lưu | POST `CapNhat` | `ChienDichHoaHongCTV_CapNhat` | public | Update Mã/Tên/IsSuDung/TuNgay/DenNgay; xóa cache mã private. | EF + Redis | — |
| 3 | Xóa chiến dịch | Xóa | POST `Xoa/{id}` | `ChienDichHoaHongCTV_Xoa` | public | Xóa chiến dịch. | EF | — |
| 4 | Lấy theo Id | Mở edit | POST `LayTheoId/{id}` | `ChienDichHoaHongCTV_LayTheoId` | public | Trả chi tiết (check đúng đơn vị). | EF | — |
| 5 | Tìm theo điều kiện (grid) | Danh sách/Lọc | POST `TimTheoDieuKien` | `ChienDichHoaHongCTV_TimTheoDieuKien` | public | Lọc chiến dịch (non-admin ép theo chi nhánh). | EF | — |
| 6 | Autocomplete | (combo) | POST `TimKiemAutoComplete` | `ChienDichHoaHongCTV_TimKiemAutoComplete` | public | Autocomplete; non-admin lọc chi nhánh; hỗ trợ ListIdExisted/ListIdSelected. | EF | — |
| 7 | Bật/tắt sử dụng | Toggle Sử dụng | POST `CapNhatTrangThaiSuDung/{id}` | `ChienDichHoaHongCTV_CapNhatTrangThaiSuDung` | public | **Toggle** `IsSuDung = !IsSuDung` (đảo trạng thái, không nhận tham số mới). | EF | Toggle thuần — gọi 2 lần về trạng thái cũ |
| 8 | Tìm nhóm HH đang dùng chiến dịch | — | POST `TimKiemNhomHoaHongSuDung` | `ChienDichHoaHongCTV_TimKiemNhomHoaHongSuDung` | public | Trả danh sách nhóm hoa hồng có gán chiến dịch. | EF | — |

## Quan hệ
- **Gọi sang:** Redis (xóa cache mã chiến dịch private sau cập nhật).
- **Dữ liệu (entities):** `ChienDichHoaHongCTV` (master); cấu hình % theo SP ở `ChienDichHoaHongCTVSanPham`; gán vào nhóm qua `NhomHoaHongCTVChienDich`.
- **Ripple/đồng bộ:** chiến dịch + cấu hình SP của nó được `CommissionFeeUpdateOrder` tra qua SP `PRC_CommissionFee_GetCauHinh_ChienDich` với `NgayApDung = DateTime.Now` → chiến dịch chỉ tính HH khi đơn nằm trong `[TuNgay, DenNgay]` và `IsSuDung=true`.

## Lỗi đã biết / lưu ý kỹ thuật
- `ChienDichHoaHongCTVService.cs:115` — `CapNhatTrangThaiSuDung` là toggle thuần `IsSuDung = !IsSuDung`; UI gọi nhầm/2 lần → trạng thái không như mong đợi (không idempotent).
- `CapNhat` chỉ MarkDirty 5 field (Mã/Tên/IsSuDung/TuNgay/DenNgay) → các field khác trên DTO sẽ KHÔNG được lưu (chủ ý partial update).
- Hiệu lực ngày áp dụng nằm ở SP tra cấu hình (`NgayApDung=Now`); ngoài khoảng `[TuNgay, DenNgay]` → chiến dịch không sinh hoa hồng dù đã gán nhóm.
