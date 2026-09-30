---
name: feedback-tach-field-quet-het-noi-hien-thi
description: Tách 1 field overload thành 2 → phải quét HẾT nơi ĐỌC/HIỂN THỊ field cũ (không chỉ nơi GHI). Hay sót: list nhiều màn × nhiều view mode × nhiều read-path (ES/DB/SP), cột bị mislabel (label mới nhưng field cũ)
metadata:
  type: feedback
---

**Khi tách 1 field bị overload thành 2 field riêng (field-split): phía GHI (assignment) thường nhớ, nhưng phía ĐỌC/HIỂN THỊ có RẤT NHIỀU site ẩn dễ sót.** [[feedback-incomplete-refactor-zombie]] lo phía GHI; đây là phía ĐỌC.

**Why:** field overload cũ xuất hiện ở đủ nơi hiển thị/đọc — khi tách, mỗi nơi đó phải re-point sang field đúng. Sót 1 nơi = chỗ đó hiện sai/trống mà nơi chính vẫn đúng (bug câm, chỉ lộ khi user soi đúng màn). Bằng chứng: Task 1046 SCRM (tách `NgayMuonNhanHang` khỏi `GiaoHangNgayGiaoHang`) — làm xong field + form + filter + list chính, NHƯNG sót **cột "Ngày muốn nhận" trên list thu-kho + kế-toán** (2 màn × 2 view mode × ES-Select/SQL-fallback/stored-proc) vẫn bind field cũ → user phát hiện qua API response. Phải quay lại làm bổ sung. Xem [[feedback-scrm-ngaymuonnhan-vs-ngaygiao]].

**How to apply — checklist QUÉT phía đọc khi field-split:**
1. **Liệt kê MỌI màn/list** hiển thị field cũ (không chỉ màn chính). Cùng 1 nghiệp vụ thường có nhiều màn (sale/kho/kế-toán...) render lại cùng data.
2. **Mỗi màn có nhiều VIEW MODE / layout** → check từng mode (vd grid phẳng vm1 vs grid gộp-cột vm2 render khác template).
3. **Mỗi list có thể có nhiều READ-PATH** trả cùng DTO: ES `.Select` + nhánh SQL-fallback (config) + **stored proc** (Dapper map theo tên cột → SP phải SELECT thêm cột). Grep hết nơi construct/return DTO — kể cả `SqlQueryAsync<Dto>` (không phải chỉ `new Dto`).
4. **Cột dễ bị MISLABEL**: label = nghĩa mới nhưng `field`/binding = field cũ (do trước đây overload nên "tạm đúng"). **Đừng tin label — check field/binding thực tế.** Sửa cả nơi hiển thị VALUE lẫn key binding (vd Angular `[ngSwitch]="col.dataType"`: đổi `field` phải đổi kèm `*ngSwitchCase`).
5. **Dòng update-local sau khi lưu** (grid patch tại chỗ) cũng đọc/ghi field cũ → đổi.
6. **Tooltip/title/native `title`** trên cell cũng có thể đọc field cũ (hoặc object lồng khác) → đổi cho nhất quán.
7. Field mới để **top-level DTO** hay **object lồng**? HTML/FE phải đọc đúng chỗ đã map (map top-level thì đừng đọc `data.info.field`).

**Cách rà nhanh:** `grep` tên field CŨ trong toàn bộ FE 2 màn (ts + html) + BE (mọi service construct DTO + SP) SAU khi sửa → còn sót nơi nào hiển thị field cũ ở cột nghĩa mới là chưa xong. Dùng review-agent để bắt read-path ẩn (SP/Dapper) — phiên 1046 chính review-agent bắt được nhánh SQL-fallback bị sót.

Liên quan [[feedback-incomplete-refactor-zombie]] (phía ghi), [[feedback-study-existing-flow]] (đọc luồng trước), [[feedback-scrm-ngaymuonnhan-vs-ngaygiao]].
