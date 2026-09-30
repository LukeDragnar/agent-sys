---
name: Tự trace case trước khi báo "xong"
description: Sau refactor lớn, phải tự trace các case edge để phát hiện bug, không chỉ dừng ở "code compile OK"
type: feedback
---

**Mọi thay đổi logic, dù 1 dòng, đều phải tự trace case thực tế bằng data giả lập** trước khi báo user "xong". Không dựa vào "code nhìn có vẻ đúng" hay "compile sạch". Fix nhỏ vẫn có thể chứa bug edge case — không có ngoại lệ "fix nhỏ không cần test".

**Why:** Session 2026-04-10 refactor luồng CHI_TIEU (10 files). Sau khi code xong và self-check bằng đọc lại code, tưởng đã OK. User yêu cầu "tự trace full case" → trace 13 case với data cụ thể → phát hiện **4 bug** ngay trong chính code vừa viết:
- Điều kiện "hoàn toàn bộ" Nợ/Trả sai (`SoTienTarget==0 && SoTienNo==0` thay vì chỉ `SoTienTarget==0`)
- Pending legacy `SoTienTarget=null` coalesce về 0 làm trừ chi tiêu về 0
- Thứ tự "ghi log trước Customer call sau" gây lost update khi retry
- Mongo audit log ghi sai field (`SoTienTichLuy` offline thay vì `SoTienTichLuyLogistic`)

Session trước đó (2026-03-27) không tự trace → deploy lên prod → 3 bug lớn lộ ra sau vài tuần (delta sai scope, điểm không tăng, throw đơn soft delete).

**How to apply:**

1. **Khi nào cần trace**: TẤT CẢ thay đổi logic — refactor, đổi signature, fix bug nhỏ, đổi 1 dòng if-else, thêm 1 field nullable. Quy mô trace tỉ lệ với scope thay đổi: fix 1 dòng có thể chỉ cần trace 2-3 case quanh điều kiện đó, refactor lớn cần >10 case.

2. **Cách trace**:
   - Tạo setup giả lập cụ thể: KH với state ban đầu, đơn với các field number cụ thể (285k, 150k...), config cụ thể (TienThanhDiem=1000)
   - Liệt kê các case edge: happy path, rollback, retry, soft delete, race, cap, legacy data, input null
   - Trace từng case **step by step**: input → mỗi bước nội bộ → state trung gian → output. Không skip bước.
   - So sánh với expected. Nếu lệch → bug.

3. **Save trace vào folder local**:
   - Tạo file `memory/local/trace/qa_<feature>_<date>.md` (folder `memory/local/trace/` đã gitignored — local only, không push)
   - Mỗi case ghi: setup, step, expected, actual, verdict ✅/❌
   - Bug tìm thấy → fix ngay trong cùng session, ghi lại vào trace để user thấy quá trình
   - Cuối file: list test plan T1, T2... để user chạy tay verify
   - **Không cần frontmatter `type:` vì không phải memory chính**, chỉ là log working
   - Có thể xóa các trace cũ không còn relevant để giữ folder gọn

4. **Đặc biệt chú ý các case**:
   - **Retry sau fail**: thứ tự đọc log / ghi log / gọi service ngoài có idempotent không?
   - **Data legacy**: field mới NULL với data cũ trong DB — coalesce về 0 có gây hại không?
   - **Edge value**: delta=0, target=0, batch rỗng, toàn pending legacy
   - **Scope bug**: 1 đơn vs tổng KH, per-đơn vs cross-đơn
   - **Race condition**: 2 instance song song dù chấp nhận không fix vẫn phải trace để biết hậu quả

5. **Không làm tắt**: "code nhìn giống pattern cũ nên chắc đúng" là cái bẫy. Phải trace bằng số thật.

6. **Báo user**: gửi trace report đầy đủ + bug phát hiện + fix đã apply. User có thể review trace mà không cần đọc code.
