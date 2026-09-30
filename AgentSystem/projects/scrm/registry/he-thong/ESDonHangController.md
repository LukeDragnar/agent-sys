---
controller: ESDonHangController
module: he-thong
service: ESDocument
man_hinh_ui: "nội bộ — không có UI (chỉ còn endpoint test log)"
muc_dich: "Controller ES cho đơn hàng — phần lớn endpoint index/search đơn hàng đã bị comment, hiện chỉ còn 1 endpoint test ghi log."
alias: ["ES đơn hàng", "index đơn hàng", "test log ES"]
auth: public
state_machine: []
deprecated: true
entities: [DonHangES, DonHangLogisticES, ContactES]
stored_procs: []
depends: [IPublishEndpoint (RabbitMQ), IElasticSearch]
cross_module_in: []
cross_module_out: []
---

# ESDonHangController — ES đơn hàng (gần như rỗng, chỉ còn test log)

Route `api/ESDonHang`, không `[TienIch]`, không `[SBAuthorize]` → **public**. Gần như toàn bộ endpoint index/search (IndexContactTest, IndexDonHangTest, TimKiemDanhSach, ThuKhoTacNghiepSearch, DonHangSaleSearch, QuanLyLeadMkt, DanhSachContactSale...) **đã bị comment** → controller coi như **deprecated**, AI bỏ qua nhanh.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Test ghi log | — (không UI) | GET `api/ESDonHang/TestLog` | — | public | — | Ghi LogError/LogWarning/LogInformation rồi trả Ok() — chỉ để verify log | — | Endpoint test, không nghiệp vụ |

## Lỗi đã biết / lưu ý kỹ thuật
- `ESDonHangController.cs:40-1419` — toàn bộ logic index/search đơn hàng đã comment; logic ES đơn hàng thực tế nằm ở DonViElasticSearchController + consumer. Controller này chỉ còn `TestLog`.
