using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace SqlCompareSuite.App.Views;

/// <summary>
/// Render SQL definition vào RichTextBox với:
///  - Tô nền dòng khác biệt (so theo từng dòng với bên kia).
///  - Syntax-highlight nhẹ: keyword (xanh), kiểu dữ liệu (teal), chuỗi 'x' (đỏ), comment (xanh lá).
///
/// THÊM (Redgate-style): RenderSideBySide() căn thẳng hàng 2 bên bằng LCS theo dòng,
/// tô nền hồng #FCE3EC cho dòng khác biệt, dựng gutter mũi tên ở giữa, đồng bộ scroll.
/// API cũ Render() GIỮ NGUYÊN signature (vẫn được code khác gọi).
/// </summary>
public static class SqlDiffRenderer
{
    // Màu nền dòng khác biệt (API cũ Render dùng — GIỮ NGUYÊN).
    private static readonly Brush RemovedBg = new SolidColorBrush(Color.FromRgb(0xFF, 0xEC, 0xEC)); // chỉ ở trái
    private static readonly Brush AddedBg = new SolidColorBrush(Color.FromRgb(0xEB, 0xFF, 0xEC));   // chỉ ở phải

    // Màu Redgate-style cho side-by-side căn thẳng hàng.
    private static readonly Brush DiffBg = new SolidColorBrush(Color.FromRgb(0xFC, 0xE3, 0xEC));     // dòng khác biệt (cả 2 bên)
    private static readonly Brush StrongDiffBg = new SolidColorBrush(Color.FromRgb(0xF2, 0xA0, 0xBE)); // ĐOẠN khác thực sự trong dòng
    private static readonly Brush PadBg = new SolidColorBrush(Color.FromRgb(0xF3, 0xF3, 0xF3));      // dòng padding (trống)
    private static readonly Brush ArrowBrush = new SolidColorBrush(Color.FromRgb(0xE9, 0x7A, 0xA6)); // mũi tên gutter (dòng đổi)
    private static readonly Brush DeleteBrush = new SolidColorBrush(Color.FromRgb(0xD0, 0x21, 0x21)); // ✗ đỏ (dòng chỉ 1 bên)

    // Màu token.
    private static readonly Brush KeywordBrush = new SolidColorBrush(Color.FromRgb(0x00, 0x00, 0xFF));
    private static readonly Brush TypeBrush = new SolidColorBrush(Color.FromRgb(0x00, 0x80, 0x80));
    private static readonly Brush StringBrush = new SolidColorBrush(Color.FromRgb(0xA3, 0x15, 0x15));
    private static readonly Brush CommentBrush = new SolidColorBrush(Color.FromRgb(0x00, 0x80, 0x00));

    private const double LineFontSize = 12;
    private static readonly FontFamily MonoFont = new("Consolas");

    private static readonly HashSet<string> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "CREATE","ALTER","DROP","TABLE","VIEW","PROCEDURE","PROC","FUNCTION","TRIGGER",
        "NOT","NULL","DEFAULT","CONSTRAINT","COLLATE","PRIMARY","KEY","FOREIGN","REFERENCES",
        "UNIQUE","CHECK","INDEX","CLUSTERED","NONCLUSTERED","ON","AS","BEGIN","END","RETURN",
        "RETURNS","SELECT","FROM","WHERE","JOIN","INNER","LEFT","RIGHT","OUTER","GROUP","BY",
        "ORDER","HAVING","INSERT","INTO","VALUES","UPDATE","SET","DELETE","DECLARE","SET",
        "IDENTITY","WITH","ADD","COLUMN","AND","OR","IN","EXISTS","CASE","WHEN","THEN","ELSE",
        "GO","UNION","ALL","DISTINCT","TOP","ASC","DESC","EXEC","EXECUTE","GRANT","DENY","REVOKE",
    };

    private static readonly HashSet<string> Types = new(StringComparer.OrdinalIgnoreCase)
    {
        "INT","BIGINT","SMALLINT","TINYINT","BIT","DECIMAL","NUMERIC","MONEY","SMALLMONEY",
        "FLOAT","REAL","DATE","DATETIME","DATETIME2","DATETIMEOFFSET","SMALLDATETIME","TIME",
        "CHAR","VARCHAR","TEXT","NCHAR","NVARCHAR","NTEXT","BINARY","VARBINARY","IMAGE",
        "UNIQUEIDENTIFIER","XML","SQL_VARIANT","ROWVERSION","TIMESTAMP","GEOGRAPHY","GEOMETRY",
    };

    // Tách token: comment block, comment dòng, chuỗi, định danh, số, từ.
    private static readonly Regex Tokenizer = new(
        @"(/\*.*?\*/)|(--[^\r\n]*)|('(?:''|[^'])*')|(\b[A-Za-z_][A-Za-z0-9_]*\b)|(\s+)|(.)",
        RegexOptions.Compiled | RegexOptions.Singleline);

    /// <summary>
    /// (API CŨ — GIỮ NGUYÊN) Render <paramref name="text"/> vào <paramref name="rtb"/>,
    /// so từng dòng với <paramref name="otherText"/> để tô dòng khác biệt.
    /// </summary>
    public static void Render(RichTextBox rtb, string text, string otherText, bool isSourceSide)
    {
        var doc = new FlowDocument
        {
            FontFamily = MonoFont,
            FontSize = LineFontSize,
            PageWidth = 2000, // tránh wrap để giữ canh dòng
        };

        var otherLines = new HashSet<string>(SplitLines(otherText), StringComparer.Ordinal);
        Brush diffBg = isSourceSide ? RemovedBg : AddedBg;

        foreach (var line in SplitLines(text))
        {
            var para = new Paragraph { Margin = new Thickness(0) };
            // Dòng không tồn tại bên kia → tô nền khác biệt.
            if (!otherLines.Contains(line))
                para.Background = diffBg;

            HighlightLine(para, line);
            doc.Blocks.Add(para);
        }

        rtb.Document = doc;
    }

    /// <summary>
    /// (PUBLIC HELPER — tái sử dụng tokenizer) Highlight <paramref name="sql"/> theo dòng
    /// vào <paramref name="rtb"/>, không tô diff. Dùng cho preview script (Deployment Wizard).
    /// </summary>
    public static void HighlightInto(RichTextBox rtb, string sql)
    {
        var doc = new FlowDocument
        {
            FontFamily = MonoFont,
            FontSize = LineFontSize,
            PageWidth = 2400,
        };
        foreach (var line in SplitLines(sql))
        {
            var para = new Paragraph { Margin = new Thickness(0) };
            HighlightLine(para, line);
            doc.Blocks.Add(para);
        }
        rtb.Document = doc;
    }

    /// <summary>
    /// (REDGATE-STYLE) Render so sánh side-by-side căn thẳng hàng:
    ///  - LCS theo dòng để 2 bên cùng số dòng (chèn dòng trống padding bên thiếu).
    ///  - Dòng khác biệt (changed / only-left / only-right): nền hồng #FCE3EC.
    ///  - Dòng padding: nền xám rất nhạt #F3F3F3.
    ///  - Gutter ItemsControl: mỗi dòng khác biệt hiện "➡" xanh, thẳng hàng theo line-height.
    /// </summary>
    public static List<int> RenderSideBySide(
        RichTextBox left, RichTextBox right, ItemsControl gutter, string srcDef, string tgtDef,
        double fontSize = LineFontSize)
    {
        var srcLines = new List<string>(SplitLines(srcDef));
        var tgtLines = new List<string>(SplitLines(tgtDef));

        // Align 2 bên bằng LCS → danh sách cặp (leftLine?, rightLine?).
        List<AlignedLine> aligned = AlignByLcs(srcLines, tgtLines);

        var leftDoc = NewDoc(fontSize);
        var rightDoc = NewDoc(fontSize);
        var gutterItems = new List<GutterMark>();
        var diffLineIndices = new List<int>(); // chỉ số dòng khác biệt → cho Previous/Next nhảy
        double lh = LineHeightPx(fontSize);
        int lineIdx = 0;

        foreach (var al in aligned)
        {
            bool isDiff = al.Kind != LineKind.Same;
            if (isDiff) diffLineIndices.Add(lineIdx);
            lineIdx++;

            // Cặp dòng đổi (cả 2 bên có) → tính đoạn khác thực sự để bôi đậm.
            int ls = -1, le = -1, rs = -1, re = -1;
            if (isDiff && al.Left != null && al.Right != null)
                ComputeInlineDiff(al.Left, al.Right, out ls, out le, out rs, out re);

            // ----- LEFT -----
            // BlockLineHeight: ép mỗi dòng cao ĐÚNG bằng lh để thẳng hàng tuyệt đối với gutter
            // (nếu không, line-height thực render hơi lệch lh → tích lũy lệch dần ~1 dòng ở cuối).
            var lpara = new Paragraph
            {
                Margin = new Thickness(0),
                LineHeight = lh,
                LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
            };
            if (al.Left == null) lpara.Background = PadBg;     // padding bên trái
            else if (isDiff) lpara.Background = DiffBg;        // khác biệt → nền nhạt cả dòng
            HighlightLine(lpara, al.Left ?? "", ls, le);       // đoạn khác → nền đậm
            leftDoc.Blocks.Add(lpara);

            // ----- RIGHT -----
            var rpara = new Paragraph
            {
                Margin = new Thickness(0),
                LineHeight = lh,
                LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
            };
            if (al.Right == null) rpara.Background = PadBg;
            else if (isDiff) rpara.Background = DiffBg;
            HighlightLine(rpara, al.Right ?? "", rs, re);
            rightDoc.Blocks.Add(rpara);

            // ----- GUTTER ----- dòng chỉ 1 bên (add/xóa) → ✗ đỏ; dòng đổi cả 2 bên → ➡
            string glyph = "";
            Brush gBrush = ArrowBrush;
            if (isDiff)
            {
                bool oneSide = al.Left == null || al.Right == null;
                if (oneSide) { glyph = "✗"; gBrush = DeleteBrush; }
                else { glyph = "➡"; gBrush = ArrowBrush; }
            }
            gutterItems.Add(new GutterMark { Glyph = glyph, Foreground = gBrush, LineHeight = lh });
        }

        left.Document = leftDoc;
        right.Document = rightDoc;
        gutter.ItemsSource = gutterItems;
        return diffLineIndices;
    }

    /// <summary>Cuộn 2 RichTextBox + gutter tới dòng thứ <paramref name="lineIndex"/> (Previous/Next nhảy tới change).</summary>
    public static void ScrollToLine(
        RichTextBox left, ScrollViewer? gutterScroll, RichTextBox right, int lineIndex, double fontSize)
    {
        double offset = lineIndex * LineHeightPx(fontSize);
        FindScrollViewer(left)?.ScrollToVerticalOffset(offset);
        FindScrollViewer(right)?.ScrollToVerticalOffset(offset);
        gutterScroll?.ScrollToVerticalOffset(offset);
    }

    /// <summary>Đồng bộ cuộn dọc giữa left RichTextBox, gutter ScrollViewer và right RichTextBox.</summary>
    public static void SyncScroll(RichTextBox left, ScrollViewer gutterScroll, RichTextBox right)
    {
        bool syncing = false;

        ScrollViewer? leftSv = FindScrollViewer(left);
        ScrollViewer? rightSv = FindScrollViewer(right);

        void Apply(double offset, object? exclude)
        {
            if (syncing) return;
            syncing = true;
            try
            {
                if (leftSv != null && !ReferenceEquals(leftSv, exclude)) leftSv.ScrollToVerticalOffset(offset);
                if (rightSv != null && !ReferenceEquals(rightSv, exclude)) rightSv.ScrollToVerticalOffset(offset);
                if (gutterScroll != null && !ReferenceEquals(gutterScroll, exclude)) gutterScroll.ScrollToVerticalOffset(offset);
            }
            finally { syncing = false; }
        }

        if (leftSv != null)
            leftSv.ScrollChanged += (_, e) => { if (e.VerticalChange != 0) Apply(leftSv.VerticalOffset, leftSv); };
        if (rightSv != null)
            rightSv.ScrollChanged += (_, e) => { if (e.VerticalChange != 0) Apply(rightSv.VerticalOffset, rightSv); };
        if (gutterScroll != null)
            gutterScroll.ScrollChanged += (_, e) => { if (e.VerticalChange != 0) Apply(gutterScroll.VerticalOffset, gutterScroll); };
    }

    // ========================= helpers =========================

    private static FlowDocument NewDoc(double fontSize) => new()
    {
        FontFamily = MonoFont,
        FontSize = fontSize,
        PageWidth = 2400,           // tránh wrap để giữ canh dòng
        PagePadding = new Thickness(0), // bỏ padding mặc định (~5px top) → text thẳng hàng với gutter
    };

    // Line-height theo cỡ chữ để 2 bên + gutter thẳng hàng.
    private static double LineHeightPx(double fontSize) => fontSize * 1.4;

    /// <summary>VM 1 ô gutter (mũi tên giữa 2 bên).</summary>
    public sealed class GutterMark
    {
        public string Glyph { get; set; } = "";
        public Brush Foreground { get; set; } = ArrowBrush;
        public double LineHeight { get; set; } = LineFontSize * 1.4;
    }

    private enum LineKind { Same, Changed }

    private sealed class AlignedLine
    {
        public string? Left;
        public string? Right;
        public LineKind Kind;
    }

    /// <summary>
    /// Căn 2 list dòng bằng LCS: dòng chung (Same) khớp 1-1; phần lệch → cặp left/right
    /// (Changed), bên thiếu = null (padding). Kết quả luôn cùng "độ cao" 2 bên.
    /// </summary>
    private static List<AlignedLine> AlignByLcs(List<string> a, List<string> b)
    {
        int n = a.Count, m = b.Count;
        // Bảng LCS.
        var dp = new int[n + 1, m + 1];
        for (int i = n - 1; i >= 0; i--)
            for (int j = m - 1; j >= 0; j--)
                dp[i, j] = string.Equals(a[i], b[j], StringComparison.Ordinal)
                    ? dp[i + 1, j + 1] + 1
                    : Math.Max(dp[i + 1, j], dp[i, j + 1]);

        var result = new List<AlignedLine>();
        // Hàng đợi tạm các dòng lệch để ghép cặp left/right cho đẹp.
        var pendingLeft = new List<string>();
        var pendingRight = new List<string>();

        void FlushPending()
        {
            int k = Math.Max(pendingLeft.Count, pendingRight.Count);
            for (int t = 0; t < k; t++)
            {
                result.Add(new AlignedLine
                {
                    Left = t < pendingLeft.Count ? pendingLeft[t] : null,
                    Right = t < pendingRight.Count ? pendingRight[t] : null,
                    Kind = LineKind.Changed,
                });
            }
            pendingLeft.Clear();
            pendingRight.Clear();
        }

        int x = 0, y = 0;
        while (x < n && y < m)
        {
            if (string.Equals(a[x], b[y], StringComparison.Ordinal))
            {
                FlushPending();
                result.Add(new AlignedLine { Left = a[x], Right = b[y], Kind = LineKind.Same });
                x++; y++;
            }
            else if (dp[x + 1, y] >= dp[x, y + 1])
            {
                pendingLeft.Add(a[x]); x++;
            }
            else
            {
                pendingRight.Add(b[y]); y++;
            }
        }
        while (x < n) { pendingLeft.Add(a[x]); x++; }
        while (y < m) { pendingRight.Add(b[y]); y++; }
        FlushPending();

        return result;
    }

    private static IEnumerable<string> SplitLines(string text)
        => (text ?? "").Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');

    /// <summary>Tô màu token trong 1 dòng và thêm vào paragraph.</summary>
    /// <summary>
    /// Tô màu token + (tuỳ chọn) bôi ĐẬM nền đoạn ký tự [diffStart, diffEnd) thực sự khác trong dòng.
    /// diffStart &lt; 0 → không có intra-line diff (cả dòng như nhau về mặt highlight đoạn).
    /// </summary>
    private static void HighlightLine(Paragraph para, string line, int diffStart = -1, int diffEnd = -1)
    {
        if (line.Length == 0)
        {
            para.Inlines.Add(new Run(" "));
            return;
        }

        int pos = 0;
        foreach (Match m in Tokenizer.Matches(line))
        {
            string tok = m.Value;
            Brush? brush = null;

            if (m.Groups[1].Success || m.Groups[2].Success) brush = CommentBrush;      // comment
            else if (m.Groups[3].Success) brush = StringBrush;                          // chuỗi 'x'
            else if (m.Groups[4].Success)                                               // từ
            {
                if (Keywords.Contains(tok)) brush = KeywordBrush;
                else if (Types.Contains(tok)) brush = TypeBrush;
            }

            AddRunSplit(para, tok, pos, brush, diffStart, diffEnd);
            pos += tok.Length;
        }
    }

    /// <summary>Thêm token, chia nhỏ để đoạn nằm trong [ds,de) có nền đậm + bold (đoạn khác thực sự).</summary>
    private static void AddRunSplit(Paragraph para, string tok, int startPos, Brush? fg, int ds, int de)
    {
        if (ds < 0 || de <= ds)
        {
            var r = new Run(tok);
            if (fg != null) r.Foreground = fg;
            para.Inlines.Add(r);
            return;
        }

        int end = startPos + tok.Length;
        int i = startPos;
        while (i < end)
        {
            bool inDiff = i >= ds && i < de;
            int j = i;
            while (j < end && (j >= ds && j < de) == inDiff) j++;
            var run = new Run(tok.Substring(i - startPos, j - i));
            if (fg != null) run.Foreground = fg;
            if (inDiff)
            {
                run.Background = StrongDiffBg;
                run.FontWeight = FontWeights.Bold;
            }
            para.Inlines.Add(run);
            i = j;
        }
    }

    /// <summary>Tính đoạn khác nhau giữa 2 dòng: bỏ tiền tố + hậu tố chung → [start,end) ở mỗi bên.</summary>
    private static void ComputeInlineDiff(string a, string b, out int aStart, out int aEnd, out int bStart, out int bEnd)
    {
        int n = a.Length, m = b.Length;
        int p = 0;
        while (p < n && p < m && a[p] == b[p]) p++;
        int s = 0;
        while (s < n - p && s < m - p && a[n - 1 - s] == b[m - 1 - s]) s++;
        aStart = p; aEnd = n - s;
        bStart = p; bEnd = m - s;
    }

    /// <summary>Tìm ScrollViewer bên trong template của RichTextBox (để đồng bộ scroll).</summary>
    private static ScrollViewer? FindScrollViewer(DependencyObject root)
    {
        if (root is ScrollViewer sv) return sv;
        int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(root, i);
            var found = FindScrollViewer(child);
            if (found != null) return found;
        }
        return null;
    }
}
