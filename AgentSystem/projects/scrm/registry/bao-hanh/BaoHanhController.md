---
# Snapshot @ BE origin/release_product e617abcbbf (quét 2026-07-15) — kho là BẢN ĐỒ, code là chân lý.
# Module Warranty MỚI TRỌN VẸN trong delta (72ac442a..e617abcbbf, 26 commit). VERIFY lại path:line trước khi sửa.
controller: BaoHanhController
module: bao-hanh
service: Warranty
man_hinh_ui: "Bảo hành > Danh sách phiếu bảo hành: list/tìm kiếm phiếu BH, thêm/sửa thủ công, xoá, import Excel (preview → hoàn tất), tải template, xuất file lỗi import. (Vị trí menu chính xác chưa verify — controller là hub CRUD phiếu BH.)"
muc_dich: "CRUD + tìm kiếm + import Excel phiếu bảo hành (entity BaoHanh). Mỗi sản phẩm bán ra (có IMEI/Serial) = 1 phiếu theo dõi vòng đời BH. Phiếu tạo bằng 2 đường: (a) thủ công qua Save/Import ở controller này, (b) TỰ ĐỘNG từ đơn thanh toán qua RabbitMQ consumer trong cùng service."
alias: ["bảo hành", "bao hanh", "phiếu bảo hành", "warranty", "quản lý bảo hành", "BH sản phẩm", "imei bảo hành"]
auth: public
state_machine: [ChuaBaoHanh, DaTiepNhan, DangXuLy, HoanThanh]
deprecated: false
entities: [BaoHanh, BaoHanhLanXuLy, BaoHanhLanXuLyPhuKien, CauHinh, DMBaoHanhMoRong, DMLoaiBaoHanh]
stored_procs: []
depends: [BaoHanhService, WarehouseProductService (cross-service lookup SP/Kho), CauHinh (JsonSetting per đơn vị), IMemoryCache, MaTienIchSCRM.TI_BaoHanh]
cross_module_in: [Order (publish TaoBaoHanhFromDonHangMessage khi đơn thanh toán thành công), Order (publish message huỷ khi đơn bị huỷ)]
cross_module_out: [WarehouseProduct (SanPham_GetListSanPhamByIds lookup thời hạn BH + Kho_GetBasicInfoByIds lookup tên kho), Zalo/SMS (cờ IsSendZalo/IsSendSMS quyết định gửi tin khi tạo/chuyển trạng thái)]
---

# BaoHanhController — Quản lý phiếu bảo hành sản phẩm

> Controller CRUD phiếu bảo hành, service **Warranty** (microservice mới, `Warranty.API` port riêng, DB `SCRM_Warranty`). `[TienIchAttribute("BaoHanhController", ..., MaTienIchSCRM.TI_BaoHanh)]` + `[Route("api/[controller]")]` + `[SBAuthorize]` controller-level; mỗi method có `[SBAuthorize(key, desc, BaoHanhRoles)]` riêng. **Role dùng chung** (`BaoHanhRoles`): `DV-AD-TONG, DV-AD-CN, DV-AD-MCN, DV-QLDV, DV-SALE, DV-NV, DV-KT, DV-KT-TONG, DV-QL-KHO, DV-QL-KHO-TONG, DV-TP-QL-KHO, DV-TN-QL-KHO` (`BaoHanhController.cs:24`). Gate tiện ích: mọi thao tác cần đơn vị bật tiện ích **Bảo hành** (`HasTienIchBaoHanhAsync`, `BaoHanhService.cs:732`).
>
> ⚠️ Phiếu BH KHÔNG chỉ tạo qua controller này — **đường tạo chính trong vận hành là AUTO từ đơn hàng** qua consumer `TaoBaoHanhFromDonHangConsumer` (cùng service). Xem mục "Endpoint message-driven" + "Luồng xuyên module".

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm phiếu BH theo điều kiện | Lưới danh sách + bộ lọc | POST api/BaoHanh/TimKiemTheoDieuKien | BaoHanh_TimKiemTheoDieuKien | public | mở màn BH | List phiếu BH theo filter (đơn vị/trạng thái/ưu tiên/khách/IMEI...). Snapshot khách+SP+kho lưu sẵn trong phiếu → list không cần call cross-service. | EF `BaoHanh` | — |
| 2 | Xem chi tiết 1 phiếu | Mở chi tiết | GET api/BaoHanh/GetById/{id} | BaoHanh_GetById | public | click 1 dòng | Trả chi tiết phiếu theo Id. | EF `BaoHanh` | — |
| 3 | Thêm/sửa phiếu thủ công | Nút Lưu | POST api/BaoHanh/Save | BaoHanh_Save | public | form thêm/sửa | `SaveAsync` — check tiện ích BH (`BaoHanhService.cs:164`), sinh mã nếu thêm mới, tính ngày hết hạn, set audit theo user login. | EF `BaoHanh` | — |
| 4 | Xoá phiếu (nhiều) | Nút Xoá | POST api/BaoHanh/Delete (body `List<Guid>`) | BaoHanh_Delete | public | chọn dòng | Soft-delete danh sách phiếu (`IsDelete=true`). | EF `BaoHanh` | — |
| 5 | Import Excel — preview | Chọn file → xem trước | POST api/BaoHanh/ImportPreview (IFormFile) | BaoHanh_Import | public | dialog import | Đọc file, validate từng dòng, trả danh sách row + lỗi để user xem trước (chưa ghi DB). | EF `BaoHanh` (read validate) | — |
| 6 | Import Excel — hoàn tất | Nút Xác nhận import | POST api/BaoHanh/HoanTatImport (body `List<BaoHanhImportRowDto>`) | BaoHanh_Import | public | sau preview hợp lệ | Ghi các dòng hợp lệ thành phiếu BH (batch sinh mã, `BaoHanhService.cs:496`). | EF `BaoHanh` | Dùng chung permission key `BaoHanh_Import` với ImportPreview. |
| 7 | Tải template import | Nút Tải mẫu | GET/POST api/BaoHanh/DownloadTemplate | BaoHanh_Import (chung nhóm import) | public | dialog import | Trả file Excel mẫu cột chuẩn để nhập. | file tĩnh | — |
| 8 | Xuất file lỗi import | Nút Tải file lỗi | POST api/BaoHanh/ExportFileErrorImport | BaoHanh_Import (chung nhóm import) | public | sau preview có lỗi | Xuất Excel các dòng lỗi + lý do để user sửa. | — | — |

> (Endpoint #7, #8 xác nhận tồn tại theo phân tích delta; ký hiệu HTTP verb/permission-key chi tiết chưa Read tận nơi — VERIFY `BaoHanhController.cs` phần cuối trước khi trace.)

## Endpoint message-driven (consumer — service-to-service, KHÔNG có HTTP/UI)

Consumer nằm trong `Warranty.API/Consumer/`, chạy **NGOÀI HTTP context** (không có user login → `EfRepository.GetUserLogin()` trả null → audit fields set MANUAL từ `NguoiTaoUserId` trong message):

- **[inbound] Order → `TaoBaoHanhFromDonHangConsumer`** (`TaoBaoHanhFromDonHangConsumer.cs:52`): nhận `TaoBaoHanhFromDonHangMessage` khi đơn thanh toán thành công. Mỗi SP có cấu hình BH (tổng số ngày > 0) → tạo 1 `BaoHanh`. SP không cấu hình BH → skip, không tạo phiếu rác (`:85`, `:105`).
- **[inbound] Order → `XoaBaoHanhFromDonHangConsumer`** (`XoaBaoHanhFromDonHangConsumer.cs`): xử lý xoá/huỷ phiếu BH khi đơn hàng bị huỷ.

## Quy tắc nghiệp vụ (gating / ngưỡng — blueprint cho trace-agent)

Xác định trên `TaoBaoHanhFromDonHangConsumer.cs` (đường auto-tạo — đường hay có bug nhất):

1. **Guard đầu vào** (`:55`, `:58`): message rỗng/không có Items → return; không có `NguoiTaoUserId` (hoặc `Guid.Empty`) → return (cần user hợp lệ để gọi Warehouse theo `IdDonVi`).
2. **Gate tiện ích BH** (`:61`): `HasTienIchBaoHanhAsync(NguoiTaoUserId)` = false → return. Đơn vị chưa bật tiện ích Bảo hành → KHÔNG tạo phiếu.
3. **Ngày kích hoạt** (`TinhNgayKichHoat`, `:267`):
   - Đơn **online** (`TypeDonHang=1`, `TypeDonHangBaoHanhEnum.Online`): LUÔN = `NgayThanhToan`. KHÔNG áp cấu hình "Mốc kích hoạt bảo hành" (mốc đó chỉ cho đơn thu ngân).
   - Đơn **offline/thu ngân** (`TypeDonHang=2`) + `MocKichHoatBaoHanh=2` (Tùy chỉnh) + `SoNgaySauKichHoat>0` → `NgayThanhToan.AddDays(SoNgaySauKichHoat)`.
   - Còn lại → mặc định = `NgayThanhToan`.
4. **Lookup thời hạn BH cross-service** (`LookupSoNgayBaoHanhAsync`, `:201`): gọi `WarehouseProduct.SanPham_GetListSanPhamByIds`. Quy đổi `ThoiHanSpTheoThang`/`ThoiHanSpTheoNgay` → số ngày qua `QuyDoiSoNgayBaoHanh` (`BaoHanhService.cs:726`): **`ngayKichHoat.AddMonths(thang).AddDays(ngay)` theo LỊCH THỰC** (KHÔNG cố định 1 tháng = 30 ngày). SP có tổng số ngày = 0 → không vào map → skip SP đó. Map rỗng (không SP nào có BH) → skip cả message (`:85`).
5. **Idempotent chống trùng** (`:107`–`:119`): key = `SanPhamMa` (+ `|Imei` nếu SP có IMEI) trong phạm vi `IdDonHang + IdDonVi + IsDelete=false`. Loại item đã có phiếu → chống tạo trùng khi **MQ retry** hoặc **đơn re-trigger trạng thái tăng nhiều lần**. SP có IMEI → mỗi IMEI 1 phiếu; SP không IMEI → 1 phiếu/SP/đơn (`BuildKeyTrungSp`, `:253`).
6. **Ngày hết hạn** (`TinhNgayHetHan`, `BaoHanhService.cs:715`): `ngayKichHoat.AddDays(SoNgaySanPham + SoNgayMoRong)` — cộng theo NGÀY. Ở đường consumer `soNgayMoRong=0` (`:135`).
7. **Snapshot cờ gửi tin** (`:73`, `:155`): `IsSendZalo`/`IsSendSMS` lấy từ `CauHinh` của đơn vị TẠI LÚC tạo phiếu (đóng băng vào phiếu, không đọc lại config về sau).
8. **Snapshot mặc định phiếu auto** (`:148`–`:181`): `ThuTuUuTien=BinhThuong(3)`, `PhanLoaiBaoHanh=BaoHanhTaiCuaHang(2)`, `TrangThaiXuLy=ChuaBaoHanh(1)`, `SanPhamDuocApDungBaoHanh=true` (KTV có thể bỏ tick khi tiếp nhận), `SanPhamIsMuaTaiCuaHang = (TypeDonHang==Offline)`.

## Quan hệ

- **Gọi sang:** `BaoHanhService` (SaveAsync/Import/SinhListMaBaoHanhAsync/HasTienIchBaoHanhAsync/TinhNgayHetHan/QuyDoiSoNgayBaoHanh); `WarehouseProductService` (cross-service HTTP: `SanPham_GetListSanPhamByIds` lookup thời hạn BH, `Kho_GetBasicInfoByIds` lookup tên kho); `CauHinh` per-đơn-vị (JsonSetting, cache `IMemoryCache` TTL 5' — key dùng CHUNG hằng `CauHinhService.CacheKeyCauHinhPrefix`, `TaoBaoHanhFromDonHangConsumer.cs:29`).
- **Dữ liệu (entities):** `BaoHanh` (bảng `SCRM_Warranty.dbo.BaoHanh`, `Warranty.DataContext/Entity/BaoHanh.cs`) — snapshot khách + SP + kho + cờ Zalo/SMS + IMEI/Serial + IMEI đổi mới. Liên quan: `BaoHanhLanXuLy` (lịch sử xử lý), `BaoHanhLanXuLyPhuKien` (phụ kiện thay thế), `DMBaoHanhMoRong`, `DMLoaiBaoHanh`, `CauHinh`.
- **Ripple / đồng bộ:**
  - **VÀO:** `Order` publish `TaoBaoHanhFromDonHangMessage` (đơn thanh toán thành công) và message huỷ → 2 consumer trong service tạo/xoá phiếu.
  - **RA:** phụ thuộc `WarehouseProduct` (nếu đổi shape `SanPham_GetListSanPhamByIds`/`Kho_GetBasicInfoByIds` → gãy lookup); cờ `IsSendZalo`/`IsSendSMS` + `ListTrangThaiXuLyGuiTinNhan` điều khiển gửi Zalo/SMS khi tạo/chuyển trạng thái.

## Luồng xuyên module (auto-tạo phiếu từ đơn)

```
[Order] đơn thanh toán thành công
   → publish TaoBaoHanhFromDonHangMessage (Items, IdDonHang, IdDonVi, TypeDonHang, NgayThanhToan, NguoiTaoUserId, IdKho, KhachHang*)
      → [Warranty] TaoBaoHanhFromDonHangConsumer.Consume
         → guard user + gate tiện ích BH
         → đọc CauHinh (cache) → TinhNgayKichHoat theo TypeDonHang + MocKichHoat
         → cross-service WarehouseProduct: lookup thời hạn BH (quy đổi tháng→ngày, AddMonths) + tên kho
         → filter SP có BH → loại item đã có phiếu (idempotent) → batch sinh mã BH{yyyy}-{seq7}
         → InsertRange BaoHanh (audit set MANUAL từ NguoiTaoUserId; snapshot cờ Zalo/SMS)

[Order] đơn bị huỷ → message huỷ → XoaBaoHanhFromDonHangConsumer → xoá phiếu tương ứng
```

> 🔴 **Ripple ngược cần nhớ khi sửa Order:** publish `TaoBaoHanhFromDonHangMessage` nằm ở luồng "đơn thanh toán thành công" của Order. **Sửa Order (đổi trạng thái thanh toán / shape message / field Items như Ma/Imei/IdSanPham/GiaBan) mà không biết message này → gãy auto-tạo phiếu BH hoặc tạo trùng.** Idempotent bám `SanPhamMa`+`Imei` → đổi cách sinh `Ma`/`Imei` bên Order làm hỏng chống-trùng.

## Lỗi đã biết / lưu ý kỹ thuật

- `TaoBaoHanhFromDonHangConsumer.cs:125` — Consumer chạy ngoài HTTP context: `EfRepository.GetUserLogin()` trả **null**. Audit fields (`NgayTao/NgayCapNhat/NguoiTaoUserId/NguoiCapNhatUserId`) PHẢI set manual từ message; quên set → audit rỗng. Bất kỳ path khác trong service nếu tái dùng EfRepository auto-audit sẽ KHÔNG hoạt động trong consumer.
- `TaoBaoHanhFromDonHangConsumer.cs:206`–`:218` — `LookupSoNgayBaoHanhAsync` nuốt exception (catch im lặng) → dựa vào MassTransit retry policy. Nếu WarehouseProduct lỗi kéo dài, message có thể retry nhiều lần; idempotent (`:107`) chặn tạo trùng nên retry an toàn về mặt dữ liệu.
- `TaoBaoHanhFromDonHangConsumer.cs:229`–`:243` — `GetCauHinhCachedAsync` cache cả giá trị **null** (config chưa có) trong 5'. Nếu đơn vị vừa lưu CauHinh, cache invalidate dựa vào `CauHinhService` Remove cùng key prefix; đường consumer chỉ Set, không Remove → phụ thuộc `CauHinhService.SaveCauHinhBaoHanh` gọi Remove để đồng bộ.
- Route thực tế của các controller phụ **khác tên tiện ích**: `CauHinhController` → `api/CauHinh` (endpoint `GetCauHinhBaoHanh`/`SaveCauHinhBaoHanh`, tiện ích code `CauHinhBaoHanhController`); `CommonController` → `api/Common` (enum dropdown, tiện ích code `CommonWarrantyController`). Đừng nhầm code tiện ích thành HTTP route.
- RBAC KHÁC nhau giữa controller: `BaoHanh` + `BaoHanhLanXuLy` dùng danh sách `BaoHanhRoles` đầy đủ; còn `DMBaoHanhMoRong`, `BaoHanhInDonConfig`, `CauHinh`, `Common` chỉ `[SBAuthorize]` bare (auth + tiện ích `TI_BaoHanh`, KHÔNG giới hạn role list).

## Node liên quan cần dựng (cùng module — chưa có trong kho)

- `registry/bao-hanh/BaoHanhLanXuLyController.md` — lịch sử lần xử lý (GetByIdBaoHanh/{idBaoHanh}, GetById/{id}, TimKiemTheoDieuKien, Save, Delete) + phụ kiện thay thế; role = `BaoHanhRoles`.
- `registry/bao-hanh/DMBaoHanhMoRongController.md` — DM bảo hành mở rộng 2 tầng: SuperAdmin (mẫu chung) + theo Đơn vị; `KhoiTaoTuMauSuperAdmin` (`DMBaoHanhMoRongController.cs:120`).
- `registry/bao-hanh/CauHinhController.md` — cấu hình BH per đơn vị (JsonSetting, cache TTL 5', Remove khi save).
- `registry/bao-hanh/CommonController.md` — enum dropdown (trạng thái xử lý / type đơn / thứ tự ưu tiên / phân loại BH).
- `registry/bao-hanh/BaoHanhInDonConfigController.md` — cấu hình in phiếu BH.
- `features/bao-hanh/quan-ly-phieu-bao-hanh/` — feature doc prose (luồng auto-tạo + idempotent + ngày kích hoạt/hết hạn + Import Excel + lần xử lý/phụ kiện + DM mở rộng).
- `features/_FRESHNESS.md` — thêm dòng watermark: `bao-hanh (Warranty) = BE e617abcbbf @ 2026-07-15`.
