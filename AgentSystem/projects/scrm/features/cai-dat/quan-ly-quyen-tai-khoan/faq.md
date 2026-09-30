> Draft — cần bổ sung từ ticket khách thật.

# FAQ — Quản lý quyền & tài khoản (thiết bị & đăng nhập)

**Q: Nhân viên báo không đăng nhập được trên điện thoại mới, vì sao?**
A: Có thể thiết bị mới đang **chờ duyệt** hoặc đã bị **hủy duyệt**, hoặc nhân viên đã dùng hết số thiết bị đăng nhập được phép. Admin vào màn Quản lý thiết bị đăng nhập để **Duyệt** thiết bị, hoặc cấp thêm slot ở màn Giới hạn đăng nhập.

**Q: Tôi muốn chặn một thiết bị lạ đang đăng nhập tài khoản nhân viên thì làm sao?**
A: Vào **Quản lý thiết bị đăng nhập**, tìm thiết bị đó và bấm **Hủy duyệt** (thu hồi quyền) hoặc **Xóa**. Lưu ý chỉ xóa được thiết bị thuộc đơn vị của bạn.

**Q: Vì sao tôi xóa thiết bị mà báo "Không tìm thấy thiết bị"?**
A: Thiết bị đó không thuộc đơn vị của bạn. Hệ thống chỉ cho phép xóa thiết bị trong cùng đơn vị để tránh tác động chéo giữa các đơn vị.

**Q: Cấp thêm thiết bị đăng nhập cho nhân viên mà báo "vượt quá", là sao?**
A: Mỗi đơn vị có hạn mức tổng số thiết bị đăng nhập đồng thời (theo gói/hợp đồng). Tổng slot bạn cấp cho tất cả nhân viên không được vượt hạn mức này. Nếu cần nhiều hơn, hãy nâng cấp gói hoặc liên hệ hỗ trợ.

**Q: Tôi giảm số thiết bị của một nhân viên thì các máy đang đăng nhập có bị ảnh hưởng không?**
A: Có. Khi bạn sửa giảm hoặc xóa slot, hệ thống **tự đăng xuất** ngay các thiết bị vượt quá số mới của nhân viên đó.

**Q: Tôi thêm slot cho cùng một nhân viên 2 lần thì sao?**
A: Không được. Mỗi nhân viên chỉ có một bản ghi slot trong đơn vị; thêm trùng sẽ báo "Trùng lặp nhân viên, mời chọn nhân viên khác". Bạn hãy **Sửa** bản ghi đã có thay vì thêm mới.

**Q: Tra log đăng nhập nhưng chỉ thấy đơn vị của tôi, không thấy đơn vị khác?**
A: Đúng theo bảo mật: tài khoản thường chỉ xem được log đăng nhập của đơn vị mình. Chỉ tài khoản SuperUser/CSKH của hệ thống mới lọc được log theo đơn vị bất kỳ.

**Q: Vừa đăng nhập xong mà tra log chưa thấy bản ghi?**
A: Log đăng nhập được ghi nền (async) ngay sau khi đăng nhập, nên có độ trễ nhỏ vài giây. Hãy tra lại sau ít phút.

**Q: Tôi sắp xếp lại cột thời gian trong log mà không thay đổi thứ tự?**
A: Hiện màn log luôn hiển thị theo thời gian giảm dần (mới nhất trước) bất kể bạn chọn sắp xếp gì. Đây là giới hạn hiện tại của màn.
