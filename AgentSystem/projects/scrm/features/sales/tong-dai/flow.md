# Luồng — Tổng đài (Voice IP / Sim số / IP mới)

> ⚠️ Luồng khác nhau theo 3 phiên bản. Phần dưới tách rõ. Bản **IP mới (Omicall)** đầy đủ nhất nên mô tả kỹ nhất.

## Tiền điều kiện
- Đơn vị đã mua/được cấp tổng đài (1 trong 3 phiên bản) và đã **khởi tạo + kết nối**.
- **Map user** CRM ↔ user tổng đài để nhân viên gọi được.
- Bản IP: có **số dư** (đã nạp tiền) để thực hiện cuộc gọi.

## Luồng cấu hình (admin)

### Bản (3) IP mới — Omicall
1. **Khởi tạo tổng đài đơn vị** (`KhoiTaoTongDaiIpDonVi`, HT-AD/HT-KT, DV-AD-TONG/CN/MCN) → tạo tenant + user + package + webhook trên Omicall. Trước đó `KiemTraDonViKhoiTao` kiểm tra đã khởi tạo chưa.
2. **Nạp tiền**: `NapTienTongDai` → giao dịch trạng thái **ChoThanhToan** → lấy **QR** (`GetThongTinQrTongDaiDonVi`) → `ThanhToanGiaoDich` (tự thanh toán) **HOẶC** super-admin `DuyetThanhToanNapTienTongDaiSA` (màn TongDaiIpDonViSA) → **DaThanhToan** (cộng số dư). Hủy: `TongDaiDonViHuyThanhToan` / SA hủy → **HuyThanhToan**. `XoaGiaoDichThanhToan` chỉ áp giao dịch còn ChoThanhToan.
3. Cấu hình **đầu số/hotline**, **kịch bản cuộc gọi** (`CRMUnit_CallScipt*`), **kịch bản bấm phím/IVR** (`CRMUnit_*KeyInteraction`), **lời chào** (`CRMUnit_Greeting*` — upload .mp3 / TTS), **nhóm nội bộ** (`CRMUnit_*InternalGroup` + thành viên).
4. **Map user** (`MapUserDonViVoiUserTongDai`), đăng ký đầu số (`DonViDangKyDauSo`), gia hạn/đổi user (tính tiền trước qua `CalculateSoTienKhiThayDoiUser`).

### Bản (2) Sim số — MyHub
1. Quản lý tổng đài đơn vị: `GetTongDaiActive`, `TimTheoDieuKien`, đổi active (`ChangeActiveTongDai/{id}`).
2. Hotline: `GetHotline`, `GetHotlineByIdUserPartner`, xóa hotline.
3. User: tìm/đổi user (fullbooking) `ChangeUser`, xuất Excel `ExportExcelTongDaiSimSo`.

### Bản (1) Voice IP cũ — quản lý cấp hệ thống
1. `TongDaiDonViTaoMoi` / `CapNhat` / `KetNoi` (HT-AD/HT-KT), đăng ký webhook, get balance.
2. Nạp tiền + `TongDaiDuyetSoDuDonVi` (duyệt số dư) / `TongDaiXoaLenhNapTien`.
3. User connective: `TongDaiUserTaoMoi`, `ChangeUserConnective`. Gói dịch vụ: `TongDaiGiaHanGoiDichVu` / `TongDaiNangCapGoiDichVu`.

## Luồng dùng (nhân viên gọi)
1. **Click-to-call**: (2) `MyHub_ClickToCall`; (3) qua tenant Omicall.
2. Hoặc **auto-call** (3): tạo phiên (`TaoPhienCuocGoi`) → hệ thống quay số → khi sale **accept** cuộc gọi → `CapNhatContactKhiAcceptAutoCall` chia/cập nhật contact theo số điện thoại.
3. Cuộc gọi sinh **lịch sử**: (3) `TaoLichSuCuocGoi` / `LayDanhSachCuocGoi`; (2) `GetHistoryByCustomer` / `TimTheoDieuKien`.
4. Xem **thống kê / báo cáo / xuất Excel**.

## Các trường hợp / case biên
- **Đơn vị chưa khởi tạo tổng đài** → không gọi được; `KiemTraDonViKhoiTao` trả "chưa khởi tạo".
- **Nạp tiền IP chưa thanh toán/chưa duyệt** → số dư chưa cộng; giao dịch còn ChoThanhToan, có thể bị xóa.
- **Xóa giao dịch nạp tiền đã thanh toán** → KHÔNG được (chỉ xóa giao dịch còn ChoThanhToan).
- **Upload lời chào không phải .mp3 / file rỗng** → 400 BadRequest.
- **User chưa map tổng đài** → nhân viên không gọi được.
- **Xuất Excel khi service bận** → 503 (`EXPORT_ERROR` / `Service_Unavailable`); lỗi khác → 500. FE phải handle riêng.
- **Lịch sử/thống kê bản IP không scope theo user** (`GetListThongKeCuocGoi`, `GetListCallHistoryByIds`) → trả theo bộ lọc client; nếu filter lỏng có thể thấy dữ liệu rộng.
- **Auto-call / tạo lịch sử cuộc gọi quyền lỏng** → chỉ cần đăng nhập (không phân quyền key) dù có side-effect chia contact.
- **Dùng bản cũ (Switchboard / MyHub CallHistory v1)** → các endpoint đã comment → không hoạt động; phải dùng bản mới.

## Ví dụ cụ thể có số (bản IP mới)
Đơn vị X khởi tạo tổng đài IP, map 10 user sale, đơn giá 50.000đ/user/tháng.
- Gia hạn 10 user 1 tháng: `CalculateSoTienKhiThayDoiUser` tính trước = **500.000đ**.
- `NapTienTongDai` 1.000.000đ → giao dịch ChoThanhToan → quét QR `ThanhToanGiaoDich` → DaThanhToan → số dư +1.000.000đ.
- Tạo phiên auto-call 200 số. Sale An accept 35 cuộc → mỗi cuộc `CapNhatContactKhiAcceptAutoCall` chia 35 contact cho An.
- Cuối ngày xuất Excel `data_callhistory.xlsx`: 35 cuộc gọi của An. Nếu service xuất bận → 503, thử lại.

## Hậu điều kiện
- Khởi tạo/kết nối → tạo tenant + webhook trên đối tác (external).
- Nạp tiền duyệt → cộng số dư tổng đài.
- Cuộc gọi → sinh bản ghi lịch sử (đọc lại bởi danh sách/thống kê/báo cáo + Báo cáo Sales tối ưu sale).
- Auto-call accept → ripple chia contact sang module Contact.
- Cấu hình kịch bản/greeting/user/đầu số → đẩy lên tenant tổng đài (Omicall/partner).
