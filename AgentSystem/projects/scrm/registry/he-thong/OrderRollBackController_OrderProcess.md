---
controller: OrderRollBackController
module: he-thong-rollback-don
service: OrderProcess
man_hinh_ui: "nội bộ — không có UI (API service-to-service tạo/cập nhật bản ghi rollback đơn; chỉ GetList được dùng để xem chi tiết)"
muc_dich: "Tạo và cập nhật các bản ghi ROLLBACK/HOÀN TÁC đơn hàng cho từng phân hệ: thu ngân, logistic (giao vận), đối soát, nhập kho trả khách. Khi 1 đơn cần hoàn tác (hủy thanh toán/hủy giao/trả khách), service đầu nguồn gọi sang đây để ghi nhận và kích hoạt rollback cross-module."
alias: ["rollback đơn", "hoàn tác đơn", "OrderRollBack", "rollback logistic", "rollback đối soát", "rollback nhập kho trả khách"]
auth: mixed
state_machine: [ThuNgan, Online/Logistic, DoiSoat, NhapKhoTraKhach]
deprecated: false
entities: [OrderRollBack, OrderRollBackLogistic, OrderRollBackLogisticDoiSoat, OrderRollBackThuNganSuccess, OrderRollBackOnlineSuccess, NhapKhoRollBackLogistic]
stored_procs: []
depends: ["IOrderRollBackService", "ConfigSystem (tham số job)"]
cross_module_in: ["Order/OrderLogistic/WarehouseProduct service gọi sang khi cần rollback 1 đơn", "Sàn TMDT gọi TMDT_CreateRollBackLogistic / TMDT_UpdateRollBackLogistic (anonymous)", "RabbitMQ: OrderRollBackSyncConsumer nhận OrderRollBackMassage → LuuOrderRollBack"]
cross_module_out: ["Job rollback gọi ngược sang Customer / Booking / WarehouseProduct / Promotion / ThuChi để hoàn tác side-effect của đơn"]
---

# OrderRollBackController (OrderProcess) — Rollback / hoàn tác đơn hàng

Controller hạ tầng của cơ chế rollback đơn. Mỗi loại rollback (thu ngân, logistic, đối soát, nhập kho trả khách) có cặp Create/Update riêng; service `OrderRollBackService` lưu bản ghi rồi các job nền (Call*RollBack*) đẩy lệnh hoàn tác sang các phân hệ liên quan. Hầu hết action gắn `[PrivateSBAuthorize()]` (chỉ gọi nội bộ service-to-service), riêng `TMDT_CreateRollBackLogistic` là `[AllowAnonymous]` để sàn TMDT bắn vào.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tạo rollback ALL | (nội bộ) | POST `api/OrderRollBack/CreateRollBackAll` | (PrivateSBAuthorize) | internal | — | `CreateRollBackAll(dto, CurrentUser)` — tạo tất cả bản ghi rollback cho đơn | EF | — |
| 2 | Cập nhật rollback | (nội bộ) | POST `.../UpdateRollBack` | (PrivateSBAuthorize) | internal | — | `UpdateRollBack(dto, CurrentUser)` | EF | — |
| 3 | Lấy theo idDonHang | (nội bộ) | GET `.../GetListByIdDonHang?idDonHang=` | (PrivateSBAuthorize) | internal | — | Trả `List<OrderRollBackThuNganSuccess>` theo đơn | EF | — |
| 4 | Tạo rollback logistic | (nội bộ) | POST `.../CreateRollBackLogistic` | (PrivateSBAuthorize) | internal | — | `CreateRollBackLogistic(dto, CurrentUser)` — rollback phần giao vận | EF | — |
| 5 | Tạo rollback logistic (TMDT) | (sàn TMDT) | POST `.../TMDT_CreateRollBackLogistic` | — | anonymous | — | `TMDT_CreateRollBackLogistic(dto)` — không có user, dùng cho sàn | EF | `[AllowAnonymous]` → open, không xác thực caller |
| 6 | Cập nhật rollback logistic | (nội bộ) | POST `.../UpdateRollBackLogistic` | (PrivateSBAuthorize) | internal | — | `UpdateRollBackLogistic(dto, CurrentUser)` | EF | — |
| 7 | Cập nhật rollback logistic (TMDT) | (nội bộ) | POST `.../TMDT_UpdateRollBackLogistic` | (PrivateSBAuthorize) | internal | — | `TMDT_UpdateRollBackLogistic(dto)` — KHÔNG có user | EF | Tên gắn "TMDT" nhưng vẫn yêu cầu PrivateSBAuthorize (khác đối xứng với Create TMDT anonymous) |
| 8 | Tạo rollback phiếu nhập trả khách | (nội bộ) | POST `.../CreateRollBackPhieuNhapTraKhach` | (PrivateSBAuthorize) | internal | — | `CreateRollBackPhieuNhapTraKhach(dto, CurrentUser)` | EF | — |
| 9 | Cập nhật rollback phiếu nhập | (nội bộ) | POST `.../UpdateRollBackPhieuNhap` | (PrivateSBAuthorize) | internal | — | `UpdateRollBackPhieuNhap(dto, CurrentUser)` | EF | — |
| 10 | Tạo rollback logistic đối soát | (nội bộ) | POST `.../CreateRollBackLogisticDoiSoat` | (PrivateSBAuthorize) | internal | — | `CreateRollBackLogisticDoiSoat(dto, CurrentUser)` | EF | — |
| 11 | Cập nhật rollback logistic đối soát | (nội bộ) | POST `.../UpdateRollBackLogisticDoiSoat` | (PrivateSBAuthorize) | internal | — | `UpdateRollBackLogisticDoiSoat(dto, CurrentUser)` | EF | — |
| 12 | Tạo rollback logistic đối soát ALL | (nội bộ) | POST `.../CreateRollBackLogisticDoiSoatAll` | (PrivateSBAuthorize) | internal | — | `CreateRollBackLogisticDoiSoatAll(dto, CurrentUser)` | EF | — |
| 13 | Lấy rollback đối soát theo đơn | (nội bộ) | GET `.../GetListByIdDonHangDoiSoat?idDonHang=` | (PrivateSBAuthorize) | internal | — | Trả `List<OrderRollBackLogisticDoiSoat>` | EF | — |
| 14 | Lấy rollback logistic theo đơn | (nội bộ) | GET `.../GetListByIdDonHangLogistic?idDonHang=` | (PrivateSBAuthorize) | internal | — | Trả `List<OrderRollBackOnlineSuccess>` | EF | — |

## Endpoint nội bộ (private / service-to-service)
- [internal] Order/OrderLogistic gọi `CreateRollBackAll` / `CreateRollBackLogistic*` khi 1 đơn hủy thanh toán/hủy giao cần hoàn tác.
- [anonymous] Sàn TMDT gọi `TMDT_CreateRollBackLogistic` để báo trạng thái cần rollback giao vận.
- [queue] `OrderRollBackSyncConsumer` (RabbitMQ) nhận `OrderRollBackMassage` → `LuuOrderRollBack` → nếu lưu ok thì lần lượt gọi `CallRollBackCustomer/Booking/WareHouse/Promotion/ThuChi`.

## Luồng xuyên module
- Trigger: đơn cần hoàn tác (hủy TT / hủy giao / trả khách) → ghi bản ghi rollback (Create*) HOẶC nhận `OrderRollBackMassage` qua queue.
- Bước: `LuuOrderRollBack` lưu → job nền `CallRollBack*` đọc `ConfigSystem` → bắn lệnh hoàn tác sang từng phân hệ.
- Side-effect cross-module: hoàn kho (WarehouseProduct), hoàn công nợ/khách (Customer), hủy booking (Booking), gỡ khuyến mại (Promotion), hoàn thu-chi (ThuChi).

## Lỗi đã biết / lưu ý kỹ thuật
- `OrderRollBackController.cs:78-84` — `TMDT_CreateRollBackLogistic` `[AllowAnonymous]`: endpoint rollback giao vận mở public, không xác thực caller → cần whitelist/HMAC ở tầng gateway.
- `OrderRollBackController.cs:94-100` — `TMDT_UpdateRollBackLogistic` lại có `[PrivateSBAuthorize]` (bất đối xứng với Create TMDT anonymous), dễ nhầm khi tích hợp.
- `OrderRollBackSyncConsumer.cs:32-41` — chuỗi `CallRollBack*` chạy tuần tự trong consumer, bọc 1 try/catch chung: 1 bước lỗi (sau khi đã lưu) chỉ log, các bước sau vẫn chạy → rollback có thể từng phần, cần đối soát lại bằng các job dọn (`JobXoaBanGhiOrderRollBack*`).
