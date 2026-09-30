---
alias: ["cấu hình mặt hàng mặc định", "mặt hàng mặc định khi tạo đơn", "cấu hình giá tại quầy", "dịch vụ tính giờ", "dịch vụ chuyển bàn", "cấu hình bàn POS", "cấu hình giảm giá đơn hàng"]
module: cua-hang
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: "cần product xác nhận — màn cấu hình mặt hàng mặc định có phân quyền riêng cho Thêm/Sửa/Xóa. Một số chức năng phục vụ POS chỉ cần đăng nhập; phần lấy giá tính giờ theo khu vực hiện công khai (không cần đăng nhập) — xem dev.md."
phu_thuoc: ["Sản phẩm / Dịch vụ / Combo (gán làm mặt hàng mặc định)", "Khu vực & Chỗ ngồi / Bàn (Booking)", "Dịch vụ tính giờ & bảng giá theo khung giờ", "POS thu ngân (nơi áp dụng cấu hình)"]
link_flow: ./flow.md
---

# Cấu hình giá & chiết khấu (mặt hàng mặc định khi tạo đơn)

## Là gì
Đây là nơi **khai báo trước các "bộ mặt hàng mặc định"** để khi bán tại quầy (POS thu ngân) hệ thống **tự gợi ý sẵn sản phẩm/dịch vụ/combo** cho từng bàn/khu vực, kèm **dịch vụ tính giờ** (tính tiền theo thời gian sử dụng) và **dịch vụ chuyển bàn**. Mỗi bộ cấu hình gắn với khu vực/chỗ ngồi cụ thể; khi thu ngân chọn bàn, các món mặc định và cách tính giờ sẽ áp theo cấu hình đó.

> Lưu ý về "chiết khấu/giảm giá đơn hàng": phần **cấu hình giảm giá đơn theo mức giá** (tính năng cũ) **đã ngừng hoạt động** (bị vô hiệu hoàn toàn). Việc giảm giá/chiết khấu trên đơn hiện chạy qua **voucher / khuyến mãi** và **chiết khấu theo bill** ở màn bán hàng — xem feature *Tạo & quản lý đơn hàng*.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Thêm bộ cấu hình mặt hàng mặc định | ✅ | quyền thêm; gán SP/DV/combo + khu vực/bàn + DV tính giờ/chuyển bàn |
| Sửa bộ cấu hình | ✅ | quyền sửa |
| Xóa bộ cấu hình | ✅ | quyền xóa (xóa cứng) |
| Bật/tắt "Trạng thái sử dụng" | ✅ | toggle per-row |
| Bật/tắt "Trạng thái mặc định" | ✅ | mỗi đơn vị + chi nhánh chỉ 1 bộ mặc định tại một thời điểm |
| Gợi ý SP/DV mặc định khi tạo đơn tại quầy | ✅ | tự động ở POS theo bàn/khu vực |
| Tính tiền theo giờ (dịch vụ tính giờ) | ✅ | khi bộ cấu hình có khai báo dịch vụ tính giờ + bảng giá khung giờ |
| **Cấu hình giảm giá đơn theo mức giá (cũ)** | ❌ | tính năng đã ngừng — toàn bộ màn bị vô hiệu; dùng voucher/KM/chiết khấu theo bill thay thế |

## Để làm gì / cho ai
- **Quản lý / chủ shop (mô hình quán/spa theo bàn):** khai báo sẵn món mặc định cho từng khu vực để thu ngân lên đơn nhanh; thiết lập dịch vụ tính giờ (vd phòng hát, bàn bi-a) và dịch vụ chuyển bàn.
- **Thu ngân:** khi chọn bàn, hệ thống tự bơm món mặc định + áp cách tính giờ → tạo đơn nhanh, ít sai sót.

## Cách dùng
**Tạo/sửa bộ cấu hình mặt hàng mặc định:**
1. Vào **Cửa hàng / Kho → Cấu hình mặt hàng mặc định**.
2. Bấm **Thêm mới cấu hình** (mở form): đặt **Tên**, chọn **sản phẩm/dịch vụ/combo** làm mặc định.
3. Chọn **khu vực/chỗ ngồi (bàn)** áp dụng; nếu là mô hình tính giờ thì chọn **dịch vụ tính giờ** (DV phải được đánh dấu là dịch vụ tính giờ) và **dịch vụ chuyển bàn** (nếu có).
4. Lưu. Nếu bật **mặc định**, các bộ cấu hình khác cùng chi nhánh sẽ tự bỏ mặc định.
5. Có thể **sửa**, **xóa**, bật/tắt **sử dụng**, bật/tắt **mặc định** trực tiếp trên danh sách.

## Cần chú ý
- **Mỗi đơn vị + chi nhánh chỉ có một bộ cấu hình "mặc định" tại một thời điểm.** Bật mặc định cho bộ mới sẽ tự tắt mặc định ở bộ cũ.
- **Một bàn/chỗ ngồi không nên cấu hình trùng ở 2 bộ** — hệ thống chặn và báo bộ nào đang trùng bàn.
- **Dịch vụ tính giờ** chỉ chọn được dịch vụ đã được cài đặt là "dịch vụ tính giờ"; **dịch vụ chuyển bàn** phải là dịch vụ tính giờ + dịch vụ bàn.
- **Sau khi xóa một bộ cấu hình, POS có thể còn hiển thị bộ đã xóa trong một thời gian** do cache chưa được làm mới đúng (xem `dev.md` — known-issue). Nếu gặp, đợi cache hết hạn hoặc thực hiện thêm một thao tác ghi khác.
- **Khai báo trống thì POS không có gợi ý** — nếu chưa cấu hình bộ mặc định/sử dụng, màn bán hàng sẽ không tự bơm món.

## Ảnh hưởng tới
- **Màn bán hàng tại quầy (POS thu ngân)** — lấy bộ cấu hình mặc định/sử dụng để gợi ý món và tính giờ theo bàn.
- **Giá tính theo giờ** — hiển thị/tính tiền theo bảng giá khung giờ của dịch vụ tính giờ.
- Không trực tiếp ảnh hưởng tới giảm giá đơn (giảm giá nay nằm ở voucher/KM/chiết khấu theo bill).
