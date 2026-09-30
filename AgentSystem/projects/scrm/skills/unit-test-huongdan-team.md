# Kế hoạch hướng dẫn team dùng Unit Test (playbook)

## Mục tiêu
Team **chạy + viết được UT**, dần dần code mới kèm test — **không sợ, không kháng cự, không thành thủ tục vô hồn**.

## 5 nguyên tắc để KHÔNG thất bại
1. **Cho thấy GIÁ TRỊ trước** (đã tìm ra bug thật) → tạo buy-in, đừng bắt đầu bằng "quy định phải test".
2. **Bắt đầu NHẸ** — chạy trước, viết sau; không ép perfect.
3. **Làm thử là chính**, lý thuyết tối thiểu.
4. **KHÔNG gate CI vội** — test chưa verify mà chặn merge = chặn nhầm người fix bug → team ghét.
5. **KHÔNG bắt viết tay test cho code cũ hàng loạt** — đã có 517 test sinh sẵn làm lưới.

---

## LỘ TRÌNH theo mốc

### 🟢 Mốc 0 — Buổi họp giới thiệu (hôm nay)
- Trình báo cáo: **517 test / 49 module + đã lòi bug thật** (NRE Deposit, 2 bug hệ thống).
- Thông điệp: *UT = lưới an toàn + bắt bug sớm, KHÔNG phải thủ tục.*
- Demo 2 phút: click `run-tests.bat` → xanh; (tùy) sửa 1 chỗ → đỏ → revert.
- **Chốt:** team đồng ý thử → sang Tuần 1.

### 🟢 Tuần 1 — Team ĐỌC + CHẠY (chưa viết)
- Mỗi dev: pull AgentSystem → đọc `rules/unit-test-gioi-thieu.md` (~30 phút) → chạy `run-tests.bat`.
- Mở 1 file test đọc (vd `DmMucDoUuTienServiceTests.cs`) — thấy comment Cho–Khi–Thì.
- **Đích:** ai cũng chạy được test + hiểu "UT là gì, vì sao".

### 🟢 Tuần 2 — Mỗi dev VIẾT test ĐẦU TIÊN (có kèm)
- Mỗi dev chọn **1 method đơn giản** trong module mình → viết **1 test** theo khuôn (`rules/unit-test.md` mục 4).
- Bạn (hoặc senior) **review test đầu** của từng người.
- **Đích:** ai cũng viết được 1 test xanh + đúng khuôn (comment Việt, Cho–Khi–Thì).

### 🟡 Tuần 3+ — Đưa vào QUY TRÌNH (mềm)
- Quy tắc mềm: **code mới / fix bug → kèm test** (khuyến khích, chưa bắt buộc cứng).
- Viết **Tiêu chí nghiệm thu (Cho–Khi–Thì)** trước khi code (feature mới).
- Review MR: **khuyến khích có test** (chưa chặn).

### 🟡 Song song — Xử lý bug đã tìm ra
- Chia backlog nghi-bug (`ut-nghibug-backlog.md`) cho **owner từng module** → review + fix + **convert test → verified**.
- Ưu tiên: 🔴 NRE Deposit → 2 bug hệ thống (GetByIdAsync, InsertAsync).

### 🔴 Sau (khi test đã VERIFIED) — CI
- Test đã verify (assert hành vi ĐÚNG, không phải "hiện tại") → mới cắm `dotnet test` vào pipeline → **gate merge**.

---

## Phân vai
| Ai | Việc |
|---|---|
| **Bạn (lead UT)** | Kèm, review test đầu của dev, quản backlog bug, quyết khi nào bật CI |
| **Mỗi dev** | Owner test module mình; code mới kèm test |
| **Senior/reviewer** | Review MR có test đúng khuôn |

## Đo lường (nhẹ, đừng KPI hoá)
- % module có test → xem `test-log/ut-coverage.md`.
- Code mới có kèm test không (review MR).

## Vật liệu sẵn có
- **Giải thích:** `rules/unit-test-gioi-thieu.md`
- **Hướng dẫn viết:** `rules/unit-test.md` + `projects/scrm/skills/unit-test.md`
- **Chạy:** `scrm/run-tests.bat` · VS Code 🧪 (mở `Tests.sln`)
- **Ví dụ:** 47 test project mẫu trong scrm (đọc để copy khuôn)

## ⚠️ Cạm bẫy phải tránh
- Đừng ép **CI gate** khi test còn characterization → chặn nhầm.
- Đừng bắt team **viết tay test code cũ hàng loạt** → dùng bộ đã sinh.
- Đừng để test thành **thủ tục vô hồn** → expected PHẢI từ tiêu chí nghiệp vụ, KHÔNG chép code.
- Đừng dạy **quá nhiều lý thuyết** → 30 phút đọc + làm thử là đủ.
