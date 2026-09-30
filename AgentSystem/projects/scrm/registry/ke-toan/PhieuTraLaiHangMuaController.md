---
controller: PhieuTraLaiHangMuaController
module: ke-toan
service: Accounting
man_hinh_ui: "Kế toán > Chứng từ mua hàng > màn 'Trả lại hàng mua' (TLHM). Lưới list + bộ lọc (keyword, trạng thái, khoảng ngày, chi nhánh). Tạo phiếu trả lại từ phiếu mua hàng gốc (chọn phiếu mua autocomplete → load SL còn trả). Nút: Tạo, Sửa, Xuất Excel (template MISA)."
muc_dich: "Quản lý chứng từ trả lại hàng mua cho nhà cung cấp: nghịch đảo phiếu mua hàng gốc. Kéo theo phiếu xuất kho (hàng trả khỏi kho), phiếu thu (NCC hoàn tiền) hoặc giảm trừ công nợ NCC, bút toán nghịch khi hoàn thành."
alias: ["trả lại hàng mua", "tra lai hang mua", "TLHM", "phiếu trả hàng mua", "trả hàng NCC", "trả nhà cung cấp", "hàng mua trả lại"]
auth: public
state_machine: [ChuaXuLy, DangXuLy, DaHoanThanh, HuyBo]
deprecated: false
entities: [PhieuTraLaiHangMua, PhieuTraLaiHangMuaSanPham]
stored_procs: []
depends: [CommonService (PhieuXuat_TaoTuTraLaiHangBan / phiếu xuất, TaoPhieuThuTraLai / phiếu thu), ButToanService (ButToan_PhieuTraLaiHangMua_Save), MisaDanhMucSync (DoiTuong/Kho/SanPham), PhieuMuaHangService (load phiếu mua gốc + SL còn trả)]
cross_module_in: []
cross_module_out: [WarehouseProduct/PhieuXuat (hàng trả khỏi kho), Branch/PhieuThu (NCC hoàn tiền), ButToan/Accounting (bút toán nghịch mua), CongNo NCC (giảm trừ - giảm công nợ phải trả), MISA (đồng bộ danh mục)]
---

# PhieuTraLaiHangMuaController — Trả lại hàng mua (TLHM, nghịch đảo phiếu mua → xuất kho + thu/giảm nợ NCC)

> Chứng từ trả hàng cho NCC. State (`TinhTrangPhieuMuaHangTraLaiEnum`): ChuaXuLy=1, DangXuLy=2, DaHoanThanh=3, HuyBo=4. Ripple **chỉ kích hoạt khi trạng thái = DaHoanThanh**: (a) `IsTraLaiHangTrongKho=true` → **phiếu xuất kho** (hàng rời kho); (b) HinhThucHoanTien=ThuLaiTienMat → **phiếu thu** (NCC hoàn tiền mặt); (c) HinhThucHoanTien=GiamTruCongNo → **bút toán nghịch** + giảm công nợ phải trả NCC + đồng bộ MISA. Chỉ cho sửa khi phiếu chưa DaHoanThanh (hoàn thành = đóng băng). Toàn bộ 8 endpoint `[SBAuthorize()]` **trống**. 100% EF, không stored proc, không message bus.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm kiếm / load lưới | Lưới + bộ lọc | `POST api/PhieuTraLaiHangMua/TraLaiHangMua_TimTheoDieuKien` | `[SBAuthorize()]` trống | public | màn TLHM | EF query theo DonVi + keyword (MaPhieu, TenNCC) + trạng thái + khoảng ngày + CN; phân quyền CN (super/admin/user). Sort NgayTao desc, paging. | **EF** `PhieuTraLaiHangMua` | — |
| 2 | Tạo phiếu trả lại | nút "Tạo" / Lưu | `POST api/PhieuTraLaiHangMua/TraLaiHangMua_Create` | `[SBAuthorize()]` trống | public | có phiếu mua gốc | `TraLaiHangMua_Create`: sinh mã (PX/PT), insert phiếu + dòng SP trong transaction; nếu trạng thái=DaHoanThanh → ripple (phiếu xuất nếu IsTraLaiHangTrongKho, phiếu thu nếu ThuLaiTienMat, bút toán+MISA nếu GiamTruCongNo). | **EF** transaction + Common | **L403 bug** (xem Lỗi đã biết). |
| 3 | Cập nhật phiếu | Sửa / Lưu | `POST api/PhieuTraLaiHangMua/TraLaiHangMua_Update` | `[SBAuthorize()]` trống | public | phiếu chưa DaHoanThanh | `TraLaiHangMua_Update`: chỉ cập nhật metadata (địa chỉ NCC, liên hệ, thông tin hoá đơn, hình thức hoàn tiền) — KHÔNG đổi dòng SP/tổng tiền, KHÔNG đổi trạng thái → không ripple. | **EF** | — |
| 4 | Xem chi tiết | mở phiếu | `POST api/PhieuTraLaiHangMua/TraLaiHangMua_TimTheoId` | `[SBAuthorize()]` trống | public | click dòng | Load phiếu + dòng SP; override SL theo `SoLuongBanTheoDonViQuyDoi` nếu có. | **EF** | — |
| 5 | Load phiếu mua gốc (SL còn trả) | chọn phiếu mua | `POST api/PhieuTraLaiHangMua/GetPhieuMuaById` | `[SBAuthorize()]` trống | public | form tạo TLHM | Load phiếu mua + dòng SP; trừ SL đã trả (group PhieuTraLaiHangMuaSanPham) → chỉ show SP còn SL>0. | **EF** | — |
| 6 | Autocomplete phiếu mua | combobox tìm phiếu mua | `POST api/PhieuTraLaiHangMua/SearchPhieuMuaAutocomplete` | `[SBAuthorize()]` trống | public | form tạo | Chỉ trả phiếu mua trạng thái DaHoanThanh (=3) và còn ít nhất 1 dòng SL chưa trả hết. | **EF** | — |
| 7 | Prefill từ phiếu mua | (tạo nhanh) | `POST api/PhieuTraLaiHangMua/TLHM_TimPhieuMuaHangById` | `[SBAuthorize()]` trống | public | form tạo | Map phiếu mua → DTO TLHM, auto-điền kho/hoá đơn/SL còn trả. | **EF** | MaPhieuNhap luôn rỗng dù có IdPhieuNhap (L1303-1308). |
| 8 | Xuất Excel | nút "Xuất Excel" | `POST api/PhieuTraLaiHangMua/ExportTraLaiHangMua` | `[SBAuthorize()]` trống | public | có dữ liệu | Lọc + join phiếu xuất/phiếu thu con, map template MISA. NotFound nếu rỗng. | EF + GemBox | — |

## Quy tắc nghiệp vụ (gating / tier / ngưỡng)
- **Chỉ trạng thái DaHoanThanh mới sinh ripple.** ChuaXuLy/DangXuLy = lưu nháp, không tác động kho/thu/bút toán.
- **Phân nhánh ripple theo HinhThucHoanTien:** `ThuLaiTienMat` → tạo phiếu thu (NCC hoàn tiền mặt); `GiamTruCongNo` → bút toán nghịch + giảm công nợ phải trả NCC + đồng bộ MISA.
- **`IsTraLaiHangTrongKho=true` + DaHoanThanh** → tạo phiếu xuất kho (hàng rời kho).
- **DaHoanThanh = đóng băng:** không cho update sau khi hoàn thành.

## Quan hệ
- **Gọi sang:** CommonService (phiếu xuất `PhieuXuat_TaoTuTraLaiHangBan`, phiếu thu `TaoPhieuThuTraLai`), ButToanService (`ButToan_PhieuTraLaiHangMua_Save`), MisaDanhMucSync, PhieuMuaHangService.
- **Dữ liệu (entities):** `PhieuTraLaiHangMua`, `PhieuTraLaiHangMuaSanPham`. Phiếu mua gốc chỉ đọc tham chiếu (không sửa).
- **Ripple / đồng bộ:** phiếu xuất kho, phiếu thu, bút toán nghịch, giảm công nợ NCC, đồng bộ MISA. Tất cả **synchronous service-to-service**, không IBus/RabbitMQ.

## Luồng xuyên module
- **Hoàn thành phiếu trả** (state=DaHoanThanh) → (IsTraLaiHangTrongKho) **phiếu xuất kho** + (ThuLaiTienMat) **phiếu thu** + (GiamTruCongNo) **bút toán nghịch** giảm công nợ phải trả NCC + đồng bộ **MISA** danh mục.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Accounting/Accounting.API/Controllers/PhieuTraLaiHangMuaController.cs`
  - Cả 8 endpoint (L41, L54, L67, L80, L93, L106, L119, L132) dùng `[SBAuthorize()]` **trống**; comment L40 cho thấy key dự kiến (`PhieuMuaHang_TimTheoDieuKien`,...) đã bị bỏ → không phân quyền key.
- BE `Accounting/Accounting.Service/Generated/PhieuTraLaiHangMuaService.cs`
  - **L403 BUG (nặng):** `if (isSuccess = true && reqData.IdTinhTrangPhieu == DaHoanThanh && ... == GiamTruCongNo)` — dùng `=` (gán) thay vì `==` (so sánh). Do precedence `&&` cao hơn `=`, biểu thức = `isSuccess = (true && cond2 && cond3)` → khối bút toán/MISA chạy chỉ dựa cond2&&cond3, **ghi đè cờ `isSuccess` của transaction** (mất kết quả rollback thật) → có thể sinh bút toán/đồng bộ MISA dù transaction lưu phiếu đã fail. Cần sửa thành `if (isSuccess == true && ...)`.
  - L906-907 `//TODO: Cần rollback phiếu xuất nếu tạo phiếu thu lỗi.` — phiếu xuất đã tạo KHÔNG rollback khi phiếu thu fail → phiếu xuất mồ côi.
  - L393-398 + outer catch L456-459: catch lồng, `throw;` rồi outer nuốt → trả message chung, mất stack trace.
  - L1303-1308: `MaPhieuNhap = string.Empty` luôn rỗng dù `IdPhieuNhap` được map (feature dở dang, không critical).
  - L1142-1223: ~80 dòng mapping cũ bị comment-out (dead code).
