---
controller: ChungTuMuaDichVuController
module: ke-toan
service: Accounting
man_hinh_ui: "Kế toán > Chứng từ mua dịch vụ (màn mới). Lưới list + bộ lọc (trạng thái, NCC, chi nhánh, keyword). Form Lưu (NCC, nhân viên mua, các dòng dịch vụ TongTienHang/CK/thuế GTGT, tick 'Là chi phí mua hàng', thanh toán ngay/công nợ). Xem chi tiết, Xóa, Hủy. Kèm popup 'Phân bổ chi phí mua hàng' trong FORM Phiếu mua hàng (ChiPhiMuaHangController) chọn chứng từ chi phí khả dụng."
muc_dich: "Chứng từ mua DỊCH VỤ (vận chuyển, bốc xếp, kiểm định...) mirror PhieuMuaHang nhưng cho dịch vụ: sinh phiếu chi/công nợ NCC + bút toán + đồng bộ MISA. Nếu tick 'Là chi phí mua hàng' (IsChiPhiMuaHang) → sau khi hạch toán được PHÂN BỔ (qua ChiPhiMuaHangController) vào 1+ PhieuMuaHang, cộng vào giá trị nhập kho & giá vốn."
alias: ["chứng từ mua dịch vụ", "chung tu mua dich vu", "MDV", "mua dịch vụ", "chi phí mua hàng", "phân bổ chi phí mua hàng", "phân bổ chi phí", "chi phí vận chuyển nhập hàng", "landed cost"]
auth: public
state_machine: [Nhap, DaHachToan, DaHuy]
deprecated: false
entities: [ChungTuMuaDichVu, ChungTuMuaDichVuChiTiet, PhieuMuaHangChiPhiMuaHang, PhieuMuaHang, PhieuMuaHangSanPham]
stored_procs: []
depends: [CommonService (SinhMaPhieu MDV, TaoPhieuThuChi phiếu chi, TaoPhieuGhiNoChonhaCungCap công nợ NCC), ButToanService (ButToan_ChungTuMuaDichVu_Save, ButToan_Xoa_IdPhieu), ChiPhiMuaHangService (phân bổ xuống PhieuMuaHang/SanPham), DongBoPhieuCrmService + MisaRequestConverter (đồng bộ MISA), CauHinhKetNoiKeToan]
cross_module_in: [ChiPhiMuaHangController (đọc chứng từ khả dụng để phân bổ), PhieuMuaHangService (luồng Lưu phiếu gọi PhanBoChiPhi với boQuaRuleE)]
cross_module_out: [Branch/ThuChi-PhieuChi (sinh phiếu chi khi thanh toán ngay), CongNo NCC (ghi nợ 331), ButToan/Accounting, MISA (đồng bộ chứng từ mua dịch vụ), WarehouseProduct/PhieuNhap (gián tiếp qua ChiPhiMuaHang → giá trị nhập kho + giá vốn)]
---

# ChungTuMuaDichVuController — Chứng từ mua dịch vụ + Phân bổ chi phí mua hàng (landed cost)

> Tính năng MỚI (nhánh `phan-bo-chi-phi-mua-hang`, merge release_product 2026-06-26). Chứng từ ghi nhận **mua dịch vụ** (không nhập kho) — cấu trúc mirror `PhieuMuaHang` (NCC, nhân viên mua, thanh toán ngay/công nợ, đồng bộ MISA). Khi tick **`IsChiPhiMuaHang=true`** thì chứng từ trở thành nguồn **chi phí mua hàng** có thể **phân bổ** vào một/nhiều `PhieuMuaHang` → cộng vào giá trị nhập kho & giá vốn sản phẩm (bài toán *landed cost*: phí vận chuyển/bốc xếp/kiểm định nhập hàng được cộng vào giá vốn). `[TienIchAttribute(MaTienIchSCRM.TI_KeToan)]` (thuộc gói Kế toán, thêm ở `ff38d122d3`).
>
> State (`EnumAccounting.TrangThaiChungTuMuaDichVu`): **Nhap=0, DaHachToan=1, DaHuy=2**. Tình trạng phân bổ KHÔNG lưu ở trạng thái — suy ra từ `DaPhanBo` vs `TongTien` (`ConLaiPhanBo = TongTien - DaPhanBo`).

## Hành động (UI / công khai) — ChungTuMuaDichVuController

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện | Hành vi đúng (expected) | Nguồn | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Danh sách chứng từ MDV | Lưới + bộ lọc | `POST api/ChungTuMuaDichVu/TimTheoDieuKien` | `SBAuthorize()` trống | public | màn danh sách | EF query theo IdDonVi + lọc trạng thái/NCC/CN/keyword; paging. | **EF** `ChungTuMuaDichVu` | Quyền trống (chỉ login). |
| 2 | Xem chi tiết | mở phiếu | `POST api/ChungTuMuaDichVu/ViewChiTiet?id=` | `SBAuthorize()` trống | public | click dòng | Load header + chi tiết theo id+DonVi. | **EF** | — |
| 3 | Lưu (thêm/sửa) | Form + nút Lưu | `POST api/ChungTuMuaDichVu/Luu` | `SBAuthorize()` trống | public | ≥1 dòng dịch vụ hợp lệ | `Validate`. `TongTien = Σ(TongTienHang − CK + thuế GTGT)`. **Thêm mới** → tạo header state=Nhap, MaPhieu trống thì `SinhMaPhieu(ChungTuMuaDichVu)` (prefix **MDV**). **Sửa** bị CHẶN nếu: `DaPhanBo>0` ("đã phân bổ, không thể sửa"), state=DaHuy, state=DaHachToan. Nếu request đẩy sang trạng thái **DaHachToan** → trong luồng lưu: sinh **phiếu chi** (nếu `IsThanhToanLuon`, BE tự tách tiền mặt/CK theo `IdPhuongThucThanhToan`), **công nợ NCC**, **bút toán**, **đồng bộ MISA** (guarantee 1 chiều: downstream fail → chứng từ MDV không lưu). | **EF** transaction | Quyền trống. |
| 4 | Xóa phiếu | nút Xóa | `POST api/ChungTuMuaDichVu/XoaPhieu?id=` | `SBAuthorize()` trống | public | state ≠ DaHachToan | **Chặn xóa khi `TrangThai=DaHachToan`** (đã hạch toán). Hợp lệ → xóa + `ButToan_Xoa_IdPhieu(id, MDV)`. | **EF** + ButToan | Quyền trống. |
| 5 | Hủy phiếu | nút Hủy | `POST api/ChungTuMuaDichVu/Huy?id=` | `SBAuthorize()` trống | public | phiếu tồn tại | Set `TrangThai=DaHuy` + `ButToan_Xoa_IdPhieu(id, MDV)` (thu hồi bút toán). | **EF** + ButToan | Quyền trống. |

## Hành động — ChiPhiMuaHangController (phân bổ chi phí → PhieuMuaHang)

> Controller riêng, **KHÔNG có `[TienIchAttribute]`** (không gate gói). Đầu vào của popup "Phân bổ chi phí mua hàng" trong form Phiếu mua hàng. File: `Accounting/Accounting.API/Controllers/ChiPhiMuaHangController.cs`.

| # | Hành động | Endpoint | Quyền | Auth | Hành vi đúng (expected) | Known-issue |
|---|---|---|---|---|---|---|
| A | Lấy chứng từ chi phí khả dụng | `POST api/ChiPhiMuaHang/LayChungTuChiPhiKhaDung` | `SBAuthorize()` trống | public | List chứng từ MDV cùng DonVi có `IsChiPhiMuaHang=true` **AND** `TrangThai=DaHachToan` **AND** `ConLaiPhanBo>0`; lọc keyword/CN/NCC; paging. | — |
| B | Xem phân bổ của 1 phiếu mua | `POST api/ChiPhiMuaHang/LayPhanBoTheoPhieuMuaHang?idPhieuMuaHang=` | `SBAuthorize()` trống | public | List mapping `PhieuMuaHangChiPhiMuaHang` của phiếu + info chứng từ nguồn. | — |
| C | Phân bổ chi phí | `POST api/ChiPhiMuaHang/PhanBoChiPhi` | `SBAuthorize()` trống | public | **Rule E:** chặn khi PMH `DaHoanThanh` (đã khóa kho/giá vốn) — trừ khi gọi từ luồng Lưu phiếu (`boQuaRuleE=true`). Validate: chứng từ phải `IsChiPhiMuaHang=true` + `DaHachToan`; **Σ phân bổ (phiếu này + phiếu khác) ≤ TongTien chứng từ** (dung sai 0.0001). Trong transaction: xóa mapping cũ của phiếu → insert mapping mới → cập nhật `DaPhanBo/ConLaiPhanBo` từng chứng từ (re-validate chống race) → set `PhieuMuaHang.TongChiPhiMuaHang` + `PhuongThucPhanBoChiPhi` → `RedistributeToSanPham` (trải xuống dòng SP). | Quyền trống. |
| D | Xóa phân bổ | `POST api/ChiPhiMuaHang/XoaPhanBo?idMapping=` | `SBAuthorize()` trống | public | **Rule E:** chặn khi PMH `DaHoanThanh`. Xóa 1 mapping → hoàn `DaPhanBo` chứng từ → tính lại `TongChiPhiMuaHang` + trải lại dòng SP (`RedistributeToSanPham`). | — |

## Quy tắc nghiệp vụ (gating / ngưỡng)
- **Chỉ chứng từ "Là chi phí mua hàng" ĐÃ HẠCH TOÁN + còn dư mới phân bổ được** (`IsChiPhiMuaHang=true` ∧ `TrangThai=DaHachToan` ∧ `ConLaiPhanBo>0`). Chứng từ Nháp không hiện trong danh sách khả dụng.
- **Không phân bổ vượt tổng chứng từ:** Σ(phân bổ mọi phiếu) ≤ `TongTien`. Validate 3 lớp: pre-validate `KiemTraVuotMucPhanBo` (luồng Lưu PMH), validate trong `PhanBoChiPhi`, và re-validate trong transaction (chống thao tác song song G6).
- **Rule E — khóa sau hoàn thành:** PMH đã `DaHoanThanh` → KHÔNG cho phân bổ/xóa phân bổ (Phase 1 không rebuild FIFO/giá vốn sau khi khóa kho). Ngoại lệ: phân bổ TRONG luồng Lưu phiếu (`boQuaRuleE=true`), vì đó là 1 phần của save chứ không phải sửa hậu kỳ.
- **Phương thức phân bổ xuống dòng SP** (`PhieuMuaHang.PhuongThucPhanBoChiPhi`): **1=Theo đơn giá** (trọng số `GiaNhap×SoLuong`, mặc định), **2=Theo số lượng** (`SoLuong`), **3=Tự nhập** (user nhập chi phí từng dòng, validate Σ khớp tổng, dung sai 1đ). Sai số làm tròn dồn vào **dòng cuối**. TuNhap khi recompute (không có danh sách) → scale giữ tỷ lệ tay hiện có; tỷ lệ cũ=0 thì fallback theo đơn giá.
- **Sửa/xóa chứng từ đã phân bổ:** `DaPhanBo>0` → không cho sửa header chứng từ (phải xóa phân bổ trước).

## Quan hệ
- **Gọi sang:** CommonService (SinhMaPhieu MDV, phiếu chi, công nợ NCC), ButToanService, MISA converter (`ChungTuMuaDichVuModel`), ChiPhiMuaHangService.
- **Dữ liệu (entities):** `ChungTuMuaDichVu` (header) + `ChungTuMuaDichVuChiTiet` (dòng dịch vụ) + `PhieuMuaHangChiPhiMuaHang` (mapping N-N phân bổ). Ghi ngược `PhieuMuaHang.TongChiPhiMuaHang` (cột mới, `dbe46d6cf2`) + `PhieuMuaHangSanPham.ChiPhiMuaHang` + `PhanTramChiPhi`.
- **Ripple:** phân bổ chi phí → cộng vào **giá trị nhập kho + giá vốn** khi tạo/cập nhật phiếu nhập từ PMH (`PhieuNhapService`, WarehouseProduct). Phiếu nhập sinh từ PMH set diễn giải "Nhập kho từ Phiếu mua hàng {mã}" (`7c695e2c0c`).

## Lỗi đã biết / lưu ý kỹ thuật
- **Migration DB chạy tay:** file `Accounting/_Sql/ChungTuMuaDichVu_ChiPhiMuaHang.sql` (tạo 3 bảng + cột) đã **XÓA khỏi repo** (`b89730070b`) sau khi chạy DB tay → **known-issue: schema mới KHÔNG có migration versioned trong source**, môi trường mới phải chạy SQL tay (tạo `ChungTuMuaDichVu`, `ChungTuMuaDichVuChiTiet`, `PhieuMuaHangChiPhiMuaHang` + cột `PhieuMuaHang.TongChiPhiMuaHang`/`PhuongThucPhanBoChiPhi`, `PhieuMuaHangSanPham.ChiPhiMuaHang`/`PhanTramChiPhi`, `PhieuNhap.TongChiPhiMuaHang`, `PhieuNhapSanPham.ChiPhiMuaHang`).
- Toàn bộ endpoint 2 controller đều `[SBAuthorize()]` **trống** (chỉ login, không gate key) — kể cả Lưu/Xóa/Hủy/PhanBoChiPhi. `cần product xác nhận` về phân quyền.
- `ChiPhiMuaHangService.PhanBoChiPhi` (`Accounting/Accounting.Service/Generated/ChiPhiMuaHangService.cs:205`): re-validate trong transaction ném Exception → rollback; message "có thể do thao tác đồng thời".
- Fix `eb1f892802`: bỏ hardcode `localhost` gọi PhieuNhap → dùng `serviceSettings.WarehouseProductServiceUrl`; thêm filter `IdDonVi` cho các query phân bổ (đa tenant).
- Fix `325f522ec6`/`5e9147e72e`/`8ece663147`: giữ chi phí mua hàng nhập tay khi lưu phiếu không có chứng từ phân bổ / khi hoàn thành-hủy phiếu nhập (map `ChiPhiMuaHang` vào `CapNhatPhieuNhapDto`).
