# Unit Test — bổ sung riêng SCRM

> ⚠️ Đọc **`rules/unit-test.md` TRƯỚC** (nguyên tắc + luật vàng + Given/When/Then + khuôn + cách chạy).
> File này CHỈ ghi phần **riêng của SCRM** — cái khác project khác.

---

## 1. Repository pattern SCRM: `IRepository<T>`
Interface trong `SB.Core` — mock được bằng Moq:
```csharp
var repo = new Mock<IRepository<CongViec>>();
repo.Setup(r => r.GetByIdAsync(It.IsAny<object>())).ReturnsAsync(entity);   // load-by-id
repo.Setup(r => r.InsertAsync(It.IsAny<CongViec>())).Returns(Task.CompletedTask);
repo.Setup(r => r.UpdateAsync(It.IsAny<CongViec>())).Returns(Task.CompletedTask);
repo.Setup(r => r.DeleteAsync(It.IsAny<CongViec>(), It.IsAny<bool>())).Returns(Task.CompletedTask);
```
> `DeleteAsync(entity, removeInDatabase = false)`:
> - **false (default) = xoá MỀM** (IsDeleted=true, khôi phục được).
> - **true = xoá CỨNG** (mất luôn — chỉ dùng cho bảng queue/replace hay tích rác).
> → **Verify chặt cờ này** (`Times.Once` với `false`/`true` đúng ý), vì sai cờ = bug nghiệp vụ.

## 2. Query IQueryable: `TableWithNolock()` → cần `MockQueryable.Moq`
Method dùng `TableWithNolock().Where(...).FirstOrDefaultAsync()/ToListAsync()/AnyAsync()`:
```csharp
// package: MockQueryable.Moq
var data = new List<CongViec> { entity1, entity2 }.BuildMock(); // IQueryable async-aware
repo.Setup(r => r.TableWithNolock()).Returns(data);
```
Không có `MockQueryable` → `ToListAsync()` trên IQueryable thường sẽ ném lỗi.

## 3. Dựng service SCRM
Constructor thường: `(DbContext context, IRepository<T> repo, ICommonCacheService cache, ...)`.
```csharp
// DbContext (concrete) — nếu method KHÔNG dùng _context (không transaction) → null!
var service = new XxxService((XxxDataContext)null!, repo.Object, cache.Object);
```
> Method chạy trong `_context.Database.CreateExecutionStrategy()` + `BeginTransactionAsync()` → **KHÔNG unit-test được sạch** (cần DbContext thật) → để integration test.

## 4. `IMethodResult<T>` — assert kết quả
| Factory trong code | → | Assert |
|---|---|---|
| `ResultWithData(data)` | | `result.Success == true`, `result.Data == data` |
| `ResultWithError(msg)` | | `result.Success == false`, `result.Message == msg` |
| `ResultWithError(msg, code)` | | `Message == msg`, `Error == code` |
> `Contants.MESS_NotFound` = "Đối tượng không tồn tại", `Contants.CODE_NotFound` = "01".

## 5. Multi-tenant — nhớ khi dựng entity giả
Entity SCRM thường có `IdDonVi` / `IdChiNhanh`. Khi test mapping/scope, set đúng để verify (vd clear cache theo `entity.IdDonVi`, không phải `userLogin.DonViId`).

## 6. Nơi để test + package thêm
- Test sống **trong repo scrm**, additive (vd folder `_UnitTests/` hoặc `*.Tests` cạnh module).
- VS Code 🧪: tạo **solution nhỏ** chỉ add project test → khỏi load `SCRM.sln` khổng lồ.
- Package thêm ngoài `rules/unit-test.md`: **`MockQueryable.Moq`** (khi test method query).

## 7. Ví dụ thật để copy
`DmMucDoUuTienService.XoaTheoIdAsync` — 5 test (mock + repo): happy (verify soft-delete + clear cache đúng IdDonVi) / not-found / validation (Guid.Empty) / exception (catch) / null-IdDonVi.
→ Method load-by-id (`GetByIdAsync`+`DeleteAsync`) mock sạch, KHÔNG cần MockQueryable — bắt đầu từ loại này dễ nhất.
