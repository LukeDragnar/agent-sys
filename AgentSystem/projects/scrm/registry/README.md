# SCRM Registry — Knowledge Graph hệ thống (backbone)

> **746 node** (mỗi node = 1 controller) phủ ~toàn bộ SCRM. Đây là **bản đồ máy-đọc-được "hệ thống làm được gì"** — nền cho chatbot, triage, onboarding, phân tích ảnh hưởng, AI agent. Trích từ **code thật** (BE controller/service + FE) qua quét tự động (2026-06-20).

## Mỗi node có gì (xem [_SCHEMA.md](_SCHEMA.md))
- **Frontmatter (máy đọc):** controller · module · service · `man_hinh_ui` · `auth` · `state_machine` · `depends` · `cross_module_in/out` (đồ thị có hướng) · `entities` · `stored_procs` · `deprecated`.
- **Bảng Hành động:** mỗi hành động/nút = endpoint + quyền + **hành vi đúng (expected)** + nguồn (EF/SP) + **known-issue**.
- **Endpoint nội bộ · Quy tắc nghiệp vụ · Quan hệ · Luồng xuyên module · Lỗi đã biết (path:line).**

## Dùng để làm gì
| Ai | Tra gì trong registry |
|---|---|
| **Chatbot / Q&A** | định vị tính năng (alias→node), "hành vi đúng" để trả lời |
| **Triage ticket** | so mô tả ticket với `hành vi đúng` + `known-issue` → lỗi hay do dùng sai |
| **Dev** | endpoint/quyền/entity/luồng code + **lỗi đã biết + lỗ hổng bảo mật** (path:line) |
| **Phân tích ảnh hưởng** | `cross_module_in/out` = đồ thị → đổi X gãy đâu |
| **Onboarding/Sản phẩm** | bản đồ đầy đủ năng lực hệ thống |

## Cách tra (cho agent)
1. Biết module → đọc `registry/<module>/` (vd `kho/`, `ke-toan/`).
2. Biết controller/nghiệp vụ → đọc đúng `<Controller>.md`.
3. Cần lần ripple → đi theo `cross_module_out` sang node khác.
4. **KHÔNG đọc cả folder** — chỉ node liên quan (giữ context nhẹ).

## Module (746 node)
he-thong 156 · marketing 151 · kho 94 · ke-toan 59 · sales 54 · cua-hang 50 · website 46 · nhan-su 36 · khach-hang 33 · bao-cao 27 · cong-viec 20 · mua-hang 8 · san-xuat 8

> Đặt tên file: `<Controller>.md`; trùng tên khác service → `<Controller>_<Service>.md`. Module theo nghiệp vụ user-facing (không theo service BE).

## Quan hệ với các tầng khác
- **registry/** = backbone (cái gì có, trace code) — Pha 1, ✅ xong.
- **[../features/](../features/)** = prose cho người/chatbot (cách dùng/FAQ/luồng) — Pha 2, đắp dần lên registry.
- **[../skills/](../skills/)** = recipe tác vụ tái sử dụng (phân trang, export...).
- **[../qa-spec.md](../qa-spec.md)/[../qa-code.md](../qa-code.md)** = checklist test/review.

## Trạng thái & bảo trì
- Tin cậy: trích code thật, **chưa adversarial-verify** từng node. "cần xác nhận" = AI chưa chắc.
- Khi code đổi nhiều → quét lại node tương ứng (controller → 1 node).
- Bug/lỗ hổng trong known-issue: **để session security riêng xử lý** (không sửa code từ đây).
