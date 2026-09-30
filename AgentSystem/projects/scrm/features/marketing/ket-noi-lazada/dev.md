# Dev — Kết nối Lazada

> ⚠️ **CHƯA HOÀN THIỆN / chưa có controller riêng.** Không tìm thấy `LazadaConnectController`, tiện ích `TI_Lazada`, hay màn cấu hình riêng cho Lazada trong registry SCRM. Trong registry, "Lazada" chỉ xuất hiện như một **giá trị `EcommerceCode`** ở cụm kết nối sàn TMĐT chung (`UnitConnectController`). Chỉ dành cho dev.

## Trạng thái xác minh
- INDEX (`projects/scrm/features/INDEX.md`, dòng Marketing) ghi: **"Kết nối Lazada — Đồng bộ gian hàng/SP Lazada (chưa hoàn thiện) — Trace: —"** (cột Trace để trống).
- Glob registry: **không** có `LazadaConnect*.md` (khác Shopee/TikTok đều có node + controller + tiện ích).
- Grep "Lazada" trong `registry/marketing`: chỉ khớp `UnitConnectController.md` — nhắc Lazada là 1 trong các sàn (Shopee/Tiktok/Lazada) mà cụm ecommerce-connect xử lý chung.
- Không tìm thấy `TI_Lazada` (so với `TI_Shopee`, `TI_Tiktok` tại `SB.Core/Contants.cs`).

## Màn hình & route (kỳ vọng, cần xác nhận)
- Nếu có hỗ trợ, dự kiến dùng chung màn `pages/ecommerce/ecomm-gian-hang-danh-sach` (tham số `EcommerceCode=Lazada`) như Shopee/TikTok — **cần trace FE để xác nhận** Lazada có nằm trong danh sách `EcommerceCode` mà màn này render hay không.

## Endpoint / Controller
- **Không có controller riêng.** Nếu chạy, sẽ tái dùng cụm: `AuthorizationController` (token), `UnitConnectController` (gian hàng + kho + callback), `WarehouseConnectController` (kho), `ProductConnectController` (ghép + đồng bộ tồn) — tất cả proxy `IEcommerceConnectService` → ecommerce-connect gateway ngoài.
- **Điểm chặn thật sự nằm ở gateway:** gateway có hỗ trợ Lazada (OAuth, đọc đơn, đẩy tồn) hay chưa quyết định Lazada dùng được hay không. Nằm **ngoài** repo CRM lõi.

## Entities (nếu dùng cụm chung)
- `Authorization`, `UnitConnect`, `ProductConnect` (cùng cụm với Shopee/TikTok). Không có entity riêng cho Lazada.

## Bug-prone / lưu ý
- **KHÔNG hứa khách dùng Lazada cho tới khi product/dev xác nhận.** Trạng thái INDEX = "chưa hoàn thiện".
- **Việc kết nối phụ thuộc gateway ngoài** — kể cả CRM mở `EcommerceCode=Lazada`, nếu gateway chưa hỗ trợ thì ủy quyền/đồng bộ vẫn gãy.
- Nếu sau này wire Lazada: cân nhắc tái dùng `IEcommerceConnectService` (như Shopee/TikTok) thay vì tạo controller stub mới — tránh lặp lại 2 stub `ShopeeConnect`/`TiktokConnect` đang treo.
- Các known-issue của cụm chung (callback url ghép cứng `UnitConnectService.cs:78-79`, đẩy quantity -1 `ProductConnectService.cs:151,173`, parse kho `Guid.Parse` `:167`) sẽ áp dụng cho Lazada nếu dùng cụm chung.

## File source
- `projects/scrm/features/INDEX.md` (dòng "Kết nối Lazada").
- Registry liên quan (cụm chung, không riêng Lazada): `UnitConnectController.md`, `ProductConnectController.md`, `AuthorizationController.md`, `WarehouseConnectController.md`.
- Tham chiếu luồng tương đương: cẩm nang `ket-noi-shopee/` và `ket-noi-tiktok/`.
