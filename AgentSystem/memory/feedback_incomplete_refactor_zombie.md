---
name: Incomplete refactor + zombie fields
description: Khi refactor công thức/logic phải xóa sạch field cũ và mọi nơi gán chúng, nếu không sẽ sinh zombie field gây bug "câm" khó phát hiện
type: feedback
---

Khi refactor 1 công thức/logic khỏi 1 field, phải đảm bảo **xóa sạch**:
1. Tất cả mọi nơi **gán** field đó (assignments)
2. Field đó trong **model/interface** (nếu có)
3. Field đó trong **DTO** gửi/nhận BE (nếu có)
4. Field đó trong **HTML template** (dù là input đã comment)

Không được để field tồn tại "cho an toàn" hoặc "phòng sau này cần". Zombie field = bug sắp tới.

**Why:** Case thực tế SCRM PhieuXuat `popup-phieu-xuat-edit.component.ts`:
- 2025-05-27 (NganLQ `f2fa64025`): Form tạo mới theo mô hình giá bán (có `item.gia`, chiết khấu, VAT).
- 2025-05-28 (NganLQ `54f9f6bd0`): Refactor sang giá vốn, xóa công thức VAT/chiết khấu, đổi `onBlur` dùng `giaVon`. Nhất quán. **NHƯNG không xóa dòng `item.gia = sanPham.gia` trong `onChangeSanPham`.** Field `item.gia` trở thành zombie: vẫn gán mỗi khi chọn SP, không dùng vào công thức nào.
- 2025-06-12 (NganLQ `7e7310ebf`, commit "update code fix bug"): Ngan thêm lại `item.thanhTien = e.gia × e.soLuong` vào `onBlur` → resurrected zombie, tạo ra mismatch: cột "Thành tiền" dùng giá bán, "Tổng" dùng giá vốn. Bug sống 5 tháng đến khi user phát hiện.

Tên field `item.gia` gây confusion thêm: lúc chọn SP mới = giá bán (`SanPham.GiaSanPham`), lúc load phiếu từ BE = giá vốn (`KhoChiTiet.GiaNhapTrungBinh` — BE overwrite). Cùng 1 tên, 2 ý nghĩa tùy context → rất dễ nhầm khi viết công thức.

**How to apply:**
- Khi refactor: sau khi đổi công thức, **grep** field cũ trong file → xóa mọi assignment còn sót, xóa khỏi model/DTO, xóa khỏi HTML. Đi kèm [feedback_remove_unused_code.md](feedback_remove_unused_code.md).
- Khi review commit "fix bug" vague (không nói bug gì): phải hỏi/trace để hiểu intent, đừng assume.
- Khi viết code đụng field có tên mơ hồ (`gia`, `value`, `amount`...) mà ý nghĩa thay đổi theo context: rename để tách bạch (vd `giaBanGoc`, `giaVonGoc`), hoặc thêm comment chỉ rõ ý nghĩa tại điểm gán.
- Khi field ở FE đã bị BE overwrite bất kể FE gửi gì: đặt comment cảnh báo để người sau biết `// BE overwrite this field, FE value is ignored on save`.
