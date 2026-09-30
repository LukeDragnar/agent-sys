# HumanResource — Nhân Sự

> Quản lý hồ sơ nhân viên và ứng viên tuyển dụng. Lưu tài liệu, ảnh, lịch sử vào công ty. Tự động tạo hồ sơ nhân viên khi user mới được tạo.

---

## Vai trò

- ← **Core/User**: khi user mới được tạo → tự động tạo NhanSu tương ứng
- → **File storage**: lưu tài liệu chứng chỉ, ảnh nhân viên

---

## Entities chính

| Entity | Ý nghĩa |
|--------|---------|
| `NhanSu` | Hồ sơ nhân viên: `BiDanh`, `Facebook`, `IdUser`, `QueQuan`, `NguonTuyenDung`, `NgayVaoCTy`, `NgayLenChinhThuc`, `NgayNghiViec` |
| `UngVien` | Ứng viên: `MaUngVien`, `TenUngVien`, `NgaySinh`, `DiaChi`, `SoDienThoai` |
| `FileNhanSu` | Tài liệu: `IdUser`, `LinkFile`, `TenFile`, `Type` (chứng chỉ, hợp đồng...) |
| `HinhAnh` | Ảnh nhân viên: `IdNhanSu`, `TenAnh`, `NgayTao` |
| `ChienDich` | Chiến dịch tuyển dụng |
| `VanBanQuyDinh` | Văn bản quy định nội bộ |

---

## API Endpoints

| Endpoint | Mô tả |
|----------|-------|
| `GET /GetHumanById/{id}` | Chi tiết nhân viên |
| `POST /ThemMoi` | Tạo hồ sơ nhân viên |
| `POST /CapNhat` | Cập nhật hồ sơ |
| `POST /GetImage` | Lấy ảnh nhân viên (filter theo ngày) |
| `POST /UploadImage` | Upload ảnh |
| `POST /XoaImage/{id}` | Xóa ảnh |
| `POST /UploadFile` | Upload tài liệu chứng chỉ |
| `POST /TimTheoDieuKien` (UngVien) | Tìm kiếm ứng viên |
| `POST /ThemMoi` (UngVien) | Tạo ứng viên mới |

---

## Luồng

```
[Tạo nhân viên tự động]
Core/User service tạo User mới
  ↓ (sync/trigger)
NhanSuService tự động tạo NhanSu.IdUser = userId

[Upload tài liệu]
HR manager POST /UploadFile (chứng chỉ, hợp đồng...)
  └→ FileNhanSu.LinkFile = URL lưu trữ, Type = loại tài liệu

[Quản lý ứng viên]
Recruiter POST /ThemMoi (UngVien)
  └→ Theo dõi UngVien theo ChienDich tuyển dụng
       → Sau khi tuyển, convert sang NhanSu
```

---

## Checklist fix bug

- [ ] Nhân viên không có hồ sơ → kiểm tra trigger tạo NhanSu khi User được tạo
- [ ] File upload không lưu → kiểm tra `FileNhanSu.LinkFile`, kết nối file storage
- [ ] Ảnh không hiển thị → filter `HinhAnh` theo `NgayTao` và `IdNhanSu`
