> Draft — cần bổ sung từ ticket khách thật.

# FAQ — Cấu hình giá & chiết khấu

**Q: "Mặt hàng mặc định" là gì, dùng để làm gì?**
A: Đó là bộ món (sản phẩm/dịch vụ/combo) bạn khai báo trước cho từng khu vực/bàn. Khi thu ngân chọn bàn ở màn bán hàng, hệ thống tự bơm sẵn các món này vào đơn để lên đơn nhanh, đỡ phải chọn lại từ đầu.

**Q: Tôi muốn tính tiền theo giờ (phòng hát, bàn bi-a) thì cấu hình ở đâu?**
A: Trong bộ cấu hình mặt hàng mặc định, bạn chọn một **dịch vụ tính giờ** (dịch vụ này phải được cài đặt là "dịch vụ tính giờ" và có bảng giá theo khung giờ). Khi khách ngồi bàn đó, hệ thống sẽ tính tiền theo thời gian sử dụng.

**Q: Vì sao tôi không bật được một dịch vụ làm "dịch vụ tính giờ"?**
A: Dịch vụ đó chưa được đánh dấu là dịch vụ tính giờ ở phần khai báo dịch vụ. Hãy vào cấu hình dịch vụ, bật thuộc tính tính giờ cho nó, rồi quay lại chọn.

**Q: Tôi bật "mặc định" cho bộ cấu hình mới thì bộ cũ thế nào?**
A: Mỗi chi nhánh chỉ có một bộ mặc định tại một thời điểm. Khi bạn bật mặc định cho bộ mới, bộ cũ sẽ tự động bị bỏ mặc định.

**Q: Hệ thống báo "bàn đã được cấu hình ở bộ khác", nghĩa là sao?**
A: Một bàn/chỗ ngồi chỉ nên nằm trong một bộ cấu hình. Bàn bạn đang chọn đã được gán cho bộ cấu hình khác. Hãy gỡ bàn đó khỏi bộ kia, hoặc chọn bàn khác.

**Q: Tôi đã xóa bộ cấu hình nhưng màn bán hàng vẫn còn hiện?**
A: Hệ thống có lưu tạm (cache) cấu hình để chạy nhanh. Sau khi xóa, cache có thể chưa được làm mới ngay nên POS còn hiển thị tạm thời. Bạn đợi cache hết hạn hoặc thực hiện thêm một thao tác cấu hình khác; nếu vẫn còn, báo bộ phận hỗ trợ.

**Q: Tôi muốn cấu hình "giảm giá đơn theo mức tiền" như trước, giờ ở đâu?**
A: Chức năng cấu hình giảm giá đơn hàng theo mức giá cũ đã ngừng sử dụng. Hiện việc giảm giá/chiết khấu trên đơn được làm qua **voucher**, **khuyến mãi** và **chiết khấu theo bill** ngay tại màn bán hàng.
