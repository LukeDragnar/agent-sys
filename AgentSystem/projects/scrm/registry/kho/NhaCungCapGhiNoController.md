---
controller: NhaCungCapGhiNoController
module: kho
service: WarehouseProduct
man_hinh_ui: "Một phần qua màn Thu chi > Phiếu chi tiền (phieu-chi-tien.component) — lấy khoản nợ NCC để chi trả. KHÔNG có màn quản lý ghi nợ NCC độc lập; phần lớn endpoint là service-to-service do Accounting/Branch gọi khi tạo phiếu mua hàng / bù trừ / trả nợ."
muc_dich: "Sổ cái công nợ phải trả nhà cung cấp: ghi nhận phát sinh nợ/có theo từng phiếu mua hàng, lấy công nợ phải trả, bù trừ công nợ, trả nợ/phiếu chi, khởi tạo & xóa số dư ban đầu, rollback theo transaction. Mỗi biến động = 1 bản ghi NhaCungCapGhiNo (Nợ/Có)."
alias: ["ghi nợ nhà cung cấp", "công nợ NCC", "công nợ phải trả nhà cung cấp", "trả nợ NCC", "bù trừ công nợ NCC", "số dư ban đầu NCC", "NhaCungCapGhiNo", "phiếu chi trả nợ NCC"]
auth: mixed                # ThemMoi/GetCongNoPhaiTra/GhiNhanBuTru/GetKhoanNo/TraNo: [SBAuthorize()] rỗng (chỉ đăng nhập); Rollback/PhieuChiTraNoNcc/SoDuBanDau: [PrivateSBAuthorize]
state_machine: []          # không có trạng thái; chỉ cờ IsDaTraHetNo (đã trả hết nợ / chưa)
deprecated: false
entities: [NhaCungCapGhiNo, NhaCungCap]
stored_procs: []
depends: [WarehouseProduct]
cross_module_in: [Accounting/Order (ThemMoi, RollbackNhaCungCapGhiNo, ThemMoiSoDuBanDauNCC, XoaSoDuBanDauNhaCungCap), Branch (PhieuChiTraNoNcc, TraNoNhaCungCap)]
cross_module_out: []
---

# NhaCungCapGhiNoController — Công nợ phải trả nhà cung cấp (sổ ghi nợ NCC)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Thêm mới ghi nợ NCC | (chủ yếu BE→BE từ phiếu mua hàng) | POST `api/NhaCungCapGhiNo/ThemMoi` | `SBAuthorize()` (rỗng) | public | — | Tạo/sửa 1 bản ghi ghi nợ NCC (Nợ/Có) gắn IdPhieuMuaHang; validate NCC/mã/ngày/loại phiếu/tài khoản. Chi nhánh ghi nhận = `dto.IdChiNhanh ?? userLogin.IdChiNhanh` | EF | **`SoTien <= 0` → trả "Thành công" + Guid.Empty (không ghi gì)** — NhaCungCapGhiNoService.cs:73-76, dễ nhầm là đã ghi. Quyền rỗng (chỉ đăng nhập) |
| 2 | Lấy công nợ phải trả | màn bù trừ công nợ | GET `api/NhaCungCapGhiNo/GetCongNoPhaiTraAsync?maDoiTuong=&ngayButru=&taiKhoan=` | `SBAuthorize()` (rỗng) | public | — | Gom ghi nợ theo IdPhieuMuaHang đến `ngayButru`; trả các phiếu còn nợ (`tongNo - tongCo > 0`) dạng `ChungTuButruViewModel` (loại 2 = phải trả) | EF | NCC không tồn tại theo mã → "Không tìm thấy nhà cung cấp với mã: ..." |
| 3 | Ghi nhận bù trừ công nợ | xác nhận bù trừ | POST `api/NhaCungCapGhiNo/GhiNhanBuTruCongNoNCCAsync` | `SBAuthorize()` (rỗng) | public | — | Mỗi khoản `SoTienBuTru>0` → insert 1 bản ghi `LoaiPhieu=BuTru`, `LaNoHayCo=Co` (giảm nợ), gắn IdPhieuBuTru/MaPhieuBuTru + IdTransaction | EF | - |
| 4 | Lấy danh sách khoản nợ NCC | màn Phiếu chi tiền (chọn phiếu trả) | POST `api/NhaCungCapGhiNo/GetKhoanNoNhaCungCap` | `SBAuthorize()` (rỗng) | public | popup chọn nợ | Gom theo IdPhieuMuaHang trong (đơn vị + **chi nhánh đăng nhập** + NCC); trả phiếu còn nợ (`SoTienDaMua - SoTienDaTra > 0`), phân trang | EF | Lọc cứng theo `userLogin.IdChiNhanh` → admin tổng không thấy nợ chi nhánh khác |
| 5 | Trả nợ NCC | xác nhận trả nợ | POST `api/NhaCungCapGhiNo/TraNoNhaCungCap` | `SBAuthorize()` (rỗng) | public | — | Với từng phiếu: tính nợ còn lại (tongNo-tongTra), chặn vượt/âm/đã hết; insert bản ghi `LoaiPhieu=TraTien`, `LaNoHayCo=Co`, TK `331`; cập nhật cờ `IsDaTraHetNo` | EF | TK kế toán hardcode `"331"`. Quyền rỗng |

## Endpoint nội bộ (private / service-to-service — `[PrivateSBAuthorize]`)
- [internal] **Accounting/Order** gọi `POST RollbackNhaCungCapGhiNo` (Accounting.Service/RollbackAccounting.cs) — **xóa toàn bộ** ghi nợ theo `IdTransaction` khi rollback đơn/chứng từ. *Lưu ý: xóa cứng, không revert bằng bút toán đảo.*
- [internal] **Branch** gọi `POST PhieuChiTraNoNcc` (Branch.Service/Commons/CommonService.cs) — tạo 1 phiếu chi trả nợ (`LoaiPhieu=TraTien`, TK `331`) + cập nhật `IsDaTraHetNo`.
- [internal] **Accounting/SoDuBanDau** gọi `POST ThemMoiSoDuBanDauNCC` (Accounting.Service/SoDuBanDauService.cs) — ghi số dư đầu kỳ NCC (`LaNoHayCo=KhoiTaoBanDau`); chặn nếu đã có bản khởi tạo. Dấu tiền theo `KieuLoai` ("Co" dương / khác âm).
- [internal] **Accounting/SoDuBanDau** gọi `POST XoaSoDuBanDauNhaCungCap` — xóa bản ghi số dư ban đầu của NCC.

## Quy tắc nghiệp vụ (validate ghi nợ / trả nợ — blueprint để trace)
1. **ThemMoi**: thiếu NCC/mã/ngày/loại phiếu/TK kế toán → lỗi tương ứng; `LoaiPhieu` phải ∈ {MuaHang, TraTien, BuTru, Khac}; `LaNoHayCo` phải ∈ {No, Co}. Có `dto.Id` → update bản ghi, ngược lại insert. (NhaCungCapGhiNoService.cs:51-160)
2. **TraNoNhaCungCap**: `TongSoTienTra<=0` → lỗi; mỗi phiếu: `SoTienTra<0` → lỗi, đã hết nợ → lỗi, vượt nợ còn lại → lỗi; tổng chi tiết > `TongSoTienTra` → lỗi. Sau insert cập nhật `IsDaTraHetNo = (tongNo-tongTra)<=0`. (NhaCungCapGhiNoService.cs:362-459)
3. **Nợ/Có (LaNoHayCo)**: 1 = Nợ (tăng phải trả khi mua hàng), 2 = Có (giảm nợ khi trả/bù trừ), KhoiTaoBanDau = số dư đầu kỳ. Công nợ còn lại = Σ Nợ − Σ Có theo IdPhieuMuaHang.

## Quan hệ
- **Gọi sang:** chỉ EF nội bộ (NhaCungCapGhiNo, NhaCungCap). Không gọi service ngoài (controller này là ĐÍCH ĐẾN, không phải nguồn).
- **Dữ liệu (entities):** `NhaCungCapGhiNo` (IdNhaCungCap, MaNhaCungCap, IdPhieuMuaHang/MaPhieuMuaHang, LoaiPhieu, LaNoHayCo, SoTien, TkKeToan, IdTransaction, IdPhieuBuTru, IsDaTraHetNo) — mỗi dòng = 1 bút toán Nợ/Có; `NhaCungCap` (đối chiếu tồn tại theo mã/đơn vị).
- **Ripple / đồng bộ:** là sổ phụ công nợ phải trả. **Accounting** ghi nợ khi tạo phiếu mua hàng, rollback khi hủy, khởi tạo/xóa số dư đầu kỳ; **Branch** chi trả nợ qua phiếu chi/thu chi. Bù trừ công nợ liên thông phiếu bù trừ (`IdPhieuBuTru`). Sửa logic Nợ/Có ở đây làm lệch báo cáo công nợ phải trả ở Accounting.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `WarehouseProduct/WarehouseProduct.API/Controllers/NhaCungCapGhiNoController.cs`.
- NhaCungCapGhiNoService.cs:73-76 — `SoTien <= 0` ở `ThemMoiAsync` trả **"Thành công" + Guid.Empty** mà KHÔNG ghi bản ghi → caller (Accounting) tưởng đã ghi nợ. Rủi ro lệch công nợ.
- TK kế toán **hardcode `"331"`** ở TraNoNhaCungCap (L421) và PhieuChiTraNoNcc (L488) — không lấy theo cấu hình.
- `RollbackNhaCungCapGhiNo` **xóa cứng** theo IdTransaction (L291) thay vì bút toán đảo → mất lịch sử; phải chắc IdTransaction đúng phạm vi.
- `GetKhoanNoNhaCungCap` lọc cứng `IdChiNhanh == userLogin.IdChiNhanh` (L310) — không có nhánh admin-tổng xem toàn đơn vị như nhiều màn khác.
- ThemMoiSoDuBanDauNCC: message "Khởi tạo số dư ban đầu cho **khách hàng** {Ten}" (L562) — copy nhầm từ NCC (cosmetic).
- `IdChiNhanh` ghi nhận: ThemMoi dùng `dto.IdChiNhanh ?? userLogin.IdChiNhanh`, nhưng GhiNhanBuTru/TraNo/PhieuChi/SoDuBanDau dùng thẳng `userLogin.IdChiNhanh` → không nhất quán nguồn chi nhánh.
- Lưu ý trùng/liên quan: `NhaCungCapController.cs` (danh mục NCC) là controller KHÁC — node này chỉ mô tả công nợ ghi nợ NCC.
</content>
