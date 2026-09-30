---
name: project_hoadon_yeucau_sp_todo
description: TODO deploy — thêm cột IsYeuCauXuatHoaDon vào 2 SP list (chỉ cần khi bật SQL-fallback mode)
metadata:
  type: project
---

# TODO còn treo — cột IsYeuCauXuatHoaDon trong 2 SP list

**Việc:** Thêm cột `IsYeuCauXuatHoaDon` vào SELECT của 2 stored proc (DB `SCRM_Order_Logistic`):
- `PRC_SCRM_Order_Logistic_DonHangLogistic_ThuKhoTacNghiep_GetListByIds` (màn thu-kho)
- `PRC_SCRM_Order_Logistic_DonHangLogistic_DonHangSale_GetListByIds` (màn sale/store)

**Vì sao CHƯA làm ngay:** 2 SP này chỉ chạy ở nhánh **SQL-fallback** (config `Allows_Return_OrderLogistic_Results_ElasticSearch_From_SQL = true`). Hiện khách **dùng ES là chính** → nhánh SP KHÔNG chạy → chưa cần. Nhánh ES đã select cột đủ (thu-kho `KhoTacNghiepService` ~2457/3358/3412, sale `DonHangService` ~3824 + `DonHangStoreService` ~2416/3699).

**An toàn tạm thời:** nếu SP có chạy mà thiếu cột → Dapper để `null` (không crash, DTO đã có field).

**Khi nào PHẢI làm:** trước khi bật SQL-fallback mode, hoặc nếu muốn cột hiện đúng ở chế độ đọc-từ-SQL. User (teamdev2) đã dặn **nhắc lại** khi tới lúc thêm.

Thuộc feature **Yêu cầu xuất HĐ (đơn logistic)** — chi tiết: `projects/scrm/features/hoa-don-yeu-cau-xuat-logistic/dev.md` (mục 7).
