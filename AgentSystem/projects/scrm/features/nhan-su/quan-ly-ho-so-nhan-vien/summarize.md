---
alias: ["hồ sơ nhân viên", "quản lý nhân viên", "tài khoản nhân viên", "danh sách nhân viên", "phòng ban", "cơ cấu tổ chức", "nhóm nhân viên", "nhóm sale", "nhóm CSKH"]
module: nhan-su
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: "Quản lý tài khoản nhân viên (thêm/sửa/xóa/khóa/đổi mật khẩu/đồng bộ): vai HT-AD (admin hệ thống); một số tra cứu đơn vị mở thêm cho HT-CSKH. Phòng ban & nhóm nhân viên: thao tác cấp đơn vị cần DV-AD-TONG, cấp chi nhánh cần DV-AD-CN (xem dev.md). Một số dropdown/khởi tạo mẫu chỉ chặn mức 'đã đăng nhập' — cần product xác nhận vai chính xác."
phu_thuoc: ["Phân quyền & vai trò (role)", "Đơn vị / chi nhánh (gắn user vào đúng đơn vị-chi nhánh, đồng bộ user)", "Ngành nghề & domain của đơn vị", "Phân ca / chấm công / hoa hồng (tham chiếu nhân viên)", "Phân công sale / CSKH (dùng nhóm nhân viên)"]
link_flow: ./flow.md
---

# Quản lý hồ sơ nhân viên

## Là gì
Đây là nơi **quản lý thông tin và tài khoản của nhân viên**, cùng với **cơ cấu tổ chức** của shop: nhân viên thuộc phòng ban nào, nhóm nào (nhóm sale, nhóm CSKH…). Gồm 3 mảng:
- **Tài khoản nhân viên**: thêm/sửa/xóa nhân viên, khóa/mở tài khoản, đổi mật khẩu, đồng bộ tài khoản theo đơn vị.
- **Phòng ban (cơ cấu tổ chức)**: dựng cây phòng ban, kéo-thả sắp xếp, gắn người quản lý cho phòng ban.
- **Nhóm nhân viên**: tạo các nhóm (sale, CSKH…), thêm/bớt người vào nhóm — đây là cơ sở để phân công sale, chia đơn, chia chat sau này.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Thêm nhân viên (tài khoản) | ✅ | admin hệ thống (HT-AD) |
| Sửa thông tin nhân viên | ✅ | admin hệ thống (HT-AD) |
| Xóa tài khoản nhân viên | ✅ | admin hệ thống (HT-AD) |
| Khóa / mở tài khoản | ✅ | admin hệ thống (HT-AD) |
| Đổi mật khẩu nhân viên | ✅ | admin hệ thống (HT-AD) |
| Đồng bộ tài khoản theo đơn vị | ✅ | admin super (CSKH/super-admin) |
| Tìm đơn vị/chi nhánh để chuyển nhân viên | ✅ | HT-AD hoặc HT-CSKH |
| Thêm / sửa / xóa phòng ban | ✅ | cấp đơn vị: DV-AD-TONG · cấp chi nhánh: DV-AD-CN |
| Sắp xếp cơ cấu phòng ban (kéo-thả) | ✅ | DV-AD-TONG / DV-AD-CN |
| Gắn người quản lý cho phòng ban | ✅ | DV-AD-TONG / DV-AD-CN |
| Thêm / sửa / xóa nhóm nhân viên | ✅ | DV-AD-TONG |
| Thêm / xóa nhân viên trong nhóm | ✅ | cấp đơn vị: DV-AD-TONG · cấp chi nhánh: DV-AD-CN |
| Khởi tạo nhóm mẫu theo ngành nghề | ⚠️ | code chỉ chặn "đã đăng nhập" — cần product xác nhận (xem "Cần chú ý") |

## Để làm gì / cho ai
- **Admin hệ thống / chủ shop:** mở tài khoản cho nhân viên mới, khóa tài khoản khi nghỉ việc, reset mật khẩu, đảm bảo nhân viên thuộc đúng đơn vị/chi nhánh.
- **Quản lý đơn vị / chi nhánh:** dựng cây phòng ban, chia nhân viên vào nhóm để phân công công việc, sale, CSKH.
- **CSKH nội bộ / super-admin:** tra cứu đơn vị để chuyển nhân viên, đồng bộ lại tài khoản khi dữ liệu lệch.

## Cách dùng
**Quản lý tài khoản nhân viên:**
1. Vào màn **Quản lý nhân viên** (khu admin).
2. Tìm theo điều kiện → bảng nhân viên hiện ra.
3. **Thêm mới** (gắn đơn vị/chi nhánh; hệ thống tự lấy ngành nghề + domain theo đơn vị), **Sửa**, **Xóa**, **Khóa/Mở**, **Đổi mật khẩu** tùy nhu cầu.
4. Khi tài khoản lệch dữ liệu → dùng **Đồng bộ tài khoản theo đơn vị**.

**Dựng cơ cấu phòng ban:**
1. Vào **Đơn vị → Cơ cấu tổ chức → Phòng ban**.
2. Thêm phòng ban (cấp đơn vị hoặc cấp chi nhánh), kéo-thả để sắp xếp cấp cha-con.
3. Có thể thêm phòng ban kèm gán luôn người quản lý.

**Quản lý nhóm nhân viên:**
1. Vào **Đơn vị → Cơ cấu tổ chức → Nhóm nhân viên**.
2. Tạo nhóm (sale/CSKH…), thêm nhân viên vào nhóm.
3. Nhóm này được dùng làm dropdown ở các màn phân công sale, chia đơn, chia chat.

## Cần chú ý
- **Phòng ban & nhóm tách thao tác theo cấp**: bản "Đơn vị" (\_Unit) cho admin tổng (DV-AD-TONG), bản "Chi nhánh" (\_Branch) cho admin chi nhánh (DV-AD-CN, tự gán chi nhánh của mình). Gọi nhầm cấp sẽ thiếu quyền.
- **Một số endpoint dropdown / khởi tạo nhóm mẫu chỉ chặn ở mức "đã đăng nhập"** (không gate vai). Việc khởi tạo nhóm mẫu theo ngành nghề về lý thuyết người dùng thường cũng gọi được — cần product/đội bảo mật xác nhận (xem dev.md).
- **Tên route một số thao tác không theo chuẩn** (ví dụ thêm nhóm là "ThemMoi-Nhom", cập nhật là "CapNhap-Nhom" — viết thiếu dấu) → chỉ ảnh hưởng dev khi gọi API, người dùng không thấy.
- **Domain tài khoản theo môi trường được gán cứng trong code** khi nhân viên không thuộc đơn vị nào → đổi domain phải sửa code (việc của dev).
- **Thay đổi tài khoản (thêm/sửa/đồng bộ) lan sang nhiều service khác** theo đơn vị — sửa sai có thể ảnh hưởng đăng nhập và phân quyền của nhân viên.

## Ảnh hưởng tới
- **Phân quyền & đăng nhập:** thêm/sửa/khóa tài khoản tác động trực tiếp quyền truy cập của nhân viên.
- **Phân công sale / CSKH / chia đơn / chia chat:** đều lấy nhóm nhân viên từ đây.
- **Phân cấp lead/quản lý sale:** dựa trên cây phòng ban và thành viên nhóm (đổi thành viên → đổi quan hệ lead/quản lý).
- **Chấm công, hoa hồng, thưởng phạt, lương:** tham chiếu hồ sơ nhân viên này.

---

## (Bổ sung Pha 2 — phần Hồ sơ nhân sự HR; product xác nhận có tách feature riêng không)

> Phần trên cover **tài khoản + cơ cấu tổ chức** (User/PhongBan/NhomNhanVien — service Core).
> Phần này cover **màn "Hồ sơ nhân sự HR"** thật (service HumanResource — `HumanController`): mở hồ sơ 1 nhân sự để xem/sửa **thông tin nhân thân + thông tin nghiệp vụ HR**, và quản lý **ảnh nhân sự** + **file giấy tờ** (Giấy tờ liên quan / Quyết định bổ nhiệm).
> **Cần product xác nhận:** có nên tách "Hồ sơ nhân sự HR" thành feature riêng hay giữ chung trong feature này.

### Hồ sơ nhân sự HR là gì
Mỗi nhân viên (tài khoản User ở phần trên) có 1 **hồ sơ nhân sự** đi kèm. Hồ sơ gồm:
- **Thông tin nhân thân** (họ tên, giới tính, email, SĐT, ngày sinh, địa chỉ, quê quán, avatar…) — lấy từ tài khoản nhân viên (User).
- **Thông tin nghiệp vụ HR**: bí danh, Facebook, ngày vào công ty, ngày lên chính thức, ngày nghỉ việc, nguồn tuyển dụng, quê quán.
- **Ảnh nhân sự**: danh sách ảnh đính kèm cho nhân sự (có thể lọc theo khoảng ngày).
- **Giấy tờ liên quan / Quyết định bổ nhiệm**: danh sách file giấy tờ theo từng loại (mỗi loại có tên + biểu tượng, có thể đính kèm link file).

### Hành động làm được (Hồ sơ nhân sự HR)
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Mở / xem hồ sơ nhân sự của 1 nhân viên | ✅ | Các vai đơn vị: admin tổng / chi nhánh / mini-chi-nhánh (DV-AD-TONG, DV-AD-CN, DV-AD-MCN), kế toán (DV-KT, DV-KT-TONG), quản lý đơn vị (DV-QLDV) |
| Thêm mới thông tin nghiệp vụ HR cho nhân sự | ✅ | Như trên |
| Sửa thông tin nghiệp vụ HR + đồng bộ danh sách giấy tờ | ✅ | Như trên |
| Xem ảnh nhân sự (lọc theo khoảng ngày) | ✅ | Như trên |
| Thêm ảnh nhân sự | ✅ | Như trên |
| Xóa ảnh nhân sự | ✅ | Như trên |
| Thêm 1 loại giấy tờ cho nhân sự | ✅ | Như trên (xem "Cần chú ý" — quyền chưa chuẩn) |
| Xóa 1 file giấy tờ | ✅ | Như trên (xem "Cần chú ý" — quyền chưa chuẩn) |
| Sửa họ tên / nhân thân ngay trong form HR | ❌ | Họ tên + nhân thân lưu ở tài khoản nhân viên (User), form HR không gửi/không sửa các field này — sửa nhân thân làm ở màn **Quản lý nhân viên** (phần trên) |
| Liệt kê toàn bộ danh sách nhân sự HR (lưới riêng) | ❌ | Hiện không mở trên màn — màn HR vào theo từng nhân viên (mở hồ sơ theo nhân viên), chưa có lưới danh sách nhân sự HR công khai. Cần product xác nhận có cần lưới riêng không |

### Cách dùng (Hồ sơ nhân sự HR)
1. Từ 1 nhân viên (tài khoản đã có ở phần Quản lý nhân viên), **mở hồ sơ nhân sự** của người đó.
2. Lần đầu mở hồ sơ của nhân viên chưa từng có hồ sơ HR → hệ thống **tự tạo sẵn 1 hồ sơ rỗng** gắn với nhân viên đó (xem "Cần chú ý"), rồi hiển thị thông tin nhân thân (từ tài khoản) + các ô nghiệp vụ HR còn trống.
3. Nhập/sửa các thông tin HR (ngày vào công ty, ngày lên chính thức, ngày nghỉ việc, nguồn tuyển dụng, bí danh, Facebook, quê quán) → **Lưu**.
4. Sang tab **ảnh nhân sự** để xem/thêm/xóa ảnh; tab **Giấy tờ liên quan / Quyết định bổ nhiệm** để thêm loại giấy tờ, đính kèm file, hoặc xóa giấy tờ.

### Cần chú ý (Hồ sơ nhân sự HR)
- **Họ tên & nhân thân không sửa ở đây**: form HR chỉ chỉnh các field nghiệp vụ HR. Họ tên/giới tính/email/SĐT… thuộc tài khoản nhân viên, muốn đổi vào màn **Quản lý nhân viên** (phần trên).
- **Mở hồ sơ lần đầu sẽ tạo sẵn 1 bản ghi hồ sơ rỗng** cho nhân viên (kể cả khi chỉ xem). Đây là hành vi hiện tại của hệ thống — chỉ là lưu ý, không ảnh hưởng người dùng thường (lưu ý kỹ thuật ghi ở dev.md).
- **Ràng buộc ngày**: ngày nghỉ việc phải **sau** ngày vào công ty; ngày lên chính thức phải **sau** ngày vào công ty — nhập ngược sẽ báo lỗi khi lưu.
- **Khi lưu hồ sơ, danh sách giấy tờ được đồng bộ theo đúng những gì đang hiển thị**: giấy tờ bị bỏ khỏi danh sách lúc lưu sẽ **bị xóa**. Muốn giữ thì đừng bỏ khỏi danh sách trước khi lưu.
- **Quyền nút "thêm/xóa giấy tờ" hiện chưa chuẩn** (dùng nhầm chung 1 khóa quyền với "xóa ảnh") — về lý thuyết ai mở được hồ sơ thì thao tác được giấy tờ — cần product/đội bảo mật xác nhận (xem dev.md).
