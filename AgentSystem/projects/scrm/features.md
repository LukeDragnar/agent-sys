# SCRM — Mô tả chức năng (Maintenance Guide)

> File này mô tả CHI TIẾT từng chức năng: màn hình làm gì, luồng nào, API nào, bug-prone ở đâu.
> Dùng khi maintain, fix bug, hoặc thêm tính năng mới.

---

## MODULE: WorkFlow

### Tổng quan
WorkFlow quản lý công việc nội bộ doanh nghiệp. Gồm 3 tầng:
- **Dự án (DuAn)** — nhóm các công việc liên quan
- **Công việc (CongViec)** — task cụ thể, có người phụ trách
- **Báo cáo (CongViecBaoCao)** — thống kê tổng quan / theo người / theo mức độ

---

### [Màn hình] Danh sách Công việc (`/work/cong-viec`)

**File:** `scrm_frontend/src/app/pages/work/cong-viec/cong-viec.component.*`

**Layout:** 2 cột
- Cột trái: Panel **Dự án** (`app-du-an`) — có thể collapse
- Cột phải: Danh sách công việc của dự án đang chọn

**Luồng chính:**
1. Load danh sách dự án khi mở màn hình
2. User chọn 1 dự án → load danh sách công việc thuộc dự án đó
3. User search keyword → filter local trên `listDuAnTemp`
4. Nút collapse panel trái → ẩn panel dự án, hiện icon expand

**State hiện tại:** Cột phải chưa implement (`ádasdas` placeholder) — đây là điểm cần develop thêm

**Lưu ý bug-prone:**
- Search dự án filter **local** (không gọi API) → nếu list dự án lớn cần chuyển sang server-side
- `isCollapseMenu` dùng `ngStyle display:block/none` thay vì `*ngIf` → DOM vẫn render, tốn memory

---

### [Màn hình] Danh sách Dự án (`app-du-an` component)

**File:** `scrm_frontend/src/app/pages/work/du-an/du-an.component.*`

**Là component con** của CongViec screen, có `@Output() isCollapseMenu`

**Chức năng:**
| Action | Method | API |
|--------|--------|-----|
| Load danh sách | `getDuAn()` | `POST api/DuAn/GetListDuAnByUserLogin` |
| Tìm kiếm | `onSearchDuAn(keyword)` | Filter local trên `listDuAnTemp` |
| Thêm mới | `onEdit(null)` | Mở modal `DuAnEditComponent` |
| Chỉnh sửa | `onEdit(id)` | Mở modal `DuAnEditComponent` với id |
| Xóa | `onDelete(id)` | `POST api/DuAn/Delete/{id}` |
| Collapse panel | `onCollapse()` | Emit event lên parent |

**Modal Edit:**
- Width: 800px
- Component: `DuAnEditComponent`
- Sau close: reload list + re-apply search filter

**Lưu ý bug-prone:**
- `onDelete` sau khi xóa gọi `getDuAn()` + `onSearchDuAn()` — nếu keyword đang có giá trị thì filter đúng, nhưng nếu vừa xóa dự án đang chọn (`duAnSelected`) thì cột phải sẽ hiển thị data cũ

---

### [API] CongViec

**Base URL:** `{SERVICE_API_WORK}api/CongViec`

| Endpoint | Method | Body | Mô tả |
|----------|--------|------|-------|
| `TimTheoDieuKien` | POST | `CongViecFilterDto` | List + filter + paging |
| `TimTheoId/{id}` | POST | — | Chi tiết 1 công việc |
| `LayDanhSachUser` | POST | — | Danh sách user để assign |
| `Save` | POST | `CongViecSaveDto` | Tạo mới (Id null) hoặc cập nhật (Id có giá trị) |
| `Xoa/{id}` | POST | — | Xóa (soft delete) |

**CongViecFilterDto:**
```
Keyword     — tìm theo TieuDe (LIKE)
TrangThai   — 0=ChuaLam, 1=DangLam, 2=HoanThanh, 3=Huy (null = tất cả)
PageInfo    — { page, pageSize }
```

**CongViecSaveDto:**
```
Id               — null = tạo mới, có giá trị = cập nhật
TieuDe           — bắt buộc
MoTa             — tùy chọn
TrangThai        — 0/1/2/3
IdMucDoUuTien    — tùy chọn
NgayBatDauDuKien — tùy chọn
NgayKetThucDuKien — tùy chọn
ListIdUser       — danh sách Guid user được assign (replace toàn bộ)
```

**Business logic khi đổi TrangThai (tự động trong `LuuAsync`):**
```
→ DangLam (1):    NgayBatDauThucTe = now,  NgayKetThucThucTe = null
→ HoanThanh (2):  NgayKetThucThucTe = now, NgayBatDauThucTe = now nếu chưa có
→ Huy (3):        NgayKetThucThucTe = now, NgayBatDauThucTe = now nếu chưa có
→ ChuaLam (0):    NgayBatDauThucTe = now nếu chưa có, NgayKetThucThucTe = null
```

**Lưu ý bug-prone:**
- `ListIdUser` là **replace**, không phải append — nếu FE gửi list rỗng hoặc null thì toàn bộ assignment bị giữ nguyên (`if (dto.ListIdUser != null)`)
- Query `TimTheoDieuKien` filter `IdDonVi = userLogin.DonViId` — nếu `userLogin` null sẽ lấy toàn bộ data
- `GroupJoin` với `DmMucDoUuTien` trong query list — nếu `IdMucDoUuTien` không tồn tại trong DM thì `TenMucDoUuTien = null` (không báo lỗi)

---

### [API] CongViecBaoCao

**Base URL:** `{SERVICE_API_WORK}api/CongViecBaoCao`

| Endpoint | Trả về | Mô tả |
|----------|--------|-------|
| `TongQuan` | `BaoCaoTongQuanDto` | Tổng số: ChuaLam, DangLam, HoanThanh, Huy, QuaHan |
| `TheoNguoiDung` | `List<BaoCaoTheoNguoiDungDto>` | Thống kê theo từng user được assign |
| `TheoMucDoUuTien` | `List<BaoCaoTheoMucDoUuTienDto>` | Thống kê theo mức độ ưu tiên |

**Định nghĩa QuaHan:** `NgayKetThucDuKien < now` AND `TrangThai ∉ {HoanThanh, Huy}`

---

### [API] DuAn

**Base URL:** `{SERVICE_API_WORK}api/DuAn`

| Endpoint | Mô tả |
|----------|-------|
| `GetListDuAnByUserLogin` | Lấy tất cả dự án của DonVi hiện tại |
| `GetById/{id}` | Chi tiết dự án |
| `Create` | Tạo mới |
| `Update` | Cập nhật |
| `Delete/{id}` | Xóa |

---

### [API] DmMucDoUuTien (Danh mục mức độ ưu tiên)

**Base URL:** `{SERVICE_API_WORK}api/DmMucDoUuTien`

Fields: `TenMucDo`, `MaCode`, `MoTa`, `MauSac` (hex color), `ThuTu` (sort order)

---

## Cấu trúc file DTO (Libs/Models.Core)

DTOs dùng chung giữa service và các module khác:
```
Libs/Models.Core/DTO/WorkFlow.Client/
├── CongViec/
│   ├── CongViecDto.cs          ← List item
│   ├── CongViecDetailDto.cs    ← Detail (có ListUser đầy đủ)
│   ├── CongViecFilterDto.cs    ← Search params
│   ├── CongViecSaveDto.cs      ← Create/Update
│   ├── BaoCaoCongViecDto.cs    ← Report result
│   └── BaoCaoCongViecFilterDto.cs ← Report filter
├── CongViecUser/
├── DmMucDoUuTien/
└── User/
```

---

## Checklist khi fix bug WorkFlow

- [ ] Bug liên quan list không load → kiểm tra `userLogin.DonViId` có null không
- [ ] Bug assign user bị mất → kiểm tra `ListIdUser` trong request có đúng không (null vs empty list)
- [ ] Bug ngày không update → xem logic tự động set NgayBatDau/KetThucThucTe trong `LuuAsync`
- [ ] Bug search không ra kết quả → check `Keyword` filter (case-insensitive Contains)
- [ ] Bug sau xóa dự án vẫn thấy công việc cũ → `duAnSelected` chưa được clear ở FE
- [ ] Bug paging sai → kiểm tra `GridInfo.Page` bắt đầu từ 1, skip = `(page-1)*pageSize`

---

---

## FEATURE: TenVietTat — Tên viết tắt sản phẩm

### Tổng quan

Cho phép cấu hình tên viết tắt / tên rút gọn cho sản phẩm, áp dụng khi in phiếu giao vận hoặc đăng đơn lên đơn vị vận chuyển. Tên gốc `TenSanPham` không thay đổi trong màn hình đơn hàng.

### Entities thay đổi

| Entity | Trường thêm |
|--------|------------|
| `SanPham`, `SanPhamCha` | `TenVietTat` (NVARCHAR 255), `SuDungTenVietTat` (int flags) |
| `DonHangLogisticSanPham` | `TenVietTat` (snapshot), `SuDungTenVietTat` (snapshot) |

### Flags enum

```csharp
[Flags]
public enum TenVietTatSuDungEnum
{
    KhongSuDung    = 0,
    InPhieuGiaoVan = 1 << 0,  // = 1 — in phiếu giao vận
    GiaoHang       = 1 << 1,  // = 2 — đăng đơn lên đơn vị vận chuyển
    // Thêm loại mới: 1 << 2 (=4), 1 << 3 (=8), ...
}
```

**Lưu ý:** bắt buộc dùng lũy thừa 2. `3 = 1|2` = cả InPhieuGiaoVan + GiaoHang.

### Snapshot

Khi tạo đơn (`MapDonHangSp`), `TenVietTat` + `SuDungTenVietTat` được copy từ `SanPham` vào `DonHangLogisticSanPham`. Từ đó dùng snapshot, không đọc lại catalog.

### Extension method

```csharp
// Dùng trên cả DonHangLogisticSanPham và SanPhamDto
item.GetTenHienThi(TenVietTatSuDungEnum.GiaoHang)
// → trả TenVietTat nếu flag bật, ngược lại trả TenSanPham/Ten
```

### Luồng áp dụng

| Luồng | File | Flag dùng |
|-------|------|-----------|
| Đăng đơn vận chuyển | `OrderService.GenerateProduct` | `GiaoHang` |
| Tính phí vận chuyển | `OrderService.MapDataReqCreateOrder` (isTinhPhi=true) | `GiaoHang` |
| In phiếu giao vận (mau1-10) | `DonHangService.GetChiTietNhieuDon` | `InPhieuGiaoVan` |

### Tính phí — lưu ý quan trọng

`TinhPhiVc` không load đơn từ DB — phải truyền `IdDonHang` từ FE để BE lookup snapshot. FE phải truyền cả `IdSanPham` trong từng item `DonHangSanPhamInfos`.

**Bug-prone:**
- Đơn tạo trước khi deploy feature: snapshot `TenVietTat = NULL` → fallback về `TenSanPham` (đúng)
- Nếu `IdDonHang` không truyền → `donHangLogistic.Id = Guid.Empty` → query snapshot rỗng → không áp dụng tên viết tắt
- Nếu `IdSanPham` không truyền trong `DonHangSanPhamInfos` → không match được snapshot

### UI (FE)

Tab **Thông tin nâng cao** trong `edit-sanpham.component`:
- Input text: Tên viết tắt
- Checkbox: `In phiếu giao vận` (flag=1), `Giao hàng` (flag=2)

---

## Template: Cách thêm module mới vào file này

```markdown
## MODULE: TênModule

### Tổng quan
[Mô tả module làm gì]

### [Màn hình] TênMànHình
**File:** `...`
**Chức năng:** [bảng action → method → API]
**Lưu ý bug-prone:** [...]

### [API] TênEntity
**Base URL:** `...`
[bảng endpoint]
[mô tả business logic đặc biệt]

### Checklist khi fix bug
- [ ] ...
```
