# Luồng — Kết nối Facebook

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc một đơn vị + chi nhánh, có role marketing/admin phù hợp.
- **Tiện ích:** fanpage + bài đăng cần tiện ích **ChatHub** (`TI_ChatHub`); tài khoản quảng cáo cần tiện ích **Facebook** (`TI_Facebook`). Chưa mua tiện ích → không vào được màn tương ứng.
- Đã **đăng nhập Facebook** và cấp quyền (để hệ thống lấy danh sách page / token / tài khoản quảng cáo).
- Có sẵn **nguồn (landing)** để gắn cho fanpage / bài đăng / tài khoản ads.
- Đối với kích hoạt page: gói cước của đơn vị quy định **số page tối đa** được kích hoạt.

## Luồng dùng (kết nối & kích hoạt fanpage)
1. Người dùng mở **Cấu hình fanpage**, đăng nhập Facebook → bấm **Đồng bộ fanpage**.
2. Hệ thống lặp qua tài khoản Facebook (`me/accounts`, có phân trang), với mỗi page: đổi sang token dài hạn, **thêm/cập nhật** fanpage vào hệ thống (page mới mặc định: đang sử dụng, **chưa kích hoạt**), lưu quyền page theo vai trò Facebook, map user ↔ page. Page mới còn được đẩy đi đồng bộ tin nhắn cũ (qua hàng đợi nền).
3. Người dùng mở 1 page → chọn **nguồn (landing)**, bật **đồng bộ tin nhắn** / **đồng bộ bình luận** → **Lưu**.
4. Người dùng bấm **Kích hoạt**:
   - Hệ thống kiểm tra **gói cước**: số page đang kích hoạt phải còn dưới giới hạn.
   - Nếu page đang kích hoạt ở chi nhánh khác → yêu cầu xác nhận; xác nhận thì tắt page bên kia.
   - OK → page chuyển sang **đã kích hoạt**, đăng ký webhook lên server nhận tin Facebook.
5. Từ lúc này, tin nhắn / bình luận của khách trên page chảy về **ChatHub**.
6. Người dùng có thể **Kiểm tra token** bất cứ lúc nào để xác nhận page còn kết nối.

## Luồng dùng (bài đăng)
1. Mở màn **Bài đăng**, chọn page và lọc "đã gắn / chưa gắn nguồn".
2. Hệ thống chỉ trả về bài của page **người dùng có quyền**.
3. Mở 1 bài → bật **sử dụng** + gắn **nguồn (landing)** → lưu (chỉ cập nhật 2 trường này).
4. Xem chi tiết 1 bài: nếu chưa có trong hệ thống, lần đầu mở sẽ kéo từ Facebook về (kèm ảnh/video) và lưu lại.

## Luồng dùng (tài khoản quảng cáo)
1. Mở màn **Tài khoản quảng cáo**, bấm **Thêm tài khoản quảng cáo** (chọn tài khoản Facebook đã kết nối).
2. Hệ thống kéo danh sách tài khoản ads từ Facebook, **thêm/cập nhật** từng tài khoản, lưu quyền; với mỗi tài khoản đẩy đi **đồng bộ ngân sách** (chuỗi: tài khoản → chiến dịch → nhóm quảng cáo → quảng cáo).
3. Mở 1 tài khoản → chọn **nguồn (landing)** → lưu. Ngân sách sẽ được gom về nguồn đó.

## Luồng cấu hình (admin)
- Mua / bật tiện ích ChatHub và Facebook cho đơn vị.
- Cấu hình gói cước (số page tối đa) — quyết định giới hạn kích hoạt fanpage.
- Khai báo **nguồn (landing)** để gắn cho fanpage / bài đăng / tài khoản ads.

## Case biên
- **Đăng nhập Facebook nhưng tài khoản không có page** → đồng bộ trả danh sách rỗng (lưu ý kỹ thuật: code có nhánh trả `null` giữa vòng lặp khi `accounts` rỗng → màn có thể không hiện gì; xem dev.md).
- **Lỗi giữa chừng khi đồng bộ fanpage** → code nuốt lỗi (try/catch rỗng) nên vẫn báo thành công với kết quả rỗng → khách tưởng "đồng bộ xong mà không có page". Cần kiểm tra lại đăng nhập / quyền Facebook.
- **Bật sử dụng page mà thiếu nguồn hoặc không chọn đồng bộ nào** → chặn, không lưu.
- **Kích hoạt khi đã đủ số page gói cước** → chặn "Đơn vị chỉ được kích hoạt tối đa N FanPage!".
- **Kích hoạt page đang active ở chi nhánh khác mà không xác nhận** → trả mã `EXISTED`, không đổi gì cho tới khi xác nhận.
- **Token hết hạn** → Kiểm tra token trả "không kết nối"; phải đăng nhập Facebook lại + đồng bộ lại.
- **Gắn nguồn cho tài khoản ads mà tài khoản không tồn tại** → lưu ý kỹ thuật: validation thiếu chặn dừng nên có thể lỗi hệ thống (xem dev.md), cần chọn đúng tài khoản đang có.
- **Xem chi tiết bài đăng không có ảnh/video đúng cấu trúc** → có thể lỗi do code truy cập phần tử đầu của danh sách media không kiểm tra rỗng (xem dev.md).

## Ví dụ cụ thể có số
Shop B mua gói ChatHub cho phép **3 fanpage** kích hoạt.
- Tài khoản Facebook của shop có 5 page. Đồng bộ → cả 5 page về danh sách, tất cả ở trạng thái "đang sử dụng, chưa kích hoạt".
- Gắn nguồn + bật đồng bộ tin nhắn cho 3 page A, B, C rồi kích hoạt → 3 page active, tin nhắn 3 page về ChatHub.
- Kích hoạt thêm page D → hệ thống chặn "Đơn vị chỉ được kích hoạt tối đa 3 FanPage!".
- Hủy kích hoạt page C → còn 2 active → kích hoạt page D OK (3 active).
- Page A đang active ở chi nhánh HN; nhân viên chi nhánh HCM kích hoạt page A → hệ thống hỏi xác nhận; xác nhận → A tắt ở HN, bật ở HCM.
- Vào màn Tài khoản quảng cáo, Thêm tài khoản → kéo 2 tài khoản ads về; gắn nguồn "Landing Hè 2026" cho tài khoản 1 → ngân sách của tài khoản 1 gom về nguồn đó.

## Hậu điều kiện
- Đồng bộ fanpage **thêm/cập nhật** danh sách page + quyền page + map user-page; page mới kéo tin nhắn cũ về.
- Kích hoạt page **đăng ký webhook** → tin nhắn / bình luận về ChatHub.
- Lưu cấu hình page / bài đăng / tài khoản ads chỉ cập nhật cấu hình (nguồn, bật/tắt đồng bộ), **xóa cache** liên quan.
- Thêm tài khoản quảng cáo khởi động **chuỗi đồng bộ ngân sách** nền (account → campaign → adset → ads).
