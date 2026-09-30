# Đồng bộ từ nguồn NGOÀI + cho user sửa tay → phải tách 2 cột

> **Khi nào đọc:** làm tính năng mà một trường vừa được **hệ thống ngoài đồng bộ định kỳ** (sàn TMĐT, đối tác vận chuyển, kế toán…) vừa **cho người dùng sửa tay**.
> Nguồn: T800/B318 — cờ "khách có yêu cầu xuất hóa đơn" của đơn sàn.

## Vấn đề

Một cột, hai người ghi:
- **Nguồn ngoài** ghi mỗi lần đồng bộ (webhook/job kéo lại).
- **Người dùng** sửa tay trên UI.

⇒ Lần đồng bộ kế tiếp **đè mất** chỉnh tay của người dùng, im lặng, không ai biết vì sao. Khách báo *"tôi sửa rồi mà hôm sau lại về như cũ"*.

Mở khoá cho user sửa mà **không** giải quyết việc này = fix nửa vời: đúng được đến lần sync sau.

## Cách làm

Tách **tín hiệu gốc** khỏi **giá trị hiệu lực**:

| Cột | Ai ghi | Ý nghĩa |
|---|---|---|
| `<Nguon>TenTruong` (vd `TMDTKhachHangYeuCau`) | **CHỈ** luồng đồng bộ ngoài | nguồn ngoài đang báo gì |
| `TenTruong` (cột cũ) | nguồn ngoài (khi user chưa đụng) **hoặc** user | giá trị **đang áp dụng** — chỗ nghiệp vụ khác đọc |

**Dấu hiệu "user đã sửa tay" = 2 cột LỆCH nhau** — vì luồng đồng bộ luôn ghi cả hai bằng nhau.

```csharp
// Trong luồng đồng bộ (webhook), TÍNH TRƯỚC khi ghi bất kỳ cột nào:
var daSuaTay = row.NguonTruong != null && row.Truong != row.NguonTruong;
row.NguonTruong = giaTriMoiTuNguon;              // vết nguồn: LUÔN ghi
if (!daSuaTay) row.Truong = giaTriMoiTuNguon;    // user chưa đụng -> theo nguồn
                                                  // user đã sửa -> GIỮ, không đè
```

## Bốn cái bẫy (đều đã dính thật ở B318)

1. **Tính `daSuaTay` SAU khi ghi cột** → so sánh với giá trị vừa đè → luôn ra "chưa sửa" → vô hiệu hoá toàn bộ cơ chế. **Phải tính trước.**
2. **Record chưa có vết nguồn** (`NguonTruong = NULL`): record do user tạo trước khi nguồn kịp gửi, hoặc dữ liệu cũ trước khi thêm cột → lần sync sau tưởng "chưa sửa tay" và đè. → lần đầu user sửa tay mà cột vết còn trống thì **chốt vết** = giá trị nguồn đã biết tại thời điểm đó.
3. **Dữ liệu đang chạy production** không tự có vết → cần **backfill**, nhưng phải lọc đúng nhóm do nguồn ghi (xem `qa-code.md` › backfill), đừng copy cả cụm.
4. **User sửa tay đúng bằng giá trị nguồn** → 2 cột trùng → mất dấu "đã sửa". Thường vô hại (giá trị giống nhau) nhưng phải ý thức và ghi vào doc.

## Ràng buộc chiều ngược lại (nếu nghiệp vụ cần)

Có trường mà **nguồn ngoài nói gì là sự thật**, user không được phép lật (vd sàn báo khách CÓ yêu cầu xuất hóa đơn = yêu cầu thật của khách):
- Khoá ở **FE** (disable đúng ô đó, **không khoá cả form** — các field khác vẫn phải sửa được), **và**
- Chặn ở **BE** (bỏ qua thay đổi trái luật, vẫn lưu phần dữ liệu hợp lệ) — vì FE chỉ là lớp trình bày, gọi thẳng API là lách được.

## Liên quan
- `qa-code.md` › mục 8b (ghi cùng trường ở nhiều nhánh) + checklist backfill
- `patterns.md` › Naming (prefix `TMDT` cho cột nghiệp vụ sàn)
- Feature: `features/hoa-don-yeu-cau-xuat-logistic/`
