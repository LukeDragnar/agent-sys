---
name: i18n loanword translation
description: VN viết tắt và loanword tiếng Anh trong source VN — phải dịch ra EN/ZH đầy đủ, không giữ nguyên acronym tiếng Việt trong cột EN
type: feedback
---

Khi sinh JSON i18n cho EN/ZH, các viết tắt tiếng Việt phải dịch ra đầy đủ, không giữ acronym VN. Loanword tiếng Anh thì giữ nguyên trong cột EN.

**⚠️ QUAN TRỌNG: Phải wrap cả acronym VN không dấu (`STT`, `KH`, `SP`, `DV`, `TT`, `TK`, `MKT`, `CK`, `DS`, `VC`, `GH`, `HH`, `NV`, `CN`, `LH`, `LDP`, `OA`, `PB`, `SL`, `TL`, ...).** Regex extract raw VN chỉ catch text có dấu (`[À-ỹĐđ]`) sẽ MISS các từ này vì chúng nằm trên `<th>STT</th>` / `<th>SDT</th>` không có ký tự có dấu. Khi quét HTML phải **grep thêm pattern**: `>STT<`, `>SP<`, `>DV<`, `>CK<`, `>TL<`, `>SL<`, `>NV<`, `>TT<`, `>TK<`, `>GH<`, `>HH<`, `>HD<`, `>HĐ<`, `>PB<`, `>CN<`, `>VC<`, `>DS<`, `>QC<`, `>LH<`, etc. Không tin chỉ vào regex diacritic.

**⚠️ QUAN TRỌNG 2: Phải wrap cả single English word đứng độc lập làm column header / label.** Ví dụ: `header: 'User'`, `header: 'Team'`, `<th>User</th>`, `<th>Team</th>` — dù là English nguyên gốc, vẫn phải wrap tsb.t() / tsb pipe vì ZH/EN side phải dịch (ZH: `用户`/`团队`). Khi quét code phải grep thêm pattern `header:\s*['"](User|Team|Sales|Lead|Combo|Voucher|IMEI|Wave|Auto)['"]` và `>User<`, `>Team<`, `>Sales<` (không nằm trong tsb wrap). Đừng để hardcode standalone English label sót.

**⚠️ QUAN TRỌNG 3: ZH bản dịch KHÔNG được giữ raw VN hoặc raw EN loanword.** Khi value VN gốc là mix VN+EN như `"User care đơn"`, `"Team sales"`, `"Sales team chăm sóc"` — bản ZH phải dịch FULL sang tiếng Trung (`订单关注用户`, `销售团队`, `客服销售团队`), không được copy "User"/"Team"/"Sales" sang ZH. Khi check key tồn tại DB nhưng UI ZH vẫn show tiếng Việt/Anh → là vì record ZH cũ trong DB sai value, phải re-insert qua InsertOrUpdateLang để overwrite.

**⚠️ QUAN TRỌNG 4: Khi VN raw là common acronym (`STT`, `Stt`, `SP`, `KH`, `NV`, `TT`, `TK`...) hoặc common label (`Tên`, `Mã`, `Email`, `Địa chỉ`...) → PHẢI dùng key common đã có trong DB, KHÔNG tạo key mới scope-specific.** Trước khi wrap "STT"/"Stt", grep `table-stt` trong DB (`i18n.txt`) hoặc session JSON; nếu có sẵn thì dùng `'table-stt' | tsb: 'STT'` thay vì tạo `label-stt`/`form-stt`/.... Tương tự cho các từ phổ thông: `table-thao-tac`, `table-email`, `label-ten`, `label-ma`, ... Lý do: tạo key trùng giá trị (vd `label-stt`/`form-stt`/`title-stt` cùng = "STT") gây phình DB và làm key mới yêu cầu re-curl mỗi route mới mặc dù value identical. **Trước khi quyết định key mới**: search `i18n.txt` + `memory/local/*_vi.json` xem có common key match — nếu match thì reuse.

**Why:** User chỉ ra "mấy cái vd như sales hay team cũng cần dịch nha" + "STT là số thứ tự chưa dịch này lưu vô memory đi sau này tự làm tôi ko muốn nhắc nữa đâu" + "Lưu vào memory đi lần sau nhớ wrap cả STT" — yêu cầu trong EN không giữ acronym VN; phải wrap acronym không dấu (đặc biệt STT).

**How to apply:**
- `STT` (viết tắt VN "Số thứ tự") → EN: `No.` | ZH: `序号`
- `CSKH` (viết tắt VN "Chăm sóc khách hàng") → EN: `Customer Service` | ZH: `客服`
- `KH` (viết tắt VN "Khách hàng") → EN: `Customer` | ZH: `客户`
- `KH cũ` → EN: `Returning customer` | ZH: `老客户`
- `KM` (viết tắt VN "Khuyến mãi") → EN: `Promotion` | ZH: `促销`
- `MKT` / `Marketing` → EN: `Marketing` | ZH: `市场营销`
- `DVVC` (viết tắt VN) → EN: `Shipping unit` | ZH: `运输单位`
- `VC` (vận chuyển) → EN: `Shipping` | ZH: `运输`
- `CTV` (Cộng tác viên) → EN: `Collaborator` | ZH: `合作者`
- `LDP` → EN: `Landing page` | ZH: `落地页`
- `OA` (Zalo OA) → giữ "OA" hoặc dịch theo ngữ cảnh
- `TL` (Tỷ lệ) → EN: `Rate` | ZH: `比率`
- `SL` (Số lượng) → EN: `Qty` | ZH: `数量`
- `SĐT` / `Sđt` (Số điện thoại) → EN: `Phone` | ZH: `电话`
- `TT` (Thanh toán/Thông tin tùy ngữ cảnh) → dịch theo ngữ cảnh
- `HH` (Hoa hồng / Hàng hóa tùy ngữ cảnh) → dịch theo ngữ cảnh
- `NV` (Nhân viên) → EN: `Employee` | ZH: `员工`
- `TMĐT` (Thương mại điện tử) → EN: `E-commerce` | ZH: `电商`
- `DS` (Doanh số) → EN: `Revenue` | ZH: `营收`
- `CK` (Chiết khấu) → EN: `Discount` | ZH: `折扣`
- `DV` (Dịch vụ) → EN: `Service` | ZH: `服务`
- `SP` (Sản phẩm) → EN: `Product` | ZH: `产品`
- `NCC` (Nhà cung cấp) → EN: `Supplier` | ZH: `供应商`
- `ĐV` (Đơn vị) → EN: `Unit` | ZH: `单位`
- `CN` (Chi nhánh) → EN: `Branch` | ZH: `分支`
- `QC` (Quảng cáo) → EN: `Ad` | ZH: `广告`
- `GH` (Giao hàng) → EN: `Delivery` | ZH: `送货`
- `TK` (Tài khoản) → EN: `Account` | ZH: `账户`
- `HĐ` (Hóa đơn / Hợp đồng tùy ngữ cảnh) → dịch theo ngữ cảnh
- `PB` (Phân bổ) → EN: `Distribute` | ZH: `分配`
- `LH` (Lịch hẹn / Liên hệ tùy ngữ cảnh) → dịch theo ngữ cảnh
- `NXB` (Nhà xuất bản) → EN: `Publisher` | ZH: `出版商`

Loanword tiếng Anh giữ nguyên trong cột EN:
- `sales` / `Sales` — EN giữ "sales" / "Sales staff" tùy ngữ cảnh | ZH: `销售`
- `team` — EN giữ "team" | ZH: `团队`
- `lead` — EN giữ "lead" | ZH: `线索`
- `combo` — EN giữ "combo" | ZH: `套餐`
- `voucher` — EN giữ "voucher" | ZH: `代金券`
- `imei`, `IMEI` — EN: `IMEI` | ZH: `IMEI`
- `user` / `User` / `users` / `Users` — EN giữ "user/User/users/Users" | ZH: `用户`
- `data` / `Data` — EN giữ "data" | ZH: `数据`
- `care` (care đơn) — EN giữ "care" (Care order) | ZH: `关注` (订单关注)
- `wave` (Wave nhận số) — EN giữ "wave" | ZH: `批次`
- `auto` / `Auto` — EN giữ "Auto" | ZH: `自动`

Sau khi update file JSON, **báo user re-curl** lại để DB sync (file đã insert phiên bản cũ).
