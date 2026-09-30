# Tính lương — Kỹ thuật

> Service **Branch** (BE .NET). Controller `LuongNhanVienController` — `[TienIchAttribute("LuongNhanVienController", "Quản lý lương nhân viên", MaTienIchSCRM.TI_Branch)]`. Không có `[SBAuthorize]` controller-level; mỗi action khai key riêng (đầy đủ, không bị comment). Mỗi bảng lương = 1 kỳ; chi tiết tách theo từng NV. Registry: `registry/nhan-su/LuongNhanVienController.md`.

## Màn hình & route (FE)
| Màn hình | Vị trí |
|---|---|
| Bảng lương nhân viên (danh sách + chi tiết) | Lương > Bảng lương nhân viên |

> Route FE cụ thể: cần product/dev xác nhận (không trace trong registry node).

## Endpoint/Controller (trace registry + source)
Controller `LuongNhanVienController` (`LuongNhanVienController.cs`), tất cả action quyền role `DV-AD-TONG, DV-AD-CN, DV-AD-MCN, DV-KT, DV-KT-TONG, DV-QLDV`:

| MaTinhNang (key) | HTTP | Mục đích | Source line |
|---|---|---|---|
| `LuongNhanVien_TimTheoDieuKien` | POST `TimTheoDieuKien` | Danh sách bảng lương theo bộ lọc | `:30` |
| `LuongNhanVien_ThemMoi` | POST `ThemMoi` | Insert 1 bảng lương | `:38` |
| `LuongNhanVien_Xoa` | POST `Xoa/{id:Guid}` | Xóa bảng lương | `:46` |
| `LuongNhanVien_GetChiTietLuong` | GET `GetChiTietLuong?id=` | Chi tiết lương theo id (kèm CurrentUser) | `:54` |
| `LuongNhanVien_GetDetailBangLuong` | GET `GetDetailBangLuong?id=` | Chi tiết toàn bảng + danh sách NV | `:63` |
| `LuongNhanVien_SaveLuongTheoNhanVien` | POST `SaveLuongTheoNhanVien` | Lưu/cập nhật lương 1 NV | `:71` |
| `LuongNhanVien_LayThongTinLuongTheoNv` | GET `LayThongTinLuongTheoNv?idNhanVien=&idBangLuong=` | Lương 1 NV trong bảng | `:79` |
| `LuongNhanVien_CapNhatTrangThaiBangLuong` | GET `CapNhatTrangThaiBangLuong?idBangLuong=&type=` | Đổi trạng thái bảng lương | `:87` |
| `LuongNhanVien_TaoLuongChiTietNhanVienTuDong` | GET `TaoLuongChiTietNhanVienTuDong?idBangLuong=` | Sinh chi tiết lương tự động cho cả bảng | `:100` |

> Lưu ý: registry node ghi `auth: public` nhưng SOURCE cho thấy mỗi action có `[SBAuthorize(...)]` với key + danh sách role ở trên (registry node hơi thiếu chính xác ở cột Auth — bám source).

## Entities
- **`LuongNhanVien`** — bảng lương theo kỳ (header).
- **`LuongChiTietNhanVien`** — chi tiết lương từng nhân viên trong bảng.
- Nguồn dữ liệu nền: `CaiDatLuongCoBan` (định mức lương — controller riêng `CaiDatLuongCoBanController`), `GioLamViecNhanVien` (công đã duyệt), `User`/`NhanVien` (Core).

## Luồng code
1. `ThemMoi` → `luongNhanVienService.InsertAsync(item)` tạo bảng lương.
2. `TaoLuongChiTietNhanVienTuDong(idBangLuong, CurrentUser)` → service lấy `CaiDatLuongCoBan` + công từ `GioLamViecNhanVien` → sinh `LuongChiTietNhanVien` cho từng NV.
3. `SaveLuongTheoNhanVien` → `LuuLuongChiTietNhanVien(request, CurrentUser)` chỉnh tay.
4. `CapNhatTrangThaiBangLuong(idBangLuong, type, CurrentUser)` → service map `type` (int) → trạng thái.

## Bug-prone / lưu ý (path:line)
- **`CapNhatTrangThaiBangLuong` nhận `idBangLuong`/`type` nullable** (`LuongNhanVienController.cs:89`) và KHÔNG kiểm tra null trước khi gọi service; mapping trạng thái nằm ở service. Truyền sai/null `type` có thể không đổi trạng thái như mong đợi — cần dev xác nhận guard ở service.
- **Tạo tự động dựa dữ liệu nền:** kết quả phụ thuộc `CaiDatLuongCoBan` + công đã duyệt; thiếu dữ liệu → chi tiết trống. Không tự đồng bộ lại khi công thay đổi sau đó.
- **Hành vi tạo lại tự động trên bảng đã có chi tiết** (ghi đè vs nhân đôi) — cần xác nhận ở service `TaoLuongChiTietNhanVienTuDong`.

## File source (path:line)
- BE controller: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Branch\Branch.API\Controllers\LuongNhanVienController.cs` (`:30`→`:106`; `:89` CapNhatTrangThaiBangLuong nullable).
- BE service: `...\Branch\Branch.Service\Generated\LuongNhanVienService.cs` (ILuongNhanVienService — InsertAsync / TaoLuongChiTietNhanVienTuDong / LuuLuongChiTietNhanVien / CapNhatTrangThaiBangLuong).
- DTO: `...\Branch\Branch.Client\Dto\LuongNhanVienDto.cs` (LuongNhanVienGridInfo, LuongChiTietNhanVienData).
- Cài đặt lương cơ bản: `...\Branch\Branch.API\Controllers\CaiDatLuongCoBanController.cs` (registry `registry/nhan-su/CaiDatLuongCoBanController.md`).
- Stored procedures: không (registry node `stored_procs: []`).
