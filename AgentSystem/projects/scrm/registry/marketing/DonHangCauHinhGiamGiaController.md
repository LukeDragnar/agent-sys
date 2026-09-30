---
controller: DonHangCauHinhGiamGiaController
module: marketing
service: Promotion
man_hinh_ui: "Marketing > Cấu hình giảm giá theo đơn hàng (cấu hình ngưỡng giá trị đơn → mức giảm)"
muc_dich: "CRUD cấu hình giảm giá tự động theo tổng giá trị đơn hàng (khoảng GiaTriTu→GiaTriDen → giảm theo % hoặc tiền), và tra cứu mức giảm áp được theo giá đơn khi thanh toán."
alias: ["cấu hình giảm giá đơn hàng", "DonHangCauHinhGiamGia", "giảm giá theo giá trị đơn", "chiết khấu bậc thang"]
auth: public
state_machine: []
deprecated: false
entities: [DonHangCauHinhGiamGia]
stored_procs: []
depends:
  - "Core/TienIch (TienIchAttribute TI_KhuyenMai)"
  - "Redis (CommonCacheService — cache cấu hình theo chi nhánh; ClearCache sau mỗi CUD)"
  - "Log (LogActivityHelper — ghi lịch sử tác động ADD/UPDATE/DELETE)"
cross_module_in:
  - "Order/OrderProcess (GetConfigOrderByPrice / LayDsCauHinhGiamGia — lấy mức giảm theo tổng đơn khi thanh toán)"
cross_module_out:
  - "Redis cache cấu hình giảm giá theo chi nhánh (phải clear sau CUD)"
---

# DonHangCauHinhGiamGia — Cấu hình giảm giá theo đơn hàng

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|
| 1 | Danh sách cấu hình (grid) | Danh sách | POST `TimTheoDieuKien` | `DonHangCauHinhGiamGia_TimTheoDieuKien` (AD-TONG/CN/MCN, QLDV) | public | Lọc theo `IdDonVi + IdChiNhanh` (token), sort `NgayCapNhat` desc, phân trang. | EF (nolock) | — |
| 2 | Thêm mới | Lưu | POST `ThemMoi` | `DonHangCauHinhGiamGia_ThemMoi` (AD-TONG/CN/MCN, QLDV) | public | Validate cấu hình (xem QTNV) + **check overlap khoảng giá**; trùng → "Cấu hình giá trị đã được thiết lập". Ghi log ADD + ClearCache. | EF | — |
| 3 | Cập nhật | Lưu | POST `CapNhatThongTin` | `DonHangCauHinhGiamGia_CapNhatThongTin` | public | Như Create, check overlap loại trừ chính nó (`Id != data.Id`). MarkDirty 5 field. Ghi log UPDATE + ClearCache. | EF | — |
| 4 | Chi tiết theo Id | Mở edit | GET `TimTheoId/{id}` | `DonHangCauHinhGiamGia_TimTheoId` | public | `GetById` đọc từ **Redis** theo chi nhánh rồi FirstOrDefault id. | Redis cache | đọc Redis, không EF → cache lệch nếu chưa clear |
| 5 | Xóa | Xóa | POST `Xoa/{id}` | `DonHangCauHinhGiamGia_Xoa` | public | Tìm theo id+DonVi; null → lỗi. Delete + log DELETE + ClearCache. | EF | route param trong POST body-less |
| 6 | Lấy mức giảm theo giá | — (thu ngân) | GET `GetConfigOrderByPrice/{price}` | `DonHangCauHinhGiamGia_GetConfigOrderByPrice` | public | Trả cấu hình `SuDung==true` mà `price` nằm trong [GiaTriTu, GiaTriDen], order GiaTriDen desc, lấy đầu. price < min → trả null. | Redis cache | — |
| 7 | Lấy danh sách cấu hình đang dùng | — | GET `LayDsCauHinhGiamGia` | `[SBAuthorize()]` (chỉ login) | public | Cấu hình `SuDung==true` của DonVi+ChiNhanh, order GiaTriDen desc. | EF (TableNoTracking) | quyền lỏng hơn các API khác |

## Quy tắc nghiệp vụ (ValidateCauHinhGiamGia + check overlap)
**Validate** (`DonHangCauHinhGiamGiaService.cs:167`):
1. Thiếu bất kỳ `GiaTriTu / GiaTriDen / GiamGiaGiaTri / GiamGiaKieuLoai` → "Vui lòng nhập đầy đủ thông tin".
2. `GiamGiaKieuLoai` không phải PhanTram/Tien → "Kiểu loại giảm giá không hợp lệ".
3. `GiaTriTu >= GiaTriDen` → "Giá trị từ phải nhỏ hơn giá trị đến".
4. PhanTram & `GiamGiaGiaTri > 100` → "Giảm giá kiểu loại không được > 100%".
5. Tien & `GiamGiaGiaTri >= GiaTriTu` → "Giá trị giảm giá phải nhỏ hơn giá trị từ.".

**Check overlap khoảng giá** (Create/Update): tồn tại cấu hình khác cùng DonVi+ChiNhanh mà khoảng [GiaTriTu,GiaTriDen] giao nhau → "Cấu hình giá trị đã được thiết lập". (Update loại trừ `Id` hiện tại.)

**Áp khi thanh toán** (`GetConfigOrderByPrice`): chọn cấu hình `SuDung==true` chứa `price`, ưu tiên `GiaTriDen` lớn nhất.

## Quan hệ
- **Gọi sang:** Redis (CommonCacheService get/set/clear theo chi nhánh), Log (LogActivityHelper).
- **Dữ liệu (entities):** `DonHangCauHinhGiamGia` (scope IdDonVi + IdChiNhanh).
- **Ripple/đồng bộ:** mỗi Create/Update/Delete → `ClearCache_DonHangCauHinhGiamGia_ChiNhanh`; Order gọi GetConfigOrderByPrice/LayDsCauHinhGiamGia lúc thanh toán.

## Lỗi đã biết / lưu ý kỹ thuật
- `DonHangCauHinhGiamGiaService.cs:65-68,104-107` — biểu thức overlap có nhánh thừa/sai: `(data.GiaTriDen >= x.GiaTriDen && data.GiaTriDen <= x.GiaTriDen)` so chính nó (luôn = bằng) → chỉ true khi GiaTriDen trùng đúng; logic overlap không phủ hết trường hợp lồng nhau → có thể tạo được khoảng chồng lấn.
- `DonHangCauHinhGiamGiaService.cs:157-165` — `GetById` đọc từ **Redis** chứ không EF → nếu cache chưa được set/clear đúng, chi tiết hiển thị lệch DB.
- `DonHangCauHinhGiamGiaService.cs:193` — `if (price == null)` so sánh `double` (value type) với null → luôn false (dead check).
- `DonHangCauHinhGiamGiaController.cs:76` — `LayDsCauHinhGiamGia` chỉ `[SBAuthorize()]` (login) trong khi CUD yêu cầu role AD/QLDV → khe hở rò cấu hình.
- `DonHangCauHinhGiamGiaService.cs:144` — Delete clear cache theo `data.IdChiNhanh.Value` (NRE nếu IdChiNhanh null).
