> Draft — cần bổ sung từ ticket khách thật.

# FAQ — Quản lý trang & giao diện

**Q: "Trang (Page)" ở đây là gì?**
A: Là các **màn hình** của hệ thống được khai báo và tổ chức dạng cây (trang cha – trang con). Danh sách này dùng để dựng menu và quyết định ai được vào màn nào theo role.

**Q: Tôi gán quyền cho role rồi mà user vẫn không thấy một màn?**
A: Ngoài phân quyền chức năng, màn đó còn phải là một **trang (Page)** được gán đúng role. Kiểm tra: trang đó đã khai báo chưa, đã gán cho role của user chưa. Nếu vừa xóa/sửa trang, thử để user đăng nhập lại cho menu nạp mới.

**Q: Sau khi sửa một trang, menu vẫn hiện như cũ?**
A: Thao tác **Xóa** trang sẽ tự làm mới cache menu, nhưng **Sửa** trang không chắc tự dọn cache ở mọi nơi. Nếu menu chưa cập nhật, thử đăng nhập lại; nếu vẫn vậy, báo dev kiểm tra cache menu.

**Q: Tôi cần thêm nhiều màn mobile cùng lúc, có cách nhập hàng loạt không?**
A: Có. Trong **Quản lý Page Mobile**, tải **file mẫu** về, điền theo cột (đường dẫn, tên hiển thị, mã role, cờ không import) rồi **Import**. File phải có ít nhất một dòng hợp lệ, nếu rỗng sẽ báo lỗi.

**Q: Tải file mẫu import nhưng lại ra file giống file backup?**
A: Đúng, hiện file mẫu và file backup page mobile **trùng tên** (`PageMobile_Backup.xlsx`). Hãy chú ý bạn đang tải từ nút nào (Tải file mẫu hay Backup) để dùng đúng file.

**Q: Vì sao có hai chỗ Page Mobile?**
A: Một bản **chỉ đọc** dành cho app mobile gọi để lấy danh sách màn được phép; một bản **quản trị** để thêm/sửa/xóa, import, backup. Muốn chỉnh dữ liệu, dùng bản quản trị.

**Q: App mobile tự biết hiển thị màn nào cho từng người thế nào?**
A: Khi đăng nhập, app gọi hệ thống lấy danh sách **key màn** mà user được phép theo role; danh sách này được mã hóa khi gửi về client. Vì vậy mỗi user thấy bộ màn khác nhau theo quyền.

**Q: Xóa một trang có làm mất dữ liệu của khách không?**
A: Xóa trang chỉ gỡ **màn/menu** khỏi cấu trúc và làm mới menu; nó không xóa dữ liệu nghiệp vụ của khách. Tuy nhiên user sẽ không còn thấy/vào màn đó nữa, nên cân nhắc trước khi xóa.
