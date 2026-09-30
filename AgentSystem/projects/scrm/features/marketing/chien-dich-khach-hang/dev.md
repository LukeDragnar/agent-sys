# Dev — Chiến dịch khách hàng (Tập điều kiện lọc khách)

> Service: **MarketingCampaign** (BE .NET) · lưu trữ **MongoDB** (`CustomerConditionFilter`). Chỉ dành cho dev.

## Màn hình & route (FE)
| Màn hình | Khu vực | Ghi chú |
|---|---|---|
| Tập điều kiện khách hàng | Marketing > Tập điều kiện khách hàng | list bộ điều kiện + Thêm/Sửa/Xóa; cấu hình điều kiện đơn hàng/ngày sinh/địa chỉ/hạng… |

## Endpoint / Controller (trace registry)

**CustomerConditionFilterController** (service MarketingCampaign, auth **public**) — registry `registry/marketing/CustomerConditionFilterController.md`:
| # | Hành động | Endpoint | Quyền (key) |
|---|---|---|---|
| 1 | Lấy danh sách | `GET api/CustomerConditionFilter/GetAll` | `CustomerConditionFilterController_GetAll` |
| 2 | Tìm theo điều kiện | `POST api/CustomerConditionFilter/SearchByCondition` | `CustomerConditionFilterController_SearchByCondition` |
| 3 | Chi tiết | `GET api/CustomerConditionFilter/GetById/{id}` | `CustomerConditionFilterController_GetById` |
| 4 | Tạo | `POST api/CustomerConditionFilter/Insert` | `CustomerConditionFilterController_Insert` |
| 5 | Cập nhật | `POST api/CustomerConditionFilter/Update` | `CustomerConditionFilterController_Update` |
| 6 | Xóa | `POST api/CustomerConditionFilter/Delete/{id}` | `CustomerConditionFilterController_Delete` |

## Quy tắc nghiệp vụ (gating / điều kiện lọc)
- **`TypeFilter` (TypeFilterCampaign):** `Normal=1` (lọc theo `ListConditions`) | `Phone=2` (tạo từ list SĐT) | null coi như Normal | khác → lỗi ngay ở controller "Kiểu loại bộ lọc không phù hợp!".
- **`TypeCodition`** (field nối điều kiện): `1 = AND`, `2 = OR`.
- **`ConditionDetail.Type`** (EnumMarketingCampaignTypeCondition): 1 đơn hàng (+ `ListIdNhanSanPham` gộp từ 2026-06-02), 2 ngày sinh, 3 địa chỉ, 4 tổng tiền mua, 5 tổng điểm tích lũy, 6 hạng KH, 7 tên KH, 8 nghề nghiệp, 9 giới tính. Chi tiết trong `ConditionDetail.JsonData` (deserialize theo Type).
- **`IsAutoDailyScan`** → job quét tự động hằng ngày (`JobScanCustomerInfoWithCondition`).
- **Scope phân quyền** (giống Campaign): IdDonVi → (non-admin) IdChiNhanh → (non-admin-CN) NguoiTaoUserId theo cây quản lý.

## Entities
- **CustomerConditionFilter** (MongoDB) — gồm `ListConditions: List<ConditionDetail>`, `TypeFilter`, `TypeCodition`, `IsAutoDailyScan`.
- `ConditionDetail` lưu generic (`Type` + `JsonData` string).

## Luồng code
- `Insert` / `Update` rẽ nhánh theo `TypeFilter` → service tách hàm Normal vs Phone (`Insert`/`CreateConditionWithTypeFilterPhone`, `Update`/`UpdateConditionWithTypeFilterPhone`).
- Service biên dịch `ListConditions` thành query lọc khách trên Customer/CustomerInfo (MongoDB).
- Tập là input cho **CustomerInfoFilter** (quét ra `CustomerInfoFiltered`) và **Campaign** (tham chiếu `IdCustomerConditionFilter` để gửi).

## Bug-prone / lưu ý
- **Validate `TypeFilter` chỉ ở controller** (Insert/Update). Service Normal vs Phone tách hàm → sửa logic lọc phải sửa **đúng nhánh**, dễ sót.
- **Không thấy guard chặn xóa tập đang được Chiến dịch dùng** (`CustomerConditionFilter.cs` / controller Delete) → xóa có thể làm chiến dịch trỏ tới `IdCustomerConditionFilter` mồ côi. **Cần product xác nhận** có chặn hay không.
- **Thêm loại điều kiện mới:** phải khai báo `Type` vào `EnumMarketingCampaignTypeCondition` + DTO deserialize tương ứng, nếu không điều kiện không được hiểu. Class `ConditionDetail_20250901` cũ đã comment (`CustomerConditionFilter.cs:47`).
- **Sửa tập ảnh hưởng nhiều chiến dịch:** tập dùng chung → đổi `ListConditions` ảnh hưởng mọi chiến dịch tham chiếu (không có cảnh báo).

## File source (path:line)
- Registry: `projects/scrm/registry/marketing/CustomerConditionFilterController.md`.
- BE (service MarketingCampaign): `D:\PROJECT\Git\_SourceForCode\Group2\scrm\...\CustomerConditionFilterController.cs`, entity `...\CustomerConditionFilter.cs` (`:47`).
- Enum: `EnumMarketingCampaignTypeCondition` (Libs Models.Core).
