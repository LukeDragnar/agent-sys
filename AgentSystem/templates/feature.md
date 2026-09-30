# Template — Feature (cẩm nang tính năng)

> Copy khuôn này khi thêm 1 tính năng vào `projects/<mảng>/features/<module>/<feature>/`. Mỗi feature 1 folder, 4 file. summarize/faq = khách+team đọc (KHÔNG lòi code); dev = chỉ dev.

## summarize.md
```markdown
---
alias: ["<cách khách/user hay gọi, 3-5 cụm>"]
module: <module>
trang_thai: <Core | Add-on | cần bật | cần cấu hình | cần product xác nhận>
goi: <gói được dùng — cần product xác nhận nếu chưa rõ>
quyen: <vai được dùng — cần product xác nhận>
phu_thuoc: [<module/tính năng phụ thuộc>]
---
# <Tên tính năng>
## Là gì            (1-2 câu định nghĩa)
## Hành động làm được   (BẢNG quét nhanh — trả lời "có thêm/sửa/xóa/… được không")
                    | Hành động | Được? | Điều kiện / quyền |
                    |---|---|---|
                    | Thêm / Sửa / Xóa / Nhập / Xuất / In / Gửi… | ✅/❌/⚠️ | tùy quyền/trạng thái; ❌ ghi rõ lý do (vd "đã ngừng dùng") |
## Để làm gì / cho ai   (giá trị nghiệp vụ + actor)
## Cách dùng        (các bước chính, ngôn ngữ thường, KHÔNG kỹ thuật)
## Cần chú ý        (cạm bẫy/ràng buộc/ngoại lệ — nuôi từ ticket; phần quý nhất)
## Ảnh hưởng tới    (đụng đây liên quan/gãy đâu)
```

## faq.md
```markdown
# <Tên> — Câu hỏi thường gặp
> Draft — cần bổ sung từ ticket khách thật.
### <Câu hỏi khách hay hỏi>?
<Trả lời ngắn, ngôn ngữ khách>   (5-8 cặp)
```

## flow.md
```markdown
# <Tên> — Luồng
## Tiền điều kiện
## Luồng dùng (user)        (bước 1,2,3 + kết quả mong đợi)
## Luồng cấu hình (admin)   (nếu có)
## Các trường hợp / case biên   (rollback, retry, input rỗng, vượt cap, tồn=0, đổi trạng thái ngược)
## Ví dụ cụ thể có số       (đề + đáp án → đề bài test)
## Hậu điều kiện
```

## dev.md (chỉ dev)
```markdown
# <Tên> — Kỹ thuật
## Màn hình & route · ## Endpoint/Controller (trace registry) · ## Entities
## Luồng code · ## Bug-prone/lưu ý (path:line) · ## File source
```
