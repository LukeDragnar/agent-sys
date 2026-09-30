---
alias: ["nhà cung cấp", "quản lý NCC", "danh sách nhà cung cấp", "nhóm nhà cung cấp", "supplier"]
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: Admin tổng / Kế toán / Quản lý kho (xóa NCC chỉ Admin tổng/CN/MCN; thêm/sửa nhóm NCC chỉ Admin tổng)
phu_thuoc: [Khách hàng (NCC đồng thời là KH), Phiếu mua hàng, Nhân viên mua hàng, Đối tác trả góp, Tài khoản kế toán công nợ]
link_flow: ./flow.md
module: kho
---

# Quản lý nhà cung cấp

## Là gì
Nơi lưu **danh bạ các đơn vị/cá nhân mà cửa hàng nhập hàng** (nhà cung cấp — NCC). Mỗi NCC có **mã riêng, tên, người liên hệ, SĐT, thông tin ngân hàng, điều khoản công nợ** (số ngày được nợ). Kèm theo là **Nhóm nhà cung cấp** để phân loại NCC cho dễ quản lý/lọc.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Thêm NCC | ✅ | tối thiểu Tên + Mã + SĐT; mã NCC duy nhất trong đơn vị |
| Sửa NCC | ✅ | NCC sinh tự động từ sàn TMĐT không cho sửa |
| Xóa NCC | ⚠️ | chỉ Admin tổng / CN / MCN; bị chặn nếu đã có phiếu mua hàng hoặc đang liên kết khách hàng |
| Nhập (import) Excel NCC | ✅ | quyền import |
| Xuất Excel NCC | ✅ | quyền xuất |
| Bật "đồng thời là khách hàng" | ✅ | đồng bộ sang module Khách hàng |
| Thêm / Sửa nhóm NCC | ✅ | chỉ Admin tổng (DV-AD-TONG) |
| Xóa nhóm NCC | ✅ | theo phân quyền nhóm NCC |

## Để làm gì / cho ai
- **Cho ai:** kế toán, quản lý kho, admin cửa hàng.
- **Để làm gì:** chọn nhanh NCC khi lập **phiếu mua hàng / đơn đặt hàng**, theo dõi **công nợ phải trả**, gom NCC theo **nhóm** để báo cáo, và import/export danh sách NCC bằng Excel.

## Cách dùng
1. Vào menu Kho → **Nhà cung cấp**. Bấm **Thêm mới**, nhập tối thiểu **Tên + Mã + Số điện thoại**.
2. Tùy chọn thêm: nhóm NCC, nhân viên mua hàng phụ trách, số tài khoản ngân hàng, số ngày được nợ / nợ tối đa, tài khoản công nợ phải trả.
3. Có thể bật **"Đồng thời là khách hàng"** nếu đối tác này vừa bán vừa mua.
4. Tạo trước các **Nhóm nhà cung cấp** ở màn hình riêng để chọn vào ô "Nhóm" khi thêm NCC.
5. Tìm kiếm theo mã/tên/người liên hệ; **Xuất Excel** hoặc **Import** danh sách hàng loạt.

## Cần chú ý (cạm bẫy)
- **Mã NCC là duy nhất** trong cùng đơn vị — nhập trùng mã sẽ báo lỗi, không lưu được.
- **Mã NCC và mã khách hàng dùng chung "không gian"**: nếu mã trùng với một khách hàng đã có, hệ thống sẽ hỏi xác nhận "đồng thời là khách hàng" — đây là chủ ý, không phải bug.
- **Số ngày nợ tối đa phải ≥ số ngày được nợ**, nếu không sẽ báo lỗi.
- **Không xóa được NCC** nếu đã phát sinh **phiếu mua hàng** hoặc đang liên kết **khách hàng**.
- NCC sinh **tự động** (từ sàn TMĐT) **không cho sửa**.

## Ảnh hưởng tới
- **Phiếu mua hàng / Đơn đặt hàng** — chọn NCC từ danh bạ này.
- **Công nợ phải trả** — điều khoản nợ và tài khoản công nợ lấy từ hồ sơ NCC.
- **Khách hàng** — khi bật "đồng thời là khách hàng", thông tin được đồng bộ sang module Khách hàng.

## Link
→ [flow.md](./flow.md) · [faq.md](./faq.md) · Kỹ thuật: [dev.md](./dev.md)
