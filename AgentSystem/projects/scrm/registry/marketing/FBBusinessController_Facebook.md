---
controller: FBBusinessController
module: marketing
service: Facebook
man_hinh_ui: "Marketing > Facebook > (popup/form chọn Business khi đồng bộ fanpage) — lưới Business của FB User đã kết nối"
muc_dich: "Tra cứu danh sách Facebook Business (BM) mà user đã đồng bộ về (qua luồng SyncPageFB V1): lọc theo tên/BusinessId, phân trang. Chỉ đọc — không thêm/sửa/xóa."
alias: ["business facebook", "BM facebook", "danh sách business", "FBBusiness"]
auth: public
state_machine: []
deprecated: false
entities: [FBBusiness]
stored_procs: []
depends:
  - "IFBBusinessService (Facebook.Service.Generated — chỉ 1 hàm TimTheoDieuKienAsync)"
cross_module_in:
  - "Marketing/Facebook UI — form hiển thị Business của FB User"
cross_module_out:
  - "Không. FBBusiness được seed từ FBFanpageController_Facebook.SyncPageFB (V1) khi page thuộc Business"
---

# FBBusinessController (Facebook) — Danh sách Facebook Business (BM)

> Controller chỉ-đọc, 1 endpoint. Dữ liệu `FBBusiness` được TẠO ở luồng `SyncPageFB` (V1) của `FBFanpageController_Facebook` chứ không phải ở đây. `[TienIch]` của controller ghi nhầm là "FBFanpageController"/"Quản lý fanpage FB" (FBBusinessController.cs:11).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm/lọc danh sách Business | Tìm kiếm | POST `api/FBBusiness/TimTheoDieuKien` | `FBBusiness_TimTheoDieuKien` ("Tìm kiếm Business" — DV-AD-TONG/CN/MCN, DV-MKT, DV-MKT-LEAD, DV-QL-MKT) | public | — | Lọc `FBBusiness` theo `IdDonVi == DonViId` **và** `NguoiTaoUserId == userLogin.Id` (chỉ business do chính user đồng bộ). KeyWord: nếu là số → match `BusinessName.Contains` HOẶC `BusinessId == số`; nếu chữ → chỉ `BusinessName.Contains`. Phân trang `PageInfo`, OrderBy `NgayTao`, trả total. | EF (FBBusiness) | Hai nhánh `if(Sorts)/else` đều `OrderBy(NgayTao)` giống hệt (FBBusinessService.cs:43-50) → param Sorts vô tác dụng. Biến `isClientCall` khai báo nhưng không dùng (FBBusinessController.cs:26). |

## Quan hệ
- **Gọi sang:** không (chỉ EF nội bộ).
- **Dữ liệu (entities):** `FBBusiness` (chính).
- **Ripple / đồng bộ:** không sinh side-effect; là điểm đọc lại dữ liệu Business đã được `SyncPageFB` V1 seed.

## Lỗi đã biết / lưu ý kỹ thuật
- `FBBusinessService.cs:43-50` — hai nhánh sort y hệt → `req.Sorts` không có tác dụng (luôn OrderBy NgayTao tăng dần).
- `FBBusinessController.cs:11` — `[TienIch]` gán nhầm tên/desc của FBFanpageController.
- `FBBusinessController.cs:26` — `bool isClientCall = true;` dead variable, không truyền xuống service (AccessToken không bị ẩn vì entity FBBusiness không chứa token).
- Quyền hàng (row-level) chặt hơn các controller khác: lọc cả `NguoiTaoUserId == user.Id` → user chỉ thấy Business mình tự sync, không thấy của đồng nghiệp cùng đơn vị.
