# Branch — Tổ Chức Nội Bộ & Thu Chi

> Quản lý phòng ban, ca làm việc, hoa hồng nhân viên, bảng lương, phiếu thu chi.

---

## Vai trò trong hệ thống

- Cung cấp cấu hình **hoa hồng** cho Order khi tính hoa hồng nhân viên
- **ThuChi** được tạo tự động khi Order thanh toán hoặc tính lương
- → **Accounting**: ThuChi liên kết với chứng từ kế toán qua `IdPhieuGoc`

---

## Entities chính

### PhongBan
`IdDonVi → IdChiNhanh → PhongBan → IdUser (trưởng phòng)`

### HoaHong (cấu hình hoa hồng theo nhóm NV)
| Field | Ý nghĩa |
|-------|---------|
| `IdNhomNhanVien` | Nhóm áp dụng |
| `GiaTri` + `KieuLoaiGiaTri` | Giá trị (% hoặc VNĐ) |
| `KieuLoaiApDung` | SP / DV / Thẻ |

### ThuChi (phiếu thu/chi)
| Field | Ý nghĩa |
|-------|---------|
| `LoaiQuy` | Tiền mặt / Ngân hàng |
| `LoaiThuChi` | Thu / Chi |
| `TaoTuDong` | Tạo từ thanh toán/lương tự động |
| `TrangThaiXuLy` | Chưa/đang/thành công/thất bại |
| `IdPhieuGoc` | Liên kết chứng từ Accounting |
| `SuDungQuyKhac` | Chuyển tiền giữa 2 quỹ |

### LuongNhanVien
| Field | Ý nghĩa |
|-------|---------|
| `Thang/Nam` | Kỳ lương |
| `TrangThai` | Chưa tính / đang tính / đã tính |
| `CongThuc` | Công thức tính lương |

### CaLamViecNhanVien / GioLamViecNhanVien
Quản lý ca và giờ làm việc — dùng bởi ChiaSo (Contact) để kiểm tra sale có đang làm việc không.

---

## Checklist fix bug

- [ ] Hoa hồng tính sai → kiểm tra `HoaHong` của nhóm nhân viên
- [ ] ThuChi không khớp kế toán → kiểm tra `ThuChi.IdPhieuGoc`
- [ ] Sale không nhận lead → kiểm tra ca làm việc `CaLamViecNhanVien`
