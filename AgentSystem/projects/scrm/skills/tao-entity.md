# Recipe — Tạo Entity mới (EF Core)

**Khi nào dùng:** tạo class entity mới (hoặc thêm field vào entity) trong `{Module}.DataContext/Entity/`. Áp cho SCRM và mọi service EF Core kế thừa `BaseEntity`/`EntityBase`.

**Mục tiêu:** code đúng ngay từ đầu để **KHÔNG lọt** ở review — nhất là kiểu cột chuỗi (lỗi hay lọt nhất).

---

## Checklist BẮT BUỘC khi tạo/sửa entity

☐ **Kế thừa `BaseEntity`** — đã có sẵn `Id (Guid)`, `NgayTao`, `NgayCapNhat`, `IsDeleted` (soft-delete). KHÔNG tự khai lại.

☐ **Multi-tenant:** có `IdDonVi (Guid?)` + `IdChiNhanh (Guid?)` nếu là bảng nghiệp vụ theo đơn vị/chi nhánh.

☐ **🔴 MỌI `public string` PHẢI có `[Column(TypeName = "...")]`** (dòng ngay trên field):
   - `VARCHAR(n)` — **ASCII-only**: `MaXXX`, `Code`, `StatusCode`, `EventName`, `TenDangNhap`, enum/khóa.
   - `NVARCHAR(n)` — **tiếng Việt / Unicode**: `TenXXX`, `MoTa`, `GhiChu`, `TieuDe`, mọi text user thấy.
   - Chọn `n` đủ dùng. **KHÔNG** để MAX trừ khi JSON/HTML/blob dài thật sự.

☐ **String không nullable → default `= ""`** (empty string), không để null vô ý.

☐ **KHÔNG khóa ngoại (FK constraint)** — chỉ dùng field `Guid` reference, không tạo FK relationship trong DB.

☐ **Field snapshot** (lưu tại thời điểm tạo) phải nhất quán với các field snapshot khác cùng entity (lưu cùng lúc).

---

## Mẫu chuẩn

```csharp
public class CongViec : BaseEntity
{
    public Guid? IdDonVi { get; set; }      // multi-tenant
    public Guid? IdChiNhanh { get; set; }

    [Column(TypeName = "VARCHAR(50)")]      // ASCII: code/enum/status
    public string MaCongViec { get; set; } = "";

    [Column(TypeName = "NVARCHAR(200)")]    // tiếng Việt: name/title
    public string TieuDe { get; set; } = "";

    [Column(TypeName = "NVARCHAR(500)")]    // tiếng Việt, nullable OK
    public string? GhiChu { get; set; }
}
```

---

## 🐛 Bẫy thường gặp

1. **Thiếu `[Column(TypeName)]`** → EF tự map `nvarchar(max)` → cột không index được, query chậm, kéo cả row nặng. **Đây là lỗi HAY LỌT NHẤT.** Tự grep `public string` trong file trước khi báo xong.
2. **Sai loại:** `Ma*/Code` để `NVARCHAR` (thừa, nên `VARCHAR`); `Ten*/MoTa` để `VARCHAR` (mất dấu tiếng Việt → phải `NVARCHAR`).
3. **Lạm dụng MAX:** để `NVARCHAR(MAX)` cho field ngắn (tên, mã) → không index, nặng vô ích.
4. **Tự tạo FK** → sai convention (hệ thống dùng Guid reference, không FK).

## Tự verify trước khi báo xong
```bash
# grep mọi string trong entity vừa tạo — mỗi cái phải có [Column] dòng trên
rg "public string" {Module}.DataContext/Entity/{Entity}.cs -B1
```

## Nguồn
- Rule nền: `projects/scrm/patterns.md` (String Property Rule) · `projects/scrm/qa-code.md` (CRITICAL String Property Column Type)
- Rule chung mọi project: `rules/db.md` → "Entity — kiểu cột chuỗi"
- review-agent bắt buộc catch: `agents/review-agent.md` → Hard-checks
