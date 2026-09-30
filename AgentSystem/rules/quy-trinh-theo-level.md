# Quy trình theo cấp độ & cờ leo thang

> Không cào bằng: việc dễ đi nhanh, việc khó/nguy hiểm đi đủ phanh. Cấp độ đặt **baseline**; quyết định cuối theo **rủi ro = độ khó + bán kính ảnh hưởng**.

## Bậc thang (định nghĩa "XONG" theo cấp)
| Cấp | Loại việc | Bắt buộc qua (cộng dồn) |
|---|---|---|
| **L1** rất dễ | đổi text/nhãn, ẩn/hiện, sắp xếp trường (FE, ~1–2h) | convention tự áp + tự soát · review nhẹ. KHÔNG cần trace/test. Cập nhật cẩm nang nếu đổi hành vi |
| **L2** dễ | lọc/tìm/export đơn giản, sửa nhỏ logic, thêm field form có sẵn (~2–4h) | + review-agent đầy đủ + trace phần logic + vài case test (nếu có logic) |
| **L3** trung bình *(ngưỡng "qua đủ quy tắc")* | logic nghiệp vụ nhỏ, API mới, thêm field DB, phân quyền cơ bản (~1–2 ngày) | + dò ảnh hưởng (impact qua `registry` cross_module) + test logic từ qa-spec + cập nhật cẩm nang — **bắt buộc** |
| **L4** khó | nghiệp vụ phức tạp, nhiều màn/luồng, RBAC, đồng bộ nội bộ (~3–5 ngày) | + duyệt kế hoạch TRƯỚC khi code + checklist case biên đầy đủ + chia nhỏ task + lead duyệt ở cổng |
| **L5** rất khó | module mới, tích hợp bên thứ 3, đổi kiến trúc, realtime đa hệ thống (>1 tuần) | + spike trước rồi mới chốt + duyệt thiết kế với CTO + giao theo giai đoạn + test máy cho phần lõi |

## Cờ leo thang — gate theo RỦI RO (không chỉ độ khó)
Task **chạm vào** 1 trong các vùng dưới → áp **tối thiểu L3** dù công sức chỉ L1–L2:
- Tiền / thanh toán / hóa đơn / kế toán
- Khuyến mãi · kho/tồn/imei · rollback đơn · **giá vốn**
- Filter multi-tenant (`IdDonVi`/`IdChiNhanh`) — rủi ro rò dữ liệu chéo tenant
- Đổi schema / migration DB
- Tích hợp ngoài (sàn · ĐVVC · ZNS · MISA/Viettel · webhook nhận tiền)
- Phân quyền (RBAC)

> Đây đúng mấy vùng dồn bug + lỗ hổng đã phát hiện khi quét registry — "nhìn dễ mà gãy nặng".

## Chỗ để metadata
- Level + cờ rủi ro → frontmatter/metadata task.
- Bán kính ảnh hưởng → `registry` node (`cross_module_in/out`) + ô "Ảnh hưởng tới" của cẩm nang.
- Level = cỡ S/M/L (L1–2=S · L3=M · L4–5=L) → nối vào cách giao việc.
