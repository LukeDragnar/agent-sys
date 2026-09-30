# Cờ "Yêu cầu xuất hóa đơn" cho đơn logistic — Tổng quan

> Cột `IsYeuCauXuatHoaDon` (bool?) trên **đơn logistic** (`DonHangLogistic`) điều khiển việc xuất HĐĐT + nguồn thông tin người mua. Áp dụng cho khách dùng **MISA**. Bối cảnh pháp lý: **NĐ 254/2026/NĐ-CP** (hiệu lực 1/7/2026).

## Cờ 3 trạng thái

| Giá trị | Ý nghĩa | Nguồn thông tin người mua khi xuất |
|---|---|---|
| **`null`** | Đơn CŨ / tenant chưa config | Đơn thường: coi như **true** (xuất theo TT khách). Ecom: coi như **false**. |
| **`true`** | Có yêu cầu xuất | Info-first: TT thuế → (đủ field theo ĐƠN+KHÁCH) TT khách → KH mặc định. Thiếu → theo Config B |
| **`false`** | Không yêu cầu | **Ecom**: theo config `EcomLayTheoThongTinDon` (theo đơn / KH mặc định) · **Đơn thường**: KH mặc định |

- Đơn thường MỚI mặc định `= true` (Config C0 `IsYeuCauXuatHoaDonMacDinh`, default true). Set lúc tạo đơn.
- Đơn **ecom**: cờ set `= false` NGAY lúc tạo (ecom chỉ true/false, không để null); khi sàn gửi invoice-data → `= IsRequested ?? false`.
- 🆕 **(T800/B318, product 29/07/2026) SHOP SỬA TAY ĐƯỢC cờ của đơn sàn** — mô tả cũ "sàn thắng, KHÔNG cho sửa tay" đã HẾT ĐÚNG:
  - Cột mới **`DonHangThongTinThue.TMDTKhachHangYeuCau`** = tín hiệu GỐC của sàn (`is_requested`), CHỈ webhook ecom ghi.
    `KhachHangYeuCau` vẫn là cờ HIỆU LỰC lúc xuất. **2 cột lệch nhau = shop đã sửa tay ⇒ webhook sàn sau KHÔNG đè.**
  - **Sàn báo CÓ yêu cầu (`TMDT... = true`) → KHÓA, không cho bỏ tick** (yêu cầu thật của khách) — chặn ở **cả FE lẫn API**; nhưng **vẫn cho cập nhật thông tin thuế**.
  - Sàn báo KHÔNG / chưa báo → shop chỉnh thoải mái.
  - 🔴 **HÀNH VI CỐ Ý, đừng "sửa"**: shop khai thông tin thuế mà **không tick** ô yêu cầu → HĐ **vẫn xuất theo KH mặc định**. Cờ = tín hiệu sàn + thao tác tick có chủ đích, hệ thống KHÔNG tự suy diễn từ "có thông tin thuế". Thấy triệu chứng "nhập MST rồi mà vẫn ra khách vãng lai" → **kiểm tra shop đã tick chưa** trước khi kết luận lỗi code.
  - Cờ đơn sàn **KHÔNG** ăn theo Config C0 (giữ nguyên: yêu cầu xuất HĐ của đơn sàn do sàn request).
- ⚠️ **Khi XUẤT, ecom KHÔNG tin cờ đơn** — lấy tín hiệu THẬT của sàn `KhachHangYeuCau` (ở `DonHangThongTinThue`). Cờ đơn ecom chỉ để hiển thị/enqueue.
- 🆕 **Cờ KHÔNG chặn xuất** — chỉ quyết NGUỒN thông tin người mua. Job auto vẫn xuất mọi đơn (xem "Điểm hành vi").

## Các cấu hình liên quan

| Config | Ở đâu | Ý nghĩa |
|---|---|---|
| **C0** `IsYeuCauXuatHoaDonMacDinh` | Cấu hình OrderLogistic | Default cờ khi tạo đơn thường mới (true) |
| **EcomLayTheoThongTinDon** (bool) | Cấu hình Invoice | Đơn ecom không yêu cầu: `true` lấy theo TT đơn (Tên/Địa chỉ/SĐT) · `false` dùng KH mặc định. Radio (🆕 **default FALSE** — khớp logic cũ ecom→KH mặc định) |
| **A** `RequiredFieldsLayTheoDon` | Cấu hình Invoice | Field bắt buộc (kiểm CẢ đơn + hồ sơ khách) mới dùng TT khách. Default [Tên, Địa chỉ]. Options: Tên/🆕Địa chỉ(chọn được)/SĐT/🆕MST |
| **B** `XuLyKhiThieuThongTin` | Cấu hình Invoice | 🆕 **3 lựa chọn**: `MacDinh` (KH mặc định) / `BaoLoi` (dừng, báo lỗi) / `MotPhanMacDinh` (field chọn lấy từ KH mặc định qua `FieldsLayTuMacDinh`, còn lại theo đơn+khách) |
| 🆕 `LoaiDonKhongXuatPhiShip` | Cấu hình OrderLogistic | Blocklist loại đơn (UnitCode) KHÔNG xuất phí ship (OR với công tắc global `IsKhongXuatHDDT_PhiGiaoHang`) |

> UI cấu hình A/B: màn **config-invoice** dạng **waterfall ①②③** (đọc trên xuống = thứ tự xử lý). 🆕 "Địa chỉ" giờ **hiện & chọn được** ở Config A (không còn ẩn cứng); thêm MST.

## Điểm hành vi quan trọng

- 🆕 **KHÔNG còn gate auto**: gate cũ `IsYeuCauXuatHoaDon == false → skip` đã **GỠ BỎ**. Cờ KHÔNG chặn xuất — **mọi đơn** vẫn tự tạo nháp / vào hàng đợi auto-publish. Cờ chỉ quyết NGUỒN TT người mua.
- **Địa chỉ vẫn bắt buộc**: `GetInfoCustomer` / `FillBuyerMotPhanMacDinh` throw khi Tên hoặc Địa chỉ VẪN rỗng (cả đơn+khách+mặc định). Địa chỉ rỗng "rác" (`", , ,"`) được lọc → coi như trống.
- **Đủ field = check ĐƠN + HỒ SƠ KHÁCH** (không chỉ đơn): đơn trống field nhưng khách có → vẫn lấy TT khách (case 2), không rơi mặc định oan. Thiếu THẬT (cả 2) → Config B.
- 🆕 **Config B `MotPhanMacDinh`**: dựng người mua theo đơn+khách rồi GHI ĐÈ field được chọn bằng KH mặc định (case 4).
- **Hiển thị cờ** (Có/Không/—) trên list: kế toán, thu-kho, sale (cột chuyển xuống **cuối, sau adId**). Website KHÔNG làm. Đơn cũ hiện "—" tới khi re-index.
- 🆕 **Viettel buyerCode**: HĐ Viettel map `buyerCode` (ngang MISA `AccountObjectCode`); bật `IsSuDungMaKhachHangMacDinh` → override = `MaKhachHangMacDinh`.
- 🆕 **Phí ship theo loại đơn**: config `LoaiDonKhongXuatPhiShip` (UnitCode blocklist) — OR với công tắc global.

## Xem thêm
- Luồng chi tiết + sơ đồ: [flow.md](flow.md)
- Kỹ thuật (file:line, DTO, projection): [dev.md](dev.md)
