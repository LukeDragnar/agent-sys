# `FunctionCommon.ConvertToInt` nuốt số thập phân về 0 (im lặng)

> Sự cố phát hiện 2026-07-28 khi soi payload gửi Viettelpost (B262 · BigSun). **Lỗi còn sống**, chưa fix.

## Hiện tượng

Đơn `BS00100000036` gửi sang VTP có:
- header `"PRODUCT_PRICE": 0` — giá trị hàng khai = 0, dù `SuDungBaoHiem = 1`
- dòng `NANO AG+` `"PRODUCT_PRICE": 0` — trong khi DB `DonGia = 38.095,24`
- hai dòng còn lại (590000, 368000) ra đúng

## Nguyên nhân

`DeliveryConnect.Service/Generated/Common/FunctionCommon.cs:53-65`:

```csharp
public static int ConvertToInt(object? data, int valDefault = 0)
{
    try { if (data is null) return valDefault; return int.Parse(data.ToString()); }
    catch { return valDefault; }   // ← số có phần thập phân rơi vào đây
}
```

`int.Parse("38095.24")` ném `FormatException` → `catch` trả **0**, không log, không báo. Số nguyên thì parse trót lọt nên lỗi chỉ lộ ở đơn có giá lẻ.

Ráp lại đúng con số trong log:

| Chỗ | Giá trị vào | Ra | Log VTP |
|---|---|---|---|
| header `PRODUCT_PRICE` | 1.954.095,**24** | **0** | `"PRODUCT_PRICE":0` |
| `LIST_ITEM` NANO AG+ | 38.095,**24** | **0** | `"PRODUCT_PRICE":0` |
| `LIST_ITEM` 2 dòng kia | 590000 / 368000 | đúng | ✓ |

Giá lẻ đến từ **giá bóc VAT**: CRM hiển thị 40.000 với VAT 5% → `DonGia = 40.000/1,05 = 38.095,24`. Nghĩa là **mọi SP có VAT đều có nguy cơ ra số lẻ** → dính lỗi này.

## Bán kính ảnh hưởng

`ConvertToInt` dùng khắp `DeliveryConnect.Service/Generated/Partner/*` để map tiền/khối lượng sang hãng — không riêng VTP:
`VtpService` (`PRODUCT_PRICE` header + từng dòng), `BestService.ProductPrice`, `BoxMeService.total_amount`, `GhtkService` giá trị bảo hiểm, `GhnService`, `SuperShipService`... Một dòng SP giá lẻ là cả field về 0.

## Cách xử (khi được duyệt sửa)

- **Không** sửa `int.Parse` thành `int.Parse` + `CultureInfo` — vấn đề là phần thập phân, không phải dấu phân cách.
- Hướng đúng: làm tròn trước khi ép (`Math.Round` rồi `Convert.ToInt32`), hoặc dùng sẵn `ConvertToLong_Round` như bên SCRM dùng cho `TotalAmount`/`Cod`.
- Sửa ở `FunctionCommon` = tận gốc nhưng **bán kính rất rộng** (toàn bộ partner service) → phải trace kỹ. Sửa ở từng chỗ gọi = an toàn hơn nhưng sót.
- Dù chọn hướng nào: **đừng để `catch` nuốt im lặng** — sai số tiền mà không có dấu vết là thứ khiến bug này sống lâu.

## Bài học chung

`try/catch → return default` trên hàm convert tiền là **bẫy câm**: input hỏng biến thành 0 hợp lệ, đi thẳng sang đối tác, không ai biết cho tới khi khách khiếu nại. Gặp field tiền ra 0 bất thường → nghi hàm convert trước khi nghi nghiệp vụ.
