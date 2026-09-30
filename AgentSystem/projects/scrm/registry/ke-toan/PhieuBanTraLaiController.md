---
controller: PhieuBanTraLaiController
module: ke-toan
service: Accounting
man_hinh_ui: "Kế toán > Chứng từ bán hàng > màn 'Hàng bán trả lại' (HBBTL). Lưới list + bộ lọc (keyword, trạng thái, khoảng ngày, chi nhánh). Tạo phiếu bán trả lại từ phiếu bán hàng gốc hoặc từ đơn hoàn ecommerce. Nút: Tạo HBBTL (form chọn phiếu bán + dòng SP trả), Xuất Excel (template MISA)."
muc_dich: "Quản lý chứng từ hàng bán bị trả lại (HBBTL): nghịch đảo doanh thu/giá vốn của phiếu bán hàng gốc. Kéo theo phiếu nhập kho (hàng trả về kho), phiếu chi (hoàn tiền mặt/CK) hoặc giảm trừ công nợ, bút toán nghịch + chứng từ liên kết. Hỗ trợ luồng đơn hoàn ecommerce."
alias: ["phiếu bán trả lại", "hàng bán trả lại", "hang ban tra lai", "HBBTL", "hàng bán bị trả lại", "bán trả lại", "đơn hoàn", "trả hàng bán"]
auth: public
state_machine: [ChuaXuLy, DangXuLy, DaHoanThanh]
deprecated: false
entities: [PhieuBanTraLai, PhieuBanTraLaiSanPham]
stored_procs: []
depends: [BranchService (TaoPhieuChiFromBanHangTraLai / phiếu chi hoàn tiền), WarehouseService (CreateFromPhieuTraLaiHangBan / phiếu nhập hàng về kho, PhieuXuat_GetByIdDonHang), ButToanService (ButToan_PhieuBanHangBiTraLai_Save), ChungTuLienKetService (đăng ký + tra cứu chứng từ liên kết), MisaDanhMucSync, PhieuBanHangService (lookup phiếu bán gốc)]
cross_module_in: [OrderLogistic/đơn hoàn ecommerce (HandleCreatePhieuTraLaiFromOrderLogistic - consumer, GetPhieuLienQuanByDonHoanSanPham, HBBTL_TimPhieuBanHangByIdDonHang)]
cross_module_out: [WarehouseProduct/PhieuNhap (hàng trả về kho), Branch/PhieuChi (hoàn tiền), ButToan/Accounting (bút toán nghịch doanh thu/giá vốn), CongNo (giảm trừ - CTKT bù trừ), ChungTuLienKet (link HBBTL↔PBH↔CTKT), MISA (đồng bộ danh mục), RabbitMQ (publish kết quả tạo HBBTL từ đối soát đơn hoàn)]
---

# PhieuBanTraLaiController — Hàng bán trả lại (HBBTL, nghịch đảo phiếu bán → nhập kho + hoàn tiền)

> Chứng từ trả lại của bán hàng. Prefix mã: **HBBTL** (hoặc PC nếu hoàn tiền). State (`IdTinhTrangPhieu`): ChuaXuLy=1, DangXuLy=2, DaHoanThanh=3. Core là `HBBTL_Save` (~L404-811): có **chống tạo trùng** theo `IdDonHangHoanSanPham` (luồng đơn hoàn ecommerce), trong transaction sinh **phiếu chi** (hoàn tiền mặt/gửi) + **phiếu nhập kho** (nếu IsKiemNhapKho — hàng về kho), rollback HBBTL nếu 2 cái này fail; còn **bút toán nghịch / CTKT bù trừ công nợ / chứng từ liên kết** thì **nuốt lỗi (log, không chặn)**. Toàn bộ 8 endpoint controller dùng `[SBAuthorize()]` **trống** (auth=public theo nghĩa chỉ cần đăng nhập). 100% EF, không stored proc.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm kiếm / load lưới | Lưới + bộ lọc | `POST api/PhieuBanTraLai/TimTheoDieuKien` | `[SBAuthorize()]` trống | public | màn HBBTL | EF query theo DonVi + CN + keyword (MaPhieu) + khoảng ngày; phân quyền CN (super/admin/user). Paging + count. | **EF** `PhieuBanTraLai` | — |
| 2 | Tạo phiếu bán trả lại | nút "Tạo HBBTL" / Lưu | `POST api/PhieuBanTraLai/HBBTL_Save` | `[SBAuthorize()]` trống | public | có phiếu bán gốc / đơn hoàn | `HBBTL_Save`: validate + check phiếu bán gốc; chống tạo trùng theo `IdDonHangHoanSanPham`; sinh mã (HBBTL/PC); insert phiếu + dòng SP. Transaction: phiếu chi (nếu TraLaiTienMat/TienGui) + phiếu nhập kho (nếu IsKiemNhapKho) — fail thì rollback. Sau đó bút toán nghịch + (giảm trừ công nợ) CTKT bù trừ + chứng từ liên kết — **lỗi các bước này chỉ log, không chặn**. Đồng bộ MISA danh mục. | **EF** transaction + Branch + Warehouse | bút toán/CTKT/link nuốt lỗi → graph chứng từ có thể thiếu (xem Lỗi đã biết). |
| 3 | Xem phiếu liên quan từ đơn hoàn | (luồng ecommerce) | `POST api/PhieuBanTraLai/GetPhieuLienQuanByDonHoanSanPham` | `[SBAuthorize()]` trống | public | có IdDonHangHoanSanPham | Reverse-query `PhieuBanTraLaiSanPham` theo list IdDonHangHoanSanPham → tìm HBBTL; với mỗi HBBTL lấy thêm các phiếu liên kết (PBH gốc, CTKT…) qua `ChungTuLienKet_LayDanhSachPhieuLienQuan`; dedup theo IdPhieu. | **EF** + ChungTuLienKet | — |
| 4 | Xem chi tiết HBBTL | mở phiếu | `POST api/PhieuBanTraLai/HBBTL_TimTheoId` | `[SBAuthorize()]` trống | public | click dòng | Load phiếu + dòng SP (left-join), map full model. | **EF** | — |
| 5 | Tìm phiếu xuất theo phiếu bán | (chuẩn bị trả) | `GET api/PhieuBanTraLai/TimPhieuXuatTheoMaPhieuBan/{idPhieuBanHang}` | `[SBAuthorize()]` trống | public | chọn phiếu bán | Lấy PBH → xác định loại đơn (offline/online) → gọi `WarehouseService.PhieuXuat_GetByIdDonHang` lấy phiếu xuất (giá vốn để nghịch). | **EF** + Warehouse | — |
| 6 | Tìm phiếu bán theo id (kèm SL còn trả) | chọn phiếu bán gốc | `GET api/PhieuBanTraLai/HBBTL_TimPhieuBanHangById/{idPhieuBanHang}` | `[SBAuthorize()]` trống | public | form tạo HBBTL | Lấy PBH + dòng SP; trừ số lượng đã trả (group PhieuBanTraLaiSanPham) → show SL còn trả được. | **EF** | — |
| 7 | Tìm phiếu bán theo IdDonHang (ecommerce) | prefill từ đơn hoàn | `GET api/PhieuBanTraLai/HBBTL_TimPhieuBanHangByIdDonHang/{idDonHang}` | `[SBAuthorize()]` trống | public | tạo HBBTL từ đơn hoàn ecommerce | Lookup PBH theo IdDonHang (DonHangLogistic.Id) rồi gọi `HBBTL_TimPhieuBanHangById` để prefill. | **EF** | — |
| 8 | Xuất Excel | nút "Xuất Excel" | `POST api/PhieuBanTraLai/ExportHangBanTraLai` | `[SBAuthorize()]` trống | public | có dữ liệu | Lọc HBBTL + dòng SP, map template MISA `hang_ban_tra_lai.xlsx`. NotFound nếu rỗng. | EF + GemBox | — |

## Endpoint nội bộ (consumer / message)
- [internal] `HandleCreatePhieuTraLaiFromOrderLogistic` (consumer, L1838+) — nhận message đối soát đơn hoàn từ RabbitMQ, tạo HBBTL rồi **publish** `TaoPhieuTraLaiDonHangDoiSoatMessage` (L1875/1922/1931/1938) báo kết quả về OrderLogistic. (Đây là điểm duy nhất có pub/sub trong service này.)

## Quan hệ
- **Gọi sang:** BranchService (phiếu chi hoàn tiền), WarehouseService (phiếu nhập hàng về kho, phiếu xuất giá vốn), ButToanService (bút toán nghịch), ChungTuLienKetService, MisaDanhMucSync, PhieuBanHangService.
- **Dữ liệu (entities):** `PhieuBanTraLai`, `PhieuBanTraLaiSanPham`.
- **Ripple / đồng bộ:** phiếu nhập kho (hàng trả về), phiếu chi (hoàn tiền), bút toán nghịch doanh thu/giá vốn, CTKT bù trừ công nợ (giảm trừ), chứng từ liên kết HBBTL↔PBH↔CTKT, đồng bộ MISA. Pub/sub RabbitMQ chỉ ở luồng consumer đối soát đơn hoàn.

## Luồng xuyên module
- **Tạo HBBTL** `HBBTL_Save` → (tuỳ HinhThucHoanTien): **phiếu chi** hoàn tiền (Branch) HOẶC **CTKT bù trừ công nợ** (giảm trừ, nghịch chiều Nợ KH→Có Sàn); nếu IsKiemNhapKho → **phiếu nhập kho** (hàng về kho); luôn sinh **bút toán nghịch** + **chứng từ liên kết** + đẩy **MISA**.
- **Đơn hoàn ecommerce** → consumer `HandleCreatePhieuTraLaiFromOrderLogistic` tự tạo HBBTL + publish kết quả về OrderLogistic.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Accounting/Accounting.API/Controllers/PhieuBanTraLaiController.cs`
  - Cả 8 endpoint (L38, L51, L63, L76, L89, L102, L114, L127) dùng `[SBAuthorize()]` **trống** → không phân quyền key riêng cho thao tác tạo phiếu trả lại (chỉ cần đăng nhập).
- BE `Accounting/Accounting.Service/Generated/PhieuBanTraLaiService.cs`
  - L792-795, L802-805 (`HBBTL_Save`): `TaoChungTuBuTruSanTuHoan` + đăng ký chứng từ liên kết **nuốt lỗi (log, không block)** → đã có phiếu + bút toán nhưng graph chứng từ có thể thiếu CTKT/link (trạng thái không nhất quán).
  - L1556: `catch { }` rỗng nuốt lỗi deserialize JSON (mất stack trace).
  - L341: double null-check `data.ListPhieuBanHangSanPham is not null && ... is not null` (thừa).
  - L185-199: method `HBBTL_Save` overload cũ bị comment-out (dead code).
  - Nhiều TODO chưa hoàn thiện: L542, L572, L579 (chiết khấu/quy đổi), L1019 (chưa add chứng từ liên quan), L1421 (chưa lấy phiếu xuất). Magic string TK "111"/"331" fallback (L2092).
