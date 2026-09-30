# GridColumn trùng `field` → LƯU CẤU HÌNH CỘT CHẾT (save config die)

> Bug-sự-cố thật **2026-07-24**, task **1046** ("Ngày muốn nhận hàng"). Fix commit FE `3532463f1` (haihnPS): comment bỏ 1 `GridColumn` thừa. Lọt cả review + trace → tới tay user mới lộ.

## Hiện tượng
Màn **đơn sale**: bấm **Lưu cấu hình cột** → **die** (không lưu được config cột). Xuất hiện sau khi thêm cột ngày (`ngayMuonNhanHang` / sắp xếp lại nhóm cột ngày giao).

## Root cause
Mảng cột trong `src/app/pages/sale/don-hang-sale/don-hang-sale.component.ts` (~dòng 813) khai **2 `GridColumn` cùng `field: 'giaoHangNgayGiaoHang'`**. Lưu cấu hình cột **key theo `field`** → 2 cột trùng key → collision → chết. Cột bị nhân đôi khi refactor mảng cột (config-form-ui 23/07 "fix dup control render — gỡ 4 block old-style trùng" + task 1046 chèn cột ngày).

## Vì sao REVIEW + TRACE LỌT (bài học QA)
1. **Sai vùng soi:** review 1046 soi **field MỚI** (`ngayMuonNhanHang` bind/tooltip/view-mode), KHÔNG soi cột **anh em** `giaoHangNgayGiaoHang` bị nhân đôi.
2. **Diff không thấy trùng tầm MẢNG:** cả 2 `GridColumn` **hợp lệ khi đứng riêng** (không lỗi convention/syntax). Đọc theo hunk diff không lộ `field` lặp ở đâu đó trong mảng ~100 dòng.
3. **Chỉ vỡ RUNTIME "save config":** trace 1046 mô phỏng hiển thị/bind giá trị ngày, KHÔNG có case "bấm Lưu cấu hình cột" → không đụng → không lộ.

## Rút ra (đã đưa vào `../qa-code.md` §12)
- Đụng mảng cột/list có key (`GridColumn[]`, columns, `field`/`key`) → **grep TOÀN mảng tìm key TRÙNG**, không chỉ đọc dòng thêm.
- Đụng column config → trace **BẮT BUỘC** case **"Lưu cấu hình cột"** (không chỉ hiển thị/bind).
- Lớp bug **"duplicate key ở config-list, chỉ chết khi save"** → review-diff + trace-theo-acceptance **không tự bắt** nếu checklist không ép soi. Liên quan họ "1 field/cột bị nhân đôi" khi refactor mảng cột.
