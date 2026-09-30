---
name: Comment code — hợp lý, dễ maintain, không thừa
description: Khi sửa/viết code phải comment đúng chỗ cần thiết để dễ maintain, không comment thừa mô tả what code đã nói rõ
type: feedback
---

Khi viết/sửa code, **comment có chọn lọc** — cân bằng giữa "không comment gì" và "comment dày đặc".

**Why:** Code không comment WHY khó maintain khi fix bug sau này (phải đoán intent). Ngược lại comment what/how rườm rà làm code nặng, outdated nhanh, gây nhiễu.

**How to apply:**

### ✅ PHẢI comment khi:

1. **WHY không obvious** — quyết định business, workaround, constraint ẩn
   ```csharp
   // Phải update log trước khi gọi Customer API vì nếu Customer fail mà log chưa ghi → retry sẽ double-count
   ```
2. **Công thức nghiệp vụ / magic number** — ghi rõ nguồn gốc (spec, ticket, requirement)
   ```csharp
   // TienThanhDiem=1000: 1 điểm cho mỗi 1000đ (config DmThamSo, xem qa-spec CHI_TIEU)
   ```
3. **Edge case được xử lý cố ý** — tránh người sau "tưởng bug" rồi sửa nhầm
   ```csharp
   // Legacy data SoTienTarget=null → coalesce về 0 gây delta sai, phải skip riêng
   if (p.SoTienTarget == null) continue;
   ```
4. **Thứ tự bắt buộc** — khi order of operations là load-bearing
   ```csharp
   // Phải hạ IsMoiNhat TRƯỚC khi insert pending mới, tránh race job claim 2 pending cùng lúc
   ```
5. **Workaround cho bug / giới hạn external** — nêu bản chất + link ticket nếu có

### ❌ KHÔNG comment khi:

1. **Chỉ mô tả WHAT code đã nói** — `// Tăng counter lên 1` cho `counter++` là rác
2. **Tên hàm/biến đã tự giải thích** — đặt tên rõ thay vì comment
3. **Lịch sử thay đổi** — "added by X, fix for issue #123", "removed old logic" → belongs PR/commit message
4. **Block comment dài nhiều đoạn** mô tả toàn bộ method — không ai đọc, chia method nhỏ hoặc dùng docstring ngắn 1 dòng
5. **Comment TODO không có action cụ thể** — `// TODO: improve this` vô nghĩa, xóa hoặc viết rõ
6. **Kể lại hành trình debug / lý do gặp lỗi / so sánh với code khác** — comment phải **tổng kết code làm gì ở mức tổng quan**, KHÔNG narrate "vì gặp lỗi X nên dùng Y", "khác chỗ Z vì...". User feedback: comment giải thích tổng quan, không thể hiện ví dụ vì sao phải code thế này do gặp lỗi gì.
   ```ts
   // ❌ display block + margin auto: tự căn giữa, VÌ nz-tag custom-color không ăn text-align của td (khác tag preset như Phương án/Trạng thái)
   // ✅ chữ đen + tự căn giữa (nền HEX custom nên set thủ công)
   ```

### Quy mô

- Method thuần CRUD, naming rõ → 0 comment là đúng
- Method có logic nghiệp vụ phức tạp → 1-3 comment WHY ở chỗ load-bearing
- KHÔNG viết docstring rườm rà kiểu `/// <summary>...</summary>` cho mọi method private — chỉ dùng cho public API cần IntelliSense

### Khi refactor

- Comment cũ **outdated** → xóa hoặc update, không để stale
- Comment cũ **vẫn đúng** → giữ, đừng xóa chỉ vì "code gọn"
- Comment cũ mô tả **behavior đã đổi** → bắt buộc update, nếu không đọc comment sẽ hiểu sai

### KHÔNG xóa comment cũ ngoài ý đồ (preservation)

- Việc xóa/sửa comment ở trên **chỉ áp dụng khi đang CHỦ Ý maintain đúng comment đó**.
- Edit **không liên quan** (vd wrap đa ngôn ngữ, đổi 1 dòng khác trong file) → **giữ NGUYÊN 100% comment cũ** (`//` lẫn `<!-- -->`), kể cả **text tiếng Việt trong comment** (không dịch, không xóa).
- **Why:** process multi-lang từng xóa nhầm nhiều comment → user yêu cầu không lặp lại. Comment cũ có thể cần dùng lại sau.
