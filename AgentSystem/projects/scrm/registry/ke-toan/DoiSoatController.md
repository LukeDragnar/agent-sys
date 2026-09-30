---
controller: DoiSoatController
module: ke-toan
service: Branch
man_hinh_ui: "nội bộ — không có UI (service Branch). Đối soát đơn hàng được kích từ màn Đơn hàng (Order) qua DonHangController; controller này chỉ nhận lệnh service-to-service."
muc_dich: "2 endpoint nội bộ đối soát / hủy đối soát đơn hàng: đánh dấu (hoặc bỏ đánh dấu) ngày đối soát lên các bản ghi thu-chi đơn hàng, hoa hồng, thưởng doanh thu, tiền tip của 1 đơn; đồng thời gắn cờ thay đổi vào lương chi tiết nếu bảng lương đang xử lý."
alias: ["đối soát", "đối soát đơn hàng", "hủy đối soát", "doi soat don hang"]
auth: internal
state_machine: []
deprecated: false
entities: [ThuChiDonHang, HoaHongNhanVienApDung, ThuongDoanhThuNhanVien, NhanVienTienTip, LuongNhanVien, LuongChiTietNhanVien]
stored_procs: []
depends: [Branch DB transaction (CreateExecutionStrategy)]
cross_module_in: [Order/DonHangController (DoiSoatDonHang, HuyDoiSoatDonHang) → Order/BranchService.DoiSoat_DoiSoatDonHang/_HuyDoiSoatDonHang]
cross_module_out: []
---

# DoiSoatController — Đối soát đơn hàng (nội bộ)

> Controller **internal** (`[PrivateSBAuthorize]` mọi endpoint), không có UI. **Order** là caller: `Order/Order.API/Controllers/DonHangController.cs:1006` (`DoiSoatDonHang`) + `:1019` (`HuyDoiSoatDonHang`) → `donHangThanhToanService` → `Order/Order.Service/Generated/OtherService/BranchService.cs:197` (`/DoiSoat/DoiSoatDonHang`) + `:222` (`/DoiSoat/HuyDoiSoatDonHang`). Cả 2 thao tác chạy trong 1 transaction Branch DB; nếu ngày đối soát thuộc bảng lương đã **Hoàn thành** → chặn.

## Endpoint nội bộ (private / service-to-service)
- **[internal] `POST api/DoiSoat/DoiSoatDonHang`** (`[PrivateSBAuthorize]`) — input `ReqDoiSoatDonHang{IdDonHang, IdTransaction?, NgayDoiSoat}`. Validate IdDonHang + NgayDoiSoat (IdTransaction tạm không bắt buộc — để đối soát đơn cũ chưa lưu IdTransaction). Chặn nếu bảng lương phủ NgayDoiSoat ở trạng thái **HoanThanh**. Set `DoiSoatNgayGiayTo/NgayThucHien/BoiUserId/UserName` lên `ThuChiDonHang` + `HoaHongNhanVienApDung` + `ThuongDoanhThuNhanVien` + `NhanVienTienTip` của đơn (lọc theo IdTransaction nếu có). Nếu bảng lương đang **DangXuLy** → gắn cờ `DoiSoatThayDoi=true` + loại thay đổi vào `LuongChiTietNhanVien`. Commit/rollback chung 1 transaction. `DoiSoatService.cs:49`.
- **[internal] `GET api/DoiSoat/HuyDoiSoatDonHang?idDonHang=&idTransaction=`** (`[PrivateSBAuthorize]`) — xóa (set null) các field DoiSoat* trên cùng 4 nhóm bản ghi. Lấy ngày đối soát hiện có để check bảng lương; nếu lương đã **HoanThanh** → chặn ("không thể chỉnh sửa"). Lương **DangXuLy** → vẫn gắn cờ thay đổi. `DoiSoatService.cs:225`.

## Quan hệ
- **Gọi sang:** không gọi service ngoài; chỉ ghi Branch DB qua `IRepository.UpdateAsync` trong transaction.
- **Dữ liệu (entities):** `ThuChiDonHang`, `HoaHongNhanVienApDung`, `ThuongDoanhThuNhanVien`, `NhanVienTienTip` (đánh/bỏ dấu đối soát theo IdHoaDon/IdDonHang); `LuongNhanVien` (check trạng thái), `LuongChiTietNhanVien` (gắn cờ `DoiSoatThayDoi`).
- **Ripple / đồng bộ:** gắn cờ thay đổi → bắt buộc cập nhật lại lương chi tiết trước khi hoàn thành bảng lương.

## Luồng xuyên module
- **Order: user đối soát đơn** → `DonHangController.DoiSoatDonHang` → `BranchService.DoiSoat_DoiSoatDonHang` → `POST /api/DoiSoat/DoiSoatDonHang` → đánh dấu đối soát + (nếu lương DangXuLy) gắn cờ `DoiSoatThayDoi` lên `LuongChiTietNhanVien`.
- **Order: hủy đối soát** → tương tự gọi `HuyDoiSoatDonHang` → reset field DoiSoat*.

## Lỗi đã biết / lưu ý kỹ thuật
- `DoiSoatService.cs:54,57,232` — IdTransaction tạm KHÔNG required (comment TODO) → đối soát có thể không khoá đúng transaction với đơn nhiều giao dịch (lọc `IdTransaction==null||Empty` bỏ qua filter).
- `DoiSoatService.cs:204,381` — trong catch của transaction chỉ set `messErr = ex.Message` rồi rollback; ngoài transaction mới kiểm tra messErr để trả lỗi (OK nhưng nuốt stacktrace).
- Auth `internal`: không có key SBAuthorize — chỉ service nội bộ gọi được; nếu lộ ra ngoài, không có phân quyền theo role.
