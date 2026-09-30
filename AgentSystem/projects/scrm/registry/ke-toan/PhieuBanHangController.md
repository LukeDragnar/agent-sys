---
controller: PhieuBanHangController
module: ke-toan
service: Accounting
man_hinh_ui: "Kế toán > Chứng từ bán hàng > màn 'Phiếu bán hàng'. Lưới list + bộ lọc (loại ngày, khoảng ngày, loại bán, trạng thái thanh toán, trạng thái hoá đơn, keyword). Thống kê tình trạng hoá đơn (đã xuất / chưa xuất / xuất muộn). Phần lớn ghi là tự động từ luồng đơn hàng (internal), UI chủ yếu xem/lọc/xuất Excel/tạo phiếu bán trả lại."
muc_dich: "Quản lý chứng từ phiếu bán hàng kế toán (sinh tự động khi đơn hàng/logistics hoàn tất). Kéo theo bút toán doanh thu/giá vốn, phiếu xuất kho, phiếu thu, chứng từ khác (chuyển nợ, giảm doanh thu phí ship) + đồng bộ MISA. Theo dõi trạng thái xuất hoá đơn."
alias: ["phiếu bán hàng", "phieu ban hang", "PBH", "chứng từ bán hàng", "hoá đơn bán hàng kế toán", "xuất hoá đơn"]
auth: mixed
state_machine: [DangXuLy, DaHoanThanh, HuyThanhToan, ChuaXuat-HoaDon, DaXuat-HoaDon, XuatMuon-HoaDon]
deprecated: false
entities: [PhieuBanHang, PhieuBanHangSanPham, PhieuBanHangMongo, DataReceiveTaoPhieu]
stored_procs: []
depends: [ButToanService (ButToan_PhieuBanHang_Save / ButToan_PhieuBanHangBiTraLai_Save), WarehouseService (GetInfoKhoKeToanByListId / giá vốn, ThemMoiFromPhieuMuaHang phiếu nhập trả lại), BranchService (TaoPhieuChiFromBanHangTraLai), CommonService (ThemMoiFromPhieuBanHang phiếu xuất, TaoPhieuThuBanHang), BuTruCongNoService, ChungTuKhacService, MisaDanhMucSync, MongoDB PhieuBanHangMongo]
cross_module_in: [OrderLogistic/luồng đơn hàng (PhieuBanHang_SaveTuDongChoDonHang, ThemMoiAsync), DoiSoat/reconciliation (GetPhieuBanHangByMaDons), Branch/PhieuThu callback (CapNhatTrangThaiPhieuBanHangAndPhieuThu), luồng hoá đơn (PhieuBanHang_UpdateTrangThaiHoaDonPhieuBan)]
cross_module_out: [WarehouseProduct/PhieuXuat (xuất kho), Branch/PhieuThu-PhieuChi (thu/chi), ButToan/Accounting (bút toán doanh thu/giá vốn), ChungTuKhac (chuyển nợ / giảm doanh thu phí ship), BuTruCongNo (rollback), MISA (DataReceiveTaoPhieu + đồng bộ danh mục), PhieuBanTraLai (sinh phiếu bán trả lại)]
---

# PhieuBanHangController — Phiếu bán hàng (chứng từ bán, sinh tự động từ đơn → bút toán + xuất kho + thu)

> Chứng từ kế toán cho bán hàng, **phần lớn sinh tự động** từ luồng đơn hàng/logistics — nhiều endpoint là `[PrivateSBAuthorize]` (internal). Lưu lịch sử thay đổi vào **MongoDB** (`PhieuBanHangMongo`). State phiếu: DangXuLy → DaHoanThanh (khi phiếu nhập/thu xong + khớp số tiền). State thanh toán: HuyThanhToan (khi rollback). State hoá đơn (`IdTinhTrangHoaDon`): ChuaXuat → DaXuat / XuatMuon (so với `NgayTieuChuanXuatHoaDon`). Mọi create/update đẩy bút toán + đồng bộ MISA qua bảng `DataReceiveTaoPhieu`. Class auth **mixed** (key-based search + nhiều `[PrivateSBAuthorize]` + `[SBAuthorize()]` trống).

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm kiếm / load lưới | Lưới + bộ lọc | `POST api/PhieuBanHang/TimTheoDieuKien` | `PhieuBanHang_TimTheoDieuKien` | public | màn phiếu bán hàng | EF query theo IdDonVi + phân quyền CN (super/CSKH all; admin theo CN chọn; user theo CN mình). Lọc keyword/ngày/loại bán/trạng thái thanh toán/trạng thái hoá đơn/trạng thái đồng bộ partner. Trả grid kèm cờ StatusSendPartner, SinhButToanSuccess. | **EF** `PhieuBanHang` | — |
| 2 | Xem chi tiết | mở phiếu | `GET api/PhieuBanHang/TimTheoId/{id}` | `PhieuBanHang_TimTheoId` | public | click dòng | Load phiếu + dòng SP (check IdDonVi). | **EF** | — |
| 3 | Lịch sử tạo phiếu | tab Lịch sử | `POST api/PhieuBanHang/LichSuTaoPhieu` | `[SBAuthorize()]` trống | public | mở phiếu | Đọc lịch sử từ **MongoDB** `PhieuBanHangMongo`, paging, sort NgayTao desc. | **MongoDB** (read-only) | — |
| 4 | Tạo phiếu bán trả lại | nút "Tạo phiếu bán trả lại" | `POST api/PhieuBanHang/ThemMoiPhieuBanHangTraLai` | `[SBAuthorize()]` trống | public | trên phiếu đã bán | `ThemMoiPhieuBanHangTraLai` — tạo tới 3 chứng từ kèm: (a) phiếu bán để giảm trừ công nợ (nếu "giamTruCongNo"), (b) phiếu chi hoàn tiền (nếu "traLaiTienMat"), (c) phiếu nhập kho (nếu IsKiemPhieuNhapKho). Có rollback nếu phiếu nhập fail sau khi đã tạo phiếu bán/chi. | **EF** + Branch + Warehouse | TODO: phiếu chi không rollback hết (xem Lỗi đã biết). |
| 5 | Autocomplete phiếu bán | combobox tìm phiếu | `POST api/PhieuBanHang/SearchPhieuBanHangAutoComplete` | `[SBAuthorize()]` trống | public | form chọn phiếu | Tìm theo MaPhieu (top 10, sort NgayTao desc). | **EF** | TODO chưa loại phiếu đã trả hết số lượng. |
| 6 | Thống kê tình trạng hoá đơn | widget thống kê | `POST api/PhieuBanHang/ThongKeTinhTrangHoaDonAsync` | `[SBAuthorize()]` trống | public | màn phiếu bán | Đếm Tổng / Đã xuất / Chưa xuất / Xuất muộn theo cùng bộ lọc. | **EF** aggregate | — |
| 7 | Xuất Excel | nút "Xuất Excel" | `POST api/PhieuBanHang/ExportPhieuBanHang` | `[SBAuthorize()]` trống | public | có dữ liệu | Xuất chi tiết dòng SP + giá vốn (gọi `WarehouseService.GetInfoKhoKeToanByListId`). File `PhieuBanHang.xlsx`. | EF + Warehouse + GemBox | — |

## Endpoint nội bộ (private / service-to-service)
- [internal] `POST ThemMoi` (`[PrivateSBAuthorize]`) — **bản cũ ("CODE CŨ KHÔNG DÙNG NỮA")**: create/update khi `IsSuDungPhieuXuatNvKt`. Ripple: Mongo, MISA, bút toán, (mua cũ đổi mới) phiếu xuất + phiếu thu tạm. *Cần xác nhận còn dùng không — code đánh dấu deprecated nội bộ.*
- [internal] `POST PhieuBanHang_SaveTuDongChoDonHang` (`[PrivateSBAuthorize]`) — **luồng chính tự tạo PBH từ đơn hàng**: chỉ sửa khi chưa đồng bộ partner. Sinh MaPhieu, map dòng SP + giá vốn, tính thuế/chiết khấu; sinh bút toán; nếu đặt cọc>0 → link `ThuChiDonHang`; tạo `ChungTuKhac` cho chuyển nợ (NhomDoiTuongNo→ThuCod) và giảm doanh thu phí giao hàng; đồng bộ MISA + DataReceiveTaoPhieu.
- [internal] `POST RollbackPhieuBanHang` (`[PrivateSBAuthorize]`) — về trạng thái HuyThanhToan; cascade rollback `PhieuBuTruCongNo` + `ChungTuKhac`.
- [internal] `POST CapNhatTrangThaiPhieuBanHangAndPhieuThu` (`[PrivateSBAuthorize]`) — **callback từ Branch/PhieuThu**: cộng `TongSoTienDaThu`, set cờ `IsPhieuNhapHoanThanh`/`IsPhieuThuThanhCong`; khi cả hai true + đã thu đủ → PBH=DaHoanThanh.
- [internal] `POST PhieuBanHang_UpdateTrangThaiHoaDonPhieuBan` (`[PrivateSBAuthorize]`) — đánh dấu đã xuất hoá đơn (IsDaXuatHoaDon=true, IdTinhTrangHoaDon=DaXuat hoặc XuatMuon nếu NgayXuatHoaDon > NgayTieuChuanXuatHoaDon).
- [internal] `POST UpdateNgayTieuChuanXuatHoaDonAsync` (`[PrivateSBAuthorize]`) — set ngày tiêu chuẩn xuất hoá đơn (từ NgayGiaoHang).
- [internal] `POST GetPhieuBanHangByMaDons` (`[PrivateSBAuthorize]`) — tra cứu PBH theo list MaDon (cho module **Đối soát**). Trả `Ok(result)` thẳng (không qua ResponseResult).

## Quan hệ
- **Gọi sang:** ButToanService, WarehouseService (giá vốn, phiếu nhập trả lại), BranchService (phiếu chi trả lại), CommonService (phiếu xuất, phiếu thu), BuTruCongNoService, ChungTuKhacService, MisaDanhMucSync, MongoDB.
- **Dữ liệu (entities):** `PhieuBanHang`, `PhieuBanHangSanPham`, `PhieuBanHangMongo` (lịch sử), `DataReceiveTaoPhieu`.
- **Ripple / đồng bộ:** bút toán doanh thu/giá vốn, phiếu xuất kho (mua cũ đổi mới), phiếu thu, chứng từ khác (chuyển nợ / giảm doanh thu phí ship), đồng bộ MISA. Không IBus.Publish trực tiếp trong service này (đẩy MISA qua `DataReceiveTaoPhieu` + job).

## Luồng xuyên module
- **Đơn hàng hoàn tất** → `PhieuBanHang_SaveTuDongChoDonHang` tạo PBH → sinh **bút toán** + nếu mua cũ đổi mới thì **phiếu xuất kho** + **phiếu thu tạm** + (chuyển nợ) **ChungTuKhac** + đẩy **MISA**.
- **Thu tiền xong** (Branch) → callback `CapNhatTrangThaiPhieuBanHangAndPhieuThu` → khi đủ điều kiện set PBH=DaHoanThanh.
- **Xuất hoá đơn** → `PhieuBanHang_UpdateTrangThaiHoaDonPhieuBan` set DaXuat/XuatMuon.
- **Tạo phiếu bán trả lại** → `ThemMoiPhieuBanHangTraLai` sinh phiếu bán giảm trừ + phiếu chi + phiếu nhập (hàng về kho).

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Accounting/Accounting.API/Controllers/PhieuBanHangController.cs`
  - Nhiều endpoint UI `[SBAuthorize()]` **trống** (LichSuTaoPhieu L56, ThemMoiPhieuBanHangTraLai L98, SearchAutoComplete L111, ThongKe L137, Export L163) → chỉ cần đăng nhập, không key riêng.
  - `GetPhieuBanHangByMaDons` trả `Ok(result)` thẳng thay vì `ResponseResult` như các endpoint khác (lệch convention).
- BE `Accounting/Accounting.Service/Generated/PhieuBanHangService.cs`
  - `ThemMoiAsync` đánh dấu nội bộ "CODE CŨ HIỆN TẠI KHÔNG DÙNG NỮA" — **nghi deprecated**, luồng thật là `PhieuBanHang_SaveTuDongChoDonHang`. *Cần xác nhận trước khi sửa.*
  - L1599 self-assignment `phieuBanHangDb.TongTienChietKhauSanPham = phieuBanHangDb.TongTienChietKhauSanPham;` (no-op, đáng lẽ lấy từ reqData).
  - L1634-1637 và L1650-1653: xoá `lstPhieuBanHangSanPhamXoa` **2 lần** (logic trùng).
  - L2068 `//TODO: xóa phiếu chi` — trong `ThemMoiPhieuBanHangTraLai`, khi phiếu nhập fail chỉ xoá bút toán, **phiếu chi không rollback** → có thể còn rác.
  - L1835 `//TODO`: chuyển nợ chưa tự tạo phiếu chi phí ship tương ứng.
  - L2602 `//TODO`: SearchAutoComplete chưa loại phiếu bán đã trả hết số lượng.
