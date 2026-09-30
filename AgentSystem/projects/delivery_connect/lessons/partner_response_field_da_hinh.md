# Lesson — Partner trả field response ĐA HÌNH (array `[]` lúc rỗng, object lúc có lỗi)

> Bug-sự-cố thật (SuperShip, 2026-06). Áp được cho MỌI partner: response đối tác hay trả 1 field lúc là array rỗng `[]`, lúc là object có dữ liệu.

## Hiện tượng
Đăng đơn / tính phí SuperShip lỗi nhưng:
- **Tính phí CRASH**: `Cannot deserialize the current JSON array ([]) into type 'ResponseErrorCheckFee'` — văng exception ngay trong `ClientHelper.ApiGet` (deserialize), vỡ luồng, user nhận lỗi khó hiểu.
- **Đăng đơn nuốt message**: SCRM nhận `"Đăng đơn không thành công : []"` — mất hẳn nội dung lỗi thật.

## Nguyên nhân
SuperShip trả field `errors` **đa hình**:
- Lỗi token/auth → `"errors": []` (**array rỗng**), và text người-đọc nằm ở **`message` top-level**.
- Lỗi validate field → `"errors": { ... }` (**object** có field lists / code).

Hai cái sai cộng lại:
1. Model khai `errors` là **type cứng** (`ResponseErrorCheckFee` = object) → Newtonsoft gặp `[]` → **throw** (deserialize fail).
2. Code build message **chỉ đọc `errors`** → khi `errors` rỗng (`[]`) thì serialize ra chuỗi `"[]"`, còn `message` (chỗ chứa text thật) bị **bỏ** vì model không map.

## Cách xử lý (đã áp)
1. **Field đa hình → khai `object?`**, đừng type cứng → không bao giờ crash deserialize. Parse có điều kiện phía service: `JsonConvert.SerializeObject(errors)` rồi chỉ `DeserializeObject<T>` khi `StartsWith("{")` (là object); `[]`/`{}`/`null` → coi như rỗng.
2. **Thêm field `message` top-level vào model** + khi build message lỗi: **ưu tiên/kết hợp `message`**, fallback `errors` khi rỗng. (Không có token thì `errors` rỗng → phải lấy `message` mới ra "API Token hết hạn".)
3. **Zero-regression:** nhánh `errors` CÓ data vẫn ra y như cũ; chỉ nhánh rỗng mới dùng `message`.

## Bonus — log mù khi parse fail
`ApiGet` vốn **chỉ log `resData` (đã deserialize), SAU khi parse** → deserialize throw thì **không log gì cả** → mù hoàn toàn body đối tác. `ApiPost` thì log `resString` (raw) **TRƯỚC** deserialize → vẫn thấy khi parse lỗi.
→ **Luôn log raw `resString` TRƯỚC deserialize** (đã sửa `ApiGet` cho khớp `ApiPost`; bỏ luôn dòng log `resData` thừa).

## Checklist khi map response đối tác
- ☐ Field nào đối tác CÓ THỂ trả khác kiểu (array vs object vs null)? → `object?` + parse điều kiện, đừng type cứng.
- ☐ Text lỗi người-đọc nằm ở field nào (`message` top-level hay trong `errors`)? → map đủ, ưu tiên cái dễ đọc.
- ☐ Client helper có log raw body TRƯỚC deserialize không? (để debug được khi parse fail)
