# DB Rules

## Nguyên tắc chung

Không tự thực thi bất kỳ lệnh nào thay đổi DB (INSERT, UPDATE, DELETE, DDL).

**Why:** User muốn review script trước khi áp dụng, tránh thay đổi ngoài ý muốn.

**How to apply:** Khi cần thay đổi DB — chỉ gen script SQL và trình bày để user xem xét. Chỉ chạy trực tiếp vào DB khi user nói rõ: "chạy đi", "apply đi", "làm vào db đi", hoặc tương đương.

> 🧰 **Cần gen script XÓA data** (nhiều mã / cascade nhiều DB service / có phanh `@Action` + confirm-time chống lỡ-save)? → dùng khuôn **`rules/xoa-data-db-an-toan.md`** (template + 13 kỹ thuật + cạm bẫy).

---

## Môi trường

- **TUYỆT ĐỐI KHÔNG** đụng vào DB production dưới bất kỳ hình thức nào — kể cả SELECT nếu có thể tránh được.
- Chỉ dùng DB dev (`dev.pushsale.vn.db`) để query/test.
- Staging DB: chỉ chạy script sau khi user confirm rõ ràng.

---

## 🔴 SELECT trên bảng LỚN — phải "ăn" được index, nếu không là quét sạch bảng

> **Sự cố thật 14/07/2026:** **CHÍNH CLAUDE sinh ra câu SELECT thống kê** (khảo sát phạm vi backfill B176) rồi đưa cho dev chạy trên product (63.x) → **disk read rate spike 3000–6000** lúc 13:30–14:30 (giờ cao điểm) → **dev bị claim**. Nguyên nhân: query quét toàn bộ `DonHangLogisticSanPham` (bảng chi tiết đơn hàng, cực to).
>
> 🔴 **Đây là LỖI CỦA AGENT, không phải của dev.** Dev tin script Claude đưa và chạy. ⇒ **Mọi câu SQL Claude đưa cho user chạy trên PRODUCT phải tự kiểm 3 vế dưới TRƯỚC KHI đưa**, kèm cảnh báo chi phí IO + giờ nên chạy. Đưa script full-scan cho user = **gây sự cố production**, dù chỉ là `SELECT`.

### 3 vế BẮT BUỘC — thiếu 1 vế là full scan

**1. Lọc bằng cột CÓ index** (`Id`, `IdDonVi`, `IdDonHang`, `NgayTao`, `EcommerceId`…).
KHÔNG lấy cột nghiệp vụ phụ (không index) làm điều kiện lọc chính — vd `TMDTSanChietKhau > 0`. Lọc bằng nó = bắt DB đọc hết bảng rồi mới loại.

**2. Cột index phải để TRẦN trong `WHERE`** (sargable) — bọc hàm là index chết ngay:
```sql
WHERE ISNULL(sp.TMDTSanChietKhau, 0) > 0            -- ❌ index vô dụng
WHERE sp.TMDTSanChietKhau > 0                       -- ✅ (NULL tự loại)

WHERE CONVERT(date, dh.NgayTao) = '2026-07-14'      -- ❌ index NgayTao vô dụng
WHERE dh.NgayTao >= '2026-07-14' AND dh.NgayTao < '2026-07-15'   -- ✅
```
*(Bọc hàm trong `SELECT`/`SUM` thì vô hại — chỉ cấm ở `WHERE`/`JOIN ON`.)*

**3. Bảng có `IdDonVi` ⇒ LUÔN có `IdDonVi` trong điều kiện.** Vừa đúng multi-tenant, vừa là cột index chính → cắt ngay phần lớn dữ liệu.

### Thứ tự truy vấn: CHA trước, CON sau
❌ Bắt đầu từ bảng **chi tiết** (to nhất) rồi join ngược lên đơn.
✅ Lọc bảng **cha** (`DonHangLogistic`) bằng cột index trước → còn ít dòng → mới join xuống bảng chi tiết:
```sql
WITH don AS (
    SELECT Id, IdDonVi, ... FROM dbo.DonHangLogistic WITH (NOLOCK)
    WHERE IsDelete = 0 AND EcommerceId = 9
      AND NgayTao >= @tu AND NgayTao < @den      -- cột index, để trần
)
SELECT ... FROM don d
JOIN dbo.DonHangLogisticSanPham sp WITH (NOLOCK)
      ON sp.IdDonHang = d.Id AND sp.IdDonVi = d.IdDonVi AND sp.IsDelete = 0
WHERE sp.TMDTSanChietKhau > 0
OPTION (MAXDOP 1);
```

### Khi buộc phải chạy trên PRODUCT
- **`OPTION (MAXDOP 1)`** — đừng ăn hết CPU/IO.
- **Chia nhỏ khoảng thời gian** (từng 1–2 tuần) rồi cộng lại — thay vì 1 phát quét 2 tháng.
- **Chạy ngoài giờ cao điểm** (spike lần này rơi đúng 13:30–14:30).
- Chỉ cần đếm/khảo sát → ưu tiên **replica / DB backup**, đừng đụng product.

---

## 🔴 Entity — kiểu cột chuỗi (VARCHAR/NVARCHAR) — áp MỌI project EF Core

**Áp cho:** mọi project dùng EF Core + `EntityBase`/`BaseEntity` (scrm, ecommerce_connect, ps_delivery, delivery_connect/receive/status). Khi TẠO/SỬA entity có `public string`.

**Rule:** MỌI string property PHẢI khai `[Column(TypeName = "...")]` — **không để trống**.
- `VARCHAR(n)` — chuỗi **ASCII-only**: `MaXXX`, `Code`, `StatusCode`, `EventName`, `TenDangNhap`, khóa/enum. Vd: `[Column(TypeName = "VARCHAR(50)")]`.
- `NVARCHAR(n)` — chuỗi **tiếng Việt / Unicode**: `TenXXX`, `MoTa`, `GhiChu`, `TieuDe`, mọi text user thấy. Vd: `[Column(TypeName = "NVARCHAR(200)")]`.
- **Chọn `n` đủ dùng, KHÔNG mặc định MAX.** `NVARCHAR(MAX)`/`VARCHAR(MAX)` chỉ khi thật sự cần (JSON/HTML/blob dài) — MAX không index được + kéo cả row nặng.

**🐛 Bẫy (lý do tồn tại rule):** thiếu `[Column(...)]` → **EF tự map `nvarchar(max)`** → cột không index được, query chậm, tốn RAM. Đây là lỗi **HAY LỌT NHẤT** khi member tạo entity.

**Cách QA/review PHẢI catch (chủ động, không đợi nhớ):**
1. Grep `public string` trong file entity vừa đụng.
2. Mỗi dòng string → dòng ngay trên PHẢI có `[Column(TypeName = "...")]`. Thiếu = **FAIL**.
3. Có `[Column]` nhưng dùng `NVARCHAR(MAX)`/`VARCHAR(MAX)` cho field ngắn (Ma/Ten/code) = **FAIL** (sai kiểu/độ dài).
4. `Ma*/Code/username/status` mà để `NVARCHAR` (nên `VARCHAR`), hoặc `Ten*/MoTa/GhiChu` để `VARCHAR` (mất dấu tiếng Việt → nên `NVARCHAR`) = **FAIL** (sai loại).

> Chi tiết scrm + ví dụ BaseEntity: `projects/scrm/patterns.md` (String Property Rule) · `projects/scrm/qa-code.md` (CRITICAL String Property Column Type) · recipe tạo entity: `projects/scrm/skills/tao-entity.md`.
