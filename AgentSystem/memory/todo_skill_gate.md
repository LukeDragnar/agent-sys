---
name: "TODO: Skill-Gate — module gác cổng bảo mật skill/MCP (tự xây từ SkillSpector)"
description: Xây module tools/skill-gate/ tự quét skill markdown + agent md + config MCP để bắt prompt-injection / data-exfil / tool-poisoning TRƯỚC KHI cài. Hấp thụ kỹ thuật từ NVIDIA SkillSpector (Apache-2.0), KHÔNG cài-để-phụ-thuộc. Chưa code — mới xong Scout (đọc source, rút kỹ thuật).
metadata:
  type: project
---

**TODO — Skill-Gate (case Lab #1, rổ 🟡 hấp thụ→tự xây).** Trạng thái: **đã rút kỹ thuật, CHƯA dựng code**. Bước 2 (dựng thử trong sandbox) chờ USER cho chạy.

## Mục tiêu
Module `tools/skill-gate/` (script Python) quét `skills/` + `agents/` + config MCP của AgentSystem → chạy checklist → in **cảnh báo + điểm rủi ro chặn cài/merge**. **KHÔNG BAO GIỜ execute file bị quét.** Để gác cổng khi cài skill/MCP lạ từ GitHub (chống lén đọc/tuồn code khách).

## Kỹ thuật ĐÁNG BÊ (rút từ SkillSpector)
- **Regex** bắt prompt-injection / data-exfil / tool-poisoning — mạnh nhất là **chữ vô hình**: zero-width, homoglyph, RTL, HTML-comment, base64 giấu trong `.md`.
- **MCP least-privilege**: soi quyền wildcard / quyền không khai báo.
- **AST** bắt call nguy hiểm cho file `.py` đính kèm.
- **OSV.dev** tra CVE (nhẹ).
- **Công thức tính điểm + band "khuyến nghị trước khi cài"**.

## BỎ (thừa với quy mô mình)
LangGraph · đa-provider LLM · MCP-server-hoá/Docker/SARIF · taint-tracking đầy đủ + YARA nhị phân (để sau, kho ta chủ yếu markdown) · LLM semantic (chỉ bật tay khi 1 skill đã bị flag — tránh tốn token + lộ nội dung).

## Tái dùng source (Apache-2.0 — giữ LICENSE/NOTICE)
CHÉP/PORT gần thẳng: bộ regex + YARA rule + OSV client + công thức điểm + unicode-normalizer. **Tự viết lại** khung chạy tối giản (bỏ LangGraph) + AST gọn.

## Quy trình BẮT BUỘC (theo Lab, xem `agents/README.md`)
1. Dựng thử trong **sandbox cô lập** → quét thử `skills/`+`agents/` → xem bắt được gì.
2. **Thử ghép với cũ**: gắn vào luồng "trước khi cài skill lạ" → báo DIFF (đổi/thêm/bỏ).
3. `lab-evaluator` chấm → **USER duyệt** → mới promote (commit riêng, revertable).

## ⚠️ Caveat cần verify khi port
Source từng analyzer + DEVELOPMENT.md của SkillSpector **bị GitHub 429 → mới đọc qua snippet, chưa soi file gốc từng dòng**. Lúc port thật phải gỡ 429 đọc kỹ.

## Refs
- Rút kỹ thuật: `memory/local/qlcv/eval-lab/skillspector-rut-ky-thuat-2026-07-09.md`
- Bóc nguồn gốc (case gốc): `memory/local/qlcv/eval-lab/tiktok-hyperainews-7651654194000137492-2026-07-09.md`
- Khung Lab: [[../agents/README.md]] · CHANGELOG: [[../agents/CHANGELOG.md]]
