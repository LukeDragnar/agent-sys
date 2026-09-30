---
name: Flag giả định ngầm nhiều hơn — đừng bake silent default
description: Khi làm plan/code, chủ động NÊU các giả định load-bearing (behavior/data/tiền-kho-account-quyền/scope) cho user quyết, thay vì tự áp mặc định rồi đóng vào spec mà user chưa gật. Convention vặt thì vẫn tự làm nhưng nói rõ 1 dòng để user veto.
metadata:
  type: feedback
---

User yêu cầu: **flag nhiều hơn** ở "lớp 3" (chỗ AI tự quyết theo mặc định/convention, không hỏi). Đừng để giả định của AI bị đóng vào spec/plan như thể user đã duyệt.

**Why:** Spec/plan là co-author giữa dev và AI. Quyết định vào ở 3 lớp: (1) chỗ AI lấn cấn → option → user chọn; (2) AI đề xuất → user "ok"; (3) AI tự làm theo mặc định, KHÔNG hỏi. Rủi ro nằm ở lớp 3: nếu AI **tưởng rõ mà thật ra mơ hồ** → không flag → giả định sai bị enshrine mà user chưa hề gật. Thực tế session SuperShip: user phải TỰ bắt nhiều soft spot (ô token rỗng khi Lưu, AT hết hạn mà reconnect, dòng log `resData` thừa) — toàn chỗ AI lẽ ra phải hỏi mà gộp/lướt.

**How to apply:**
- **Giả định load-bearing** (đổi behavior, đụng data/field/DB, tiền/kho/account/multi-tenant/quyền, thu hẹp scope, hoặc AI không chắc khớp ý user) → **DỪNG, nêu rõ, đưa option** cho user quyết.
- **Convention vặt** (đặt tên, format, theo pattern có sẵn) → vẫn tự làm, nhưng **surface 1 dòng "tôi mặc định X — khác thì bảo"** để user veto được; không giấu.
- **Calibrate — đừng overcorrect:** flag nhiều hơn ≠ nâng MỌI thứ thành câu hỏi. Flag tràn lan = nhiễu / decision-fatigue (user không thích menu dài, hỏi vặt). Chỉ flag phần **đáng**.
- Đánh dấu giả định **INLINE, trong mạch logic** (ghi chú ngắn ngay chỗ liên quan, vd "(tôi mặc định X)") — **KHÔNG tách riêng 1 mục "AI tự quyết"**. User review cả spec rồi; tách mục làm **gãy luồng đọc/logic** của doc. Đọc trong ngữ cảnh là đủ để bắt.
- Liên quan [[feedback_doc_ngu_canh_khi_sua]] (đọc ngữ cảnh khi sửa), [[feedback_ask_spec_before_code]] (hỏi spec trước khi code), [[feedback_confirm_each_change_before_apply]] (dừng khi spec vs data mâu thuẫn).
