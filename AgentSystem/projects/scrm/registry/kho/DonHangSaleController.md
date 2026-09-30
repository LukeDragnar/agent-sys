---
controller: DonHangSaleController
module: kho
service: OrderLogistic
man_hinh_ui: "Sale > Đăng đơn (màn tác nghiệp đăng đơn dành cho nhân viên sale) — list đơn chờ đăng + nút Đăng đơn / Hủy đăng đơn / Care đơn; là bản 'khẩu độ sale' của màn Thủ kho tác nghiệp."
muc_dich: "Endpoint tác nghiệp đăng đơn dành riêng cho role sale (DV-SALE): đẩy đơn vào hàng đợi đăng đơn, process từng đơn lên đối tác giao vận, hủy đăng đơn, đổi mã đơn, care đơn. Cùng IThuKhoTacNghiepService với ThuKhoTacNghiep nhưng bộ permission key gắn tiền tố DonHangSale_ + role sale."
alias: ["đăng đơn sale", "đơn hàng sale", "sale đăng đơn", "DonHangSale", "tác nghiệp sale", "hủy đăng đơn sale"]
auth: public
state_machine: [DonMoi(1000), ChoVanDon(1), DaDang(20), DangGiaoHang(30), DaGiaoHang(31), HuyDangDon(5), HuyDon(999)]
deprecated: false
entities: [DonHangLogistic, DonHangDangDon, DonHangHuyDangDon]
stored_procs: [PRC_SCRM_OrderLogistic_DonHangDangDon_GetForDangDon]
depends:
  - "IThuKhoTacNghiepService (= ThuKhoTacNghiepService/KhoTacNghiepService.cs — chung service với ThuKhoTacNghiep, KeToanTacNghiep)"
cross_module_in: []
cross_module_out:
  - "DeliveryConnect (đăng đơn → gửi lệnh tạo vận đơn sang đối tác giao vận khi ProcessDonHangDangDon)"
  - "WarehouseProduct (trừ/cộng SoLuongChoXuat khi đăng/hủy đăng đơn)"
  - "Contact/Customer (UpdateTrangThaiCareDon, blacklist)"
---

# DonHangSaleController — Đăng đơn (khẩu độ Sale)

> Controller này là một "mặt nạ permission" cho role sale: dùng **chung** `IThuKhoTacNghiepService` với `ThuKhoTacNghiepController` và `KeToanTacNghiepController`. Khác biệt duy nhất là bộ `SBAuthorize` key (`DonHangSale_*`) + danh sách role (có `DV-SALE`). Hành vi nghiệp vụ = giống ThuKhoTacNghiep (xem node đó để hiểu sâu luồng đăng đơn).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm đơn để đăng | "Tìm kiếm" | POST `api/DonHangSale/DangDonSearch` | `DonHangSale_DangDonSearch` | public | màn list | Lọc đơn đủ điều kiện đăng đơn (chốt đơn, có ĐVVC, có kho, chưa đăng) | EF | - |
| 2 | Đẩy đơn vào hàng đợi đăng | "Đăng đơn" | POST `api/DonHangSale/InsertDonHangDangDon` | `DonHangSale_InsertDonHangDangDon` | public | đơn chưa đăng | Validate per-đơn → insert bản ghi `DonHangDangDon` (StatusProcessd=ChuaXuly). KHÔNG đăng ngay, chỉ đưa vào queue | EF | Gate cấu hình `IsSaleDangDon` — sale chỉ đăng được nếu đơn vị bật cờ này (KhoTacNghiepService.cs:5320) |
| 3 | Process hàng đợi đăng đơn | (poll nền sau Insert) | POST `api/DonHangSale/ProcessDonHangDangDon` | `DonHangSale_ProcessDonHangDangDon` | public | có bản ghi chờ | Bốc 1 bản ghi `DonHangDangDon` bằng SP (lock chống bốc trùng) → gửi lệnh tạo vận đơn sang ĐTGV → set DaDang + trừ kho | SP + EF | - |
| 4 | Đếm tổng đơn chờ đăng | (badge) | POST `api/DonHangSale/GetDonHangDangDonTotal` | `DonHangSale_GetDonHangDangDonByUser` | public | - | Đếm bản ghi DonHangDangDon ChuaXuly của user | EF | Quyền dùng chung key với GetByUser (không tách riêng) |
| 5 | List đơn đang đăng theo user | (grid kết quả) | POST `api/DonHangSale/GetDonHangDangDonByUser` | `DonHangSale_GetDonHangDangDonByUser` | public | - | List bản ghi DonHangDangDon của user | EF | - |
| 6 | Xóa đơn chờ đăng theo trạng thái | "Xóa" | POST `api/DonHangSale/DeleteDonHangDangDonByStatus` | `DonHangSale_DeleteDonHangDangDonByStatus` | public | - | Xóa bản ghi DonHangDangDon theo status | EF | - |
| 7 | Đổi mã đơn | "Đổi mã đơn" | POST `api/DonHangSale/ThayDoiMaDon` | `DonHangSale_ThayDoiMaDon` | public | - | Sinh lại mã đơn cho đơn (theo Id) | EF | - |
| 8 | Hủy đăng đơn (đẩy hàng đợi) | "Hủy đăng đơn" | POST `api/DonHangSale/InsertDonHangHuyDangDon` | `DonHangSale_InsertDonHangHuyDangDon` | public | đơn DaDang | Đưa đơn vào queue hủy đăng (`DonHangHuyDangDon`) | EF | - |
| 9 | Process hủy đăng đơn | (poll nền) | POST `api/DonHangSale/ProcessDonHangHuyDangDon` | `DonHangSale_ProcessDonHangHuyDangDon` | public | - | Bốc 1 bản ghi hủy đăng → gửi lệnh hủy ĐTGV + hoàn kho → đơn về chưa đăng | EF | - |
| 10 | Đếm tổng / list / xóa hủy đăng đơn | badge / grid / xóa | POST `GetDonHangHuyDangDonTotal`, `GetDonHangHuyDangDonByUser`, `DeleteDonHangHuyDangDonByStatus`, `HuyDangDonSearch` | `DonHangSale_GetDonHangHuyDangDonTotal` / `..ByUser` / `..DeleteDonHangHuyDangDonByStatus` / `..HuyDangDonSearch` | public | - | Tương tự nhánh đăng đơn nhưng cho hàng đợi hủy đăng | EF | - |
| 11 | Care đơn | "Care" | POST `api/DonHangSale/UpdateTrangThaiCareDon` | `DonHangSale_UpdateTrangThaiCareDon` | public | - | Cập nhật trạng thái care đơn | EF | - |

## Quan hệ
- **Gọi sang:** `IThuKhoTacNghiepService` (cùng instance service với ThuKhoTacNghiep/KeToanTacNghiep). Đăng đơn ripple sang DeliveryConnect (gửi lệnh ĐTGV) + WarehouseProduct (trừ/cộng kho).
- **Dữ liệu (entities):** `DonHangLogistic`, `DonHangDangDon` (hàng đợi đăng), `DonHangHuyDangDon` (hàng đợi hủy đăng).
- **Ripple / đồng bộ:** đăng đơn → tạo vận đơn ĐTGV, trừ tồn chờ xuất; hủy đăng → hủy vận đơn + hoàn kho.

## Luồng xuyên module (đăng đơn — pattern Insert→Process)
1. Sale chọn đơn → `InsertDonHangDangDon`: validate per-đơn (đúng đơn vị/chi nhánh, có ĐVVC, có kho, chưa DaDang) → ghi bản ghi `DonHangDangDon` StatusProcessd=ChuaXuly. KHÔNG gọi ĐTGV ở bước này.
2. FE poll `ProcessDonHangDangDon`: bốc 1 bản ghi bằng SP `..._DonHangDangDon_GetForDangDon` (lock để tránh 2 user bốc cùng đơn) → gửi lệnh tạo vận đơn sang đối tác → set GiaoHangTrangThaiMa=DaDang(20), trừ kho chờ xuất.
3. Hủy đăng đơn theo pattern tương tự (queue `DonHangHuyDangDon` → process → hủy vận đơn ĐTGV + hoàn kho).

## Lỗi đã biết / lưu ý kỹ thuật
- Sale chỉ đăng đơn được nếu đơn vị bật `IsSaleDangDon` trong cấu hình init (KhoTacNghiepService.cs:5320); không bật → trả "Đơn vị chưa được cấu hình đăng đơn".
- `GetDonHangDangDonTotal` (action 4) dùng CHUNG permission key `DonHangSale_GetDonHangDangDonByUser` với action 5 — không tách key riêng (DonHangSaleController.cs:38).
- Endpoint `UpdateDoiSoatNoiBo` bị comment chết trong file (DonHangSaleController.cs:111-117) — đối soát nội bộ đã bỏ ở khẩu độ sale.
- Controller KHÔNG `partial`, chỉ 1 file; tất cả endpoint gắn `DV-SALE` (đây là điểm phân biệt với ThuKhoTacNghiep/KeToanTacNghiep dùng cùng service).
