# SCRM — Domain Knowledge

## Tổng quan hệ thống
SCRM là hệ thống CRM đa tenant (multi-tenant) cho doanh nghiệp vừa và nhỏ.
Mỗi doanh nghiệp = 1 DonVi (IdDonVi), có thể có nhiều ChiNhanh (IdChiNhanh).

## Module chính & quan hệ

```
Core ──────────────── User, DonVi, ChiNhanh, Permission
  │
  ├── Authen ───────── Xác thực nội bộ (nhân viên)
  ├── AuthenCustomer ── Xác thực khách hàng (end-user)
  │
  ├── Customer ─────── Khách hàng (CRM core)
  │     └── Contact ── Liên hệ của khách hàng
  │
  ├── Order ─────────── Đơn hàng
  │     ├── OrderProcess ── Xử lý đơn
  │     ├── OrderLogistic ── Vận chuyển
  │     └── Invoice ──── Hóa đơn từ đơn hàng
  │
  ├── WarehouseProduct ─ Kho hàng + sản phẩm
  │
  ├── WorkFlow ──────── Công việc nội bộ
  │     ├── CongViec ── Task/ticket
  │     ├── DuAn ─────── Project
  │     └── QuyTrinh ─── Workflow/process template
  │
  ├── Booking ──────── Lịch hẹn / đặt lịch
  ├── Promotion ──────── Khuyến mãi
  ├── Marketing ──────── Campaign marketing
  ├── HumanResource ─── Nhân sự
  ├── Accounting ──────── Kế toán / thu chi
  ├── Report ─────────── Báo cáo tổng hợp
  │
  └── Channels (tích hợp bên ngoài):
        Facebook, FacebookChat, TiktokWebhook
        SMS, Switchboard (tổng đài)
        Website (landing page)
```

## WorkFlow Module — Chi tiết

### Entities
```
DuAn (Project)
├── Id, IdDonVi, IdChiNhanh
├── TenDuAn, MaDuAn, MoTa
└── [many] CongViec

CongViec (Task)
├── Id, IdDonVi, IdChiNhanh
├── TieuDe (NVARCHAR 500)
├── MoTa (NVARCHAR 4000)
├── TrangThai: ChuaLam=0, DangLam=1, HoanThanh=2, Huy=3
├── IdMucDoUuTien → DmMucDoUuTien
├── NgayBatDauDuKien, NgayKetThucDuKien (kế hoạch)
├── NgayBatDauThucTe, NgayKetThucThucTe  (thực tế — tự set khi đổi trạng thái)
└── [many-many] CongViecUser → User

CongViecUser (assignment)
├── IdCongViec → CongViec
├── IdUser → User
├── IdDonVi, IdChiNhanh

DmMucDoUuTien (Priority)
├── TenMucDo (tên: Thấp, Trung bình, Cao, Khẩn cấp)
└── MauSac (màu hex)
```

### Business rules — CongViec
- Đổi sang **DangLam**: set NgayBatDauThucTe = now, NgayKetThucThucTe = null
- Đổi sang **HoanThanh / Huy**: set NgayKetThucThucTe = now; nếu chưa có NgayBatDauThucTe thì set luôn
- Đổi sang **ChuaLam**: nếu chưa có NgayBatDauThucTe thì set = now; xóa NgayKetThucThucTe
- Assign user: replace toàn bộ list (xóa cũ, thêm mới trong 1 transaction)
- Filter: theo IdDonVi (bắt buộc), keyword (TieuDe), TrangThai, IdDuAn

### API endpoints — CongViec
```
POST api/CongViec/TimTheoDieuKien    ← list + filter + paging
POST api/CongViec/TimTheoId/{id}     ← detail
POST api/CongViec/LayDanhSachUser    ← danh sách user để assign
POST api/CongViec/Save               ← create + update (chung)
POST api/CongViec/Xoa/{id}           ← soft delete
```

## Core concepts

### UserLogin (auth context)
```csharp
// Inject vào mọi service method
public class UserLogin {
    Guid? DonViId;      // tenant ID
    Guid? IdChiNhanh;   // branch ID
    Guid? UserId;
    string? UserName;
    // ...
}
// Lấy từ JWT token, đọc qua CurrentUser ở BaseController
```

### Soft delete
- BaseEntity có `IsDeleted` — không xóa vật lý
- Query mặc định filter `IsDeleted = false`

### ResponseData (frontend)
```typescript
{ success: boolean, message: string, data: any, total: number }
// Luôn check res.success trước khi dùng res.data
```
