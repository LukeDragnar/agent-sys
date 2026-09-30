# Dev — Quản lý log & nhật ký (Log Delivery / Log Ecommerce)

> Service: **LogDelivery** + **LogEcommerce** (BE .NET). Màn nội bộ admin hệ thống (HT-AD). Log lưu **MongoDB** (không phải SQL nghiệp vụ). Chỉ dành cho dev.

## Màn hình & route (FE)
| Màn hình | Vai trò | Ghi chú |
|---|---|---|
| Log Delivery (HT-AD) | Tra cứu nhật ký data giao vận + nút Gửi lại | màn nội bộ Hệ thống |
| Log Ecommerce (HT-AD) | Tra cứu nhật ký data sàn TMĐT + nút Gửi lại | màn nội bộ Hệ thống |

> Route FE cụ thể: cần xác nhận khi cần (registry chưa ghi route FE). Cả 2 màn thuộc nhóm "Hệ thống / Log & nhật ký".

## Endpoint / Controller (trace registry)
**LogDeliveryController** — route `api/LogDelivery`, `[SBAuthorize]` key (HT-AD) → internal. (registry: `projects/scrm/registry/he-thong/LogDeliveryController.md`)
| MaTinhNang (key) | Route | Mục đích |
|---|---|---|
| `LogDeliveryController_TimTheoDieuKienAsync` | POST `api/LogDelivery/TimTheoDieuKien` | Lọc log theo `LogDeliveryFilter` (Mongo) |
| `LogDeliveryController_ResendDataErrorToDelivery` | POST `api/LogDelivery/ResendDataErrorToDelivery` | Đẩy lại bản ghi lỗi (`ReqResendDataError`) vào luồng Delivery |
| (public, không `[SBAuthorize]`) | GET `api/LogDelivery/TestJob` | Gọi `JobClearDataLogMongo` dọn log Mongo — endpoint test |

**LogEcommerceController** — route `api/LogEcommerce`, `[SBAuthorize]` key (HT-AD) → internal. `[TienIch]` đã comment. (registry: `projects/scrm/registry/he-thong/LogEcommerceController.md`)
| MaTinhNang (key) | Route | Mục đích |
|---|---|---|
| `LogEcommerceController_TimTheoDieuKien` | POST `api/LogEcommerce/TimTheoDieuKien` | Lọc log theo `LogEcommerceFilter` (Mongo) |
| `LogContactReceive_ResendToContact` (⚠️ key dùng nhầm) | POST `api/LogEcommerce/ResendDataErrorToEcom` | Đẩy lại bản ghi lỗi vào luồng Ecommerce |

## Entities
- `LogDelivery` — bản ghi log data giao vận (lưu MongoDB).
- `LogEcommerce` — bản ghi log data nhận từ sàn (lưu MongoDB).
- `ReqResendDataError` — payload đóng gói khi gửi lại data lỗi (dùng chung cho cả 2 luồng).

## Luồng code
- **Tra cứu:** controller → `ILogDeliveryService.TimTheoDieuKien` / `ILogEcommerceService.TimTheoDieuKien` → query kho log Mongo theo filter → trả danh sách. Read-only.
- **Gửi lại:** controller → `...Service.ResendDataErrorToDelivery` / `ResendDataErrorToEcom` → đóng gói `ReqResendDataError` → đẩy lại vào luồng xử lý đích (Delivery / Ecommerce). Side-effect cross-luồng.
- **Dọn log:** `JobClearDataLogMongo` (qua endpoint test `TestJob` hoặc job định kỳ) → xóa data log Mongo cũ.

## Bug-prone / lưu ý
- **`LogDeliveryController.cs:34` — `TestJob` PUBLIC, không `[SBAuthorize]`**, gọi `JobClearDataLogMongo` (xóa log Mongo). Bất kỳ ai biết URL đều có thể gọi dọn log → cần giới hạn quyền hoặc xóa endpoint. (Đã ghi nhận; KHÔNG sửa code ở session này — để session security xử lý.)
- **`LogEcommerceController.cs:30` — `ResendDataErrorToEcom` dùng NHẦM permission key `LogContactReceive_ResendToContact`** (key của module LogContact) thay vì key riêng của Ecommerce → phân quyền sai module; nếu siết/cấp quyền theo key này sẽ ảnh hưởng nhầm sang LogContact.
- **`LogDeliveryController.cs:27` — text mô tả `ResendDataErrorToDelivery` ghi "Gửi lại data Landing To Contact"** (copy-paste sai context) → đừng tin mô tả này; hành vi thực là resend vào luồng Delivery.
- **Resend = xử lý lại data thật** ở luồng đích → gửi lại bản ghi không-lỗi hoặc gửi lại nhiều lần → xử lý/cập nhật trùng. Không có cơ chế idempotent ở tầng controller (cần kiểm tra phía consumer luồng đích).

## File source (path:line)
- BE: `LogDeliveryController.cs:27` (mô tả sai), `:34` (TestJob public).
- BE: `LogEcommerceController.cs:30` (key resend dùng nhầm).
- Service interface: `ILogDeliveryService`, `ILogEcommerceService` (TimTheoDieuKien / Resend / JobClearDataLogMongo).
- Registry: `projects/scrm/registry/he-thong/LogDeliveryController.md`, `projects/scrm/registry/he-thong/LogEcommerceController.md`.
- Log store: MongoDB (không trong repo source).
