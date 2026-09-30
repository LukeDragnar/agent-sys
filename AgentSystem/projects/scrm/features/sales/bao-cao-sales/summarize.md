---
alias: ["báo cáo sale", "báo cáo chia số", "KPI sale", "tỉ lệ chốt đơn", "tối ưu sale", "báo cáo công việc sale", "bảng xếp hạng sale", "phong thần bảng"]
module: sales
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: "cần product xác nhận — đa số báo cáo phân quyền theo từng báo cáo (key riêng mỗi báo cáo), KHÔNG khóa ở cấp chung; user thường bị ép chi nhánh của mình + lọc theo cơ cấu tổ chức (nhóm/nhân viên được phép xem). Chi tiết key/lỗ hổng ở dev.md."
phu_thuoc: ["Contact / tác nghiệp sale (nguồn số liệu chia số, nhãn, trạng thái)", "Đơn hàng (doanh số, tỉ lệ chốt)", "Tổng đài (số liệu cuộc gọi enrich tối ưu sale)", "Cơ cấu tổ chức (phòng ban/nhóm/user sale để lọc)", "Cấu hình khoảng ngày cho phép tìm kiếm"]
link_flow: ./flow.md
---

# Báo cáo Sales

> **Báo cáo Sales** = bộ báo cáo phục vụ quản lý đội sale: **chia số** (data phân cho ai), **năng suất/KPI** (công việc sale, tối ưu sale, bảng xếp hạng), **hiệu quả bán** (tỉ lệ chốt đơn, doanh số, KH cũ/mới, theo nguồn landing). Phần lớn nằm trong **hub Báo cáo Online**; riêng báo cáo **chia số contact theo ngày** ở controller riêng.

## Là gì
Nơi quản lý sale xem **số liệu hoạt động của đội**: ai được chia bao nhiêu số/contact, mỗi nhân viên gọi/tác nghiệp/chốt được bao nhiêu, xếp hạng nhân viên/nhóm, tỉ lệ chốt đơn theo sản phẩm, doanh số, khách cũ/mới, theo nguồn landing. Hầu hết báo cáo **chỉ đọc** (không sửa dữ liệu) và có nút **Xuất Excel**.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Xem báo cáo chia số contact theo ngày | ✅ | cần quyền xem báo cáo chia số; trong khoảng ngày cho phép |
| Xem báo cáo công việc sale | ✅ | tùy quyền; lọc theo cơ cấu tổ chức |
| Xem báo cáo tối ưu sale | ✅ | có enrich số liệu cuộc gọi (MyHub/Omicall) |
| Xem bảng xếp hạng / phong thần bảng sale | ✅ | tùy quyền |
| Xem tỉ lệ chốt đơn sản phẩm | ✅ | tùy quyền |
| Xem KH cũ/mới, theo nguồn landing | ✅ | tùy quyền |
| Xuất Excel báo cáo | ✅ | hầu hết báo cáo có nút Xuất Excel; khi dịch vụ xuất bận có thể lỗi tạm thời, thử lại |
| Sửa/xóa số liệu trong báo cáo | ❌ | báo cáo chỉ đọc — không chỉnh sửa số liệu |
| Xem dữ liệu ngoài khoảng ngày cho phép | ⚠️ | bị chặn nếu đơn vị giới hạn "chỉ xem N ngày" |
| Xem dữ liệu nhân viên/nhóm ngoài quyền | ⚠️ | bị lọc theo cơ cấu tổ chức; user thường bị ép chi nhánh mình |

## Để làm gì / cho ai
- **Quản lý sale / trưởng nhóm:** theo dõi năng suất, chia số công bằng, đánh giá KPI, xếp hạng nhân viên.
- **Nhân viên sale:** xem kết quả của mình (trong phạm vi được phép).
- **Lãnh đạo:** nhìn bức tranh hiệu quả đội sale, tỉ lệ chốt, doanh số.

## Cách dùng
1. Vào **Báo cáo** → chọn báo cáo sale cần xem (chia số / công việc sale / tối ưu sale / bảng xếp hạng / tỉ lệ chốt...).
2. Đặt **bộ lọc**: chi nhánh, **Từ/Đến ngày**, phòng ban/nhóm/nhân viên sale, sản phẩm/nguồn (tùy báo cáo).
3. Bấm xem → lưới số liệu hiện ra (có phân trang/sắp xếp tùy báo cáo).
4. Bấm **Xuất Excel** để tải file nếu cần.

## Cần chú ý
- **Báo cáo chỉ đọc** — không sửa được số liệu; muốn đổi số phải sửa ở nghiệp vụ gốc (contact/đơn).
- **Giới hạn khoảng ngày**: nếu đơn vị bật "chỉ được xem dữ liệu trong N ngày", chọn khoảng rộng hơn sẽ bị chặn.
- **Phạm vi xem theo quyền**: user thường bị **ép chi nhánh của mình** và chỉ thấy nhóm/nhân viên mình được phép (lọc cơ cấu tổ chức) — thấy ít hơn admin là bình thường.
- **Số liệu khác nhau giữa các báo cáo** do mỗi báo cáo đếm theo tiêu chí riêng (theo ngày chia / theo đơn chốt / theo cuộc gọi); không nên so trực tiếp tổng giữa 2 báo cáo khác loại.
- **Xuất Excel có thể lỗi tạm thời** khi dịch vụ xuất bận — thử lại sau.
- **Phân quyền báo cáo khá lỏng ở tầng kỹ thuật** (chi tiết ở dev.md) — không khẳng định quyền chặt với khách; phạm vi xem chủ yếu dựa vào giới hạn chi nhánh + cơ cấu tổ chức.

## Ảnh hưởng tới
- Chỉ **đọc** từ kho dữ liệu báo cáo — **không ghi/không thay đổi** dữ liệu nghiệp vụ.
- Nguồn số liệu: tác nghiệp **Contact**, **Đơn hàng**, **Tổng đài** (cuộc gọi). Sai số liệu thường do dữ liệu gốc, không phải báo cáo.
