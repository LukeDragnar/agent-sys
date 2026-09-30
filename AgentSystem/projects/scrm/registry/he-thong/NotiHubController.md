---
controller: NotiHubController
module: he-thong
service: SocketServer
man_hinh_ui: "nội bộ — không có UI (realtime push hub đa mục đích, FE/POS lắng nghe socket)"
muc_dich: "Push realtime đa mục đích tới FE qua SignalR (DonHangThanhToanHub): message theo user, màn hình khách POS, đăng/hủy đăng đơn, gửi message theo group tùy ý."
alias: ["NotiHub", "DonHangThanhToanHub", "socket POS", "màn hình khách", "đăng đơn realtime", "SendMesage"]
auth: mixed
state_machine: []
deprecated: false
entities: [HubSendMesageManHinhKhachDto, HubSendMesageDangDonDto, HubSendMesageHuyDangDonDto, HubSendMesageToGroup, InvoiceResponseDto]
stored_procs: []
depends: ["IHubContext<DonHangThanhToanHub>", "IUserPrincipalService", "SignalR"]
cross_module_in: ["FE/POS & service nghiệp vụ gọi các endpoint để push noti theo user/đơn vị/group"]
cross_module_out: ["Push SignalR tới group RoomUser_{id} / DangDon_/HuyDangDon_/MobileTest_ / group tùy ý → FE & màn hình khách"]
---

# NotiHubController — Push realtime đa mục đích (DonHangThanhToanHub)

Hub đa mục đích: gửi message theo user đang đăng nhập (`RoomUser_{userId}`), đẩy nội dung màn hình khách POS, báo trạng thái đăng/hủy đăng đơn theo đơn vị-chi nhánh, test mobile, và gửi message tới một group bất kỳ. Một số endpoint cần đăng nhập (`[SBAuthorize]` + `GetUserLogin()`), một số mở (đăng đơn). Endpoint publish invoice đã bị **vô hiệu** (thân comment hết, trả true rỗng).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Gửi message cho chính user | (không UI) | GET `/api/NotiHub/SendMesage?mess=` | — (`[SBAuthorize]`) | internal | user login != null | Push `message {MaDonHang=mess}` tới `RoomUser_{user.Id}`. | — (SignalR) | GET có side-effect push |
| 2 | Push màn hình khách (POS) | (POS) | POST `/SendMesageManHinhKhach` | — (`[SBAuthorize]`) | internal | user login != null | Push `SendMesageManHinhKhach(rq)` tới `RoomUser_{user.Id}`. | — (SignalR) | — |
| 3 | Báo đăng đơn | (xử lý đăng đơn) | POST `/SendMesageDangDon` | — (không attr; `[PrivateSBAuthorize]` đã comment) | anonymous | rq != null | Push `SendMesageDangDon(rq)` tới `DangDon_{IdDonVi}_{IdChiNhanh}`. | — (SignalR) | `[PrivateSBAuthorize]` bị comment → mở anonymous |
| 4 | Báo hủy đăng đơn | (hủy đăng đơn) | POST `/SendMesageHuyDangDon` | — (không attr; `[PrivateSBAuthorize]` comment) | anonymous | rq != null | Push `SendMesageHuyDangDon(rq)` tới `HuyDangDon_{IdDonVi}_{IdChiNhanh}`. | — (SignalR) | anonymous (comment) |
| 5 | Test mobile | (test) | POST `/SendDataMobileTest` | — (`[SBAuthorize]`) | internal | user login != null | Push `SendDataMobileTest(rq)` tới `MobileTest_{user.Id}`. | — (SignalR) | endpoint test còn sót |
| 6 | Gửi message tới group tùy ý | (không UI) | POST `/SendMessageToGroup/{groupname}` | — (không attr) | anonymous | — | Set GroupName, push `ReceiveGroupMessage(message)` tới group `message.GroupId`. | — (SignalR) | anonymous → push vào group bất kỳ |
| 7 | Publish invoice (VÔ HIỆU) | (không UI) | POST `/SendMesagePublishInvoice` | — (không attr) | anonymous | — | **No-op**: thân đã comment, chỉ trả `true`. | — | deprecated thực tế — không push gì |

## Quan hệ
- **Gọi sang:** `IHubContext<DonHangThanhToanHub>`, `IUserPrincipalService.GetUserLogin()`.
- **Dữ liệu (entities):** các DTO push (ManHinhKhach/DangDon/HuyDangDon/ToGroup/InvoiceResponseDto) — không lưu DB.
- **Ripple / đồng bộ:** FE/màn hình khách POS join `RoomUser_{id}` / `DangDon_*` / `HuyDangDon_*` nhận event realtime (cập nhật đơn, hiển thị màn hình khách, trạng thái đăng đơn).

## Lỗi đã biết / lưu ý kỹ thuật
- `NotiHubController.cs:51,65,106` — `[PrivateSBAuthorize]` bị **comment** trên SendMesageDangDon/HuyDangDon/PublishInvoice → các endpoint này thành anonymous, ai cũng push được noti đăng/hủy đơn.
- `NotiHubController.cs:107-115` `SendMesagePublishInvoice` thân code comment hết → no-op, nên đánh dấu bỏ.
- `NotiHubController.cs:27` `SendMesage` là `[HttpGet]` nhưng có side-effect (push socket) — không idempotent.
- `NotiHubController.cs:94` `SendMessageToGroup` anonymous + nhận GroupId tùy client → có thể bơm message vào group bất kỳ.
