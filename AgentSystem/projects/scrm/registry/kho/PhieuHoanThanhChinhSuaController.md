---
controller: PhieuHoanThanhChinhSuaController
module: kho
service: WarehouseProduct
man_hinh_ui: "2 nơi: (1) nút Sửa/Xóa phiếu mở từ list/modal Phiếu nhập-xuất-chuyển kho khi phiếu đã HoanThanh (qua PopupPhieuNhapEditComponent); (2) màn 'Lịch sử sửa phiếu' — route /lich-su-sua-phieu (LichSuSuaPhieuComponent) xem lịch sử + chi tiết diff + retry."
muc_dich: "1 controller gom toàn bộ tính năng SỬA / XÓA phiếu kho (nhập/xuất/chuyển) đã ở trạng thái HoanThanh: diff header+dòng SP, đảo & áp lại tồn KhoChiTiet, đồng bộ KhoChiTietLichSu, lưu audit session (SQL) + chi tiết diff (Mongo); ripple xuống giá vốn + đơn hàng (giai đoạn 2, hiện TẮT)."
alias: ["sửa phiếu đã hoàn thành", "sửa phiếu nhập hoàn thành", "sửa phiếu xuất hoàn thành", "sửa phiếu chuyển kho hoàn thành", "xóa phiếu kho đã hoàn thành", "chỉnh sửa phiếu kho đã chốt", "lịch sử sửa phiếu", "điều chỉnh tồn sau khi đã hoàn thành"]
auth: internal
state_machine: [Pending, Processing, Done, Failed]
deprecated: false
entities: [PhieuHoanThanhChinhSua, PhieuHoanThanhChinhSuaChiTiet (Mongo), PhieuNhap, PhieuNhapSanPham, PhieuXuat, PhieuXuatSanPham, PhieuChuyenKho, PhieuChuyenKhoSanPham, KhoChiTiet, KhoChiTietLichSu, SanPhamImei, SanPhamLoDate]
stored_procs: [PRC_SCRM_Warehouse_RecomputeGiaVon, PRC_SCRM_Order_RecomputeGiaVonTuPhieuXuat]
depends: [WarehouseProduct, CauHinh (SuperAdmin flags), Redis (lock per phiếu/đơn vị), MongoDB, MassTransit/RabbitMQ, CommonService (SyncKhoChiTietLichSu), Accounting/Order (qua giá vốn — gián tiếp)]
cross_module_in: [PhieuNhapController/PhieuXuatController/PhieuChuyenKhoController (FE list của các phiếu này mở nút Sửa/Xóa khi phiếu HoanThanh → gọi controller này), CauHinh-kho (bật cờ ChoPhepSuaPhieu*DaHoanThanh)]
cross_module_out: [KhoChiTiet (tồn kho — cộng/trừ atomic), KhoChiTietLichSu (đồng bộ qua queue), SanPhamImei + SanPhamLoDate (hủy/thêm/đảo), Accounting+Order (giá vốn bình quân + lan xuống đơn hàng — GIAI ĐOẠN 2, hiện TẮT)]
---

# PhieuHoanThanhChinhSua — Sửa / Xóa phiếu kho đã hoàn thành

> Controller "gom" cố ý: 1 controller xử cả 3 loại phiếu (nhập/xuất/chuyển) + 2 hành động (sửa/xóa) — comment trong code: "1 controller gom toàn bộ — tránh đụng constructor 3 controller cũ". Toàn bộ method hiện gắn `[SBAuthorize]` rỗng (permission key cụ thể đã viết sẵn nhưng **đang comment**).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Sửa phiếu nhập đã hoàn thành | "Sửa" trên list phiếu nhập (qua PopupPhieuNhapEditComponent) | POST `api/PhieuHoanThanhChinhSua/CapNhatPhieuNhap` | `[SBAuthorize]` (key `PhieuHoanThanhChinhSua_CapNhatPhieuNhap` đang **comment**) | internal | Chỉ hiện khi cờ `choPhepSuaPhieuDaHoanThanh` && `choPhepSuaPhieuNhapDaHoanThanh` bật; phiếu `HoanThanh`; KHÔNG phải phiếu tự động | Validate → diff header+dòng SP → cộng/trừ `KhoChiTiet` atomic → xử IMEI/lô → insert session SQL + Mongo diff → sync KhoChiTietLichSu. Phiếu auto (IdDonHang/IdDonHangLogistic/IdPhieuMuaHang/IdTransaction) bị chặn | EF | Bắt buộc `GhiChuLyDoSua`; diff rỗng → "Không có thay đổi nào để lưu" |
| 2 | Sửa phiếu xuất đã hoàn thành | "Sửa" trên list phiếu xuất | POST `api/PhieuHoanThanhChinhSua/CapNhatPhieuXuat` | `[SBAuthorize]` (key `_CapNhatPhieuXuat` comment) | internal | Cờ `choPhepSuaPhieuXuatDaHoanThanh`; phiếu `HoanThanh`; không auto | Tương tự #1 nhưng đảo dấu tồn (xuất = giảm kho). Tăng SL → pick thêm IMEI `DaNhapKho` (endpoint #9) | EF | - |
| 3 | Sửa phiếu chuyển kho đã hoàn thành | "Sửa" trên list phiếu chuyển kho | POST `api/PhieuHoanThanhChinhSua/CapNhatPhieuChuyenKho` | `[SBAuthorize]` (key `_CapNhatPhieuChuyenKho` comment) | internal | Cờ `choPhepSuaPhieuChuyenKhoDaHoanThanh`; `HoanThanh`; `IdTransaction == null` | Như trên + tác động 2 đầu kho (nguồn -, đích +). **Hiện KHÔNG cho đổi kho** (chỉ +/- SL) — đổi kho cần bổ sung field DTO (xem code:345) | EF | - |
| 4 | Xóa phiếu nhập đã hoàn thành | "Xóa" trên list phiếu nhập (cần lý do) | POST `api/PhieuHoanThanhChinhSua/XoaPhieuNhap` (body `{id, ghiChuLyDoXoa}`) | `[SBAuthorize]` (rỗng) | internal | `HoanThanh`, không auto | Xóa mềm header+dòng SP, **trừ lại** tồn đã cộng, xóa IMEI + giảm tồn lô, sync KhoChiTietLichSu (IsDelete=true). Chặn nếu IMEI/lô đã dùng phiếu sau | EF | - |
| 5 | Xóa phiếu xuất đã hoàn thành | "Xóa" trên list phiếu xuất (cần lý do) | POST `api/PhieuHoanThanhChinhSua/XoaPhieuXuat` (body `{id, ghiChuLyDoXoa}`) | `[SBAuthorize]` (rỗng) | internal | `HoanThanh`, không auto | Xóa mềm + **cộng lại** tồn đã trừ, trả IMEI về kho + cộng lại tồn lô. Chặn nếu IMEI/lô đã dùng phiếu sau | EF | KHÔNG có XoaPhieuChuyenKho (chỉ sửa, không xóa) |
| 6 | Lấy tình trạng IMEI của dòng SP | (modal IMEI khi sửa) | POST `api/PhieuHoanThanhChinhSua/LayTinhTrangImei` | `[SBAuthorize]` (key `_LayTinhTrangImei` comment) | internal | Mở modal sửa dòng có IMEI | Trả list IMEI + trạng thái (cho FE biết IMEI nào còn hủy/giảm được) | EF | - |
| 7 | Lấy tình trạng lô/date của dòng SP | (modal lô/date khi sửa) | POST `api/PhieuHoanThanhChinhSua/LayTinhTrangLoDate` | `[SBAuthorize]` (key `_LayTinhTrangLoDate` comment) | internal | Mở modal sửa dòng có lô | Trả list lô + SL còn lại | EF | - |
| 8 | Lấy IMEI có thể xuất thêm | (popup pick IMEI khi tăng SL phiếu xuất) | POST `api/PhieuHoanThanhChinhSua/LayImeiCoTheXuatThem` (body `{idKho, idSanPham}`) | `[SBAuthorize]` | internal | Sửa phiếu xuất, tăng SL dòng IMEI | Trả IMEI còn `DaNhapKho` trong kho xuất để pick | EF | - |
| 9 | Lịch sử sửa phiếu | màn "Lịch sử sửa phiếu" (/lich-su-sua-phieu) | POST `api/PhieuHoanThanhChinhSua/TimList` | `[SBAuthorize]` (key `_TimList` comment) | internal | Mở màn lịch sử | Paging + filter (keyword mã phiếu / ngày / người sửa / loại phiếu / trạng thái / hành động) trên bảng `PhieuHoanThanhChinhSua` | EF | - |
| 10 | Chi tiết 1 lần sửa | "Xem chi tiết" trên màn lịch sử | GET `api/PhieuHoanThanhChinhSua/ChiTiet/{idSession}` | `[SBAuthorize]` (key `_ChiTiet` comment) | internal | Click 1 dòng lịch sử | Load Mongo doc `PhieuHoanThanhChinhSuaChiTiet` (diff header + diff từng dòng SP + snapshot cũ/mới). Match cả `_id` lẫn `IdPhieuHoanThanhChinhSua` (data cũ) | Mongo | - |
| 11 | Trạng thái sync session | (poll sau khi save) | GET `api/PhieuHoanThanhChinhSua/TrangThaiSync/{idSession}` | `[SBAuthorize]` (key `_TrangThaiSync` comment) | internal | Sau khi sửa, FE poll | Trả TrangThai/NgayBatDauXuLy/NgayHoanThanhXuLy/ThongTinLoi. **Hiện session set thẳng `Done`** lúc insert (giai đoạn 2 tắt) | EF | Poll gần như vô nghĩa hiện tại — luôn Done ngay |
| 12 | Thử lại session Failed | "Thử lại" trên màn lịch sử | POST `api/PhieuHoanThanhChinhSua/RetryFailed/{idSession}` | `[SBAuthorize]` (key `_RetryFailed` comment) | internal | Session `Failed` | Reset về `Pending` + clear lỗi + reset SoLanThuLai để job pickup lại | EF | Pipeline recompute đang TẮT → thực tế ít session Failed; republish event đang comment |
| 13 | Rollback lần sửa cuối | "Rollback" | POST `api/PhieuHoanThanhChinhSua/RollbackLanSuaCuoi/{idPhieuEdit}` | `[SBAuthorize]` (key `_Rollback` comment) | internal | - | **CHƯA implement** — luôn trả "Tính năng rollback đang phát triển..." | - | Stub — giai đoạn 1 chưa làm |

## Quy tắc nghiệp vụ (gating sửa/xóa phiếu hoàn thành)
Thứ tự gate trong `SuaPhieuHoanThanhService.*Core` + `KiemTraQuyenVaCauHinh`:
1. **Id hợp lệ + phiếu tồn** (đúng `IdDonVi`, `IdChiNhanh`) → sai = "Không tìm thấy phiếu".
2. **Trạng thái == HoanThanh** → khác = "Phiếu chưa hoàn thành" (PhieuNhap `DmKhoMaTinhTrang == TinhTrangPhieuNhapEnum.HoanThanh`; tương tự xuất/chuyển).
3. **Không phải phiếu tự động** → có `IdDonHang` / `IdDonHangLogistic` / `IdPhieuMuaHang`(nhập) / `IdPhieuBanHang`(xuất) / `IdTransaction` → chặn ("tạo tự động — không cho sửa/xóa thủ công").
4. **Bắt buộc `GhiChuLyDoSua`** (không rỗng).
5. **Cấu hình đơn vị** (`CauHinhSettingSuperAdminDto`): `ChoPhepSuaPhieuDaHoanThanh` master ON **và** cờ con theo loại (`ChoPhepSuaPhieuNhap/Xuat/ChuyenKhoDaHoanThanh`) ON. Default tất cả `false` — bật qua API SuperAdmin (FE config-kho).
6. **(SỬA) Guard IMEI/lô**: IMEI HuyBo chỉ khi đang `DaNhapKho`; IMEI ThemMoi (nhập) phải chưa tồn trong đơn vị; giảm/xóa dòng IMEI → suy ra IMEI ngầm bị ảnh hưởng, chặn nếu IMEI đã phát sinh đơn/chuyển kho khác (bug 437.6); lô không đủ tồn để giảm → chặn.
7. **(XÓA) Guard mạnh hơn**: mọi IMEI của phiếu phải chưa dùng phiếu sau (`KiemTraImeiCuaPhieuDaDung`) + mọi lô phải còn đủ tồn để hoàn lại (`KiemTraLoDateCuaPhieuDaDung`) → vi phạm = liệt kê IMEI/lô + chặn.
8. **Ẩn giá (`IsUserBiAnGia`)**: nếu user bị ẩn giá (không phải admin/CSKH + phiếu từ chuyển kho có `IdTransaction`, hoặc phiếu chuyển xem từ chi nhánh khác + đơn vị bật `IsCheThongTinGiaNhap`) → **override field giá trong DTO bằng giá DB cũ** trước khi diff/apply (defensive, chống user ẩn giá ghi đè giá về 0).

Gate ĐÃ comment (lưu ý khi bật lại): check quyền role admin (`KiemTraQuyenVaCauHinh` §1) và giới hạn tuổi phiếu `SoNgayChoPhepSuaToiDa` (§4) hiện đang comment → **mọi user login + đơn vị bật cờ đều sửa được, không giới hạn ngày**.

## Quan hệ
- **Gọi sang:** `CauHinhService.GetAsync` (cờ SuperAdmin + IsCheThongTinGiaNhap); `CommonService.SyncKhoChiTietLichSu` (queue đồng bộ lịch sử kho); `DiffEngine.BuildDiffAsync` (so snapshot field-by-field, có `PriceFieldsRegistry` để bỏ qua field giá khi ẩn giá); `SanPhamImeiService`; Redis (`LockTakeAsync`/`LockRelease`).
- **Dữ liệu (entities):** `PhieuHoanThanhChinhSua` (SQL session audit: TypePhieuEdit 1/2/3, HanhDong 1=Sửa/2=Xóa, TrangThai, GhiChuLyDoSua), `PhieuHoanThanhChinhSuaChiTiet` (Mongo: diff + snapshot cũ/mới), 3 cặp `Phieu*` + `Phieu*SanPham`, `KhoChiTiet` (+ `KhoChiTietLichSu`), `SanPhamImei`, `SanPhamLoDate`.
- **Ripple / đồng bộ:** tồn `KhoChiTiet.SoLuong` (SQL inline `SET SoLuong = SoLuong + @delta`, **cho phép âm**), `KhoChiTietLichSu` (qua queue), IMEI/lô. **Giai đoạn 2 (recompute SoLuongTruoc/Sau + giá vốn bình quân + lan giá vốn xuống DonHang/PhieuBanHang/Logistic) hiện TẮT** — `DongBoSauSuaPhieuService.ProcessNextSessionForDonViAsync` return ngay ở dòng đầu; session set thẳng `Done` lúc insert.

## Luồng xuyên module (thao tác lớn: Sửa phiếu nhập đã hoàn thành)
```
CapNhatPhieuNhap (FE từ list phiếu nhập, phiếu HoanThanh)
  → WithPhieuLockAsync (Redis lock per phiếu, TTL 2', chống double-submit)
  → validate (state HoanThanh, không auto, lý do, cấu hình đơn vị, guard IMEI/lô)
  → DIFF: header (ApplyHeaderChanges + DiffEngine) + dòng SP (BuildDiffSanPham → Them/Sua/Xoa theo Id line)
  → diff rỗng → "Không có thay đổi nào để lưu"
  → BUILD phase (NGOÀI tx): tính delta KhoChiTiet từng dòng (BuildTacDongKho_NhapXuat: nhập +delta, xuất -delta),
        gom SQL update + record insert; build listThayDoiSanPham (audit) + backfill Mã/Tên SP (bug 438.28)
  → EXECUTE phase (tx ngắn qua ExecutionStrategy vì DbContext bật EnableRetryOnFailure):
        UpdateAsync header → bulk insert/update dòng SP
        → MergeSqlBatch(KhoChiTiet) execute 1 round-trip
        → IMEI: HuyBo→soft-delete, ThemMoi→tạo SanPhamImei; lô: ApDungLoDateThayDoiAsync
        → InsertSession (SQL audit, set TrangThai = Done luôn) → commit
  → POST-COMMIT (ngoài strategy, chống duplicate khi retry transient):
        InsertMongoChiTiet (diff + snapshot) → SyncKhoChiTietLichSu (queue, NhapKho, IsDelete=false)
  → return idSession  (PublishEventDongBo + pipeline giá vốn ĐANG COMMENT → giai đoạn 2)
```
Triage nhanh "tồn sai sau khi sửa phiếu hoàn thành": check `BuildTacDongKho_NhapXuat`/`_ChuyenKho` (dấu delta), `BuildKhoChiTietDeltaAsync` (atomic SQL vs insert record mới), và quy đổi đơn vị tính (`ApplyLineChanges`: SoLuong gốc = SL hiển thị × TiLeQuyDoi). "Giá vốn/đơn hàng KHÔNG đổi sau sửa" = đúng hiện trạng — giai đoạn 2 đang tắt.

## Lỗi đã biết / lưu ý kỹ thuật
- **Pipeline recompute giai đoạn 2 TẮT toàn bộ** — `DongBoSauSuaPhieuService.cs:60` `return;` ngay đầu `ProcessNextSessionForDonViAsync`; `ExecRecomputePipeline` + 2 SP (`PRC_SCRM_Warehouse_RecomputeGiaVon`, `PRC_SCRM_Order_RecomputeGiaVonTuPhieuXuat`) + republish queue đều comment. Hệ quả: sửa/xóa phiếu hoàn thành **KHÔNG** tính lại giá vốn bình quân & **KHÔNG** lan xuống đơn hàng. Nếu cần bật lại: bỏ `return`, uncomment `ExecRecomputePipeline` + `Publish(QueueSyncKhoChiTietLichSu)`.
- **Job fallback TẮT** — `JobScanPhieuHoanThanhChinhSuaPending.Execute` có `return;` đầu hàm ("tạm thời ko cho job này chạy"). Đáng ra chạy mỗi 60s reset Processing quá hạn + retry Failed.
- **Session luôn `Done` ngay** — `InsertSession` set `TrangThai = Done` (comment: "tác động hết vào phiếu luôn, không sửa số lượng trước sau nữa") → `TrangThaiSync`/`RetryFailed` gần như vô tác dụng hiện tại.
- **Quyền chưa siết** — toàn bộ endpoint `[SBAuthorize]` rỗng (permission key + role đã viết sẵn nhưng comment); check role admin trong `KiemTraQuyenVaCauHinh` cũng comment → **bất kỳ user login nào** (đơn vị đã bật cờ) đều sửa/xóa được. Cần xác nhận có chủ ý trước khi lên production.
- **RollbackLanSuaCuoi là stub** — luôn trả lỗi "đang phát triển" (`PhieuHoanThanhChinhSuaService.cs:444`).
- **Phiếu chuyển kho không cho đổi kho** — `SuaPhieuHoanThanhService.PhieuChuyenKho.cs:345` chỉ +/- SL; đổi kho nguồn/đích cần bổ sung field DTO + cập nhật `BuildTacDongKho_ChuyenKho` (đã có logic rollback-kho-cũ + apply-kho-mới nhưng chưa nối DTO).
- **KhoChiTiet cho phép âm** — `BuildKhoChiTietDeltaAsync` tạo record mới với `SoLuong = delta` (có thể âm) "theo spec". Lưu ý khi triage tồn âm bất thường.
- **Code base mặc định (canonical mirror) CHƯA có feature này** — chỉ tồn tại ở mirror Group2 (`_SourceForCode/Group2/scrm/WarehouseProduct`). Mirror `scrm/WarehouseProduct` (và Group1/Group3) **không có** controller này → là feature mới chưa merge về tất cả nhánh. Khi task đụng tính năng này phải xác nhận dev đang ở nhánh có code.
- **Service partial nhiều file**: `SuaPhieuHoanThanhService` = .cs + .PhieuNhap/.PhieuXuat/.PhieuChuyenKho/.XoaPhieu/.ImeiLoDate; `PhieuHoanThanhChinhSuaService` = .cs + .Imei/.LoDate. Sửa logic phải quét hết partial.

## Path nguồn (mirror Group2)
- Controller: `WarehouseProduct.API/Controllers/PhieuHoanThanhChinhSuaController.cs`
- Service entry (sửa/xóa): `WarehouseProduct.Service/Generated/PhieuHoanThanhChinhSua/SuaPhieuHoanThanhService*.cs`
- Service core (validate/session/history): `.../PhieuHoanThanhChinhSuaService*.cs`
- Recompute giai đoạn 2 (tắt): `.../DongBoSauSuaPhieuService.cs` + `RecomputeSoLuongTruocSauService.cs`
- Consumer: `WarehouseProductConsumer.API/Consumer/SuaPhieuHoanThanh/EventDongBoSauSuaPhieuConsumer.cs`
- Job (tắt): `WarehouseProductConsumer.API/Jobs/JobScanPhieuHoanThanhChinhSuaPending.cs`
- Entity SQL: `WarehouseProduct.DataContext/Entity/PhieuHoanThanhChinhSua.cs`; Mongo: `.../CollectionMongoDB/PhieuHoanThanhChinhSuaChiTiet.cs` (chứa enum TypePhieuEdit/TrangThai/HanhDong)
- FE service: `scrm_frontend/src/app/services/warehouseproduct/phieuHoanThanhChinhSua.service.ts`
- FE màn lịch sử: `.../WarehouseProduct/quanly-kho/lichsu-sua-phieu/` (route `/lich-su-sua-phieu`)
- FE modal sửa: `.../quanly-kho/phieu-nhap/popup-phieu-nhap-edit/`; cờ cấu hình: `caidat-hethong/configsystem/config-kho/`
