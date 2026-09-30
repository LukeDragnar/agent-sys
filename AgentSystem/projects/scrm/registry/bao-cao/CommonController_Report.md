---
controller: CommonController
module: bao-cao
service: Report
man_hinh_ui: "nội bộ — không có UI riêng; phục vụ các màn Báo cáo: dropdown kiểu xem báo cáo, dropdown năm/tuần, cấu hình thời gian gọi-lại báo cáo, và check giới hạn truy cập (chống spam) trước khi chạy báo cáo nặng."
muc_dich: "Controller helper dùng chung cho Report-service: cấp dropdown kiểu xem báo cáo (theo role), bind năm/tuần, đọc cấu hình thời gian gọi-lại/log báo cáo, và gate giới hạn truy cập (DoProcessTicket — khóa số user chạy báo cáo nặng đồng thời). Là service nền mà OrderReport và các report khác phụ thuộc gián tiếp (qua CommonService)."
alias: ["common report", "dropdown kiểu xem báo cáo", "bind năm tuần", "giới hạn truy cập báo cáo", "ticket báo cáo", "thời gian gọi lại báo cáo"]
auth: public
state_machine: []
deprecated: false
entities: [User, ConfigSystem]
stored_procs: []
depends: [CommonService (dropdown kiểu xem, bind năm/tuần, cấu hình thời gian báo cáo, Redis CauHinhReportHeThong), TicketInfoService.DoProcessTicket (khóa truy cập qua Redis), CoreService/ContactService (cache user/cơ cấu khi cần)]
cross_module_in: [Các màn Report FE gọi BindNam/BindSoTuanCuaNam/GetListKieuXem trước khi mở lưới báo cáo, gọi CheckGioiHanTruyCap trước khi chạy báo cáo nặng]
cross_module_out: []
---

# CommonController — Helper dùng chung cho Report-service (dropdown + gate truy cập)

> Controller **helper nền** của Report-service (route `api/Common`). KHÔNG có `[TienIch]` và KHÔNG `[SBAuthorize]` controller-level → mỗi method tự gắn `[SBAuthorize]`/`[SBAuthorize()]` (đều **public** — chỉ cần đăng nhập). Cung cấp dữ liệu phụ trợ cho mọi màn báo cáo: dropdown "kiểu xem" báo cáo (lọc theo role: admin/sale/marketing/kho...), bind năm & số tuần trong năm, đọc cấu hình thời gian gọi-lại/tự-gọi/log báo cáo (từ `ConfigSystem` qua cache Redis), và **gate giới hạn truy cập** `CheckGioiHanTruyCap` chống spam báo cáo nặng. **Không stored proc**; dữ liệu lấy từ EF (`User`, `ConfigSystem`) + Redis + tính toán C#. CommonService (đứng sau controller này) còn chứa `CheckCauHinhKhoangThoiGianChoPhepTimKiem` / `GetStatusShowQueryReport` mà **OrderReportController phụ thuộc** (gate khoảng ngày + log query) — tuy gọi trực tiếp qua service, không qua HTTP controller này.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Dropdown kiểu xem báo cáo | Combobox "Kiểu xem" | `GET api/Common/GetListKieuXem`, `.../GetListKieuXemReportMkt` | (mặc định) | public | mở màn báo cáo | Trả danh sách kiểu xem (1.Kho...12.PT giao hàng) LỌC theo role user (admin full; sale/marketing/kho rút gọn). Bản Mkt mở rộng cho marketing lead. | **EF** (không entity — build từ role + hằng) | — |
| 2 | Bind số tuần của năm | Dropdown "Tuần" | `GET api/Common/BindSoTuanCuaNam/{year}` | (mặc định) | public | chọn năm xong | Tính số tuần ISO của năm → list "Tuần i". | (tính C#) | — |
| 3 | Bind danh sách năm | Dropdown "Năm" | `GET api/Common/BindNam` | (mặc định) | public | màn báo cáo theo năm | List năm từ năm hiện tại về 2018. | (tính C#) | — |
| 4 | Check giới hạn truy cập báo cáo | (gate trước khi chạy báo cáo nặng) | `POST api/Common/CheckGioiHanTruyCap` | (mặc định) | public | trước khi chạy report | Chạy `DoProcessTicket` cho ConfigCodeStep1; false → Task.Delay(1s) + trả về (chưa đến lượt). Step2 null → pass. Có Step2 → check tiếp. Khóa số user đồng thời theo cấu hình. | **EF** `ConfigSystem` (cache) + Redis ticket | gate xếp hàng dựa Redis TTL ngắn (5-10s) — timing nhạy |
| 5 | Lấy thời gian gọi lại / tự gọi báo cáo | (config polling báo cáo) | `POST api/Common/GetThoiGianGoiLaiBaoCao`, `.../GetThoiGianTuGoiBaoCao` | (mặc định) | public | màn báo cáo tổng thể | Đọc config `SO_GIAY_GIOI_HAN_DOC_BAO_CAO` / `SO_PHUT_GOI_LAI_BAO_CAO_TONG_THE` từ cache; SuDung!=true hoặc 0 → null. | **EF** `ConfigSystem` (cache Redis) | — |
| 6 | Lấy số giây tối đa để log báo cáo | (config log) | `GET api/Common/GetTimeToLog` | (mặc định) | public | nội bộ logging report | Đọc config `SO_GIAY_LOG_BAO_CAO`; SuDung!=true → null. | **EF** `ConfigSystem` (cache Redis) | NPE tiềm ẩn: đọc `item.SuDung` trước khi check `item != null` (xem dưới) |

## Quy tắc nghiệp vụ (gating / tier / ngưỡng)
- **GetListKieuXem theo role (có thứ tự):** admin tổng / HT-AD / CSKH / admin CN / chia sổ / kế toán / test → full 12 mục; SaleLead/QuảnLýSale → {2,3,4,7}; Sale thường → {2,3,4}; MarketingLead/QuảnLýMkt → {3,5,8,9,10,11}; Marketing thường → bỏ mục 8; QuảnLýKho → {1,6,12}; còn lại → chỉ {0 "Lựa chọn"}. (Bản `...ReportMkt` cho marketing lead/thường mở rộng full 12 mục.)
- **CheckGioiHanTruyCap (DoProcessTicket):** đọc config `configCode` (số user xử lý đồng thời). Config null/0/SuDung=false → luôn "Đã đến lượt". Ngược lại: nếu user đã lock → strict reject / non-strict pass; nếu còn slot (`lstLocked < maxLockNumber`) và đến lượt → lock + true; hết slot → xếp hàng, trả false.
- **Cấu hình thời gian:** mọi config (`SO_GIAY_GIOI_HAN_DOC_BAO_CAO`, `SO_PHUT_GOI_LAI_BAO_CAO_TONG_THE`, `SO_GIAY_LOG_BAO_CAO`) lấy từ cache `CauHinhReportHeThong` (nguồn entity `ConfigSystem`, do ConfigSystemController quản lý). `Value=="0"` hoặc `SuDung!=true` → coi như tắt (null).

## Quan hệ
- **Gọi sang:** `CommonService` (dropdown, bind năm/tuần, đọc config từ cache); `TicketInfoService.DoProcessTicket` (khóa truy cập, Redis); gián tiếp Core/Contact/WarehouseProduct service qua CommonService (cache user/cơ cấu, sản phẩm ES) cho các helper khác.
- **Dữ liệu (entities):** EF `User` (CommonService.UpdateStatus/GetStatusShowQueryReport), `ConfigSystem` (cache CauHinhReportHeThong); Redis (ticket lock, status query, cấu hình).
- **Ripple / đồng bộ:** không bắn message. Là dependency NỀN: `OrderReportController` (và các report khác) dùng `CommonService.CheckCauHinhKhoangThoiGianChoPhepTimKiem` + `GetStatusShowQueryReport` để gate khoảng ngày & log query — cùng service `CommonService` đứng sau controller này.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Report/Report.Service/Generated/Common/CommonService.cs`
  - **L655 `GetTimeToLog` NPE tiềm ẩn:** truy cập `item.SuDung` TRƯỚC khi check `item != null` (check null ở L661 sau đó) → config thiếu `SO_GIAY_LOG_BAO_CAO` sẽ ném NullReferenceException.
  - L319 region comment sai chính tả "anthor service"; nhiều method async không await (GetListKieuXem...) — chỉ trả dữ liệu tính sẵn.
  - `CheckCauHinhKhoangThoiGianChoPhepTimKiem` (L541): trả về số ngày cho phép khi vượt ngưỡng, trả `-1` khi hợp lệ → caller (OrderReport) hiểu `!= -1` là chặn.
- BE `Report/Report.Service/Generated/TicketInfoService.cs`
  - L63-145 `DoProcessTicket` quản lý hàng đợi bằng Redis với TTL/Created rất ngắn (5000ms) → cơ chế khóa nhạy timing, dễ lệch dưới tải cao; xếp hàng/giải phóng dựa `RequestAt.AddMilliseconds(10000)`.
- BE `Report/Report.API/Controllers/Common/CommonController.cs`
  - L63 `CheckGioiHanTruyCap`: chủ động `Task.Delay(1000)` khi chưa đến lượt (giữ FE chờ) — tăng latency có chủ đích.
