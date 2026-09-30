# EcommerceConnect — QA Code Checklist

Checklist review code để tránh lỗi convention / structural lặp lại.

## Persistence & Stored Procedures

- Khi thấy `await SomeSP(parent.Id)` hoặc `await InsertXxx_ByYyyId(id)` → **bắt buộc verify** entity `parent` đã được `InsertAsync` / `UpdateAsync` trước. Set property in-memory **không tính là persist**. (Xem `patterns.md` → Pattern: Persist parent TRƯỚC khi gọi SP.)
- Khi `await reposXxx.InsertAsync(entity)` mới đặt sau khi đã có nhánh thoát sớm (continue / return) → check kỹ thứ tự để không phá nhánh khác.
- Khi sửa entity → `MarkDirty(nameof(entity.Field))` cho từng field thay đổi trước `UpdateAsync`. Không có MarkDirty → SqlSugar bỏ qua field.
- **`DeleteAsync` là soft-delete + persist dirty** (project này): `MarkDirty(Description) → DeleteAsync(entity)` là **1 hit DB**, set `IsDeleted=true` + persist mọi field đã MarkDirty. KHÔNG cần `UpdateAsync` riêng trước `DeleteAsync` (thừa, 2 hit). Khi QA: **KHÔNG flag** `MarkDirty → DeleteAsync` là "field không persist" — đó là false-positive. (Áp dụng `ecommerce_connect`; SCRM/OrderLogistic chưa verify.)
- **Luồng sync/create thủ công** (nút bấm, không phải webhook job): KHÔNG `InsertAsync` upfront → build in-memory, push ngoài, **thành công mới Insert** (tránh row mồ côi `Status=null`). Webhook job thì ngược lại (Insert-upfront + retry net). Xem `patterns.md` → Defer persist.

## Ranh giới logic ECOMM vs CRM

- Add field/config/logic mới → tự hỏi "dùng cho mọi nguồn (kể cả non-sàn) hay chỉ 1 sàn?". Mọi nguồn → **CRM/SCRM-side**; chỉ 1 sàn → ECOMM. Generic business logic (auto-create HBBTL, deadline, phí) KHÔNG nhồi vào ECOMM/UnitConnect. Xem `patterns.md` → Ranh giới ECOMM vs CRM.

## Retry Jobs

- Khi job retry call external service (CRM / Shopee / TikTok) với FK fields (CrmOrderId, CrmUnitId, ...) → **bắt buộc re-lookup** nếu field hiện tại NULL. Không dùng giá trị stale từ row đang retry.
- Job retry phải có giới hạn rõ ràng: `ResendNo` max hoặc `CreatedDate` cutoff. Tránh zombie record retry vô hạn rồi không pickup nữa vì hết time window.
- Khi pick batch để retry: `.Where(x => x.InProcess == false ...)` + ngay sau đó `lock` bằng cách set `InProcess = true` rồi `UpdateAsync` → tránh duplicate processing khi nhiều job instance.

## External API Responses

- Khi forward data từ external API (Shopee, TikTok) sang service downstream (CRM) → check `item.error` / status field trong **từng item**, không assume `httpResponse.Success == true` đồng nghĩa data hợp lệ.
- API list/batch response thường wrap success ở level ngoài nhưng có error per-item bên trong → loop check trước khi map.
- Khi map field từ JSON response: `item.invoice_detail?.field` an toàn syntax nhưng → nếu `invoice_detail = null` thì TẤT CẢ field map sẽ null → check `invoice_detail == null` ở đầu nhánh, không build object với toàn field null rồi gửi đi.

## Logging & Status

- Description / message khi `SetDelete` hoặc `SetResend` phải nói rõ lý do (Shopee error / no CrmOrderId / CRM reject) — không dùng message generic kiểu "Cập nhật thành công" cho case bỏ qua.
- Service dùng `_logDataService` → `SetInfoBase(nameof(Method), ...)` ở đầu method + `WriteLog(...)` trong `finally` (xem `patterns.md` → Logging/SetInfoBase).
- Mỗi catch block → log `LogDataService.FormatLogDataException(ex)`, không nuốt exception.

## Query Performance

- Khi lookup chỉ cần 1-2 field từ entity có nhiều cột `NVARCHAR(MAX)` (`OrderData`, `ProductDraft`, `DataReceive` đều có 5+ MAX columns) → dùng `.Select(o => new { o.FieldA, o.FieldB }).FirstOrDefaultAsync()` để tránh kéo về toàn bộ row. Đặc biệt trong job batch 50-100 record.
- `.TableWithNolock()` cho query read-only để tránh lock.

## Threading & Async

- Không gọi `.Result` / `.Wait()` trên async — luôn `await`. Code generated có thể có chỗ legacy sai.
- Trong loop foreach gọi `await` lần lượt là cố ý (sync semantic) — không tự ý đổi sang `Task.WhenAll` mà không hiểu order dependency.

## Entity — kiểu cột chuỗi (lúc TẠO/SỬA entity)

- MỌI `public string` PHẢI khai `[Column(TypeName = "...")]` — thiếu → EF sinh `nvarchar(max)` (không index, kéo cả row nặng — chính các cột `OrderData`/`ProductDraft`/`DataReceive` MAX ở note "Query Performance" là hệ quả để lại). Grep `public string` trong entity → mỗi field phải có `[Column]` dòng trên.
- `VARCHAR(n)` cho ASCII (Ma/Code/Status/username); `NVARCHAR(n)` cho tiếng Việt/Unicode (Ten/MoTa/GhiChu). Chọn `n` đủ dùng, **KHÔNG** mặc định MAX (chỉ MAX khi JSON/HTML/blob dài thật sự).
- Rule chung mọi project: `rules/db.md` → "Entity — kiểu cột chuỗi".

## Code Convention

- Comment giải thích **WHY** (lý do business / race condition / workaround), không comment **WHAT** (đã có tên method/var rồi).
- Tên method action: `ProcessXxx`, `SendXxx`, `SetDeleteXxx`, `SetResendXxx`. Đừng phá pattern.
- Entity kế thừa `EntityBase` → có sẵn `Id`, `IsDeleted`, `CreatedBy*`, `UpdatedBy*`, `DeletedBy*`. Đừng tự khai báo lại.
