# Dev — Hóa đơn mua bán (Phiếu mua / bán / trả lại hàng mua)

> Service: **Accounting** (BE .NET) + cross-service WarehouseProduct / Branch / Customer / Order / MISA. 3 controller. Trace registry: `registry/ke-toan/PhieuMuaHangController.md`, `PhieuBanHangController.md`, `PhieuTraLaiHangMuaController.md`. Chỉ dành cho dev.

## Màn hình & route
- **Kế toán → Chứng từ mua hàng → Phiếu mua hàng** (lưới + form + import).
- **Kế toán → Chứng từ mua hàng → Trả lại hàng mua**.
- **Kế toán → Chứng từ bán hàng → Phiếu bán hàng** (chủ yếu xem/lọc/xuất; tạo phiếu bán trả lại).

## Endpoint / Controller (trace registry)

**`PhieuMuaHangController`** (`auth: mixed`; state `TinhTrangPhieuMuaHangEnum`: ChuaXuLy=1, DangXuLy=2, DaHoanThanh=3, HuyBo=4, **Nhap=5**):
| Hành động | Endpoint | Quyền |
|---|---|---|
| Tìm/lọc | `POST api/PhieuMuaHang/TimTheoDieuKien` | `PhieuMuaHang_TimTheoDieuKien` |
| Thêm | `POST api/PhieuMuaHang/ThemMoi` | `PhieuMuaHang_ThemMoi` |
| Chi tiết | `GET api/PhieuMuaHang/TimTheoId/{id}` | `PhieuMuaHang_TimTheoId` |
| Cập nhật | `POST api/PhieuMuaHang/CapNhatThongTin` | `PhieuMuaHang_CapNhatThongTin` |
| Xóa | `DELETE api/PhieuMuaHang/XoaTheoId/{id}` | `[SBAuthorize()]` trống |
| Hoàn thành | `GET api/PhieuMuaHang/ChuyenTrangThaiHoanThanh?id=` | `[SBAuthorize()]` trống |
| Hủy | `POST api/PhieuMuaHang/Huy` | `[SBAuthorize()]` trống |
| Xuất Excel | `POST api/PhieuMuaHang/ExportExcelPhieuMuaHang` | `[SBAuthorize()]` trống |
| Phân bổ chiết khấu | `POST api/PhieuMuaHang/PhanBoChietKhauTheoTongHoaDon` | **`[AllowAnonymous]`** |
| Import (partial) | `DownloadTemplate` / `ImportTemplate` / `GetDanhSach...Import` / `DeleteTmp...` / `InsertTmp...ToPhieuMuaHang` | `[SBAuthorize()]` trống |
| (internal) callback kho/branch | `POST CapNhatTrangThaiPhieuAndThuHo` | `[PrivateSBAuthorize]` |
| (internal) mua cũ đổi mới → tạo PBH | `POST TạoPhieuBanHangFromPhieuMuaHang` | `[SBAuthorize()]` trống |
| (internal) check NCC | `GET IsNccDaCoPhieuMuaHang/{idNcc}` | `[PrivateSBAuthorize]` |

**`PhieuBanHangController`** (`auth: mixed`; phần lớn internal; lịch sử ở MongoDB `PhieuBanHangMongo`):
| Hành động | Endpoint | Quyền |
|---|---|---|
| Tìm/lọc | `POST api/PhieuBanHang/TimTheoDieuKien` | `PhieuBanHang_TimTheoDieuKien` |
| Chi tiết | `GET api/PhieuBanHang/TimTheoId/{id}` | `PhieuBanHang_TimTheoId` |
| Lịch sử | `POST api/PhieuBanHang/LichSuTaoPhieu` | `[SBAuthorize()]` trống |
| Tạo phiếu bán trả lại | `POST api/PhieuBanHang/ThemMoiPhieuBanHangTraLai` | `[SBAuthorize()]` trống |
| Autocomplete | `POST api/PhieuBanHang/SearchPhieuBanHangAutoComplete` | `[SBAuthorize()]` trống |
| Thống kê HĐ | `POST api/PhieuBanHang/ThongKeTinhTrangHoaDonAsync` | `[SBAuthorize()]` trống |
| Xuất Excel | `POST api/PhieuBanHang/ExportPhieuBanHang` | `[SBAuthorize()]` trống |
| (internal) tự tạo từ đơn | `POST PhieuBanHang_SaveTuDongChoDonHang` | `[PrivateSBAuthorize]` |
| (internal) rollback | `POST RollbackPhieuBanHang` | `[PrivateSBAuthorize]` |
| (internal) callback thu | `POST CapNhatTrangThaiPhieuBanHangAndPhieuThu` | `[PrivateSBAuthorize]` |
| (internal) cập nhật trạng thái HĐ | `POST PhieuBanHang_UpdateTrangThaiHoaDonPhieuBan` | `[PrivateSBAuthorize]` |
| (internal) tra theo MaDon (đối soát) | `POST GetPhieuBanHangByMaDons` | `[PrivateSBAuthorize]` |

**`PhieuTraLaiHangMuaController`** (`auth: public`; state ChuaXuLy=1, DangXuLy=2, DaHoanThanh=3, HuyBo=4; **cả 8 endpoint `[SBAuthorize()]` trống**):
| Hành động | Endpoint |
|---|---|
| Tìm/lọc | `POST api/PhieuTraLaiHangMua/TraLaiHangMua_TimTheoDieuKien` |
| Tạo | `POST .../TraLaiHangMua_Create` |
| Cập nhật | `POST .../TraLaiHangMua_Update` |
| Chi tiết | `POST .../TraLaiHangMua_TimTheoId` |
| Load phiếu mua gốc | `POST .../GetPhieuMuaById` |
| Autocomplete phiếu mua | `POST .../SearchPhieuMuaAutocomplete` |
| Prefill | `POST .../TLHM_TimPhieuMuaHangById` |
| Xuất Excel | `POST .../ExportTraLaiHangMua` |

## Entities
- `PhieuMuaHang` + `PhieuMuaHangSanPham`; `DataReceiveTaoPhieu` (queue đẩy MISA). `MaLoaiMuaHang`: 1 vào kho, 2 cũ đổi mới, 3 từ khách, 4 nội bộ.
- `PhieuBanHang` + `PhieuBanHangSanPham`; `PhieuBanHangMongo` (lịch sử). State thanh toán: HuyThanhToan (rollback). State HĐ (`IdTinhTrangHoaDon`): ChuaXuat / DaXuat / XuatMuon.
- `PhieuTraLaiHangMua` + `PhieuTraLaiHangMuaSanPham`. `HinhThucHoanTien`: ThuLaiTienMat / GiamTruCongNo; cờ `IsTraLaiHangTrongKho`.

## Luồng code
- **PMH Thêm** (`PhieuMuaHangService.cs`): admin tổng chọn CN khác → **clone userLogin** trước khi override (tránh poison cache HybridCache, L509-522). Trong transaction: insert PMH + SP; state≠Nhap → `TinhToanTaoCacPhieu` (phiếu nhập + chi + công nợ NCC). Sau commit → `ButToan_PhieuMuaHang_Save` + cập nhật đơn đặt hàng.
- **PMH Hoàn thành** (`ChuyenTrangThaiHoanThanh`, L1843): kho `HoanThanhPhieuNhapTheoPhieuMuaHang` → Completed/AlreadyCompleted KHÔNG chạy body (tránh double bút toán); NoPhieuNhap (loại 4) → hoàn thành trực tiếp + MISA + bút toán.
- **PMH Hủy**: parse `IdsThuChi` ("id;id") → pre-check phiếu chi (read-only) → hủy phiếu nhập (kho) → hủy phiếu chi (Branch) → set HuyBo. Thứ tự PN→PC để idempotent khi retry.
- **PBH tự sinh** (`PhieuBanHang_SaveTuDongChoDonHang`): map dòng SP + giá vốn, tính thuế/chiết khấu, sinh bút toán, link `ThuChiDonHang` nếu đặt cọc, tạo `ChungTuKhac` (chuyển nợ → ThuCod, giảm doanh thu phí ship), MISA.
- **PBH callback thu** (`CapNhatTrangThaiPhieuBanHangAndPhieuThu`): cộng `TongSoTienDaThu`, đủ điều kiện → DaHoanThanh.
- **TLHM Create** (`PhieuTraLaiHangMuaService.cs`): sinh mã (PX/PT), insert phiếu + SP trong transaction; nếu DaHoanThanh → ripple theo `IsTraLaiHangTrongKho` + `HinhThucHoanTien`.

## Bug-prone / lưu ý
- **TLHM L403 BUG (nặng)** (`PhieuTraLaiHangMuaService.cs:403`): `if (isSuccess = true && reqData.IdTinhTrangPhieu == DaHoanThanh && ... == GiamTruCongNo)` — dùng `=` (gán) thay vì `==`. Do precedence, biểu thức **ghi đè cờ `isSuccess`** của transaction → có thể sinh bút toán/đồng bộ MISA dù transaction lưu phiếu đã fail. Cần sửa `==`.
- **TLHM phiếu xuất mồ côi** (`...Service.cs:906-907` `//TODO`): phiếu xuất đã tạo KHÔNG rollback khi phiếu thu fail.
- **TLHM catch lồng** (L393-398 + L456-459): `throw;` rồi outer nuốt → mất stack trace.
- **PMH phân quyền lỏng** (`PhieuMuaHangController.cs`): L64 Xóa, L108 Hoàn thành, L120 Hủy, L146 Export, L79 tạo PBH dùng `[SBAuthorize()]` trống; L133 `PhanBoChietKhauTheoTongHoaDon` = `[AllowAnonymous]`. Thao tác xóa/hủy/hoàn thành đáng lẽ cần key riêng.
- **PMH callback nới lỏng thanh toán** (`...Service.cs:1216-1239`): điều kiện check `IsThanhToanThuChiThanhCong`/`TongSoTienDaChi==TongGiaNhap` đã comment out → hiện chỉ cần `IsPhieuNhapHoanThanh=true` là set hoàn thành.
- **PBH phân quyền lỏng**: LichSuTaoPhieu L56, ThemMoiPhieuBanHangTraLai L98, SearchAutoComplete L111, ThongKe L137, Export L163 đều `[SBAuthorize()]` trống.
- **PBH `ThemMoiAsync` deprecated** ("CODE CŨ HIỆN TẠI KHÔNG DÙNG NỮA") — luồng thật là `PhieuBanHang_SaveTuDongChoDonHang`. *Cần xác nhận trước khi sửa.*
- **PBH tạo phiếu bán trả lại — phiếu chi không rollback hết** (`...Service.cs:2068` `//TODO: xóa phiếu chi`): khi phiếu nhập fail chỉ xóa bút toán → còn rác phiếu chi.
- **PBH no-op / logic trùng**: L1599 self-assignment `TongTienChietKhauSanPham`; L1634-1637 và L1650-1653 xóa `lstPhieuBanHangSanPhamXoa` 2 lần.
- **PBH `GetPhieuBanHangByMaDons` trả `Ok(result)` thẳng** thay vì `ResponseResult` (lệch convention).

## File source (path:line)
- BE controllers: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Accounting\Accounting.API\Controllers\PhieuMuaHangController.cs` (L64/L108/L120/L133/L146), `PhieuBanHangController.cs` (L56/L98/L111/L137/L163), `PhieuTraLaiHangMuaController.cs` (L41–L132 — 8 endpoint).
- BE services: `...\Accounting.Service\Generated\PhieuMuaHangService.cs` (L509-522 clone userLogin, L1216-1239 callback, L1843-1857 hoàn thành), `PhieuBanHangService.cs` (L1599, L1634-1653, L2068, L1835, L2602), `PhieuTraLaiHangMuaService.cs` (**L403 bug**, L906-907, L1303-1308).
- Enum `TinhTrangPhieuMuaHangEnum`: `Libs/Models.Core/Enum.cs:1055` (Nhap=5).
- DTO: `Models.Core.DTO.Accounting.Client.*` (PhieuMuaHang, PhieuBanHang, PhieuTraLaiHangMua).
- SP: không có (toàn EF; PBH lịch sử ở MongoDB).
