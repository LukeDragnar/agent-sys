using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.Win32;
using SqlCompareSuite.Core.Data;
using SqlCompareSuite.Core.Execution;
using SqlCompareSuite.Core.Models;
using SqlCompareSuite.Core.Project;
using SqlCompareSuite.Core.Reporting;
using SqlCompareSuite.Core.Schema;
using SqlCompareSuite.Core.SourceControl;

namespace SqlCompareSuite.App.Views;

public partial class SchemaCompareView : UserControl
{
    private readonly ObservableCollection<SchemaRowVm> _rows = new();
    private ICollectionView? _rowsView;

    // Schema đọc được, giữ lại để generate script.
    private DatabaseSchema? _sourceSchema;
    private DatabaseSchema? _targetSchema;

    // Snapshot override: nếu != null thì Compare DÙNG snapshot thay vì đọc DB live.
    private DatabaseSchema? _sourceSnapshotOverride;
    private DatabaseSchema? _targetSnapshotOverride;
    private string? _sourceSnapshotName;
    private string? _targetSnapshotName;

    // Kết quả so sánh gần nhất, dùng cho Export Report.
    private SchemaComparisonResult? _lastResult;

    public SchemaCompareView()
    {
        InitializeComponent();

        _rowsView = CollectionViewSource.GetDefaultView(_rows);
        _rowsView.Filter = RowFilter;
        // Nhóm theo trạng thái (Different / Only in Source / ...).
        _rowsView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(SchemaRowVm.StatusText)));
        // Sort: Different trên, Identical DƯỚI CÙNG, rồi theo tên.
        _rowsView.SortDescriptions.Add(new SortDescription(nameof(SchemaRowVm.StatusOrder), ListSortDirection.Ascending));
        _rowsView.SortDescriptions.Add(new SortDescription(nameof(SchemaRowVm.Name), ListSortDirection.Ascending));
        DiffGrid.ItemsSource = _rowsView;

        UpdateBanner();
    }

    // Đảo chiều compare/deploy mà KHÔNG đổi vị trí 2 server (giống Redgate).
    // _reversed=false: TRÁI là Source → PHẢI là Target (deploy sửa phải).
    // _reversed=true : PHẢI là Source → TRÁI là Target (deploy sửa trái).
    private bool _reversed;

    /// <summary>DB nguồn để compare/deploy (theo chiều hiện tại).</summary>
    private DbConnectionInfo CompareSource() => _reversed ? TargetPanel.GetConnectionInfo() : SourcePanel.GetConnectionInfo();
    /// <summary>DB đích (sẽ bị sửa khi deploy) theo chiều hiện tại.</summary>
    private DbConnectionInfo CompareTarget() => _reversed ? SourcePanel.GetConnectionInfo() : TargetPanel.GetConnectionInfo();

    /// <summary>Cập nhật banner: server giữ NGUYÊN vị trí trái/phải; chỉ đổi màu vai trò + mũi tên theo chiều.</summary>
    private void UpdateBanner()
    {
        // Hai server LUÔN ở vị trí cố định: trái = SourcePanel, phải = TargetPanel.
        var left = SourcePanel.GetConnectionInfo();
        var right = TargetPanel.GetConnectionInfo();
        SourceServerLabel.Text = string.IsNullOrWhiteSpace(left.Server) ? "(no server)" : left.Server;
        SourceDbLabel.Text = string.IsNullOrWhiteSpace(left.Database) ? "(no database)" : left.Database;
        TargetServerLabel.Text = string.IsNullOrWhiteSpace(right.Server) ? "(no server)" : right.Server;
        TargetDbLabel.Text = string.IsNullOrWhiteSpace(right.Database) ? "(no database)" : right.Database;

        // Vai trò (màu) + mũi tên theo chiều. Bên TARGET = nền hồng (sẽ bị sửa), bên SOURCE = nền trắng.
        var pink = (System.Windows.Media.Brush)FindResource("RgBanner");
        var white = System.Windows.Media.Brushes.White;
        var dark = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x22, 0x22, 0x22));
        var gray = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x66, 0x66, 0x66));
        var lightPink = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFB, 0xE0, 0xEC));

        if (!_reversed)
        {
            // Trái = Source (trắng), Phải = Target (hồng) — chevron Deploy chỉ PHẢI.
            LeftSideBorder.Background = white; RightSideBorder.Background = pink;
            SourceServerLabel.Foreground = dark; SourceDbLabel.Foreground = gray;
            TargetServerLabel.Foreground = white; TargetDbLabel.Foreground = lightPink;
            DeployChevron.RenderTransform = null;
            DeployButton.Content = "Deploy  →";
            // Bố cục Redgate: [Selected] bên TRÁI, [Deploy →] bên PHẢI.
            Grid.SetColumn(SelectedPanel, 0); Grid.SetColumn(ChevronHost, 1);
            SelectedPanel.Margin = new Thickness(0, 0, 14, 0);
        }
        else
        {
            // Phải = Source (trắng), Trái = Target (hồng) — chevron Deploy chỉ TRÁI.
            LeftSideBorder.Background = pink; RightSideBorder.Background = white;
            SourceServerLabel.Foreground = white; SourceDbLabel.Foreground = lightPink;
            TargetServerLabel.Foreground = dark; TargetDbLabel.Foreground = gray;
            DeployChevron.RenderTransform = new System.Windows.Media.ScaleTransform(-1, 1);
            DeployButton.Content = "←  Deploy";
            // Bố cục Redgate đảo: [Deploy ←] bên TRÁI, [Selected] bên PHẢI.
            Grid.SetColumn(ChevronHost, 0); Grid.SetColumn(SelectedPanel, 1);
            SelectedPanel.Margin = new Thickness(14, 0, 0, 0);
        }
        if (IncludeDirArrow != null) IncludeDirArrow.Text = _reversed ? "←" : "→";
    }

    /// <summary>Mở dialog Edit project → cập nhật 2 panel kết nối + banner.</summary>
    private void EditProjectButton_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new ProjectSetupDialog(SourcePanel.GetConnectionInfo(), TargetPanel.GetConnectionInfo())
        {
            Owner = Window.GetWindow(this),
            ProjectKind = "Schema",
        };
        if (dlg.ShowDialog() == true && dlg.ResultSource != null && dlg.ResultTarget != null)
        {
            SourcePanel.SetConnectionInfo(dlg.ResultSource);
            TargetPanel.SetConnectionInfo(dlg.ResultTarget);
            _reversed = false; // user vừa định nghĩa source/target rõ ràng → chiều mặc định
            UpdateBanner();
            StatusText.Text = "Connections updated.";
        }
    }

    /// <summary>
    /// Nút Deploy trên banner = mở Deployment Wizard 3 bước (Redgate-style).
    /// Sinh script từ các object Include=true qua GenerateSyncScriptWithWarnings,
    /// rồi truyền vào wizard. Schema không hỗ trợ backup → makeBackupScript=null.
    /// </summary>
    private async void DeployButton_Click(object sender, RoutedEventArgs e)
    {
        if (_sourceSchema == null || _targetSchema == null)
        {
            MessageBox.Show("Run Compare first.", "No comparison", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        List<SchemaDiffItem> selected = _rows.Where(r => r.Include).Select(r => r.Item).ToList();
        if (selected.Count == 0)
        {
            MessageBox.Show("No objects selected (Include).", "Nothing to deploy",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        DatabaseSchema src = _sourceSchema;
        DatabaseSchema tgt = _targetSchema;
        bool redgateWrap = WrapInTransactionChk.IsChecked == true;
        var scriptOptions = new SchemaScriptOptions
        {
            WrapInTransaction = false, // sinh raw; wrapper Redgate tự bọc transaction nếu bật
            IncludeDrops = IncludeDropsChk.IsChecked == true,
        };

        SetBusy(true, "Preparing deployment...");
        SyncScriptResult result;
        string deployScript;
        try
        {
            result = await Task.Run(() =>
                new SchemaScriptGenerator().GenerateSyncScriptWithWarnings(src, tgt, selected, scriptOptions));
            deployScript = WrapDeployScript(result.Script, redgateWrap);
            // Đồng bộ ô script cũ để các nút Copy/Save/Execute cũ vẫn dùng được.
            ScriptBox.Text = deployScript;
            ShowWarnings(result.Warnings);
        }
        catch (System.Exception ex)
        {
            MessageBox.Show(ex.Message, "Generate failed", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText.Text = "Failed.";
            SetBusy(false, null);
            return;
        }
        finally
        {
            SetBusy(false, null);
        }

        DbConnectionInfo target = CompareTarget();
        if (!Validate(target, "Target")) return;

        string summary = SummaryText.Text;
        var objects = selected.Select(i => $"{i.TypeText}: {i.Key}  [{i.StatusText}]").ToList();

        var wizard = new DeploymentWizard(
            target,
            deployScript,
            result.Warnings,
            summary,
            objects,
            makeBackupScript: null) // Schema không hỗ trợ backup
        {
            Owner = Window.GetWindow(this),
        };
        wizard.ShowDialog();

        if (wizard.Deployed)
            StatusText.Text = "Deployed via wizard.";
        if (wizard.ShouldRecompare)
            CompareButton_Click(sender, e);
    }

    /// <summary>Filter tab (RadioButton) → đẩy SelectedIndex vào FilterCombo cũ (giữ logic filter).</summary>
    private void FilterTab_Checked(object sender, RoutedEventArgs e)
    {
        if (FilterCombo == null) return;
        if (sender is System.Windows.Controls.RadioButton rb &&
            int.TryParse(rb.Tag?.ToString(), out int idx))
        {
            FilterCombo.SelectedIndex = idx;
        }
    }

    private string _search = "";

    private bool RowFilter(object obj)
    {
        if (obj is not SchemaRowVm vm) return false;
        // Lọc theo ô tìm kiếm (tên object) trước.
        if (_search.Length > 0 && vm.Name.IndexOf(_search, System.StringComparison.OrdinalIgnoreCase) < 0)
            return false;
        return (FilterCombo.SelectedIndex) switch
        {
            1 => vm.Status == DiffStatus.Different,
            2 => vm.Status == DiffStatus.OnlyInSource,
            3 => vm.Status == DiffStatus.OnlyInTarget,
            4 => vm.Status == DiffStatus.Identical,
            _ => true, // All
        };
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _search = SearchBox.Text?.Trim() ?? "";
        _rowsView?.Refresh();
    }

    private void FilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _rowsView?.Refresh();
    }

    // ====================== Find trong định nghĩa (panel diff, bên target) ======================
    private int _findPos = 0;
    // Các đoạn đang tô vàng (lưu kèm nền cũ để khôi phục, tránh xoá mất nền hồng của diff).
    private readonly System.Collections.Generic.List<(System.Windows.Documents.TextRange Range, object OldBg)> _findHighlights = new();
    private static readonly System.Windows.Media.Brush FindHighlightBrush =
        new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0xF1, 0x76)); // vàng

    private void FindBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key != System.Windows.Input.Key.Enter) return;
        bool back = (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Shift) != 0;
        DoFind(forward: !back);
        e.Handled = true;
    }

    /// <summary>Gõ chữ trong ô Find → tô vàng TẤT CẢ match + nhảy tới match đầu.</summary>
    private void FindBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        string q = FindBox.Text ?? "";
        _findPos = 0;
        if (q.Length == 0)
        {
            ClearFindHighlights();
            FindStatus.Text = "";
            var p = SideTargetDef.Document.ContentStart;
            SideTargetDef.Selection.Select(p, p);
            return;
        }
        HighlightAllMatches(SideTargetDef, q);
        DoFind(true);
    }

    /// <summary>Tô vàng mọi chỗ khớp trong định nghĩa target (giữ nguyên nền cũ để khôi phục).</summary>
    private void HighlightAllMatches(RichTextBox rtb, string q)
    {
        ClearFindHighlights();
        if (string.IsNullOrEmpty(q)) return;
        string text = GetFlatText(rtb.Document);
        var cmp = System.StringComparison.OrdinalIgnoreCase;
        for (int i = 0; (i = text.IndexOf(q, i, cmp)) >= 0; i += q.Length)
        {
            var s = NavigateToOffset(rtb.Document, i);
            var en = NavigateToOffset(rtb.Document, i + q.Length);
            var range = new System.Windows.Documents.TextRange(s, en);
            object old = range.GetPropertyValue(System.Windows.Documents.TextElement.BackgroundProperty);
            try { range.ApplyPropertyValue(System.Windows.Documents.TextElement.BackgroundProperty, FindHighlightBrush); }
            catch { continue; }
            _findHighlights.Add((range, old));
        }
    }

    /// <summary>Bỏ tô vàng, trả nền cũ (giữ nền hồng diff).</summary>
    private void ClearFindHighlights()
    {
        foreach (var (range, old) in _findHighlights)
        {
            try
            {
                range.ApplyPropertyValue(System.Windows.Documents.TextElement.BackgroundProperty,
                    old == System.Windows.DependencyProperty.UnsetValue ? null : old);
            }
            catch { /* range stale sau khi reload document → bỏ qua */ }
        }
        _findHighlights.Clear();
    }

    private void FindNext_Click(object sender, RoutedEventArgs e) => DoFind(true);
    private void FindPrev_Click(object sender, RoutedEventArgs e) => DoFind(false);

    private void FindClose_Click(object sender, RoutedEventArgs e)
    {
        ClearFindHighlights();
        FindBox.Text = "";
        FindStatus.Text = "";
        _findPos = 0;
        var p = SideTargetDef.Document.ContentStart;
        SideTargetDef.Selection.Select(p, p);
    }

    private void DoFind(bool forward)
    {
        string q = FindBox.Text ?? "";
        if (q.Length == 0) { FindStatus.Text = ""; return; }

        var rtb = SideTargetDef;
        string text = GetFlatText(rtb.Document);
        var cmp = System.StringComparison.OrdinalIgnoreCase;
        if (text.Length == 0) { FindStatus.Text = "0/0"; return; }

        int total = 0; for (int i = 0; (i = text.IndexOf(q, i, cmp)) >= 0; i += q.Length) total++;
        if (total == 0) { FindStatus.Text = "0/0"; ClearFindHighlights(); return; }
        if (_findHighlights.Count == 0) HighlightAllMatches(rtb, q); // đảm bảo đã tô vàng

        int idx;
        if (forward)
        {
            idx = text.IndexOf(q, System.Math.Min(_findPos, text.Length), cmp);
            if (idx < 0) idx = text.IndexOf(q, 0, cmp); // wrap
        }
        else
        {
            int from = System.Math.Min(System.Math.Max(0, _findPos - q.Length - 1), text.Length - 1);
            idx = text.LastIndexOf(q, from, cmp);
            if (idx < 0) idx = text.LastIndexOf(q, text.Length - 1, cmp); // wrap
        }
        if (idx < 0) { FindStatus.Text = $"0/{total}"; return; }
        _findPos = forward ? idx + q.Length : idx;

        int ord = 0; for (int i = 0; (i = text.IndexOf(q, i, cmp)) >= 0 && i <= idx; i += q.Length) { ord++; if (i == idx) break; }
        FindStatus.Text = $"{ord}/{total}";
        SelectAndScroll(rtb, idx, q.Length);
    }

    private static string GetFlatText(System.Windows.Documents.FlowDocument doc)
    {
        var sb = new System.Text.StringBuilder();
        var p = doc.ContentStart;
        while (p != null)
        {
            if (p.GetPointerContext(System.Windows.Documents.LogicalDirection.Forward) == System.Windows.Documents.TextPointerContext.Text)
                sb.Append(p.GetTextInRun(System.Windows.Documents.LogicalDirection.Forward));
            p = p.GetNextContextPosition(System.Windows.Documents.LogicalDirection.Forward);
        }
        return sb.ToString();
    }

    private static System.Windows.Documents.TextPointer NavigateToOffset(System.Windows.Documents.FlowDocument doc, int target)
    {
        int count = 0;
        var p = doc.ContentStart;
        while (p != null)
        {
            if (p.GetPointerContext(System.Windows.Documents.LogicalDirection.Forward) == System.Windows.Documents.TextPointerContext.Text)
            {
                string run = p.GetTextInRun(System.Windows.Documents.LogicalDirection.Forward);
                if (count + run.Length >= target)
                    return p.GetPositionAtOffset(target - count) ?? doc.ContentEnd;
                count += run.Length;
            }
            p = p.GetNextContextPosition(System.Windows.Documents.LogicalDirection.Forward);
        }
        return doc.ContentEnd;
    }

    private void SelectAndScroll(RichTextBox rtb, int offset, int len)
    {
        var s = NavigateToOffset(rtb.Document, offset);
        var en = NavigateToOffset(rtb.Document, offset + len);
        rtb.Selection.Select(s, en);
        var rect = s.GetCharacterRect(System.Windows.Documents.LogicalDirection.Forward);
        var sv = FindSv(rtb);
        if (sv != null)
        {
            double target = sv.VerticalOffset + rect.Top - 60;
            sv.ScrollToVerticalOffset(target < 0 ? 0 : target);
        }
    }

    private static ScrollViewer? FindSv(System.Windows.DependencyObject root)
    {
        if (root is ScrollViewer sv) return sv;
        int n = System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < n; i++)
        {
            var f = FindSv(System.Windows.Media.VisualTreeHelper.GetChild(root, i));
            if (f != null) return f;
        }
        return null;
    }

    /// <summary>Click vào BẤT KỲ chỗ nào trên header nhóm → đóng/mở nhóm (trừ khi click trúng checkbox/toggle).</summary>
    private void GroupHeader_MouseUp(object sender, MouseButtonEventArgs e)
    {
        // CheckBox & ToggleButton đều là ToggleButton → click trúng chúng thì để tự xử lý.
        if (e.OriginalSource is System.Windows.DependencyObject d &&
            FindUp<System.Windows.Controls.Primitives.ToggleButton>(d) != null)
            return;
        if (sender is System.Windows.DependencyObject host)
        {
            var toggle = FindDown<System.Windows.Controls.Primitives.ToggleButton>(host);
            if (toggle != null) toggle.IsChecked = !(toggle.IsChecked ?? false);
        }
    }

    private static T? FindUp<T>(System.Windows.DependencyObject? d) where T : class
    {
        while (d != null) { if (d is T t) return t; d = System.Windows.Media.VisualTreeHelper.GetParent(d); }
        return null;
    }

    private static T? FindDown<T>(System.Windows.DependencyObject root) where T : class
    {
        int n = System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < n; i++)
        {
            var c = System.Windows.Media.VisualTreeHelper.GetChild(root, i);
            if (c is T t) return t;
            var f = FindDown<T>(c);
            if (f != null) return f;
        }
        return null;
    }

    private async void CompareButton_Click(object sender, RoutedEventArgs e)
    {
        DbConnectionInfo source = CompareSource();
        DbConnectionInfo target = CompareTarget();

        // Chỉ validate connection của bên KHÔNG dùng snapshot (snapshot bỏ qua đọc DB).
        if (_sourceSnapshotOverride == null && !Validate(source, "Source")) return;
        if (_targetSnapshotOverride == null && !Validate(target, "Target")) return;

        SchemaCompareOptions options = BuildCompareOptions();
        DatabaseSchema? srcOverride = _sourceSnapshotOverride;
        DatabaseSchema? tgtOverride = _targetSnapshotOverride;

        SetBusy(true, "Reading & comparing schemas...");
        try
        {
            var (srcSchema, tgtSchema, result) = await Task.Run(() =>
            {
                var reader = new SchemaReader();
                var src = srcOverride ?? reader.Read(source);
                var tgt = tgtOverride ?? reader.Read(target);
                var cmp = new SchemaComparer().Compare(src, tgt, options);
                return (src, tgt, cmp);
            });

            _sourceSchema = srcSchema;
            _targetSchema = tgtSchema;
            _lastResult = result;

            // Thời gian sửa cuối object (sys.objects.modify_date) — NOLOCK, 1 query/bên, lỗi thì bỏ qua.
            var srcMod = await Task.Run(() => SafeReadModifyDates(source));
            var tgtMod = await Task.Run(() => SafeReadModifyDates(target));

            // Giữ trạng thái tick CŨ theo key — reload không mất tick của object vẫn còn khác biệt.
            var prevCheck = new System.Collections.Generic.Dictionary<string, bool>();
            foreach (var r in _rows) prevCheck[r.Item.Key] = r.Include;

            foreach (var r in _rows) r.PropertyChanged -= RowVm_PropertyChanged; // hygiene: gỡ subscriber cũ
            _rows.Clear();
            _groupChecks.Clear();   // group header sẽ re-register khi render lại
            _groupCounts.Clear();
            foreach (var item in result.Items)
            {
                var rowVm = new SchemaRowVm(item);
                // Thời gian sửa cuối của object ở CẢ 2 bên (source + target).
                if (srcMod.TryGetValue(item.Key, out var sd)) rowVm.SourceModifiedText = sd.ToString("yyyy-MM-dd HH:mm");
                if (tgtMod.TryGetValue(item.Key, out var td)) rowVm.TargetModifiedText = td.ToString("yyyy-MM-dd HH:mm");
                // Reload: object còn trong kết quả + từng được tick → giữ tick; còn lại default uncheck.
                if (prevCheck.TryGetValue(item.Key, out var wasChecked)) rowVm.Include = wasChecked;
                rowVm.PropertyChanged += RowVm_PropertyChanged; // theo dõi tick để đếm Selected
                _rows.Add(rowVm);
            }
            _rowsView?.Refresh();
            UpdateSelectedCount();

            // Reset script + warnings từ lần generate trước (không còn khớp với result mới).
            ScriptBox.Text = "";
            ShowWarnings(null);

            SummaryText.Text =
                $"Only in Source: {result.CountOnlyInSource}   " +
                $"Only in Target: {result.CountOnlyInTarget}   " +
                $"Different: {result.CountDifferent}   " +
                $"Identical: {result.CountIdentical}";
            StatusText.Text = $"Done. {result.Items.Count} object(s).";

            DeployButton.Visibility = Visibility.Visible;
        }
        catch (System.Exception ex)
        {
            MessageBox.Show(ex.Message, "Compare failed", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText.Text = "Failed.";
        }
        finally
        {
            SetBusy(false, null);
        }
    }

    private async void GenerateButton_Click(object sender, RoutedEventArgs e)
    {
        if (_sourceSchema == null || _targetSchema == null)
        {
            MessageBox.Show("Run Compare first.", "No comparison", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        List<SchemaDiffItem> selected = _rows.Where(r => r.Include).Select(r => r.Item).ToList();
        if (selected.Count == 0)
        {
            MessageBox.Show("No objects selected (Include).", "Nothing to sync", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        DatabaseSchema src = _sourceSchema;
        DatabaseSchema tgt = _targetSchema;
        bool redgateWrap = WrapInTransactionChk.IsChecked == true;
        var scriptOptions = new SchemaScriptOptions
        {
            WrapInTransaction = false, // sinh raw; wrapper Redgate tự bọc transaction nếu bật
            IncludeDrops = IncludeDropsChk.IsChecked == true,
        };

        SetBusy(true, "Generating sync script...");
        try
        {
            SyncScriptResult result = await Task.Run(() =>
                new SchemaScriptGenerator().GenerateSyncScriptWithWarnings(src, tgt, selected, scriptOptions));
            ScriptBox.Text = WrapDeployScript(result.Script, redgateWrap);
            ShowWarnings(result.Warnings);
            StatusText.Text = $"Script generated ({selected.Count} object(s)).";
        }
        catch (System.Exception ex)
        {
            MessageBox.Show(ex.Message, "Generate failed", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText.Text = "Failed.";
        }
        finally
        {
            SetBusy(false, null);
        }
    }

    private void SelectAllButton_Click(object sender, RoutedEventArgs e)
    {
        foreach (var row in _rows) row.Include = true;
        UpdateSelectedCount();
    }

    /// <summary>Khi tick/untick 1 dòng → cập nhật "Selected X of Y".</summary>
    private void RowVm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SchemaRowVm.Include)) UpdateSelectedCount();
    }

    /// <summary>Cập nhật "Selected X of Y" + tristate của master checkbox header.</summary>
    private void UpdateSelectedCount()
    {
        int total = _rows.Count;
        int sel = _rows.Count(r => r.Include);
        SelectedCountText.Text = $"{sel} of {total}";
        // Master checkbox: none → false, all → true, 1 phần → null (indeterminate).
        if (HeaderCheckBox != null)
            HeaderCheckBox.IsChecked = (total == 0 || sel == 0) ? false : (sel == total ? true : (bool?)null);
        RefreshGroupStates();
    }

    /// <summary>Master checkbox ở header → chọn/bỏ chọn TẤT CẢ.</summary>
    private void HeaderCheckBox_Click(object sender, RoutedEventArgs e)
    {
        bool toAll = HeaderCheckBox.IsChecked == true; // sau click đã là true/false
        foreach (var r in _rows) r.Include = toAll;
        UpdateSelectedCount();
    }

    // ----- Group header: tristate checkbox + đếm sel/tổng -----
    private readonly System.Collections.Generic.List<CheckBox> _groupChecks = new();
    private readonly System.Collections.Generic.List<TextBlock> _groupCounts = new();

    private void GroupCheck_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox cb && !_groupChecks.Contains(cb)) _groupChecks.Add(cb);
        RefreshGroupStates();
    }

    private void GroupCount_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is TextBlock tb && !_groupCounts.Contains(tb)) _groupCounts.Add(tb);
        RefreshGroupStates();
    }

    /// <summary>Cập nhật tristate + text "sel/total" cho mọi group header đang hiển thị.</summary>
    private void RefreshGroupStates()
    {
        _groupChecks.RemoveAll(cb => !cb.IsLoaded);
        _groupCounts.RemoveAll(tb => !tb.IsLoaded);

        foreach (var cb in _groupChecks)
        {
            if (cb.DataContext is not System.Windows.Data.CollectionViewGroup g) continue;
            int t = g.Items.OfType<SchemaRowVm>().Count();
            int s = g.Items.OfType<SchemaRowVm>().Count(v => v.Include);
            cb.IsChecked = (t == 0 || s == 0) ? false : (s == t ? true : (bool?)null);
        }
        foreach (var tb in _groupCounts)
        {
            if (tb.DataContext is not System.Windows.Data.CollectionViewGroup g) continue;
            int t = g.Items.OfType<SchemaRowVm>().Count();
            int s = g.Items.OfType<SchemaRowVm>().Count(v => v.Include);
            tb.Text = $"{s}/{t}";
        }
    }

    /// <summary>Double-click vào banner → đảo Source ⇄ Target, rồi so lại nếu đã có kết quả.</summary>
    private void Banner_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ClickCount != 2) return;
        // Bỏ qua nếu double-click trúng nút (vd Deploy) — không đảo nhầm.
        if (e.OriginalSource is System.Windows.DependencyObject d && FindAncestor<Button>(d) != null) return;
        SwapButton_Click(sender, e); // đã tự re-compare theo chiều mới
    }

    private static T? FindAncestor<T>(System.Windows.DependencyObject? d) where T : class
    {
        while (d != null)
        {
            if (d is T t) return t;
            d = System.Windows.Media.VisualTreeHelper.GetParent(d);
        }
        return null;
    }

    /// <summary>Chạy so sánh (cho phím F5 từ MainWindow).</summary>
    public void RunCompare() => CompareButton_Click(this, new RoutedEventArgs());

    /// <summary>
    /// Ctrl+F → focus ô tìm: nếu đang đọc định nghĩa (focus trong panel diff) thì vào ô Find dưới,
    /// ngược lại (đang ở danh sách object) vào ô tìm object TRÊN.
    /// </summary>
    public void FocusFind()
    {
        bool inDiff = System.Windows.Input.Keyboard.FocusedElement is System.Windows.DependencyObject fe && IsInDiffPanel(fe);
        if (inDiff) { FindBox.Focus(); FindBox.SelectAll(); }
        else { SearchBox.Focus(); SearchBox.SelectAll(); }
    }

    /// <summary>Phần tử đang focus có nằm trong panel định nghĩa (SideProgrammablePanel) không.</summary>
    private bool IsInDiffPanel(System.Windows.DependencyObject? d)
    {
        while (d != null)
        {
            if (ReferenceEquals(d, SideProgrammablePanel)) return true;
            d = System.Windows.Media.VisualTreeHelper.GetParent(d) ?? System.Windows.LogicalTreeHelper.GetParent(d);
        }
        return false;
    }

    /// <summary>Mở/đóng popup Options.</summary>
    private void OptionsButton_Click(object sender, RoutedEventArgs e)
        => OptionsPopup.IsOpen = !OptionsPopup.IsOpen;

    /// <summary>Checkbox trên group header → tick/bỏ tick Include cho TẤT CẢ object trong nhóm.</summary>
    private void GroupCheck_Click(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox cb && cb.DataContext is System.Windows.Data.CollectionViewGroup grp)
        {
            bool val = cb.IsChecked == true;
            foreach (var it in grp.Items)
                if (it is SchemaRowVm vm) vm.Include = val;
            UpdateSelectedCount();
        }
    }

    // ====================== Header panel compare (Redgate-style) ======================

    private double _diffFontSize = 12;                                  // cỡ chữ panel diff
    private SqlCompareSuite.Core.Models.SchemaDiffItem? _currentDiffItem; // object đang xem để re-render

    /// <summary>Raise khi Save/Load project — MainWindow đổi tên tab theo tên file.</summary>
    public event System.Action<string>? ProjectTitleChanged;

    // Vị trí các dòng khác biệt trong panel diff hiện tại + con trỏ (Previous/Next nhảy tới change).
    private System.Collections.Generic.List<int> _diffLineIndices = new();
    private int _diffPointer = -1;

    /// <summary>⚙ → đổi cỡ chữ panel diff rồi render lại object đang xem.</summary>
    private void DiffTextSize_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem mi && double.TryParse(mi.Tag?.ToString(), out double size))
        {
            _diffFontSize = size;
            if (_currentDiffItem != null) ShowSideBySide(_currentDiffItem);
        }
    }

    /// <summary>Bọc script thô thành script deploy phong cách Redgate (nếu bật) — header + transaction + guard.</summary>
    private string WrapDeployScript(string rawScript, bool redgateWrap)
    {
        if (!redgateWrap) return rawScript;
        var s = CompareSource();
        var t = CompareTarget();
        return SqlCompareSuite.Core.Execution.DeploymentScriptWrapper.Wrap(
            rawScript,
            targetName: $"{t.Server}.{t.Database}",
            sourceName: $"{s.Server}.{s.Database}",
            timestamp: System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
    }

    private void DiffPrev_Click(object sender, RoutedEventArgs e) => MoveToDiff(-1);
    private void DiffNext_Click(object sender, RoutedEventArgs e) => MoveToDiff(+1);

    /// <summary>Nhảy tới dòng KHÁC BIỆT trước/kế trong panel diff hiện tại (focus vào change, cuộn tới).</summary>
    private void MoveToDiff(int delta)
    {
        if (_diffLineIndices.Count == 0) return;
        _diffPointer += delta;
        if (_diffPointer < 0) _diffPointer = 0;
        if (_diffPointer >= _diffLineIndices.Count) _diffPointer = _diffLineIndices.Count - 1;
        SqlDiffRenderer.ScrollToLine(
            SideSourceDef, SideGutterScroll, SideTargetDef, _diffLineIndices[_diffPointer], _diffFontSize);
    }

    /// <summary>Dựng text definition đầy đủ của 1 table để hiển thị diff side-by-side (giống Redgate).</summary>
    private static string BuildTableDefinitionText(SqlCompareSuite.Core.Models.TableDef? t)
    {
        if (t == null) return "(not present)";
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"CREATE TABLE [{t.Schema}].[{t.Name}]");
        sb.AppendLine("(");
        var cols = t.Columns.OrderBy(c => c.OrdinalPosition).ToList();
        for (int i = 0; i < cols.Count; i++)
        {
            var c = cols[i];
            string line = $"    [{c.Name}] ";
            if (c.IsComputed)
                line += $"AS {c.ComputedDefinition}";
            else
            {
                line += SafeType(c);
                if (c.IsIdentity) line += $" IDENTITY({c.IdentitySeed ?? "1"},{c.IdentityIncrement ?? "1"})";
                line += c.IsNullable ? " NULL" : " NOT NULL";
                if (!string.IsNullOrEmpty(c.DefaultDefinition)) line += $" DEFAULT {c.DefaultDefinition}";
            }
            if (i < cols.Count - 1) line += ",";
            sb.AppendLine(line);
        }
        sb.AppendLine(")");

        foreach (var idx in t.Indexes.OrderBy(x => x.Name, System.StringComparer.OrdinalIgnoreCase))
        {
            if (idx.IsPrimaryKey)
                sb.AppendLine($"PRIMARY KEY ({string.Join(", ", idx.Columns)})");
            else
            {
                string u = idx.IsUnique ? "UNIQUE " : "";
                string cl = idx.IsClustered ? "CLUSTERED " : "NONCLUSTERED ";
                string inc = idx.IncludedColumns.Count > 0 ? $" INCLUDE ({string.Join(", ", idx.IncludedColumns)})" : "";
                string flt = !string.IsNullOrEmpty(idx.FilterDefinition) ? $" WHERE {idx.FilterDefinition}" : "";
                sb.AppendLine($"{u}{cl}INDEX [{idx.Name}] ({string.Join(", ", idx.Columns)}){inc}{flt}");
            }
        }
        foreach (var fk in t.ForeignKeys.OrderBy(x => x.Name, System.StringComparer.OrdinalIgnoreCase))
            sb.AppendLine($"FOREIGN KEY ({string.Join(", ", fk.Columns)}) REFERENCES [{fk.RefSchema}].[{fk.RefTable}] ({string.Join(", ", fk.RefColumns)})");
        foreach (var ck in t.CheckConstraints.OrderBy(x => x.Name, System.StringComparer.OrdinalIgnoreCase))
            sb.AppendLine($"CHECK {ck.Definition}");

        return sb.ToString();
    }

    private void SelectNoneButton_Click(object sender, RoutedEventArgs e)
    {
        foreach (var row in _rows) row.Include = false;
    }

    private void SwapButton_Click(object sender, RoutedEventArgs e)
    {
        // KHÔNG đổi vị trí server — chỉ đảo chiều (vai trò source/target + mũi tên + chiều deploy).
        _reversed = !_reversed;
        UpdateBanner();
        StatusText.Text = _reversed ? "Đảo chiều: deploy ⬅ (phải → trái)." : "Chiều deploy: ➡ (trái → phải).";
        // BẮT BUỘC re-compare để kết quả/script khớp chiều mới. Xoá kết quả CŨ trước → nếu
        // re-compare lỗi cũng KHÔNG còn kết quả cũ để deploy nhầm script cũ vào target mới.
        if (_lastResult != null)
        {
            _lastResult = null;
            ScriptBox.Text = "";
            CompareButton_Click(sender, e);
        }
    }

    private async void ExecuteButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ScriptBox.Text))
        {
            MessageBox.Show("No script to execute. Generate a sync script first.",
                "Nothing to execute", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        DbConnectionInfo target = CompareTarget();
        if (!Validate(target, "Target"))
            return;

        var confirm = MessageBox.Show(
            $"This will MODIFY the Target database:\n\n    {target}\n\n" +
            "The script runs inside a single transaction (rollback on error).\n" +
            "Have you reviewed the script?\n\nProceed?",
            "Confirm execute on Target",
            MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
        if (confirm != MessageBoxResult.Yes)
            return;

        string script = ScriptBox.Text;

        SetBusy(true, "Executing on target...");
        try
        {
            ScriptExecutionResult result = await Task.Run(() =>
                new SqlScriptExecutor().Execute(target, script));

            MessageBox.Show(
                result.Message,
                result.Success ? "Execution succeeded" : "Execution failed",
                MessageBoxButton.OK,
                result.Success ? MessageBoxImage.Information : MessageBoxImage.Error);
            StatusText.Text = result.Success
                ? $"Executed. {result.BatchesExecuted} batch(es)."
                : "Execution failed.";
        }
        catch (System.Exception ex)
        {
            MessageBox.Show(ex.Message, "Execute failed", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText.Text = "Failed.";
        }
        finally
        {
            SetBusy(false, null);
        }
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(ScriptBox.Text)) return;
        try
        {
            Clipboard.SetText(ScriptBox.Text);
            StatusText.Text = "Copied to clipboard.";
        }
        catch (System.Exception ex)
        {
            MessageBox.Show(ex.Message, "Copy failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(ScriptBox.Text))
        {
            MessageBox.Show("Nothing to save.", "Empty", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var dlg = new SaveFileDialog
        {
            Filter = "SQL script (*.sql)|*.sql|All files (*.*)|*.*",
            DefaultExt = ".sql",
            FileName = "schema_sync.sql",
        };
        if (dlg.ShowDialog() == true)
        {
            try
            {
                System.IO.File.WriteAllText(dlg.FileName, ScriptBox.Text);
                StatusText.Text = $"Saved: {dlg.FileName}";
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message, "Save failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void DiffGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        // Double-click KHÔNG mở View Details nữa (chi tiết đã hiện ở panel side-by-side dưới).
        // Vẫn dùng nút 🔍 View Details nếu cần mở cửa sổ riêng.
    }

    /// <summary>Chọn 1 dòng → cập nhật panel so sánh side-by-side ở dưới.</summary>
    private void DiffGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DiffGrid.SelectedItem is not SchemaRowVm vm)
            return;
        if (_sourceSchema == null || _targetSchema == null)
        {
            SidePanelTitle.Text = $"{vm.Item.FullName}  —  (run Compare để xem chi tiết)";
            return;
        }
        ShowSideBySide(vm.Item);
    }

    /// <summary>Render panel so sánh: programmable (text 2 bên) hoặc table (2 list cột).</summary>
    private void ShowSideBySide(SqlCompareSuite.Core.Models.SchemaDiffItem item)
    {
        _currentDiffItem = item; // nhớ để re-render khi đổi cỡ chữ
        SidePanelTitle.Text = $"{item.Key}   {(_reversed ? "←" : "→")}   {item.Key}        [{item.StatusText}]";
        string key = item.Key;

        if (item.ObjectType == SqlCompareSuite.Core.Models.DbObjectType.Table)
        {
            // ----- TABLE: definition ĐẦY ĐỦ side-by-side (giống Redgate) → thấy HẾT change + highlight -----
            SideTableGrid.Visibility = Visibility.Collapsed;
            SideProgrammablePanel.Visibility = Visibility.Visible;

            SqlCompareSuite.Core.Models.TableDef? src = null, tgt = null;
            _sourceSchema?.Tables.TryGetValue(key, out src);
            _targetSchema?.Tables.TryGetValue(key, out tgt);

            string srcDef = BuildTableDefinitionText(src);
            string tgtDef = BuildTableDefinitionText(tgt);
            _diffLineIndices = SqlDiffRenderer.RenderSideBySide(SideSourceDef, SideTargetDef, SideGutter, srcDef, tgtDef, _diffFontSize);
            _diffPointer = -1;
            EnsureSideScrollSync();
        }
        else
        {
            // ----- PROGRAMMABLE: 2 ô definition, diff theo dòng + syntax highlight -----
            SideTableGrid.Visibility = Visibility.Collapsed;
            SideProgrammablePanel.Visibility = Visibility.Visible;

            string srcDef = "", tgtDef = "";
            if (_sourceSchema != null && _sourceSchema.Programmables.TryGetValue(key, out var sp))
                srcDef = sp.Definition ?? "";
            if (_targetSchema != null && _targetSchema.Programmables.TryGetValue(key, out var tp))
                tgtDef = tp.Definition ?? "";

            if (string.IsNullOrEmpty(srcDef)) srcDef = "(not present in source)";
            if (string.IsNullOrEmpty(tgtDef)) tgtDef = "(not present in target)";

            // Redgate-style: căn thẳng hàng 2 bên (LCS) + gutter mũi tên + tô nền xanh dòng khác biệt.
            _diffLineIndices = SqlDiffRenderer.RenderSideBySide(SideSourceDef, SideTargetDef, SideGutter, srcDef, tgtDef, _diffFontSize);
            _diffPointer = -1;
            EnsureSideScrollSync();
        }

        // Definition vừa render lại → tô vàng lại nếu đang có từ khoá Find.
        _findHighlights.Clear();
        if (!string.IsNullOrEmpty(FindBox?.Text)) HighlightAllMatches(SideTargetDef, FindBox.Text);
    }

    // Cờ: đã wire đồng bộ scroll cho panel side-by-side chưa (chỉ wire 1 lần).
    private bool _sideScrollWired;

    /// <summary>Wire đồng bộ cuộn dọc left/gutter/right (1 lần, sau khi đã render lần đầu).</summary>
    private void EnsureSideScrollSync()
    {
        if (_sideScrollWired) return;
        _sideScrollWired = true;
        // Defer để ScrollViewer trong template RichTextBox đã được tạo.
        Dispatcher.BeginInvoke(new System.Action(() =>
            SqlDiffRenderer.SyncScroll(SideSourceDef, SideGutterScroll, SideTargetDef)),
            System.Windows.Threading.DispatcherPriority.Loaded);
    }

    /// <summary>Map cột (theo tên) → chữ ký kiểu, để so 2 bên xem cột nào khác.</summary>
    private static System.Collections.Generic.Dictionary<string, string> BuildColSig(
        SqlCompareSuite.Core.Models.TableDef? table)
    {
        var map = new System.Collections.Generic.Dictionary<string, string>(
            System.StringComparer.OrdinalIgnoreCase);
        if (table == null) return map;
        foreach (var c in table.Columns)
            map[c.Name] = SafeType(c);
        return map;
    }

    private static System.Collections.Generic.List<SideColVm> ToSideCols(
        SqlCompareSuite.Core.Models.TableDef? table,
        System.Collections.Generic.Dictionary<string, string> otherSigs)
    {
        var list = new System.Collections.Generic.List<SideColVm>();
        if (table == null) return list;
        foreach (var c in table.Columns.OrderBy(c => c.OrdinalPosition))
        {
            string type = SafeType(c);
            // Khác biệt nếu bên kia không có cột này, hoặc có nhưng kiểu khác.
            bool diff = !otherSigs.TryGetValue(c.Name, out var otherType) || otherType != type;
            list.Add(new SideColVm { Name = c.Name, Type = type, IsDifferent = diff });
        }
        return list;
    }

    private static string SafeType(SqlCompareSuite.Core.Models.ColumnDef c)
    {
        try { return c.FullTypeText(); }
        catch { return c.DataType; }
    }

    /// <summary>VM 1 cột trong panel side-by-side (tô màu khi IsDifferent).</summary>
    public sealed class SideColVm
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public bool IsDifferent { get; set; }
    }

    /// <summary>
    /// Căn hàng cột 2 bảng theo TÊN cột để hiển thị trên 1 grid (1 scroll).
    /// Theo thứ tự cột source; cột chỉ có ở target xếp xuống cuối.
    /// </summary>
    private static System.Collections.Generic.List<SideTableRowVm> ToAlignedRows(
        SqlCompareSuite.Core.Models.TableDef? src, SqlCompareSuite.Core.Models.TableDef? tgt)
    {
        var rows = new System.Collections.Generic.List<SideTableRowVm>();

        var tgtMap = new System.Collections.Generic.Dictionary<string, SqlCompareSuite.Core.Models.ColumnDef>(
            System.StringComparer.OrdinalIgnoreCase);
        if (tgt != null)
            foreach (var c in tgt.Columns) tgtMap[c.Name] = c;
        var usedTgt = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

        if (src != null)
            foreach (var sc in src.Columns.OrderBy(c => c.OrdinalPosition))
            {
                tgtMap.TryGetValue(sc.Name, out var tc);
                if (tc != null) usedTgt.Add(sc.Name);
                string st = SafeType(sc);
                string tt = tc != null ? SafeType(tc) : "";
                bool diff = tc == null || !string.Equals(st, tt, System.StringComparison.OrdinalIgnoreCase);
                rows.Add(new SideTableRowVm
                {
                    SourceName = sc.Name,
                    SourceType = st,
                    TargetName = tc?.Name ?? "",
                    TargetType = tt,
                    Arrow = tc == null ? "→" : (diff ? "≠" : "="),
                    IsDifferent = diff,
                });
            }

        // Cột chỉ có ở target.
        if (tgt != null)
            foreach (var tc in tgt.Columns.OrderBy(c => c.OrdinalPosition))
            {
                if (usedTgt.Contains(tc.Name)) continue;
                rows.Add(new SideTableRowVm
                {
                    SourceName = "",
                    SourceType = "",
                    TargetName = tc.Name,
                    TargetType = SafeType(tc),
                    Arrow = "←",
                    IsDifferent = true,
                });
            }

        return rows;
    }

    /// <summary>VM 1 hàng so cột 2 bảng trên cùng 1 grid.</summary>
    public sealed class SideTableRowVm
    {
        public string SourceName { get; set; } = "";
        public string SourceType { get; set; } = "";
        public string Arrow { get; set; } = "";
        public string TargetName { get; set; } = "";
        public string TargetType { get; set; } = "";
        public bool IsDifferent { get; set; }
    }

    /// <summary>
    /// Tự tính độ rộng 2 cột "Object name" khi grid đổi size — né bug WPF DataGrid
    /// (cột Width="*" bị co về min khi có grouping).
    /// </summary>
    /// <summary>Đọc modify_date an toàn (lỗi/quyền/snapshot → trả map rỗng, không chặn compare).</summary>
    private static System.Collections.Generic.Dictionary<string, System.DateTime> SafeReadModifyDates(
        SqlCompareSuite.Core.Models.DbConnectionInfo conn)
    {
        try { return new SqlCompareSuite.Core.Schema.SchemaReader().ReadModifyDates(conn); }
        catch { return new System.Collections.Generic.Dictionary<string, System.DateTime>(); }
    }

    private void DiffGrid_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (ObjColSource == null || ObjColTarget == null) return;
        // Trừ cột cố định: Type 120 + checkbox 44 + Modified(S) 130 + Modified(T) 130 + scrollbar 18.
        double fixedPart = 120 + 44 + 130 + 130 + 18;
        double avail = DiffGrid.ActualWidth - fixedPart;
        double each = avail / 2;
        if (each < 140) each = 140; // tối thiểu cho dễ đọc
        ObjColSource.Width = new DataGridLength(each);
        ObjColTarget.Width = new DataGridLength(each);
    }

    private void ViewDetailsButton_Click(object sender, RoutedEventArgs e)
    {
        OpenDetails();
    }

    private void OpenDetails()
    {
        if (DiffGrid.SelectedItem is not SchemaRowVm vm)
        {
            MessageBox.Show("Select a row first.", "No selection",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (_sourceSchema == null || _targetSchema == null)
        {
            MessageBox.Show("Run Compare first.", "No comparison",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        DiffViewerWindow.ShowSchema(vm.Item, _sourceSchema, _targetSchema, Window.GetWindow(this));
    }

    private void SaveProjectButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dlg = new SaveFileDialog
            {
                Filter = "Schema Compare project (*.sqlcmp)|*.sqlcmp|JSON (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = ".sqlcmp",
                FileName = "comparison.sqlcmp",
            };
            if (dlg.ShowDialog() != true) return;

            var project = new ComparisonProject
            {
                Kind = "Schema",
                Source = CompareSource(),
                Target = CompareTarget(),
            };
            ComparisonProjectStore.Save(dlg.FileName, project);
            RecentProjectStore.Add(dlg.FileName, "Schema");
            ProjectTitleChanged?.Invoke(System.IO.Path.GetFileNameWithoutExtension(dlg.FileName));
            StatusText.Text = $"Project saved: {dlg.FileName}";
        }
        catch (System.Exception ex)
        {
            MessageBox.Show(ex.Message, "Save project failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadProjectButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Schema Compare project (*.sqlcmp)|*.sqlcmp|JSON (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = ".sqlcmp",
            };
            if (dlg.ShowDialog() != true) return;

            ComparisonProject project = ComparisonProjectStore.Load(dlg.FileName);
            if (!string.Equals(project.Kind, "Schema", System.StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(
                    $"This project is of kind '{project.Kind}', not 'Schema'. Connections will still be loaded.",
                    "Kind mismatch", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            SourcePanel.SetConnectionInfo(project.Source);
            TargetPanel.SetConnectionInfo(project.Target);
            _reversed = false;
            UpdateBanner();
            RecentProjectStore.Add(dlg.FileName, "Schema");
            ProjectTitleChanged?.Invoke(System.IO.Path.GetFileNameWithoutExtension(dlg.FileName));
            StatusText.Text = $"Project loaded: {dlg.FileName}";
        }
        catch (System.Exception ex)
        {
            MessageBox.Show(ex.Message, "Load project failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>Dựng SchemaCompareOptions từ panel Options (patterns + ignore rules).</summary>
    private SchemaCompareOptions BuildCompareOptions()
    {
        return new SchemaCompareOptions
        {
            IncludeNamePatterns = SplitPatterns(IncludePatternsBox.Text),
            ExcludeNamePatterns = SplitPatterns(ExcludePatternsBox.Text),
            IncludeTypes = BuildIncludeTypes(),
            IgnoreWhiteSpace = IgnoreWhiteSpaceChk.IsChecked == true,
            IgnoreComments = IgnoreCommentsChk.IsChecked == true,
            IgnoreConstraintNames = IgnoreConstraintNamesChk.IsChecked == true,
            IgnoreCollation = IgnoreCollationChk.IsChecked == true,
            IgnoreIdentitySeed = IgnoreIdentitySeedChk.IsChecked == true,
            IgnoreFillFactor = IgnoreFillFactorChk.IsChecked == true,
        };
    }

    /// <summary>
    /// Tập loại object được bao gồm từ các checkbox. Nếu TẤT CẢ được tick → trả null (= mọi loại).
    /// </summary>
    private HashSet<DbObjectType>? BuildIncludeTypes()
    {
        var map = new (CheckBox Box, DbObjectType Type)[]
        {
            (TypeTableChk, DbObjectType.Table),
            (TypeViewChk, DbObjectType.View),
            (TypeProcChk, DbObjectType.StoredProcedure),
            (TypeFuncChk, DbObjectType.Function),
            (TypeTriggerChk, DbObjectType.Trigger),
            (TypeSequenceChk, DbObjectType.Sequence),
            (TypeSynonymChk, DbObjectType.Synonym),
            (TypeSchemaChk, DbObjectType.Schema),
            (TypeUserChk, DbObjectType.User),
            (TypeRoleChk, DbObjectType.Role),
            (TypePermissionChk, DbObjectType.Permission),
            (TypePartFuncChk, DbObjectType.PartitionFunction),
            (TypePartSchemeChk, DbObjectType.PartitionScheme),
        };

        var set = new HashSet<DbObjectType>();
        foreach (var (box, type) in map)
            if (box.IsChecked == true) set.Add(type);

        // Tất cả tick → null (không lọc) cho nhẹ; ngược lại trả tập đã chọn.
        return set.Count == map.Length ? null : set;
    }

    /// <summary>Tách chuỗi pattern phân tách bằng ';' thành list (bỏ phần rỗng).</summary>
    private static List<string> SplitPatterns(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return new List<string>();
        return text.Split(';', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries)
                   .ToList();
    }

    /// <summary>Hiển thị warnings dưới ô script. Rỗng → "No warnings".</summary>
    private void ShowWarnings(List<string>? warnings)
    {
        WarningsList.Items.Clear();
        if (warnings == null || warnings.Count == 0)
        {
            WarningsHeader.Text = "No warnings";
            WarningsList.Visibility = Visibility.Collapsed;
            return;
        }
        WarningsHeader.Text = $"⚠ {warnings.Count} warning(s):";
        foreach (var w in warnings) WarningsList.Items.Add(w);
        WarningsList.Visibility = Visibility.Visible;
    }

    // ====================== SNAPSHOT ======================

    private async void SaveSourceSnapshotButton_Click(object sender, RoutedEventArgs e)
        => await SaveSnapshot(isSource: true);

    private async void SaveTargetSnapshotButton_Click(object sender, RoutedEventArgs e)
        => await SaveSnapshot(isSource: false);

    /// <summary>
    /// Lưu snapshot 1 bên. Ưu tiên schema đã đọc/override; nếu chưa có thì đọc nhanh từ DB live.
    /// </summary>
    private async Task SaveSnapshot(bool isSource)
    {
        string side = isSource ? "Source" : "Target";
        DatabaseSchema? schema = isSource
            ? (_sourceSnapshotOverride ?? _sourceSchema)
            : (_targetSnapshotOverride ?? _targetSchema);

        DbConnectionInfo conn = isSource ? SourcePanel.GetConnectionInfo() : TargetPanel.GetConnectionInfo();

        var dlg = new SaveFileDialog
        {
            Filter = "Schema snapshot (*.snapshot;*.json)|*.snapshot;*.json|All files (*.*)|*.*",
            DefaultExt = ".snapshot",
            FileName = $"{side.ToLowerInvariant()}.snapshot",
        };
        if (dlg.ShowDialog() != true) return;

        // Chưa có schema sẵn → cần đọc DB live, nên phải validate connection.
        if (schema == null && !Validate(conn, side)) return;

        string path = dlg.FileName;
        SetBusy(true, $"Saving {side} snapshot...");
        try
        {
            await Task.Run(() =>
            {
                var s = schema ?? new SchemaReader().Read(conn);
                SchemaSnapshotStore.Save(path, s);
            });
            StatusText.Text = $"{side} snapshot saved: {path}";
        }
        catch (System.Exception ex)
        {
            MessageBox.Show(ex.Message, "Save snapshot failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SetBusy(false, null);
        }
    }

    private async void LoadSourceSnapshotButton_Click(object sender, RoutedEventArgs e)
        => await LoadSnapshot(isSource: true);

    private async void LoadTargetSnapshotButton_Click(object sender, RoutedEventArgs e)
        => await LoadSnapshot(isSource: false);

    /// <summary>Nạp snapshot 1 bên làm override. Compare sẽ dùng nó thay vì đọc DB live.</summary>
    private async Task LoadSnapshot(bool isSource)
    {
        string side = isSource ? "Source" : "Target";
        var dlg = new OpenFileDialog
        {
            Filter = "Schema snapshot (*.snapshot;*.json)|*.snapshot;*.json|All files (*.*)|*.*",
            DefaultExt = ".snapshot",
        };
        if (dlg.ShowDialog() != true) return;

        string path = dlg.FileName;
        SetBusy(true, $"Loading {side} snapshot...");
        try
        {
            DatabaseSchema schema = await Task.Run(() => SchemaSnapshotStore.Load(path));
            string name = System.IO.Path.GetFileName(path);
            if (isSource)
            {
                _sourceSnapshotOverride = schema;
                _sourceSnapshotName = name;
            }
            else
            {
                _targetSnapshotOverride = schema;
                _targetSnapshotName = name;
            }
            UpdateSnapshotInfo();
            StatusText.Text = $"{side} = snapshot: {name}";
        }
        catch (System.Exception ex)
        {
            MessageBox.Show(ex.Message, "Load snapshot failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SetBusy(false, null);
        }
    }

    /// <summary>Xóa cả 2 snapshot override → quay lại đọc DB live khi Compare.</summary>
    private void ClearSnapshotButton_Click(object sender, RoutedEventArgs e)
    {
        _sourceSnapshotOverride = null;
        _targetSnapshotOverride = null;
        _sourceSnapshotName = null;
        _targetSnapshotName = null;
        UpdateSnapshotInfo();
        StatusText.Text = "Snapshots cleared (live DB sẽ được đọc khi Compare).";
    }

    /// <summary>Cập nhật label báo bên nào đang dùng snapshot.</summary>
    private void UpdateSnapshotInfo()
    {
        var parts = new List<string>();
        if (_sourceSnapshotName != null) parts.Add($"Source = snapshot: {_sourceSnapshotName}");
        if (_targetSnapshotName != null) parts.Add($"Target = snapshot: {_targetSnapshotName}");
        SnapshotInfoText.Text = parts.Count > 0 ? string.Join("   |   ", parts) : "";
    }

    // ====================== SCRIPT FOLDER (SOURCE CONTROL) ======================

    private async void LoadScriptFolderSourceButton_Click(object sender, RoutedEventArgs e)
        => await LoadScriptFolder(isSource: true);

    private async void LoadScriptFolderTargetButton_Click(object sender, RoutedEventArgs e)
        => await LoadScriptFolder(isSource: false);

    /// <summary>
    /// Chọn 1 folder chứa *.sql (source control), đọc thành DatabaseSchema và gán
    /// vào field override — tái dùng đúng cơ chế snapshot override để Compare dùng
    /// schema này thay cho đọc DB live.
    /// </summary>
    private async Task LoadScriptFolder(bool isSource)
    {
        string side = isSource ? "Source" : "Target";
        var dlg = new OpenFolderDialog
        {
            Title = $"Chọn folder script (*.sql) cho {side}",
        };
        if (dlg.ShowDialog() != true) return;

        string folder = dlg.FolderName;
        string folderName = System.IO.Path.GetFileName(folder.TrimEnd('\\', '/'));
        if (string.IsNullOrEmpty(folderName)) folderName = folder;

        SetBusy(true, $"Reading script folder for {side}...");
        try
        {
            DatabaseSchema schema = await Task.Run(() => new ScriptFolderReader().Read(folder));

            // Tái dùng đúng field override + label snapshot.
            string label = $"scripts: {folderName}";
            if (isSource)
            {
                _sourceSnapshotOverride = schema;
                _sourceSnapshotName = label;
            }
            else
            {
                _targetSnapshotOverride = schema;
                _targetSnapshotName = label;
            }
            UpdateSnapshotInfo();
            StatusText.Text =
                $"{side} = {label} ({schema.Tables.Count} table(s), {schema.Programmables.Count} programmable(s)).";
        }
        catch (System.Exception ex)
        {
            MessageBox.Show(ex.Message, "Load script folder failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SetBusy(false, null);
        }
    }

    // ====================== EXPORT REPORT ======================

    private void ExportReportButton_Click(object sender, RoutedEventArgs e)
    {
        if (_lastResult == null)
        {
            MessageBox.Show("Run Compare first.", "No comparison", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dlg = new SaveFileDialog
        {
            Filter = "HTML report (*.html)|*.html|XML report (*.xml)|*.xml|CSV report (*.csv)|*.csv",
            DefaultExt = ".html",
            FileName = "schema_comparison.html",
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            string sourceName = DescribeSide(_sourceSnapshotName, SourcePanel.GetConnectionInfo());
            string targetName = DescribeSide(_targetSnapshotName, TargetPanel.GetConnectionInfo());
            string ext = System.IO.Path.GetExtension(dlg.FileName).ToLowerInvariant();
            string content = ext switch
            {
                ".xml" => SchemaReport.ToXml(_lastResult),
                ".csv" => SchemaReport.ToCsv(_lastResult),
                _ => SchemaReport.ToHtml(_lastResult, sourceName, targetName),
            };
            System.IO.File.WriteAllText(dlg.FileName, content);
            StatusText.Text = $"Report saved: {dlg.FileName}";
        }
        catch (System.Exception ex)
        {
            MessageBox.Show(ex.Message, "Export report failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>Tên hiển thị 1 bên cho report: snapshot filename hoặc "server/database".</summary>
    private static string DescribeSide(string? snapshotName, DbConnectionInfo conn)
        => snapshotName ?? $"{conn.Server}/{conn.Database}";

    private static bool Validate(DbConnectionInfo conn, string side)
    {
        if (string.IsNullOrWhiteSpace(conn.Server) || string.IsNullOrWhiteSpace(conn.Database))
        {
            MessageBox.Show($"{side}: Server and Database are required.", "Missing input",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
        return true;
    }

    // ====================== SAVED CONNECTIONS (panel trái MainWindow gọi) ======================

    /// <summary>Connection nguồn hiện tại (đọc từ ConnectionPanel ẩn).</summary>
    public DbConnectionInfo GetSourceConnection() => CompareSource();

    /// <summary>Connection đích hiện tại (đọc từ ConnectionPanel ẩn).</summary>
    public DbConnectionInfo GetTargetConnection() => CompareTarget();

    /// <summary>Áp connection vào Source rồi cập nhật banner.</summary>
    public void SetSourceConnection(DbConnectionInfo conn)
    {
        if (conn == null) return;
        SourcePanel.SetConnectionInfo(conn);
        UpdateBanner();
        StatusText.Text = "Source applied from saved connection.";
    }

    /// <summary>Áp connection vào Target rồi cập nhật banner.</summary>
    public void SetTargetConnection(DbConnectionInfo conn)
    {
        if (conn == null) return;
        TargetPanel.SetConnectionInfo(conn);
        UpdateBanner();
        StatusText.Text = "Target applied from saved connection.";
    }

    /// <summary>Áp toàn bộ 1 project đã lưu (connection Source/Target) vào view này.</summary>
    public void ApplyProject(ComparisonProject p)
    {
        if (p == null) return;
        if (p.Source != null) SetSourceConnection(p.Source);
        if (p.Target != null) SetTargetConnection(p.Target);
        _reversed = false; // project lưu source/target theo chiều → áp vào để mặc định
        UpdateBanner();
        StatusText.Text = "Project applied.";
    }

    private void SetBusy(bool busy, string? status)
    {
        CompareButton.IsEnabled = !busy;
        DeployButton.IsEnabled = !busy;
        EditProjectButton.IsEnabled = !busy;
        GenerateButton.IsEnabled = !busy;
        CopyButton.IsEnabled = !busy;
        SaveButton.IsEnabled = !busy;
        SwapButton.IsEnabled = !busy;
        SelectAllButton.IsEnabled = !busy;
        SelectNoneButton.IsEnabled = !busy;
        ExecuteButton.IsEnabled = !busy;
        ViewDetailsButton.IsEnabled = !busy;
        SaveProjectButton.IsEnabled = !busy;
        LoadProjectButton.IsEnabled = !busy;
        SaveSourceSnapshotButton.IsEnabled = !busy;
        SaveTargetSnapshotButton.IsEnabled = !busy;
        LoadSourceSnapshotButton.IsEnabled = !busy;
        LoadTargetSnapshotButton.IsEnabled = !busy;
        ClearSnapshotButton.IsEnabled = !busy;
        LoadScriptFolderSourceButton.IsEnabled = !busy;
        LoadScriptFolderTargetButton.IsEnabled = !busy;
        ExportReportButton.IsEnabled = !busy;
        BusyBar.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
        BusyText.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
        Mouse.OverrideCursor = busy ? Cursors.Wait : null;
        if (status != null) StatusText.Text = status;
    }
}
