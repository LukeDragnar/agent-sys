# Dev — Cấu hình gói sử dụng

> Controller: `NangCapGoiSuDungController` (Core, phía đơn vị), `GoiSuDungController` (Core, QuanTriArea — quản lý gói + duyệt nâng cấp), `SmsLichSuNapTienController` (service SMS, phía đơn vị) + bản QuanTri duyệt nạp. FE module `caidat-hethong`.
>
> **INDEX để Trace = "—"** (không gán controller). Thực tế feature gom 3 controller trên (xác định qua FE `caidat-hethong` + registry). Ghi nhận: INDEX chưa map controller cho feature này.

## Màn hình & route (FE)
| Màn hình | Route | Component |
|---|---|---|
| Gói sử dụng (danh sách gói) | `caidat/goi-su-dung` | `GoisudungComponent` |
| Nâng cấp gói sử dụng | `caidat/nang-cap-goi-su-dung` | `NangcapGoisdComponent` |
| Lịch sử nạp tiền SMS + Nạp tiền | `caidat/lich-su-nap-tien-sms` | `LichSuNapTienSMSComponent` (con `nap-tien-sms`) |

- Route gốc: `app-routing.module.ts:141` (`caidat` → `CaidatHethongModule`).
- Route con: `caidat-hethong-routing.module.ts:83` (goi-su-dung), `:87` (nang-cap-goi-su-dung), `:224` (lich-su-nap-tien-sms).

## Endpoint / Controller (trace registry)

### Nâng cấp gói (phía đơn vị) — `NangCapGoiSuDungController` (Core)
> Registry: `registry/he-thong/NangCapGoiSuDungController.md`. `[TienIch("NangCapGoiSuDungController", TI_Chung)]`, kế thừa `SBBaseController` nhưng không dùng CRUD chuẩn.

| MaTinhNang | Route | Mục đích |
|---|---|---|
| `NangCapGoiSuDung_NangCapSuDung` (DV-AD-TONG, HT-CSKH) | `POST api/NangCapGoiSuDung/NangCapSuDung` | tạo yêu cầu nâng cấp/đổi gói |
| `_GetGoiSuDungHienTai` (DV-AD-*, HT-CSKH) | `GET .../GetGoiSuDungHienTai` | gói hiện tại |
| `_GetListGoiSuDung` | `GET .../GetListGoiSuDung` | gói có thể nâng cấp |
| — (login) | `GET .../GetThoiHanGoi` | ⚠ không gắn key → mọi user login gọi được |
| `_CheckSoLuotApDungMaGioiThieu` (DV-AD-TONG) | `POST .../CheckSoLuotApDungMaGioiThieu` | kiểm tra lượt mã giới thiệu |

### Quản lý gói + duyệt nâng cấp (admin hệ thống) — `GoiSuDungController` (Core, QuanTriArea)
> Registry: `registry/he-thong/GoiSuDungController.md`. Route `api/QuanTriArea/GoiSuDung`, state `[ChoNangCap, DaDuyet, HuyDuyet]`.

| MaTinhNang | Route | Mục đích |
|---|---|---|
| `GoiSuDung_ThemMoiGoiSd` / `_CapNhatGoiSd` | `POST .../ThemMoiGoiSd`, `/CapNhatGoiSd` | CRUD gói |
| `GoiSuDung_TimTheoDieuKien` | `POST .../TimTheoDieuKien` | lưới gói |
| `GoiSuDung_Xoa` | `POST .../Xoa/{id}` | xóa (check tồn tại) |
| `GoiSuDung_DanhSachNangCap` | `POST .../DanhSachNangCap` | DS yêu cầu nâng cấp |
| `GoiSuDung_DuyetYeuCauNangCap` | `POST .../DuyetYeuCauNangCap` | duyệt (gate runtime SuperUser/HT-KT/HT-AD) |
| — (login, gate code) | `GET .../HuyDuyetNangCap?idNangCap=` | hủy duyệt (key bị comment) |
| `GoiSuDung_GoiPheDuyetInfo` | `POST .../GoiPheDuyetInfo/{id}` | thông tin gói phê duyệt |

### Nạp tiền SMS (phía đơn vị) — `SmsLichSuNapTienController` (service SMS)
> Registry: `registry/marketing/SmsLichSuNapTienController.DonVi.md`. Route `api/SMSLichSuNapTien`, state `[ChuaDuyet, DaDuyet, Huy]`. Bản QuanTri trùng tên (`api/QuanTriArea/SMSLichSuNapTien`) lo duyệt → `SMSLichSuNapTienController.QuanTri.md`.

| MaTinhNang | Route | Mục đích |
|---|---|---|
| `SMSLichSuNapTien_TimTheoDieuKien` (DV-AD-TONG, HT-CSKH) | `POST api/SMSLichSuNapTien/TimTheoDieuKien` | lịch sử nạp |
| `SMSLichSuNapTien_NapTienSMS` (DV-AD-TONG, HT-CSKH) | `POST api/SMSLichSuNapTien/NapTienSMS` | tạo yêu cầu nạp (`SMNapTienDto`, chờ duyệt) |

## Entities
- **`GoiSuDung`** — định nghĩa gói.
- **`NangCapGoiSuDung`** — yêu cầu nâng cấp/đổi gói (+ tham chiếu MaGioiThieu khi check mã).
- **`SMSLichSuNapTien`** — yêu cầu nạp tiền SMS + trạng thái duyệt. Sau duyệt → cộng `AccountSms`.

## Luồng code
- **Nâng cấp:** controller → `INangCapGoiSuDungService.NangCapSuDung` (map DTO→entity, AutoMapper). `CheckSoLuotApDungMaGioiThieu` tham chiếu `MaGioiThieu`.
- **Duyệt:** `GoiSuDungController` → `INangCapGoiSuDungService` (list/duyệt/hủy/info) + `IGoiSuDungService` (CRUD gói). Gate quyền duyệt/hủy bằng if trong code (`CurrentUser.IsSuperUser || IsHeThongKeToan() || IsAdminHeThong()`).
- **Nạp SMS:** controller → `ISmsLichSuNapTienService` (tạo yêu cầu); bản QuanTri duyệt → cộng số dư AccountSms.

## Bug-prone / lưu ý
- **`NangCapGoiSuDungController.cs:61` `GetThoiHanGoi`** không gắn `SBAuthorize` key → mọi user đăng nhập gọi được, không gate role admin như các action khác.
- **`GoiSuDungController.cs:97-99` `HuyDuyetNangCap`** chỉ `[SBAuthorize()]` (key bị comment), gate quyền hoàn toàn bằng if trong code — dễ sót nếu refactor bỏ if.
- **Check quyền duyệt/hủy lặp** ở `DuyetYeuCauNangCap` và `HuyDuyetNangCap` cùng pattern → nên gom helper.
- **Nhãn key nạp SMS sai mô tả:** `NapTienSMS` ghi nhầm "Tìm kiếm lịch sử nạp tiền sms" (`SmsLichSuNapTienController.cs:40`) — chỉ là nhãn, không ảnh hưởng hành vi.
- **`[SBAuthorize]` controller-level của SmsLichSuNapTien bị comment** (`cs:11`) → chỉ còn auth ở method.
- **Trùng tên class** SMSLichSuNapTien (đơn vị vs QuanTri) phân biệt qua route `api/` vs `api/QuanTriArea/`.

## File source (path:line)
- BE nâng cấp (đơn vị): `D:\PROJECT\Git\_SourceForCode\Group2\scrm\...\Controllers\NangCapGoiSuDungController.cs:61` (GetThoiHanGoi không key).
- BE gói + duyệt: `...\Areas\...\GoiSuDungController.cs:97-99` (HuyDuyetNangCap gate code).
- BE nạp SMS: `...\SMS\...\Controllers\SmsLichSuNapTienController.cs:40` (nhãn sai), `:11` (auth controller comment).
- FE: `D:\PROJECT\Git\_SourceForCode\Group2\scrm_frontend\src\app\pages\caidat-hethong\nangcap-goisd\nangcap-goisd.component.ts`; `...\caidat-hethong\lich-su-nap-tien-sms\lich-su-nap-tien-sms.component.ts` (con `nap-tien-sms`).
- Registry: `projects/scrm/registry/he-thong/{NangCapGoiSuDungController, GoiSuDungController}.md`; `registry/marketing/SmsLichSuNapTienController.DonVi.md` (+ `.QuanTri.md`).
