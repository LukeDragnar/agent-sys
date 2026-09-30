# SocketServer — Real-time WebSocket

> Cung cấp kênh real-time cho frontend qua SignalR. Thông báo trạng thái đơn hàng, tiến trình import file, xử lý ecommerce, hóa đơn.

---

## Vai trò

- ← **Backend services**: gọi Hub API để push update đến client
- → **Frontend**: nhận real-time update qua WebSocket (SignalR)

---

## Hubs (SignalR endpoints)

| Hub | URL | Mục đích |
|-----|-----|---------|
| `DonHangThanhToanHub` | `/initServer` | Trạng thái đơn hàng + thanh toán real-time |
| `ProcessEcommerceHub` | `/processEcommerceHub` | Tiến trình xử lý đơn TMĐT |
| `InvoiceHub` | `/invoice-socket` | Trạng thái hóa đơn điện tử |
| `FileImportHub` | `/fileImportHub` | Tiến trình import file |
| `LogDataProcessHub` | `/donHangXoaHub` | Log xử lý/xóa đơn hàng |

---

## Grouping theo Hub

### DonHangThanhToanHub
- `RoomUser_{userId}` — phòng riêng của user
- `DangDon_{donViId}_{idChiNhanh}` — đơn đang tạo
- `HuyDangDon_{donViId}_{idChiNhanh}` — đơn bị hủy
- `MobileTest_{userId}` — test mobile

### ProcessEcommerceHub / FileImportHub / LogDataProcessHub
- Group theo `unitConnectType` / `fileImportId`
- Broadcast: `ReceiveProcessUpdate` với `successCount`, `failCount`, `totalCount`

### InvoiceHub
- `CHINHANH_{id}_{groupName}`
- `DONVI_{id}_{groupName}`
- `USER_{id}_{groupName}`

---

## Luồng

```
[Real-time đơn hàng]
Cashier tạo đơn → backend gọi DonHangThanhToanHub
  └→ SendToGroup("DangDon_{donViId}_{chiNhanh}", data)
       → Frontend POS nhận, hiển thị ngay

[Tiến trình import file]
Backend xử lý import CSV/Excel
  ├→ Mỗi N record: Hub.SendAsync(fileImportId, successCount, failCount, total)
  └→ Frontend nhận ReceiveProcessUpdate → cập nhật progress bar

[Xử lý đơn TMĐT]
Backend sync đơn Shopee/Lazada
  └→ ProcessEcommerceHub.SendAsync(unitConnectType, counts)
       → Frontend nhận update theo từng sàn

[Hóa đơn điện tử]
Invoice service phát hành HĐ xong
  └→ InvoiceHub notify group CHINHANH/DONVI/USER
       → Frontend cập nhật trạng thái HĐ
```

---

## Cấu hình

- **Auth**: JWT middleware (`JwtMiddleware`) — user phải xác thực trước khi connect
- **CORS**: dynamic origin từ `CoreConnectStr`, allow credentials
- **Auto join/leave**: `OnConnectedAsync` / `OnDisconnectedAsync` tự quản lý group membership

---

## Checklist fix bug

- [ ] Frontend không nhận update → kiểm tra WebSocket connect, JWT token hợp lệ chưa
- [ ] Group không đúng → kiểm tra logic join group (donViId, chiNhanh, userId)
- [ ] Progress bar không update → kiểm tra backend có gọi Hub đúng endpoint không
- [ ] CORS error → kiểm tra `CoreConnectStr` config, origin có được allow không
