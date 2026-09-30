# SCRM — Service Architecture Map

> File này mô tả toàn bộ service, nhóm domain, và quan hệ giữa các service.
> Dùng như "bản đồ" để định hướng trước khi đọc features hoặc debug cross-service.

---

## Tổng quan

SCRM là hệ thống microservice ~40 service. Mỗi service có cấu trúc chuẩn:
```
D:/PROJECT/Git/scrm/{Module}/
├── {Module}.API/
│   ├── Controllers/     ← HTTP endpoints (route, authorization)
│   ├── Consumer/        ← MassTransit message consumers
│   └── Job/             ← Quartz scheduled jobs
├── {Module}.Service/
│   └── Generated/       ← Business logic (IXxxService, XxxService)
└── {Module}.DataContext/
    └── Entity/          ← Database entities (EF Core / SqlSugar)
```
DTOs dùng chung nằm ở `Libs/Models.Core/DTO/{Module}.Client/`

**Ví dụ tìm code theo tên class:**
- `DataReceiveCrmService` → `AccountingReceive/AccountingReceive.Service/Generated/DataReceiveCrmService.cs`
- `DataReceiveController` → `AccountingReceive/AccountingReceive.API/Controllers/DataReceiveController.cs`
- `DataReceiveCrm` (entity) → `AccountingReceive/AccountingReceive.DataContext/Entity/DataReceiveCrm.cs`

---

## Nhóm Domain

### 1. Auth & Core (nền tảng)
| Service | Mô tả |
|---------|-------|
| **Authen** | Xác thực nhân viên nội bộ |
| **AuthenCustomer** | Xác thực khách hàng (portal) |
| **AuthenPartner** | Xác thực đối tác |
| **Core** | User, Permission, DonVi (tenant), cấu hình hệ thống |
| **Branch** | Chi nhánh |

> Mọi service đều phụ thuộc Core để lấy thông tin DonVi, User, Permission.

---

### 2. Khách hàng & Lead
| Service | Mô tả | Entities chính |
|---------|-------|----------------|
| **Customer** | Hồ sơ KH, hạng, điểm, công nợ, hợp đồng, care history | KhachHang, DMHangKhachHang, KhachHangGhiNo, KhachHangHopDong, ConfigDoiDiem |
| **Contact** | Lead/liên hệ, chia sẻ care đơn | nhiều channel receive |
| **Booking** | Đặt lịch hẹn | — |
| **LogContact** | Log lịch sử tương tác với KH | — |

---

### 3. Đơn hàng (trung tâm hệ thống)
| Service | Mô tả | Entities chính |
|---------|-------|----------------|
| **Order** | Tạo/quản lý đơn hàng tại quầy, online | DonHang, DonHangSanPham, DonHangDichVu, DonHangKhuyenMai, DonHangPtThanhToan, CauHinh |
| **OrderLogistic** | Đơn giao hàng qua đối tác vận chuyển | DonHangLogistic, DMDoiTac, CauHinhChiaSoCareDon, DonHangLogisticNghiepVuCauHinh |
| **OrderProcess** | Rollback đơn, xử lý lỗi thanh toán online | OrderRollBack, OrderRollBackLogistic |
| **Deposit** | Đặt cọc đơn hàng | — |
| **CommissionFee** | Tính hoa hồng cho nhân viên/đối tác | — |
| **TrackMap** | Theo dõi lộ trình giao hàng thực tế | — |

---

### 4. Tài chính
| Service | Mô tả | Entities chính |
|---------|-------|----------------|
| **Invoice** | Hóa đơn điện tử, mẫu hóa đơn | Invoice, InvoiceTemplate, Account |
| **Accounting** | Kế toán, chứng từ, tài khoản, bút toán | PhieuBanHang, ThuChi, ButToan, DmTaiKhoanKeToan, BuTruCongNo |
| **AccountingReceive** | Nhận và sync dữ liệu kế toán từ các service khác | — |

---

### 5. Kho & Sản phẩm
| Service | Mô tả | Entities chính |
|---------|-------|----------------|
| **WarehouseProduct** | Kho, sản phẩm, dịch vụ, phiếu nhập/xuất, IMEI, combo, thẻ dịch vụ | SanPham, DichVu, Kho, KhoChiTiet, PhieuNhap, PhieuXuat, PhieuChuyenKho, SanPhamImei, ComBoSpa, TheDichVu |
| **SanXuat** | Quản lý sản xuất | — |
| **Promotion** | Khuyến mãi, voucher, hoa hồng giới thiệu | KhuyenMai, VoucherMa, HoaHongGioiThieu, DonHangCauHinhGiamGia |

---

### 6. Marketing & Kênh liên lạc
| Service | Mô tả |
|---------|-------|
| **MarketingCampaign** | Chiến dịch marketing |
| **SMS** | Gửi SMS |
| **Notification** | Thông báo in-app |
| **SocketServer** | Realtime (WebSocket) |
| **Switchboard** | Tổng đài điện thoại |
| **Facebook** | Quản lý page Facebook |
| **FacebookChat** | Chat Facebook Messenger |
| **FacebookWebhook / FacebookChatWebhook** | Nhận event từ Facebook |
| **TiktokWebhook** | Nhận event từ TikTok Shop |
| **ChiaSoChat** | Chia sẻ chat giữa nhân viên |
| **Contact (receive)** | ContactReceive, ContactReceiveChat, ContactReceiveFB — nhận lead từ nhiều kênh |

---

### 7. Đối tác & Tích hợp ngoài
| Service | Mô tả |
|---------|-------|
| **PartnerConnect** | Kết nối đối tác (API 3rd party) |
| **Website** | Website bán hàng |
| **WebhookBank** | Nhận webhook xác nhận thanh toán ngân hàng |
| **WebhookServer** | Server xử lý webhook chung |
| **Webhook_Ladi** | Nhận lead từ Ladi |
| **Webhook_Subiz** | Nhận lead từ Subiz |
| **LogEcommerce** | Log giao dịch thương mại điện tử |

---

### 8. Nội bộ & Vận hành
| Service | Mô tả |
|---------|-------|
| **HumanResource** | Nhân sự |
| **WorkFlow** | Công việc, dự án nội bộ |
| **Report** | Báo cáo tổng hợp |
| **SandBoxAI** | Thử nghiệm AI |
| **ClearData** | Xóa/reset data |

---

### 9. Infrastructure (dùng chung)
| Service/Lib | Mô tả |
|-------------|-------|
| **File** | Upload/download file |
| **GoogleDrive** | Tích hợp Google Drive |
| **ESDocument** | ElasticSearch indexing |
| **Log / LogService** | Log tập trung |
| **LogDelivery** | Log riêng cho giao hàng |
| **Libs/SB.Core** | Base classes, extensions, helpers dùng chung |
| **Libs/Models.Core** | DTOs dùng chung giữa tất cả service |
| **Libs/Core.ElasticSearch** | Client ES dùng chung |

---

## Quan hệ chính giữa các service

```
[Kênh ngoài]
  Facebook/Tiktok/Ladi/Subiz webhooks
  Switchboard / Contact receive
        ↓
  Contact ──────────────────────────→ Customer (chuyển lead thành KH)
                                           ↑
[Tạo đơn hàng]                            │ cập nhật điểm, hạng, lịch sử
  Order ──────────────────────────────────┘
    │
    ├──→ WarehouseProduct   (xuất kho)
    ├──→ Promotion          (áp khuyến mãi)
    ├──→ Invoice            (phát hành hóa đơn)
    ├──→ Accounting         (tạo chứng từ kế toán)
    ├──→ Notification       (thông báo)
    └──→ OrderLogistic      (tạo vận đơn giao hàng)
              │
              ├──→ TrackMap          (theo dõi lộ trình)
              └──→ OrderProcess      (rollback nếu lỗi)

[Finance flow]
  WebhookBank → AccountingReceive → Accounting
```

---

## Features chi tiết

### Đơn hàng & Khách hàng
| File | Nội dung |
|------|---------|
| [features/order.md](./features/order.md) | Đơn hàng tại quầy — entities, API, luồng thanh toán, hoa hồng NV |
| [features/order-logistic.md](./features/order-logistic.md) | Đơn giao hàng — COD, chi tiêu KH, chia care đơn |
| [features/order-process.md](./features/order-process.md) | Rollback đơn hàng — đồng bộ trạng thái với kho/KH/khuyến mãi/logistics |
| [features/customer.md](./features/customer.md) | Hồ sơ KH — hạng, điểm tích lũy, công nợ, chăm sóc |
| [features/deposit.md](./features/deposit.md) | Đặt cọc — phiếu cọc, hoàn cọc, gửi SMS/Zalo |
| [features/commission-fee.md](./features/commission-fee.md) | Hoa hồng cộng tác viên — tính theo sản phẩm/doanh số/chiến dịch |

### Kho & Sản phẩm
| File | Nội dung |
|------|---------|
| [features/warehouse.md](./features/warehouse.md) | Kho & sản phẩm — SP/DV/Combo/Thẻ, phiếu nhập/xuất/chuyển kho, IMEI |
| [features/gia-von-theo-ky.md](./features/gia-von-theo-ky.md) | Tính lại giá vốn theo kỳ — snapshot tháng, scope CN/Kho/SP, MassTransit job, modal trên `/quan-ly-ton-kho` |
| [features/reset-ton-kho.md](./features/reset-ton-kho.md) | Reset / rà soát tồn kho — 4 loại phiên (rà soát/scan/reset/tái tạo phiếu), engine bình quân gia quyền `ReplayGiaVon` (Anchor/Stream), cutover/delta |
| [features/promotion.md](./features/promotion.md) | Khuyến mãi, voucher, hoa hồng giới thiệu — điều kiện, cấp bậc, lần mua |
| [features/san-xuat.md](./features/san-xuat.md) | Sản xuất — BOM, lệnh sản xuất, lệnh công việc, xuất hàng |

### Tài chính & Kế toán
| File | Nội dung |
|------|---------|
| [features/invoice.md](./features/invoice.md) | Hóa đơn điện tử — phát hành async qua MISA/Viettel, InvoiceStatus |
| [features/accounting.md](./features/accounting.md) | Kế toán — PhieuBanHang, ButToan, BuTruCongNo, DongBoPhieuCrmConsumer |
| [features/accounting-receive.md](./features/accounting-receive.md) | Cầu nối CRM → Kế toán — dual-write, idempotency theo IdTransaction |
| [features/webhook-bank.md](./features/webhook-bank.md) | Xác nhận thanh toán ngân hàng — QR code (MB Bank), Zengi chi hộ |

### Auth & Nền tảng
| File | Nội dung |
|------|---------|
| [features/core.md](./features/core.md) | Multi-tenant — DonVi, ChiNhanh, User, Permission |
| [features/authen.md](./features/authen.md) | Xác thực nhân viên — JWT, RefreshToken, UserSession, 2FA |
| [features/branch.md](./features/branch.md) | Chi nhánh — PhongBan, HoaHong, ThuChi, LuongNhanVien |
| [features/authen-customer.md](./features/authen-customer.md) | Xác thực khách hàng (app B2C) — AES login, proxy sang service nghiệp vụ |
| [features/authen-partner.md](./features/authen-partner.md) | Xác thực đối tác — sandbox API, MongoDB audit log |

### Lead & Chăm sóc KH
| File | Nội dung |
|------|---------|
| [features/contact-lead.md](./features/contact-lead.md) | Lead — nhận từ Facebook/TikTok/form, chia lead cho sale, tác nghiệp |
| [features/booking.md](./features/booking.md) | Lịch hẹn — đặt lịch, phân công NV, thẻ dịch vụ, UTM tracking |
| [features/log-contact.md](./features/log-contact.md) | Log contact webhook — MongoDB, retry |

### Marketing & Liên lạc
| File | Nội dung |
|------|---------|
| [features/marketing-campaign.md](./features/marketing-campaign.md) | Chiến dịch SMS/Zalo — lọc KH, gửi định kỳ/lặp lại |
| [features/sms.md](./features/sms.md) | Gửi SMS/Zalo — multi-provider, OTP, retry, callback |
| [features/notification.md](./features/notification.md) | Push notification — Firebase FCM, device token, topic |
| [features/socket-server.md](./features/socket-server.md) | Real-time WebSocket — SignalR hubs: đơn hàng, import, ecommerce |
| [features/switchboard.md](./features/switchboard.md) | Tổng đài — hotline, lịch sử cuộc gọi, số dư |

### Webhook & Tích hợp ngoài
| File | Nội dung |
|------|---------|
| [features/facebook-webhook.md](./features/facebook-webhook.md) | Nhận event Facebook — verify, fanpage-server mapping, round-robin queue |
| [features/tiktok-webhook.md](./features/tiktok-webhook.md) | Nhận event TikTok — comment, video, order; round-robin 10 queues |
| [features/website.md](./features/website.md) | Website bán hàng B2C — combo/thẻ, UTM, sync sang Order |
| [features/partner-connect.md](./features/partner-connect.md) | Kết nối Bravo/TingTong — sync KH/đơn hàng/logistics |
| [features/track-map.md](./features/track-map.md) | Tìm kiếm bản đồ — autocomplete Track Asia API |
| [features/log-delivery.md](./features/log-delivery.md) | Log webhook giao hàng — MongoDB, filter theo ShippingUnitCode |
| [features/log-ecommerce.md](./features/log-ecommerce.md) | Log webhook TMĐT — MongoDB, filter theo EcomCode |

### Infrastructure & Nội bộ
| File | Nội dung |
|------|---------|
| [features/es-document.md](./features/es-document.md) | Elasticsearch indexing — 30+ consumers, batch 1000, reindex theo đơn vị |
| [features/human-resource.md](./features/human-resource.md) | Nhân sự — hồ sơ NV, ứng viên, tài liệu, ảnh |
| [features/report.md](./features/report.md) | Báo cáo — dashboard CEO, booking, doanh thu, AI report |
| [features/action-dashboard.md](./features/action-dashboard.md) | Action Dashboard — framework dashboard tự thiết kế: widget, nguồn dữ liệu, import Excel, filter/source definition |
