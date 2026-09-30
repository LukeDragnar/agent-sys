---
controller: CustomerConditionFilterController
module: marketing
service: MarketingCampaign
man_hinh_ui: "Marketing > Tập điều kiện khách hàng — list bộ điều kiện lọc + Thêm/Sửa/Xóa; cấu hình điều kiện đơn hàng/ngày sinh/địa chỉ/hạng... để chiến dịch dùng"
muc_dich: "Quản lý 'tập điều kiện' lọc khách hàng (CustomerConditionFilter trong MongoDB): mỗi tập = list ConditionDetail nối AND/OR; Chiến dịch tham chiếu tập này để chọn khách gửi. Hỗ trợ 2 loại: lọc theo điều kiện (Normal) và lọc theo list SĐT (Phone)."
alias: ["tập điều kiện khách hàng", "bộ lọc khách hàng", "điều kiện lọc khách", "tập khách hàng theo điều kiện", "customer condition filter", "lọc khách theo đơn hàng/ngày sinh/hạng"]
auth: public
state_machine: []
deprecated: false
entities: [CustomerConditionFilter]
stored_procs: []
depends: [MongoDB (CustomerConditionFilter, CustomerInfo), Customer (sync), CustomerInfoFiltered (quét ra tệp)]
cross_module_in: [Campaign (tham chiếu IdCustomerConditionFilter)]
cross_module_out: [CustomerInfoFilter (quét điều kiện ra tệp khách đã lọc)]
---

# CustomerConditionFilterController — Tập điều kiện lọc khách hàng

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy danh sách điều kiện | (dropdown/list) | GET `api/CustomerConditionFilter/GetAll` | `CustomerConditionFilterController_GetAll` | public | - | List tập điều kiện của user (theo scope phân quyền) | Mongo `CustomerConditionFilter` | - |
| 2 | Tìm theo điều kiện | "Tìm kiếm" | POST `api/CustomerConditionFilter/SearchByCondition` | `CustomerConditionFilterController_SearchByCondition` | public | màn list | List tập điều kiện theo filter + scope (IdDonVi → IdChiNhanh → NguoiTaoUserId) | Mongo | - |
| 3 | Chi tiết tập điều kiện | "Xem" | GET `api/CustomerConditionFilter/GetById/{id}` | `CustomerConditionFilterController_GetById` | public | - | Trả full tập điều kiện gồm ListConditions | Mongo | - |
| 4 | Tạo tập điều kiện | "Thêm mới" | POST `api/CustomerConditionFilter/Insert` | `CustomerConditionFilterController_Insert` | public | - | **Rẽ nhánh theo TypeFilter**: Normal(1)/null → `Insert` (lọc theo điều kiện ListConditions); Phone(2) → `CreateConditionWithTypeFilterPhone` (tạo từ list SĐT); khác → lỗi "Kiểu loại bộ lọc không phù hợp!" | Mongo | - |
| 5 | Cập nhật tập điều kiện | "Sửa" | POST `api/CustomerConditionFilter/Update` | `CustomerConditionFilterController_Update` | public | - | Rẽ nhánh giống Insert: Normal/null → `Update`; Phone → `UpdateConditionWithTypeFilterPhone`; khác → lỗi | Mongo | - |
| 6 | Xóa tập điều kiện | "Xóa" | POST `api/CustomerConditionFilter/Delete/{id}` | `CustomerConditionFilterController_Delete` | public | - | Xóa tập điều kiện theo id | Mongo | Cần xác nhận chặn xóa khi đang có Chiến dịch tham chiếu (IdCustomerConditionFilter) — không thấy guard ở controller |

## Quy tắc nghiệp vụ (gating / tier / điều kiện lọc)
- **TypeFilter** (TypeFilterCampaign): `Normal=1` (lọc theo bộ điều kiện ListConditions) | `Phone=2` (tạo bộ lọc từ list SĐT truyền vào). null coi như Normal. Giá trị khác → trả lỗi ngay ở controller.
- **TypeCodition** (trên entity, field nối điều kiện): `1 = AND`, `2 = OR` — cách ghép các ConditionDetail trong ListConditions.
- **ConditionDetail.Type** (EnumMarketingCampaignTypeCondition) — các loại điều kiện lọc 1 khách:
  1 = Điều kiện đơn hàng (gồm nhãn sản phẩm — ListIdNhanSanPham, gộp từ 2026-06-02), 2 = Ngày sinh, 3 = Địa chỉ, 4 = Tổng tiền mua hàng, 5 = Tổng điểm tích lũy, 6 = Danh sách hạng khách hàng, 7 = Tên khách hàng, 8 = Nghề nghiệp, 9 = Giới tính. Dữ liệu chi tiết mỗi điều kiện nằm trong `ConditionDetail.JsonData` (deserialize theo Type).
- **IsAutoDailyScan**: tập có cờ này → Job quét tự động hằng ngày (JobScanCustomerInfoWithCondition).
- **Scope phân quyền** giống Campaign: IdDonVi → (non-admin) IdChiNhanh → (non-admin-CN) NguoiTaoUserId theo cây quản lý.

## Quan hệ
- **Gọi sang:** Customer/CustomerInfo (MongoDB) để biên dịch điều kiện thành query lọc khách.
- **Dữ liệu (entities):** `CustomerConditionFilter` (Mongo, gồm `ListConditions: List<ConditionDetail>`).
- **Ripple / đồng bộ:** Tập này là input cho CustomerInfoFilter (quét ra tệp `CustomerInfoFiltered`) và cho Campaign (gửi). Sửa điều kiện → ảnh hưởng tập khách của mọi chiến dịch tham chiếu.

## Lỗi đã biết / lưu ý kỹ thuật
- Validate `TypeFilter` chỉ chặn ở controller (Insert/Update). Service Insert/Update Normal vs Phone tách hàm — sửa logic lọc phải sửa đúng nhánh.
- `ConditionDetail` lưu dạng generic (`Type` + `JsonData` string) → mọi điều kiện serialize JSON; class `ConditionDetail_20250901` cũ đã comment (CustomerConditionFilter.cs:47). Khi thêm loại điều kiện mới phải khai báo Type vào EnumMarketingCampaignTypeCondition + DTO deserialize tương ứng.
- Không thấy guard chặn xóa tập đang được Chiến dịch dùng → xóa có thể làm chiến dịch trỏ tới IdCustomerConditionFilter mồ côi. Cần xác nhận.
