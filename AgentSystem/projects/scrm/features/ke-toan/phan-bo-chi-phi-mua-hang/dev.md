# Dev — Chứng từ mua dịch vụ & Phân bổ chi phí mua hàng

> Service: **Accounting** (BE .NET) + cross-service WarehouseProduct (PhieuNhap giá vốn) / Branch (ThuChi) / ButToan / MISA. Controllers `ChungTuMuaDichVuController` + `ChiPhiMuaHangController`. Trace registry: `registry/ke-toan/ChungTuMuaDichVuController.md`. Tính năng mới (nhánh `phan-bo-chi-phi-mua-hang`, merge release_product 2026-06-26, commit `1100d02111`). Chỉ dành cho dev.

## Màn hình & route
- **Kế toán → Chứng từ mua dịch vụ**: lưới + form Lưu + xem chi tiết + xóa + hủy.
- **Popup phân bổ chi phí** trong FORM Phiếu mua hàng: gọi `ChiPhiMuaHangController`.

## Endpoint / Controller
**`ChungTuMuaDichVuController`** (`[TienIchAttribute(MaTienIchSCRM.TI_KeToan)]`; tất cả `[SBAuthorize()]` trống):
| Hành động | Endpoint |
|---|---|
| Danh sách | `POST api/ChungTuMuaDichVu/TimTheoDieuKien` |
| Chi tiết | `POST api/ChungTuMuaDichVu/ViewChiTiet?id=` |
| Lưu (thêm/sửa) | `POST api/ChungTuMuaDichVu/Luu` |
| Xóa | `POST api/ChungTuMuaDichVu/XoaPhieu?id=` |
| Hủy | `POST api/ChungTuMuaDichVu/Huy?id=` |

**`ChiPhiMuaHangController`** (KHÔNG có TienIchAttribute; tất cả `[SBAuthorize()]` trống):
| Hành động | Endpoint |
|---|---|
| Chứng từ khả dụng | `POST api/ChiPhiMuaHang/LayChungTuChiPhiKhaDung` |
| Phân bổ của 1 phiếu | `POST api/ChiPhiMuaHang/LayPhanBoTheoPhieuMuaHang?idPhieuMuaHang=` |
| Phân bổ | `POST api/ChiPhiMuaHang/PhanBoChiPhi` |
| Xóa phân bổ | `POST api/ChiPhiMuaHang/XoaPhanBo?idMapping=` |

## Entities (schema mới — CHẠY DB TAY, không có migration versioned)
- **`ChungTuMuaDichVu`** (header): NCC + nhân viên mua + thanh toán (IsThanhToanLuon/ChiTienMat/ChiChuyenKhoan) + `IsChiPhiMuaHang` + `TongTien`/`DaPhanBo`/`ConLaiPhanBo` + `TrangThai` (Nhap=0/DaHachToan=1/DaHuy=2) + đồng bộ MISA (StatusSendPartner/AppId). Nhiều string field có `[Column(TypeName)]`.
- **`ChungTuMuaDichVuChiTiet`** (dòng dịch vụ): IdDichVu/MaDichVu/TenDichVu + IdTkChiPhi + TongTienHang/CK/thuế GTGT.
- **`PhieuMuaHangChiPhiMuaHang`** (mapping N-N): `IdPhieuMuaHang` × `IdChungTuMuaDichVu` (+optional `IdChungTuMuaDichVuChiTiet`) + `SoTienPhanBo`.
- **Cột bổ sung:** `PhieuMuaHang.TongChiPhiMuaHang` + `PhuongThucPhanBoChiPhi`; `PhieuMuaHangSanPham.ChiPhiMuaHang` + `PhanTramChiPhi`; `PhieuNhap.TongChiPhiMuaHang`; `PhieuNhapSanPham.ChiPhiMuaHang`.
- DTO: `Models.Core.DTO.Accounting.Client.ChungTuMuaDichVu` (`ChungTuMuaDichVuRequest`, `ChiPhiMuaHangRequest` → `AllocateChiPhiMuaHangRequest` {PhuongThucPhanBoChiPhi, LstPhanBo[AllocationItem], LstChiPhiTheoDong[LineCostItem]}).

## Luồng code
- **Luu** (`ChungTuMuaDichVuService.cs:242`): Validate → `TongTien=Σ(TongTienHang−CK+GTGT)`. Thêm mới → header state=Nhap, SinhMaPhieu(ChungTuMuaDichVu). Sửa: chặn nếu `DaPhanBo>0` (L262) / DaHuy (L264) / DaHachToan (L266). Đẩy DaHachToan (L389+): sinh phiếu chi (tự tách tiền mặt/CK theo IdPhuongThucThanhToan L322-330) + công nợ NCC + bút toán + MISA (guarantee 1 chiều — downstream fail thì không lưu, L394).
- **XoaPhieu** (`:608`): chặn khi `TrangThai=DaHachToan` (L617); OK → `ButToan_Xoa_IdPhieu(id, MDV)`.
- **Huy** (`:652`): set DaHuy (L660) + `ButToan_Xoa_IdPhieu(id, MDV)`.
- **PhanBoChiPhi** (`ChiPhiMuaHangService.cs:205`): Rule E chặn PMH DaHoanThanh (L217, trừ boQuaRuleE); validate chứng từ IsChiPhiMuaHang+DaHachToan (L235-238); validate Σ ≤ TongTien (L261, dung sai 0.0001). Transaction: xóa mapping cũ phiếu → insert mapping mới → cập nhật DaPhanBo/ConLaiPhanBo + re-validate chống race (L307) → set PhieuMuaHang.TongChiPhiMuaHang + PhuongThucPhanBoChiPhi → `RedistributeToSanPham`.
- **RedistributeToSanPham** (`:473`): TuNhap(3)+lstChiPhiTheoDong → áp đúng, validate Σ khớp (dung sai 1đ, L487); TuNhap không danh sách → scale giữ tỷ lệ tay; theo đơn giá(1)=GiaNhap×SoLuong / số lượng(2)=SoLuong, sai số dồn dòng cuối (L529). `ApplyLineCost` (:575) ghi `ChiPhiMuaHang` + `PhanTramChiPhi`.
- **XoaPhanBo** (`:344`): Rule E; hoàn DaPhanBo chứng từ + tính lại tổng + trải lại SP.
- **HoanPhanBoKhiXoaPhieu** (`:408`) / **RecomputeAllocationForPhieu** (`:537`): reset/recompute khi xóa hoặc lưu lại phiếu.

## Bug-prone / lưu ý
- **KHÔNG có migration versioned:** file `Accounting/_Sql/ChungTuMuaDichVu_ChiPhiMuaHang.sql` (171 dòng) đã **xóa khỏi repo** (`b89730070b`) sau khi chạy DB tay → **môi trường mới phải chạy SQL tay** để tạo 3 bảng + các cột. Đây là known-issue quan trọng.
- **Toàn bộ endpoint `[SBAuthorize()]` trống** (kể cả Lưu/Xóa/Hủy/PhanBoChiPhi) — không gate permission key. Rủi ro phân quyền.
- **Rule E (Phase 1)**: PMH DaHoanThanh không cho đổi phân bổ vì không rebuild FIFO/giá vốn. Ngoại lệ `boQuaRuleE=true` chỉ dùng trong luồng Lưu phiếu.
- **Race G6**: re-validate DaPhanBo trong transaction (đọc lại qua GetByIdAsync) — nếu request song song đẩy vượt thì ném Exception + rollback.
- Fix `eb1f892802`: bỏ hardcode localhost gọi PhieuNhap → `serviceSettings.WarehouseProductServiceUrl`; thêm filter IdDonVi (đa tenant).
- Fix `5e9147e72e`/`8ece663147`/`325f522ec6`: giữ ChiPhiMuaHang khi tạo/cập nhật/hoàn thành-hủy phiếu nhập (map vào CapNhatPhieuNhapDto) + giữ chi phí nhập tay khi lưu phiếu không có phân bổ.

## File source (path:line — VERIFY lại, watermark có thể drift)
- BE controller: `Accounting/Accounting.API/Controllers/ChungTuMuaDichVuController.cs`, `ChiPhiMuaHangController.cs`.
- BE service: `Accounting/Accounting.Service/Generated/ChungTuMuaDichVuService.cs` (Luu:242, XoaPhieu:608, Huy:652), `ChiPhiMuaHangService.cs` (PhanBoChiPhi:205, RedistributeToSanPham:473, XoaPhanBo:344).
- Entity: `Accounting/Accounting.DataContext/Entity/ChungTuMuaDichVu.cs`, `ChungTuMuaDichVuChiTiet.cs`, `PhieuMuaHangChiPhiMuaHang.cs`.
- Enum: `Libs/Models.Core/Enum/Accounting/EnumAccountingType.cs:129` (TrangThaiChungTuMuaDichVu), MDV prefix `:122`.
- Cross-service: WarehouseProduct `PhieuNhapService` (giá vốn), Branch ThuChi (phiếu chi), MISA `ChungTuMuaDichVuModel` + `MisaRequestConverter`.
