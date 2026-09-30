---
controller: DonHangKhachHangController
module: cua-hang
service: Order
man_hinh_ui: "Cửa hàng — màn công nợ/ghi nợ khách hàng (thao tác 'Phân bổ tiền thanh toán vào danh sách đơn hàng' của khách): chưa xác định UI cụ thể (chưa thấy FE service gọi endpoint `PhanBoTienThanhToanVaoDanhSachDonHang`); nhiều khả năng là API nội bộ cho luồng gạch nợ / phân bổ thanh toán nhiều đơn."
muc_dich: "Phân bổ một khoản tiền thanh toán/trả nợ của khách hàng vào nhiều đơn hàng (ghi nợ) cùng lúc — gạch nợ / chỉ định tiền cho từng đơn."
alias: ["phân bổ tiền thanh toán", "gạch nợ đơn hàng", "trả nợ nhiều đơn", "phân bổ thanh toán khách hàng", "ghi nợ đơn hàng"]
auth: public
state_machine: []
deprecated: false
entities: [DonHang, KhachHangGhiNo]
stored_procs: []
depends: [Customer (KhachHangGhiNo dto), Order (đơn hàng + phương thức thanh toán)]
cross_module_in: []
cross_module_out: [Cập nhật công nợ khách hàng (KhachHangGhiNo) + phương thức thanh toán đơn hàng]
---

# DonHangKhachHangController — Phân bổ tiền thanh toán vào đơn hàng khách (ghi nợ)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Phân bổ tiền thanh toán vào nhiều đơn | (thao tác gạch nợ / phân bổ thanh toán) | POST `api/DonHangKhachHang/PhanBoTienThanhToanVaoDanhSachDonHang` | — (`[SBAuthorize()]` rỗng) | public | - | Nhận danh sách đơn ghi nợ (`LstDonHangGhiNo`) + danh sách phương thức thanh toán (`LstPhuongThucThanhToanDonHang`) + cờ `IsChiDinhTienChoDonHang`; phân bổ tiền thanh toán/trả nợ vào các đơn của khách (cập nhật công nợ + thanh toán) | EF (service) | Trả `Ok(rs)` trực tiếp (KHÔNG dùng `ResponseResult`) → response shape khác các controller khác |

## Quan hệ
- **Gọi sang:** dùng dto `Customer.Client.Dto.KhachHangGhiNo` (ghi nợ khách hàng) — phối hợp dữ liệu công nợ phía Customer.
- **Dữ liệu (entities):** `DonHang` (danh sách đơn được phân bổ), `KhachHangGhiNo` (công nợ khách), phương thức thanh toán đơn (`ThanhToan_DonHangPtThanhToanDto`).
- **Ripple / đồng bộ:** phân bổ tiền → cập nhật trạng thái thanh toán/công nợ của từng đơn trong danh sách.

## Lỗi đã biết / lưu ý kỹ thuật
- **Auth chỉ login**: `[SBAuthorize()]` rỗng + comment `//TODO: phân quyền` ngay trên endpoint (`DonHangKhachHangController.cs:29-31`) → permission key dự kiến (`DonHangNhanVien_TinhHoaHong`) chưa gắn; endpoint thao tác tiền/công nợ nhưng chỉ yêu cầu đăng nhập.
- **Trả `Ok(rs)` thay vì `ResponseResult(rs)`** (`:40`) → bỏ qua chuẩn bao response của hệ thống (status/code) → FE phải parse khác; lưu ý khi tích hợp.
- Comment `[Route]`/`[ApiController]` trùng lặp ở đầu class (dòng commented + dòng active) — chỉ dòng active có hiệu lực, không ảnh hưởng.
- Controller gắn `[TienIchAttribute("DonHangKhachHangController", ..., MaTienIchSCRM.TI_Order)]` → vẫn gate theo tiện ích TI_Order ở tầng tiện ích.
- Code thật: BE `Order/Order.API/Controllers/DonHangKhachHangController.cs`. FE: chưa xác định service gọi endpoint này.
