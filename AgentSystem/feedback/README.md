# feedback/ — Vòng lặp nuôi kho từ tín hiệu ngoài

> Kho dày lên nhờ **dữ liệu thật từ ngoài**: bug khách báo, ticket CSKH, sales, marketing, log trace. Đây là đường đi của tín hiệu → thành quy tắc trong kho. Khác `agents/duc-ket-agent.md` (rút kinh nghiệm sau 1 việc) — đây ở tầng cao, định kỳ, gom nhiều nguồn.

## Đường đi (5 bước)
1. **Gom tín hiệu** → dồn vào [inbox.md](inbox.md) (hoặc 1 Google Sheet) — bug/ticket/sales/marketing/log thô.
2. **AI tìm pattern** — lỗi nào lặp nhiều? phàn nàn nào trùng? Gom thành *vấn đề*, không liệt kê từng cái.
3. **AI đề xuất** — mỗi pattern → thêm quy tắc/checklist/recipe/known-issue nào.
4. **Người duyệt** — chốt cái nào vào kho (BẮT BUỘC — chặn rác).
5. **Ghi vào kho** — cập nhật đích tương ứng (bảng dưới).

> ↻ Kho dày hơn → code & test tốt hơn → ít bug/phàn nàn hơn → vòng sau nhẹ đi.

## Tín hiệu nào đi đâu
| Nguồn | Pattern | Đích |
|---|---|---|
| Bug kỹ thuật lặp | 1 loại lỗi mã lặp | `qa-code.md` / `rules/` / `registry` known-issue |
| Bug nghiệp vụ (tính sai...) | 1 trường hợp hay sai | `qa-spec.md` + `features/<tn>/` |
| Phàn nàn khách | nhiều khách kêu cùng tính năng | `features/<tn>/summarize.md` (Cần chú ý) + `faq.md` |
| Sales | tính năng hay đòi · lý do chốt hụt | `features/<tn>/summarize.md` + `qa-spec.md` |
| Marketing/hỗ trợ | cách khách gọi tên · câu hỏi lặp | `domain.md` + `features/<tn>/faq.md` |
| Log trace | case edge hay lọt | `qa-spec.md` + checklist trace |
| Quy trình tay lặp nhiều | việc đáng đóng gói | `skills/` recipe mới |

## 2 chốt KHÔNG được bỏ
1. **Người duyệt trước khi ghi** — AI đề xuất, người chốt.
2. **Gom thành pattern, không nhồi từng cái** — 1 bug ≠ 1 dòng kho; chỉ cái lặp/nghiêm trọng mới thành quy tắc.

## 2 giai đoạn
- **GĐ1 (làm ngay, bán tự động):** định kỳ gom vào inbox/Sheet → gọi AI phân tích → người duyệt → ghi.
- **GĐ2 (tự động, cần CTO):** nối API để dữ liệu tự chảy về, AI phân tích định kỳ; vẫn giữ chốt người duyệt.
