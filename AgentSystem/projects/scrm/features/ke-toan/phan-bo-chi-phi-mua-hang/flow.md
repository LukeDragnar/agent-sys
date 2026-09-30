# Flow / Test — Chứng từ mua dịch vụ & Phân bổ chi phí mua hàng

> Luồng nghiệp vụ + case test cho landed cost. Registry: `registry/ke-toan/ChungTuMuaDichVuController.md`.

## Luồng chính
1. **Lập chứng từ mua dịch vụ** → trạng thái **Nháp (0)**. Mã tự sinh prefix **MDV** nếu để trống. `TongTien = Σ(TongTienHang − chiết khấu + thuế GTGT)`.
2. **Hạch toán** (đẩy sang **Đã hạch toán (1)**) → sinh phiếu chi (nếu thanh toán ngay) + công nợ NCC + bút toán + đồng bộ MISA. `ConLaiPhanBo = TongTien − DaPhanBo`.
3. **Phân bổ** (form Phiếu mua hàng): chọn chứng từ khả dụng → nhập số tiền → chọn phương thức trải dòng SP → lưu. Cập nhật `DaPhanBo/ConLaiPhanBo` chứng từ + `PhieuMuaHang.TongChiPhiMuaHang` + dòng SP.
4. **Hoàn thành phiếu mua** → chi phí đã phân bổ vào giá vốn khóa lại (Rule E chặn sửa phân bổ sau đó).

## Trạng thái chứng từ (TrangThaiChungTuMuaDichVu)
- **Nhap = 0** · **DaHachToan = 1** · **DaHuy = 2**.
- Tình trạng phân bổ KHÔNG phải trạng thái riêng — suy ra: `DaPhanBo = 0` (chưa phân bổ) / `0 < DaPhanBo < TongTien` (phân bổ 1 phần) / `DaPhanBo = TongTien` (hết).

## Case test
| # | Tình huống | Kỳ vọng |
|---|---|---|
| 1 | Lập chứng từ, để trống mã | Mã tự sinh prefix MDV, trạng thái Nháp |
| 2 | Sửa chứng từ đã có DaPhanBo>0 | Chặn "Chứng từ đã được phân bổ, không thể sửa" |
| 3 | Sửa chứng từ đã hạch toán | Chặn "Chứng từ đã hoàn thành, không thể sửa" |
| 4 | Xóa chứng từ đã hạch toán | Chặn (chỉ Hủy được) |
| 5 | Hủy chứng từ | Trạng thái Đã hủy + bút toán bị thu hồi |
| 6 | Danh sách chứng từ khả dụng khi chứng từ còn Nháp | Không hiện (chỉ hiện DaHachToan + ConLaiPhanBo>0) |
| 7 | Phân bổ tổng vượt TongTien chứng từ | Chặn "không được lớn hơn giá trị còn lại... (còn lại {số})" |
| 8 | 1 chứng từ 1.000.000đ phân bổ 600k phiếu A + 400k phiếu B | OK, DaPhanBo=1.000.000, ConLaiPhanBo=0 |
| 9 | Phân bổ theo đơn giá, 2 dòng SP | Chi phí trải theo GiaNhap×SoLuong, sai số dồn dòng cuối |
| 10 | Phân bổ tự nhập, Σ dòng ≠ tổng phân bổ | Chặn "Tổng chi phí tự nhập không khớp tổng phân bổ" (dung sai 1đ) |
| 11 | Phân bổ khi phiếu mua đã Hoàn thành | Chặn "Phiếu mua hàng đã hoàn thành, không thể phân bổ chi phí" (Rule E) |
| 12 | Xóa phân bổ khi phiếu chưa hoàn thành | Hoàn DaPhanBo chứng từ + tính lại TongChiPhiMuaHang + dòng SP |
| 13 | 2 request phân bổ song song cùng chứng từ đẩy vượt | Re-validate trong transaction chặn, rollback |
| 14 | Đổi phân bổ của chính phiếu này (đã có mapping cũ) | Mapping cũ bị thay, phanBoPhieuKhac = DaPhanBo − oldThisPhieu |

## Edge / lưu ý
- **Phân bổ trong luồng Lưu phiếu mua hàng** dùng `boQuaRuleE=true` → cho phép phân bổ dù logic Rule E, vì đó là 1 phần của save.
- **Chi phí nhập tay giữ nguyên** khi lưu phiếu không có chứng từ phân bổ (fix `325f522ec6`) — `HoanPhanBoKhiXoaPhieu` không đụng dòng SP trong trường hợp này.
