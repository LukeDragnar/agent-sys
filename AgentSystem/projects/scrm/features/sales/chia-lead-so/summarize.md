---
alias: ["chia số", "chia lead", "phân bổ lead", "chia số tự động", "chia số thủ công", "chia số theo nguồn", "bật tắt nhận lead", "cấu hình chia số"]
module: sales
trang_thai: cần cấu hình
goi: cần product xác nhận
quyen: "cần product xác nhận — phần lớn thao tác yêu cầu cụm role admin/quản lý đơn vị, nhưng một số màn hiện bị hạ quyền (chỉ cần đăng nhập) — chi tiết kỹ thuật xem dev.md. Riêng 'Bật/tắt giới hạn check trùng số' chỉ role HT-CSKH (toàn hệ thống)."
phu_thuoc: ["Nguồn dữ liệu / Landing (chia theo nguồn)", "Phòng ban / Nhóm nhân viên / Nhân viên sale-cskh (đích nhận lead)", "Contact / khách hàng (đối tượng được phân bổ)", "Kho số (cấu hình team/nhóm sale nhận data ảnh hưởng pool kho số)", "Sản phẩm + Ca làm việc (điều kiện trong cấu hình chia tự động)"]
link_flow: ./flow.md
---

# Chia Lead / số (phân bổ lead cho sale)

## Là gì
Đây là **trung tâm cấu hình và vận hành việc chia lead** (chia số) cho đội sale / CSKH. Khi có lead/contact mới chưa có người phụ trách, hệ thống dựa vào cấu hình ở đây để **chia cho ai, theo định mức nào, theo nguồn nào**. Có 3 cách chia chính:
- **Chia theo nguồn**: mỗi nguồn dữ liệu (landing/kênh) gán sẵn một kiểu chia mặc định.
- **Chia tự động**: cài "công thức" chia (theo nhóm/nhân viên, định mức số lead mỗi người, theo sản phẩm, theo ca làm việc) để hệ thống tự chia.
- **Chia thủ công**: admin/quản lý tự tay phân bổ lead chưa có chủ về nhóm hoặc nhân viên cụ thể.

Ngoài ra còn các công tắc **bật/tắt nhận lead** cho từng tài khoản / nhóm sale-cskh, và bật/tắt giới hạn kiểm tra trùng số.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Cấu hình chia số theo nguồn | ✅ | tùy quyền; gán kiểu chia mặc định cho từng nguồn/chi nhánh |
| Thêm / Sửa cấu hình chia tự động | ✅ | tùy quyền; đặt tên, định mức, nhóm/NV, sản phẩm, ca làm việc |
| Xóa cấu hình chia tự động | ✅ | tùy quyền |
| Xem chi tiết cấu hình chia tự động | ✅ | tùy quyền |
| Phân bổ thủ công lead về nhóm / sale (theo định mức) | ✅ | tùy quyền; chỉ lead chưa có chủ |
| Phân bổ ngay lead cụ thể về nhóm / NV | ✅ | tùy quyền |
| Bật/tắt nhận lead cho 1 hoặc nhiều tài khoản | ✅ | tùy quyền |
| Bật/tắt nhóm sale-cskh nhận data | ✅ | tùy quyền |
| Bật/tắt giới hạn kiểm tra trùng số | ⚠️ | chỉ role HT-CSKH (cấp hệ thống) |
| Xem lịch sử thay đổi cấu hình chia số | ✅ | mọi user đăng nhập đều xem được (xem lưu ý dev.md) |

## Để làm gì / cho ai
- **Quản lý sale / chủ shop:** đảm bảo lead được chia công bằng và đúng người, theo nguồn, theo định mức, theo ca trực — không để lead bị bỏ rơi hoặc dồn vào một người.
- **Admin đơn vị:** cài cấu hình chia tự động và xử lý các lead tồn chưa chia bằng phân bổ thủ công.
- **Nhân viên sale/CSKH:** được bật/tắt nhận lead tùy tình trạng (đang bận, nghỉ, hết định mức…).

## Cách dùng
1. Vào màn **Chia số**, có các tab: **Chia số theo nguồn**, **Chia số thủ công**, **Chia số tự động**, **Bật/tắt nhận lead**, **Bật/tắt nhóm sale-cskh**.
2. **Chia theo nguồn**: chọn nguồn dữ liệu → gán kiểu chia mặc định → Lưu.
3. **Chia tự động**: bấm thêm cấu hình, đặt tên, chọn nhóm sale/cskh và nhân viên, đặt định mức mỗi người, chọn sản phẩm/ca làm việc nếu cần → Lưu. Hệ thống sẽ dùng cấu hình này để tự chia lead mới.
4. **Chia thủ công**: chọn nguồn/contact chưa phân bổ → chọn nhóm/NV đích → **Áp dụng phân bổ** (theo định mức) hoặc **Phân bổ ngay** (gán lead cụ thể).
5. **Bật/tắt nhận lead**: trong tab tương ứng, bật/tắt cho từng tài khoản hoặc cả nhóm.

## Cần chú ý
- **Lead chỉ chia được khi chưa có chủ.** Phân bổ thủ công chỉ áp cho contact chưa phân bổ; lead đã có người phụ trách không bị chia lại tự động.
- **Định mức và "nhận data" quyết định ai được chia.** Nếu một sale tắt nhận data, hoặc đã đủ định mức, lead sẽ không chia về người đó. Đây là cấu hình, không phải lỗi.
- **Đổi cấu hình ảnh hưởng cả runtime tự chia.** Cấu hình tạo ở đây được phần chia tự động đọc khi có lead mới — sửa cấu hình là đổi cách chia cho mọi lead về sau.
- **Phân bổ lead là thao tác có "ripple":** đổi chủ contact ảnh hưởng tới kho số và đơn hàng của contact đó.
- **"Bật/tắt giới hạn check trùng số" là quyền cấp hệ thống** (HT-CSKH), không phải admin đơn vị thường thao tác.
- Lịch sử thay đổi cấu hình chia số hiện ai đăng nhập cũng xem được (xem lưu ý kỹ thuật ở dev.md).

## Ảnh hưởng tới
- **Contact / khách hàng:** lead được gán nhân viên/nhóm phụ trách (`PhanBoTuDau`), ghi log tác nghiệp.
- **Kho số:** cấu hình nhóm/team nhận data ảnh hưởng pool kho số; đổi chủ contact ảnh hưởng việc nhặt số.
- **Đơn hàng của lead:** đổi người phụ trách kéo theo cập nhật trên đơn hàng liên quan.
- **Chia tự động (runtime):** dùng đúng cấu hình ở đây để tự chia lead mới về sale/cskh.
