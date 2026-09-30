---
controller: WarehouseProductUpdateController
module: kho
service: WarehouseProduct
man_hinh_ui: "nội bộ — không có UI (service-to-service). Trigger gián tiếp từ thao tác Đơn hàng/Logistic: thanh toán, đăng/hủy đơn, chốt đơn, đối soát hoàn."
muc_dich: "Cập nhật tồn kho + giá vốn khi xử lý đơn hàng: trừ/cộng SoLuong & SoLuongChoXuat trên KhoChiTiet, tính ThanhTien theo GiaNhapTrungBinh, sinh PhieuXuat/PhieuNhap, trừ/hoàn thẻ dịch vụ + combo, gán/gỡ IMEI, rollback & đối soát đơn hoàn."
alias: [cap nhat kho khi thanh toan, tru ton khi ban, cong ton khi huy, dang don, huy don, chot don, doi soat don hoan, tru the dich vu online, warehouse update, WarehouseProductUpdate]
auth: mixed   # đa số [PrivateSBAuthorize] (service-to-service); 1 endpoint [AllowAnonymous] (TMDT_WarehouseLogisticUpdate)
state_machine: []
deprecated: false
entities: [KhoChiTiet, PhieuXuat, PhieuXuatSanPham, PhieuNhap, PhieuNhapSanPham, SanPhamImei, TheDichVuKhachHang, TheDichVuKhachHangLichSuGiaoDich, ComBoSpa, ComBoSpaKhachHang, SanPhamLoDate]
stored_procs: []   # không đi store; trừ/cộng tồn bằng SQL UPDATE tương đối sinh động (StringHelpers.GenStringSql...SoLuongKho)
depends: [CommonService, DongBoKeToanService, PhieuXuatService, SanPhamImeiService, CauHinhService, ElasticSearchService, PartnerConnect/Bravo, MongoDB, Redis]
cross_module_in: [Order, OrderLogistic, OrderProcess, ClearData]
cross_module_out: [Accounting, ElasticSearch, Ecommerce]
---

# WarehouseProductUpdate — Cập nhật tồn kho & giá vốn khi xử lý đơn hàng

> Controller **GHI tồn** (đối lập với `KhoChiTietController` chỉ-đọc). KHÔNG có UI: tất cả endpoint là service-to-service (`[PrivateSBAuthorize]`), trừ `TMDT_WarehouseLogisticUpdate` `[AllowAnonymous]`. Caller chính: **Order** (thanh toán), **OrderLogistic** (đăng/hủy/chốt đơn vận, trừ thẻ online, đối soát hoàn), **OrderProcess/ClearData** (rollback/bù trừ khi lỗi). Logic nặng nằm ở `WarehouseProductUpdateService` (9311 dòng). Trừ/cộng tồn ưu tiên **SQL UPDATE tương đối** (clamp `>=0`) thay vì set tuyệt đối → tránh lost-update khi nhiều đơn cùng chạm 1 `KhoChiTiet`.

## Hành động (UI / công khai)

> Không có hành động UI. Toàn bộ là endpoint nội bộ — xem mục "Endpoint nội bộ" bên dưới.

## Endpoint nội bộ (private / service-to-service)

**Nhóm A — Thanh toán đơn (Order):**
- [internal] Order `CommonService` gọi `POST ValidateThanhToanDonHang` trước, rồi `POST ThanhToanDonHang` để chốt thanh toán → trừ tồn thật + tính giá vốn + sinh phiếu xuất + tạo/trừ thẻ & combo. (Order/.../Common/CommonService.cs:381, :813)
- [internal] OrderProcess/ClearData `OrderRollBackService` gọi `POST RollBackThanhToanDonHang` (và lại `ThanhToanDonHang`) khi cần **bù trừ/rollback** đơn lỗi. (OrderProcess/.../OrderRollBackService.cs:243,:1280; ClearData/.../OrderRollBackService.cs:160,:959)

| # | Endpoint | Caller | Auth | Hành vi đúng (expected) | Nguồn | Known-issue |
|---|---|---|---|---|---|---|
| 1 | `POST api/WarehouseProductUpdate/ValidateThanhToanDonHang` | Order | internal | Nếu `CauHinh.ChoPhepTonKhoAm=false`: gộp SP mua trực tiếp + SP từ dịch vụ (quy đổi qua `DichVuSanPham.DinhLuong`), check từng SP `KhoChiTiet.SoLuong>=req` **và** `(SoLuong-SoLuongChoXuat)>=req`; thiếu → trả lỗi. Chỉ validate, **không ghi**. | EF | Dùng `(SoLuong-SoLuongChoXuat)` (trừ phần đã giữ chờ xuất) — invariant. Svc L3171-3297. |
| 2 | `POST api/WarehouseProductUpdate/ThanhToanDonHang` | Order, OrderProcess, ClearData | internal | Trả `Guid`. (a) Tạo/cập nhật `TheDichVuKhachHang` + `ComBoSpaKhachHang` (log MongoDB). (b) Gộp tồn (SP/dịch vụ/LoDate). (c) Check tồn nếu `ChoPhepTonKhoAm=false` (local hoặc Bravo API). (d) Sinh `PhieuXuat` (XuatThuNgan, MaTinhTrang=HoanThanh) + `PhieuXuat` riêng cho dịch vụ (XuatSuDung); `GiaVon=KhoChiTiet.GiaNhapTrungBinh`, `ThanhTien=SoLuong*GiaNhapTrungBinh`. (e) Trừ `KhoChiTiet.SoLuong` qua SQL. (f) IMEI→`DaXuatKho`, lưu `IdPhieuNhapCu`. (g) Hoàn thành `PhieuNhap` nếu có `IdPhieuMuaHang`. (h) Trừ combo + LoDate. Tất cả trong 1 transaction. | EF + SQL UPDATE | Giá vốn = `GiaNhapTrungBinh` (KHÔNG dùng giá bán) — invariant kế toán. LoDate hard-code `TkNo="6418"/TkCo="1561"`. Svc L951-2507. |
| 3 | `POST api/WarehouseProductUpdate/RollBackThanhToanDonHang` | OrderProcess, ClearData | internal | Đảo #2: hoàn thẻ (`SoTienConLai/SoLanConLai` từ lịch sử giao dịch) + hoàn combo + sinh `PhieuNhap` (PhanLoaiNhap=HuyChoXuatHang) cộng lại `SoLuong`, hoàn IMEI về `DaNhapKho`, rollback PhieuMuaHang/combo/LoDate. | EF + SQL UPDATE | Lookup phiếu theo `IdDonHang` — rủi ro nếu trùng đơn. Svc L2508-3170. |

**Nhóm B — Đơn vận / chốt đơn (OrderLogistic):** chạm `SoLuongChoXuat` (giữ chỗ chờ xuất), KHÔNG trừ tồn vật lý.
- [internal] OrderLogistic `CommonService` điều phối nguyên cụm này. (OrderLogistic/.../Common/CommonService.cs)

| # | Endpoint | Caller | Auth | Hành vi đúng (expected) | Nguồn | Known-issue |
|---|---|---|---|---|---|---|
| 4 | `POST .../WarehouseLogisticUpdate` | OrderLogistic, OrderProcess | internal | Chốt đơn vận: chọn kho (theo `IsKhoTong`/kho tổng), check tồn (local hoặc Bravo), **chỉ tăng `SoLuongChoXuat`** (`SoLuong` không đổi). Sinh `PhieuXuat` (ChuyenChoXuatHang) `SoLuong=0`. IMEI→`DaXuatKho`. Trừ combo. | SQL UPDATE | **KHÔNG sync kế toán** (chủ ý — phiếu chốt SL=0). Negative cho phép nếu `kho.IsChoPhepChotAm=true`. Svc L3298-4019. |
| 5 | `POST .../WarehouseLogisticUpdateWhenSaveDonHangSauKhiQuetImeiSP` | Order, OrderLogistic | internal | Khi lưu đơn sau quét IMEI: IMEI mới→`DaXuatKho` (set IdPhieu, lưu IdPhieuNhapCu); IMEI bỏ→trả `DaNhapKho`. Không đụng `KhoChiTiet`. | EF | Svc L4020-4138. |
| 6 | `POST .../WarehouseRemoveImeiWhenSaveDonHang` | OrderLogistic | internal | Gỡ IMEI khỏi đơn chưa chốt: `DaXuatKho`→`DaNhapKho`, khôi phục IdPhieu cũ. Không đụng tồn. | EF | Svc L4139-4224. |
| 7 | `POST .../RollBackDonHangLogistic` | OrderLogistic, OrderProcess | internal | Đảo #4: `RollbackKhoLogistic` sinh `PhieuNhap` (HuyChoXuatHang), **trừ lại `SoLuongChoXuat`**, IMEI về `DaNhapKho`, rollback combo. Sync lịch sử kho + ES. | SQL UPDATE | Svc L4574-4938. |
| 8 | `POST .../CapNhatDonHangLogisticSauChotDon` | OrderLogistic | internal | Sửa đơn sau chốt: **đổi kho** → 2 phiếu xuất (gỡ kho cũ + thêm kho mới), check kho mới `(SoLuong-SoLuongChoXuat)>=req`; nếu `kho.IsKhongCongLaiSoLuongKhiChuyenKho=true` → bỏ qua chuyển tồn vật lý. **Cùng kho** → 1 phiếu, cập nhật chờ xuất + làm lại combo. | SQL UPDATE | Sync kế toán có điều kiện. Svc L7190-8109. |
| 9 | `POST .../DangHuyDon` | OrderLogistic | internal | (V1) Type=1 đăng đơn: trừ `SoLuong`+`SoLuongChoXuat`, sinh PhieuXuatGiaoVan. Type=2 hủy: cộng lại + sinh `PhieuNhap` (HuyGiaoVan). | EF (set tuyệt đối) | V1 ghi tuyệt đối (MarkDirty) → **rủi ro race**; không guard âm. Ưu tiên dùng V2. Svc L4939-5257. |
| 10 | `POST .../DangHuyDon_V2` | OrderLogistic | internal | Như #9 nhưng **Redis lock** theo đơn + **idempotent 2 chiều** (đăng chỉ trừ khi chưa có phiếu xuất giao vận; hủy chỉ cộng khi đã có) + **SQL tương đối clamp>=0** + xử lý IMEI + sync kế toán phiếu xuất/nhập. | SQL UPDATE | Bản an toàn, nên dùng thay #9. Svc L5258-6006. |
| 11 | `POST .../DangHuyDonKhongTaoPhieu` | OrderLogistic | internal | Như đăng/hủy nhưng **không tạo phiếu**: chỉ trừ/cộng `SoLuongChoXuat` qua SQL `...ChoXuatKhoLogistic` (không entry kế toán). | SQL UPDATE | Không guard, giả định state validate ở upstream. Svc L7073-7189. |
| 12 | `POST .../CapNhatImei` | OrderLogistic | internal | Cập nhật 1 IMEI: chỉ chấp nhận khi đang `DaNhapKho` → set `DaXuatKho`; log Mongo. | EF | Svc L8110-8152. |

**Nhóm C — Thẻ dịch vụ / đối soát (OrderLogistic):**

| # | Endpoint | Caller | Auth | Hành vi đúng (expected) | Nguồn | Known-issue |
|---|---|---|---|---|---|---|
| 13 | `POST .../TruTienTheKhiXacNhanThanhToanOnline` | OrderLogistic | internal | Trừ thẻ dịch vụ khi xác nhận thanh toán online: thẻ tiền → trừ `SoTienConLai` (+ `SoTienKhuyenMaiConLai`); thẻ lần → trừ `SoLanConLai`. Trừ combo (`MapComboTruKho`). Ghi `TheDichVuKhachHangLichSuGiaoDich` + log tương tác KH. Không đụng tồn. | EF | **Không guard số dư âm** trên `SoTienConLai` (giả định validate upstream). Svc L8153-8650. |
| 14 | `POST .../DoiSoatDonHangDaHoan` | OrderLogistic | internal | Đối soát đơn hoàn: `TrangThaiDoiSoat=true` → `PhieuNhap` (NhapTraLaiTuKhach), **cộng** `SoLuong` (giá = `GiaNhapTrungBinh` chứ KHÔNG phải `DonGia`), IMEI→`DaNhapKho`, hoàn LoDate. `false` → `PhieuXuat` (XuatKhac), **trừ** `SoLuong` (guard `SoLuong-qty>0` trừ khi `IsBoQuaKiemTraConHangKhiDangDon`). | SQL UPDATE | Giá vốn = `GiaNhapTrungBinh` (invariant). Svc L8651-9311. |

**Nhóm D — Ecommerce (TMDT):**

| # | Endpoint | Caller | Auth | Hành vi đúng (expected) | Nguồn | Known-issue |
|---|---|---|---|---|---|---|
| 15 | `POST .../TMDT_WarehouseLogisticUpdate` | OrderLogistic / webhook TMDT | **anonymous** | Chốt kho đơn sàn (TikTok/Shopee): check tồn → sinh `PhieuXuat`+`PhieuXuatSanPham`, **trừ `KhoChiTiet.SoLuong` qua SQL tương đối** (clamp>=0). Sync lịch sử kho + kế toán + ES. | SQL UPDATE | **`[AllowAnonymous]`** — bề mặt tấn công (webhook không auth, không UserLogin). Ctrl L72-77. Svc L4225-4573. |
| 16 | `POST .../TMDTWarehouseProductUpdate` | OrderLogistic / TMDT | internal | Luồng cập nhật/undo chốt sàn riêng: idempotent (kiểm tra phiếu xuất giao vận đã tồn tại → trả success, tránh trừ kép). PhieuXuat/PhieuXuatSanPham + SQL update + sync kế toán/ES. | SQL UPDATE | Svc L6007-7072. |

## Quy tắc nghiệp vụ (gating / ngưỡng)

1. **Cho phép tồn âm khi thanh toán:** `CauHinh.ChoPhepTonKhoAm` → `true`: bỏ check tồn (#1/#2) | `false`: bắt buộc `SoLuong>=req` **và** `(SoLuong-SoLuongChoXuat)>=req`.
2. **Cho phép chốt âm (đơn vận):** `kho.IsChoPhepChotAm` → điều kiện guard ở #4.
3. **Check tồn qua API đối tác (Bravo):** kho Bravo + `IsKiemTraTonKhoQuaApi` → gọi PartnerConnect `GetInventory` thay vì SUM `KhoChiTiet` local (#1/#2/#4).
4. **Chuyển kho không cộng lại SL:** `kho.IsKhongCongLaiSoLuongKhiChuyenKho=true` (#8) → bỏ qua chuyển tồn vật lý, chỉ đổi giữ chỗ.
5. **Trừ đối soát hoàn:** guard `SoLuong-qty>0` ở #14, bỏ guard nếu `IsBoQuaKiemTraConHangKhiDangDon`.
6. **Giá vốn (invariant xuyên suốt):** mọi phiếu xuất/nhập tính `GiaVon/ThanhTien` theo `KhoChiTiet.GiaNhapTrungBinh` (chi phí), KHÔNG dùng giá bán `DonGia`.

## Quan hệ
- **Gọi sang:** `PhieuXuatService` (GenKhoChiTiet... — sinh SQL update tồn), `CommonService.SyncKhoChiTietLichSu` (queue lịch sử kho), `DongBoKeToanService.DongBoPhieuXuat/DongBoPhieuNhap` (đồng bộ chứng từ sang Accounting/MISA), `SanPhamImeiService.PushLogsQueue` (log IMEI), `CauHinhService` (ChoPhepTonKhoAm / Bravo / giới hạn), `ElasticSearchService.ESIndexKhoChiTiet` (reindex sau commit, batch 50), PartnerConnect/Bravo `GetInventory`, MongoDB (SanPhamImeiMongo, lịch sử thẻ/combo, tương tác KH), Redis (lock đơn ở `DangHuyDon_V2`).
- **Dữ liệu (entities):** ghi `KhoChiTiet` (SoLuong, SoLuongChoXuat, GiaNhapTrungBinh), `PhieuXuat`/`PhieuXuatSanPham`, `PhieuNhap`/`PhieuNhapSanPham`, `SanPhamImei` (TrangThai, IdPhieu, IdPhieuNhapCu), `TheDichVuKhachHang`(+LichSuGiaoDich), `ComBoSpa`/`ComBoSpaKhachHang`, `SanPhamLoDate`.
- **Ripple / đồng bộ:** Accounting (đồng bộ phiếu — trừ luồng chốt đơn vận #4/#11 cố ý không sync), ElasticSearch (reindex KhoChiTiet), Ecommerce (số lượng tồn đẩy sang sàn qua job ở service khác, không trực tiếp endpoint này).

## Luồng xuyên module
- **Thanh toán đơn (Order → kho):** Order `Validate` → `ThanhToanDonHang` → trừ `KhoChiTiet.SoLuong` + sinh PhieuXuat (GiaVon=GiaNhapTrungBinh) + trừ thẻ/combo/IMEI + LoDate → đồng bộ PhieuXuat sang **Accounting**, reindex **ES**. Lỗi → OrderProcess/ClearData gọi `RollBackThanhToanDonHang` bù trừ.
- **Đăng / chốt / hủy đơn vận (OrderLogistic → kho):** đăng đơn (#9/#10) giữ chỗ `SoLuongChoXuat`; chốt (#4) sinh PhieuXuat SL=0 (không sync kế toán); sửa sau chốt (#8) đổi kho; hủy (#7) hoàn chờ xuất. Idempotency + Redis lock ở `_V2` chống trừ/cộng kép khi retry message.
- **Đơn sàn TMDT (#15/#16):** webhook sàn → trừ `KhoChiTiet.SoLuong` (SQL tương đối) + PhieuXuat → sync kế toán + ES.
- **Đối soát đơn hoàn (#14):** đơn hoàn về kho → cộng/trừ `SoLuong` theo trạng thái đối soát, giá theo `GiaNhapTrungBinh`, hoàn/gỡ IMEI + LoDate.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `WarehouseProduct/WarehouseProduct.API/Controllers/WarehouseProductUpdateController.cs`
  - L72-77 `TMDT_WarehouseLogisticUpdate` = `[AllowAnonymous]` → endpoint công khai duy nhất, không UserLogin (bề mặt tấn công webhook sàn).
  - Còn lại `[PrivateSBAuthorize]` — không có UI/permission key, gọi service-to-service. `[SBAuthorize(...)]` ở L25 bị comment (không dùng permission UI).
  - L150-155 `XuatImeiKhiLuuDonHangLogistic` bị comment ở controller (caller OrderLogistic vẫn trỏ tới — endpoint không tồn tại runtime).
- BE `WarehouseProduct/WarehouseProduct.Service/Generated/WarehouseProductUpdateService.cs`
  - L951-2507 `ThanhToanDonHang`: giá vốn lấy `GiaNhapTrungBinh`; LoDate hard-code `TkNo="6418"/TkCo="1561"` (khác cấu hình các phiếu khác).
  - L4939-5257 `DangHuyDon` (V1): ghi tồn **tuyệt đối** (MarkDirty) → race khi nhiều đơn cùng SP; **không guard âm**. Ưu tiên `DangHuyDon_V2` (L5258-6006: Redis lock + idempotent 2 chiều + SQL tương đối clamp>=0).
  - Trừ/cộng tồn nóng dùng `StringHelpers.GenStringSql{Tru,Cong}SoLuongKho` / `...ChoXuatKhoLogistic` (UPDATE tương đối, clamp `>=0`) — invariant chống lost-update; sửa filter phải đồng bộ cả nhánh đăng & hủy.
  - L3298-4019 `WarehouseLogisticUpdate` cố ý **không** `DongBoPhieuXuat` (phiếu chốt SL=0) — đừng "sửa nhầm" thành sync.
  - L8651-9311 `DoiSoatDonHangDaHoanAsync` & L951+ `ThanhToanDonHang`: cùng invariant **GiaNhapTrungBinh ≠ DonGia** cho hạch toán chi phí.
  - L8153-8650 `TruTienTheKhiXacNhanThanhToanOnline`: không guard số dư thẻ âm (validate ở upstream).
