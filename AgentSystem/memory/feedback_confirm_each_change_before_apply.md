---
name: feedback_confirm_each_change_before_apply
description: Khi thay đổi đã dồn nhiều, phải confirm cụ thể từng hạng mục trước, user chỉ định làm cái nào mới làm cái đó
metadata:
  type: feedback
---

Khi 1 task đã phát sinh NHIỀU thay đổi (nhiều file/nhiều repo/nhiều vòng), KHÔNG tiếp tục tự ý sửa thêm. Phải:
1. Dừng lại, liệt kê cụ thể từng hạng mục thay đổi (đã làm / đang treo / đề xuất).
2. Chờ user duyệt và chỉ định rõ "làm cái nào".
3. Chỉ làm đúng cái user chỉ định, không gộp/mở rộng.

**Why:** Session đơn hoàn SCRM thay đổi dồn quá nhiều (refactor unified logistics + bộ trạng thái canonical 7 mức + rename TrangThaiHoanTMDT→MaDoiTac/TenDoiTac + map reverse logistics) khiến user khó kiểm soát. User nói: "giờ đang thay đổi nhiều lắm rồi nên phải confirm cụ thể đã rồi tôi bảo làm đâu thì làm".

**How to apply:** Liên quan [[feedback_ask_spec_before_code]] và [[feedback_pre_commit_workflow]]. Khi đề xuất gap/fix, trình bày dạng danh sách để user tick chọn, KHÔNG tự thực thi. Đặc biệt khi spec official và data thật mâu thuẫn → confirm với user, không tự quyết defensive.

---

**Bổ sung 15/07/2026 — 2 lỗi tái phạm cùng gốc "làm trước khi user chốt":**

1. **Hành động KHÓ ĐẢO NGƯỢC phải chờ user gật XONG rồi mới chạy — không vừa-nghe-vừa-làm.**
   - Sự cố: user nhờ "dọn Group4". Trong lúc user đang gõ *"ơ xóa nhánh local làm gì / cứ để đó"* thì Claude **đã `git branch -D` xong 7 nhánh local** `scrm`. May còn SHA trong scrollback nên khôi phục được — nhưng đó là **hên, không phải quy trình**.
   - **Luật:** lệnh xóa/reset/force/ghi-đè hàng loạt → **liệt kê đích danh cái sẽ xóa → chờ 1 câu "xóa đi" rõ ràng → mới chạy**. "Dọn sạch đi" là lệnh chung, KHÔNG tính là đã duyệt từng nhánh cụ thể. (Chính auto-mode classifier cũng đã chặn 1 lần vì lý do này — đừng tìm cách lách.)

2. **Đừng "vẽ thêm việc" khi user chỉ nhờ 1 việc gọn — quản-lý/kế-hoạch cũng là scope.**
   - Sự cố: user chốt *tạo 2 task ticket*, Claude cứ chực **gom/tách/xếp lịch/đề xuất thêm task** tới mức user phải nói *"tôi hơi rối rồi"* và *"đừng xếp vội"*. Trước đó cũng bị *"mấy cái tồn tại rồi thì phải giữ lấy chứ sao lại gom"*.
   - **Luật:** làm **đúng cái được nhờ**, rồi **DỪNG để user dẫn**. Task/nhánh/việc **đã tồn tại → GIỮ, không nuốt vào cái mới**. Đề xuất mở rộng tối đa **1 dòng ngắn**, không bày cả menu xếp lịch khi user chưa hỏi.

Liên quan [[feedback_flag_gia_dinh_ngam]] (calibrate — đừng overcorrect thành menu dài) và [[feedback_bao_su_that_doc_code_minh_truoc]] (báo đúng sự thật, đừng đổ lỗi hệ thống).
