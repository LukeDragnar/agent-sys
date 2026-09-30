# Dev — Sale DMS

> ⚠️ **KHÔNG trace được trong snapshot hiện tại.** Đây là ghi chú điều tra, không phải tài liệu kỹ thuật đầy đủ.

## Trạng thái trace (quan trọng)
- Trace trong `features/INDEX.md` ghi `SaleDMS_*` (giả định là prefix MaTinhNang/quyền).
- **Kết quả quét nguồn (2026-06-20):**
  - BE `D:\PROJECT\Git\_SourceForCode\Group2\scrm`: `grep -rl "SaleDMS"` → **0 file**. `grep -rho "SaleDMS[A-Za-z_]*"` → **0 key**.
  - FE `D:\PROJECT\Git\_SourceForCode\Group2\scrm_frontend`: `grep -rli "saledms|sale-dms"` → **0 file**; `grep -rho "SaleDMS[A-Za-z_]*"` → **0 key**; không có route DMS trong `pages/`.
  - Registry `projects/scrm/registry/`: không có node nào chứa `SaleDMS`. (`DMS` chỉ khớp `DMSanPham` = danh mục sản phẩm, KHÔNG liên quan.)
- ⇒ **Sale DMS không có controller/màn hình/permission key trong nguồn đang quét.** Nguồn duy nhất là **bảng `Page` (menu) trên staging** (như INDEX nêu).

## Giả thuyết (cần product/dev xác nhận)
1. **Sản phẩm/ứng dụng riêng**: Sale DMS là app field-sales tách repo (không nằm trong solution `SCRM.sln` đang quét).
2. **Đang phát triển / placeholder**: menu đã khai báo nhưng code chưa merge vào snapshot này.
3. **Menu gom màn có sẵn**: nếu thực chất chỉ là view "đi tuyến" trên các màn Đơn hàng/Kho/Khách hàng hiện có → trace thật phải trỏ về các controller đó (DonHangLogistic, KhoChiTiet, KhachHang...), KHÔNG phải `SaleDMS_*`.

## Việc cần làm để hoàn thiện node này
- Truy bảng `Page` staging (READ-ONLY, IdDonVi `CB720173-CC95-4EBD-96B0-FAF550D8531E`): `SELECT Ten, Url, MaTinhNang FROM Page WHERE MaTinhNang LIKE 'SaleDMS%' OR Ten LIKE N'%DMS%'` để biết menu thật trỏ vào URL/MaTinhNang nào → từ đó truy ra repo/controller.
- Hỏi product: Sale DMS là sản phẩm riêng hay module trong SCRM? Repo nào?

## Bug-prone / lưu ý
- **Đừng giả định Sale DMS đã có** trong cẩm nang khách: chưa kiểm chứng được. Mọi mô tả nghiệp vụ trong `summarize/flow/faq` đều gắn cờ "cần product xác nhận".
- Nếu sau này tìm thấy code: tạo lại node registry chuẩn + cập nhật 4 file feature này theo hành vi thật.

## File source (path:line)
- Không có. (Đã quét toàn bộ `scrm` + `scrm_frontend`, không khớp.)
