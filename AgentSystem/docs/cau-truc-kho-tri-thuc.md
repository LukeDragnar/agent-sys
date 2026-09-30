# Cấu trúc Kho tri thức — SandBoxVN (chuẩn)

> Bản gốc trình bày đẹp: [cau-truc-kho-tri-thuc-v11.html](cau-truc-kho-tri-thuc-v11.html) (mở bằng trình duyệt để xem/chia sẻ).
> File này là bản markdown để **agent/AI đọc và áp dụng**. Đây là tài liệu khuôn mẫu để dựng + duy trì kho tri thức dùng chung cho cả đội.

---

## Phần 1 — Tư duy nền: kho là bản đồ, không phải kho chứa

Kho tri thức = **tấm bản đồ chỉ đường**, không chứa toàn bộ chi tiết hệ thống (chi tiết nằm trong mã). Mỗi file phải trả lời 3 câu:
- **Nó là gì?** — một mục đích rõ (mô tả / quy tắc / trí nhớ), không trộn lẫn.
- **Khi nào AI nạp nó?** — AI không đọc cả kho mỗi lần; mỗi file gắn 1 loại việc, chỉ nạp khi việc đó tới.
- **Ai giữ + khi nào cập nhật?** — mỗi file 1 chủ, 1 dịp cập nhật. Không ai giữ = file chết.

**5 nguyên tắc nền:**
1. Phân tầng theo phạm vi: toàn công ty → mảng → phân hệ.
2. Tách 3 loại nội dung: **mô tả · quy tắc · trí nhớ** — không trộn.
3. File nhỏ trỏ nhau qua README, **không gộp thành file khổng lồ**.
4. Mỗi file một chủ + một nhịp cập nhật.
5. Đặt chung trong mã nguồn để tự đồng bộ.

---

## Phần 2 — Sơ đồ cây

> `projects/` = các **mảng lớn của SandBox** (mỗi mảng 1 hệ thống con), không phải dự án rời. Toàn bộ cây đặt **chung trong mã nguồn**.

```
SandBox/
├── README.md            ← TẦNG 1 · MỤC LỤC (bản đồ): tìm tính năng → projects/<mảng>/features/
├── CONVENTIONS.md       ← quy ước đặt tên file, ngôn ngữ, cách thêm mới
│
├── rules/               ← TẦNG TOÀN CÔNG TY (áp dụng MỌI mảng)
│   ├── code.md          quy tắc viết mã chung
│   ├── git.md           quy tắc nhánh, commit, gộp mã
│   ├── build.md         quy tắc build / chạy
│   ├── db.md            quy tắc dữ liệu, đặt tên bảng
│   └── quy-trinh-theo-level.md   quy trình theo cấp + cờ leo thang
│
├── agents/              ← VAI TRÒ chung (AI làm việc gì) — dùng mọi mảng
│   ├── code-agent.md         chỉ đọc & sửa mã
│   ├── review-agent.md       rà mã theo checklist
│   ├── trace-agent.md        tự chạy thử case edge, báo lỗi
│   ├── git-agent.md          thao tác git, xác nhận trước
│   └── duc-ket-agent.md      đúc kết sau khi xong → đề xuất ghi vào kho
│
├── skills/              ← LỆNH TẮT chung (cách làm 1 việc nhanh) — mọi mảng
│   ├── phan-trang.md
│   └── dinh-dang-tien.md
│
├── projects/            ← TẦNG TỪNG MẢNG LỚN
│   ├── scrm/
│   │   ├── architecture.md   bản đồ & điểm nối nguy hiểm
│   │   ├── patterns.md       chuẩn viết mã của mảng
│   │   ├── qa-code.md        checklist rà mã
│   │   ├── qa-spec.md        checklist NGHIỆP VỤ (đề bài cho test)
│   │   ├── domain.md         thuật ngữ nghiệp vụ
│   │   ├── lessons.md        lỗi đã gặp của riêng mảng
│   │   ├── skills/           ← lệnh tắt RIÊNG mảng (vd multi-lang/)
│   │   └── features/         ← mỗi tính năng 1 thư mục
│   │       └── <tinh-nang>/
│   │           ├── summarize.md   (Tầng 2: là gì · để làm gì · cần chú ý · ảnh hưởng)
│   │           └── flow.md        (Tầng 3: tiền đk · luồng user · luồng admin · case · ví dụ số · hậu đk)
│   ├── ps_delivery/  ecommerce_connect/  delivery_connect/  delivery_receive/  delivery_status/
│
├── templates/           ← MẪU để thêm mới
│   ├── feature.md            khuôn 1 tính năng mới
│   └── agent.md              khuôn 1 vai mới
│
└── memory/              ← TRÍ NHỚ TÍCH LŨY (xuyên suốt mọi mảng)
    ├── decisions.md          quyết định chung đã chốt & lý do
    ├── feedback-rules.md     quy tắc rút ra từ phản hồi
    └── trace/                log các lần trace (tự sinh)
```

**Điều chỉnh quan trọng:** tách `skills/` (lệnh tắt) khỏi `agents/` (vai trò) — khác bản chất; thêm **đúc-kết-agent**; chuyển **multi-lang** về `projects/scrm/skills/` (lệnh tắt riêng SCRM, không phải vai chung).

> **Ghi chú khi áp dụng vào repo này:** repo `AgentSystem/` hiện giữ `memory/` theo kiểu **1-fact-1-file (70 file) + MEMORY.md index** — đúng nguyên tắc 3 ("file nhỏ trỏ nhau") hơn là gộp 2 file to. Khuyến nghị `decisions/` và `feedback-rules/` là **thư mục chứa file nhỏ**. Lớp `projects/scrm/skills/` (20 recipe đã làm) chính là lớp "skills riêng mảng" trong sơ đồ này — đã đặt đúng tên.

---

## Phần 2B — Cẩm nang tính năng (3 tầng, phóng to dần)

- **Tầng 1 · Mục lục — `README.md`:** bản đồ tìm tính năng. README phải index **đủ tính năng của mọi mảng** để support/sales/đào tạo tìm được dù nằm sâu.
- **Tầng 2 · Tóm tắt — `features/<tính-năng>/summarize.md`:** viết bằng ngôn ngữ nghiệp vụ (CSKH/sales đọc hiểu). Mục **"Cần chú ý"** (cạm bẫy + ảnh hưởng) quý nhất — nuôi từ vòng lặp bug.
- **Tầng 3 · Luồng — `features/<tính-năng>/flow.md`:** bước 1,2,3… phục vụ viết test/hướng dẫn/demo.

Cập nhật cẩm nang là **một phần của "XONG"**, không phải việc làm thêm khi rảnh.

**Khuôn `summarize.md`:** frontmatter [máy đọc: alias · trạng thái Core/Add-on/cần bật/cần cấu hình · gói · quyền · phụ thuộc module · link] → Là gì → Để làm gì/cho ai → Cần chú ý → Ảnh hưởng tới → Link xuống. *frontmatter là bản gốc, prose chỉ diễn giải.*

**Khuôn `flow.md`:** Tiền điều kiện → Luồng dùng (user) → Luồng bật/cấu hình (admin) → Các case/nhánh (rollback, retry, input rỗng, vượt cap, tồn=0, đổi trạng thái ngược) → Ví dụ cụ thể có số → Hậu điều kiện. *= use case + ví dụ số → đề bài test.*

**Ranh giới 2 tầng:** Tóm tắt = **cảnh báo** ("đừng quên tặng phẩm vẫn trừ tồn"); Luồng = **bước + case** ("B3 trừ tồn; case tồn=0 → chặn"). Cùng chủ đề, khác độ chi tiết — không chép qua lại.

---

## Phần 2C — Agent (vai trò) khác Skill (lệnh tắt)

- **AGENT = vai trò** ("ai làm"): một "người làm" dùng cho nhiều việc. VD code-agent, review-agent, trace-agent, đúc-kết-agent → đặt ở `agents/`.
- **SKILL = lệnh tắt** ("làm việc gì"): quy trình đóng gói cho MỘT loại việc lặp lại. VD xuất Excel, phân trang, đa ngôn ngữ, định dạng tiền → đặt ở `skills/` (chung) hoặc `projects/<mảng>/skills/` (riêng).
- **Phân biệt nhanh:** vai trò dùng nhiều việc → agent; cách làm 1 việc cụ thể lặp lại → skill. (`multi-lang` = cách chuyển đa ngôn ngữ → skill, chỉ dùng SCRM → `projects/scrm/skills/`.)

---

## Phần 2D — Bước đúc kết + Vòng lặp nuôi kho

### Đúc-kết-agent (sau MỖI việc)
Gọi "đúc kết lại đi" → AI nhìn lại việc vừa làm, tự hỏi 4 câu → đề xuất → người duyệt → ghi kho:

| AI tự hỏi | Nếu có → ghi vào đâu |
|---|---|
| Có quy tắc nào nên thành chuẩn? | `rules/` (nếu chung) hoặc `patterns.md` của mảng |
| Có lỗi nào vừa gặp, nên ghi để không tái phạm? | `projects/<mảng>/lessons.md` |
| Có quy trình nào lặp đủ nhiều, nên đóng thành lệnh tắt? | `skills/` (skill mới) |
| Có quyết định/quy ước nào nên lưu? | `memory/decisions.md` |

> Phải đóng thành "vai" gọi được + đưa vào quy trình, không để tùy hứng — nếu trông chờ ai nhớ thì sẽ quên, kho nghèo dần.

### Vòng lặp nuôi kho từ thực tế (tầng cao hơn — định kỳ)
Gom **dữ liệu thật nhiều nguồn** (bug, ticket khách, sales, marketing, log trace), tìm pattern lặp, bổ sung kho:

**5 bước:** ① Gom tín hiệu (về 1 nơi, vd Google Sheets) → ② AI tìm pattern (lỗi nào lặp, phàn nàn nào trùng) → ③ AI đề xuất (thêm quy tắc/checklist/skill) → ④ **Người duyệt** (chốt cái nào vào kho — chặn rác) → ⑤ Ghi vào kho.
> ↻ Kho dày hơn → code & test tốt hơn → ít bug/phàn nàn hơn → vòng sau nhẹ đi.

**Tín hiệu nào đi về đâu:**

| Nguồn tín hiệu | Pattern | Bổ sung vào |
|---|---|---|
| Tổng hợp bug kỹ thuật | Một loại lỗi mã lặp lại | `qa-code.md` / `rules/` |
| Bug nghiệp vụ (tính sai…) | Một trường hợp nghiệp vụ hay sai | `qa-spec.md` + `lessons.md` |
| Phản hồi/phàn nàn khách | Nhiều khách kêu cùng tính năng | `features/<tính-năng>/summarize.md` |
| Đội kinh doanh (sales) | Tính năng khách hay đòi · lý do chốt hụt | `summarize.md` + `qa-spec.md` |
| Marketing/hỗ trợ | Cách khách gọi tên · câu hỏi lặp | `domain.md` + tài liệu hướng dẫn |
| Log trace | Một dạng case edge hay lọt | checklist trace / `qa-spec.md` |
| Quy trình làm tay lặp nhiều | Một việc đáng đóng gói | skill mới trong `skills/` |

**2 giai đoạn:** (1) Làm ngay, bán tự động — gom vào Sheet, gọi AI phân tích, người duyệt rồi ghi. (2) Tự động (cần CTO) — nối Agent SDK/API, AI phân tích định kỳ, vẫn giữ chốt người duyệt.

**2 chốt KHÔNG được bỏ:** ① **Người duyệt trước khi ghi** (AI đề xuất, người chốt — tự ghi thẳng thì rác vào kho). ② **Gom thành pattern, không nhồi từng cái** (1 bug không thành 1 dòng kho; chỉ cái lặp/nghiêm trọng mới thành quy tắc).

> Cơ sở thực tế: phân tích **245 bug** → cô đọng **7 quy tắc**. ~85% là lỗi logic (test/trace bắt được), dồn ở khuyến mãi (>40 bug) và sửa–xóa phiếu kho/imei (>50 bug).

---

## Phần 3 — Phần test nằm ở đâu

**Nguyên tắc:** tài liệu chữ → ở kho; code (kể cả code test) → ở mã nguồn.

| Ở KHO (tài liệu chữ) | Ở MÃ NGUỒN (là code) |
|---|---|
| `qa-spec.md` — checklist nghiệp vụ = "đề bài" (vd mua 3 tặng 1 → trả 200k) | File test tự động — sống **cạnh tính năng trong mã**, tự chạy mỗi khi sửa code |
| `qa-code.md` — checklist rà mã | (KHÔNG để file test trong kho — kho không chạy được code) |
| `agents/trace-agent.md` — vai tự chạy thử case edge bằng số giả lập | |
| `memory/local/trace/` — log các lần trace | |

- **Trace** (làm ngay): AI đọc mã, nhẩm case edge theo qa-spec, báo lỗi. Checklist case edge: rollback, retry, race, soft delete, legacy null, input rỗng, cap, đổi trạng thái ngược.
- **Test máy** (nâng cấp sau, cần CTO): file test chạy code thật, tự chạy mỗi lần sửa. Dựng cho phần lõi trước.
- **Test tầng LOGIC trước** (dữ liệu vào → kiểm kết quả ra), không leo test giao diện (giòn, đổi nút là hỏng).

---

## Phần 4 — Bảng tra các tầng

| Thành phần | AI nạp khi nào | Ai giữ | Khi nào cập nhật |
|---|---|---|---|
| README | Đầu tiên, mọi việc | Lead kỹ thuật | Khi thêm vai/mảng mới |
| CONVENTIONS | Khi thêm/sửa file kho | Lead kỹ thuật | Hiếm |
| rules | Luôn nạp | Lead/CTO | Khi đổi chuẩn chung (hiếm) |
| agents | Khi gọi đúng vai | Lead kỹ thuật | Khi quy trình đổi/thêm vai |
| projects/<mảng> | Khi làm trong mảng đó | Lead của mảng | Khi kiến trúc/chuẩn mảng đổi |
| features/<tính-năng> | Khi cần hiểu/làm/test 1 tính năng | Người thạo tính năng | Mỗi khi tính năng đổi (phần của XONG) |
| qa-spec | Trước khi báo xong; gốc viết test | Người thạo phân hệ + tester | Khi có trường hợp nghiệp vụ mới |
| memory | Khi cần tra "lần trước xử thế nào" | Cả đội | **Liên tục** |

> **Lessons:** lỗi riêng 1 mảng → `projects/<mảng>/lessons.md`. Chỉ thứ xuyên suốt mọi mảng mới lên `memory/` gốc.

---

## Phần 5 — Lằn ranh: rules chung hay patterns riêng

**Dùng cho ≥2 mảng → `rules/`. Chỉ 1 mảng → `patterns.md` của mảng đó.**
- `rules/` (chung): "tên biến viết thường", "mọi thao tác xóa phải hỏi xác nhận", "tiền hiển thị có dấu phân cách".
- `patterns` riêng mảng: "SCRM xử lý hàng đợi theo cách này", "giao vận gọi dịch vụ ngoài theo mẫu kia".
- Quy tắc riêng mảng lặp sang mảng thứ 2 → nâng lên `rules/`. Đừng chép 1 quy tắc vào nhiều mảng.

### Kỷ luật đặt tên (CONVENTIONS.md)
- **Tên file kho:** tiếng Anh, chữ thường, gạch ngang (`qa-spec.md`, không `QA_Spec.md`).
- **Tên trường/biến:** chọn MỘT ngôn ngữ, nhất quán (cấm nửa Anh nửa Việt).
- **Không viết tắt tối nghĩa:** `tongTienKhuyenMai` > `ttkm`.
- **Nội dung tài liệu:** tiếng Việt, không viết tắt — người không kỹ thuật đọc cũng hiểu.

---

## Phần 6 — Nên & không nên

**✓ Nên:** bắt đầu nhỏ (1 README + vài quy tắc) · mỗi lỗi gặp → 1 dòng vào lessons · file nhỏ trỏ nhau qua README · đặt chung trong mã nguồn (tự đồng bộ).

**✗ Không nên:** gom hết vào 1 file khổng lồ · chép lại chi tiết đã có trong mã · để file test trong kho · để file không có người chủ.

---

## Phần 7 — Quy trình theo cấp độ & cờ leo thang

Cấp độ đặt **baseline** quy trình; quyết định cuối theo **rủi ro = độ khó + bán kính ảnh hưởng**.

| Cấp | Loại việc | Bắt buộc qua (cộng dồn) |
|---|---|---|
| **L1** rất dễ | đổi text/nhãn, ẩn/hiện, sắp xếp trường (FE, ~1–2h) | convention tự áp + tự soát · review nhẹ. Không cần trace/test. Cập nhật cẩm nang nếu đổi hành vi |
| **L2** dễ | lọc/tìm/export đơn giản, sửa nhỏ logic (~2–4h) | + review-agent đầy đủ + trace phần logic + vài case test |
| **L3** trung bình *(ngưỡng "qua đủ quy tắc")* | logic nghiệp vụ nhỏ, API mới, thêm field DB, phân quyền cơ bản (~1–2 ngày) | + dò ảnh hưởng (impact) + test logic từ qa-spec + cập nhật cẩm nang — **bắt buộc** |
| **L4** khó | nghiệp vụ phức tạp, nhiều màn/luồng, RBAC, đồng bộ nội bộ (~3–5 ngày) | + duyệt kế hoạch TRƯỚC khi code + checklist case biên + chia nhỏ task + lead duyệt ở cổng |
| **L5** rất khó | module mới, tích hợp bên thứ 3, đổi kiến trúc, realtime đa hệ thống (>1 tuần) | + spike trước + duyệt thiết kế với CTO + giao theo giai đoạn + test máy cho phần lõi |

### Cờ leo thang (gate theo rủi ro, không chỉ độ khó)
Task **chạm vào** một trong các vùng dưới → áp **tối thiểu L3** dù công sức chỉ L1–L2:
- Tiền / thanh toán / hóa đơn / kế toán
- Khuyến mãi · kho/tồn/imei · rollback đơn
- Filter multi-tenant (IdDonVi) — rủi ro rò dữ liệu chéo tenant
- Đổi schema / migration DB
- Tích hợp bên ngoài (sàn · ĐVVC · ZNS · MISA/Viettel)
- Phân quyền (RBAC)
> Đây đúng mấy vùng dồn bug trong phân tích 245 lỗi — "nhìn dễ mà gãy nặng".

**Chỗ để mọi thứ:** Level + cờ rủi ro → frontmatter/metadata task · Bậc thang quy trình → `rules/quy-trinh-theo-level.md` · Bán kính ảnh hưởng → ô "Ảnh hưởng tới" của cẩm nang. Level = cỡ S/M/L (L1–2=S · L3=M · L4–5=L) → nối vào WIP & cách giao việc.

---

## Quy tắc một câu để nhớ
> Kho tri thức = **tấm bản đồ**, không phải quyển bách khoa. Nó chỉ đường cho AI tới đúng chỗ trong mã, đặt luật chung, và lưu bài học. **Đề bài test ở kho, máy chấm (code test) ở mã.** File nhỏ, trỏ nhau, nằm chung mã, có người giữ, cập nhật liên tục.
