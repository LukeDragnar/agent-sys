# Registry Node — SCHEMA (chuẩn cho mọi controller node)

> Mỗi controller = 1 file `registry/<module>/<Controller>.md`. Đây là **backbone knowledge-graph** (Pha 1): metadata + quan hệ + hành vi đúng. Prose chi tiết (cách dùng/FAQ) đắp ở `features/` (Pha 2).
> Schema v2 (2026-06-20), chốt từ pilot 5 controller.

## Frontmatter (bắt buộc)
```yaml
controller: <Name>Controller
module: <module slug>            # kho, khach-hang, marketing, ke-toan, sales...
service: <BE service folder>     # WarehouseProduct, Customer, Promotion...
man_hinh_ui: "<menu > tab > vị trí thật trên UI; hoặc 'nội bộ — không có UI'>"
muc_dich: "<1 câu controller lo việc gì>"
alias: ["<cách user/khách hay gọi>", ...]
auth: public | internal | anonymous | mixed   # mức truy cập controller-level
state_machine: [<các trạng thái nếu có>]       # vd [Nhap, DangXuLy, HoanThanh]; rỗng nếu không
deprecated: false                              # true nếu controller/bản cũ khóa cứng
entities: [<entity chính>]
stored_procs: [<SP nếu controller report/view>]
depends: [<service/module phụ thuộc>]
cross_module_in: [<ai GỌI SANG controller này>]    # cạnh vào (graph)
cross_module_out: [<controller này ẢNH HƯỞNG/đồng bộ tới đâu>]   # cạnh ra (ripple)
```

## Thân file
```markdown
# <Controller> — <tên nghiệp vụ tiếng Việt>

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
<!-- 1 dòng = 1 hành động HOẶC nhóm biến thể gần trùng (cột Endpoint gộp nhiều).
     "Hành vi đúng" = kết quả mong đợi → gốc để TRIAGE lỗi/không.
     "Điều kiện hiện/state" = nút ẩn/hiện theo trạng thái (vd chỉ hiện khi maTinhTrang=HoanThanh).
     Chỉ liệt kê hành động có nút/ý nghĩa; gom hành động tầm thường. -->

## Endpoint nội bộ (private / service-to-service)   ← chỉ có nếu controller có
<!-- bullet gom theo MỤC ĐÍCH (không bảng): caller → endpoint → ripple. Vd:
- [internal] Accounting gọi `GetThongTinPhieuNhap` khi đồng bộ chứng từ -->

## Quy tắc nghiệp vụ (gating / tier / ngưỡng)   ← chỉ controller cấu hình phức tạp (KM, voucher, cấu hình)
<!-- danh sách CÓ THỨ TỰ điều kiện rẽ nhánh: điều kiện → nhánh true → nhánh false → field cấu hình.
     Đây là blueprint cho trace-agent mô phỏng số. -->

## Quan hệ
- **Gọi sang:** <service/endpoint khác>
- **Dữ liệu (entities):** <...>
- **Ripple / đồng bộ:** <consumer/event/cross-service>

## Luồng xuyên module   ← chỉ thao tác lớn có ripple (nhập kho, áp KM, đăng đơn...)
<!-- chuỗi: trigger → bước → side-effect cross-module -->

## Lỗi đã biết / lưu ý kỹ thuật
<!-- bullet 1 dòng, kèm path:line. Ngắn gọn, không prose dài. -->
```

## Quy ước nhất quán (để 874 node query được)
- **`Quyền (key)`**: chỉ ghi permission KEY (`PhieuNhap_ThemMoi`), KHÔNG ghi danh sách role (role để map riêng nếu cần).
- **`Auth`**: chuẩn 4 giá trị `public` / `internal` / `anonymous` / `mixed`.
- **1 dòng = 1 nhóm biến thể** được phép (gom endpoint gần trùng), tránh bảng 60 dòng.
- **Controller lớn (>~25 endpoint)**: bảng chỉ giữ hành động có UI; phần còn lại gom ở "Endpoint nội bộ" theo mục đích.
- **Controller view/report**: cột `Nguồn (EF/SP)` quan trọng — ghi rõ đi EF hay stored proc nào.
- **`cross_module_in/out`**: tách rạch ròi (cạnh có hướng) để dựng graph; KHÔNG trộn side-effect nội bộ với cross-service.
- **Known-issue**: 1 dòng + `path:line`; dài thì 1 ý/dòng.
- **`deprecated: true`** nếu controller/method là bản cũ khóa cứng (vd luôn trả lỗi) → AI bỏ qua nhanh.
