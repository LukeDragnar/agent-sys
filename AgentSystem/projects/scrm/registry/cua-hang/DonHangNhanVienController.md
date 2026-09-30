---
controller: DonHangNhanVienController
module: cua-hang
service: Order
man_hinh_ui: "nội bộ — không có UI (controller hiện rỗng, không endpoint active). Các endpoint nhân-viên-thao-tác-đơn thực tế nằm trong DonHangController, region 'Nhân viên thao tác')."
muc_dich: "Controller dự kiến cho thao tác đơn hàng của nhân viên (vd tính hoa hồng) — nhưng hiện KHÔNG có endpoint active: method duy nhất (TinhHoaHong) đã bị comment toàn bộ."
alias: ["đơn hàng nhân viên", "tính hoa hồng nhân viên", "hoa hồng đơn hàng"]
auth: internal
state_machine: []
deprecated: true
entities: []
stored_procs: []
depends: [Order]
cross_module_in: []
cross_module_out: []
---

# DonHangNhanVienController — (controller rỗng / giữ chỗ)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| — | **Không có endpoint active** | — | — | — | — | — | Controller chỉ có constructor; không action nào được expose | — | Controller rỗng (giữ chỗ) |

## Endpoint deprecated (đã comment)
- `[deprecated]` **`TinhHoaHong`** (GET, key gốc comment `DonHangNhanVien_TinhHoaHong`, role `DV-AD-TONG, DV-AD-CN, DV-QLDV`) — tính hoa hồng theo `idDonHangs` + `idTransaction`; toàn bộ method đã bị comment (`DonHangNhanVienController.cs:33-44`) → KHÔNG khả dụng.

## Quan hệ
- **Gọi sang:** (không có — controller không thực thi hành động nào)
- **Dữ liệu (entities):** không expose qua controller này
- **Ripple / đồng bộ:** không

## Lỗi đã biết / lưu ý kỹ thuật
- **Controller RỖNG**: chỉ có constructor inject `IDonHangNhanVienService`, không endpoint active nào (`DonHangNhanVienController.cs`). Class `partial` → có thể có phần bổ sung ở file khác, nhưng file này không chứa action.
- Các thao tác đơn hàng của **nhân viên thực tế sống trong `DonHangController`** (region "Nhân viên thao tác"), KHÔNG ở controller này → khi tìm endpoint nhân viên, tra `DonHangController`.
- `[TienIchAttribute("DonHangNhanVienController", ..., MaTienIchSCRM.TI_Order)]` còn gắn ở class nhưng không có action để áp.
- Code thật: BE `Order/Order.API/Controllers/DonHangNhanVienController.cs`.
