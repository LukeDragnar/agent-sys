---
controller: KhuyenMaiKhachHangController
module: marketing
service: Promotion
man_hinh_ui: "Marketing/CSKH > Khuyến mãi của khách hàng (gán KM riêng cho 1 KH; xem KM khách được tặng — tab trong màn KH/lịch hẹn)"
muc_dich: "Gán khuyến mãi riêng cho từng khách hàng (KM được tặng) và tra cứu danh sách KM mà 1 khách hàng được hưởng (cho màn khách hàng, lịch hẹn, và service khác)."
alias: ["khuyến mãi khách hàng", "KhuyenMaiKhachHang", "KM được tặng", "KM riêng khách"]
auth: mixed
state_machine: []
deprecated: false
entities: [KhuyenMaiKhachHang, KhuyenMaiKhachHangLichSu, KhuyenMai]
stored_procs: []
depends:
  - "Core/TienIch (TienIchAttribute TI_KhuyenMai)"
cross_module_in:
  - "Customer/màn lịch hẹn (TimKiemTheoIdKhachHang_LichHen — private)"
  - "Order/service khác (GetDanhSachKhuyenMaiByKhachHangPrivate — lấy KM khách được tặng khi tạo/thanh toán đơn)"
cross_module_out:
  - "PromotionUpdateWithBillService (KhuyenMaiKhachHang là bảng đếm lượt/khách — IsSuDung, SoLuongDaDung khi thanh toán)"
---

# KhuyenMaiKhachHang — Khuyến mãi của khách hàng

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|
| 1 | Gán KM cho khách hàng | Lưu / Tặng KM | POST `ThemMoi` | `KhuyenMaiKhachHang_ThemMoi` (AD-TONG/CN/MCN) | public | `Create(payload)` — tạo bản ghi KM gán cho KH (KM được tặng). | EF | — |
| 2 | KM khách được tặng | Danh sách (tab KH) | POST `TimKiemTheoIdKhachHang` | `KhuyenMaiKhachHang_TimKiemTheoIdKhachHang` (AD-TONG/CN/MCN, QLDV, SALE, NV) | public | Lọc KM đã gán cho KH. | EF | — |

## Endpoint nội bộ (private / service-to-service)
- `[PrivateSBAuthorize]` — POST `TimKiemTheoIdKhachHang_LichHen`: KM khách được tặng cho **màn lịch hẹn** (quyền key UI bị comment → chỉ còn private).
- `[PrivateSBAuthorize]` — POST `GetDanhSachKhuyenMaiByKhachHangPrivate` (payload `GetDataCrossServiceDto`): service khác (Order) lấy KM khách được tặng khi tạo/thanh toán đơn.

## Quan hệ
- **Gọi sang:** không gọi service ngoài ở controller.
- **Dữ liệu (entities):** `KhuyenMaiKhachHang` (KM gán theo KH — cờ IsSuDung, IdDonHang, NgaySuDung, SoLuongDaDung), `KhuyenMaiKhachHangLichSu` (lịch sử mỗi lần áp), `KhuyenMai` (master).
- **Ripple/đồng bộ:** khi thanh toán, PromotionUpdateWithBillService update KhuyenMaiKhachHang (IsSuDung=true, tăng SoLuongDaDung) + insert KhuyenMaiKhachHangLichSu; hủy đơn → reset.

## Lỗi đã biết / lưu ý kỹ thuật
- `KhuyenMaiKhachHangController.cs:47-48` — `TimKiemTheoIdKhachHang_LichHen` đã đổi từ `[SBAuthorize(key)]` (comment) sang `[PrivateSBAuthorize]` → giờ chỉ gọi nội bộ được; nếu UI lịch hẹn còn gọi qua gateway public sẽ 401.
- KhuyenMaiKhachHang là **nguồn đếm số lần/khách** — sửa rule giới hạn/khách phải đồng bộ với KhuyenMaiService (KiemTraGioiHanApDungTheoDon) và PromotionUpdateWithBillService.
