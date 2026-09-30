# Luồng "Yêu cầu xuất hóa đơn" — Sơ đồ

> Từ lúc **đơn về** → set cờ → (cập nhật tay) → xuất HĐ. Cột `IsYeuCauXuatHoaDon`.

## TOÀN CẢNH (theo thời gian)

```
════════════════ ① ĐƠN VỀ (tạo đơn) ════════════════
  ┌────────────┐   ┌────────────┐   ┌──────────────────────┐
  │ Tạo đơn TAY│   │ Import đơn │   │ Đơn ECOM về từ SÀN    │
  └─────┬──────┘   └─────┬──────┘   └──────────┬───────────┘
        │ cờ=C0          │ cờ=C0     tạo: cờ=false │→ có invoice-data:
        └────────────────┴──────────┬──────────┘   cờ=IsRequested??false
                                    ▼
                  ┌──────────────────────────────────┐
                  │ DonHangLogistic.IsYeuCauXuatHoaDon│ = null │ true │ false
                  └────────────────┬─────────────────┘
                                   │
═══════════ ② CẬP NHẬT CỜ MANUAL (tuỳ chọn) ═══════════
             User mở popup "Thông tin thuế" trên đơn
              ┌────────────────────▼───────────────────┐
              │ Toggle "Yêu cầu xuất hóa đơn"           │
              │   Đơn ECOM?  CÓ → DISABLE (chỉ xem)     │ ◄─ sàn thắng
              │              KHÔNG → bật/tắt tay        │
              └────────────────────┬───────────────────┘
                                   │ Lưu → SyncYeuCauXuatHoaDonToDon
                                   │   (ghi cờ + MarkDirty + Update + ES index; chặn ecom)
                                   ▼
                  ┌──────────────────────────────────┐
                  │ DonHangLogistic (cờ ĐÃ cập nhật)  │
                  └────────────────┬─────────────────┘
                                   │
═══════════ ③ ĐẾN THỜI ĐIỂM XUẤT HĐ ═══════════
         ┌─────────────────────────┴────────────────────┐
         ▼ AUTO (job publish/draft)          ▼ MANUAL (user bấm xuất)
  ┌────────────────────┐              ┌────────────────────────┐
  │ 🆕 KHÔNG gate cờ   │              │ (không gate)           │
  │  (cờ không chặn xuất│              │                        │
  │   — mọi đơn đều qua)│              │                        │
  └─────────┬──────────┘              └───────────┬────────────┘
            └──────────────────┬──────────────────┘
                               ▼
              ┌────────────────────────────────┐
              │ Fill_InvoiceBuyerInfo → XUẤT HĐ │  ◄─ chi tiết ↓
              └────────────────────────────────┘
```
> ⚠️ **Gate cũ "cờ==false → skip" đã GỠ BỎ** (commit `61cce5fb27`): cờ KHÔNG chặn xuất, chỉ quyết NGUỒN TT người mua.

## CHI TIẾT: Fill_InvoiceBuyerInfo (chọn người mua)

```
   coYeuCauXuatHoaDon = ecom ? (KhachHangYeuCau ?? cờđơn ?? false) : cờđơn
        ┌──────────────────┴──────────────────┐
        ▼ != false (null/true = CÓ yêu cầu)    ▼ false (KHÔNG yêu cầu)
   ┌──────────────┐                      ┌──────────────┐
   │ CÓ YÊU CẦU   │                      │ KHÔNG YÊU CẦU│
   │(Config A/B)  │                      └──────┬───────┘
   └──────┬───────┘                        ┌────┴────┐
    ┌─────▼─────┐                          ▼ecom     ▼thường
    │Có TT thuế?│                    ┌──────────────┐ ┌──────┐
    └─────┬─────┘                    │EcomLayTheo    │ │case 0│
    có│      │không                  │ThongTinDon?   │ │KHmặcđ│
      ▼      ▼                       │(default FALSE)│ └──────┘
  ┌──────┐ ┌────────────────────────┐└────┬─────┘
  │case 1│ │ ĐỦ field bắt buộc?      │true│  │false
  └──────┘ │ [ConfigA] check ĐƠN +   │    ▼    ▼
           │ HỒ SƠ KHÁCH (đủ 1 = ok) │┌──────┐┌──────┐
           └──────┬─────────────────┘ │case 3││case 0│
              đủ │      │ thiếu THẬT   │theođơn││KHmặcđ│
                 ▼      ▼ (cả 2 rỗng)  └──────┘└──────┘
          ┌──────┐  ┌──────────────────────┐
          │case 2│  │ Config B: XuLyKhiThieu │
          └──────┘  └──────────┬───────────┘
                    ┌──────────┼───────────────┐
                    ▼ MacDinh  ▼ BaoLoi        ▼ MotPhanMacDinh 🆕
              ┌──────────┐ ┌──────────┐  ┌───────────────────┐
              │ case 0   │ │ throw    │  │ case 4            │
              │ KH mặc   │ │ (dừng,   │  │ base đơn+khách    │
              │ định     │ │ thủ công)│  │ + ghi đè field    │
              └──────────┘ └──────────┘  │ chọn = KH mặc định│
                                         └───────────────────┘
```

**5 cách lấy TT người mua (`cachLayThongTinKhachHang`):**
- **case 1** = TT thuế đã khai (company: MST+tên+địa chỉ / personal / household — validate riêng)
- **case 2** = TT khách (`GetInfoCustomer`: tên/địa chỉ đơn → fallback hồ sơ KH; throw khi CẢ đơn LẪN hồ sơ KH đều rỗng tên/địa chỉ)
- **case 3** = Ecom lấy theo đơn (Tên/Địa chỉ/SĐT từ đơn) — khi `EcomLayTheoThongTinDon=true`; false → case 0
- **case 0** = KH mặc định (`customerNameDefault` / `maKhachHangMacDinh`)
- 🆕 **case 4** = 1 phần KH mặc định (`FillBuyerMotPhanMacDinh`): dựng theo đơn+khách rồi ghi đè field trong `FieldsLayTuMacDinh` (Tên/Địa chỉ/MaKhachHang) bằng KH mặc định

> 🆕 **Ecom dùng `KhachHangYeuCau` (tín hiệu sàn), KHÔNG tin cờ đơn** — cờ ecom null → false (không coi null=true). Đơn thường: cờ null=true.

## Config A/B — waterfall (màn config-invoice)

```
┌─────────────────────────────────────────────────────────┐
│  THỨ TỰ LẤY TT NGƯỜI MUA (đơn CÓ yêu cầu xuất HĐ)         │
├─────────────────────────────────────────────────────────┤
│  ①  Khách đã khai TT thuế   → xuất theo TT thuế          │
│  ②  Chưa khai → lấy TT khách (đơn + hồ sơ khách)         │
│       [Config A] Field bắt buộc (kiểm đơn HOẶC khách):   │
│         ☑Tên  ☑Địa chỉ  ☐SĐT  ☐MST  🆕(default Tên+Đchỉ)│
│  ③  KHÔNG đủ field (cả đơn lẫn khách) → [Config B]        │
│       ○ Xuất bằng KH mặc định  (default)                │
│       ○ Dừng & báo lỗi → xử lý thủ công                  │
│       ○ 🆕 1 phần KH mặc định (chọn field lấy từ mặc định)│
└─────────────────────────────────────────────────────────┘
```
