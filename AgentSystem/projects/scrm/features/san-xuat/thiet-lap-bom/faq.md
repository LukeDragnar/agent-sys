> Draft — cần bổ sung từ ticket khách thật. Module Sản Xuất còn sơ khai (level 1).

# FAQ — Thiết lập BOM (định mức)

**Q: BOM là gì, để làm gì?**
A: BOM (định mức / công thức) là **bản mô tả cách sản xuất một sản phẩm**: cần nguyên vật liệu nào, mỗi loại bao nhiêu, qua các công đoạn nào. Khi sản xuất, mỗi lệnh sẽ dựa vào BOM để biết phải xuất nguyên liệu gì và nhập thành phẩm gì. Không có BOM thì không tách được lệnh sản xuất.

**Q: Một sản phẩm có thể có nhiều BOM không?**
A: Có. Một sản phẩm có thể có nhiều công thức khác nhau, nhưng **chỉ một cái được đặt làm mặc định**. Khi bạn đặt một BOM làm mặc định, các BOM khác cùng sản phẩm sẽ tự bỏ cờ mặc định.

**Q: "Nguyên vật liệu" và "Kết quả" trong một bước khác nhau thế nào?**
A: **Nguyên vật liệu** là đầu vào của bước (sẽ bị xuất kho khi chạy), **Kết quả** là đầu ra của bước (sẽ được nhập kho). Đầu ra của bước trước có thể trở thành đầu vào của bước sau (gọi là bán thành phẩm).

**Q: Tôi sửa và lưu lại quy trình, các bước cũ có bị mất không?**
A: Khi bạn bấm **Lưu quy trình**, hệ thống **xóa toàn bộ bước và nguyên liệu cũ rồi tạo lại** theo những gì đang hiển thị. Vì vậy hãy đảm bảo bảng đang hiển thị là đầy đủ và đúng trước khi lưu. Các lệnh sản xuất đã tách trước đó **không bị ảnh hưởng** (chúng dùng bản đã sao chép tại thời điểm tách).

**Q: Tôi đã sửa BOM rồi, sao lệnh sản xuất đang chạy vẫn theo định mức cũ?**
A: Đúng vậy — lệnh sản xuất "chụp lại" quy trình từ BOM **tại thời điểm tách lệnh**. Sửa BOM sau đó không hồi tố lên các lệnh đã tạo. Muốn áp định mức mới, hãy tách lệnh mới.

**Q: "Sao chép BOM" tạo ra cái gì?**
A: Tạo một **bản sao đầy đủ** (cả quy trình + nguyên liệu) với mã mới và tên có hậu tố "- Copy". Tiện khi muốn làm công thức biến thể mà không sửa BOM gốc.

**Q: Công đoạn (danh mục) dùng để làm gì?**
A: Danh mục **Công đoạn** giúp đặt tên/chuẩn hóa các bước trong quy trình (ví dụ: Pha chế, Đóng gói, Dán nhãn). Bạn khai báo trước rồi dùng khi dựng quy trình BOM.

**Q: "% Sai lệch với định mức" để làm gì?**
A: Đó là phần dung sai cho phép so với định mức chuẩn (ví dụ hao hụt nguyên liệu khi sản xuất). Ý nghĩa chi tiết cách áp dụng cần product xác nhận.
