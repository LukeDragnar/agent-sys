---
controller: ThuKhoTacNghiepController
module: he-thong
service: AuthenPartner
man_hinh_ui: "Nội bộ — không có UI. API cho đối tác tra đơn hàng logistic dưới góc nhìn thủ kho/tác nghiệp kho theo điều kiện."
muc_dich: "Cổng API đối tác tra danh sách đơn hàng logistic theo nghiệp vụ thủ kho (đã chốt đơn) theo chi nhánh/điều kiện — ES hoặc SQL store tùy cấu hình; có gating IsChoPhepTraDonHang và cấu hình kho tổng/chia số care đơn."
alias: ["api thủ kho đối tác", "tra đơn thủ kho partner", "GetOrderLogisticByConditions", "thu kho tac nghiep api"]
auth: internal
state_machine: []
entities: [DonHangLogistic, DonHangLogisticInfo, DMGiaoHangTrangThai]
stored_procs: [PRC_SCRM_OrderLogistic_DonHangLogistic_ThuKhoTacNghiep_Search, PRC_SCRM_Order_Logistic_DonHangLogistic_ThuKhoTacNghiep_GetListByIds]
deprecated: false
depends: ["SBAuthorizePartner (JWT partner + CauHinh AccessToken/DeviceId + IsSuDungApiGetData)", "Core service (CauHinh/GetCauHinhAPIPrivate — IsChoPhepTraDonHang)", "OrderLogistic service (CauHinh/GetAllAsyncPrivate, Common/DropdownDMGiaoHangTrangThai...)", "ElasticSearch (DonHangLogisticES)", "SQL Server (SqlSugar store ThuKhoTacNghiep)", "MongoDB (DonHangLogisticInfo)", "Redis (throttle 60s)"]
cross_module_in: ["Hệ thống đối tác → POST ThuKhoTacNghiep/GetOrderLogisticByConditions"]
cross_module_out: ["Core service (cấu hình API đơn vị)", "OrderLogistic service (cấu hình tìm kiếm + DM trạng thái giao hàng)", "ElasticSearch / SQL OrderLogistic (đọc đơn)"]
---

# ThuKhoTacNghiepController — API tra đơn hàng (góc nhìn thủ kho) cho đối tác (AuthenPartner)

Controller AuthenPartner cho đối tác tra **đơn hàng logistic đã chốt đơn** dưới góc nhìn nghiệp vụ thủ kho/tác nghiệp kho. Kế thừa `BasePartnerController` + `[SBAuthorizePartner]` (mô hình auth đối tác xem node `CommonController_AuthenPartner`). `DonViId` lấy từ token. Throttle 60s/lần, `PageSize ≤ 100`.

Logic ở `ThuKhoTacNghiepService.GetOrderLogisticByConditions`: bắt buộc chọn chi nhánh; check cấu hình API đơn vị `IsChoPhepTraDonHang`; lấy cấu hình tìm kiếm OrderLogistic + danh mục trạng thái giao hàng; xử lý cấu hình **kho tổng**/**chia số care đơn**; truy vấn **ElasticSearch** (`DonHangTrangThaiChotDon=1`, `IsDelete=false`) hoặc **SQL store** tùy cờ `Allows_Search_OrderLogistic_By_SQL`; enrich tên/màu trạng thái giao hàng và (nếu `IsChoPhepTraDonHangKemSanPham`) chi tiết sản phẩm từ Mongo. Trả về list đơn + nhóm count theo trạng thái giao hàng.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tra đơn (thủ kho) | (API) | POST `api/ThuKhoTacNghiep/GetOrderLogisticByConditions` | — | internal `[SBAuthorizePartner]` | — | Throttle 60s → kẹp `PageSize<=100` → bắt buộc `IdChiNhanh`; check `IsChoPhepTraDonHang`; query ES (chốt đơn=1, chưa xóa) hoặc SQL store; enrich trạng thái giao hàng + Mongo chi tiết SP (nếu cấu hình cho phép); trả list + count theo trạng thái. | ES hoặc SP `..._ThuKhoTacNghiep_Search` / `..._ThuKhoTacNghiep_GetListByIds` + Mongo | Thiếu chi nhánh → "Hãy chọn chi nhánh!"; `IsChoPhepTraDonHang==false` → "Không được phép tra cứu đơn hàng!" |

## Quy tắc nghiệp vụ (gating)
1. Auth `SBAuthorizePartner` (JWT + CauHinh AccessToken/DeviceId + `IsSuDungApiGetData`).
2. Throttle 60s/lần per đơn vị.
3. `IdChiNhanh` rỗng/Guid.Empty → "Hãy chọn chi nhánh!".
4. Cấu hình API đơn vị `IsChoPhepTraDonHang == false` → "Không được phép tra cứu đơn hàng!".
5. Mặc định `KieuNgay` ("-1"/rỗng) → `DonHangNgayChot`. ES luôn ràng `DonHangTrangThaiChotDon=1` + `IsDelete=false`.
6. Kho tổng: nếu `IsSuDungKhoTong==true` và `IdChiNhanhTong == user.IdChiNhanh` → là chi nhánh tổng (cho phép lọc theo `IdChiNhanhTaoDon`); ngược lại lọc đúng theo chi nhánh.
7. Chia số care đơn: `IsSuDungChiaSoCareDon==true` ảnh hưởng filter kho/care đơn (xem `KiemTraDieuKienCauHinhThuKho`).
8. Nguồn dữ liệu: `Allows_Search_OrderLogistic_By_SQL==false` → ES; ngược lại → SQL store. `Allows_Return_OrderLogistic_Results_ElasticSearch_From_SQL` bật → lấy id ES rồi enrich SQL.
9. `IsChoPhepTraDonHangKemSanPham==true` mới query Mongo `DonHangLogisticInfo` (timeout 2000ms) lấy chi tiết SP.

## Quan hệ
- **Gọi sang:** Core (`CauHinh/GetCauHinhAPIPrivate`), OrderLogistic (`CauHinh/GetAllAsyncPrivate`, `Common/DropdownDMGiaoHangTrangThaiOnSidePrivate`), ElasticSearch, SQL OrderLogistic store, Mongo.
- **Dữ liệu (entities):** `DonHangLogistic` (read), `DonHangLogisticInfo` (Mongo), `DMGiaoHangTrangThai`.
- **Ripple / đồng bộ:** chỉ đọc.

## Lỗi đã biết / lưu ý kỹ thuật
- `ThuKhoTacNghiepService.cs:709, 813-814` — `model.DenNgay.Value.ToMaxMinutes()` và parse `TuNgay`/`DenNgay` không null-check → đối tác không gửi ngày sẽ NullReferenceException.
- `ThuKhoTacNghiepService.cs:152` — nếu `KiemTraDieuKienCauHinhThuKho` lỗi mà `userLogin.IsKiemTrungRole()` true thì **bỏ qua lỗi cấu hình** và chạy tiếp với cấu hình rỗng; với partner (`Id=Guid.Empty`, không role) nhánh này không kích hoạt, nhưng logic lẫn lộn role nhân viên trong service partner.
- `ThuKhoTacNghiepService.cs:409-414` — điều kiện set `IsShowDoiSoatNoiBo` liệt kê `GiaoHangTrangThaiMa == 4` **hai lần** (thừa, không sai kết quả).
- `ThuKhoTacNghiepService.cs:625-628` — nhánh SQL store nuốt exception trả 500 "Lỗi lấy thông tin!"; Mongo enrich bọc try/catch rỗng (dòng 384-386, 537-539) → chi tiết SP thiếu âm thầm khi Mongo timeout.
- Service tham chiếu nhiều cấu hình nội bộ (kho tổng, chia số care đơn, role kiểm trung) vốn dành cho UI nhân viên — khi expose qua API đối tác cần đảm bảo cấu hình đơn vị đúng để không lộ đơn ngoài phạm vi.
