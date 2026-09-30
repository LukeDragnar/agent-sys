---
controller: CustomerInfoFilterController
module: marketing
service: MarketingCampaign
man_hinh_ui: "Marketing > Tập điều kiện khách hàng > màn 'Tệp khách hàng đã lọc' — thống kê số khách thỏa điều kiện, chạy quét tạo tệp, xem/xóa tệp + tệp lỗi, thêm/xóa khách theo SĐT"
muc_dich: "Engine quét khách hàng theo tập điều kiện: đếm số khách thỏa, chạy tiến trình lọc (phân trang) sinh tệp CustomerInfoFiltered, quản lý tệp đã lọc + tệp lỗi; thêm/xóa khách thủ công theo list SĐT."
alias: ["lọc tập khách hàng", "quét khách theo điều kiện", "tệp khách hàng đã lọc", "đếm khách thỏa điều kiện", "thống kê số lượng khách hàng", "thêm khách theo số điện thoại", "customer info filter"]
auth: mixed
state_machine: []
deprecated: false
entities: [CustomerInfoFiltered, CustomerInfoFilterError, CustomerConditionFilter, CustomerInfo]
stored_procs: []
depends: [MongoDB (CustomerInfo, CustomerInfoFiltered, CustomerInfoFilterError, CustomerConditionFilter)]
cross_module_in: [CustomerConditionFilter (cung cấp điều kiện), Campaign Job (quét gửi)]
cross_module_out: [Campaign (tệp khách → đối tượng gửi)]
---

# CustomerInfoFilterController — Quét & quản lý tệp khách hàng đã lọc

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Đếm khách thỏa điều kiện (theo tập đã lưu) | "Thống kê số lượng" | POST `api/CustomerInfoFilter/NumberOfQualifiedCustomers/{idCondition}` | `CustomerInfoFilter_NumberOfQualifiedCustomers` | public | màn chi tiết tập | Validate tập tồn tại trong scope (IdDonVi → IdChiNhanh → NguoiTaoUserId), trả số khách CustomerInfo thỏa điều kiện | Mongo aggregation `CustomerInfo` | - |
| 2 | Đếm khách thỏa (preview điều kiện chưa lưu) | (nút preview khi đang sửa) | POST `api/CustomerInfoFilter/CheckNumberOfQualifiedCustomers` | `[SBAuthorize(true)]` (chỉ cần đăng nhập) | public | - | Nhận CustomerConditionFilter trực tiếp (chưa lưu), set IsAdmin theo role, build query Aggregation đếm khách thỏa | Mongo aggregation `CustomerInfo` | Quyền `[SBAuthorize(true)]` (no key) — permission key bị comment, chỉ cần đăng nhập |
| 3 | Chạy tiến trình quét lọc | "Quét"/"Thực hiện lọc" | POST `api/CustomerInfoFilter/ProcessToFilterCustomerInfo` | `CustomerInfoFilter_ProcessToFilterCustomerInfo` | public | - | Validate IdTransaction/IdCondition/PageNumber bắt buộc; quét theo trang điều kiện → ghi CustomerInfoFiltered (khách thỏa) + CustomerInfoFilterError (lỗi); trả số bản ghi xử lý | Mongo `CustomerInfoFiltered`/`Error` | Quét theo PageNumber (FE phải lặp gọi đến hết) |
| 4 | Tìm tệp khách đã lọc | "Tìm kiếm" (tab tệp) | POST `api/CustomerInfoFilter/TimKiemTheoDieuKien` | `CustomerInfoFilter_TimKiemTheoDieuKien` | public | tab tệp đã lọc | List khách trong tệp `CustomerInfoFiltered` theo điều kiện/phân trang | Mongo | - |
| 5 | Tìm tệp khách lỗi | "Tìm kiếm" (tab lỗi) | POST `api/CustomerInfoFilter/TimKiemTheoDieuKienError` | `CustomerInfoFilter_TimKiemTheoDieuKienError` | public | tab tệp lỗi | List khách lỗi `CustomerInfoFilterError` (lỗi trong lúc lọc) | Mongo | - |
| 6 | Xóa toàn bộ tệp đã lọc | "Xóa tệp" | POST `api/CustomerInfoFilter/DeleteCustomerInfoFiltered/{idCondition}` | `CustomerInfoFilter_DeleteCustomerInfoFiltered` | public | - | Xóa tất cả bản ghi CustomerInfoFiltered theo idCondition | Mongo | - |
| 7 | Xóa tệp khách lỗi | "Xóa tệp lỗi" | POST `api/CustomerInfoFilter/DeleteCustomerInfoFilterError/{idCondition}` | `CustomerInfoFilter_DeleteCustomerInfoFilterError` | public | - | Xóa tất cả CustomerInfoFilterError theo idCondition | Mongo | - |
| 8 | Thêm khách vào tệp theo list SĐT | "Thêm theo SĐT" | POST `api/CustomerInfoFilter/AddCustomerInfoFilteredByListPhone` | `CustomerInfoFilter_AddCustomerInfoFilteredByListPhone` | public | tập TypeFilter=Phone | Thêm khách vào tệp đã lọc theo danh sách số điện thoại | Mongo | - |
| 9 | Xóa khách khỏi tệp theo list SĐT | "Xóa theo SĐT" | POST `api/CustomerInfoFilter/DeleteCustomerInfoFilteredByPhoneList` | `CustomerInfoFilter_DeleteCustomerInfoFilteredByPhoneList` | public | - | Xóa bản ghi tệp theo list SĐT | Mongo | - |
| 10 | Xóa khách khỏi tệp theo id | (per-row) | POST `api/CustomerInfoFilter/DeleteCustomerInfoFilteredById` | `CustomerInfoFilter_DeleteCustomerInfoFilteredById` | public | - | Xóa bản ghi tệp theo id (dùng chung DTO PhoneListCustomerConditionFilterDto) | Mongo | - |

## Quy tắc nghiệp vụ (gating / điều kiện lọc)
- **Build query** từ CustomerConditionFilter qua `GenarateConditonRENDER_Aggregation(model, idDonVi, idChiNhanh, isAdmin)` → Mongo aggregation trên collection CustomerInfo. IsAdmin do role quyết định (admin = không giới hạn chi nhánh).
- **Scope tập điều kiện** (NumberOfQualifiedCustomers): tập phải thuộc IdDonVi; non-admin thêm IdChiNhanh; non-admin-CN thêm NguoiTaoUserId ∈ cây quản lý → mới được đếm/quét.
- **Quét phân trang**: ProcessToFilterCustomerInfo nhận IdTransaction + IdCondition + PageNumber → xử lý 1 trang/lần; khách thỏa → CustomerInfoFiltered, khách lỗi → CustomerInfoFilterError. FE lặp tới hết.
- **Loại điều kiện**: xem node CustomerConditionFilterController (EnumMarketingCampaignTypeCondition 1–9). Magic number thời gian/trạng thái chốt đơn đã enum-hoá (EnumKieuLoaiBoLocThoiGian) từ 2026-06-01.

## Quan hệ
- **Gọi sang:** MongoDB (CustomerInfo để lọc, CustomerInfoFiltered/Error để ghi tệp), Customer (đồng bộ CustomerInfo qua consumer).
- **Dữ liệu (entities):** `CustomerInfoFiltered`, `CustomerInfoFilterError`, đọc `CustomerConditionFilter` + `CustomerInfo`.
- **Ripple / đồng bộ:** Tệp `CustomerInfoFiltered` là đối tượng nhận của Chiến dịch (Campaign Job đọc tệp để gửi SMS/Zalo).

## Lỗi đã biết / lưu ý kỹ thuật
- File service tên `CustomerInfoFilteredService_LessThanV6.cs` — gợi ý có nhánh xử lý theo version dữ liệu (<V6). Cần xác nhận có service V6+ song song không khi sửa logic lọc.
- `CheckNumberOfQualifiedCustomers` dùng `[SBAuthorize(true)]` không gắn permission key (key bị comment ở controller:29) — chỉ cần đăng nhập là gọi được preview đếm; cân nhắc nếu cần siết quyền.
- `ProcessToFilterCustomerInfo` quét theo từng PageNumber, không tự loop hết — nếu FE không gọi đủ trang → tệp thiếu khách. Job nền (JobScanCustomerInfoWithCondition) xử lý auto cho tập IsAutoDailyScan.
- Endpoint Add/Delete theo SĐT đều dùng chung DTO `PhoneListCustomerConditionFilterDto` (#8/#9/#10) — đọc kỹ field nào dùng (Phone list vs Id) để tránh nhầm.
