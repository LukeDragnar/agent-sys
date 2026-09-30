# SP quy đổi đơn vị: gửi ĐVVC bằng số lượng LẺ → cả SL lẫn giá trị đơn gấp `TiLeQuyDoi` lần

> Sự cố 21/07/2026 · khách BigSun · Viettelpost · sinh 2 mã việc: **B265** (triệu chứng số lượng) và **B262** (triệu chứng giá trị). Fix: commit `746cab8387` (nganlq), deploy OrderLogistic.API 4.4140.

## Hiện tượng

Đơn `BS00100000036`: CRM hiển thị SL **2 / 2 / 1** (đơn vị Kg), tổng tiền hàng 1.954.095 → sau CK 1.180.000.
Payload sang VTP lại là **20 / 20 / 1**, `PRODUCT_QUANTITY: 41`, và màn VTP hiện **Tổng giá trị 19.160.000**:

```
590.000 × 20 = 11.800.000
368.000 × 20 =  7.360.000
             = 19.160.000   ← VTP tự cộng từ LIST_ITEM (giá × số lượng)
```

Lệch đúng `TiLeQuyDoi = 10` lần.

## Nguyên nhân

SP bán theo đơn vị quy đổi (Kg/thùng...) lưu **2 số lượng**:
- `SoLuong` = số quy ra **đơn vị nhỏ** (để trừ kho) = `SoLuongBanTheoDonViQuyDoi × TiLeQuyDoi`
- `SoLuongBanTheoDonViQuyDoi` = số **bán thật** theo đơn vị bán

Và `DonGia` là giá theo **đơn vị bán** (`ThanhTien = DonGia × SoLuongBanTheoDonViQuyDoi` — xem `Accounting.DataContext/Entity/PhieuBanTraLaiSanPham.cs`).

`OrderService.GenerateProduct` (dựng payload đăng đơn) trước đây lấy thẳng `s.SoLuong` → ghép **giá đơn-vị-bán × số-lượng-đơn-vị-nhỏ** → sai hệ số. Vì VTP (và các hãng khác) cộng tổng giá trị từ `LIST_ITEM`, lỗi hiện ra **hai triệu chứng khác nhau** cho cùng một dòng code: khách kho báo "sai số lượng", khách sale báo "sai giá trị đơn".

Fix hiện tại:
```csharp
Quantity = FunctionCommon.ConvertToInt(
    s.IsSuDungQuyDoiDonViTinh == true ? s.SoLuongBanTheoDonViQuyDoi : s.SoLuong),
Price = FunctionCommon.ConvertToDouble(s.DonGia),
```

## Bẫy còn lại

- **Chỗ dựng payload là `OrderService.GenerateProduct`/`MapDataReqCreateOrder`, KHÔNG phải controller hãng.** `ViettelPostController` và các controller hãng khác chỉ là **stub metadata** (`GiaoHangDoiTacMa`/`Ten`) — tìm logic ở đó là mất buổi. Logic riêng hãng nằm bên `delivery_connect`.
- **Hai nhánh dựng Products khác nhau**: `isTinhPhi = true` (tính phí) lấy từ Mongo `DonHangSanPhamInfos`, `isTinhPhi = false` (đăng đơn thật) lấy từ DB qua `GenerateProduct`. Sửa đơn vị/số lượng phải rà **cả hai**, không thì tính phí một đằng đăng đơn một nẻo.
- Chỗ nào ghi Mongo `DonHangSanPhamInfo.SoLuong` cũng phải đổi sang `SoLuongBanTheoDonViQuyDoi` (đã có ở `DonHangService.cs` ~2017-2040).
- `IsSuDungQuyDoiDonViTinh = true` mà `BangQuyDoiJson` rỗng/không khớp `IdDonViTinh` → cả `SoLuong`, `SoLuongBanTheoDonViQuyDoi`, `DonGia` **không được set** (nhánh `else` chỉ dành cho SP không quy đổi) → `ConvertToInt(null) = 0` → dòng SP đóng góp **0đ**. Chưa gặp trên thực tế nhưng đường code cho phép.
- Giá trị khai sang ĐVVC vẫn lấy **trước chiết khấu** (`tongTienSp = Σ DonGia × SL`) → sau fix này đơn ví dụ ra 1.954.095 chứ không phải 1.180.000. Đó là task **1136** (giá trị bảo hiểm sau chiết khấu), còn treo.
- Dòng SP có giá lẻ (giá bóc VAT) còn dính lỗi `ConvertToInt` bên `delivery_connect` → xem `projects/delivery_connect/lessons/converttoint-nuot-so-le-ve-0.md`.
