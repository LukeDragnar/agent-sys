---
name: sua-phieu-xuat-hoan-thanh-validate-ton-lodate
description: Sửa phiếu XUẤT đã hoàn thành thiếu validate tồn lô date → tăng SL vượt tồn vẫn lưu. Validate xuất KHÁC chiều nhập (ngược dấu + FE phát ThemMoi), phải mirror ApDung + cộng dồn theo lô.
metadata:
  type: feedback
---

# Sửa phiếu XUẤT hoàn thành — validate tồn lô date (chiều xuất ≠ chiều nhập)

**Sự cố (session 2026-07-01):** Sửa phiếu xuất ĐÃ HOÀN THÀNH, tăng SL dòng SP có lô date vượt tồn khả dụng của lô → **vẫn lưu thành công** (tồn lô âm). VD: lô A tồn 10 → xuất 5 (hoàn thành) → lô còn 5 → sửa 5→11 → lưu OK (SAI, đúng ra phải chặn "còn 5").

**Root cause:** `CapNhatPhieuXuatDaHoanThanhCore` KHÔNG validate tồn lô date trước khi lưu (phiếu NHẬP có gọi `KiemTraLoDateBiViPham`, phiếu XUẤT thì không). `ApDungLoDateThayDoiAsync(+1)` áp thẳng delta âm vào `SanPhamLoDate.SoLuong` → cho âm.

## Bài học cốt lõi

### 1. Sửa phiếu hoàn thành: NHẬP và XUẤT là 2 CHIỀU NGƯỢC DẤU — đừng tái dùng validate nhập cho xuất
- `KiemTraLoDateBiViPham` tính chiều **nhập**: `luongGiam = SoLuongCu − SoLuongMoi` và **bỏ qua hành động `ThemMoi`** (`else → continue`). Với **xuất tăng SL**, `luongGiam = Cu − Moi < 0 → continue` → không bao giờ báo lỗi.
- → Phải có method riêng cho chiều xuất: `KiemTraTonKhoLoDateSauThayDoi(..., dauTacDong=+1)`.

### 2. FE tăng SL dòng lô date (sửa phiếu xuất hoàn thành) phát entry `ThemMoi`, KHÔNG phải `Sua`
- `onChangeSoLuongSuaHoanThanh` → mở modal "Thêm lô-date" → push `loDateThayDoiList` entry **`ThemMoi`** phần chênh (khóa lô cũ qua `maLoDateCanBo`).
- → Validate xuất **phải cover `ThemMoi`** (delta = `+1*(0 − SoLuongMoi) < 0`), không được bỏ như bên nhập.

### 3. KhoChiTiet CHO tồn âm (theo spec), nhưng SP lô date / IMEI thì KHÔNG
- `BuildKhoChiTietDeltaAsync` cố tình `SET SoLuong = SoLuong + delta` cho âm — đúng cho SP thường.
- Ranh giới: **chặn nằm ở tầng lô date / IMEI**, KHÔNG ở KhoChiTiet. SP thường (không entry lô/imei) vẫn cho âm → validate chỉ chạy khi có `dto.ListLoDateThayDoi`.
- IMEI đã có guard sẵn `KiemTraImeiThemPhieuXuatKhongHopLe` (chặn thêm IMEI không còn trong kho).

### 4. Validate PHẢI mirror đúng công thức delta của apply
- Cả validate lẫn apply dùng `delta = dauTacDong * (SoLuongCu − SoLuongMoi) * tiLe`, cùng cách chọn lô key (ThemMoi→`LoSanPhamMoi`, Sua/HuyBo→`LoSanPhamCu`).
- Lệch công thức → validate và apply mâu thuẫn (chặn nhầm hoặc cho lọt).

### 5. CỘNG DỒN theo composite key lô TRƯỚC khi so tồn
- 2 dòng cùng SP + cùng lô: check riêng từng dòng có thể pass, nhưng **TỔNG > tồn** phải chặn. Gom `netDelta` theo key `(IdSanPham, IdKho, MaLoDate, LoSanPham)` rồi mới so `conLai + netDelta < 0`.
- Biên "vừa đủ" (tồn = SL xuất) phải CHO lưu → so `< -eps` (eps `1e-9`), không phải `< 0`.

### 6. Đặt validate NGOÀI transaction, chạy TRƯỚC apply
- Trả `MethodResult.ResultWithError` sớm, chưa chạm DB → an toàn cả case lô chưa tồn tại.

## Điểm cần nhớ khác
- **Phiếu chuyển kho sửa-hoàn-thành đang TẮT**: `CapNhatPhieuChuyenKhoDaHoanThanh` return dead code ngay dòng đầu ("Tính năng sửa phiếu chuyển hiện tại không sử dụng") + không xử lý `ListLoDateThayDoi`. Muốn chặn tồn cho vế nguồn phải bật lại flow trước.
- **Chưa lock lô cross-phiếu**: `LayTonKhoLoDate` là snapshot, 2 phiếu KHÁC nhau xuất cùng lô song song vẫn có thể cùng pass (hành vi có sẵn của hệ thống, không phải regression).

## File liên quan (BE `WarehouseProduct.Service/Generated/PhieuHoanThanhChinhSua/`)
- `PhieuHoanThanhChinhSuaService.LoDate.cs` — `KiemTraTonKhoLoDateSauThayDoi` (validate xuất), `KiemTraLoDateBiViPham` (validate nhập), `LayTonKhoLoDate`.
- `SuaPhieuHoanThanhService.ImeiLoDate.cs` — `ApDungLoDateThayDoiAsync` (apply, công thức delta gốc).
- `SuaPhieuHoanThanhService.PhieuXuat.cs` — call-site validate (khối 2.6).

**Liên quan:** [[gia-von-theo-ky]] (sửa phiếu xuất quá khứ → mark stale snapshot giá vốn), [[reset-ton-kho]].
