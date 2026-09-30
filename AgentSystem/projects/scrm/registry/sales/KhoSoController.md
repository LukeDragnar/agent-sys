---
controller: KhoSoController
module: sales
service: Contact
man_hinh_ui: "Sale > Kho số (route sale/khoso — KhoSoComponent; popup Lịch sử xem số: lichsu-xemso)"
muc_dich: "Pool số/lead (kho số) cho sale tự nhặt: tìm/lọc kho số, xem số (lộ SĐT), chọn số (nhặt lead về mình), lịch sử xem số, và admin khởi tạo lại kho số theo đơn vị/chi nhánh."
alias: [KhoSo, "kho số", "nhặt số", "lấy số", "pool lead", "xem số", "chọn số"]
auth: public
state_machine: []
deprecated: false
entities: [KhoSo, KhoSoLog, KhoSoViewLog, Contact]
stored_procs: []
depends:
  - "KhoSoStoreService (TimTheoDieuKienAsync — list lọc)"
  - "KhoSoService (XemSo/ChonSo/LichSuXemSo/KhoiTaoLai/DropdownNhomFilter)"
  - "KhoSoCauHinhChiNhanhService + CauHinhService (GetKhoSoCfgApplyAsync — cấu hình kho số áp dụng)"
  - "CommonService (GetDMTacNghiepByIdDonViChiNhanh — tác nghiệp IsKhoSo)"
  - "ES (ESIndexContact / ESIndexKhoSo), MassTransit (DataUpdateContactMessage), RabbitMQ contact sync"
cross_module_in: []
cross_module_out:
  - "Order/OrderLogistic — ChonSo gọi CapNhatSaleUserIdDonHangLog cập nhật SaleUserId lên đơn hàng/đơn log của contact"
  - "Elasticsearch — reindex Contact + KhoSo sau ChonSo"
  - "MongoDB (qua DataUpdateContactMessage) — đồng bộ IdNhomNhanVien của contact"
  - "Contact RabbitMQ — KiemTraThayDoiContact publish thay đổi contact"
---

# KhoSo — Kho số (pool lead cho sale nhặt)

> Kho số = pool các lead/số chưa có chủ; sale **xem số** (lộ SĐT, bị giới hạn tần suất) rồi **chọn số** (nhặt lead về mình → xóa khỏi kho, gán SaleUserId, đẩy đơn + ES + Mongo). Toàn bộ điều kiện được gate bởi cấu hình kho số (`GetKhoSoCfgApplyAsync`).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm/lọc kho số (grid) | Danh sách / Lọc | POST `TimTheoDieuKien` | `KhoSo_TimTheoDieuKien` | public | Mở màn Kho số | `KhoSoStoreService.TimTheoDieuKienAsync(gridInfo, CurrentUser)` — trả list kho số theo điều kiện + phân quyền theo team/đơn vị. | EF (store) | — |
| 2 | Xem số (lộ SĐT) | Icon xem số 👁 | POST `XemSo` | `KhoSo_XemSo` | public | Chỉ hiện khi cấu hình `ChoPhepSuDungIconViewSo=true` | Gate: có cấu hình → `IsSuDungKhoSo` → `ChoPhepSuDungIconViewSo` → kho số tồn tại + đúng đơn vị → cooldown sale (`SoPhutGiuaCacLanSaleXemSo`) → cooldown contact (`SoPhutGiuaCacLanContactMoXemSoTrongNgay`). Pass → insert `KhoSoViewLog`, trả `{Id, KhachHangPhone}`. | EF | — |
| 3 | Lịch sử xem số | (popup) Lịch sử xem số | POST `LichSuXemSo` | `KhoSo_LichSuXemSo` | public | Chỉ khi `ShowLsTacNghiepKhoSo=true` | Trả `KhoSoViewLog` phân trang theo IdKhoSo (đơn vị+chi nhánh user). | EF | — |
| 4 | **Chọn số (nhặt lead)** | Chọn số | POST `ChonSo` | `KhoSo_ChonSo` | public | Hiện theo cấu hình + role sale | Gate nhiều tầng (xem "Quy tắc"). Pass → trong transaction: **xóa khỏi KhoSo**, insert `KhoSoLog`, update Contact (gán SaleUserId/nhóm, `KhoSoIsDaVaoKho=false`, `PhanBoTuDau=KhoSo`), cập nhật đơn hàng, log tác nghiệp; sau commit reindex ES + publish Mongo + Rabbit. | EF + transaction | — |
| 5 | Khởi tạo lại kho số theo đơn vị | Khởi tạo lại (đơn vị) | POST `KhoiTaoLaiKhoSoTheoDonVi` | `KhoSo_KhoiTaoLaiKhoSoTheoDonVi` | public | Chỉ admin; chỉ chạy khi `IsSuDungKhoSo=false` | Reset toàn bộ kho số đơn vị: phải TẮT cấu hình dùng kho số trước, rồi loop `StartKhoiDong(false)` theo lô 200 bản ghi. | EF (batch) | GET-clone không, nhưng reset hàng loạt nguy hiểm |
| 6 | Khởi tạo lại kho số theo chi nhánh | Khởi tạo lại (chi nhánh) | POST `KhoiTaoLaiKhoSoTheoChiNhanh` | `KhoSo_KhoiTaoLaiKhoSoTheoChiNhanh` | public | Chỉ admin; chỉ khi cấu hình chi nhánh `IsSuDungKhoSo=false` | Như #5 nhưng scope chi nhánh (`GetKhoSoCauHinhChiNhanhAsync`), loop `StartKhoiDong(true)` lô 200. | EF (batch) | — |
| 7 | Dropdown nhóm filter | (combo lọc nhóm) | GET `DropdownNhomFilter` | — (`[SBAuthorize]` rỗng) | public | Combo trên màn lọc | Trả danh sách nhóm để filter kho số theo `CurrentUser`. | EF | Không gắn permission key |

## Quy tắc nghiệp vụ (gating ChonSo — KhoSoService.cs:1373)
1. Có cấu hình kho số (`GetKhoSoCfgApplyAsync`) → không có ⇒ lỗi "Không tìm thấy cấu hình".
2. `IsSuDungKhoSo == true` → false ⇒ "Đơn vị không cấu hình sử dụng kho số."
3. User phải có 1 trong role `DV-QL-SALE / DV-SALE / DV-SALE-LEAD` ⇒ ngược lại "Chỉ có role sale... mới được chọn số!".
4. Cooldown lấy số của sale: nếu `SoPhutGiuaCacLanSaleLaySo != null` và lần lấy gần nhất + X phút > now ⇒ chặn (báo giờ mở lại).
5. Giới hạn số lần/ngày: `CountMaxSaleLaySoTrongNgay > 0` và đếm KhoSoLog hôm nay ≥ ngưỡng ⇒ "Bạn đã vượt quá số lần chọn số trong ngày".
6. Kho số tồn tại + đúng đơn vị user ⇒ ngược lại "Không tìm thấy contact!".
7. Nếu tác nghiệp KHÔNG `IsMacDinhVaoKhoTong` → áp lọc theo nhóm sale được phép (`ChiHienThiKhoSoCuaTeam`):
   - `SauXPhutVaoKhoNotSaleNhatViewAllDonVi > 0`: lead quá X phút trong kho HOẶC thuộc nhóm user ⇒ được lấy; ngược lại "Không đủ điều kiện lấy số này!".
   - `<= 0`: chỉ lấy được nếu `khoSo.IdNhomNhanVien` thuộc nhóm user.
8. Contact tồn tại + `KhoSoIsDaVaoKho==true` ⇒ ngược lại "Contact không vào kho số...".
9. Contact CHƯA chốt đơn (`LgtDonHangTrangThaiChotDon != DaChotDon` và `DmDonHangTrangThai != DaThanhToan`) ⇒ ngược lại "Contact đã chốt đơn...".
10. Contact CHƯA đối soát (`LgtTrangThaiDoiSoat != true`) ⇒ ngược lại "Contact đã được đối soát...".
> Pass hết ⇒ thực thi xóa-kho + gán contact trong transaction (xem hành động #4).

## Quan hệ
- **Gọi sang:** KhoSoStoreService (list), CauHinhService/KhoSoCauHinhChiNhanhService (cấu hình áp dụng), CommonService (tác nghiệp IsKhoSo), CapNhatSaleUserIdDonHangLog (đẩy SaleUserId lên đơn).
- **Dữ liệu (entities):** `KhoSo` (pool), `KhoSoLog` (log nhặt số — SaleFrom→SaleTo), `KhoSoViewLog` (log xem số), `Contact` (đối tượng được gán chủ).
- **Ripple / đồng bộ:** ChonSo reindex ES (Contact + KhoSo), publish `DataUpdateContactMessage` (Mongo nhóm NV), `KiemTraThayDoiContact` (Rabbit), cập nhật đơn hàng. Khởi tạo lại = batch delete pool.

## Luồng xuyên module (ChonSo → nhặt lead → đồng bộ)
trigger: sale bấm "Chọn số" → gate 10 bước → **transaction**: DeleteAsync(khoSo) + Insert KhoSoLog + Update Contact (chủ mới, `PhanBoTuDau=KhoSo`) + CapNhatSaleUserIdDonHangLog (cập nhật đơn hàng/đơn logistic của contact) + log tác nghiệp → commit → side-effect: ESIndexContact + ESIndexKhoSo + publish Mongo (nếu có IdNhomNhanVien) + Rabbit KiemTraThayDoiContact. Lỗi giữa chừng ⇒ RollbackAsync, trả message lỗi.

## Lỗi đã biết / lưu ý kỹ thuật
- `KhoSoController.cs:84` — `DropdownNhomFilter` dùng `[SBAuthorize]` rỗng (không permission key), khác các endpoint còn lại đều keyed.
- `KhoSoController.cs:66-67,75-76` — còn comment `//[SBAuthorize()]` cạnh các nút khởi tạo lại (di tích bỏ auth tạm trong dev) — cần đảm bảo không bật nhầm.
- `KhoSoController.cs:91-97` — endpoint test `TestKhoSoVaoKho` `[AllowAnonymous]` bị comment out (giữ lại trong source) — không nên uncomment lên prod.
- `KhoSoService.cs:1108-1117` & `1422-1425` — XemSo/ChonSo có comment `// && a.IdDonVi == userLogin.DonViId` (di tích) rồi check đơn vị bằng if riêng → ý đồ ok nhưng dễ gây nhầm khi đọc.
- `KhoSoService.cs:1411` — đếm số lần lấy số/ngày dùng `TableWithNolock().Count(...)` đồng bộ (nolock) → dưới race nhiều request đồng thời có thể đếm thiếu → vượt nhẹ `CountMaxSaleLaySoTrongNgay`.
- `KhoSoService.cs:1181-1370` — toàn bộ bản ChonSo cũ bị comment giữ trong file (`//TODO:Backup hàm này - tamdc 20251106`) → ~190 dòng dead code, dễ sửa nhầm nhánh.
- `KhoSoService.cs:1453,1461` — cast `(Guid)khoSo.IdNhomNhanVien` không null-check → nếu kho số có `IdNhomNhanVien=null` mà rơi vào nhánh lọc nhóm sẽ ném NullReferenceException.
- `KhoSoService.cs:1593-1648` — KhoiTaoLai chỉ chạy khi `IsSuDungKhoSo=false`; reset theo lô 200 không transaction tổng → nếu lỗi giữa chừng pool ở trạng thái nửa reset.
