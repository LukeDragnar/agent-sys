---
controller: TkNganHangHeThongController
module: ke-toan
service: WebhookBank
man_hinh_ui: "Cấu hình > Quản lý tài khoản ngân hàng chi nhánh (TienIch 'TKNganHangController'). CRUD + chọn mặc định tài khoản NH dùng thu/chi (Payment.API)."
muc_dich: "Quản lý tài khoản ngân hàng của chi nhánh/đơn vị (thu/chi, kết nối online/terminal, set mặc định) + endpoint private lấy theo đơn vị/chi nhánh cho service khác."
alias: ["tài khoản ngân hàng", "ngân hàng chi nhánh", "set ngân hàng mặc định", "TK ngân hàng hệ thống"]
auth: mixed
state_machine: []
deprecated: false
entities: [TkNganHangHeThong]
stored_procs: []
depends: [TkNganHangHeThongService]
cross_module_in: [FE cấu hình ngân hàng, QrcodeBankController (lấy TK theo điều kiện), service nội bộ (private)]
cross_module_out: []
---

# TkNganHangHeThongController — Quản lý tài khoản ngân hàng chi nhánh

> Controller-level `[SBAuthorize()]` (yêu cầu đăng nhập). CRUD + set mặc định + nhiều endpoint lấy danh sách theo chi nhánh/đơn vị. Có endpoint **private** (`[PrivateSBAuthorize]`) cho service khác và **1 endpoint `[AllowAnonymous]` `TimTheoTerminalId/{terminalId}`** → đây là điểm cần lưu ý bảo mật (phơi thông tin TK ngân hàng theo TerminalId không cần đăng nhập). `auth: mixed`.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện | Lưới NH | `POST .../TimTheoDieuKien` | `TkNganHangHeThong_TimTheoDieuKien` | internal | — | `TimTheoDieuKienAsync`. | **EF** `TkNganHangHeThong` | — |
| 2 | Thêm / Sửa / Xóa | nút CRUD | `POST .../ThemMoi` · `POST .../CapNhatThongTin` · `POST .../Xoa/{id}` | `TkNganHangHeThong_ThemMoi` / `_CapNhatThongTin` / `_Xoa` | internal | Xóa: chặn nếu `KiemTraNganHangDaSuDung` | CRUD; Xóa kiểm tra đã sử dụng → chặn. | **EF** `TkNganHangHeThong` | — |
| 3 | Set mặc định | nút "Mặc định" | `GET .../SetMacDinh/{id:Guid}` | `TkNganHangHeThong_SetMacDinh` | internal | — | Đặt TK làm mặc định cho `CurrentUser`. | **EF** | — |
| 4 | Lấy theo loại / chi nhánh / đơn vị | dropdown | `GET .../GetListNganHangByType/{type}` · `GetListTaiKhoanByChiNhanh[_V2]` · `GetListTaiKhoanByDonVi` · `GetThongTinNganHangByIdChiNhanh` | `_GetListNganHangByType` / (login) | internal | — | Trả danh sách TK theo điều kiện. | **EF** | — |
| 5 | NH thu/chi mặc định + kiểm tra kết nối online | (nội bộ UI) | `GET .../GetNganHangThuMacDinh` · `GetNganHangChiMacDinh` · `CheckKetNoiOnline/{id}` · `CheckKetNoiThuOnline/{idNganHang}` | (login `[SBAuthorize]`) | internal | — | Lấy NH mặc định (thu=0/chi=1); kiểm tra TK có kết nối terminal online. | **EF** + service | — |
| 6 | Tìm theo Id | (mở chi tiết) | `GET .../TimTheoId/{id:Guid}` | `_TimTheoId` | internal | non-admin chỉ xem TK cùng chi nhánh | Lọc theo chi nhánh nếu không phải admin. | **EF** | — |

## Endpoint nội bộ (private / service-to-service)
- [internal `[PrivateSBAuthorize]`] `POST GetAllByDonViPrivateAsync`, `POST GetByIdPrivateAsync`, `POST GetTkNganHangByIdChiNhanhs` — service khác lấy TK ngân hàng theo đơn vị/chi nhánh.
- **[anonymous]** `GET TimTheoTerminalId/{terminalId}` — `[AllowAnonymous]` (comment đè `[PrivateSBAuthorize]`) → trả thông tin TK ngân hàng theo TerminalId KHÔNG cần auth.

## Quan hệ
- **Gọi sang:** `TkNganHangHeThongService` (CRUD + truy vấn).
- **Dữ liệu (entities):** `TkNganHangHeThong` (BankCode/SoTaiKhoan/TerminalId/IsKetNoiTerminal/chi nhánh).
- **Ripple / đồng bộ:** `QrcodeBankController.GetTaiKhoanNganHang` đọc bảng này để chọn TK thu mặc định khi tạo QR; `BankSendController` map TerminalId → STK.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `WebhookBank/Payment.API/Controllers/TkNganHangHeThongController.cs:184-191` — **`[AllowAnonymous] TimTheoTerminalId/{terminalId}`** (đè lên `[PrivateSBAuthorize]` đã comment) → **lỗ hổng phơi data**: bất kỳ ai đoán/biết TerminalId có thể tra thông tin tài khoản ngân hàng (STK/chủ TK) mà không cần đăng nhập. Nên đổi sang `[PrivateSBAuthorize]`.
- Controller `[SBAuthorize()]` cấp class + permission key chi tiết từng method (chuẩn). `TimTheoId` lọc chi nhánh cho non-admin (L97-99).
