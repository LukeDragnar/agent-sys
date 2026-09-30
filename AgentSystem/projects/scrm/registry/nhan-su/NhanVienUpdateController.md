---
controller: NhanVienUpdateController
module: nhan-su
service: Branch
man_hinh_ui: "nội bộ — không có UI. Toàn bộ endpoint do Order gọi service-to-service khi thanh toán / rollback đơn hàng để sinh & gỡ dữ liệu nhân viên (tip, hoa hồng, thưởng doanh thu) và thu-chi liên quan."
muc_dich: "Khi đơn hàng được thanh toán: tính & ghi tiền tip + hoa hồng nhân viên + thưởng doanh thu (chỉ lần thanh toán đầu) trong 1 transaction Branch. Khi đơn lỗi/hoàn tác: rollback các dữ liệu nhân viên đó và rollback phiếu thu-chi (theo đơn hoặc theo IdTransaction)."
alias: ["nhân viên update", "tính hoa hồng nhân viên", "tiền tip nhân viên", "thưởng doanh thu", "rollback thanh toán đơn hàng", "rollback thu chi"]
auth: internal
state_machine: []
deprecated: false
entities: [NhanVienTienTip, HoaHongNhanVienApDung, HoaHongNhanVienApDungChiTiet, HoaHongNhanVienApDungTong, ThuongDoanhThuNhanVien, ThuongDoanhSoChiTiet, ThuChi, ThuChiDonHang]
stored_procs: []
depends: [Customer/KhachHang_TimTheoId (lấy KH để tính hoa hồng), HoaHongNhanVienApDungService (init hoa hồng), ThuChiService (rollback thu chi), JobSyncThuChiKeToan, Branch DB transaction]
cross_module_in: [Order/CommonService (NhanVienUpdate), Order/BranchService (RollBackThanhToanDonHang, RollbackThuChi)]
cross_module_out: [Accounting (rollback/đồng bộ phiếu thu-chi qua JobSyncThuChiKeToan)]
---

# NhanVienUpdateController — Cập nhật/rollback dữ liệu nhân viên khi thanh toán đơn (nội bộ)

> Controller **internal**, KHÔNG có UI. Mọi endpoint `[PrivateSBAuthorize]` (trừ 1 endpoint rollback theo IdTransaction đã bị comment auth — xem known-issue). Caller là **Order**: `Order/Order.Service/Generated/Common/CommonService.cs:408` (`/NhanVienUpdate/NhanVienUpdate`), `Order/Order.Service/Generated/OtherService/BranchService.cs:130` (`/NhanVienUpdate/RollBackThanhToanDonHang`) + `:151` (`/NhanVienUpdate/RollbackThuChi`). Ghi dữ liệu trong transaction Branch DB.

## Endpoint nội bộ (private / service-to-service)
- **[internal] `POST api/NhanVienUpdate/NhanVienUpdate`** (`[PrivateSBAuthorize]`) — input `DanhSachDonHangTheoKhachHang`. Lấy KH qua Customer `KhachHang_TimTheoId` (nếu IdKhachHang≠Empty, lỗi → trả lỗi). Duyệt từng đơn: chỉ tính **tiền tip** + **thưởng doanh thu** khi `SoTienDaThanhToan==0` (lần thanh toán đầu); luôn init **hoa hồng nhân viên** (ApDung + ChiTiet + Tong) qua `HoaHongNhanVienApDungService`. Có thay đổi → ghi tất cả trong 1 transaction. `NhanVienUpdateService.cs:96`. **Caller:** Order `CommonService.cs:408` (gọi khi thanh toán đơn hàng).
- **[internal] `POST api/NhanVienUpdate/RollBackThanhToanDonHang`** (`[PrivateSBAuthorize]`) — gỡ dữ liệu nhân viên (tip/hoa hồng/thưởng) của đơn khi rollback thanh toán. `NhanVienUpdateService.cs:422`. **Caller:** Order `BranchService.cs:130`.
- **[internal] `POST api/NhanVienUpdate/RollbackThuChi`** (`[PrivateSBAuthorize]`) — rollback phiếu thu-chi + thu-chi đơn hàng của đơn (qua ThuChiService). `NhanVienUpdateService.cs:597`. **Caller:** Order `BranchService.cs:151`.
- **[note] `POST api/NhanVienUpdate/RollBackThanhToanDonHangByIdTransaction`** — luồng đơn mới rollback theo `IdTransaction`. `[PrivateSBAuthorize]` đã bị **comment** (`NhanVienUpdateController.cs:61`) → hiện chỉ còn auth controller mặc định (không class-level [SBAuthorize] → endpoint này thực tế gần như mở; xem known-issue). `NhanVienUpdateService.cs:514`.

## Quan hệ
- **Gọi sang:** Customer `KhachHang_TimTheoId` (tính hoa hồng theo KH), `HoaHongNhanVienApDungService.InitHoaHongNhanVienApDung`, `ThuChiService` (rollback thu chi), `JobSyncThuChiKeToan`.
- **Dữ liệu (entities):** `NhanVienTienTip`, `HoaHongNhanVienApDung(+ChiTiet/+Tong)`, `ThuongDoanhThuNhanVien`, `ThuongDoanhSoChiTiet` (tạo/gỡ); `ThuChi` + `ThuChiDonHang` (rollback).
- **Ripple / đồng bộ:** rollback thu-chi kéo theo đồng bộ ngược sang Accounting (qua JobSyncThuChiKeToan); dữ liệu hoa hồng/thưởng/tip là nguồn cho tính lương + đối soát (xem DoiSoatController).

## Luồng xuyên module
- **Order thanh toán đơn** → `NhanVienUpdate` → tính tip/hoa hồng/thưởng (chỉ lần đầu với tip & thưởng) → ghi Branch trong transaction → là input cho lương + đối soát.
- **Order rollback đơn** → `RollBackThanhToanDonHang` (gỡ dữ liệu NV) + `RollbackThuChi` (gỡ phiếu thu-chi) → đồng bộ ngược Accounting.

## Lỗi đã biết / lưu ý kỹ thuật
- `NhanVienUpdateController.cs:61` — `RollBackThanhToanDonHangByIdTransaction` đã comment `[PrivateSBAuthorize]`; class không có `[SBAuthorize]` → endpoint này **không còn auth nội bộ** (rủi ro: gọi rollback từ ngoài bằng IdTransaction).
- `NhanVienUpdateService.cs:136,174` — tip & thưởng chỉ tính khi `SoTienDaThanhToan==0` (lần thanh toán đầu); thanh toán bổ sung KHÔNG sinh lại tip/thưởng (đúng ý đồ, nhưng dễ hiểu nhầm).
- `NhanVienUpdateService.cs:198` — khi tính thưởng lỗi → `throw new Exception(messageError)` (ném ra ngoài transaction, không trả MethodResult lỗi mềm).
- Auth `internal`: không phân quyền theo role; phụ thuộc hoàn toàn `[PrivateSBAuthorize]`.
