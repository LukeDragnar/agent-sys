---
controller: DatLichChiTietController
module: cua-hang
service: Booking
man_hinh_ui: "nội bộ — không có UI riêng (chi tiết đặt lịch thường được trả kèm trong DatLichController; endpoint này là tra cứu raw DatLichChiTiet)"
muc_dich: "Tìm/phân trang bản ghi chi tiết đặt lịch (DatLichChiTiet) thuần theo điều kiện grid, không enrich và không filter theo đơn vị."
alias: ["chi tiết đặt lịch", "đặt lịch chi tiết", "datlichchitiet", "chi tiết lịch hẹn (raw)"]
auth: anonymous
state_machine: []
deprecated: false
entities: [DatLichChiTiet]
stored_procs: []
depends: [Booking (BaseService/IRepository<DatLichChiTiet>)]
cross_module_in: []
cross_module_out: []
---

# DatLichChiTietController — Chi tiết đặt lịch (tra cứu raw)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm chi tiết đặt lịch (grid) | (không có UI riêng) | POST `api/DatLichChiTiet/TimTheoDieuKien` | (không key) | anonymous | - | Query toàn bảng `DatLichChiTiet` (NoLock); sort theo `gridInfo.Sorts` hoặc mặc định `NgayCapNhat` desc; phân trang Skip/Take; map sang `DatLichChiTietDto`; trả kèm `total = query.Count()` | EF (TableWithNolock) | Xem known-issue |

## Lỗi đã biết / lưu ý kỹ thuật
- **`[AllowAnonymous]` + KHÔNG filter theo DonVi/ChiNhánh** (Controller:38-40; Service:32-49) — `TimTheoDieuKienAsync` query toàn bộ bảng `DatLichChiTiet` không lọc tenant và không cần token → bất kỳ ai cũng có thể phân trang đọc chi tiết đặt lịch của mọi đơn vị (rò rỉ dữ liệu cross-tenant). Cần xác nhận có chủ ý / có nên gắn auth + filter DonVi.
- **`total = query.Count()` đếm cả bảng** (Service:36) — total không phản ánh bộ lọc nào (gridInfo chỉ dùng Sorts + PageInfo, không có điều kiện Where) → phân trang trên toàn bộ bản ghi.
- **Controller `[SBAuthorize]` cấp class bị comment** (Controller:22) — không áp auth controller-level; chỉ còn `[AllowAnonymous]` của action.
- Code thật: BE controller `Booking/Booking.API/Controllers/DatLichChiTietController.cs`; service `Booking/Booking.Service/Generated/DatLichChiTietService.cs`.
