# Contact & Lead — Tiếp Nhận và Phân Phối Lead

> Quản lý toàn bộ vòng đời của lead: tiếp nhận từ nhiều kênh → phân phối cho sale → tác nghiệp → chuyển đổi thành khách hàng.

---

## Vai trò trong hệ thống

- ← **Facebook/FacebookChat**: nhận lead từ comment, messenger
- ← **TiktokWebhook**: nhận lead từ TikTok Shop
- ← **Webhook_Ladi/Subiz**: nhận từ form/live chat
- ← **Website**: nhận từ form đăng ký
- → **Customer**: chuyển đổi lead thành khách hàng khi chốt đơn
- → **OrderLogistic**: tạo đơn giao hàng từ lead
- → **Notification/SMS**: thông báo cho nhân viên được phân bổ
- → **SocketServer**: realtime notify khi có lead mới

---

## Các sub-service

| Service | Vai trò |
|---------|---------|
| **Contact.API** | CRUD contact, tác nghiệp, quản lý lead |
| **ContactReceive.API** | Nhận lead từ form/landing page |
| **ContactReceiveFB.API** | Nhận lead từ Facebook |
| **ContactReceiveChat.API** | Nhận lead từ chat (Subiz, live chat) |
| **ChiaSo.API** | Engine phân phối lead cho sale |
| **ContactConsumer.API** | Xử lý queue messages (RabbitMQ) |

---

## Entities chính

### Contact (lead chính)
| Field | Ý nghĩa |
|-------|---------|
| `KhachHangSoDienThoai` | Số điện thoại (key định danh) |
| `KhachHangTen` | Tên |
| `KhachHangTinNhan` | Nội dung liên hệ/tin nhắn |
| `Status` | Trạng thái: new / assigned / converted... |
| `SaleUserId` | Nhân viên sale được phân bổ |
| `SaleNgayNhanData` | Ngày nhận lead |
| `IsKhachCu` | Lead là khách hàng cũ |
| `IsSoTrung` | Số điện thoại trùng với lead khác |
| `IdLichHen` | Lịch hẹn đặt từ lead |
| `KhoSoIsDaVaoKho` | Đã vào kho sơ (archive) |
| UTM fields | Tracking nguồn quảng cáo |

**Tracking đơn hàng logistics (prefix `Lgt`):**
- `LgtIdDonHang`, `LgtMaDon` — liên kết đơn OrderLogistic
- `LgtGiaoHangTrangThaiMa` — trạng thái giao hàng
- `LgtTrangThaiDoiSoat` — đối soát COD

**Workflow tác nghiệp:**
- `SaleTacNghiepCanId/Ten` — tác nghiệp đang cần làm
- `SaleTacNghiepKetQuaId/Ten` — kết quả tác nghiệp
- `SaleTacNghiepTiepId` — tác nghiệp tiếp theo
- `SaleTacNghiepSauBaoLauSoPhut` — thời gian chờ tác nghiệp tiếp

### DMTacNghiep (danh mục tác nghiệp)
Định nghĩa các bước trong workflow sale:
- `Ten` — tên (vd: "Gọi tư vấn", "Demo sản phẩm", "Chốt đơn")
- `IsKhoiDau` — tác nghiệp khởi đầu (auto assign khi nhận lead)
- `IsKhoSo` — kết thúc bằng đưa vào kho sơ
- `KhoSoThoiGian` — thời gian trước khi vào kho sơ (phút)
- `ListIdNhanKhachHang` — auto gắn nhãn KH khi chọn tác nghiệp này

### KhoSo (kho sơ / archive)
Lead hết hạn tác nghiệp mà chưa chốt → chuyển vào KhoSo:
- CSKH team sẽ nhận từ KhoSo để chăm sóc lại
- `KhoSoCount` đếm số lần vào kho sơ

### CauHinhChiaSo (cấu hình phân phối lead)
Engine chia lead dựa trên nhiều tiêu chí:

**Loại contact (8 loại):**
| Mã | Ý nghĩa |
|----|---------|
| SM | Số mới |
| SMT | Số mới từ TikTok |
| SKC | Số khách cũ |
| SKCT | Số khách cũ từ TikTok |
| SCSKH | CSKH cấp 1 |
| SCSKHL2 | CSKH cấp 2 |
| STMDT | Số từ TMĐT |
| SVRF | Số verified |

**Điều kiện chia:**
- `ApDungCaLamViec` — chỉ chia khi sale đang trong ca làm việc
- `ApDungChiaLeadTheoNhomSaleTuDong` — chia tự động theo nhóm
- `ApDungChiaLeadTheoSaleTuDong` — chia tự động theo từng người
- `ApDungChiaLeadNhomSaleTheoSanPham` — chia theo sản phẩm

### ChiaSoDayCountV2 (tracking định mức ngày)
Đếm số lead đã chia mỗi ngày theo từng loại:
- `WaveCount`, `Count` — số wave và tổng số chia
- `FallbackCount` — số lần fallback (sale không nhận)
- `LastReceive` — lần cuối nhận lead
- Reset hàng ngày theo cấu hình

---

## API Endpoints chính

**Contact.API — `{SERVICE_API_CONTACT}api/contact`**

| Endpoint | Mô tả |
|----------|-------|
| `TimTheoDieuKienAsync` | Tìm kiếm lead |
| `ChiTietContact/{id}` | Chi tiết lead |
| `CapNhatContact/{id}` | Cập nhật thông tin |
| `CapNhatKetQuaTacNghiep` | Ghi kết quả tác nghiệp |
| `CapNhatChuyenTacNghiep` | Chuyển sang tác nghiệp tiếp |
| `CapNhatNhanKhachHang` | Gắn nhãn KH |
| `CreateContactCSKH` | Tạo contact CSKH |

---

## Luồng nghiệp vụ

### 1. Nhận lead từ Facebook
```
Facebook gửi webhook event
  ↓
FBDataWebhook lưu payload (JSON thô)
  ↓
RabbitMQ Consumer xử lý:
  ├→ Parse phone/name/message
  ├→ Tạo Conversation (MongoDB) — lưu thread chat
  └→ Gọi ContactReceiveFB.API
       ↓
       Kiểm tra IsSoTrung (số đã có chưa?)
       ├─ Trùng → link với contact cũ (IdContactTrung)
       └─ Mới → tạo Contact mới, Status = "new"
            ↓
            Đẩy vào ChiaSo queue
```

### 2. Phân phối lead cho sale (ChiaSo)
```
Contact mới vào queue
  ↓
ChiaSoConsumer xử lý:
  ├→ Xác định loại (SM/SKC/SCSKH...)
  ├→ Kiểm tra CauHinhChiaSo của chi nhánh
  ├→ Kiểm tra ca làm việc (nếu cấu hình)
  ├→ Chọn sale theo định mức (ChiaSoDayCountV2)
  │     → Chọn sale có Count thấp nhất
  │     → Nếu không ai nhận → FallbackCount++
  └→ Gán SaleUserId + SaleNgayNhanData
       ↓
       Notify realtime qua SocketServer
```

### 3. Tác nghiệp sale
```
Sale nhận lead → tác nghiệp đầu (IsKhoiDau)
  ↓
Gọi điện / chat / demo
  ↓
CapNhatKetQuaTacNghiep()
  ├─ KetQua = "Chốt đơn" → tạo Contact status = converted
  │     └→ Tạo OrderLogistic hoặc Customer
  ├─ KetQua = "Hẹn lại" → tạo DMTacNghiep tiếp + set thời gian
  └─ KetQua = "Không liên lạc được" → sau N phút → vào KhoSo
```

### 4. Kho sơ → CSKH
```
Lead trong KhoSo
  ↓
CSKH team nhận (loại SCSKH)
  ↓
Chăm sóc thêm, thử chốt lại
  ↓
Nếu vẫn không → SCSKHL2 (cấp 2)
```

---

## Business logic đặc biệt

**Phát hiện trùng số (`IsSoTrung`):**
- Khi nhận lead mới, check số điện thoại đã tồn tại trong hệ thống chưa
- Nếu trùng → không tạo contact mới, link vào `IdContactTrung`
- Tránh sale nhận 2 lần cùng 1 khách

**Chia lead theo định mức (ChiaSoDayCountV2):**
- Mỗi loại contact (SM/SKC...) có counter riêng
- `WaveCount`: chia vòng tròn (round-robin)
- `FallbackCount`: khi sale không online/không trong ca → chuyển tiếp
- Reset mỗi ngày theo `InitNumber`

**Kho sơ:**
- Lead vào kho sơ sau `KhoSoThoiGian` phút không có tác nghiệp
- `KhoSoCount` tăng mỗi lần vào kho (theo dõi KH khó chốt)
- Sau N lần vào kho → có thể auto chuyển sang SCSKHL2

---

## Checklist fix bug

- [ ] Lead không được phân bổ → kiểm tra `CauHinhChiaSo`, xem `ApDungCaLamViec`
- [ ] Sale nhận cùng 1 lead 2 lần → kiểm tra `IsSoTrung`, `IdContactTrung`
- [ ] Webhook Facebook không tạo contact → xem `FBDataWebhook.IsProcessing`, check consumer log
- [ ] Lead mất → kiểm tra `KhoSo`, tìm theo `IdContact`
- [ ] Định mức chia không reset → kiểm tra `ChiaSoDayCountV2`, xem background job reset
- [ ] Tác nghiệp không chuyển tiếp → kiểm tra `SaleTacNghiepTiepId`, `SaleTacNghiepSauBaoLauSoPhut`
