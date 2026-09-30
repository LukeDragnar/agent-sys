# Lesson — Deploy entity có field MỚI mà quên cột DB ⇒ PRODUCT GÃY

> **Sự cố thật: 2026-07-14.** Deploy `Order.API` (feature "Yêu cầu xuất hóa đơn cho đơn thu ngân", CV3536-2) lên product → **`POST /api/DonHang/TaoDon` chết ngay**, không tạo được đơn offline nào.

## Triệu chứng
```
An error occurred while saving the entity changes. See the inner exception for details.
   at Order.DataContext.EfRepository`1.InsertAsync(T entity)      EfRepository.cs:207
   at Order.Services.Generated.DonHangService.TaoDon(...)          DonHangService.cs:978
   at Order.API.Controllers.DonHangController.TaoDon(...)          DonHangController.cs:100
```
> `EfRepository.InsertAsync` + *"error saving the entity changes"* = **EF `SaveChanges` chết** — 90% là **schema DB không khớp entity**.

## Nguyên nhân
- Commit thêm **1 field mới** vào entity: `Order/Order.DataContext/Entity/DonHang.cs` → `public bool? IsYeuCauXuatHoaDon { get; set; }`
- `TaoDon` set field đó (`DonHangService.cs:856`) → EF sinh `INSERT ... IsYeuCauXuatHoaDon ...`
- **DB product CHƯA CÓ cột** → INSERT fail → **mọi lệnh tạo đơn chết**.

## 🔴 GỐC RỄ: SCRM **KHÔNG CÓ MIGRATION VERSIONED**
- Module `Order` **không có file `.sql` nào** trong repo (`git log -- Order/*.sql` = rỗng).
- Mọi thay đổi schema (**ALTER TABLE**, **sửa Stored Procedure**) đều là **CHẠY TAY** trên từng môi trường.
- → Deploy code KHÔNG tự tạo cột. **Staging chạy tay rồi ≠ product đã chạy.** Cực dễ quên bước product.
- Cùng bệnh với `ecommerce_connect`: file `Accounting/_Sql/ChungTuMuaDichVu_ChiPhiMuaHang.sql` **bị xóa khỏi repo sau khi chạy DB tay** → môi trường mới không dựng nổi schema.

## ✅ LUẬT — task đụng entity/DB
1. **Thêm/sửa field entity ⇒ BẮT BUỘC liệt kê bước DB** (ALTER TABLE + sửa SP + reindex ES) vào **sổ chờ-update** (`memory/local/qlcv/CHO-UPDATE.md`) NGAY khi vào `13. Test hoàn thành`.
2. **Deploy product = 2 phần, không phải 1:** (a) đẩy code · (b) **chạy script DB**. Thiếu (b) → **gãy production**, không phải "thiếu tính năng".
3. Script ALTER phải **idempotent** (`IF NOT EXISTS ... ALTER`), để chạy lại an toàn.
4. Field mới **nullable** (`bool?` → `BIT NULL`) → đơn cũ = NULL = luồng cũ (zero regression). Đừng `NOT NULL` không default.
5. **SP cũng phải chạy tay** — vd `PRC_SCRM_Order_SearchDanhSachDon` thêm cột hiển thị. SP **không nằm trong repo** → phải giữ bản đã sửa lúc làm staging để chạy lên product.
6. Đổi **ES model/DTO** → cân nhắc **reindex**, không thì cột mới hiện "—" cho data cũ.

## Script chữa cháy (mẫu)
```sql
IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('dbo.DonHang') AND name = 'IsYeuCauXuatHoaDon')
    ALTER TABLE dbo.DonHang ADD IsYeuCauXuatHoaDon BIT NULL;
```

## Cách nhận diện sớm (khi review/QA)
- Diff có đụng `*/DataContext/Entity/*.cs` **thêm property** → **hỏi ngay: "đã có script ALTER cho product chưa?"**
- Commit message có chữ **"chạy tay"** / **"thêm cột"** → chắc chắn có bước DB thủ công.
- Review checklist: xem `rules/db.md` (kiểu cột chuỗi) + lesson này (tồn tại cột).

## Liên quan
- `memory/local/qlcv/CHO-UPDATE.md` — sổ chờ-update (nơi phải ghi bước DB)
- `projects/ecommerce_connect/` — cùng bệnh (SQL bị xóa khỏi repo sau khi chạy tay)
