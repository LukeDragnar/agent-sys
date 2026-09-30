# FAQ — Chứng từ mua dịch vụ & Phân bổ chi phí mua hàng

**Chi phí mua hàng (landed cost) là gì?**
Là các khoản phí liên quan đến việc mua/nhập hàng — vận chuyển, bốc xếp, kiểm định, thuê ngoài... Thay vì hạch toán rời, hệ thống cho phép **cộng các phí này vào giá vốn hàng nhập** để tính lãi/lỗ chính xác hơn.

**Tôi ghi phí vận chuyển ở đâu?**
Vào **Kế toán → Chứng từ mua dịch vụ**, lập chứng từ với nhà cung cấp dịch vụ vận chuyển và **tick "Là chi phí mua hàng"**. Sau khi hạch toán, chứng từ này sẽ đem đi phân bổ vào phiếu mua hàng.

**Làm sao để phí đó vào giá vốn hàng?**
Mở **form Phiếu mua hàng** cần cộng phí → popup phân bổ chi phí → chọn chứng từ dịch vụ (đã hạch toán) → nhập số tiền phân bổ. Chi phí sẽ trải xuống từng dòng sản phẩm và cộng vào giá vốn khi nhập kho.

**Một hóa đơn vận chuyển chia cho nhiều lô hàng được không?**
Được. Một chứng từ chi phí có thể phân bổ cho **nhiều phiếu mua hàng**, và một phiếu mua có thể nhận chi phí từ **nhiều chứng từ**. Hệ thống chỉ chặn khi tổng phân bổ vượt tổng tiền chứng từ.

**Tại sao chứng từ của tôi không hiện trong danh sách phân bổ?**
Chứng từ chỉ khả dụng khi: đã tick "Là chi phí mua hàng", **đã hạch toán** (không còn nháp), và **vẫn còn số dư chưa phân bổ hết**.

**Chi phí được chia xuống các sản phẩm theo cách nào?**
3 cách: **theo đơn giá** (giá nhập × số lượng — mặc định), **theo số lượng**, hoặc **tự nhập** số tiền từng dòng. Sai số làm tròn dồn vào dòng cuối.

**Đã phân bổ rồi, giờ muốn sửa chứng từ thì sao?**
Phải **xóa phân bổ trước**, sau đó mới sửa được chứng từ. Chứng từ đã phân bổ hoặc đã hạch toán không cho sửa trực tiếp.

**Phiếu mua đã hoàn thành, phân bổ thêm chi phí được không?**
Không. Khi phiếu mua **Hoàn thành** thì kho và giá vốn đã khóa — không phân bổ thêm hay xóa phân bổ. Hãy phân bổ **trước khi** hoàn thành phiếu.

**Chứng từ mua dịch vụ có đồng bộ sang MISA không?**
Có, nếu đơn vị có cấu hình kết nối kế toán — chứng từ mua dịch vụ được đẩy sang MISA (kèm tên nhân viên mua, MST, người liên hệ).
