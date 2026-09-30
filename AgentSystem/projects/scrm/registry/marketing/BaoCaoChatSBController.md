---
controller: BaoCaoChatSBController
module: marketing
service: FacebookChat
man_hinh_ui: "ChatHub (Tin nhắn Facebook) > Báo cáo chat (tab Tag chat / Tổng quan / Nhân viên)"
muc_dich: "Tổng hợp & thống kê hoạt động chat của ChatHub: báo cáo theo tag chat, tổng quan tin nhắn/comment theo ngày (kèm biểu đồ), top nhân viên phản hồi nhanh, tỷ lệ & chi tiết phản hồi từng nhân viên. Đọc trộn MongoDB (Conversation/MessageChatBox) + SQL (FBTagChat/FBCustomer)."
alias: ["báo cáo chat", "báo cáo ChatHub", "thống kê chat Facebook", "BaoCaoChatSB", "report tag chat", "tỷ lệ phản hồi nhân viên"]
auth: internal
state_machine: []
deprecated: false
entities: [FBTagChat, FBCustomer, Conversation, MessageChatBox]
stored_procs: []
depends:
  - "IBaoCaoChatService (FacebookChat.Service.Generated.BaoCaoChatService)"
  - "IMongoDbClient (collection Conversation, MessageChatBox)"
  - "IRepository<FBTagChat>, IRepository<FBCustomer> (SQL)"
  - "Core/TienIch (MaTienIchSCRM.TI_ChatHub — đơn vị phải mua tiện ích ChatHub)"
cross_module_in: []
cross_module_out: []
---

# BaoCaoChatSBController — Báo cáo chat ChatHub (Facebook)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Báo cáo tag chat | Báo cáo tag chat | POST `api/BaoCaoChatSB/BaoCaoTagChat` | `BaoCaoChatSB_BaoCaoTagChat` ("Báo cáo tag chat") | internal | Tab Tag chat | Lấy `FBTagChat` của (DonViId+ChiNhanh) → lấy `Conversation` theo filter (PageId, TuNgay/DenNgay theo `LastTime`) → đếm số conversation per tag (distinct tag trong 1 conv), left-join để giữ cả tag count=0, sort giảm theo Value. | Mongo `Conversation` + EF `FBTagChat` | — |
| 2 | Báo cáo chi tiết tag chat (search) | Tìm theo từ khóa | POST `api/BaoCaoChatSB/BaoCaoTagChatByKeyWord` | `BaoCaoChatSB_BaoCaoTagChatByKeyWord` ("Báo cáo chi tiết tag chat") | internal | Tab Tag chat (ô search) | Như (1) nhưng lọc thêm `FBTagChat.Description` chứa `KeyWord` (lowercase, in-memory). | Mongo `Conversation` + EF `FBTagChat` | Filter keyword chạy in-memory sau ToList → ok do data tag nhỏ. |
| 3 | Báo cáo tổng quan chat | Tổng quan | POST `api/BaoCaoChatSB/BaoCaoTongQuanChat` | `BaoCaoChatSB_BaoCaoTongQuanChat` ("Báo cáo tổng quan chat") | internal | Tab Tổng quan | Đếm theo ngày: tin nhắn KH/Trang/có SĐT, comment KH/Trang/có SĐT (từ `MessageChatBox`), số conversation message/comment (từ `Conversation`), khách chat mới/cũ (join `FBCustomer` theo `PSId`, so `NgayTao` + `IsKhachTuongTac`). Fill đủ ngày trong khoảng, sort giảm theo `ThoiGian`. | Mongo `MessageChatBox`+`Conversation` + EF `FBCustomer` | **Gate 31 ngày:** `BaoCaoChatService.cs:180` — `DenNgay-TuNgay > 31` → trả lỗi; `BaoCaoChatService.cs:180,323` deref `.Value` không null-check → nếu thiếu TuNgay/DenNgay sẽ NRE. |
| 4 | Báo cáo tổng quan theo biểu đồ | Biểu đồ tổng quan | POST `api/BaoCaoChatSB/BaoCaoTongQuanChartChat` | `BaoCaoChatSB_BaoCaoTongQuanChartChat` ("Báo cáo tổng quản chat theo biểu dồ") | internal | Tab Tổng quan (chart) | Từ `MessageChatBox`: group theo ngày, đếm tin nhắn (`MsId!=null`) & comment (`CommentId!=null`), sinh đủ category ngày (dd/MM/yyyy), ngày trống = 0. | Mongo `MessageChatBox` | Gate 31 ngày như (3). Permission key typo trong nhãn ("tổng quản"/"biểu dồ") — chỉ ảnh hưởng label. |
| 5 | Top 5 nhân viên phản hồi nhanh | Top 5 nhân viên | POST `api/BaoCaoChatSB/BaoCaoTop5NhanVien` | `BaoCaoChatSB_BaoCaoTop5NhanVien` ("Báo cáo top 5 nhân viên phản hồi chat nhanh nhất") | internal | Tab Nhân viên | Lọc `MessageChatBox` (IsComming=true, NguoiTaoUserId!=null, ThoiGianPhanHoi!=null) → group theo `NguoiTaoUserId`, tính `Average(ThoiGianPhanHoi)` ms → sort tăng, Take(5). Rỗng → trả `messages` (null/empty). | Mongo `MessageChatBox` | `BaoCaoChatService.cs:405` cast `(int)g.Average(...)` — quá tải int nếu thời gian phản hồi rất lớn. |
| 6 | Tỷ lệ phản hồi theo nhân viên | Tỷ lệ phản hồi | POST `api/BaoCaoChatSB/BaoCaoTyLePhanHoi` | `BaoCaoChatSB_BaoCaoTyLePhanHoi` ("Báo cáo tỷ lệ phẩn hồi chat của nhân viên") | internal | Tab Nhân viên | Lọc `MessageChatBox` (IsComming=true, NguoiTaoUserId!=null) → tổng `totalRecord` qua CountDocuments → group theo nhân viên, `TyLePhanHoi = Count*100/totalRecord` (2 chữ số), sort tăng theo TongSoTin. | Mongo `MessageChatBox` | — |
| 7 | Chi tiết chat từng nhân viên | Chi tiết nhân viên | POST `api/BaoCaoChatSB/BaoCaoChiTietNhanVienChat` | `BaoCaoChatSB_BaoCaoChiTietNhanVienChat` ("Báo cáo chi tiết chat từng nhân viên") | internal | Tab Nhân viên (drill-down) | 2 filter: (a) tin đến IsComming=true (group `NguoiTaoUserId`: tổng tin, tỷ lệ phản hồi, TG phản hồi TB, số mess/cmmt, phiên trả lời mess/cmmt; lọc thêm `ListUserId` nếu có); (b) tin mang SĐT về IsComming=false & `UserIdMangSdtVe!=null` (group `UserIdMangSdtVe`). Left-join 2 kết quả theo userId, sort giảm theo TongSoTin. | Mongo `MessageChatBox` | `ListUserId` chỉ áp cho filter (a), không áp cho (b) → SĐT mang về không bị lọc theo user chọn. |

## Quan hệ
- **Gọi sang:** chỉ `IBaoCaoChatService` (đọc thuần, không gọi service ngoài). Service đọc MongoDB collection `Conversation` & `MessageChatBox` + EF `FBTagChat`/`FBCustomer`.
- **Dữ liệu (entities):** `MessageChatBox` (mongo, từng tin nhắn/comment — `IsComming`, `MsId`, `CommentId`, `IsHavePhone`, `ThoiGianPhanHoi`, `NguoiTaoUserId`, `UserIdMangSdtVe`), `Conversation` (mongo — `TagChats`, `IdKhachHang`, `IsMessage`/`IsComment`, `LastTime`), `FBTagChat` (SQL — Description/Color), `FBCustomer` (SQL — PSId/IsKhachTuongTac/NgayTao).
- **Ripple / đồng bộ:** không có — controller chỉ đọc/báo cáo, không ghi, không phát event.

## Lỗi đã biết / lưu ý kỹ thuật
- `BaoCaoChatService.cs:180` & `:323` — `(gridInfo.DenNgay.Value - gridInfo.TuNgay.Value)` deref `.Value` trực tiếp; nếu FE không gửi TuNgay/DenNgay → `NullReferenceException` trước khi tới gate 31 ngày. Gate giới hạn báo cáo tổng quan/chart tối đa 31 ngày.
- `BaoCaoChatService.cs:405,502` — cast `(int)Average(ThoiGianPhanHoi)`: overflow nếu thời gian phản hồi (ms) tích lũy lớn.
- `BaoCaoChatService.cs:471-472` vs `:475-487` — filter `ListUserId` chỉ áp cho nhánh tin đến (incoming), không áp cho nhánh "mang SĐT về" → cột `SdtMangVe` không bị lọc theo nhân viên đã chọn.
- Permission label có typo ("tổng quản chat", "biểu dồ", "phẩn hồi") — không ảnh hưởng logic, chỉ hiển thị.
- Toàn bộ filter Mongo gắn cứng `IdDonVi`+`IdChiNhanh` từ `CurrentUser` → tách biệt dữ liệu theo đơn vị/chi nhánh.
