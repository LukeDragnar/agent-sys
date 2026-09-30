# Template — Phiếu bàn giao Dev → Test (E2E)

> Copy khuôn này khi **code xong 1 feature** cần test E2E. Dev điền → giao cho team test (Playwright) → họ viết test-case bám phiếu, không phải đoán.
> Lưu: `memory/local/{feature}-e2e-handoff.md` (gitignore). Spec đính kèm viết theo `templates/spec.md`.
> Mẫu vàng đã chạy thật: `memory/local/ghtk-e2e-handoff.md` + 2 file `tieu-chi-nghiem-thu-ghtk-*`.
> **Nguyên tắc:** phiếu thiếu mục BẮT BUỘC → test sai/không chạy được. "Đọc spec rồi chạy" thay vì "test 1 feature mất cả buổi".

---

## Cấu trúc phiếu (copy phần dưới)

```markdown
# Phiếu bàn giao Dev → Test: <feature>

## 0. Phạm vi  [BẮT BUỘC]
- ✅ LÀM (in scope): <các luồng/màn thuộc đợt này>
- ❌ NGOÀI phạm vi / CHƯA làm: <cái gì KHÔNG test đợt này — chống test lạc vào phần chưa build>

## 1. Spec nghiệm thu  [BẮT BUỘC — thiếu là test đoán bừa]
- Given/When/Then cho MỌI luồng + quy tắc ẩn (điều kiện hiện/ẩn, thời điểm, ưu tiên, dedupe…).
- Viết theo `templates/spec.md`; đính file: `tieu-chi-nghiem-thu-<feature>.md`.
- Đánh dấu ⚠️ điểm "chưa verify code" → test chạy ra khác = NGHI BUG, không sửa spec ôm code.

## 2. Quyền (RBAC)  [BẮT BUỘC]
- Vai nào được làm gì; chặn ở FE hay BE; vai bị từ chối (để test ca chặn-quyền).

## 3. Môi trường + tài khoản  [BẮT BUỘC]
- URL + xác nhận ĐANG CHẠY (branch/deploy nào có code — local? sandbox nào?).
- Tài khoản đủ vai: admin + non-admin (test chặn quyền) + tài khoản tích hợp (vd GHTK kết nối).

## 4. Data / seed  [BẮT BUỘC]
- Data tối thiểu để feature chạy. Ai seed: hạ tầng khó dựng → DEV seed; còn lại test tự tạo (tự xoá).

## 5. Vị trí UI + luồng đụng (ripple/regression)  [BẮT BUỘC]
- Đường vào: menu / route tới màn.
- Màn/luồng CŨ bị thay đổi behavior → test khoanh vùng kiểm hồi quy (đổi X có thể vỡ Y).

## 6. Hook ổn định lên control khó bấm  [NÊN — giảm flaky]
- UI không có id → gắn 1 class/`data-testid` lên đúng chỗ KHÓ (nút động, cell bảng…). Chỉ chỗ khó.

## 7. Mã data mẫu  [NÊN]
- Mã SP / id chi nhánh / mã đối tượng… để test tham chiếu (khỏi dò tay).
```

## Checklist dev trước khi giao
```
☐ Mục 0–5 (BẮT BUỘC) điền đủ; phần chưa làm đã ghi rõ ở "NGOÀI phạm vi"
☐ Spec Given/When/Then đính kèm + ⚠️ điểm chưa verify
☐ Env xác nhận đang chạy + tài khoản đủ vai (gồm non-admin + tài khoản tích hợp)
☐ Nói rõ ai seed data nào
☐ Luồng cũ bị đụng đã liệt kê (ripple) để test khoanh regression
☐ (nên) hook chỗ khó bấm + mã data mẫu
```

## Câu gọn gửi dev (nhắc nhanh)
> "Đưa tớ: (1) spec Given/When/Then + điểm ⚠️, (2) quyền, (3) env + tài khoản đủ vai,
> (4) data seed, (5) đường vào + luồng đụng; được nữa thì (6) gắn 1 class lên nút khó bấm
> + (7) cho mã data mẫu. Và ghi rõ cái gì NGOÀI phạm vi."

## Cách giao
- **Mỗi feature 1 phiếu**, đính spec, giao NGAY khi code xong (đừng để nguội).
- Spec sống ở `memory/local`; sau khi feature ship + product chốt → graduate vào `features/` (xem "Vòng đời spec" trong `templates/spec.md`).
