# 🤖 Đội Agent — Điểm vào cho cả team

> Đọc file này đầu tiên để hiểu **đội agent** của hệ thống làm gì, tổ chức ra sao, và tìm mọi thứ ở đâu.
> Mục tiêu: cả team **nắm chung một mô hình**, không phụ thuộc ghi chú riêng của một người.

## Đội này là gì?

Một tập hợp **agent chuyên trách** để tự động hóa công việc dev + quản lý cho SCRM. Vận hành theo mô hình:

**USER chỉ huy → CLAUDE điều phối (funnel) → agent chuyên trách thực thi.**

Claude nhận report từ các agent, gom lại, trình USER **1 quyết định tại một thời điểm** (không dội việc thô lên user).

## Đọc theo thứ tự

1. **[TEAM.md](TEAM.md)** — 🏢 Sơ đồ tổ chức: các tổ (Lab R&D · QA · Thực thi), ai làm gì, artifact để đâu.
2. **[CHANGELOG.md](CHANGELOG.md)** — 📓 Nhật ký phát triển: mỗi lần thêm/đổi/bỏ agent hay quy trình → ghi 1 dòng. **Xem đây để biết đội có gì mới.**
3. **File từng agent** (`*-agent.md`) — spec chi tiết vai + rule + output của mỗi agent.

## Quy ước "team đọc chung vs cá nhân" (QUAN TRỌNG)

| Loại | Nơi để | Commit? |
|---|---|---|
| **Khung đội** (sơ đồ, agent spec, quy trình) | `agents/` | ✅ Commit — team đọc chung |
| **Nhật ký phát triển đội** | `agents/CHANGELOG.md` | ✅ Commit |
| **Quyết định áp dụng** (Lab chốt dùng/không dùng gì) | `agents/lab-decisions/` | ✅ Commit — team thấy đã quyết gì & vì sao |
| **Dữ liệu việc cá nhân** (worklog, task, report thô) | `memory/local/...` | ❌ Gitignore — riêng từng dev |

→ **Nguyên tắc:** cái gì cả team cần nắm thì **commit + viết rõ ràng cho người khác đọc**; scratch/việc-riêng để `memory/local/`. Khi Lab ra 1 kết luận đáng để team biết → **chắt lọc thành 1 Decision Brief** bỏ vào `agents/lab-decisions/`, không để chìm trong local.

## 🧪 Lab R&D hoạt động thế nào (TRIẾT LÝ CỐT LÕI)

> Đây là linh hồn của đội. Đọc kỹ.

**Không "bê đồ mới trên git về dùng" theo hype** — cái mới ra liên tục, chạy theo là hệ thống đổi xoành xoạch, không bao giờ ổn định.

**Mặc định = HẤP THỤ → TỰ XÂY:** tham khảo nhiều nguồn hay → rút tinh hoa/kỹ thuật/source → **gộp thành 1 cái CỦA MÌNH**, mình làm chủ, ít lệ thuộc. (10 repo đều hay → gộp tinh hoa thành 1 hệ của riêng mình, không phải "dùng cái nào".)

**🟢 dùng nguyên bản chỉ là NGOẠI LỆ** — khi thứ đó không đáng công tự xây (vd model nền: không tự train được → chọn cái mạnh nhất, ít đổi).

### Quy trình (có CỔNG CÔ LẬP — không cái gì vào hệ chính mà chưa qua Lab)

```
Scout        → tham khảo nhiều nguồn, lọc cái đáng hấp thụ (3 rổ: 🟢 dùng · 🟡 hấp thụ-tự xây · ⚪ theo dõi)
   ↓
Experiment (SANDBOX cô lập — bản SAO, TUYỆT ĐỐI không đụng hệ chính):
   (a) thử bản thân cái mới — chạy được/không, hay ở đâu
   (b) GHÉP thử vào bản sao "cái cũ" → chạy CẢ CỤM → xuất rõ: ĐỔI gì · THÊM gì · BỎ/thay gì · cũ có gãy không
   ↓
Evaluator    → chấm CẢ CỤM ĐÃ GHÉP (không chỉ cái mới): tốt + an toàn + cái cũ còn nguyên?
   ↓
🚧 CỔNG DUYỆT — USER xem "diff đổi/thêm/bỏ" rồi gật
   ↓
Promote vào hệ chính — CÓ KIỂM SOÁT: chỉ đẩy phần diff đã validated · qua QA (review→trace) · commit RIÊNG revert được
```

**Luật cứng:**
- Chưa qua Lab (gồm **thử-ghép-với-cũ**) + chưa USER duyệt → **CẤM đụng hệ chính**. Vì hệ chính hỏng thì revert rất khó → Lab chắn ở giữa, toang thì toang trong sandbox.
- Mọi lần ghép phải kèm **báo cáo "đổi / thêm / bỏ"** để USER thấy rõ trước khi gật.
- Hệ chính luôn ở trạng thái sạch, **revertable**.

## Nguyên tắc vận hành (tinh thần đội)

- **Chủ động phát triển:** Claude tự đề xuất cải tiến, không đợi bị sai — nhưng hành động có tác động thì xin chỉ thị USER trước.
- **Trung thực + bằng chứng:** mọi kết luận kèm nguồn; chạy thật > nghe nói; số không kiểm được → ghi "chưa rõ".
