# SCRM — Cẩm nang tính năng (features/)

> Cẩm nang theo **tính năng** cho cả team + khách hàng (nuôi chatbot AI sau này). Đắp dần lên trên registry (knowledge-graph theo controller).

## Cấu trúc

```
features/
├── INDEX.md                         # KHUNG: danh sách tính năng toàn hệ thống theo nghiệp vụ
├── README.md                        # file này
└── <module>/                        # nhóm nghiệp vụ: khach-hang, kho, marketing, sales, ke-toan, website...
    ├── README.md                    # (tùy module) mục lục tính năng con
    └── <feature>/                   # 1 tính năng = 1 folder, 4 file
        ├── summarize.md             # "Là gì" + bảng "Hành động làm được" + cách dùng + cần chú ý  (khách + team đọc, KHÔNG lòi code)
        ├── faq.md                   # câu hỏi khách hay hỏi (nuôi từ ticket thật)
        ├── flow.md                  # luồng dùng + case biên + ví dụ có số (test/đào tạo)
        └── dev.md                   # kỹ thuật: route/endpoint/entities/luồng code/bug-prone (chỉ dev)
```

## Quy ước

- **Mỗi feature đủ 4 file** `summarize.md` · `faq.md` · `flow.md` · `dev.md`. Khuôn gốc: [`templates/feature.md`](../../../templates/feature.md). Mẫu tham chiếu: `khach-hang/quan-ly-thong-tin-kh/`, `website/dat-hang-online/`.
- **summarize.md bắt buộc có bảng "Hành động làm được"** (quét nhanh thêm/sửa/xóa/nhập/xuất… được không) ngay sau "## Là gì".
- **Phân vai đọc:** `summarize` + `faq` cho khách & team (ngôn ngữ thường, không kỹ thuật) · `flow` cho test/đào tạo · `dev` chỉ cho dev (được tham chiếu code/path:line).
- **Nguồn dev.md:** trace từ `projects/scrm/registry/<module>/<Controller>.md` — không bịa; chỗ chưa chắc ghi "cần product xác nhận".

## Trỏ tới

- KHUNG tổng + trạng thái phủ: [`INDEX.md`](./INDEX.md)
- Knowledge-graph theo controller (nguồn "hệ thống làm được gì"): [`../registry/README.md`](../registry/README.md)
- Khuôn thêm feature mới: [`templates/feature.md`](../../../templates/feature.md)
