---
controller: PublicPreOrderController
module: cua-hang
service: PreOrder
man_hinh_ui: "Trang đặt trước cho KHÁCH (mobile web) — khách quét QR ở bàn → xem menu chi nhánh → chọn món + nhập tên/SĐT → tạo đơn đặt trước (route public, không đăng nhập)"
muc_dich: "API công khai (AllowAnonymous) cho khách: quét QR lấy menu theo chi nhánh, lưu đơn đặt trước, tra lại đơn theo mã PreOrder."
alias: ["đặt trước qua QR", "menu khách scan QR", "trang order của khách", "lưu đơn pre-order", "public preorder"]
auth: anonymous
state_machine: []
deprecated: false
entities: [BangQRCode, DonHangPre, DonHangPreSanPham, DonHangPreSanPhamDinhKem]
stored_procs: []
depends: [PreOrder, WarehouseProduct (GetSanPhamForPreOrder), CommonCache (GetCoCauDonViTuCache), Customer (FindOrCreatedCustomResult), Redis (claim mã PreOrder)]
cross_module_in: [Khách (mobile web, không auth) quét QR ở bàn]
cross_module_out: [Customer — tạo/tìm khách hàng khi có tên+SĐT; DonHangPre — sinh đơn TrangThai=0 cho admin/Order xử lý]
---

# PublicPreOrderController — API đặt trước công khai cho khách (quét QR)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy menu theo mã QR | khách scan QR → load menu | GET `public/PublicPreOrder/GetSanPham?maQR=` | — (controller `[AllowAnonymous]`) | anonymous | trang khách | maQR rỗng → "Mã QR không hợp lệ!"; tra `GetByMaQR`, null → "Mã QR không tồn tại hoặc đã bị xóa!"; lấy SP cho preorder (`GetSanPhamForPreOrder` theo DV+CN), lấy info chi nhánh từ cache CoCau (SĐT/địa chỉ/sliders/avatar); trả gói {ĐV, CN, chỗ ngồi, khu vực, list nhóm SP, list SP, list đính kèm} | EF (BangQR) + WarehouseProduct + cache CoCau | Lock theo IP (`SetIfNotExists`) đã **comment** — không còn chống spam scan |
| 2 | Lưu đơn đặt trước | "Gửi đơn" / "Đặt trước" | POST `public/PublicPreOrder/Save` | — | anonymous | đã chọn món | request.MaQR rỗng → lỗi; tra BangQR, null → lỗi; gọi `DonHangPreService.Save(request, bangQR)`: validate list SP không rỗng + (tên ⇔ SĐT phải cùng có/cùng trống); sinh `MaPreOrder` (PO-xxxxxx) claim qua Redis; nếu có tên+SĐT → gọi Customer `FindOrCreatedCustomResult` lấy IdKhachHang; insert đơn (TrangThai=0) + SP + đính kèm; trả {MaPreOrder, Id} | EF + Redis + HTTP Customer | - |
| 3 | Tra đơn theo mã PreOrder | "Xem đơn của tôi" | GET `public/PublicPreOrder/GetByMaPreOrder?maPreOrder=` | — | anonymous | sau khi đặt | maPreOrder rỗng → lỗi; trả `DonHangPreFullDto` theo mã (không filter ĐV/CN) | EF | Public: ai biết mã PreOrder đều tra được đơn (tên/SĐT khách lộ) |

## Quy tắc nghiệp vụ (validate khi Save — `DonHangPreService.Save`)
Thứ tự (dừng ở lỗi đầu tiên):
1. `ListSanPham` null/rỗng → "Danh sách sản phẩm không được trống!".
2. **XOR tên/SĐT**: `có_tên != có_SĐT` → "Vui lòng điền đầy đủ tên và số điện thoại!" (cho phép cả hai trống = đặt ẩn danh; hoặc cả hai có).
3. Sinh `MaPreOrder` = `PO-` + 6 ký tự [A-Z0-9], claim Redis key `preorder:ma:{code}` (TTL 5 phút) → nếu key đã tồn tại hoặc trùng DB thì thử lại, quá 10 lần ném lỗi.
4. Nếu có tên+SĐT → tạo/tìm khách (Customer `FindOrCreatedCustomResult`, NguoiTao="PREORDER") → gán `IdKhachHang` (lỗi gọi Customer không chặn lưu đơn — chỉ bỏ qua idKhachHang).
5. Insert đơn (TrangThai=0) + từng `DonHangPreSanPham`; **sản phẩm đính kèm** cũng được thêm thành dòng SP riêng (IsSanPhamDinhKem=true) — nếu IdSanPham đính kèm trùng SP đã có thì cộng dồn SoLuong.
6. Cuối: `RemoveAsync` lock key `preorder:ma:{maPreOrder}` (nhả claim).

## Quan hệ
- **Gọi sang:**
  - `IBangQRCodeService.GetByMaQR` — resolve ĐV/CN/chỗ ngồi từ mã QR.
  - `IWarehouseProductPreOrderService.GetSanPhamForPreOrder(idDonVi, idChiNhanh)` — menu cho preorder.
  - `ICommonCacheService.GetCoCauDonViTuCache` — info chi nhánh (SĐT, địa chỉ, slider, avatar).
  - Customer `POST /api/KhachHang/FindOrCreatedCustomResult` (qua `_clientHelper`) — tạo/tìm khách.
  - Redis (`IRedisCacheService`) — claim/nhả mã PreOrder.
- **Dữ liệu (entities):** `BangQRCode` (đọc), `DonHangPre` + `DonHangPreSanPham` + `DonHangPreSanPhamDinhKem` (ghi).
- **Ripple / đồng bộ:** đơn Save → vào danh sách admin (`DonHangPreController`) + được Order/thungan nhập qua Private endpoint → khách hàng mới có thể được tạo bên Customer.

## Luồng xuyên module (khách đặt trước qua QR)
```
Khách quét QR (in/dán ở bàn) → public/GetSanPham (menu theo CN) 
  → chọn món + (tùy chọn) tên/SĐT → public/Save
      → claim MaPreOrder (Redis) → [nếu có tên+SĐT] Customer.FindOrCreatedCustomResult → idKhachHang
      → insert DonHangPre TrangThai=0 + SP + đính kèm
  → admin/Order xử lý → MarkImported (TrangThai=1)
```

## Lỗi đã biết / lưu ý kỹ thuật
- **`PublicPreOrderController` kế thừa `ControllerBase`** (không `BaseController`) → không có `CurrentUser`; mọi ngữ cảnh ĐV/CN lấy từ BangQRCode resolve qua maQR.
- **Lock chống spam theo IP đã bị comment** (`PublicPreOrderController:43-49`) — endpoint public không còn rate-limit ở tầng app, dễ bị spam scan/tạo đơn rác (Job clear dọn sau N ngày).
- **GetByMaPreOrder public không kiểm tra quyền sở hữu** — lộ thông tin đơn (tên/SĐT) nếu đoán/biết mã PO-xxxxxx (6 ký tự).
- **Gọi Customer lỗi không rollback đơn** — đơn vẫn lưu, chỉ thiếu IdKhachHang.
- **Sản phẩm đính kèm nhân đôi vai trò**: vừa là bản ghi DinhKem, vừa là 1 dòng DonHangPreSanPham (IsSanPhamDinhKem=true) — khi tính tiền/đếm SP phải lưu ý tránh đếm trùng.
- Code thật: BE `PreOrder/PreOrder.API/Controllers/Public/PublicPreOrderController.cs`, service `PreOrder/PreOrder.Service/Generated/DonHangPreService.cs` (Save) + `BangQRCodeService.cs` (GetByMaQR).
