# Convention: Thêm config mới — object lồng + cờ opt-in

**Áp dụng khi:** thêm cấu hình mới vào DTO cấu hình lớn (vd `CauHinhSettingDto` — hàng chục field phẳng), nhất là khi hành vi mới áp vào một luồng đang chạy.

## Quy tắc
1. **Gom vào OBJECT LỒNG có tên rõ nghĩa**, KHÔNG rải thêm field phẳng.
   - `CauHinhSettingDto` đã là cục ~40 field tên trần (`RequiredFieldsLayTheoDon`, `XuLyKhiThieuThongTin`, `EcomLayTheoThongTinDon`...) — thêm field phẳng nữa càng tối nghĩa, khó maintain.
   - Đúng: `public CauHinhDonThuongKhongYeuCauXuatHoaDon? DonThuongKhongYeuCauXuatHoaDon { get; set; }` — 1 object lồng, **tên đầy đủ không viết tắt**; field con ngắn gọn được (`IsApDung`, `RequiredFields`, `XuLyKhiThieu`) vì đã có namespace.
2. **Cờ `IsApDung` opt-in (default false)** khi hành vi mới áp vào luồng CŨ.
   - `false/null` → giữ nguyên hành vi cũ; `true` → mới chạy nhánh mới.
   - Nhờ vậy: tenant cũ **không đổi hành vi** sau deploy, **không cần backfill DB**.
3. **AutoMapper**: object lồng CÙNG type ở `CauHinhSettingDto` ↔ `CauHinhSettingInitDto` → `AssignableMapper` tự copy (source/dest cùng type = assignable), **KHÔNG cần `CreateMap` con**. Đã có `CreateMap<CauHinhSettingDto, CauHinhSettingInitDto>().ReverseMap()`.
4. **Lưu**: config lồng nằm trong cột JSON có sẵn (`CauHinh.JsonSetting`) → **không cột DB mới, không migration**. Config cũ chưa có key → deserialize null → cờ opt-in false → hành vi cũ (backward-compat tự nhiên).

## Đừng
- ❌ Đẻ magic number mới cho dispatch (`cachLay = 41`) + nhân bản helper `_B2` khi thêm biến thể → tái dùng dispatch có sẵn + tham số hóa helper (xem `../qa-code.md` mục 14).

Ví dụ thật: B2 "đơn thường không yêu cầu xuất HĐ" (session 2026-07-16). Module song song: `../lessons/sua-orderlogistic-quen-order-song-song.md`.
