---
controller: CauHinhController
module: he-thong
service: AuthenCustomer
man_hinh_ui: "Cấu hình giao diện app mobile khách hàng theo đơn vị (bật/tắt + sắp xếp các section trang chủ HomePage). Màn admin cấu hình mini-app, không phải màn của khách."
muc_dich: "Lưu/đọc cấu hình hiển thị app mobile khách (JsonSetting → MobilePageJson: danh sách Pages + Sections ẩn/hiện) theo từng đơn vị."
alias: ["cấu hình app mobile", "cấu hình mini app", "cấu hình trang chủ app", "ẩn hiện section app khách", "MobilePageJson", "cấu hình AuthenCustomer"]
auth: mixed
state_machine: []
deprecated: false
entities: [CauHinh]
stored_procs: []
depends: ["SQL Server (AuthenConnectStr) — EF Core (CauHinh)", "Redis (cache JsonSetting theo IdDonVi, TTL 1 ngày)", "HomePageSectionEnum / PageEnum (template mặc định)"]
cross_module_in: ["Admin/mini-app config UI → Save, Get/{idDonVi}", "AuthenService.Register/LoginZalo → CauHinhService.GetAsync để lấy IdChiNhanhDefault"]
cross_module_out: ["App mobile khách đọc cấu hình này để render trang chủ (ẩn/hiện/sắp xếp section)"]
---

# CauHinhController — Cấu hình giao diện app mobile khách (theo đơn vị)

Controller CRUD nhẹ cho **cấu hình app khách hàng** của service AuthenCustomer. Tên trùng `CauHinhController` của service Authen nên file có hậu tố `_AuthenCustomer`. Kế thừa `ApiController<CauHinh, CauHinhDto>` (có sẵn ThemMoi/CapNhatThongTin/Xoa/TimTheoId từ base) nhưng dùng thực tế chỉ 2 method tự định nghĩa: `Save` và `Get`. Mỗi đơn vị có **đúng 1 bản ghi** CauHinh (key = IdDonVi, IsDelete=false). Nội dung là JSON `JsonSetting` → `MobilePageJson.Pages[].Sections[]` (mỗi section: Key/Name/Order/IsShow) điều khiển trang chủ HomePage của app.

Controller-level KHÔNG có attribute auth (chỉ `[ApiController]`); base CRUD cũng không gắn `[SBAuthorizeCustomer]` → **auth ở mức `mixed`/lỏng** (đáng lưu ý: endpoint cấu hình không bị khóa rõ ràng ở tầng controller).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lưu cấu hình app | "Lưu" | POST `api/CauHinh/Save` | — | public (không attribute) | Màn cấu hình mini-app | Validate `JsonSetting.MobilePageJson.Pages` không rỗng (else "Chưa có cấu hình ẩn/hiện..."). Tìm CauHinh theo IdDonVi (IsDelete=false): chưa có → insert mới; đã có → update JsonSetting. Clear cache Redis của đơn vị. Trả về Id. | EF (CauHinh) + Redis | Bọc try/catch trả `ex.Message` ra client (lộ chi tiết lỗi). Không gắn auth attribute |
| 2 | Lấy cấu hình app | (load màn config / app render) | GET `api/CauHinh/Get/{idDonVi}` | — | public (không attribute) | — | Đọc cache Redis → DB. Chưa có/parse được → trả **template mặc định** (`GetTemplateAsync`: full section của HomePage, IsShow=true) và cache lại. IdDonVi rỗng → trả `JsonSettingDto` rỗng. | EF + Redis + template enum | Trả template mặc định khi chưa cấu hình (không lỗi 404) |

## Quy tắc nghiệp vụ (cấu hình)
1. Validate Save: `JsonSetting` null → "Model không hợp lệ"; `MobilePageJson==null || Pages rỗng` → "Chưa có cấu hình ẩn/hiện danh sách cách page mobile".
2. Đơn vị có nhiều nhất 1 bản ghi CauHinh active (IdDonVi + IsDelete=false) → Save là upsert.
3. Get fallback nhiều tầng: Redis → DB → template enum (`HomePageSectionEnum`/`PageEnum`). IdDonVi=Guid.Empty và mọi nguồn null → trả `JsonSettingDto` rỗng (case superuser/HT_AD).
4. Cache key `CK_CauHinhAuthenCustomerSetting_<IdDonVi>`, TTL 1 ngày; Save clear cache để Get sau lấy bản mới.

## Quan hệ
- **Gọi sang:** chỉ EF + Redis (không gọi service ngoài).
- **Dữ liệu (entities):** `CauHinh` (IdDonVi, JsonSetting string JSON, IsDelete).
- **Ripple / đồng bộ:** `AuthenService.Register`/`LoginZalo` đọc `CauHinhService.GetAsync(donVi.Id)` để lấy `IdChiNhanhDefault` khi tạo tài khoản; app mobile đọc Get để render HomePage.

## Lỗi đã biết / lưu ý kỹ thuật
- `CauHinhController.cs` + `CauHinhService.cs:91-94` — `SaveAsync` catch trả `ex.Message` thẳng cho client → lộ thông tin nội bộ.
- `CauHinhController.cs:13-37` — controller + base CRUD (ThemMoi/CapNhat/Xoa/TimTheoId) **không gắn `[SBAuthorizeCustomer]`/`[PrivateSBAuthorize]`** → endpoint cấu hình có thể gọi không cần token; kiểm tra middleware/global filter trước khi coi là an toàn.
- `CauHinhService.cs:47` — `messErr.Any()` dùng trên string ("" rỗng → false) để quyết định lỗi; hoạt động nhưng kiểu kiểm tra string-as-error dễ nhầm.
