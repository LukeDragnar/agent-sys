---
controller: TongDaiIpDonViController
module: sales
service: Switchboard
man_hinh_ui: "Tổng đài IP > Cấu hình đơn vị (khởi tạo tổng đài, nạp tiền/thanh toán, đầu số, hotline, kịch bản cuộc gọi, nhóm nội bộ, lời chào/greeting, kịch bản bấm phím, auto-call/phiên gọi)"
muc_dich: "Controller trung tâm của tổng đài IP cấp đơn vị: khởi tạo tenant tổng đài, quản lý nạp tiền & giao dịch thanh toán (QR), đầu số/hotline, kịch bản cuộc gọi (call script), nhóm nội bộ (internal group), lời chào ghi âm (greeting/TTS), kịch bản bấm phím (key interaction) và auto-call (phiên cuộc gọi)."
alias: ["tổng đài IP đơn vị", "khởi tạo tổng đài", "nạp tiền tổng đài", "kịch bản cuộc gọi", "auto call", "greeting tổng đài", "nhóm nội bộ tổng đài"]
auth: mixed
state_machine: [GiaoDich_ChoThanhToan, GiaoDich_DaThanhToan, GiaoDich_HuyThanhToan]
deprecated: false
entities: [TongDaiIpDonVi, GiaoDichTongDai, DauSoTenant, Hotline, CallScript, InternalGroup, Greeting, KeyInteraction, PhienCuocGoi]
stored_procs: []
depends: [ITongDaiIpDonViService, "Omicall/tenant tổng đài IP (external)", "Cổng thanh toán QR", "Contact (CapNhatContactKhiAcceptAutoCall — chia contact cho sale)"]
cross_module_in: ["CRM đơn vị (KhoiTaoTongDaiIpDonVi, KiemTraDonViKhoiTao)", "TongDaiIpDonViSAController (super-admin duyệt/hủy giao dịch nạp tiền)"]
cross_module_out: ["Contact (CapNhatContactKhiAcceptAutoCall: khi sale nhận cuộc gọi auto-call → chia/cập nhật contact theo số điện thoại)", "External tenant tổng đài IP (khởi tạo tenant, đăng ký đầu số, đẩy kịch bản/greeting/key interaction)"]
---

# TongDaiIpDonVi — Tổng đài IP cấp đơn vị (khởi tạo, thanh toán, kịch bản, auto-call)

> Controller lớn (~40 endpoint). Bảng dưới giữ nhóm hành động UI chính; nhóm CRUD cấu hình (call script / internal group / greeting / key interaction) gom 1 dòng/nhóm; auto-call gom riêng.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Khởi tạo tổng đài IP đơn vị | Khởi tạo | POST `KhoiTaoTongDaiIpDonVi` | `TongDaiIpDonVi_KhoiTaoTongDaiIpDonVi` (HT-AD, HT-KT, DV-AD-TONG/CN/MCN) | public | đơn vị chưa khởi tạo | Tạo tenant + user + package + webhook tổng đài cho đơn vị. | EF + external tenant | — |
| 2 | Kiểm tra đơn vị đã khởi tạo | (load) | POST `KiemTraDonViKhoiTao` | `[SBAuthorize()]` (login) | public | mở màn tổng đài IP | Trả trạng thái đơn vị đã khởi tạo tổng đài hay chưa. | EF | — |
| 3 | Get thông tin tổng đài user | (load) | POST `GetInfoTongDaiUser` | `TongDaiIpDonVi_GetInfoTongDaiUser` | public | — | Trả thông tin tổng đài của đơn vị/user. | EF | — |
| 4 | Nạp tiền / giao dịch chờ thanh toán | Nạp tiền | POST `NapTienTongDai`, POST `GetDanhSachGiaoDichChoThanhToan`, POST `XoaGiaoDichThanhToan` | `TongDaiIpDonVi_NapTienTongDai` / `_GetDanhSachGiaoDichChoThanhToan` / `_XoaGiaoDichThanhToan` | public | tab nạp tiền | Tạo giao dịch nạp tiền (trạng thái chờ thanh toán); list/xóa giao dịch chờ. | EF | — |
| 5 | Thanh toán giao dịch + QR + hủy | Thanh toán | POST `ThanhToanGiaoDich`, GET `GetThongTinQrTongDaiDonVi`, POST `TongDaiDonViHuyThanhToan` | `TongDaiIpDonVi_ThanhToanGiaoDich` / `_GetThongTinQrTongDaiDonVi` / `_TongDaiDonViHuyThanhToan` | public | giao dịch ở trạng thái chờ | Thanh toán → đổi state sang đã thanh toán; lấy QR theo `maGiaoDich`; hủy → state hủy. | EF + cổng QR | — |
| 6 | Lịch sử thanh toán đơn vị | Lịch sử | POST `GetLichSuThanhToanDonVi` | `TongDaiIpDonVi_GetLichSuThanhToanDonVi` | public | tab lịch sử nạp tiền | Trả lịch sử giao dịch thanh toán của đơn vị. | EF | — |
| 7 | Danh sách đầu số (sb/tenant) + dropdown | (load) | POST `CRMUnit_GetListDauSoSb`, POST `CRMUnit_GetListDauSoTenant`, GET `DropDownDauSoTenant` | `_CRMUnit_GetListDauSoSb` / `_CRMUnit_GetListDauSoTenant` / dropdown `[SBAuthorize]` | public | màn đầu số | Trả danh sách đầu số sandbox / tenant / dropdown. | EF + tenant | — |
| 8 | Danh sách tổng đài đơn vị | (load) | POST `GetDanhSachTongDaiDonVi` | `TongDaiIpDonVi_GetDanhSachTongDaiDonVi` | public | màn quản lý tổng đài | Trả danh sách tổng đài của đơn vị. | EF | — |
| 9 | Hotline (get/update) | Lưu hotline | POST `CRMUnit_GetInfoHotLine`, POST `CRMUnit_UpdateHotline` | `_CRMUnit_GetInfoHotLine` / `_CRMUnit_UpdateHotline` | public | tab hotline | Đọc/cập nhật thông tin đầu số tenant (hotline). | EF + tenant | — |
| 10 | Kịch bản cuộc gọi (list/add/update/delete) | Lưu kịch bản | POST `CRMUnit_CallSciptList`, `CRMUnit_CallSciptAdd`, `CRMUnit_CallSciptUpdate`, `CRMUnit_CallSciptDelete` | `_CRMUnit_CallScipt*` (HT-AD, HT-KT, DV-AD-TONG/CN/MCN) | public | tab kịch bản cuộc gọi | CRUD call script (đẩy lên tenant tổng đài). | tenant | — |
| 11 | Nhóm nội bộ (CRUD + thành viên) | Lưu nhóm | POST `CRMUnit_GetInternalGroup`, `CRMUnit_AddInternalGroup`, `CRMUnit_UpdateInternalGroup`, `CRMUnit_DeleteInternalGroup`, `CRMUnit_AddMemberInternalGroup`, `CRMUnit_RemoveMemberInternalGroup` | `_CRMUnit_*InternalGroup` / `_*MemberInternalGroup` | public | tab nhóm nội bộ | CRUD internal group + thêm/xóa nhân viên trong nhóm. | tenant | — |
| 12 | Lời chào / Greeting (list/delete/TTS/upload) | Upload | POST `CRMUnit_GreetingList`, `CRMUnit_GreetingDelete`, `CRMUnit_GreetingUploadTextToSpeech`, `CRMUnit_GreetingUpload` | `_CRMUnit_Greeting*` | public | tab lời chào | List/xóa/TTS/upload file ghi âm. Upload **chỉ chấp nhận .mp3**, file rỗng → BadRequest. | tenant + file | Validate: chỉ `.mp3`, file null/length=0 → 400 |
| 13 | Kịch bản bấm phím / Key interaction (get/create/update/delete) | Lưu | POST `CRMUnit_GetKeyInteraction`, `CRMUnit_CreateKeyInteraction`, `CRMUnit_UpdateKeyInteraction`, `CRMUnit_DeleteKeyInteraction` | `_CRMUnit_*KeyInteraction` | public | tab bấm phím (IVR) | CRUD kịch bản bấm phím IVR. | tenant | — |
| 14 | Auto-call / phiên cuộc gọi (CRUD phiên) | Tạo phiên | POST `GetDanhSachPhien`, `TaoPhienCuocGoi`, `CapNhatPhienCuocGoi`, `GetChiTietPhien`, `XoaPhienCuocGoi` | `[SBAuthorize()]` (login, KHÔNG key role) | public | màn auto-call | CRUD phiên cuộc gọi auto-call cho `CurrentUser`. | EF | Toàn bộ nhóm auto-call chỉ `[SBAuthorize]` rỗng (không phân quyền key) |
| 15 | Cập nhật contact khi accept auto-call | (ngầm) | POST `CapNhatContactKhiAcceptAutoCall` | `[SBAuthorize()]` (login) | public | khi sale nhận auto-call | Theo `soDienThoai` → chia/cập nhật contact cho sale nhận cuộc gọi. **Ripple sang Contact**. | EF + Contact | Chỉ `[SBAuthorize]` rỗng nhưng có side-effect chia contact |

## Quy tắc nghiệp vụ (gating / state giao dịch nạp tiền)
1. Tạo giao dịch (`NapTienTongDai`) → state **ChoThanhToan**.
2. `ThanhToanGiaoDich` (đơn vị tự thanh toán qua QR) HOẶC `DuyetThanhToanNapTienTongDaiSA` (super-admin duyệt, ở `TongDaiIpDonViSAController`) → state **DaThanhToan**.
3. `TongDaiDonViHuyThanhToan` / super-admin `HuyDuyetThanhToanToanTongDaiSA` → state **HuyThanhToan**.
4. `XoaGiaoDichThanhToan` chỉ áp giao dịch còn **ChoThanhToan**.
→ Số tiền/duyệt là blueprint cho trace-agent mô phỏng (nạp X → chờ → duyệt → cộng số dư tenant).

## Endpoint nội bộ (private / service-to-service)
- Không có `[PrivateSBAuthorize]` trong controller này; tương tác external tenant đi qua service `ITongDaiIpDonViService` (gọi API Omicall/tenant), không phải endpoint inbound.

## Quan hệ
- **Gọi sang:** `ITongDaiIpDonViService` → external tenant tổng đài IP (Omicall): khởi tạo tenant, đầu số, đẩy call script/greeting/key interaction; cổng QR thanh toán.
- **Dữ liệu (entities):** tổng đài IP đơn vị, giao dịch nạp tiền, đầu số/hotline, call script, internal group, greeting, key interaction, phiên auto-call.
- **Ripple / đồng bộ:** `CapNhatContactKhiAcceptAutoCall` chạm module **Contact** (chia contact cho sale theo số điện thoại); thanh toán giao dịch liên kết super-admin duyệt ở `TongDaiIpDonViSAController`.

## Luồng xuyên module
- Khởi tạo: CRM đơn vị → `KhoiTaoTongDaiIpDonVi` → tạo tenant + user + package + webhook trên tổng đài IP (external).
- Nạp tiền: đơn vị → `NapTienTongDai` (chờ) → QR `ThanhToanGiaoDich` HOẶC super-admin `DuyetThanhToanNapTienTongDaiSA` → cộng số dư.
- Auto-call: tạo phiên → khi sale accept cuộc gọi → `CapNhatContactKhiAcceptAutoCall` → chia contact sang Contact module.

## Lỗi đã biết / lưu ý kỹ thuật
- `TongDaiIpDonViController.cs:503-572` — TOÀN BỘ nhóm auto-call (`GetDanhSachPhien`, `TaoPhienCuocGoi`, `CapNhatPhienCuocGoi`, `GetChiTietPhien`, `XoaPhienCuocGoi`, `CapNhatContactKhiAcceptAutoCall`) chỉ `[SBAuthorize()]` rỗng — không có permission key → bất kỳ user login nào thao tác được, dù có side-effect chia contact.
- `TongDaiIpDonViController.cs:425-440` — `CRMUnit_GreetingUpload` validate chặt: chỉ `.mp3`, file null/rỗng → 400 BadRequest.
- `TongDaiIpDonViController.cs:52,53` — `KiemTraDonViKhoiTao` chỉ `[SBAuthorize()]` (login) — đúng vì cần check trước khởi tạo.
