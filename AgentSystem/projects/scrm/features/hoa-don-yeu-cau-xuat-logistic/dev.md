# Cờ "Yêu cầu xuất hóa đơn" đơn logistic — Kỹ thuật

> Service **OrderLogistic** + **Invoice** + **ESDocument**. DB-first (không EF migration → đổi schema phải ALTER TABLE tay). ⚠️ `path:line` là snapshot lúc viết — **verify với code hiện tại** (mở source qua codemap/ripgrep), behavior/field có thể đã đổi.

## 1. Cột + Config

| Thứ | File | Ghi chú |
|---|---|---|
| Cột đơn `IsYeuCauXuatHoaDon` (bool?) | `OrderLogistic/OrderLogistic.DataContext/Entity/DonHangLogistic.cs` (~1033) | Nullable, KHÔNG backfill. SQL ALTER TABLE tay |
| Config C0 `IsYeuCauXuatHoaDonMacDinh` (bool, default true) | `Libs/Models.Core/DTO/OrderLogistic.Client/CauHinh/CauHinhSettingDto.cs` (2 class: Dto + InitDto) | Default cờ khi tạo đơn |
| Config `EcomLayTheoThongTinDon` (bool, **default FALSE**) | `Libs/Models.Core/DTO/Invocie.Client/CauHinh/CauHinhSettingDto.cs` (2 class, ~46/~178) | Ecom không yêu cầu: `true`=theo đơn / `false`=KH mặc định (radio). **Default đổi true→FALSE** (khớp logic cũ: đơn ecom → KH mặc định). Field-map cũ `EcomFieldsLayTheoDon` đã comment |
| Config A `RequiredFieldsLayTheoDon` (List<string>?) + B `XuLyKhiThieuThongTin` (string?) | cùng file trên (2 class) | Config A default `[TenNguoiMua, DiaChi]`. Config B **3 giá trị**: `MacDinh`/`BaoLoi`/`MotPhanMacDinh` |
| Config B `FieldsLayTuMacDinh` (List<string>?) | cùng file trên (2 class, ~61/~193) | 🆕 Khi Config B=`MotPhanMacDinh`: field lấy từ KH mặc định (`TenNguoiMua`/`DiaChi`/`MaKhachHang`), còn lại giữ theo đơn+khách |
| Config `LoaiDonKhongXuatPhiShip` (List<string>?) | `Libs/Models.Core/DTO/OrderLogistic.Client/CauHinh/CauHinhSettingDto.cs` (~278/~438) | 🆕 Blocklist loại đơn (UnitCode) KHÔNG xuất phí ship — OR với công tắc global `IsKhongXuatHDDT_PhiGiaoHang` |
| ES field `IsYeuCauXuatHoaDon` | `ESDocument/ESDocument.DataContext/Entity/DonHangLogisticModel.cs` (~614) + `DonHangLogisticES.cs` (~619) | JsonProperty `isYeuCauXuatHoaDon` |

## 2. Set cờ lúc tạo đơn (= Config C0)

| Nguồn | File | Ghi chú |
|---|---|---|
| Tạo tay | `OrderLogistic.Service/Generated/DonHangService.cs` (~1019, `TinhToanTaoUpdateDon`) | `= cfgDonVi.CauHinhSettingDto.IsYeuCauXuatHoaDonMacDinh` |
| Ecom — lúc tạo đơn | `OrderLogistic.Service/Generated/Ecommerce/DonHangService.cs` (~571, tạo `donHangLogisticDb`) | 🆕 **set `= false` NGAY khi tạo** (ecom chỉ true/false; sàn chưa báo = false, KHÔNG để null trôi) |
| Ecom — khi có invoice-data | `.../Ecommerce/DonHangService.cs` (~2106, `CapNhatHoaDonDonHangEcommerce`) | `KhachHangYeuCau = IsRequested ?? false`; `IsYeuCauXuatHoaDon = IsRequested ?? false` + MarkDirty (null=sàn không báo=false, ecom thắng) |
| Import | `OrderLogistic.Service/Generated/DonHang/DonHangImportService.cs` (~1766) | load config 1 lần trước loop |
| Xóa TT thuế → set cờ false | `DonHangInvoiceService.cs` (~64) | 🆕 Xóa `DonHangThongTinThue` → `SyncYeuCauXuatHoaDonToDon(id, false, ...)` (đơn không còn yêu cầu) |

## 3. ⭐ Quyết định người mua — `Fill_InvoiceBuyerInfo_Into_InvoiceDonHang`
File `OrderLogistic.Service/Generated/DonHangInvoiceService.cs` (~1495). Trước hết tính biến `coYeuCauXuatHoaDon` (KHÁC với cờ đơn cho ecom), rồi chọn `cachLayThongTinKhachHang` (0/1/2/3/4):

```csharp
// (~1509) ECOM: tín hiệu THẬT của SÀN = dataThongTinThue.KhachHangYeuCau; đơn thường: cờ đơn.
bool? coYeuCauXuatHoaDon = isDonHangEcommerce
    ? (dataThongTinThue?.KhachHangYeuCau ?? donHang.IsYeuCauXuatHoaDon ?? false) // ecom null=false
    : donHang.IsYeuCauXuatHoaDon;                                                // đơn thường: null=true (đơn cũ)

if (coYeuCauXuatHoaDon != false) {                                    // null hoặc true = CÓ yêu cầu
    if (có TT thuế hợp lệ)                                     cachLay = 1;
    else if (DuThongTinBatBuocTheoDon(donHang, infoDefault, listInfoKhachHang)) cachLay = 2;  // đủ field theo ĐƠN+KHÁCH
    else if (XuLyKhiThieuThongTin == "BaoLoi")                throw;         // Config B: báo lỗi
    else if (XuLyKhiThieuThongTin == "MotPhanMacDinh")        cachLay = 4;   // 🆕 Config B: 1 phần KH mặc định
    else                                                      cachLay = 0;   // Config B: mặc định (không throw)
}
else { // false — không yêu cầu
    bool ecomLayTheoDon = infoDefault?.EcomLayTheoThongTinDon ?? false;      // default FALSE
    cachLay = (isDonHangEcommerce && ecomLayTheoDon) ? 3 : 0;
}
```

> 🆕 **Ecom KHÔNG tin cờ đơn nữa** — dùng `dataThongTinThue.KhachHangYeuCau` (tín hiệu sàn). Cờ đơn ecom null + KhachHangYeuCau null → **false** (KHÔNG coi null=true), nhờ đó đơn ecom-không-yêu-cầu mới rơi đúng nhánh config "ecom → theo đơn / KH mặc định". Đơn THƯỜNG vẫn null=true (đơn cũ).
> **Đơn RỖNG:** `DuThongTinBatBuocTheoDon` check **CẢ đơn LẪN hồ sơ khách** (không chỉ đơn) → đơn trống field nhưng khách có → vẫn case 2 (không rơi mặc định oan). Thiếu THẬT (cả 2 rỗng) → Config B.

**Helper:**
- `DuThongTinBatBuocTheoDon(donHang, infoDefault, listInfoKhachHang)` (~1683): default required `[TenNguoiMua, DiaChi]`. 🆕 Mỗi field check **đơn HOẶC hồ sơ khách** (`khach = listInfoKhachHang.FirstOrDefault(KhachHangId)`): `TenNguoiMua`=đơn.KhachHangTen∥khach.Name · `DiaChi`=đơn.diaChi∥khach.Address · `SoDienThoai`=đơn.KhachSoDienThoai · 🆕 `MaSoThue`=khach.MaSoThue · 🆕 `Email`=khach.Email. **Địa chỉ rỗng "rác"** (`", , ,"`/`"--, , ,"`) bị bỏ dấu phẩy/space/gạch/chấm → rỗng = coi như KHÔNG có.
- 🆕 `FillBuyerMotPhanMacDinh(invoice, donHang, listInfoKhachHang, infoDefault)` (~1631): case 4 — base = đơn→khách (merge, KHÔNG throw); rồi GHI ĐÈ field trong `FieldsLayTuMacDinh` bằng KH mặc định (`TenNguoiMua`→Name · `DiaChi`→Address · `MaKhachHang`→buyerCode). Cuối cùng nếu Tên/Địa chỉ VẪN rỗng → throw (đoạn cuối, đã tới KH mặc định vẫn thiếu).
- `FillEcomBuyerInfoTheoDon(buyer, donHang)` (~1665): case 3 — lấy HẾT theo đơn (Tên/Địa chỉ/SĐT). *(method cũ `FillEcomFieldMapBuyerInfo` field-map đã comment.)*
- `GetInfoCustomer` (~2065): case 2 — tên/địa chỉ từ ĐƠN, **fallback record KH** (theo KhachHangId). Throw khi CẢ đơn LẪN record KH rỗng tên/địa chỉ. 🆕 Địa chỉ rỗng dùng cùng chuẩn "loại rác" (bỏ phẩy/space/gạch/chấm).

**Mapping key ↔ field đơn:** `TenNguoiMua`↔`KhachHangTen` · `DiaChi`↔`GiaoHangDiaChiFullText` · `SoDienThoai`↔`KhachSoDienThoai` · `MaSoThue`/`Email`↔chỉ có ở **hồ sơ khách** (đơn không có field).

## 4. Config service (Invoice)
`Invoice/Invoice.Service/Generated/CauHinhService.cs` `GetCustomerInfoDefault` (~300): populate `EcomFieldsLayTheoDon`, `RequiredFieldsLayTheoDon`, `XuLyKhiThieuThongTin` vào `CustomerInfoInvoice` (`Libs/Models.Core/DTO/Invocie.Client/CommonDto.cs`).

## 5. Auto-publish/draft — ⚠️ KHÔNG còn gate theo cờ
`OrderLogistic.Service/Generated/Common/CommonService.cs` (auto-draft ~3842, auto-publish ~3874): **gate cũ đã GỠ BỎ** (commit `61cce5fb27`).
- 🆕 **Cờ `IsYeuCauXuatHoaDon` KHÔNG chặn xuất** — mọi đơn (kể cả cờ=false) vẫn tự tạo nháp / vào hàng đợi auto-publish. Cờ **chỉ quyết NGUỒN TT người mua** (khách cụ thể vs KH mặc định) ở `Fill_InvoiceBuyerInfo`.
- Trước đây có `if (IsYeuCauXuatHoaDon == false && UnitCode != Ecommerce) continue;` → đã xóa, cùng `using Models.Core.Enum.EnumCommon;` thừa.
- SP `PRC_..._Get_DonHang_To_Publish_Invoice` không đọc cờ → không đụng.

## 5b. 🆕 T800/B318 — cột `TMDTKhachHangYeuCau` + shop sửa tay được (product 29/07/2026)

> Đọc kèm `convention/dong-bo-ngoai-vs-sua-tay.md` (pattern chung: nguồn ngoài đồng bộ + user sửa tay).

| Thứ | Chỗ | Ghi chú |
|---|---|---|
| Cột `DonHangThongTinThue.TMDTKhachHangYeuCau` (bool?) | `OrderLogistic.DataContext/Entity/DonHangThongTinThue.cs` | Tín hiệu GỐC của sàn (`is_requested`). **CHỈ webhook ecom ghi** (+1 ngoại lệ: chốt vết, xem dưới). ALTER + BACKFILL chạy tay, script KHÔNG commit vào repo |
| Webhook `CapNhatHoaDonDonHangEcommerce` | `.../Ecommerce/DonHangService.cs` (~2109) | `daSuaTay = (TMDT... != null && KhachHangYeuCau != TMDT...)` **tính TRƯỚC mọi lệnh ghi**; luôn ghi `TMDT...`; chỉ đè `KhachHangYeuCau` khi `!daSuaTay`; cờ ĐƠN đi theo cờ **hiệu lực** |
| `ChanBoYeuCauCuaSan` | `DonHangInvoiceService.cs` | ecom + `TMDT... == true` + payload `false` → trả `null` = **không đổi cờ** (TT thuế vẫn lưu). Chốt chặn API, không lách được |
| `ChotVetSanTruocKhiSuaTay` | `DonHangInvoiceService.cs` | Record chưa có vết sàn (user tạo trước khi sàn gửi / dữ liệu cũ) → lần đầu shop đổi toggle thì ghi `TMDT...` = mốc sàn đã biết (nhánh UPDATE: `KhachHangYeuCau` hiện tại; nhánh INSERT: cờ đơn) ⇒ webhook sau thấy lệch, không đè |
| `SyncYeuCauXuatHoaDonToDon` | `DonHangInvoiceService.cs` | **Bỏ guard chặn ecom** (shop sửa được cờ đơn sàn). Riêng luồng **XÓA TT thuế** truyền `apDungChoDonEcom: false` → không lật cờ đơn sàn |
| INSERT record TT thuế khi user KHÔNG đụng toggle | `DonHangInvoiceService.cs` | Đơn ecom = **cờ sàn trên đơn** (không tự bật true) · đơn thường = `true` (giữ production). Áp CHUNG cả 2 nhánh (khai TT thuế / chỉ email) |
| FE | `bo-sung-thongtin-cty` `.ts` + `.html` | `[nzDisabled]="khoaYeuCauTheoSan"` (chỉ khoá khi `isYeuCauXuatHoaDonTMDT === true`) + chú thích trạng thái cờ sàn, mỗi trạng thái **1 key đa ngôn ngữ trọn câu** |

⚠️ **Còn hở (biết, chưa xử):** (a) **Xóa thông tin thuế → nhập lại → bỏ tick** thì lách được luật khoá (xóa xong không còn record để đối chiếu); (b) lưu TT thuế lọc `IdChiNhanh` còn lúc xuất chỉ lọc `IdDonVi` → user khác chi nhánh sinh record thứ 2, cũng lách được; (c) webhook vẫn **ghi đè nội dung** MST/tên mỗi lần sàn gửi (thiết kế chỉ bảo vệ *cờ*, không bảo vệ *dữ liệu*); (d) `LoaiHoaDon` sàn gửi không validate → giá trị lạ có thể throw lúc xuất.

## 6. Sync + Toggle (popup Thông tin thuế)
`DonHangInvoiceService.cs`:
- `LayThongTinThue` (~68): trả `IsYeuCauXuatHoaDon` + `IsDonHangEcommerce`.
- `CapNhatThongTinThue` (~106): helper `SyncYeuCauXuatHoaDonToDon(idDonHang, yeuCau (bool?), userLogin, donDaLoad)` — set cờ + MarkDirty + UpdateAsync + ESIndex.
  - 🔴 **Chống regression — thiết kế ĐÃ ĐỔI (quan trọng):** guard KHÔNG còn `IsYeuCauXuatHoaDon != null`. Thay vào đó:
    - Param `yeuCau` giờ là **`bool?`** — `if (yeuCau == null) return;` (~294): user KHÔNG chủ động đổi toggle → **KHÔNG sync** (giữ nguyên cờ đơn, kể cả đơn cũ null → không lật).
    - Guard còn lại (~296): `don != null && UnitCode != Ecommerce && IsYeuCauXuatHoaDon != yeuCau`. Có giá trị (không null) → sync kể cả đơn đang null.
    - Caller giờ truyền `payload.IsYeuCauXuatHoaDon` **TRỰC TIẾP (không `?? true`)** → null propagate xuống → no-sync. (Trước đây `?? true` + guard `!= null` chặn; nay chuyển sang null=no-op ở helper.)
  - Guard chặn ecom (`UnitCode != Ecommerce`) vẫn giữ — cờ ecom do sàn.
- DTO `CapNhatThongTinThueDto` (CommonDto.cs): `IsYeuCauXuatHoaDon` (bool?) + `IsDonHangEcommerce` (bool).
- FE: `bo-sung-thongtin-cty` toggle (disable ecom, `showYeuCauToggle` chỉ logistic).

## 7. Hiển thị cờ trên list (Có/Không/—)
List có **2 nhánh** gate bởi config `Allows_Return_OrderLogistic_Results_ElasticSearch_From_SQL`: `if(!allow)`→**ES full projection** · `else`→**SP**.

| Màn | Service (BE) | Chỗ ES đã select | SP (nếu bật SQL-mode) |
|---|---|---|---|
| Kế toán | `KeToanTacNghiepService.cs` | ~2105 | — |
| Thu kho | `KhoTacNghiepService.cs` | ~2457, ~3358, join ~3412 | `PRC_..._ThuKhoTacNghiep_GetListByIds` (chưa thêm cột) |
| Sale | `DonHangService.cs` DB projection ~3824; `DonHangStoreService.cs` ES ~2416, ~3699 | | `PRC_..._DonHangSale_GetListByIds` (chưa thêm cột) |

- DTO: `DonHangLogisticFilterResponse` (`DonHangLogisticFilterDto.cs` ~172), `ThuKhoTacNghiepFilterResponse` (`ThuKhoTacNghiepFilterDto.cs` ~342).
- ES-consumer map entity→ES model bằng **AutoMapper by-name** (`MappingProfile.cs`) → field tự map, không cần sửa consumer.
- FE cột `dataType: 'isYeuCauXuatHoaDon'` + cell `*ngSwitchCase` render true→Có / false→Không / null→—. Component: `ke-toan-tac-nghiep`, `thu-kho-tac-nghiep`, `don-hang-sale`.
- **Website KHÔNG làm**: entity/service riêng (`WebsiteDonHang`, Website.Service), cờ ở đơn logistic, link qua `WebsiteDonHang.IdDonHang` (chỉ có sau convert) → cần cross-service, giá trị thấp (đa số "—").

## 8. FE config waterfall
`caidat-hethong/configsystem/config-invoice` (.ts ~86-91):
- `requiredFieldsLayTheoDon` (multi-select, **default `['TenNguoiMua', 'DiaChi']`**). Options 🆕 = `TenNguoiMua`, **`DiaChi`** (giờ CHỌN ĐƯỢC, không còn ẩn), `SoDienThoai`, 🆕 `MaSoThue`.
- `xuLyKhiThieuThongTin` (radio, default `MacDinh`): 🆕 **3 lựa chọn** `MacDinh` / `BaoLoi` / `MotPhanMacDinh`.
- 🆕 `fieldsLayTuMacDinh` (multi-select, default `[]`): chỉ dùng khi Config B=`MotPhanMacDinh` — field lấy từ KH mặc định (`TenNguoiMua`/`DiaChi`/`MaKhachHang`).
- `ecomLayTheoThongTinDon` (radio, **default `false`** — khớp BE).
- Phí ship theo loại đơn: config `loaiDonKhongXuatPhiShip` (multi-select UnitCode) ở màn cấu hình order.

## 9. Viettel buyerCode (`AccountObjectCode`)
`Invoice/Invoice.Service/Generated/ViettelInvoiceService.cs` (~1615, dựng `req.buyerInfo`):
- 🆕 `req.buyerInfo.buyerCode = DonHang.BuyerInfo?.buyerCode;` — map mã KH người mua (áp NGANG MISA `AccountObjectCode`). Nguồn từ OrderLogistic: mặc định KhachHangMa; Config B "1 phần mặc định" chọn `MaKhachHang` → `MaKhachHangMacDinh`.
- 🆕 (~1646) nếu `cauHinh.IsSuDungMaKhachHangMacDinh == true && MaKhachHangMacDinh` có → **override** `req.buyerInfo.buyerCode = cauHinh.MaKhachHangMacDinh` (ngang MISA `MeInvoiceService:1866`).

## 10. Phí ship theo loại đơn
`DonHangInvoiceService.cs` (publish payload, ~1230): `data.IsKhongXuatHDDT_PhiGiaoHang = IsKhongXuatHDDT_PhiGiaoHang || (loaiDonKhongXuatPhiShip?.Contains(donHang.UnitCode) == true);`
- 🆕 Không xuất phí ship nếu **công tắc global BẬT** HOẶC **loại đơn (UnitCode) nằm blocklist** `LoaiDonKhongXuatPhiShip` (OR).

## Việc DB/OPS khi deploy
1. SQL `ALTER TABLE DonHangLogistic ADD IsYeuCauXuatHoaDon BIT NULL` (nullable, không backfill).
2. **ES re-index** đơn cũ (để cột hiện đúng, không "—").
3. (Tuỳ chọn) thêm cột `IsYeuCauXuatHoaDon` vào 2 SP `GetListByIds` NẾU bật SQL-fallback mode.
