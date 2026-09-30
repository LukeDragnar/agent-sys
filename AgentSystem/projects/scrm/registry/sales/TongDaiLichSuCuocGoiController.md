---
controller: TongDaiLichSuCuocGoiController
module: sales
service: Switchboard
man_hinh_ui: "Tổng đài > Lịch sử cuộc gọi (danh sách + thống kê cuộc gọi); tạo lịch sử cuộc gọi khi thực hiện cuộc gọi"
muc_dich: "Quản lý lịch sử cuộc gọi tổng đài: tạo bản ghi lịch sử khi gọi, lấy danh sách lịch sử cuộc gọi, thống kê cuộc gọi; expose API private để service khác lấy lịch sử cuộc gọi omicall theo contact và theo ids."
alias: ["lịch sử cuộc gọi tổng đài", "call history tổng đài", "thống kê cuộc gọi", TongDaiLichSuCuocGoi]
auth: mixed
state_machine: []
deprecated: false
entities: [TongDaiLichSuCuocGoi, CallHistory]
stored_procs: []
depends: [ITongDaiLichSuCuocGoiService]
cross_module_in: ["service nội bộ (GetLichSuCuocGoiOmi — lấy lịch sử cuộc gọi omi theo contact)", "Contact/CSKH (GetListCallHistoryByIds)"]
cross_module_out: []
---

# TongDaiLichSuCuocGoi — Lịch sử cuộc gọi tổng đài

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tạo lịch sử cuộc gọi | (tự động khi gọi) | POST `api/TongDaiLichSuCuocGoi/TaoLichSuCuocGoi` | `[SBAuthorize]` (chỉ login) | public | khi thực hiện cuộc gọi | Ghi bản ghi lịch sử cuộc gọi theo `CurrentUser`. | EF | Key gốc `TongDaiDonVi_TaoLichSuCuocGoi` đã bị comment → còn `[SBAuthorize]` rỗng (lỏng hơn ý đồ ban đầu), path:25-26 |
| 2 | Danh sách lịch sử cuộc gọi | (load màn) | POST `api/TongDaiLichSuCuocGoi/GetListCallHistory` | `TongDaiCallHistory_GetListCallHistory` (HT-AD, HT-KT, DV-SALE, DV-QL-SALE, DV-SALE-LEAD) | public | màn lịch sử cuộc gọi | Trả danh sách lịch sử cuộc gọi theo filter + `CurrentUser`. | EF | — |
| 3 | Thống kê danh sách cuộc gọi | Thống kê | POST `api/TongDaiLichSuCuocGoi/GetListThongKeCuocGoi` | `TongDaiCallHistory_GetListThongKeCuocGoi` (HT-AD, HT-KT, DV-AD-TONG) | public | tab thống kê cuộc gọi | Trả thống kê danh sách cuộc gọi theo filter (`ReqGetListThongKe`). | EF | Không truyền `CurrentUser` → không scope theo user (chỉ theo filter), path:43-49 |
| 4 | Lấy lịch sử cuộc gọi theo ids | — | POST `api/TongDaiLichSuCuocGoi/GetListCallHistoryByIds` | `[SBAuthorize()]` (login) | public | — | Trả lịch sử cuộc gọi theo danh sách ids (`ReqGetListByIds`). | EF | Không truyền `CurrentUser` → không scope theo user, path:69-75 |

## Endpoint nội bộ (private / service-to-service)
- `[PrivateSBAuthorize]` `POST GetLichSuCuocGoiOmi(CallHistoryByContactFilterDto)` — service khác lấy lịch sử cuộc gọi omicall theo contact (filter theo contact), có truyền `CurrentUser`. path:56-62

## Quan hệ
- **Gọi sang:** `ITongDaiLichSuCuocGoiService` (toàn bộ logic tạo/lấy/thống kê lịch sử cuộc gọi).
- **Dữ liệu (entities):** lịch sử cuộc gọi tổng đài (call history) — bản ghi theo đơn vị/user, gắn contact.
- **Ripple / đồng bộ:** `TaoLichSuCuocGoi` sinh bản ghi lịch sử → các màn danh sách/thống kê đọc lại; `GetLichSuCuocGoiOmi` cho phép module contact/CSKH đọc lịch sử omicall theo contact.

## Lỗi đã biết / lưu ý kỹ thuật
- `TongDaiLichSuCuocGoiController.cs:25-26` — `TaoLichSuCuocGoi` key role `TongDaiDonVi_TaoLichSuCuocGoi` đã comment, chỉ còn `[SBAuthorize]` rỗng → ai login cũng tạo được lịch sử cuộc gọi.
- `TongDaiLichSuCuocGoiController.cs:43-49,69-75` — `GetListThongKeCuocGoi` và `GetListCallHistoryByIds` KHÔNG truyền `CurrentUser` xuống service → không tự scope theo user/đơn vị, phụ thuộc filter từ client (rủi ro lộ dữ liệu nếu filter không bắt buộc).
