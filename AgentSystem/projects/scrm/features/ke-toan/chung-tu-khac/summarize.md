---
alias: ["chứng từ khác", "chứng từ nghiệp vụ khác", "hạch toán thủ công", "bút toán tự do", "phiếu nghiệp vụ khác"]
module: ke-toan
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: "cần product xác nhận — chỉ màn Danh sách có quyền riêng; lập chứng từ, xem chi tiết, xóa, xuất Excel chỉ cần đăng nhập (kể cả lập & xóa chứng từ hạch toán — rủi ro phân quyền, xem dev.md). Người không phải admin bị ép xem theo chi nhánh mình."
phu_thuoc: ["Danh mục tài khoản kế toán (chọn TkNo / TkCo)", "Đối tượng nợ / có (khách hàng / nhà cung cấp / tài khoản — đồng bộ MISA)", "Bút toán kế toán (sinh hạch toán Nợ/Có)", "Cấu hình kết nối kế toán / MISA (quyết định có đẩy chứng từ sang MISA)"]
link_flow: ./flow.md
---

# Chứng từ khác (Chứng từ nghiệp vụ khác)

## Là gì
**Chứng từ hạch toán thủ công, đa dòng** — dùng để ghi nhận những nghiệp vụ kế toán không nằm trong các phiếu mua/bán/thu/chi chuẩn. Mỗi chứng từ gồm nhiều dòng, mỗi dòng là một cặp **Tài khoản Nợ – Tài khoản Có** kèm đối tượng nợ/có và số tiền. Đây là công cụ "bút toán tự do" cho kế toán.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Lập chứng từ (nhiều dòng Nợ/Có) | ✅ | chỉ cần đăng nhập; phải nhập ≥ 1 dòng hợp lệ |
| Xem danh sách chứng từ | ✅ | cần quyền xem danh sách |
| Xem chi tiết chứng từ | ✅ | chỉ cần đăng nhập |
| Xóa chứng từ | ⚠️ | chỉ cần đăng nhập; **không xóa được chứng từ tạo tự động** (sinh từ màn thanh toán) |
| Xuất Excel | ✅ | chỉ cần đăng nhập; rỗng → "Không có dữ liệu" |
| Sửa chứng từ đã lập | ❓ | cần product xác nhận — không có endpoint cập nhật riêng trong phạm vi registry |

## Để làm gì / cho ai
- **Kế toán:** ghi các bút toán điều chỉnh, kết chuyển, phân bổ, hoặc nghiệp vụ đặc thù mà không có phiếu chuẩn — linh hoạt theo cặp Nợ/Có tự chọn.
- **Kế toán trưởng:** kiểm soát các bút toán thủ công và đồng bộ chúng sang phần mềm kế toán (MISA) nếu đơn vị có kết nối.

## Cách dùng
1. Vào **Kế toán → Chứng từ nghiệp vụ khác**.
2. Bấm lập chứng từ mới: nhập số chứng từ (hoặc để trống cho hệ thống tự sinh), ngày chứng từ, diễn giải.
3. Thêm các dòng hạch toán: mỗi dòng chọn **Tài khoản Nợ**, **Tài khoản Có**, đối tượng nợ/có (nếu cần) và số tiền.
4. **Lưu** — hệ thống kiểm tra hợp lệ, đồng bộ đối tượng sang MISA, sinh bút toán, và (nếu đơn vị có cấu hình kết nối kế toán) đưa chứng từ vào hàng đợi đẩy MISA.
5. Trên lưới có thể lọc theo số chứng từ / diễn giải / khoảng ngày / trạng thái đồng bộ; xem chi tiết; xóa; xuất Excel.

## Cần chú ý
- **Số chứng từ không được trùng** trong cùng đơn vị + chi nhánh; để trống thì hệ thống tự sinh.
- **Không xóa được chứng từ tạo tự động** (chứng từ sinh từ màn thanh toán có gắn hóa đơn) — chỉ chứng từ lập tay mới xóa được.
- **Đẩy MISA có điều kiện:** chứng từ chỉ tự vào hàng đợi đẩy MISA khi đơn vị/chi nhánh **có cấu hình kết nối kế toán**. Không có cấu hình thì chứng từ vẫn lưu nhưng phải tích đẩy thủ công sau.
- **Mỗi người xem dòng khác nhau:** người không phải admin chỉ thấy chứng từ chi nhánh mình.
- Một số thông báo lỗi hiện sai ngữ cảnh (ví dụ báo "Id phiếu bù trừ không hợp lệ" khi xóa) — chỉ là text, không ảnh hưởng nghiệp vụ (xem dev.md).

## Ảnh hưởng tới
- **Bút toán & sổ kế toán** — mỗi dòng Nợ/Có sinh hạch toán tương ứng.
- **Đồng bộ MISA** — đối tượng nợ/có và chứng từ được đẩy sang MISA nếu có kết nối.
