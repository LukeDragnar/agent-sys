---
alias: ["thưởng phạt nhân viên", "hoa hồng nhân viên", "hoa hồng NV", "thưởng doanh số", "cấu hình hoa hồng", "bậc thưởng doanh số", "khoán hoa hồng"]
module: nhan-su
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: "Thưởng phạt: các thao tác có key riêng (ThuongPhatNhanVien_ThemMoiThuongPhatNv, _XoaTheoId, _TimTheoDieuKien…); danh mục thưởng phạt (DMThuongPhat) ghi cần AD-TONG, đọc mở thêm KT/QLDV. Hoa hồng nhân viên: phần LƯU cấu hình giới hạn AD/QLDV (bỏ KT); một số endpoint enum không có phân quyền (anonymous). Thưởng doanh số: thao tác có key (ThuongDoanhSo_ThemMoiAsync, _GetListThuongDoanhSo). Vai cụ thể đầy đủ: cần product xác nhận."
phu_thuoc: ["Danh mục thưởng phạt (loại thưởng/phạt)", "Hồ sơ nhân viên (gắn thưởng phạt/hoa hồng theo NV)", "Nhóm hoa hồng (HH custom theo nhóm)", "Đơn hàng (đơn gọi vào để tính & áp hoa hồng cho NV)", "Sản phẩm/dịch vụ/thẻ/combo (đối tượng tính thưởng doanh số)", "Tính lương / tổng hợp công (lấy tổng thưởng phạt làm đầu vào)"]
link_flow: ./flow.md
---

# Thưởng phạt & hoa hồng nhân viên

## Là gì
Đây là nơi **cấu hình và áp các khoản thưởng/phạt và hoa hồng** cho nhân viên. Gồm 3 mảng:
- **Thưởng phạt nhân viên**: ghi từng khoản thưởng hoặc phạt (theo loại trong danh mục) cho một nhân viên; xem tổng thưởng/phạt/khấu trừ của họ.
- **Hoa hồng nhân viên**: cấu hình hoa hồng nhiều tầng — theo đối tượng (dịch vụ/sản phẩm/thẻ/combo) cho từng nhân viên, hoa hồng tối đa (trần), hoa hồng mặc định, và hoa hồng theo nhóm hoa hồng. Khi tạo đơn, hệ thống dựa cấu hình này để tính hoa hồng cho nhân viên.
- **Thưởng doanh số**: cấu hình thưởng theo **bậc doanh số** — mỗi cấu hình gắn nhóm nhân viên + nhóm đối tượng + các mốc doanh số (từ → đến → mức thưởng theo VND hoặc %).

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Thêm khoản thưởng/phạt cho nhân viên | ✅ | có key `ThuongPhatNhanVien_ThemMoiThuongPhatNv` |
| Xóa khoản thưởng/phạt | ✅ | có key `_XoaTheoId` |
| Xem thưởng/phạt + tổng theo nhân viên | ✅ | theo quyền xem |
| Thêm / sửa / xóa danh mục loại thưởng phạt | ✅ | ghi: chỉ AD-TONG; đọc: mở thêm KT/QLDV |
| Cấu hình hoa hồng theo đối tượng-nhân viên | ✅ | lưu: AD/QLDV (không cho KT) |
| Cấu hình hoa hồng tối đa (trần) | ✅ | lưu: AD/QLDV |
| Cấu hình hoa hồng mặc định theo đối tượng | ✅ | lưu: AD/QLDV |
| Cấu hình hoa hồng theo nhóm hoa hồng | ✅ | theo quyền lưu |
| Áp hoa hồng mặc định cho NV chưa có | ✅ | theo quyền |
| Xem lịch sử hoa hồng theo nhân viên | ✅ | theo quyền xem |
| Xóa cấu hình hoa hồng | ✅ | có key (xóa thuộc tính mở rộng: AD-TONG) |
| Lưu cấu hình thưởng theo bậc doanh số | ✅ | có key; **LƯU = thay toàn bộ cấu hình của chi nhánh** (xem "Cần chú ý") |
| Xem danh sách cấu hình thưởng doanh số | ✅ | theo quyền |

## Để làm gì / cho ai
- **Chủ shop / quản lý:** thiết lập chính sách thưởng phạt, hoa hồng, thưởng theo doanh số để khuyến khích nhân viên; ghi thưởng/phạt phát sinh.
- **Kế toán:** xem tổng thưởng/phạt/khấu trừ để đưa vào lương (lưu ý: KT không được sửa cấu hình hoa hồng).
- **Nhân viên:** hưởng hoa hồng theo đơn đã làm và thưởng theo doanh số đạt được.

## Cách dùng
**Ghi thưởng/phạt cho nhân viên:**
1. Khai báo trước **danh mục loại thưởng/phạt** (cấu hình > thưởng phạt > danh mục) — cần admin tổng.
2. Vào hồ sơ nhân viên → tab Thưởng phạt → thêm khoản (chọn loại, số tiền) → lưu. Có thể xem tổng thưởng/phạt/khấu trừ của nhân viên.

**Cấu hình hoa hồng:**
1. Vào **Cấu hình → Hoa hồng nhân viên**.
2. Chọn tầng cấu hình: theo đối tượng-nhân viên, hoa hồng tối đa, mặc định theo đối tượng, hoặc theo nhóm hoa hồng → nhập giá trị → lưu (AD/QLDV).
3. Có thể "áp mặc định" cho nhân viên chưa có cấu hình riêng.
4. Khi bán hàng, hệ thống tự tính hoa hồng cho nhân viên theo cấu hình này.

**Cấu hình thưởng doanh số:**
1. Vào **Cấu hình → Thưởng doanh số**.
2. Tạo cấu hình: đặt tên, chọn nhóm nhân viên + đối tượng (dịch vụ/SP/thẻ/combo), nhập các bậc (doanh số từ → đến → mức thưởng VND hoặc %).
3. Bấm **Lưu** → hệ thống **thay toàn bộ** cấu hình thưởng doanh số của chi nhánh bằng danh sách vừa nhập.

## Cần chú ý
- **LƯU thưởng doanh số là "thay toàn bộ" theo chi nhánh**: mỗi lần lưu, hệ thống **xóa hết cấu hình cũ của chi nhánh rồi ghi mới**. Nếu gửi danh sách rỗng/thiếu thì sẽ **xóa sạch** cấu hình — phải gửi đầy đủ mỗi lần lưu.
- **Quy tắc bậc doanh số**: doanh số "đến" phải lớn hơn "từ"; mỗi mốc không được để 0/để trống; nếu thưởng theo % thì không quá 100%. Sai sẽ báo lỗi và không lưu.
- **Hoa hồng: bản ghi "mặc định" là cấu hình gắn nhân viên rỗng** — đây là cách hệ thống phân biệt "mặc định nhóm/đối tượng" với "hoa hồng riêng từng nhân viên". Khi cấu hình, NV có cấu hình riêng được ưu tiên; ai chưa có thì lấy mặc định.
- **Kế toán không sửa được cấu hình hoa hồng** (chỉ xem) — đây là thiết kế phân quyền, không phải lỗi.
- **Một số nút "xóa" hoa hồng và thao tác tính hoa hồng dùng phương thức kỹ thuật không chuẩn** (xóa bằng GET, vài endpoint enum mở công khai) — không ảnh hưởng người dùng nhưng là lưu ý kỹ thuật (xem dev.md).
- **Quyền xem hoa hồng trong đơn**: quản lý/admin xem được hoa hồng của tất cả nhân viên trong đơn; nhân viên thường chỉ xem hoa hồng của chính mình.

## Ảnh hưởng tới
- **Đơn hàng:** khi xếp nhân viên vào đơn, hệ thống gọi cấu hình hoa hồng để tính hoa hồng từng người cho từng mặt hàng.
- **Tính lương / tổng hợp công:** tổng thưởng/phạt/khấu trừ và hoa hồng là đầu vào để tính lương.
- **Đối soát hoa hồng:** đơn hàng có thể tra lại hoa hồng đã áp theo mã đơn.
