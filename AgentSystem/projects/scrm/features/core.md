# Core — Nền Tảng Multi-Tenant

> Quản lý DonVi (tenant), ChiNhanh, User, Role, Permission. Mọi service đều phụ thuộc Core.

---

## Vai trò trong hệ thống

- Cung cấp `IdDonVi` (tenant key) — mọi query đều filter theo field này
- Cung cấp `UserLogin` context (IdDonVi, IdChiNhanh, ListQuyen)
- ← **Authen**: xác thực xong → Core cấp thông tin user đầy đủ
- → Tất cả service: mọi request đều mang JWT chứa context từ Core

---

## Kiến trúc Multi-Tenant

```
DonVi (Tenant)
  └─ ChiNhanh [1..N]
       └─ User [1..N]
            └─ UserRole → Role → RolePermision → Permission
```

- `IdDonVi` là **partition key** của toàn hệ thống
- User có `IsMultipleUnit = true` → thuộc nhiều đơn vị
- `DonViRedis` cache thông tin tenant trong Redis

---

## Entities chính

### DonVi
| Field | Ý nghĩa |
|-------|---------|
| `TenMien` | Domain riêng của đơn vị |
| `IdGoiSuDung` | Gói dịch vụ |
| `NgayHetHanSuDung` | Hạn sử dụng |
| `LevelGoiDichVu` | Cấp độ gói |
| `IsCustomPermision` | Quyền tùy chỉnh riêng |
| `IsLoginOneDevice` | Giới hạn 1 thiết bị đăng nhập |
| `IndexName*` | Tên index Elasticsearch riêng |

### ChiNhanh
| Field | Ý nghĩa |
|-------|---------|
| `IdDonVi` | Thuộc đơn vị nào |
| `Latitude/Longtitude` | Tọa độ GPS |
| `IsHienThiDatLich` | Hiển thị cho khách đặt lịch |

### User
| Field | Ý nghĩa |
|-------|---------|
| `IsSuperUser` | Admin toàn hệ thống |
| `IsAdmin` / `IsAdChiNhanh` | Admin đơn vị / chi nhánh |
| `IsNhanVien` / `IsCSKH` / `IsTruongNhom` | Vai trò |
| `IsMultipleUnit` | Thuộc nhiều đơn vị |
| `IdNhomNhanVien` / `IdPhongBan` | Nhóm, phòng ban |
| `ListQuyen` | Cache danh sách quyền (JSON) |

### Permission model
```
User ──M:N──> Role ──1:N──> RolePermision → Permission.KeyCode
```
- `Permission.KeyCode` dùng trong `[SBAuthorize("KeyCode")]` trên controller
- `Permission.Loai`: AUTO (hệ thống sinh) / USER (admin tạo thủ công)

---

## Checklist fix bug

- [ ] Không thấy data → kiểm tra `IdDonVi` trong JWT có khớp với data không
- [ ] Không có quyền → kiểm tra `UserRole → RolePermision → Permission.KeyCode`
- [ ] Cache hạng/nhóm sai → clear Redis key theo `IdDonVi`
