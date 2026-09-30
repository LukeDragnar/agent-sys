# OrderLogistic — Đơn Hàng Giao Hàng

> Quản lý đơn bán online/giao hàng qua đối tác vận chuyển (GHTK, GHN, v.v.).
> Khác với Order (bán tại quầy), OrderLogistic là đơn có địa chỉ giao + đối tác vận chuyển.

---

## Vai trò trong hệ thống

- ← **Order**: đơn logistic có thể được tạo song song hoặc từ đơn tại quầy
- ← **Facebook/Tiktok/Ladi/Subiz webhooks**: lead → tạo đơn logistic
- ← **Contact**: chuyển đổi lead thành đơn
- → **WarehouseProduct**: xuất kho
- → **TrackMap**: theo dõi lộ trình giao hàng
- → **Customer**: cập nhật chi tiêu khi đơn giao thành công / hoàn
- → **Accounting**: chứng từ COD, phí vận chuyển
- → **Invoice**: phát hành hóa đơn
- → **Notification**: thông báo trạng thái giao hàng
- ↔ **OrderProcess**: rollback khi lỗi thanh toán online

---

## Entities chính

### DonHangLogistic
| Field | Ý nghĩa |
|-------|---------|
| `MaDon` | Mã đơn nội bộ |
| `MaDonGiaoVan` | Mã vận đơn từ đối tác (GHTK/GHN...) |
| `MaDonEcomm` | Mã đơn từ sàn TMĐT |
| `GiaoHangTrangThaiMa` | Trạng thái giao hàng hiện tại |
| `UserIdCareDon` | Nhân viên đang care đơn này |
| `UserIdSale/Marketing/CSKH` | Phân công team |
| `DonHangIsDangDon` | Đang đón hàng tại kho |
| `DonHangTrangThaiChotDon` | Trạng thái chốt đơn |
| `KhoId` | Kho xuất hàng |
| `ChoXuatKhoTrangThai` | Trạng thái chờ xuất kho |
| `NhapHoanTrangThai` | Trạng thái nhập hoàn |
| `TrangThaiDoiSoat` | Đối soát COD |

**Giá/phí:**
- `DonHangGiaoHangShipFee` — phí ship thực (trả cho ĐTVV)
- `DonHangThuKhachShipFee` — phí ship thu khách
- `DonHangHoTroShipFee` — hỗ trợ phí ship (từ CauHinhCod)
- `DonHangDatCoc` — tiền đặt cọc

**Hoàn đơn:**
- `DonHangHoanMaDonGiaoVan` — mã vận đơn hoàn
- `DonHangHoanCOD` — tiền COD hoàn về
- `DonHangHoanTrangThai` — trạng thái hoàn

### NghiepVuPending / NghiepVuHistory (queue xử lý async)

| Table | Mục đích |
|-------|---------|
| `DonHangLogisticNghiepVuPending` | Hàng đợi chờ xử lý (job quét mỗi 5 phút) |
| `DonHangLogisticNghiepVuHistory` | Lịch sử đã xử lý (thành công hoặc thất bại) |
| `DonHangLogisticNghiepVuCauHinh` | Cấu hình: `TrangThaiTang` = list trạng thái tăng chi tiêu |

**Race condition protection (scale > 1 instance):**
- `IsProcessing` — đang được xử lý bởi 1 instance
- `ProcessingStartedAt` — thời điểm bắt đầu (cleanup nếu stuck > 5 phút)
- `ProcessingToken` — GUID unique per batch, dùng để SELECT đúng records sau UPDATE

**2-step atomic claim:**
```csharp
var token = Guid.NewGuid();
await _context.Database.ExecuteSqlRawAsync(@"
    UPDATE TOP(100) p SET p.IsProcessing=1, p.ProcessingStartedAt=GETDATE(), p.ProcessingToken={0}
    FROM dbo.DonHangLogisticNghiepVuPending p
    WHERE p.IsProcessing=0 AND p.IsDelete=0 AND p.ThoiGianXuLy<={1}", token, now);
var pendingList = await _reposPending.TableWithNolock()
    .Where(x => x.ProcessingToken == token).ToListAsync();
```

**Stuck cleanup:** Job quét mỗi 5 phút — nếu `IsProcessing=1` nhưng `ProcessingStartedAt < now-5min` → reset hoặc MoveToHistory lỗi (tối đa 3 lần `SoLanThuLai`).

### DonHangLogisticChiTieuLog (audit chi tiêu)
| Field | Ý nghĩa |
|-------|---------|
| `Huong` | 1 = tăng chi tiêu, -1 = giảm |
| `SoTien` | Số tiền (luôn dương) |
| `TriggerEvent` | `UPDATE_STATUS` hoặc `DOISOAT` |
| `TrangThaiGiaoHang` | Trạng thái tại thời điểm xử lý |
| `IdTransaction` | Dùng để rollback khi giảm |

### CauHinhChiaSoCareDon (cấu hình chia care đơn)
Hệ thống tự động assign `UserIdCareDon` cho mỗi đơn dựa trên:
- **Kiểu 1**: Chia theo nhóm user + định mức ngày
- **Kiểu 2**: Liên kết nhóm Sale ↔ nhóm Care, có thể chia theo sản phẩm

### QueueDonHangChiaCareDon (queue chia)
Background job đọc bảng này để thực hiện chia care đơn với độ trễ (`SoPhutCauHinhChiaSau`).

---

## API Endpoints chính

**Base URL:** `{SERVICE_API_ORDERLOGISTIC}api/DonHangLogistic`

| Endpoint | Mô tả |
|----------|-------|
| `TaoDonLogisticDto` | Tạo đơn mới |
| `CapNhatDonLogisticDto` | Cập nhật đơn |
| `GetChiTietDon` | Chi tiết đơn |
| `ChotDon` | Chốt đơn (gửi đối tác vận chuyển) |
| `HuyChotDon` | Hủy chốt |
| `InDon` | In phiếu giao hàng |
| `Xoa` | Xóa đơn |
| `TimTheoDieuKien` | Tìm kiếm đơn |
| `CapNhatSaleUserId` | Cập nhật nhân viên sale |

**Base URL:** `{SERVICE_API_ORDERLOGISTIC}api/ChiaSoCareDon`

| Endpoint | Mô tả |
|----------|-------|
| `ChiaNhomCareDon_PhanBoNgay` | Phân bổ ngay về nhóm |
| `ChiaUserCareDon_PhanBoNgay` | Phân bổ ngay về user |
| `ChiaUserCareDon_PhanBoVeUser` | Phân bổ hàng loạt |
| `GetLogChiaSoByIdDonHang` | Log lịch sử chia |

---

## Luồng nghiệp vụ

### 1. Tạo đơn → Giao hàng → Chi tiêu khách
```
TaoDonLogisticDto
  ↓
ChotDon → gửi thông tin sang đối tác vận chuyển (GHTK/GHN)
  ↓
Đối tác callback cập nhật GiaoHangTrangThaiMa
  ↓
TaoNghiepVuPendingAsync() → insert DonHangLogisticNghiepVuPending
  ↓ (sau PhutDelay phút)
JobXuLyChiTieuKhachHang (chạy mỗi 5 phút)
  ├─ Claim batch bằng ProcessingToken (atomic UPDATE + SELECT)
  ├─ XuLyStuckPendingAsync() — cleanup stuck > 5 phút
  ├─ TrangThaiTang → CustomerUpdate.TangChiTieu()
  │      → tăng SoTienTichLuyLogistic
  └─ Trạng thái khác → CustomerUpdate.GiamChiTieu()
         → giảm (rollback bằng IdTransaction)
```

**Rule quan trọng:** Mọi thay đổi chi tiêu OrderLogistic đều phải đi qua NghiepVuPending — **không gọi CustomerUpdate trực tiếp** từ API/consumer.

### 2. Chia care đơn tự động
```
Đơn đạt trạng thái giao hàng nhất định
  ↓
Tạo record QueueDonHangChiaCareDon (có delay SoPhutCauHinhChiaSau)
  ↓
Background job quét Queue
  ↓
ChiaSoCareDonService.ChiaSoCareDon()
  ├→ Kiểm tra CauHinhChiaSoCareDon của chi nhánh
  ├→ Xác định user/nhóm nhận (dựa trên định mức DinhMucChiaSo)
  ├→ Update DonHangLogistic.UserIdCareDon
  └→ Ghi ChiaSoCareDonDayCount (tracking định mức)
```

### 3. Đối soát COD
```
Đối tác vận chuyển chuyển tiền COD về
  ↓
Cập nhật TrangThaiDoiSoat
  ↓
TaoNghiepVuPendingAsync() (TriggerEvent = DOISOAT)
  ↓ (qua job, không gọi CustomerUpdate trực tiếp)
JobXuLyChiTieuKhachHang
  └→ tăng/giảm chi tiêu theo kết quả đối soát
```

---

## Business logic đặc biệt

### Chi tiêu khách hàng — qua NghiepVuPending
**Luồng:** tất cả thay đổi chi tiêu → `TaoNghiepVuPendingAsync()` → job xử lý async.

**Anti-double protection:**
- Mỗi lần xử lý, kiểm tra `lastLog.Huong` với hướng hiện tại
- Nếu cùng hướng → skip (tránh cộng/trừ 2 lần)
- Nếu đổi hướng (tăng → giảm) → thực hiện rollback

**Khi tăng:** lưu `IdTransaction` trong log → dùng để rollback sau
**Khi giảm:** gọi Customer service với `IdTransaction` cũ để đảo ngược; rollback cả `KhachHangGhiNo` lẫn `KhachHangGhiNoDetail`

### ONLINE / OFFLINE trên FE (KhachHangDetail)
- **ONLINE** = `soTienTichLuyLogistic` (chỉ tích lũy, không cộng nợ)
- **OFFLINE** = `soTienTichLuy` (chỉ tích lũy, không cộng nợ)
- `soTienNoDonHangLogistic` và `soTienNoDonHang` là field nợ riêng, không gộp vào tổng chi tiêu

### Hỗ trợ phí ship COD (CauHinhCod)
- Nếu giá trị đơn ≥ `GiaTriTroLen` → hỗ trợ `GiamGiaGiaTri` cho phí ship
- `KieuLoai` xác định cách tính hỗ trợ (enum `EnumKieuLoaiHoTroPhiShip`)

### Chia care đơn định mức
- Mỗi user/nhóm có `DinhMucChiaSo` đơn/ngày
- `ChiaSoCareDonDayCount` theo dõi số đơn đã nhận trong ngày
- Khi `Count >= WaveLimit` → chuyển sang user/nhóm tiếp theo
- Reset theo `InitNumber` (cấu hình trong CauHinhChiaSoCareDon)

---

## Checklist fix bug

- [ ] Chi tiêu KH không cập nhật → kiểm tra `DonHangLogisticChiTieuLog`, xem `TriggerEvent` + `Huong`
- [ ] Chi tiêu bị cộng/trừ 2 lần → anti-double không hoạt động, kiểm tra `lastLog`
- [ ] Care đơn không được chia → kiểm tra `QueueDonHangChiaCareDon.IsCompleted`, xem log background job
- [ ] Chia sai user → kiểm tra `DinhMucChiaSo` + `ChiaSoCareDonDayCount.Count`
- [ ] COD không đối soát được → kiểm tra `TrangThaiDoiSoat`, `GiaoHangTrangThaiMa`
- [ ] Đơn chốt lỗi → kiểm tra `DonHangTrangThaiChotDon`, xem log gọi API đối tác
