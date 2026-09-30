# SCRM — KHUNG cẩm nang tính năng (Feature Index)

> **Đây là KHUNG (skeleton)** — danh sách tính năng toàn hệ thống, chia theo nghiệp vụ. Mục tiêu: cẩm nang cho **cả team + khách hàng**, sau này nuôi **chatbot AI**.
>
> **Nguồn:** trích tự động từ **bảng `Page` (registry menu thật, 738 màn hình active, 18 nhóm)** trên staging — gộp màn → feature theo mục tiêu người dùng. Ngày dựng: 2026-06-20.
>
> **Trạng thái:** 🟡 Pass A (rổ "có menu") xong. Còn:
> - ⏳ **Pass B (rổ "nền/không menu"):** job, webhook nhận đơn TMDT, đồng bộ, tự áp KM, gửi SMS/Zalo định kỳ... — quét BE bổ sung.
> - 🔴 **Cần người (product/CSKH):** cột "Khách hay gọi" (alias), gói/quyền, FAQ từ ticket thật — AI mới draft.
> - ✅ **Trace (cột nội bộ)** = `MaTinhNang`≈controller để dev/AI lần ra code. **KHÔNG hiển thị cho khách.**
>
> Mỗi feature sau sẽ thành 1 folder `<module>/<feature>/` chứa `summarize.md` · `faq.md` · `flow.md` · `dev.md` (xem `website/` mẫu).

---

## ⚠️ Lưu ý đọc khung
- **`Mobile`** = bản app di động, **lặp lại** tính năng web (Marketing/Sales/Kho...) → khi dựng cẩm nang nên trỏ về feature web, không viết lại.
- **`Hệ thống`** = phần **super-admin/nội bộ** (phân quyền, đơn vị, hợp đồng, danh mục gốc) → phần lớn KHÔNG cho khách hàng cuối, chỉ admin SaaS.
- **`Báo cáo`** (module riêng) **trùng** nhiều với báo cáo nằm trong từng module → cần gộp/khử trùng khi điền chi tiết.
- Một số feature **dùng chung controller** giữa module (vd tổng đài, contact) → bình thường, không phải lỗi.

---

## Mobile (app di động — mirror web)
| Feature | Khách hay gọi | Là gì | Trace (nội bộ) |
|---|---|---|---|
| Quản lý Leads & Marketing | Lead, nguồn khách, dashboard MKT | Theo dõi/phân bổ lead từ kênh marketing | SanPhamLanding, HoSoKhachHang_LeadMKT, MarketingDashboard, Voucher |
| Danh sách khách hàng | Khách, contact, CRM | Tra thông tin KH + lịch sử giao dịch | KhachHang_SearchKhachHang360, Contact |
| Đơn hàng & Bán hàng | Đơn, order, POS | Tạo/xử lý đơn + thanh toán tại quầy | DonHangLogistic, DonHang_TaoDon |
| Lịch hẹn & Booking | Đặt lịch, appointment | Lên lịch phục vụ + nhắc | DatLich, GetListContactDatLichChoDuyet |
| ChatHub & tương tác | Chat, Facebook, messenger | Quản lý tin nhắn Facebook | FBUserFanpages_FacebookChat |
| Kiểm tra tồn kho | Tồn kho, stock | Xem tồn theo chi nhánh | KhoChiTiet_GetTonKhoSanPham |
| Kho vận | Thủ kho, vận đơn | Phiếu nhập xuất + vận đơn + ghi hình đóng hàng | ThuKhoTacNghiep, Kho |
| Sản phẩm & Combo | Sản phẩm, combo | Danh mục SP/DV/combo/thẻ | SanPham, ComBoSpa, DichVu, TheDichVu |
| Báo cáo kho | Báo cáo tồn, xuất nhập | Thống kê nhập xuất, dự báo hết hàng | ReportWarehouse* |
| Thu ngân & bàn giao ca | Thu ngân, giao ca | Giao dịch + bàn giao ca + doanh số | BienBanBanGiaoCa, OrderReportCashiers |
| Kế toán & đối soát | Đối soát, thu chi | Thu chi + đối soát đơn | ThuChi, KeToanTacNghiep |
| Nhân viên & chấm công | Chấm công, wifi | Hồ sơ NV + chấm công + quét wifi | User_TimTheoDieuKienNhanVien, GioLamViecNhanVien |
| Báo cáo (chấm công/hoa hồng/cửa hàng/online/booking/CEO) | Báo cáo | Bộ báo cáo bản mobile (nhiều loại) | BaoCaoOnline*, BaoCaoCuaHang*, BC_HoaHong* |
| Trợ lý AI (G.R.A.I.N) | AI, trợ lý, chatbot | Placeholder trợ lý AI (chưa phát triển) | — |
| Quyền User & Admin | Phân quyền | Cấu hình quyền theo user/cấp | up-AdminChiNhanh |

> Ghi chú: Mobile mirror web; gộp các báo cáo mobile thành 1 dòng. Trợ lý AI là placeholder.

## Marketing
| Feature | Khách hay gọi | Là gì | Trace (nội bộ) |
|---|---|---|---|
| Quản lý Lead | Khách tiềm năng | Thu thập/phân loại liên hệ từ nguồn MKT | HoSoKhachHang_LeadMKT, MarketingDashboard, Seeding |
| Báo cáo Marketing | Báo cáo MKT | Phân tích kết quả MKT theo SP/kênh/nguồn | MarketingDashboard, Report_BaoCaoMktTheoSanPham |
| Quản lý nguồn dữ liệu | Kênh tiếp nhận, landing | Cấu hình kênh + nguồn dữ liệu KH | SanPhamLanding, DMKenhTiepNhan |
| Kết nối Facebook | Fanpage, bài đăng | Tích hợp fanpage + bài đăng + ngân sách ads | FBFanpage, FBPost, AdsAccount |
| Kết nối Shopee | Gian hàng Shopee | Đồng bộ gian hàng/SP/đơn Shopee | ShopeeConnect |
| Kết nối TikTok | Gian hàng TikTok | Đồng bộ gian hàng/SP/đơn TikTok | TiktokConnect |
| Kết nối Lazada | Gian hàng Lazada | Đồng bộ gian hàng/SP Lazada (chưa hoàn thiện) | — |
| Gửi SMS & Zalo OA | Tin nhắn KH | Gửi/quản lý chiến dịch SMS/Zalo | SmsSend, AccountSms, PatternSms |
| Chiến dịch khách hàng | Campaign đối tượng | Lọc + chạy chiến dịch nhóm KH | CustomerConditionFilter |
| Quản lý khuyến mãi | Khuyến mãi, discount | Tạo/quản lý chương trình KM | KhuyenMai_TimTheoDieuKien |
| Quản lý voucher | Mã giảm giá, coupon | Phát hành/theo dõi voucher | Voucher_TimTheoDieuKien |
| Người giới thiệu | CTV giới thiệu | Tra mã giới thiệu/đơn vị/hợp đồng | NguonGioiThieu, MaGioiThieu |
| Hoa hồng giới thiệu | CTV, hoa hồng | CTV + nhóm hoa hồng + chiến dịch | CongTacVien, NhomHoaHongCTV |
| ChatHub Marketing | Chat Facebook | Trả tin nhắn KH trên Facebook | FBUserFanpages |
| Quản lý Website | Nội dung web | Tin tức/domain/theme/banner/chuyên gia/menu | TinTuc, Domain, Theme, Banner, ChuyenGia |
| Quản lý Subiz | Subiz chat | Cấu hình kết nối chat Subiz | ConfigChat |

## Kho
| Feature | Khách hay gọi | Là gì | Trace (nội bộ) |
|---|---|---|---|
| Quản lý vận đơn | Đăng đơn, cấu hình vận đơn | Tiếp nhận/xử lý/cấu hình yêu cầu giao hàng | ThuKhoTacNghiep, CauHinh_Order, MauKichThuocDongHang, LuotNhatKho |
| Quản lý tồn kho | Kiểm tra kho, tồn chi nhánh | Theo dõi số lượng hàng có sẵn | KhoChiTiet, CauHinh_Warehouse, Kho_TimTheoDieuKien |
| [Rà soát / Reset tồn kho & giá vốn](kho/ra-soat-reset-ton-kho/summarize.md) | Rà soát tồn, reset toàn kho, tính lại giá vốn | Dò lệch/reset/tái tạo tồn + giá vốn cả đơn vị (job nền, resumable) | RaSoatTonKho_BatDau, RaSoatTonKho_ResetToanKho, RaSoatTonKhoSession, KhoChiTietGiaVonTheoKy |
| Kiểm kho | Phiếu kiểm kho | Lập/quản lý phiếu kiểm tra tồn | PhieuKiemKho |
| Quản lý lô date & Imei | Lô date, hạn dùng, imei | Tạo/theo dõi lô hàng, hạn, imei | SinhLoDateTuDong, SanPhamImei, SinhImeiTuDong |
| Chia số care đơn | Phân bổ đơn, giao NV care | Gán đơn cho NV chăm sóc | CauHinhChiaSoCareDon, ChiaSoCareDon |
| Báo cáo kho | Báo cáo nhập xuất tồn | Báo cáo hoạt động kho | ReportWarehouse*, ChiaSoCareDonReport |
| Quản lý sản phẩm | SP, dịch vụ, combo, thẻ | Cấu hình SP/DV/combo/thẻ + danh mục | SanPham, DichVu, ComBoSpa, TheDichVu, NhanSanPham |
| Quản lý nhà cung cấp | NCC, nhóm NCC | Danh sách nhà cung cấp | NhaCungCap, NhomNhaCungCap |
| Quản lý SP chi nhánh | SP/DV/combo/thẻ chi nhánh | Cấu hình SP riêng từng chi nhánh | DichVu/ComBoSpa/SanPham...ChiNhanh |
| Kết nối đơn vị vận chuyển | Viettel, GHN, J&T... | Tích hợp 12 hãng vận chuyển + tùy chỉnh | ViettelPost/Ghn/Jnt/VnPost/SuperShip/Ems/Ahamove/Best/Njv/Spx/Ghtk/Custom |
| Lịch sử nghiệp vụ logistics | Log giao hàng | Lịch sử sự kiện/trạng thái giao hàng | NghiepVuManagement |
| Quản lý bảo hành | Xử lý bảo hành | Tiếp nhận/xử lý yêu cầu bảo hành | (chưa gate) |
| Quản lý đơn hàng hoàn | Hoàn hàng | Xử lý đơn khách trả lại | (chưa gate — xem ecommerce_connect) |

## Sales
| Feature | Khách hay gọi | Là gì | Trace (nội bộ) |
|---|---|---|---|
| Quản lý Contact | Contact, danh sách KH | Quản lý liên hệ + blacklist | Contact, ContactPhoneBlacklist |
| Quản lý đơn hàng | Đơn, order | Tạo/theo dõi đơn bán | DonHangLogistic |
| Lịch hẹn gặp khách | Lịch hẹn | Đặt + duyệt lịch hẹn | DatLich, GetListContactDatLichChoDuyet |
| Thiết lập tác nghiệp | Tác nghiệp, quy trình | Cấu hình tác vụ + danh mục tác nghiệp | DMTacNghiepCauHinh |
| Quản lý kho số floating | Kho số, database số | Quản lý kho SĐT thả nổi | KhoSo |
| Chia Lead / số | Phân bổ lead, chia số | Cấp phát số cho sale (tự động/thủ công) | CauHinhChiaSo |
| Sale DMS | Sale DMS, cửa hàng | Bán hàng + kho + tuyến + lịch trình sale | SaleDMS_* |
| Telesales | Tele | Đội telesales gọi điện | Contact |
| Tổng đài (Voice IP / Sim số / IP mới) | Tổng đài, cuộc gọi | Quản lý tổng đài + lịch sử + ghi âm (3 phiên bản) | TongDai*, MyHub* |
| ChatHub Sale | Chat Facebook | Quản lý chat KH | FBUserFanpages |
| Báo cáo Sales | Báo cáo sale, KPI | Báo cáo chia số/sale/KPI cá nhân | ContactReport, BaoCaoOnline |
| Cấu hình Sale (contact/đơn/giảm giá/ship/kho số) | Cấu hình | Bộ cấu hình cho sale | CauHinh_Contact/Order, DonHangCauHinhGiamGia... |

> Ghi chú: tổng đài có 3 phiên bản (cũ/sim số/IP mới) — dễ nhầm, cần ghi rõ phiên bản.

## Mua hàng
| Feature | Khách hay gọi | Là gì | Trace (nội bộ) |
|---|---|---|---|
| Quản lý tồn (dự báo) | Dự báo, cảnh báo hết hàng | Theo dõi tồn + dự kiến thêm hàng | KhoChiTiet_ReportTonKhoDonVi, SanPhamGioiHanTonKho |
| Quản lý đơn đặt hàng | Đặt hàng, đơn mua | Quản lý đơn đặt hàng nhập | DonDatHang |
| Quản lý đặt cọc | Cọc tiền, trả cọc | Phiếu cọc + đợt trả + SMS tự động | DepositReceipt, DepositOrderDeliveryBatch |
| Báo cáo mua hàng | Báo cáo | Thống kê mua hàng | (chưa gate) |

## Kế toán
| Feature | Khách hay gọi | Là gì | Trace (nội bộ) |
|---|---|---|---|
| Thu chi | Phiếu thu/chi/chuyển tiền | Ghi nhận dòng tiền vào/ra quỹ | ThuChi, ThuChiLyDo |
| Hóa đơn mua bán | Phiếu mua/bán, trả lại | Chứng từ mua/bán + trả lại/giảm giá | PhieuMuaHang, PhieuBanHang, PhieuTraLaiHangMua |
| Công nợ | Bù trừ nợ, quản lý nợ | Bù trừ + báo cáo phải thu/phải trả | PhieuBuTruCongNo, BaoCaoKeToan_CongNo |
| Chứng từ khác | Chứng từ, nhập xuất kho | Chuyển kho/nhập/xuất/mua dịch vụ | ChungTuKhac |
| Hóa đơn điện tử | HĐĐT | Tích hợp MeInvoice + ViettelInvoice | MeInvoice, ViettelInvoice |
| Báo cáo quỹ | Báo cáo quỹ | Số dư quỹ theo tài khoản | BaoCaoKeToan_QuyTaiKhoan |
| Tính lương | Lương nhân viên | Tính/quản lý lương | LuongNhanVien |
| Đối soát đơn online | Đối soát | Đối soát giao dịch cổng thanh toán | KeToanTacNghiep |
| Tạo hóa đơn tự động | HĐĐT tự động | Phát sinh HĐ tự động từ đơn | KeToanTacNghiep, DonHang_TaoDon |
| [Yêu cầu xuất HĐ (đơn logistic)](hoa-don-yeu-cau-xuat-logistic/summarize.md) | Cờ yêu cầu xuất HĐ | Cờ `IsYeuCauXuatHoaDon` điều khiển xuất HĐĐT + nguồn TT người mua (Config A/B, gate auto) | DonHangLogistic, DonHangInvoice |
| [Chứng từ mua dịch vụ & phân bổ chi phí mua hàng](ke-toan/phan-bo-chi-phi-mua-hang/summarize.md) | Chi phí mua hàng, phí vận chuyển vào giá vốn, landed cost | Chứng từ mua dịch vụ (MDV) tick "Là chi phí mua hàng" → phân bổ vào phiếu mua hàng → cộng giá trị nhập kho + giá vốn | ChungTuMuaDichVu, ChiPhiMuaHang, PhieuMuaHangChiPhiMuaHang |

## Cửa hàng
| Feature | Khách hay gọi | Là gì | Trace (nội bộ) |
|---|---|---|---|
| Tạo & quản lý đơn hàng | Bán hàng, tính tiền | Tạo đơn, bàn phục vụ, in bếp | DonHang_TaoDon, DonHang_InDonBepNv |
| Cấu hình giá & chiết khấu | Quản lý giá, giảm giá | Mặt hàng mặc định + giảm giá đơn | CauHinhDonHangMatHangMacDinh, DonHangCauHinhGiamGia |
| Báo cáo & thống kê | Thu ngân, doanh thu | Doanh số thu ngân + hoa hồng | OrderReportCashiers, BC_HoaHong |
| Quản lý ca làm việc | Giao ca | Biên bản giao ca | BienBanBanGiaoCa |
| Quản lý nhân viên xử lý | Xếp nhân viên | Cấu hình xếp NV theo ca | CauHinhXepNhanVien |
| Cấu hình hệ thống cửa hàng | Cài đặt, setup | Đơn chi nhánh + máy in + khu vực bàn | CauHinh_Order, DMMayIn, KhuVuc |
| Lịch hẹn khách hàng | Đặt lịch, book | Lịch hẹn + nguồn + thống kê | DatLich, NguonDatLich, BookingReport |
| Pre-Order | Đặt trước, QR | Mã QR khách đặt trước + danh sách | BangQRCode_PreOrder, DonHangPre |

## Công việc
| Feature | Khách hay gọi | Là gì | Trace (nội bộ) |
|---|---|---|---|
| Quản lý Chat (Chat Hub) | Chat với khách, phân bổ chat | Tiếp nhận/phân công chat + báo cáo hiệu suất | FBUserFanpages_FacebookChat, CauHinhChiaChat, BaoCaoChatSB |
| Quản lý công việc | Công việc, nhiệm vụ | Tạo/gán/theo dõi task + báo cáo | CongViec_TimTheoDieuKien |
| Cấu hình công việc | Thiết lập quy trình, mẫu | Mẫu quy trình + luật tự động + sơ đồ tổ chức | CongViec_TimTheoDieuKien |
| Lịch sử xóa | Khôi phục, thùng rác | Tra/khôi phục công việc đã xóa | LichSuXoa |

## Khách hàng
| Feature | Khách hay gọi | Là gì | Trace (nội bộ) |
|---|---|---|---|
| Quản lý thông tin KH | Danh sách KH 360 | Thông tin + lịch sử mua + điểm + tương tác | KhachHang_SearchKhachHang360, DMKhachHang |
| Phân loại & đánh nhãn KH | Nhãn & phân khúc KH | Gắn nhãn + nhóm + hạng KH | NhanKhachHang, KhachHangNhomKhachHang, DMHangKhachHang |
| Quản lý trạng thái KH | Trạng thái KH | Theo dõi trạng thái để chăm sóc | TrangThaiKhachHang |
| Thẻ & combo dịch vụ | Thẻ KH, gói dịch vụ | Cấp thẻ + combo + template | TheDichVuKhachHang, ComBoSpaKhachHang, DonViTemplate |
| Điểm thưởng & đổi thưởng | Điểm tích lũy, quà | Cấu hình + đổi điểm | ConfigDoiDiem |
| Nhận xét & đánh giá | Feedback KH | Ghi nhận phản hồi dịch vụ | KhachHangDanhGia |
| Báo cáo & phân tích KH | Chân dung KH | Phân tích hành vi/giá trị KH | OrderCustomerReportVisitors |

## Nhân sự
| Feature | Khách hay gọi | Là gì | Trace (nội bộ) |
|---|---|---|---|
| Quản lý chấm công | Chấm công, check-in | Giờ làm + phân ca + check-in + báo cáo | GioLamViecNhanVien, CaLamViecNhanVienNangCao, BSSID, UserIMEI |
| Quản lý hồ sơ nhân viên | Hồ sơ NV, phòng ban | Thông tin + tổ chức + phân quyền NV | User_TimTheoDieuKienNhanVien, PhongBan, NhomNhanVien |
| Thưởng phạt & hoa hồng | Thưởng, hoa hồng | Khoán hoa hồng + thưởng doanh số + thưởng phạt | ThuongPhatNhanVien, HoaHongNhanVien, ThuongDoanhSo |
| Yêu cầu & xin phép | Phiếu yêu cầu | Phiếu yêu cầu/xin phép từ NV | PhieuYeuCau_Branch |

## Báo cáo (module tổng — trùng nhiều với báo cáo trong module)
| Feature | Khách hay gọi | Là gì | Trace (nội bộ) |
|---|---|---|---|
| Báo cáo doanh số | Báo cáo bán hàng | Doanh thu theo thời gian/điểm bán/SP | BaoCaoCuaHang_OrderReport*, BaoCaoOnline_* |
| Báo cáo khách hàng | Báo cáo KH | Hành vi/phân khúc/trung thành KH | BaoCaoOnline_ReportTheoKhachHang, ReportKhachHangCuMoi |
| Báo cáo sản phẩm | Báo cáo hàng, combo | Tỷ lệ bán/tồn/hiệu suất SP | BaoCaoOnline_ReportTyLeChotDon, ReportCombo |
| Báo cáo bậc thang & KPI | BXH | Xếp hạng NV/phòng ban/nguồn MKT | BaoCaoOnline_ReportBangXepHang* |
| Báo cáo sale & lead | Báo cáo sale | Công việc/lead/tối ưu bán hàng | BaoCaoOnline_ReportCongViecSale, ReportToiUuSale |
| Báo cáo marketing | Báo cáo MKT | Kết quả campaign + lead MKT | BaoCaoOnline_ReportCongViecMkt |
| Báo cáo giao vận | Báo cáo vận chuyển | Doanh số/hiệu suất theo ĐVVC | BaoCaoOnline_ReportDoanhSoTheoGiaoVan |
| Báo cáo tổng hợp bán hàng | Báo cáo tổng quát | Tổng hợp bán hàng + tác nghiệp | BaoCaoOnline_ReportTongHopBanHang |
| Dashboard tổng quan / CEO | Báo cáo CEO | Toàn cảnh kinh doanh cho lãnh đạo | BaoCaoCeo_ReportDoanhSoTongQuan |
| Báo cáo tài chính (công nợ) | Báo cáo công nợ | Công nợ phải thu | (chưa gate) |
| Báo cáo theo nhãn / phí sàn | Báo cáo nhãn | Doanh số/phí theo nhãn/sàn TMĐT | BaoCaoCuaHang_ReportNhan |

## Sản Xuất
| Feature | Khách hay gọi | Là gì | Trace (nội bộ) |
|---|---|---|---|
| Quản lý sản xuất | Đơn hàng sản xuất | Quy trình SX: sales order → production → work order → plan | (level 1) |
| Thiết lập BOM | Định mức nguyên vật liệu | Cấu hình thành phần SP + quy trình SX | Bom Setup |
| Lịch sử sản xuất | Lịch sử đơn/lệnh/công việc | Tra lịch sử đặt hàng/lệnh SX/công việc | (level 1) |

## Tiện ích
| Feature | Khách hay gọi | Là gì | Trace (nội bộ) |
|---|---|---|---|
| Quản lý tiện ích | Danh sách tiện ích | Xem/kích hoạt tiện ích dịch vụ | (666_Tiện ích) |
| Lịch sử đăng ký tiện ích | Lịch sử dùng tiện ích | Theo dõi đăng ký/hủy tiện ích | (666_Tiện ích) |

## Cài đặt
| Feature | Khách hay gọi | Là gì | Trace (nội bộ) |
|---|---|---|---|
| Cấu hình hệ thống | Cài đặt chung | Tham số chung + bảo mật + chi nhánh + TK ngân hàng | TkNganHangHeThong |
| Quản lý quyền & tài khoản | Phân quyền | Quyền + phê duyệt thiết bị + log đăng nhập | UserDevice |
| Cấu hình gói sử dụng | Nâng cấp gói | Gói + nạp SMS + nâng cấp | — |
| Quản lý tài chính | Đối tác trả góp | Đối tác tín dụng + lịch sử domain | LichSuThayDoiDomain |
| Cấu hình thông báo | Gửi thông báo khách | Mẫu + cấu hình gửi thông báo web | — |
| Lịch sử hoạt động | Audit log | Nhật ký hoạt động/đăng nhập/tìm kiếm | LichSuTacDong, LichSuBoLoc, LichSuXemThongTin |
| Cấu hình AI | Cấu hình AI | Token + prompt cho tính năng AI | — |

## Hệ thống (super-admin / nội bộ SaaS — phần lớn KHÔNG cho khách)
| Feature | Khách hay gọi | Là gì | Trace (nội bộ) |
|---|---|---|---|
| Phân quyền & quản lý quyền | Phân quyền vai trò | Quyền truy cập theo role | Admin.Permision, Admin.Role |
| Quản lý tài khoản & người dùng | Quản lý user | Tài khoản người dùng hệ thống | Admin.User |
| Quản lý trang & giao diện | Cấu hình trang/menu | Cấu trúc trang web/mobile + quyền trang | Admin.Page, Admin.PageMobile |
| Quản lý đơn vị & hợp đồng | Chi nhánh, hợp đồng | Đơn vị + hợp đồng + tiện ích + domain | Admin.DonVi, Admin.HopDong, DonViTienIch |
| Quản lý gói & tiện ích | Gói cước, tiện ích | Gói + tiện ích + giá + KM | Admin.TienIch, Admin.GoiSuDung |
| Danh mục khách hàng (gốc) | Nhóm/hạng/trạng thái KH | Danh mục phân loại KH cấp hệ thống | Admin.DMNhomKhachHang, Admin.DMHangKhachHang |
| Danh mục sản phẩm & kho (gốc) | SP/DV/kho gốc | Danh mục SP/DV/kho cấp hệ thống | Admin.SanPham, Admin.DMKho |
| Danh mục hành chính | Tỉnh/quận/xã, địa chỉ | Dữ liệu hành chính + địa chỉ giao vận | QuocGia, TinhTP, QuanHuyen, PhuongXa |
| Danh mục đơn hàng & tác nghiệp | Loại đơn, tác nghiệp | Loại đơn + tác nghiệp + quy trình | Admin.DMDonHang, Admin.DMTacNghiep |
| Danh mục khác | Nguồn lịch, ngành nghề... | Nguồn đặt lịch/lĩnh vực/ngành/đối tác/bảo hành | Admin.NguonDatLich, Admin.NganhNghe |
| RollBack & xử lý đơn | Hoàn tác đơn | Hoàn tác/khôi phục trạng thái đơn | Admin.OrderProcess |
| Quản lý đa phương tiện | Media, tài nguyên | Quản lý ảnh/video/tài liệu | HT_Luu_DaPhuongTien |
| Quản lý cấu hình SMS | SMS hệ thống | Tài khoản + cấu hình gửi SMS đơn vị | AccountSmsUnitUsed |
| Mã giới thiệu đại lý | Referral, code giới thiệu | Mã giới thiệu + tracking đại lý | MaGioiThieu_Admin |
| Mẫu & quản lý thông báo | Template thông báo | Mẫu + quản lý thông báo hệ thống/mobile | Admin.NotiTemplate |
| Quản lý log & nhật ký | Audit, log | Lịch sử lỗi/giao hàng/ecommerce | LogDelivery, LogEcommerce |
| Đồng bộ dữ liệu | Sync data | Đồng bộ dữ liệu theo đơn vị | Admin.DongBoTheoDonVi |
| Phê duyệt giao dịch tổng đài | Phê duyệt thanh toán | Duyệt giao dịch thanh toán tổng đài | Admin.TongDaiIpDonViSA |
| Cấu hình Elasticsearch | Tìm kiếm, index | Index + phương thức search | Admin.DonViElasticSearch |
| Quản lý ngôn ngữ | Đa ngôn ngữ, i18n | Cấu hình ngôn ngữ giao diện | (internal) |
| Cấu hình chung hệ thống | Tham số hệ thống | System config + mobile dictionary + CORS | Admin.ConfigSystem, Admin.DomainCors |

---

## Bước tiếp theo (để hoàn thiện khung)
1. ⏳ **Pass B — rổ "nền/không menu":** fan-out quét BE tìm feature chạy nền (webhook nhận đơn Shopee/TikTok, job đồng bộ, tự áp KM, rollback, gửi SMS/Zalo định kỳ, NghiepVu queue...). Bổ sung vào từng module.
2. 🔴 **Người điền:** alias theo lời khách, gói/quyền, FAQ từ ticket thật.
3. **Khử trùng:** gộp module `Báo cáo` với báo cáo trong từng module; tách Mobile (mirror) ra ghi chú.
4. **Validate:** module lead duyệt từng nhóm (chia đúng/sai, sót feature nào).
5. Sau khi khung chốt → điền dần `summarize/faq/flow/dev` cho từng feature (ưu tiên feature khách hay hỏi + vùng nhiều bug).

---

## Tiến độ Pha 2 (snapshot review 2026-06-20)

> Quét toàn bộ `features/`. Mỗi feature đủ 4 file (`summarize` · `faq` · `flow` · `dev`). Snapshot tại thời điểm review — một số folder Pass-B đang được hoàn thiện (thiếu `dev.md`/`flow.md`).

| Module | Số feature folder | Đủ 4 file? | Ghi chú thiếu |
|---|---|---|---|
| marketing | 17 | ✅ phần lớn | có Pass-B `tu-dong-nhac-va-gui-dinh-ky` |
| he-thong | 21 | ✅ phần lớn | vài node nền đang thêm `dev.md` |
| kho | 16 | ⚠️ | Pass-B `dong-bo-don-tmdt-tu-dong`, `nghiep-vu-tu-dong-logistics` còn thiếu `dev`(/`flow`) |
| sales | 12 | ✅ | — |
| bao-cao | 11 | ✅ | trùng có chủ đích với báo cáo trong module |
| ke-toan | 10 | ⚠️ | Pass-B `tu-dong-hoa-don-doi-soat-nen` thiếu `dev` |
| cua-hang | 8 | ✅ | — |
| cai-dat | 7 | ✅ | — |
| khach-hang | 7 | ✅ | có `README.md` |
| cong-viec | 5 | ✅ | gồm Pass-B `thong-bao-realtime-nen` |
| mua-hang | 4 | ✅ | — |
| nhan-su | 4 | ✅ | — |
| san-xuat | 3 | ✅ | gồm `thiet-lap-bom` |
| tien-ich | 2 | ✅ | — |
| website | 1 | ⚠️ | `dat-hang-online` — có `README.md`; bản mẫu |
| mobile | 0 | — | chỉ `README.md` (mirror web, không tạo folder riêng) |

**Tổng: ~128 feature folder, ~125 đủ 4 file** (số biến động khi các node Pass-B đang được đắp tiếp).

**Khử trùng — slug trùng giữa module:** chỉ 1 cặp `bao-cao-marketing` (ở `marketing/` và `bao-cao/`). Nội dung KHÁC nhau (bản `marketing` = báo cáo MKT theo SP/nguồn; bản `bao-cao` = công việc/campaign MKT) → overlap có chủ đích, không phải lỗi (đã ghi ở mục "Lưu ý đọc khung").

**Frontmatter & bảng "Hành động làm được":** 100% `summarize.md` đã quét đều có frontmatter đủ 6 key (`alias`/`module`/`trang_thai`/`goi`/`quyen`/`phu_thuoc`) + bảng "Hành động làm được".
