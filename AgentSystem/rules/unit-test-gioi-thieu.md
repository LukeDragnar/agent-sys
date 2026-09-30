# Unit Test — Giới thiệu khái niệm (đọc TRƯỚC khi làm)

> Dành cho người **chưa biết / mới biết** UT. Đọc 1 lần để hiểu "là gì / vì sao".
> Muốn viết test thật → sau đó đọc `rules/unit-test.md` (hướng dẫn) + `projects/<proj>/skills/unit-test.md` (đặc thù project).

---

## 1. Unit test là gì — và KHÔNG là gì
- **Là:** kiểm **"code ra QUYẾT ĐỊNH đúng chưa"** cho 1 đơn vị nhỏ (thường 1 method), chạy **trong bộ nhớ**.
- **KHÔNG:** không đụng DB/Redis/mạng. Phụ thuộc ra-ngoài được thay bằng **đồ giả (mock)**.
- Vì cô lập → chạy **mili-giây**, chạy lại hàng nghìn lần mỗi build.

## 2. Nó nằm ở đâu — Tháp kiểm thử (Testing Pyramid)
```
      ▲  E2E / UI     ← ít, chậm, đắt (bấm qua trình duyệt)
     ╱ ╲ Integration  ← vừa (có DB/API thật)
    ╱───╲ Unit test   ← NHIỀU NHẤT, nhanh, rẻ (cô lập, không DB)
```
→ UT là tầng đáy: **nhiều nhất, rẻ nhất, nhanh nhất**. Làm trước tiên.

## 3. Nó test cái gì — và vì sao "data giả" vẫn đúng
- Lỗi phần mềm **gần như luôn ở LOGIC** (code quyết định sai), không phải ở "DB có xoá được 1 dòng không" (cái đó luôn chạy).
- Ví dụ lỗi UT bắt: lẽ ra xoá mềm lại xoá cứng; lẽ ra chặn lại cho qua; clear cache nhầm đơn vị.
- **Logic không quan tâm data đến từ đâu** → thay DB bằng đồ giả để cô lập đúng phần logic. Đồ giả còn dựng được ca DB thật khó tạo (vd "giữa chừng ném lỗi thì sao?").

## 4. UT chạy LOGIC THẬT xuyên service (khác trace)
- Khi test gọi `service.Method(...)` → **CPU chạy method THẬT**: validate thật, if/else thật, try/catch thật.
- **Chỉ repository/cache** (cổng ra DB/Redis) là giả. Ruột service = code thật.
- Khác `trace-agent`: trace **đọc code & mô phỏng trong đầu** (có thể đọc nhầm); UT **chạy ra kết quả thật** (không sai do đọc nhầm).
- → UT = bản **tự-động-hoá-vĩnh-viễn** của trace: cùng tư duy "data giả qua logic", nhưng máy chạy lại mãi.

## 5. Nguồn "ĐÚNG" = Tiêu chí nghiệm thu (không phải code)
- "Đúng" **không nằm trong code** (soft/hard delete đều chạy, code không tự biết cái nào đúng).
- Nguồn đúng = **Tiêu chí nghiệm thu**: mô tả hành vi mong đợi, viết kiểu **Given / When / Then**:
  - **Given** (cho trước) = bối cảnh · **When** (khi) = hành động · **Then** (thì) = kết quả đúng.
  - Đây là chuẩn thế giới (**BDD**). 1 artifact: **tester → test case, dev → unit test**.
- (Trước hay gọi là "spec" — nhưng team dùng từ **"Tiêu chí nghiệm thu"** cho dễ hiểu.)

## 6. 🔴 Nỗi sợ lớn nhất — và cách tránh
> **"Vô tình viết code sai → viết test khớp code → sai thành đúng."**

- Nếu lấy **expected từ code** → "code tự xác nhận chính nó" → **đóng dấu hợp lệ cho bug**. Tệ hơn không có test.
- **Cách tránh (luật vàng):** expected lấy từ **Tiêu chí nghiệm thu** (nguồn độc lập với code), KHÔNG chép code.
- Code khác tiêu chí → **báo NGHI BUG**, không sửa test cho khớp code.

## 7. Vòng đời "nguồn đúng" — WIP → release
```
ĐANG LÀM (WIP)     → chưa có doc trong KB → DEV là nguồn tiêu chí (viết Given/When/Then)
     │ (lên release_product)
LÊN RELEASE        → nhập tiêu chí vào KB features/ (tài liệu sống, "blessed")
BẢO TRÌ            → KB + test cùng phản ánh release, sửa thì sửa cả 2
```
→ Feature mới chưa có spec là **bình thường**: dev đóng vai nguồn đúng, viết tiêu chí trước.

## 8. Cấu trúc tài liệu (đọc gì, ở đâu)
| File | Nội dung | Khi nào đọc |
|---|---|---|
| `rules/unit-test-gioi-thieu.md` (file này) | Khái niệm / tư duy | Đọc TRƯỚC, 1 lần |
| `rules/unit-test.md` | Nguyên tắc + khuôn CHUNG (luật vàng, mock, cách chạy) | Khi viết test |
| `projects/<proj>/skills/unit-test.md` | Đặc thù project (repo pattern, DTO, nơi để test) | Khi viết test cho project đó |

## 9. Chốt 1 câu
> **UT = kiểm quyết định của code (không đụng DB), chạy lại mãi để bắt lỗi sớm; kết quả đúng lấy từ Tiêu chí nghiệm thu — KHÔNG chép từ code.**
