---
alias: ["đồng bộ dữ liệu", "đồng bộ theo đơn vị", "sync đơn vị", "đồng bộ marketing campaign", "đồng bộ khách hàng theo đơn vị", "sync khách hàng"]
module: he-thong
trang_thai: cần product xác nhận — dành cho SuperUser (super-admin nội bộ SaaS)
goi: cần product xác nhận
quyen: "cần product xác nhận — mọi thao tác chặn cứng chỉ SuperUser (IsSuperUser=true). Từng action có [SBAuthorize] gợi ý role HT-AD, HT-CSKH nhưng điều kiện quyết định là cờ SuperUser."
phu_thuoc: ["Marketing Campaign service (consumer chạy đồng bộ thực tế qua RabbitMQ)", "Khách hàng (dữ liệu được đồng bộ: customer / địa chỉ / chi tiêu)", "Redis (khóa tiến trình tránh chạy chồng)"]
link_flow: ./flow.md
---

# Đồng bộ dữ liệu (theo đơn vị — Marketing Campaign)

## Là gì
Đây là công cụ **nội bộ super-admin** để **đăng ký đơn vị dùng Marketing Campaign** và **chạy đồng bộ lại dữ liệu khách hàng theo từng đơn vị**. "Đồng bộ" ở đây là làm cho dữ liệu khách hàng phục vụ Marketing Campaign khớp lại với dữ liệu gốc — gồm 3 kiểu:
- **Đồng bộ khách hàng (SyncCustomerCampaign):** đẩy/cập nhật dữ liệu khách của đơn vị sang hệ Marketing Campaign.
- **Cập nhật địa chỉ dạng full-text (UpdateCustomerAddressFullText):** chuẩn hóa lại trường địa chỉ khách để tìm kiếm tốt hơn.
- **Đồng bộ chi tiêu khách (SyncCustomerSpending):** tính/cập nhật lại số liệu chi tiêu của khách theo các kiểu loại chi tiêu được chọn.

Việc đồng bộ **không chạy ngay tại chỗ** mà được hệ thống gửi sang một dịch vụ chạy nền (qua hàng đợi) để xử lý, tránh treo màn hình. Hệ thống có **khóa tiến trình** để một đơn vị không bị đồng bộ chồng chéo cùng lúc.

> Đây KHÔNG phải màn cho khách hàng. Chỉ **SuperUser** (super-admin nội bộ) mới thao tác được; mọi nút đều bị chặn nếu không phải SuperUser.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Xem danh sách đơn vị dùng Marketing Campaign | ✅ | chỉ SuperUser |
| Thêm / cập nhật đơn vị (đăng ký dùng Marketing Campaign) | ✅ | chỉ SuperUser |
| Đồng bộ khách hàng theo đơn vị | ✅ | chỉ SuperUser; phải chọn ≥1 đơn vị; số bản ghi/lần ≤ 2.000 |
| Cập nhật địa chỉ full-text khách | ✅ | chỉ SuperUser; số bản ghi/lần ≤ 10.000 |
| Đồng bộ chi tiêu khách | ✅ | chỉ SuperUser; ≤ 2.000 bản ghi/lần; phải chọn đúng kiểu loại chi tiêu |
| Hủy đồng bộ của đơn vị | ✅ | chỉ SuperUser |
| Chạy khi đơn vị đang có tiến trình đồng bộ khác | ❌ | bị chặn: "Đang có tiến trình đồng bộ dữ liệu khác đang chạy!" |

## Để làm gì / cho ai
- **Super-admin / đội kỹ thuật vận hành:** khi dữ liệu khách của một đơn vị bị lệch giữa hệ nghiệp vụ và hệ Marketing Campaign (do sự cố, migrate, sửa data tay…), dùng công cụ này để **đồng bộ lại** cho khớp.
- **Sau khi bật Marketing Campaign cho một đơn vị mới:** đăng ký đơn vị rồi đồng bộ dữ liệu nền (khách/địa chỉ/chi tiêu) để campaign chạy đúng.

## Cách dùng
1. Vào **Hệ thống → Đồng bộ theo đơn vị** (chỉ SuperUser thấy/dùng được).
2. Xem **danh sách đơn vị** đang dùng Marketing Campaign; **Thêm đơn vị** nếu cần đăng ký mới.
3. Chọn ≥1 đơn vị, chọn **kiểu đồng bộ** (khách hàng / địa chỉ full-text / chi tiêu) và **số bản ghi mỗi lần** (batch size) trong giới hạn cho phép.
4. Với "chi tiêu": chọn thêm **kiểu loại chi tiêu** cần tính lại.
5. Bấm **Đồng bộ**. Hệ thống kiểm tra điều kiện và khóa tiến trình rồi đẩy việc đồng bộ sang dịch vụ chạy nền.
6. Có thể bấm **Hủy** để hủy tiến trình đồng bộ của đơn vị.

## Cần chú ý
- **Chỉ SuperUser dùng được** — không phải SuperUser bấm bất kỳ nút nào đều bị từ chối (hiện trả lỗi dạng "dữ liệu không hợp lệ", không phải lỗi quyền rõ ràng — known-issue).
- **Giới hạn batch:** khách hàng & chi tiêu tối đa **2.000** bản ghi/lần; địa chỉ full-text tối đa **10.000** bản ghi/lần. Vượt → báo lỗi, không chạy.
- **Không chạy chồng:** nếu đơn vị đang có tiến trình đồng bộ cùng kiểu đang chạy thì bị chặn. Nhưng nếu **hai lần bấm quá gần nhau** vẫn có thể cùng lọt qua (known-issue về khóa — xem dev.md).
- **Đồng bộ chạy nền, không tức thì:** bấm xong việc được xếp hàng đợi và xử lý bởi dịch vụ Marketing Campaign; kết quả không hiện ngay.
- **Tác động dữ liệu thật:** đồng bộ ghi/cập nhật dữ liệu khách trong hệ Marketing Campaign → cân nhắc trước khi chạy với batch lớn.

## Ảnh hưởng tới
- **Marketing Campaign:** dữ liệu khách/địa chỉ/chi tiêu của đơn vị được cập nhật → ảnh hưởng kết quả lọc đối tượng, chiến dịch.
- **Hiệu năng hệ thống:** đồng bộ batch lớn nhiều đơn vị cùng lúc tạo tải lên dịch vụ nền và CSDL.
