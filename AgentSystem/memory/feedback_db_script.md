---
name: DB script — luôn dùng DB_NAME.dbo.TableName
description: Mọi SQL script phải prefix tên DB trước dbo (VD Customer.dbo.KhachHang, OrderLogistic.dbo.DonHangLogistic)
type: feedback
---

Khi gen SQL script, luôn dùng format `DB_NAME.dbo.TableName` thay vì chỉ `dbo.TableName` hoặc `TableName`.
Phải check `appsettings.Staging.json` để lấy đúng tên DB thực tế trước khi gen script.

**Why:** User chạy script trên SSMS có thể đang ở DB khác, thiếu prefix sẽ query sai DB.

**How to apply:** 
- Check `database=...` trong connection string của `appsettings.Staging.json`
- Tên DB thực tế: `SCRM_Order_Logistic`, `SCRM_Customer`, `SCRM_Core`, `SCRM_Order`, `SCRM_WarehouseProduct`, `SCRM_Report`
- Mọi SELECT/INSERT/UPDATE/DELETE phải có `SCRM_Order_Logistic.dbo.`, `SCRM_Customer.dbo.`, etc.
