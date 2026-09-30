---
controller: LogPartnerConnectController
module: marketing
service: PartnerConnect
man_hinh_ui: "Marketing > Kết nối đối tác > Lịch sử gửi (xem log data đã gửi sang TingTong / Bravo; gửi lại data Bravo bị lỗi)."
muc_dich: "Xem lịch sử (log) đồng bộ data sang đối tác TingTong/Bravo theo bộ lọc + grid, và gửi lại thủ công các bản data Bravo gửi lỗi."
alias: ["LogPartnerConnect", "lịch sử gửi đối tác", "log TingTong", "log Bravo", "gửi lại Bravo", "resend bravo"]
auth: internal
state_machine: []
deprecated: false
entities: [LogPartnerConnect, DataBravoError]
stored_procs: []
depends:
  - ILogPartnerConnectService
  - IDataBravoErrorService
cross_module_in: []
cross_module_out: ["ResendDataManualBravo trigger gửi lại data sang Bravo (API ngoài)"]
---

# LogPartnerConnect — Lịch sử gửi đối tác + gửi lại Bravo lỗi

> Màn lịch sử (grid + filter) cho data đã gửi sang đối tác. 2 nguồn log: TingTong (`GetDSLogTingTong`) và Bravo (`GetDSLogBravo`). `ResendDataManualBravo` cho phép gửi lại thủ công các bản Bravo lỗi (đi qua `IDataBravoErrorService`). Tất cả chỉ `[SBAuthorize]` trống (key role gốc HT-AD/DV-AD-TONG/HT-CSKH đã comment).

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | DS log TingTong | Tab "TingTong", grid | `POST api/LogPartnerConnect/GetDSLogTingTong` | `[SBAuthorize]` (key đã comment) | internal | màn lịch sử | Trả grid log gửi TingTong theo `LogPartnerFilterDto` + đơn vị. | EF (grid filter) | Key `LogPartnerConnect_GetDSLogTingTong` (HT-AD,DV-AD-TONG,HT-CSKH) đã comment (L23). |
| 2 | DS log Bravo | Tab "Bravo", grid | `POST api/LogPartnerConnect/GetDSLogBravo` | `[SBAuthorize]` | internal | màn lịch sử | Trả grid log gửi Bravo theo `LogBravoFilterDto` + đơn vị. | EF (grid filter) | — |
| 3 | Gửi lại data Bravo lỗi | Nút "Gửi lại" trên dòng lỗi | `POST api/LogPartnerConnect/ResendDataManualBravo` | `[SBAuthorize]` | internal | dòng log Bravo trạng thái lỗi | Gửi lại các bản data lỗi sang Bravo (qua `IDataBravoErrorService`). | EF + API ngoài | — |

## Quan hệ
- **Gọi sang:** `ILogPartnerConnectService` (đọc log), `IDataBravoErrorService` (resend).
- **Dữ liệu (entities):** bản ghi log gửi đối tác + bản lỗi Bravo.
- **Ripple/đồng bộ:** Resend gọi lại API Bravo → sinh log mới.

## Lỗi đã biết / lưu ý kỹ thuật
- `LogPartnerConnectController.cs:23-24` — key quyền `LogPartnerConnect_GetDSLogTingTong` bị comment; cả 3 endpoint dùng `[SBAuthorize]` trống → bất kỳ user đăng nhập xem log/gửi lại được.
