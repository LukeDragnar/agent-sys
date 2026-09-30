---
controller: TongDaiController
module: he-thong
service: AuthenPartner
man_hinh_ui: "Nội bộ — không có UI. API cho đối tác lấy lịch sử file ghi âm cuộc gọi tổng đài (MyHub / Omicall) theo contact/đơn hàng/SĐT."
muc_dich: "Cổng API đối tác lấy danh sách file ghi âm cuộc goi từ tổng đài kết nối (SIM_SO=MyHub, IP=Omicall) theo ContactIds/DonHangIds/PhoneCustomer, có phân trang và khóa chống gọi trùng (Redis lock)."
alias: ["api ghi âm tổng đài", "lấy file ghi âm đối tác", "LayFileGhiAm", "call history partner", "tong dai api"]
auth: internal
state_machine: []
entities: [FileGhiAm, Contact, DonHangLogistic]
stored_procs: [PRC_SCRM_SwitchBoardConnect_MyHubCallHistory, PRC_SCRM_SwitchBoardConnect_CallHistory, PRC_SCRM_OrderLogistic_DonHangLogistic_GetContactId]
deprecated: false
depends: ["SBAuthorizePartner (JWT partner + CauHinh AccessToken/DeviceId + IsSuDungApiGetData)", "SQL Server (SqlSugar store call history MyHub/Omicall + lấy ContactId theo đơn hàng)", "Redis (throttle 60s + distributed lock 30s per đơn vị)"]
cross_module_in: ["Hệ thống đối tác → GET TongDai/LayFileGhiAm"]
cross_module_out: ["SQL SwitchBoardConnect/OrderLogistic (đọc lịch sử ghi âm + map ContactId)"]
---

# TongDaiController — API lấy file ghi âm tổng đài cho đối tác (AuthenPartner)

Controller AuthenPartner cho đối tác lấy **lịch sử file ghi âm cuộc gọi** từ hệ thống tổng đài kết nối. Kế thừa `BasePartnerController` + `[SBAuthorizePartner]` (mô hình auth đối tác xem node `CommonController_AuthenPartner`). `DonViId` lấy từ token. Throttle 60s/lần (CommonCacheService).

Lưu ý: controller này khai báo `[Route("api/[controller]")]` ở class nhưng **không lặp lại `[ApiController]`** (vẫn kế thừa từ BasePartnerController). Logic ở `TongDaiService.LayFileGhiAm`:
1. **Distributed lock Redis** `lock_fileGhiAm_get:{DonViId}` (30s) — chống đối tác cùng đơn vị gọi đồng thời; lock đang giữ → "Đang lấy thông tin ở đơn vị này, vui lòng thử lại sau."
2. Validate `PageIndex >= 1`, `PageSize <= 100`.
3. Nếu có `DonHangIds` → store `..._GetContactId` map ra ContactIds, gộp với `ContactIds` truyền vào.
4. Theo `TypeTongDai`: `SIM_SO` → store MyHub; `IP` → store Omicall; khác → "Chưa hỗ trợ tổng đài ...".
5. Trả list `FileGhiAmFilterResponse` + tổng số bản ghi. Lock luôn được release ở `finally`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy file ghi âm | (API) | GET `api/TongDai/LayFileGhiAm` | — | internal `[SBAuthorizePartner]` | — | Throttle 60s → override `DonViId` → service: lock Redis 30s; validate page; map ContactId từ DonHangIds (store); theo `TypeTongDai` chạy store MyHub (SIM_SO) hoặc Omicall (IP); trả list ghi âm + totalCount. | SP `..._MyHubCallHistory` / `..._CallHistory` / `..._GetContactId` | Lock đang giữ → "Đang lấy thông tin ở đơn vị này..."; `PageIndex<=0` → "PageIndex phải bắt đầu từ 1"; `PageSize>100` → "Không thể lấy quá 100 bản ghi"; type lạ → "Chưa hỗ trợ tổng đài ..." |

## Quy tắc nghiệp vụ (gating)
1. Auth `SBAuthorizePartner` (JWT + CauHinh AccessToken/DeviceId + `IsSuDungApiGetData`).
2. Throttle 60s/lần per đơn vị (CommonCacheService, key `Id=Guid.Empty`).
3. Distributed lock Redis 30s per đơn vị (`SetIfNotExistsAsync`): chỉ 1 request lấy ghi âm tại 1 thời điểm cho mỗi đơn vị.
4. `PageIndex <= 0` → lỗi (page bắt đầu từ 1). `PageSize > 100` → lỗi (không quá 100 bản ghi) — **lưu ý: trả lỗi, KHÔNG tự kẹp** như các controller khác.
5. `TypeTongDai`: `"SIM_SO"` → MyHub store; `"IP"` → Omicall store; khác → "Chưa hỗ trợ tổng đài ...".
6. ContactIds = (ContactIds truyền vào) ∪ (ContactId suy ra từ DonHangIds qua store), lọc bỏ Guid.Empty + distinct.

## Quan hệ
- **Gọi sang:** SQL store SwitchBoardConnect (`MyHubCallHistory`, `CallHistory`) + OrderLogistic (`GetContactId`).
- **Dữ liệu (entities):** `FileGhiAm` (lịch sử ghi âm), `Contact`/`DonHangLogistic` (lọc theo id).
- **Ripple / đồng bộ:** chỉ đọc; Redis lock là side-effect tạm thời (tự release).

## Lỗi đã biết / lưu ý kỹ thuật
- `TongDaiController.cs:11` — class chỉ có `[Route]`, thiếu `[ApiController]` riêng (kế thừa từ base nên vẫn hoạt động); không nhất quán với các controller khác trong service.
- `TongDaiController.cs:28-29` — endpoint là `[HttpGet]` nhưng nhận `LayFileGhiAmAPI gridInfo` chứa list (`ContactIds`, `DonHangIds`) → model binding qua query string với list/Guid khá bất tiện cho đối tác; thường GET với body phức tạp dễ gây nhầm.
- `TongDaiService.cs:53,64` — `lstContactIds = gridInfo.ContactIds ?? new List<Guid>()` rồi `AddRange` ContactId từ đơn hàng; nếu `gridInfo.ContactIds` là reference tới list gốc thì bị mutate (side-effect lên DTO request), tuy không ảnh hưởng kết quả.
- `TongDaiService.cs:96-102` — catch trả "Lỗi: {message}" lộ message exception ra ngoài cho đối tác; `finally` luôn release lock (kể cả khi lock chưa lấy được, `lockKey` đã set nên Remove an toàn).
- `PageSize>100` trả lỗi cứng (không kẹp) → khác hành vi DonHangLogistic/ThuKho/Contact (tự kẹp về 100); đối tác cần biết để gửi đúng.
