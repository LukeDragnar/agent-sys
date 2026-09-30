---
alias: ["quản lý ngôn ngữ", "đa ngôn ngữ", "i18n", "dịch giao diện", "bản dịch", "ngôn ngữ giao diện", "translate", "từ điển label"]
module: he-thong
trang_thai: cần product xác nhận — chức năng nội bộ SaaS (quản lý bản dịch giao diện)
goi: cần product xác nhận
quyen: "cần product xác nhận — code HIỆN chỉ yêu cầu ĐĂNG NHẬP (không gắn permission key riêng) cho phần CRUD/Gen/Import ngôn ngữ; lý tưởng nên giới hạn admin hệ thống (HT-AD). Riêng việc tải file ngôn ngữ cho giao diện là công khai (anonymous)."
phu_thuoc: ["File ngôn ngữ tĩnh trong thư mục Resources/i18n (giao diện đọc nhãn từ đây)", "Bảng từ điển dịch trong DB (NgonNgu / NgonNguTranslate)", "Từ điển cấu hình mobile (MobileDictionary) cho nhãn trên app di động"]
link_flow: ./flow.md
---

# Quản lý ngôn ngữ (đa ngôn ngữ / i18n)

## Là gì
Đây là nơi **quản lý các bản dịch nhãn giao diện** của hệ thống theo từng "key" (mã nhãn). Mỗi nhãn hiển thị trên màn hình (tiêu đề nút, tên cột, thông báo…) tương ứng một key; với mỗi key có thể khai báo nội dung theo từng ngôn ngữ. Đội nội bộ sửa bản dịch ở đây, sau đó **sinh ra file ngôn ngữ** để giao diện đọc và hiển thị.

Có 2 phần liên quan:
- **Bản dịch giao diện web** (key → nội dung theo ngôn ngữ): thêm/sửa/xóa key, sinh file, nhập bản dịch hàng loạt.
- **Từ điển cấu hình mobile** (nhãn/cấu hình cho app di động): quản lý theo cặp Mã – Giá trị – Giá trị mặc định – Mô tả; app đọc để hiển thị.

> Đây là phần nội bộ vận hành. Khách hàng cuối chỉ thấy kết quả: giao diện hiển thị đúng ngôn ngữ/nhãn.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Xem danh sách key dịch | ✅ | hiện chỉ cần đăng nhập (nên giới hạn admin — *cần product xác nhận*) |
| Thêm key dịch | ✅ | hiện chỉ cần đăng nhập |
| Sửa key dịch | ✅ | hiện chỉ cần đăng nhập |
| Xóa key dịch | ✅ | hiện chỉ cần đăng nhập; **không cảnh báo nếu giao diện còn dùng key đó** |
| Nhập bản dịch hàng loạt (theo ngôn ngữ) | ✅ | upload file theo từng ngôn ngữ |
| **Sinh (Gen) file ngôn ngữ** | ✅ | bắt buộc làm sau khi sửa thì giao diện mới thấy thay đổi |
| Tải file ngôn ngữ cho giao diện | ✅ | công khai (giao diện tự gọi khi mở app, kể cả chưa đăng nhập) |
| (Mobile) Thêm/Sửa/Xóa từ điển nhãn mobile | ✅ | hiện không gắn quyền chi tiết (*cần product xác nhận*) |

## Để làm gì / cho ai
- **Đội nội bộ / vận hành SaaS:** chỉnh sửa câu chữ trên giao diện mà không cần build lại code; bổ sung ngôn ngữ; sửa nhãn sai chính tả; chuẩn hóa thuật ngữ.
- **App di động:** lấy nhãn/cấu hình từ từ điển mobile để hiển thị.

## Cách dùng
**Sửa nhãn giao diện web:**
1. Vào **Hệ thống → Quản lý ngôn ngữ**.
2. Tìm key cần sửa (theo điều kiện/lọc), mở chi tiết để xem bản dịch các ngôn ngữ.
3. Sửa nội dung (hoặc thêm key mới / xóa key không dùng).
4. **Bấm "Gen file lang"** để sinh lại file ngôn ngữ — **bước này bắt buộc**, nếu bỏ qua thì sửa trong hệ thống nhưng giao diện vẫn hiển thị nhãn cũ.
5. Tải lại giao diện để thấy nhãn mới.

**Nhập bản dịch hàng loạt:** chọn ngôn ngữ → upload file bản dịch → hệ thống nạp vào → nhớ **Gen file** lại.

**Từ điển mobile:** vào màn Quản lý Dictionary → thêm/sửa/xóa cặp Mã–Giá trị; app sẽ đọc từ điển này.

## Cần chú ý
- **Sửa xong PHẢI bấm Gen file** thì giao diện mới đổi. Đây là cạm bẫy số một: "sửa nhãn mà không thấy đổi" gần như luôn do quên Gen.
- **Xóa key cần cẩn thận:** hệ thống không kiểm tra xem giao diện có còn dùng key đó không. Xóa nhầm key đang dùng → chỗ đó hiển thị trống/lỗi nhãn.
- **Phần này hiện chỉ chặn ở mức "đã đăng nhập"** chứ chưa có quyền riêng — ai đăng nhập cũng sửa được bản dịch. Đây là điểm rủi ro nội bộ (*cần product xác nhận có siết quyền hay không*).
- Việc tải file ngôn ngữ cho giao diện là **công khai** (app cần lấy nhãn trước cả khi đăng nhập) — đây là thiết kế bình thường.

## Ảnh hưởng tới
- **Mọi màn hình** đọc nhãn từ file ngôn ngữ đã Gen — sửa bản dịch ảnh hưởng toàn hệ thống.
- **App di động** đọc từ điển mobile để hiển thị nhãn/cấu hình.
