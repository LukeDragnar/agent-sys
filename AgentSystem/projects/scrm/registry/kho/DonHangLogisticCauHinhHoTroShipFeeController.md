---
controller: DonHangLogisticCauHinhHoTroShipFeeController
module: kho
service: OrderLogistic
man_hinh_ui: "Đơn hàng logistic > Cấu hình > Cấu hình hỗ trợ phí ship (theo chi nhánh) — bảng bậc thang 'Giá trị đơn hàng từ' → 'Hỗ trợ', nút Thêm/Sửa/Xóa/Lưu nhiều dòng"
muc_dich: "Cấu hình bậc thang (tier) hỗ trợ/thu phí ship COD theo chi nhánh: với mỗi ngưỡng giá trị đơn (GiaTriTroLen), định giá trị hỗ trợ (GiamGiaGiaTri) + kiểu (ThuThucTe / HoTroGiam). Dùng khi tính phí vận chuyển đơn để set DonHangHoTroShipFee."
alias: ["cấu hình hỗ trợ phí ship", "cấu hình COD", "hỗ trợ phí ship theo đơn", "bậc thang hỗ trợ ship", "DonHangLogisticCauHinhCod", "free ship theo ngưỡng"]
auth: public
state_machine: []
deprecated: false
entities: [DonHangLogisticCauHinhCod]
stored_procs: []
depends:
  - "IDonHangLogisticCauHinhHoTroShipFeeServcie (OrderLogistic.Service/Generated/DonHangLogisticCauHinhHoTroShipFeeServcie.cs)"
  - "ICommonCacheService (cache cấu hình theo chi nhánh — ClearCache/GetOrSetCache_CauHinhHoTro_ShipFee_CN)"
  - "ILogActivityHelper (lịch sử tác động EventLichSuTacDong)"
cross_module_in:
  - "OrderService.TinhPhiVc (đọc cache cấu hình để áp tier → DonHangHoTroShipFee khi tính phí giao hàng)"
cross_module_out: []
---

# DonHangLogisticCauHinhHoTroShipFeeController — Cấu hình hỗ trợ phí ship (COD)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Thêm 1 mức hỗ trợ | "Thêm" | POST `api/DonHangLogisticCauHinhHoTroShipFee/Create` | `DonHangLogisticCauHinhHoTroShipFeeController_Create` (DV-AD-TONG, DV-AD-CN) | public | - | Insert 1 mức (GiaTriTroLen, GiamGiaGiaTri, KieuLoai) theo chi nhánh user; validate ngưỡng không trùng; clear cache + log lịch sử | EF | - |
| 2 | Sửa 1 mức | "Sửa" | POST `api/DonHangLogisticCauHinhHoTroShipFee/Update` | `DonHangLogisticCauHinhHoTroShipFeeController_Update` (DV-AD-TONG, DV-AD-CN) | public | có Id | Update GiamGiaGiaTri/GiaTriTroLen/KieuLoai (MarkDirty 3 field); validate trùng ngưỡng (loại trừ chính nó); clear cache + log | EF | - |
| 3 | Xóa 1 mức | "Xóa" | POST `api/DonHangLogisticCauHinhHoTroShipFee/Delete/{id:Guid}` | `DonHangLogisticCauHinhHoTroShipFeeController_Delete` (DV-AD-TONG, DV-AD-CN) | public | có Id thuộc chi nhánh | Xóa bản ghi theo IdDonVi+IdChiNhanh+Id; clear cache + log | EF | - |
| 4 | Lưu nhiều mức (upsert batch) | "Lưu" toàn bảng | POST `api/DonHangLogisticCauHinhHoTroShipFee/UpdateMany` | `DonHangLogisticCauHinhHoTroShipFeeController_UpdateMany` (DV-AD-TONG, DV-AD-CN) | public | - | Tách list: Id!=null → update, Id==null → insert; validate từng dòng + chống trùng GiaTriTroLen trong list; persist + clear cache + log từng dòng | EF | - |
| 5 | Lấy toàn bộ cấu hình chi nhánh | load màn | POST `api/DonHangLogisticCauHinhHoTroShipFee/GetAll` | `[SBAuthorize]` (mở — chỉ login) | public | - | Trả list cấu hình theo IdChiNhanh từ cache, sort tăng dần theo GiaTriTroLen (bậc thang) | EF + Redis cache | - |

## Quy tắc nghiệp vụ (gating / tier hỗ trợ phí ship)
1. **Đơn vị tier:** mỗi bản ghi = 1 bậc `GiaTriTroLen` (giá trị đơn hàng TỪ ngưỡng này) → `GiamGiaGiaTri` (giá trị hỗ trợ) + `KieuLoai`.
2. **KieuLoai (EnumKieuLoaiHoTroPhiShip):** `ThuThucTe` (thu phí ship thực tế) hoặc `HoTroGiam` (hỗ trợ giảm). Giá trị khác 2 enum này → reject "'Kiểu loại' không đúng".
3. **Validate bắt buộc:** GiaTriTroLen & GiamGiaGiaTri không null và ≥ 0; KieuLoai hợp lệ; GiaTriTroLen KHÔNG trùng trong cùng chi nhánh (Create/Update/UpdateMany đều check).
4. **Áp dụng khi tính phí (ngoài controller này):** `OrderService.TinhPhiVc` đọc cache `GetOrSetCache_CauHinhHoTro_ShipFee_CN(IdChiNhanh)` (đã sort theo GiaTriTroLen) để chọn bậc phù hợp với giá trị đơn → cộng vào `DonHangHoTroShipFee` (OrderService.cs:491).
5. **Scope:** cấu hình theo cặp IdDonVi + IdChiNhanh (per chi nhánh), không global.

## Quan hệ
- **Gọi sang:** `ICommonCacheService` (cache theo chi nhánh), `ILogActivityHelper` (log EventLichSuTacDong, TableId SCRM_ORDERLOGISTIC_DonHangLogisticCauHinhCod).
- **Dữ liệu (entities):** `DonHangLogisticCauHinhCod` (IdDonVi, IdChiNhanh, GiaTriTroLen, GiamGiaGiaTri, KieuLoai).
- **Ripple / đồng bộ:** mọi mutate → `ClearCache_CauHinhHoTro_ShipFee_CN(IdChiNhanh)` để TinhPhiVc đọc cấu hình mới ở lần tính phí kế tiếp.

## Lỗi đã biết / lưu ý kỹ thuật
- **Validate bug (overwrite message thay vì early-return)** — `Validate(...)` (DonHangLogisticCauHinhHoTroShipFeeServcie.cs:130-180) gán `message` tuần tự không break; nếu nhiều điều kiện sai, **chỉ message của check CUỐI** được trả về (vd vừa null vừa trùng → báo trùng, che lỗi null). Không sai data nhưng thông báo có thể không khớp lỗi đầu tiên.
- `Update` (service:95) tạo entity mới từ DTO rồi MarkDirty — KHÔNG set IdDonVi/IdChiNhanh trên object update (chỉ filter theo Id+đơn vị khi đọc Old/New); ổn vì MarkDirty chỉ 3 field, nhưng lưu ý nếu thêm field tenant sau này.
- `GetAll` permission `[SBAuthorize]` mở (chỉ login) trong khi Create/Update/Delete/UpdateMany gắn key riêng — đọc cấu hình không phân quyền theo key.
- Tier KHÔNG validate tính liên tục/chồng lấp ngoài chống trùng GiaTriTroLen — nếu cấu hình thiếu bậc, đơn dưới mọi ngưỡng sẽ không match bậc nào (logic chọn bậc ở OrderService quyết định).
