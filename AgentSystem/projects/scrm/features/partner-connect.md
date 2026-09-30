# PartnerConnect — Kết Nối Hệ Thống Bên Ngoài

> Quản lý kết nối và đồng bộ dữ liệu với các hệ thống ERP/CRM bên ngoài: Bravo (ERP), TingTong (CRM). Hỗ trợ sync khách hàng, đơn hàng, logistics sang partner.

---

## Vai trò

- → **Bravo ERP**: sync khách hàng, đơn hàng, logistics, tồn kho
- → **TingTong CRM**: kết nối tài khoản, tạo lead
- ← **Order / OrderLogistic / Customer**: nhận event để sync
- → **Redis**: cache cấu hình kết nối
- → **MongoDB**: log mọi kết nối

---

## Entities chính

| Entity | Ý nghĩa |
|--------|---------|
| `CauHinhKetNoiPartnerConnect` | Cấu hình kết nối: `PartnerConnectType`, `Username`, `Password`, `AccessToken`, `RefreshToken`, `ExpireToken`, `IsSuDung` |
| `CauHinh` | Cấu hình chung của đơn vị: `JsonSetting`, `JsonSetting_SuperAdmin` |
| `ConfigSystem` | Cấu hình hệ thống: `Ma`, `Ten`, `GroupCode`, `Value` |
| `LogPartnerConnect` (MongoDB) | Log tất cả request/response với partner |

**PartnerConnectType:** Bravo=1, TingTong=2, ...

---

## API Endpoints

| Endpoint | Mô tả |
|----------|-------|
| `POST /api/cauhinhketnoisandbox/InsertOrUpdateAsync` | Thêm/cập nhật cấu hình kết nối |
| `GET /api/cauhinhketnoisandbox/TimTheoDieuKien` | Tìm kiếm cấu hình |
| `GET /api/cauhinhketnoisandbox/GetCauHinhTingTong` | Lấy cấu hình TingTong |
| `POST /api/cauhinhketnoisandbox/ConnectTingTongAsync` | Kết nối TingTong |
| `POST /api/cauhinhketnoisandbox/TingTongLead` | Tạo lead trên TingTong |
| `GET /api/cauhinhketnoisandbox/GetCfgBravoAsync` | Lấy cấu hình Bravo |
| `POST /api/cauhinhketnoisandbox/ConnectBravoAsync` [AllowAnonymous] | Kết nối Bravo |
| `POST /api/cauhinhketnoisandbox/SaveCfgBravoAsync` | Lưu cấu hình Bravo |
| `POST /api/cauhinhketnoisandbox/GetItemAsync` | Lấy danh mục hàng từ Bravo |
| `POST /api/cauhinhketnoisandbox/GetWarehouseAsync` | Lấy kho từ Bravo |
| `POST /api/cauhinhketnoisandbox/GetInventoryAsync` | Lấy tồn kho từ Bravo |
| `POST /api/CauHinh/SinhMaTokenAPI` | Sinh API token cho đơn vị |

---

## Luồng

```
[Kết nối Bravo]
Admin lưu CauHinhKetNoiPartnerConnect (Username/Password Bravo)
  ↓
ConnectBravoAsync() → gọi Bravo auth API → lấy AccessToken/RefreshToken
  └→ Lưu token + ExpireToken

[Sync dữ liệu sang Bravo (event-driven)]
Order service tạo đơn → publish event
  ↓ (MassTransit)
DonHang_SyncToBravoConsumer
  ├→ Lấy CauHinhKetNoiPartnerConnect của đơn vị
  ├→ Check token còn hạn, refresh nếu cần
  └→ Gọi Bravo API để tạo đơn bên Bravo

Tương tự cho:
  KhachHang_SyncToBravoConsumer    → sync KH
  DonHangLogistic_SyncToBravoConsumer → sync logistics
  DonHangLogisticTrangThai_SyncToBravoConsumer → cập nhật trạng thái

[Kết nối TingTong]
Admin POST ConnectTingTongAsync → xác thực TingTong
  ↓
TingTongLead() → tạo lead trên hệ thống TingTong

[Cấu hình API token]
Admin: SinhMaTokenAPI() → tạo API token cho đơn vị
  └→ Lưu vào CauHinh.JsonSetting
```

---

## Consumers (MassTransit)

| Consumer | Mô tả |
|----------|-------|
| `DonHang_SyncToBravoConsumer` | Sync đơn hàng mới sang Bravo |
| `DonHangLogistic_SyncToBravoConsumer` | Sync đơn logistics sang Bravo |
| `DonHangLogisticTrangThai_SyncToBravoConsumer` | Cập nhật trạng thái logistics trên Bravo |
| `KhachHang_SyncToBravoConsumer` | Sync khách hàng sang Bravo |
| `SendTacNghiepCanConsumer` | Gửi tác nghiệp sang đối tác |

---

## Đặc điểm quan trọng

- **Token management**: tự động refresh AccessToken khi hết hạn (`ExpireToken`)
- **Redis cache**: key `CK_CauHinhPartnerConnectSetting_{idDonVi}` — invalidate khi update
- **Change tracking**: deep clone setting cũ/mới để log `LichSuTacDong`
- **Multi-partner**: cùng 1 đơn vị có thể kết nối Bravo + TingTong đồng thời
- Config API token (`SinhMaTokenAPI`) để đối tác gọi ngược lại SCRM

---

## Checklist fix bug

- [ ] Sync Bravo không chạy → kiểm tra `AccessToken` có hết hạn, xem `LogPartnerConnect` MongoDB
- [ ] Consumer không nhận event → kiểm tra kết nối RabbitMQ, consumer registration
- [ ] Token Bravo hết hạn → trigger ConnectBravoAsync để refresh
- [ ] TingTong lead lỗi → kiểm tra cấu hình TingTong trong `CauHinhKetNoiPartnerConnect`
