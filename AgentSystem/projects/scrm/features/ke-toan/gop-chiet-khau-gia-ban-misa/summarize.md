---
alias: ["gộp chiết khấu vào giá bán", "trừ chiết khấu vào giá", "giá net khi đồng bộ MISA", "IsTruChietKhauKhiDongBo", "IsGiaDaTruChietKhau", "không hạch toán TK 521", "đồng bộ phiếu bán hàng MISA", "lệch 1đ MISA", "reconcile MISA"]
module: ke-toan
service: Accounting
trang_thai: mới (nhánh nganlq/gop-chiet-khau-vao-gia-ban, delta release_product) — cần product xác nhận
snapshot: "Snapshot — VERIFY lại với code hiện tại. Watermark scan: HEAD origin/release_product = e617abcbbf (2026-07-15). Kho là bản đồ, code là chân lý: path:line có thể lệch, mở source qua codemap/ripgrep để chốt."
phu_thuoc: ["CauHinhKetNoiKeToan (cờ bật/tắt cơ chế)", "Phiếu bán hàng (nơi đánh dấu IsGiaDaTruChietKhau)", "MISA / kết nối kế toán (tầng convert + đồng bộ)", "OrderLogistic / đơn online (loại phiếu bán online cũng áp reconcile)"]
link_registry: ../../registry/ke-toan/PhieuBanHangController.md
---

# Gộp chiết khấu vào giá bán khi đồng bộ MISA (giá net, bỏ TK 521)

> ⚠️ **Snapshot — verify với code.** Scan tại HEAD `origin/release_product = e617abcbbf`. Số liệu/behavior dưới đây trích từ source THẬT (`D:/PROJECT/Git/_SourceForBuild/scrm_build/Accounting/...`) nhưng vẫn nên mở lại file để chốt `path:line` khi sửa. KHÔNG cite được migration versioned cho 2 cột mới (xem "Lưu ý kỹ thuật").

## Là gì
Một **cơ chế mới ở tầng đồng bộ phiếu bán hàng sang MISA**: khi bật, hệ thống **trừ thẳng chiết khấu vào đơn giá** (đẩy đơn giá NET sang MISA) thay vì tách một dòng chiết khấu riêng và hạch toán vào TK 521. Điểm mấu chốt: việc trừ diễn ra **lúc đồng bộ** — **KHÔNG sửa đơn hàng / phiếu gốc** (khác các cấu hình cũ vốn sửa số trên phiếu). Đi kèm là **fix lệch 1đ** khi đồng bộ MISA cho luồng cũ (không bật cơ chế), dồn phần dư làm tròn vào chiết khấu để giữ VAT đúng.

Hai cờ mới:
- **`CauHinhKetNoiKeToan.IsTruChietKhauKhiDongBo`** (mặc định `false`) — bật/tắt cơ chế ở cấu hình kết nối kế toán. `CauHinhKetNoiKeToan.cs:35`.
- **`PhieuBanHang.IsGiaDaTruChietKhau`** (mặc định `false`) — cờ đánh dấu phiếu đã tạo ở chế độ **giá net**, phục vụ **báo cáo / đối soát** phân biệt phiếu "chiết khấu = 0 do cố ý gộp vào giá" với phiếu thật sự không có chiết khấu. `PhieuBanHang.cs:80`.

## Hành vi đúng (expected)

| Trạng thái cờ | Khi đồng bộ phiếu bán hàng sang MISA |
|---|---|
| `IsTruChietKhauKhiDongBo = true` (bật) | Đơn giá đẩy sang MISA là **giá NET** (đã trừ chiết khấu). `total_sale_amount = Σ(amount dòng net)`, `total_discount_amount = 0` (cả `sa_invoice`). **Không set `discount_type`** (không còn dòng chiết khấu → không hạch toán TK 521). Header VAT = Σ VAT dòng (tính lại trên giá sau giảm). |
| `IsTruChietKhauKhiDongBo = false` (tắt, luồng cũ) | Giữ giá gốc + dòng chiết khấu riêng; `discount_type = 3` khi có `ChietKhauToanBo`. **Áp thêm reconcile lệch 1đ** (xem dưới). |

### Reconcile lệch làm tròn (chỉ khi cờ TẮT, `TongTienThu > 0`)
Mục tiêu: tổng MISA `total = sale − discount + vat` khớp đúng **tiền khách trả thực** (`TongTienThu`), **giữ nguyên VAT** cho đúng số thuế (vì `total = sale − discount + vat`, muốn `total += lệch` thì `discount −= lệch`). Dồn phần dư vào **dòng chiết khấu cuối cùng có VAT > 0** (fallback dòng cuối). `MisaRequestConverter.cs:1889–1934`.

Điều kiện áp (guard chống che sai số thật):
- `lech != 0`, **và** `|lech| <= số dòng detail`, **và** `discount_amount + delta >= 0` (không làm chiết khấu âm).
- **Tách 2 nhánh theo `IdLoaiBanHang`:**
  - `IdLoaiBanHang == 1` (Bán hàng tại cửa hàng / offline) → **giữ NGUYÊN công thức cũ** (đã test đúng, không đổi hành vi).
  - Còn lại (2=online, 3=đối soát online, 4=thu hàng hoàn online, 5=thu cũ đổi mới, 6=sàn TMĐT, 7=bán trả lại — gồm **đơn OrderLogistic**) → dùng **công thức làm tròn theo số MISA foot**. Lý do: các loại này `total_sale_amount` / `total_discount_amount` có thể **LẺ** (ví dụ đơn OrderLogistic `230434.78`) khiến `lech ≈ 0` → **bỏ sót reconcile** ở công thức cũ.

## Để làm gì / cho ai
- **Kế toán:** một số đơn vị muốn MISA nhận **giá bán net**, không hạch toán chiết khấu qua TK 521 — cơ chế này làm việc đó ở tầng đồng bộ, không đụng dữ liệu bán hàng gốc.
- **Đối soát:** cờ `IsGiaDaTruChietKhau` trên phiếu giúp báo cáo phân biệt phiếu net (chiết khấu đã gộp vào giá) với phiếu chiết khấu = 0 bình thường.
- **Fix lệch 1đ:** giải quyết chênh lệch làm tròn giữa tổng SCRM và tổng MISA cho **cả phiếu bán hàng offline và đơn online (OrderLogistic)** mà vẫn giữ đúng số VAT.

## Luồng (ripple)
```
CauHinh: IsTruChietKhauKhiDongBo (CauHinhKetNoiKeToanService: create/update + MarkDirty)
   │
Lưu phiếu bán hàng (PhieuBanHangService, isTruChietKhauVaoGia)
   ├─ ValueGiamGiaChung = 0  ; TongTienChietKhauSanPham = 0  (khi bật)   [PhieuBanHangService.cs:1618–1620]
   └─ set PhieuBanHang.IsGiaDaTruChietKhau + MarkDirty                    [PhieuBanHangService.cs:1632–1633]
   │
Đồng bộ MISA (DongBoPhieuCrmService.Sync → ConvertToMisaFormat(..., cauHinh.IsTruChietKhauKhiDongBo))  [DongBoPhieuCrmService.cs:1094–1102]
   └─ MisaRequestConverter.ConvertPhieuBanHang(..., isTruChietKhauKhiDongBo)                            [MisaRequestConverter.cs:1544]
        ├─ bật → đơn giá net + total_discount=0 + bỏ discount_type       [1781–1787, 1875–1887, 1948–1952]
        └─ tắt → reconcile lệch dồn vào chiết khấu (offline vs online)   [1896–1934]
```

## Lưu ý kỹ thuật / known-issue
- **Nguồn cờ ở luồng LƯU phiếu cần verify:** `PhieuBanHangService` set `IsGiaDaTruChietKhau = isTruChietKhauVaoGia` (`PhieuBanHangService.cs:1632`); comment tại `:1630–1631` ghi cờ lấy từ config Accounting và **áp cho mọi đơn (offline + online)**. Còn tầng đồng bộ MISA đọc trực tiếp `cauHinh.IsTruChietKhauKhiDongBo` (`DongBoPhieuCrmService.cs:1101`). → Cần mở source xác nhận `isTruChietKhauVaoGia` ở luồng lưu có phải cùng nguồn `IsTruChietKhauKhiDongBo` hay là cấu hình khác (tránh giả định hai chỗ luôn đồng bộ).
- **Hai cột mới không cite được migration versioned:** `CauHinhKetNoiKeToan.IsTruChietKhauKhiDongBo` + `PhieuBanHang.IsGiaDaTruChietKhau` — môi trường mới cần kiểm tra script tạo cột (giống pattern known-issue của cụm phân bổ chi phí mua hàng, xem `phan-bo-chi-phi-mua-hang/dev.md`).
- **`MisaRequestConverter.ConvertToMisaFormat` đổi signature** thêm tham số `bool isTruChietKhauKhiDongBo = false` (`MisaRequestConverter.cs:22` interface, `:58` impl) — mọi caller khác dùng default `false` (giữ luồng cũ), chỉ luồng đồng bộ phiếu bán hàng truyền cờ thật.
- **Reconcile chỉ chạy khi cờ TẮT.** Khi bật (`isTruChietKhauKhiDongBo=true`) thì `total_discount_amount` đã ép về 0, không vào nhánh reconcile — số net tự khớp vì header = Σ dòng.
- **Registry liên quan cần cập nhật thêm:** `registry/ke-toan/PhieuBanHangController.md` (cờ IsGiaDaTruChietKhau) và node cấu hình kết nối kế toán (`CauHinhKetNoiKeToanController.md`) — cờ IsTruChietKhauKhiDongBo trong create/update.

## Nguồn (path:line — verify lại)
- `Accounting/Accounting.DataContext/Entity/CauHinhKetNoiKeToan.cs:35` — cờ `IsTruChietKhauKhiDongBo`.
- `Accounting/Accounting.DataContext/Entity/PhieuBanHang.cs:80` — cờ `IsGiaDaTruChietKhau`.
- `Accounting/Accounting.Service/Generated/CauHinhKetNoiKeToanService.cs:210,307,320,346` — set khi create/update (MarkDirty) + map ra DTO.
- `Accounting/Accounting.Service/Generated/PhieuBanHangService.cs:1618–1633` — recompute ValueGiamGiaChung/TongTienChietKhauSanPham = 0 + set IsGiaDaTruChietKhau.
- `Accounting/Accounting.Service/Generated/DongBoPhieuCrmService.cs:1094–1102` — truyền cờ vào ConvertToMisaFormat.
- `Accounting/Accounting.Service/Generated/Partners/MISA/MisaRequestConverter.cs:22,58,1544,1781–1787,1875–1934,1948–1952` — signature + logic net + reconcile lệch (offline/online).
