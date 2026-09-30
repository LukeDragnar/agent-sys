---
controller: NghiepVuManagementController
module: kho
service: OrderLogistic
man_hinh_ui: "Quản lý nghiệp vụ Order Logistic (HT-CSKH) — màn theo dõi đơn + Pending + History, search MaDon, filter trạng thái, nút Thử lại"
muc_dich: "Theo dõi & vận hành hàng đợi nghiệp vụ Order Logistic: liệt kê đơn kèm thống kê Pending/Thành công/Thất bại, xem chi tiết Pending & History, thử lại các nghiệp vụ lỗi (re-publish vào MQ)."
alias: ["quản lý nghiệp vụ", "queue nghiệp vụ", "pending nghiệp vụ", "thử lại nghiệp vụ lỗi", "retry pending", "lịch sử xử lý nghiệp vụ", "đơn hàng pending logistic"]
auth: internal
state_machine: [Pending, ThanhCong, Loi, BoQua]
deprecated: false
entities: [DonHangLogisticNghiepVuPending, DonHangLogisticNghiepVuHistory]
stored_procs: []
depends: [OrderLogistic, RabbitMQ (XuLyNghiepVuMessage), NghiepVuJobService]
cross_module_in: [OrderLogisticConsumer (XuLyNghiepVuConsumer → NghiepVuJobService)]
cross_module_out: [RabbitMQ (publish XuLyNghiepVuMessage → consumer xử lý lại nghiệp vụ)]
---

# NghiepVuManagement — Quản lý hàng đợi nghiệp vụ Order Logistic

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm đơn + thống kê | "Tìm kiếm" | POST `api/NghiepVuManagement/TimDonHang` | `NghiepVuManagement_TimDonHang` | internal | màn list đơn | Trả list đơn (search MaDon, filter đa trạng thái pending/thanh-cong/that-bai) kèm số Pending/SoThanhCong/SoThatBai gom theo IdDonHang | EF: Pending + History, group count theo TrangThaiXuLy | - |
| 2 | Tìm bản ghi Pending | (tab Pending) | POST `api/NghiepVuManagement/TimPending` | `NghiepVuManagement_TimPending` | internal | tab Pending | Lọc Pending theo IdDonHang/IdKhachHang/NghiepVu hoặc chỉ bản ghi lỗi (`SoLanThuLai > 0`) | EF: `DonHangLogisticNghiepVuPending` | - |
| 3 | Tìm lịch sử History | (tab History) | POST `api/NghiepVuManagement/TimHistory` | `NghiepVuManagement_TimHistory` | internal | tab History | Lọc History theo IdDonHang/IdKhachHang/NghiepVu hoặc TrangThaiXuLy (1=ThanhCong, 2=Loi, 3=BoQua) | EF: `DonHangLogisticNghiepVuHistory` | - |
| 4 | Thử lại 1 Pending lỗi | "Thử lại" (row Pending) | POST `api/NghiepVuManagement/ThuLaiPending/{id}` | `NghiepVuManagement_ThuLaiPending` | internal | row Pending đang lỗi | Reset `SoLanThuLai=0` + `ThoiGianXuLy=now` → **publish `XuLyNghiepVuMessage`** trigger xử lý ngay | EF update + MQ publish | - |
| 5 | Tạo lại Pending từ History | "Thử lại" (row History lỗi/bỏ qua) | POST `api/NghiepVuManagement/ThuLaiTuHistory/{id}` | `NghiepVuManagement_ThuLaiTuHistory` | internal | row History `TrangThaiXuLy=Loi(2)` hoặc `BoQua(3)`; chặn nếu `ThanhCong` | Tạo Pending mới từ History (SoLanThuLai=0), hạ `IsMoiNhat` của Pending cũ cùng (IdDonHang, NghiepVu), publish `XuLyNghiepVuMessage` | EF insert + raw SQL update IsMoiNhat + MQ publish | Dùng raw SQL `@NghiepVu` để hạ IsMoiNhat — sửa schema cần đồng bộ |

## Quy tắc nghiệp vụ (gating / retry)
1. **ThuLaiPending**: bản ghi phải tồn tại (404 nếu không). Reset `SoLanThuLai=0` để consumer xử lý lại từ đầu (không bị chặn bởi ngưỡng retry tự động).
2. **ThuLaiTuHistory**: `TrangThaiXuLy == ThanhCong(1)` → chặn (không tạo lại đơn đã thành công). Chỉ `Loi(2)` / `BoQua(3)` mới được tạo lại Pending.
3. **IsMoiNhat**: mỗi (IdDonHang, NghiepVu) chỉ 1 Pending "mới nhất" — tạo Pending mới phải hạ cờ Pending cũ → tránh consumer xử lý song song 2 bản ghi cùng nghiệp vụ.

## Quan hệ
- **Gọi sang:** RabbitMQ qua `IPublishEndpointDonHang.Publish(XuLyNghiepVuMessage)` — đẩy vào hàng đợi cho consumer xử lý.
- **Dữ liệu (entities):** `DonHangLogisticNghiepVuPending` (hàng đợi chờ), `DonHangLogisticNghiepVuHistory` (lịch sử đã xử lý).
- **Ripple / đồng bộ:** publish message → `OrderLogisticConsumer.API/Consumer/XuLyNghiepVuConsumer` → `NghiepVuJobService` xử lý nghiệp vụ thật (cộng/trừ liên quan đơn logistic theo loại nghiệp vụ).

## Luồng xuyên module (thử lại nghiệp vụ lỗi)
```
User bấm "Thử lại" (Pending lỗi hoặc History lỗi)
  → reset/tạo Pending (SoLanThuLai=0, IsMoiNhat)
  → IPublishEndpointDonHang.Publish(XuLyNghiepVuMessage{ IdPending, NghiepVu, ... })
  → MQ → XuLyNghiepVuConsumer.Consume → NghiepVuJobService xử lý
  → KHÔNG catch exception trong consumer (cố ý) → MassTransit tự retry / DLQ
  → kết quả ghi sang History (ThanhCong/Loi/BoQua)
```

## Lỗi đã biết / lưu ý kỹ thuật
- Consumer cố ý **không catch exception** (`XuLyNghiepVuConsumer.cs:33`) để MassTransit retry/DLQ — xử lý idempotent qua `IsProcessing`. Khi debug "đơn retry mãi không xong" → soi DLQ + `IsProcessing` thay vì log app.
- `ThuLaiTuHistory` hạ IsMoiNhat bằng **raw SQL** (parameter `@NghiepVu`) — không qua EF tracking; đổi tên cột/bảng nghiệp vụ phải sửa cả chỗ này.
- Controller-level không có `[SBAuthorize]` chung; mỗi method tự gắn permission key riêng (HT-CSKH).
