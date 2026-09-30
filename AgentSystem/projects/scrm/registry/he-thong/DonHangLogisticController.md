---
controller: DonHangLogisticController
module: he-thong
service: AuthenPartner
man_hinh_ui: "Nội bộ — không có UI. API cho đối tác (đặc biệt Pushsale) tra danh sách đơn hàng logistic theo điều kiện."
muc_dich: "Cổng API đối tác để tra đơn hàng logistic: TimTheoDieuKien (bản cũ, đi store DonHangSale_Search) và GetOrderByConditions (bản Pushsale, map sang định dạng pushsale + chi tiết sản phẩm/lịch sử giao hàng)."
alias: ["api đơn hàng đối tác", "tra đơn logistic partner", "pushsale order api", "GetOrderByConditions", "lấy đơn hàng cho đối tác"]
auth: internal
state_machine: []
entities: [DonHangLogistic, DonHangLogisticInfo, ChiNhanh]
stored_procs: [PRC_SCRM_OrderLogistic_DonHangLogistic_DonHangSale_Search, PRC_SCRM_Report_APIPartner_GetOderByCondition, PRC_SCRM_Report_APIPartner_GetOrderDetailByStrOrderLogisticId]
deprecated: false
depends: ["SBAuthorizePartner (JWT partner + CauHinh AccessToken/DeviceId + IsSuDungApiGetData)", "Core service (CauHinh/GetCauHinhAPIPrivate — check IsChoPhepTraDonHang, User/GetListNhanVienByIdsPrivate, Common/GetListQuocGiaSystem_Private)", "OrderLogistic service (DonHangLogistic/ChiTietSanPhamDonHang)", "Log service (LichSuTacNghiep)", "SQL Server (SqlSugar store report)", "MongoDB (DonHangLogisticInfo)", "Redis (throttle 60s + cache quốc gia)"]
cross_module_in: ["Hệ thống đối tác (Pushsale) → POST TimTheoDieuKien / GetOrderByConditions"]
cross_module_out: ["Core service (cấu hình API + user marketing + quốc gia)", "OrderLogistic service (chi tiết sản phẩm đơn)", "Log service (lịch sử tác nghiệp giao hàng)"]
---

# DonHangLogisticController — API tra đơn hàng logistic cho đối tác (AuthenPartner)

Controller AuthenPartner cho đối tác (đặc biệt **Pushsale**) tra đơn hàng logistic. Kế thừa `BasePartnerController` + `[SBAuthorizePartner]` (mô hình auth đối tác xem node `CommonController_AuthenPartner`). `DonViId` lấy từ token, override vào payload → cách ly đơn vị.

Có 2 endpoint dữ liệu + 1 endpoint clear cache test:
- `TimTheoDieuKien` (bản cũ): đi thẳng store `..._DonHangSale_Search`, trả `DonHangLogisticFilterResponse`.
- `GetOrderByConditions` (bản Pushsale): kiểm tra cờ `IsChoPhepTraDonHang` của cấu hình API đơn vị → đi store `PRC_SCRM_Report_APIPartner_GetOderByCondition`, map sang `ResOrderPushsaleDto` (chuẩn pushsale), enrich tên nhân viên marketing (Core), tên tỉnh/huyện/xã (Mongo), tên quốc gia (cache/Core), và (nếu `IsChoPhepTraDonHangKemSanPham`) chi tiết sản phẩm gọi sang OrderLogistic; có thể kèm lịch sử giao hàng (`IsHistories`).

Throttle 60s/lần (CommonCacheService). `PageSize` kẹp ≤ 100.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tra đơn (bản cũ) | (API) | POST `api/DonHangLogistic/TimTheoDieuKien` | — | internal `[SBAuthorizePartner]` | — | Throttle 60s → override `DonViId` → store `..._DonHangSale_Search` (nhiều param lọc) → trả list `DonHangLogisticFilterResponse` + count. | SP `PRC_SCRM_OrderLogistic_DonHangLogistic_DonHangSale_Search` | Không kẹp PageSize ở đây; `idUserSaleString`/`idNhomSaleString` luôn rỗng (hardcode "") → 2 param sale luôn DBNull |
| 2 | Tra đơn (Pushsale) | (API) | POST `api/DonHangLogistic/GetOrderByConditions` | — | internal `[SBAuthorizePartner]` | — | Throttle 60s → kẹp `PageSize<=100` → override `IdDonVi` → check `IsChoPhepTraDonHang`; store report → map ResOrderPushsaleDto; enrich MKT/quốc gia/địa lý; nếu cấu hình cho phép thì kèm chi tiết SP (OrderLogistic) & lịch sử (Log). | SP `PRC_SCRM_Report_APIPartner_GetOderByCondition` + HTTP Core/OrderLogistic/Log + Mongo | `IsChoPhepTraDonHang==false` → "Không được phép tra cứu đơn hàng!" |
| 3 | Clear throttle (test) | (API) | GET `api/DonHangLogistic/ClearTime?pass=...` | — | internal `[SBAuthorizePartner]` | — | Chỉ khi `pass=="HaihnTest"` mới reset cache time-call của `GetOrderByConditions`; còn lại trả Ok rỗng. | Redis | Mật khẩu test hardcode `HaihnTest` còn sót trong production code |

## Quy tắc nghiệp vụ (gating)
1. Auth `SBAuthorizePartner` (JWT + CauHinh AccessToken/DeviceId + `IsSuDungApiGetData`).
2. Throttle 60s/lần per đơn vị (key gồm `Id=Guid.Empty`).
3. `GetOrderByConditions`: cấu hình API `IsChoPhepTraDonHang == false` → chặn "Không được phép tra cứu đơn hàng!".
4. `IsChoPhepTraDonHangKemSanPham == true` mới gọi sang OrderLogistic lấy chi tiết sản phẩm (kèm field `details`).
5. `IsHistories == true` mới lấy lịch sử tác nghiệp giao hàng (gọi Log service).
6. `PageSize > 100` (endpoint #2) → ép 100.

## Luồng xuyên module (Pushsale tra đơn)
1. Pushsale POST `GetOrderByConditions` (Bearer partner token) → check cấu hình API đơn vị (Core).
2. Store report SQL trả data + count → map sang định dạng pushsale.
3. Enrich: nhân viên marketing (Core `User/GetListNhanVienByIdsPrivate`), tên tỉnh/huyện/xã (Mongo `DonHangLogisticInfo`), quốc gia (Redis cache hoặc Core).
4. Nếu cho phép: chi tiết sản phẩm (OrderLogistic `ChiTietSanPhamDonHang`) + lịch sử giao hàng (Log `GetLichSuTacNghiep...`).

## Quan hệ
- **Gọi sang:** Core (cấu hình API, user MKT, quốc gia), OrderLogistic (chi tiết SP đơn), Log (lịch sử tác nghiệp).
- **Dữ liệu (entities):** `DonHangLogistic` (read qua store), `DonHangLogisticInfo` (Mongo enrich).
- **Ripple / đồng bộ:** chỉ đọc — không ghi dữ liệu nghiệp vụ.

## Lỗi đã biết / lưu ý kỹ thuật
- `DonHangLogisticController.cs:57` — `ClearTime` dùng mật khẩu test hardcode `"HaihnTest"` còn trong code production; nếu pass sai vẫn trả `Ok()` (no-op) → endpoint gây nhầm lẫn.
- `DonHangLogisticService.cs:104-105` — `idNhomSaleString`/`idUserSaleString` khởi tạo `""` và **không bao giờ được gán** → 2 param `@StrIdNhomNhanVien`/`@StrUserIdSale` của store luôn DBNull (filter sale/nhóm sale vô hiệu ở `TimTheoDieuKien`).
- `DonHangLogisticService.cs:410-413, 457-460, 582-585` — nhiều khối `catch (Exception ex){ var msgEx = ex.Message; }` nuốt lỗi im lặng trong convert/populate → đơn lỗi sẽ bị bỏ qua âm thầm (data thiếu nhưng vẫn trả Success).
- `DonHangLogisticController.cs` (#1) — `TimTheoDieuKien` không kẹp `PageSize` (chỉ #2 kẹp) → đối tác có thể yêu cầu trang lớn.
