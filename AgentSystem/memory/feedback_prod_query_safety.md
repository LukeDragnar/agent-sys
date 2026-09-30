---
name: feedback-prod-query-safety
description: Query trên PRODUCTION bắt buộc TOP + WITH(NOLOCK) + WHERE IsDelete + IdDonVi, không thì chết hệ thống
metadata:
  type: feedback
---

Khi SELECT trên **production** (hoặc bất kỳ DB đang chạy tải thật), câu query BẮT BUỘC có đủ:

- `WITH (NOLOCK)` — mọi bảng (không khóa, không chặn giao dịch đang chạy).
- `TOP n` — luôn giới hạn số dòng trả về (đừng quét full bảng).
- `WHERE IsDelete = 0` — lọc bản ghi đã xóa mềm.
- `WHERE IdDonVi = '<...>'` — scope theo đơn vị (multi-tenant), tránh quét chéo toàn hệ thống.

**Why:** Query nặng / full-scan / thiếu NOLOCK trên prod có thể **lock + làm chết hệ thống** đang phục vụ khách. User nhấn mạnh: "nếu không chết hệ thống đấy".

**How to apply:** Trước khi đưa BẤT KỲ query prod nào cho user chạy (SQL Server hoặc Mongo), rà đủ 4 điều trên. Mongo: `.limit(n)` (= TOP), filter `IsDelete: false` + `IdDonVi: "<...>"`. Lấy `IdDonVi` từ bản ghi gốc trước (TOP 1), rồi mới scope các query sau.

Claude **không có quyền đụng prod** — chỉ đưa query để user tự chạy. Xem [[feedback-sql-base-column-ngaytao]] (NgayTao thay CreatedDate + NOLOCK).

---

**Bổ sung 15/07/2026 — điều kiện lọc phải "ăn" được index (sargable), và SCRIPT DO CLAUDE SINH RA cũng gây sự cố:**

- Sự cố thật: **script SELECT thống kê do Claude sinh** (khảo sát phạm vi backfill B176) đưa cho dev chạy trên product 63.x → **disk read spike 3000–6000** lúc 13:30–14:30 (giờ cao điểm) → **dev bị claim**. Query quét full `DonHangLogisticSanPham` (bảng chi tiết, cực to).
- **Đây là LỖI CỦA AGENT** — dev tin script Claude đưa và chạy. Đưa script full-scan cho user = **gây sự cố production, dù chỉ là SELECT**.
- 3 lỗi trong câu đó, tự kiểm TRƯỚC khi đưa:
  1. Lọc bằng **cột nghiệp vụ phụ không index** (`TMDTSanChietKhau > 0`) thay vì cột có index → full scan.
  2. **Bọc hàm lên cột** ở `WHERE`: `ISNULL(sp.TMDTSanChietKhau,0) > 0` → index chết. Cột index phải để **TRẦN** (`col > 0`, `NgayTao >= @a AND < @b`), cấm `ISNULL(...)`/`CONVERT(date,...)` trong `WHERE`/`JOIN ON`.
  3. Bắt đầu truy vấn từ **bảng chi tiết to nhất** rồi join ngược lên cha → phải lọc **bảng CHA trước** (ít dòng) rồi mới join xuống chi tiết.
- Chạy trên product: thêm `OPTION (MAXDOP 1)` + **chia nhỏ khoảng ngày** + tránh giờ cao điểm + ưu tiên replica.
- Rule kỹ thuật đầy đủ + ví dụ CTE: `rules/db.md` → "SELECT trên bảng LỚN — phải ăn được index". Đã là **hard-check bắt buộc** của `review-agent` (soi mọi `.sql`/LINQ + script đưa user). Xem [[feedback_no_db_index]].

---

**Bổ sung 24/07/2026 — `IsDelete` phải áp cho MỌI bảng, kể cả HEADER trong JOIN; và lọc `MaDon` trên bảng partition KHÔNG phải seek:**

- Lỗi thật: điều tra HĐ combo, viết detector JOIN `combo → DonHangLogistic h` mà **chỉ lọc `IsDelete=0` ở bảng combo, QUÊN ở header `h`** → có thể lọt đơn xoá mềm thành false-positive. **`IsDelete=0` phải có ở TỪNG bảng** (join thì để trong `ON` hoặc `WHERE` cho cả cha lẫn con), dễ quên khi join nhiều bảng.
- Lọc `WHERE MaDon IN (...)` trên `DonHangLogistic` (bảng PARTITION khổng lồ) **~10s** (không phải index seek) — 1 lần lấy `Id` rồi mọi query sau **seek theo `Id` (PK) < 1s**. **SELECT nghi > 5s phải BÁO trước, đừng chạy mù** (user: "select quá 5 giây phải báo không là chết").
- Xem lesson `projects/scrm/lessons/hoadon-combo-vat-double.md` + [[feedback_agent_khong_choc_db_prod]] (agent tuyệt đối không tự chạy query lên prod).
