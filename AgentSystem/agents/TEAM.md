# 🏢 SƠ ĐỒ TỔ CHỨC — Đội Agent (AgentSystem / qlcv)

> Bản đồ "ai làm gì" của cả đội agent. Mỗi lần thêm/đổi vai → cập nhật ở đây **và** ghi 1 dòng vào [CHANGELOG.md](CHANGELOG.md).
> Nguyên tắc vận hành: **USER chỉ huy · CLAUDE điều phối (funnel) · các agent chuyên trách thực thi**. Claude nhận report, trình USER **1 quyết định tại một thời điểm**.

```
                     ┌──────────────────────────────────────┐
                     │        USER (thiennq) — CHỈ HUY       │  quyết định cuối, ra chỉ thị
                     └───────────────────┬──────────────────┘
                                         │ chỉ thị / duyệt (gật–lắc)
                     ┌───────────────────▼──────────────────┐
                     │   CLAUDE — QUẢN LÝ (điều phối/funnel) │  spawn agent, gom report,
                     │   • điều phối việc  • giải vấn đề     │  trình 1 quyết định mỗi lúc
                     │   • CHỦ ĐỘNG đề xuất phát triển       │
                     └───┬─────────────┬─────────────┬──────┘
           ┌─────────────┘             │             └──────────────┐
   ┌───────▼────────┐        ┌─────────▼─────────┐        ┌─────────▼────────┐
   │   TỔ LAB R&D   │        │      TỔ QA        │        │   TỔ THỰC THI    │
   │ (nghiên cứu)   │        │  (gác chất lượng) │        │   (làm & giao)   │
   ├────────────────┤        ├───────────────────┤        ├──────────────────┤
   │ model-scout    │        │ review-agent      │        │ code-agent       │
   │  (radar ngành) │        │ trace-agent       │        │ git-agent        │
   │ lab-experiment │        │ unit-test-agent   │        │ duc-ket-agent    │
   │  (thử thật)    │        └───────────────────┘        │  (đúc kết tri    │
   │ lab-evaluator  │                                     │   thức)          │
   │  (chấm k.quan) │                                     │ deploy-agent     │
   │                │                                     │  (đẩy staging)   │
   └────────────────┘                                     └──────────────────┘
```

## Các tổ & vai

### 🔬 Tổ Lab R&D — "tìm cái hot + hợp, quyết áp dụng gì" (chống churn, ưu tiên tự-xây)
Dây chuyền: **Scout → Experiment → Evaluator → Decision Brief → USER quyết**
| Agent | Vai | File |
|---|---|---|
| **model-scout** | Radar ngành: quét model/tool/kỹ thuật hot → lọc **3 rổ** (🟢 Dùng ngay · 🟡 Học-tự-xây · ⚪ Theo dõi). Có chế độ B: bóc 1 link/video sếp quăng. | [model-scout-agent.md](model-scout-agent.md) |
| **lab-experiment** | Thử THẬT trên bài toán SCRM mẫu → ghi dữ liệu thô (chạy được/không, chất, giá, thời gian). Không kết luận. | [lab-experiment-agent.md](lab-experiment-agent.md) |
| **lab-evaluator** | Trọng tài: chấm khách quan/đối kháng → verdict + rổ, so baseline Opus 4.8. | [lab-evaluator-agent.md](lab-evaluator-agent.md) |

### 🛡️ Tổ QA — gác chất lượng mọi thay đổi code
| Agent | Vai | File |
|---|---|---|
| **review-agent** | Check convention + spec (đọc code đã sửa). | [review-agent.md](review-agent.md) |
| **trace-agent** | Mô phỏng data số chạy qua code, bám plan. | [trace-agent.md](trace-agent.md) |
| **unit-test-agent** | Viết/cập nhật UT bám tiêu chí nghiệm thu. | [unit-test-agent.md](unit-test-agent.md) |

### 🔧 Tổ Thực thi — làm việc & giữ tri thức
| Agent | Vai | File |
|---|---|---|
| **code-agent** | Executor: sửa code theo brief. | [code-agent.md](code-agent.md) |
| **git-agent** | Thao tác git an toàn (chỉ `_SourceForBuild`). | [git-agent.md](git-agent.md) |
| **duc-ket-agent** | Đúc kết bài học → nuôi kho tri thức. | [duc-ket-agent.md](duc-ket-agent.md) |
| **deploy-agent** | Đẩy **staging**: rà git → chốt FE/BE/service → pull+verify → build → push image. Cấm Production/Sprint/Devonline. | [deploy-agent.md](deploy-agent.md) |

## Nơi để artifact (USER xem ở đâu)
| Thứ | Vị trí |
|---|---|
| **Sơ đồ tổ chức** (bản này) | `agents/TEAM.md` |
| **Nhật ký phát triển/thay đổi đội** | `agents/CHANGELOG.md` ← xem cái này để biết có gì mới |
| Report radar model/tool | `memory/local/qlcv/model-scout/{ngày}.md` |
| Report thử nghiệm + verdict Lab | `memory/local/qlcv/eval-lab/{ứng-viên}-*.md` |
| Nhật ký công việc qlcv | `memory/local/qlcv/WORKLOG.md` |
