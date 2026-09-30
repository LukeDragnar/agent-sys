---
name: feedback_bao_su_that_doc_code_minh_truoc
description: Khi 1 lệnh/tool/thao tác thất bại, ĐỌC KỸ code/param của chính mình TRƯỚC khi kết luận "hệ thống chặn / API không hỗ trợ" — đừng đổ lỗi ra ngoài. Báo cáo đúng sự thật, nhận lỗi của mình.
metadata:
  type: feedback
---

Khi một lệnh, tool, hay API call **thất bại / không ra kết quả mong đợi**: đọc lại **code + tham số của chính mình** trước, đừng vội kết luận "hệ thống chặn", "API không cho", "tool không hỗ trợ".

**Why:** Sự cố 14/07/2026 — Claude tạo task qlcv nhưng mô tả rỗng, rồi kết luận **"API không cho ghi mô tả"** và đề xuất user paste tay / sửa permission. Thực tế hàm `create` trong `sb_work.py` **đã có sẵn tham số `mota`**, chỉ là phần dispatch CLI **không truyền xuống** — lỗi của chính mình. User phải nói *"cái mô tả phải có khi tạo mới chứ"* mới nhìn lại. Đổ lỗi ra ngoài = **chẩn đoán sai gốc** + làm user mất niềm tin (tưởng công cụ hỏng trong khi mình chưa đọc kỹ).

**How to apply:**
- Lệnh fail → **đọc source/def của tool + tham số mình truyền** trước. Hỏi: "mình đã truyền đúng chưa?", "hàm có nhận cái này không?" — TRƯỚC khi đổ cho hệ thống.
- Chỉ kết luận "hệ thống/API chặn" khi **đã kiểm chứng** (đọc code tool, thử tối thiểu, xem message thật). Nói "có vẻ bị chặn" khi chưa chắc, đừng khẳng định.
- Phân biệt 2 lớp chặn khác nhau: (a) **whitelist/logic của chính worker mình viết** (sửa được ngay) vs (b) **permission classifier của harness** (thật sự ngoài tầm). Đừng gộp "worker tự từ chối" thành "hệ thống chặn".
- **Báo cáo đúng sự thật, nhận lỗi mình sớm** — khi user hỏi "sao lại thế" thì trả lời thật ngay từ đầu, đừng để bị vặn mới khai. Overclaim / đổ lỗi sai = lỗi nặng hơn bản thân sự cố.
- Liên quan [[feedback_khong_ket_luan_khong_co_tu_grep_hut]] (đừng kết luận "không có" từ 1 lần grep hụt), [[feedback_confirm_each_change_before_apply]] (làm trước khi user chốt).
