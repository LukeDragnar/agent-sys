# Pushsale — Domain & Nghiệp vụ

> Entities, khái niệm và luồng nghiệp vụ chính. Thuật ngữ tiếng Việt không dấu theo codebase.

## Khái niệm trung tâm

| Thuật ngữ | Ý nghĩa |
|---|---|
| **Đơn vị** (`id_don_vi`) | Tenant — một doanh nghiệp/shop dùng Pushsale. Ranh giới cô lập dữ liệu. Một **Tổng công ty** (Corporation) có nhiều đơn vị/chi nhánh. |
| **Contact** | Đối tượng lõi của LD: vừa là **lead** (data marketing) vừa tiến hóa thành **đơn hàng** khi sale chốt. "Cập nhật contact" = thao tác chính của sale. |
| **Customer** (Khách hàng) | Hồ sơ KH trong CRM, có rank/hạng, gắn nhiều contact/đơn, thông tin Facebook. |
| **LD (Lâu Đài)** | Tên nội bộ của khối nghiệp vụ lõi (contact, đơn, sản phẩm, kho, invoice). |
| **Gói sử dụng** | Gói dịch vụ đơn vị mua để dùng phần mềm (PAY / MuaGoiSuDung). Có giới hạn số account (`LD_DonVi_IsMaxAccount`). |
| **COD** | Tiền thu hộ khi giao hàng (Cash On Delivery) — Accounting cập nhật/đối soát. |
| **Chia số** (`ChiaSo`) | Phân bổ data/lead cho sale; role `ChiaSo`. |

## Người dùng & Role

User là DNN user + bản ghi `APP_UserProfile` (gắn `id_don_vi`). Tạo qua `CommonHelper.CreateUser()` (tạo DNN user + profile + gán role + `WidgetHelper.SyncUser()` đồng bộ sang service).

Role nghiệp vụ → màn hình thấy được:
- **Sale** — bán hàng: danh sách contact, cập nhật contact/đơn, lịch sử gọi, khiếu nại.
- **Marketing** — quản lý data/seeding, biểu đồ marketing.
- **Warehouse** — kho: biên bản, nhập xuất.
- **Accounting** — phiếu chi, COD, hủy hóa đơn.
- **TongDai/CSKH** — đầu số, group tổng đài, lịch sử active.
- **UnitAdmin / Admin** — quản trị đơn vị (gói, khuyến mãi, account).
- **CEO** — dashboard, báo cáo doanh số toàn đơn vị.
- **ChiaSo** — chia data cho sale.
- **TaoDonVi** — tạo đơn vị mới.
- **SuperUser** (DNN) — toàn quyền + truy cập Admin/Monitor tools.

## Các luồng nghiệp vụ chính

### 1. Marketing → Sale (lead → đơn)
Marketing nạp/seeding data → contact vào hệ thống (có thể từ LadiPage `LADI`, sàn TMĐT `Ecommerce`/Shopee, form). → **Chia số** phân bổ contact cho sale → Sale gọi (`CallHistoryByContact`, tích hợp `TONGDAI`) và **cập nhật contact** (`CapNhatContact` / `CapNhatContact2`) → chốt thành **đơn hàng**.

### 2. Đơn hàng → Giao vận (logistics)
Đơn chốt → đẩy sang hãng vận chuyển (domain **GH**: AhaMove, BEST, BoxMe, GHTK, JNT, NJV, SPX, Ship60, SuperShip, HeyU, HolaShip, DHCL). → In phiếu giao vận theo hãng (`Reports/vInDonGiaoVan*`). → Theo dõi trạng thái giao + log (`GHLog`). → Thu **COD** → Accounting đối soát.

### 3. Kế toán (Accounting)
`PhieuChi`, cập nhật/đối soát **COD**, hủy hóa đơn (`CancelInvoice`), hóa đơn điện tử (`Invoice`/`HDDTDAL`).

### 4. Báo cáo & thống kê
`BangTongHopBanHang` (SP `LD_Contact_BangTongHopBanHang`), `BC_DoanhThu`, dashboard CEO (`BaoCaoDoanhSo`, `ChartDashboard`), thống kê PushSale (`ThongKeDoanhSoDonVi`, `ThongKeTangTruong`). Domain **RP** + `lib/base.report`.

### 5. Kênh giao tiếp (webhook DB riêng `PS_Webhook_*`)
- **TongDai** — cuộc gọi/đầu số (`PS_Webhook_TongDai`).
- **SMS** — gửi/nhận SMS (`PS_Webhook_Sms`, `SMSHelper`).
- **ChatBot** — `PS_CHATBOT_Webhook_*`.
- **Customers webhook**, **Ecommerce** (Shopee) — `PS_Webhook_Customers`, `PS_Webhook_Ecommerce`.
- **Facebook** — chat, chiến dịch quảng cáo (Mains/LD/Facebook).

## Data & DB map (xem `architecture.md` mục 5)

- `*_portal` — DNN (user, role, page, module setting).
- `*_data` — nghiệp vụ (LD, APP, HT, RP, KhoSo dùng chung DB data).
- `*_logs` — log hệ thống/hoạt động.
- `PS_Webhook_*` — dữ liệu webhook các kênh.

## Mua/gia hạn gói (PAY)
`Mains/PAY/MuaGoiSuDung`, `Checkout` — đơn vị mua gói → mở khóa tính năng/account. Kết hợp `Admin/CapNhatGoiSuDung`, `CapNhatKhuyenMai`.

## Lưu ý khi đọc/sửa nghiệp vụ
- Logic chốt số/doanh số/đối soát phần lớn nằm trong **Stored Procedure** (không ở repo). Khi trace 1 con số sai → nghi SP trước, đọc cách code-behind gọi SP nào + tham số gì.
- Mỗi thao tác sale/admin thường kèm **ghi log hoạt động** (`APP_UserActivities_Log`) — đừng bỏ khi thêm chức năng tương tự.
- Toàn bộ ràng buộc theo `id_don_vi`; test phải tạo data ≥2 đơn vị để bắt lỗi rò rỉ tenant.
