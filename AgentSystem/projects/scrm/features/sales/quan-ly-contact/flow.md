# Luồng — Quản lý Contact

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc một đơn vị (IdDonVi) và chi nhánh.
- Có data để chăm: contact được tạo thủ công, import, từ landing/MKT, từ TMĐT, hoặc được chia xoay vòng từ kho số.
- Để chia xoay vòng (nút Nhận data): đơn vị đã cấu hình chia số và contact đang ở trạng thái ẩn thông tin.
- Để duyệt lịch: đơn vị bật yêu cầu duyệt lịch; người duyệt là sale leader.
- Để xem danh sách black list: có role admin chi nhánh / multi / tổng (DV-AD-CN/MCN/TONG).

## Luồng dùng (Sale chăm contact)
1. Sale mở **Danh sách contact**, lọc theo từ khóa / nhãn / cuộc hội thoại → hệ thống trả list theo scope đơn vị + quyền của sale.
2. Nếu contact đang ẩn (chia xoay vòng): bấm **Nhận data** → hệ thống bỏ ẩn thông tin liên hệ + bắt đầu đếm thời gian tác nghiệp.
3. Sale mở **Chi tiết contact** (có 3 biến thể: có ghi log xem / không ghi log / chỉ xem) → gọi/chăm khách.
4. Sau khi liên hệ: **Cập nhật kết quả tác nghiệp**, **Gắn nhãn khách hàng**, **Ghi chú nội bộ** (hoặc lưu link nhóm Zalo).
5. Theo tiếp: **Chuyển tác nghiệp** / **Tác nghiệp tiếp (v1/v2)**; hẹn khách: **Đặt lịch** → ripple sang Booking; chốt: **Tạo đơn hàng** (hoặc Tạo đơn khách cũ từ contact cũ đã chốt).

## Luồng cấu hình / quản lý (Leader)
1. **Phân bổ data:** chọn contact (hoặc theo bộ lọc) → **Phân bổ** cho sale / nhóm sale / MKT landing.
2. **Thu hồi:** **Thu hồi sale** theo bộ lọc → gỡ sale khỏi tất cả contact khớp điều kiện.
3. **Duyệt lịch:** vào tab **Chờ duyệt lịch** → xem `GetListContactDatLichChoDuyet` → **Duyệt** (tạo lịch thật) hoặc **Hủy duyệt** (trả về Chờ duyệt).

## Luồng black list (admin / CSKH)
1. Mở **Danh sách black list** (cần quyền admin chi nhánh trở lên).
2. **Thêm / sửa** số cần chặn (kèm lý do); hoặc thêm từ popup chi tiết contact (`InsertOrUpdateBlackList`).
3. Khi tạo đơn, OrderLogistic gọi nội bộ `IsBlackListByPhonePrivate` để kiểm tra số → cảnh báo nếu trùng.
4. **Xóa** số khỏi black list theo id.

## Các trường hợp / case biên
- **Nhận data khi đã có người nhận:** contact đã được sale khác nhận → không nhận lại được (đã thuộc người khác).
- **Đặt lịch khi đơn vị bật duyệt:** lịch vào trạng thái **DatLich_ChoDuyet**; nếu leader chưa duyệt → lịch chưa tạo thật. Leader duyệt → **DatLich_DaDuyet** + tạo lịch; hủy duyệt → quay lại **DatLich_ChoDuyet (HuyDuyet)**.
- **Xóa nhiều contact mà không phải admin tổng:** bị từ chối (`DeleteMultipleContact` chỉ DV-AD-TONG).
- **Import file sai cột:** cột Excel map theo index cứng, dữ liệu bắt đầu từ dòng 7 — sai vị trí cột → dòng lỗi; tải file lỗi về sửa.
- **Import cột ngày sinh + địa chỉ (mới 07/2026):** file import phiên bản mới thêm cột **Ngày sinh** (ngay sau SĐT) + 4 cột địa chỉ cuối (**Tỉnh/Huyện/Xã** nhập TÊN + **Địa chỉ** số nhà). Tỉnh/Huyện/Xã: BE resolve tên→mã theo phân cấp (khớp trong phạm vi cha). **Địa chỉ sai KHÔNG loại dòng** — chỉ bỏ cấp sai + gộp cảnh báo, contact vẫn import. **Ngày sinh > hôm nay → dòng lỗi** (`"Ngày sinh không được vượt quá hiện tại"`); sai định dạng → lỗi. Ngày sinh import propagate sang KH (backfill nếu KH chưa có, không override).
- **Tác nghiệp tiếp ngay lập tức:** nếu số phút chờ ≤ 0 → hệ thống ép mốc tác nghiệp tiếp tối thiểu **+1 phút** (`DateTime.Now.AddSeconds(60)`) để chặn set mốc về quá khứ.
- **Phân bổ contact đã chốt đơn offline:** trước đây contact có đơn offline (`IdDonHang`) bị bỏ qua khi phân bổ; nay **bỏ chặn** — cho phép phân bổ (chỉ còn kiểm chốt đơn qua cờ `isAcceptPBDHDaChot`).
- **Số trùng black list khi lên đơn:** OrderLogistic nhận cờ blacklist → cảnh báo (không tự chặn cứng — *cần product xác nhận hành vi chặn hay chỉ cảnh báo*).
- **Đơn logistic đổi/xóa trạng thái:** OrderLogistic gọi private (`UpdateDonHangContact`, `DongBoContactXoaDonHang`…) → cập nhật ngược cờ đơn trên contact; nếu service đồng bộ lỗi, cờ đơn trên contact có thể lệch tạm thời.
- **Endpoint `FixBugContact`:** luôn trả rỗng (debug-only, dead code) — gọi vào không có tác dụng.

## Ví dụ cụ thể có số
Đơn vị B chạy chia số xoay vòng, sáng nay đẩy 200 contact vào pool.
- Sale Hà mở danh sách → thấy 200 contact đang ẩn số. Hà bấm **Nhận data** trên 1 contact → contact đó mở số, bắt đầu đếm thời gian; pool còn 199 ẩn.
- Hà gọi, khách hẹn xem hàng → Hà **Đặt lịch** cho ngày 25/06. Đơn vị bật duyệt lịch → lịch vào **Chờ duyệt**.
- Leader Nam vào tab "Chờ duyệt lịch", thấy 1 lịch → **Duyệt** → lịch ngày 25/06 được tạo thật, contact sang **DatLich_DaDuyet**.
- Hôm sau khách chốt, Hà bấm **Tạo đơn hàng** từ contact → đơn nháp được tạo, đồng bộ ngược lại cờ đơn trên contact.
- Một số 09xx bom hàng 3 lần → CSKH thêm vào **Black list** kèm lý do "bom hàng". Lần sau sale lên đơn cho số này → hệ thống cảnh báo.
- Leader cần dọn 50 contact rác → chỉ admin tổng đơn vị mới bấm được **Xóa hàng loạt**; leader không có quyền này.

## Hậu điều kiện
- Nhận data → contact gắn sale phụ trách, bắt đầu mốc thời gian tác nghiệp (không xóa data).
- Cập nhật kết quả / nhãn / ghi chú → ghi vào contact, dùng cho báo cáo tác nghiệp sau.
- Đặt lịch được duyệt → tạo bản ghi lịch hẹn (Booking) + đồng bộ trạng thái lịch ↔ contact.
- Tạo đơn từ contact → tạo đơn logistic + ripple đồng bộ ngược về contact.
- Thêm/xóa black list → ghi vào DB; OrderLogistic dùng để cảnh báo khi tạo đơn.
