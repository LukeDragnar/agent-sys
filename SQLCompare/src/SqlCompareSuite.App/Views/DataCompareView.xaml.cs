using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using SqlCompareSuite.Core.Data;
using SqlCompareSuite.Core.Execution;
using SqlCompareSuite.Core.Models;
using SqlCompareSuite.Core.Project;
using SqlCompareSuite.Core.Reporting;

namespace SqlCompareSuite.App.Views;

/// <summary>
/// Data Compare kiểu Redgate, LAZY:
///  - Compare cả DB → list bảng chỉ ĐẾM count (nhẹ, hiện dần), KHÔNG load rows.
///  - Chọn 1 bảng → mới full-compare rows bảng đó (grid dưới).
/// </summary>
public partial class DataCompareView : UserControl
{
    private readonly ObservableCollection<TableSummaryVm> _tables = new();
    private ICollectionView? _tablesView;

    private TableSummaryVm? _current;        // bảng đang xem ở grid dưới
    private ICollectionView? _rowsView;

    // Generation để bỏ qua kết quả đếm của lần Compare cũ (khi user Compare lại).
    private int _compareGen;
    // Options dùng để lazy-load rows + deploy (khớp với options đã dùng lúc đếm).
    private DataCompareOptions _lastLoadOptions = new();

    // Bảng được chọn để compare (từ Edit Project → tab Tables & Views). null = so tất cả bảng.
    private List<string>? _selectedTables;

    private DataComparisonResult? _result => _current?.Result;

    public event System.Action<string>? ProjectTitleChanged;

    public DataCompareView()
    {
        InitializeComponent();

        _tablesView = CollectionViewSource.GetDefaultView(_tables);
        _tablesView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(TableSummaryVm.TypeText)));
        // Sort theo TypeOrder (Different trên, Identical dưới cùng) rồi tên bảng.
        _tablesView.SortDescriptions.Add(new SortDescription(nameof(TableSummaryVm.TypeOrder), ListSortDirection.Ascending));
        _tablesView.SortDescriptions.Add(new SortDescription(nameof(TableSummaryVm.TableKey), ListSortDirection.Ascending));
        // Live shaping: khi count tới (đổi TypeOrder/TypeText) → item tự nhảy nhóm + đúng thứ tự.
        if (_tablesView is ICollectionViewLiveShaping live)
        {
            live.IsLiveGrouping = true;
            live.LiveGroupingProperties.Add(nameof(TableSummaryVm.TypeText));
            live.IsLiveSorting = true;
            live.LiveSortingProperties.Add(nameof(TableSummaryVm.TypeOrder));
            live.LiveSortingProperties.Add(nameof(TableSummaryVm.TableKey));
        }
        _tablesView.Filter = TableSearchFilter;
        TablesGrid.ItemsSource = _tablesView;

        FilterCombo.SelectedIndex = 0;

        UpdateBanner();
        UpdateSelectedCount();
    }

    // Đảo chiều compare/deploy mà KHÔNG đổi vị trí 2 server (giống Redgate). false: trái=Source→phải=Target.
    private bool _reversed;
    private DbConnectionInfo CompareSource() => _reversed ? TargetPanel.GetConnectionInfo() : SourcePanel.GetConnectionInfo();
    private DbConnectionInfo CompareTarget() => _reversed ? SourcePanel.GetConnectionInfo() : TargetPanel.GetConnectionInfo();

    /// <summary>Banner: server giữ NGUYÊN vị trí trái/phải; đổi màu vai trò + chevron Deploy theo chiều.</summary>
    private void UpdateBanner()
    {
        var left = SourcePanel.GetConnectionInfo();   // luôn hiển thị bên trái
        var right = TargetPanel.GetConnectionInfo();  // luôn hiển thị bên phải
        SourceServerLabel.Text = string.IsNullOrWhiteSpace(left.Server) ? "(no server)" : left.Server;
        SourceDbLabel.Text = string.IsNullOrWhiteSpace(left.Database) ? "(no database)" : left.Database;
        TargetServerLabel.Text = string.IsNullOrWhiteSpace(right.Server) ? "(no server)" : right.Server;
        TargetDbLabel.Text = string.IsNullOrWhiteSpace(right.Database) ? "(no database)" : right.Database;

        var pink = (System.Windows.Media.Brush)FindResource("RgBanner");
        var white = System.Windows.Media.Brushes.White;
        var dark = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x22, 0x22, 0x22));
        var gray = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x66, 0x66, 0x66));
        var lightPink = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFB, 0xE0, 0xEC));

        if (!_reversed)
        {
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

    private void EditProjectButton_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new ProjectSetupDialog(SourcePanel.GetConnectionInfo(), TargetPanel.GetConnectionInfo(), _selectedTables)
        {
            Owner = Window.GetWindow(this),
            ProjectKind = "Data",
        };
        if (dlg.ShowDialog() == true && dlg.ResultSource != null && dlg.ResultTarget != null)
        {
            SourcePanel.SetConnectionInfo(dlg.ResultSource);
            TargetPanel.SetConnectionInfo(dlg.ResultTarget);
            _reversed = false; // user vừa định nghĩa source/target rõ ràng → chiều mặc định
            _selectedTables = dlg.SelectedTables;   // null = so tất cả; có list = chỉ so các bảng này
            UpdateBanner();
            int? n = _selectedTables?.Count;
            StatusText.Text = n is > 0 ? $"Connections updated. Sẽ so {n} bảng đã chọn." : "Connections updated.";
        }
    }

    // ====================== COMPARE: list + count (lazy) ======================

    private async void CompareButton_Click(object sender, RoutedEventArgs e)
    {
        DbConnectionInfo source = CompareSource();
        DbConnectionInfo target = CompareTarget();

        if (!Validate(source, "Source") || !Validate(target, "Target")) return;

        DataCompareOptions options = BuildCompareOptions(out string? optionsError);
        if (optionsError != null)
        {
            MessageBox.Show(optionsError, "Invalid option", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string? singleTable = TableCombo.SelectedItem as string;
        int gen = ++_compareGen;

        // hygiene: gỡ subscriber cũ của mọi row trước khi bỏ các bảng cũ.
        foreach (var t in _tables)
            foreach (var r in t.Rows) r.PropertyChanged -= RowVm_PropertyChanged;
        _tables.Clear();
        _current = null;
        DiffGrid.ItemsSource = null;
        SidePanelTitle.Text = "(chọn 1 bảng ở trên để xem chi tiết row)";

        var svc = new DataCompareService();

        // --- Chế độ 1 bảng cụ thể (chọn trong Options) → full compare luôn bảng đó ---
        if (!string.IsNullOrWhiteSpace(singleTable))
        {
            _lastLoadOptions = options;
            SetBusy(true, $"Comparing {singleTable} ...");
            try
            {
                var res = await Task.Run(() => svc.Compare(source, target, singleTable!, options));
                var vm = new TableSummaryVm(res.TableKey);
                vm.SetSummary(SummaryFromResult(res));
                vm.SetResult(res);
                BuildRows(vm, res);
                _tables.Add(vm);
                TablesGrid.SelectedItem = vm;
                StatusText.Text = $"Done. {res.Rows.Count} row(s).";
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message, "Compare failed", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "Failed.";
            }
            finally { SetBusy(false, null); }
            return;
        }

        // --- Chế độ cả DB: list tên bảng NGAY, rồi ĐẾM count nền (đừng compare full vội) ---
        // Lazy-load rows dùng options chỉ giữ TopN + IncludeIdenticalRows (khớp lúc đếm).
        _lastLoadOptions = new DataCompareOptions { TopN = options.TopN, IncludeIdenticalRows = options.IncludeIdenticalRows };

        SetBusy(true, "Loading table list...");
        List<string> common;
        try
        {
            common = await Task.Run(() => svc.GetCommonTables(source, target));
        }
        catch (System.Exception ex)
        {
            MessageBox.Show(ex.Message, "Compare failed", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText.Text = "Failed.";
            SetBusy(false, null);
            return;
        }

        // Giới hạn theo bảng đã chọn ở Edit Project (nếu có).
        if (_selectedTables is { Count: > 0 })
        {
            var pick = new HashSet<string>(_selectedTables, System.StringComparer.OrdinalIgnoreCase);
            common = common.Where(t => pick.Contains(t)).ToList();
        }

        foreach (var t in common)
            _tables.Add(new TableSummaryVm(t));

        SetBusy(false, $"{common.Count} table(s). Đang đếm khác biệt ...");
        UpdateSelectedCount();

        // Đếm nền từng bảng, cập nhật count dần. UI vẫn tương tác được (chọn bảng để full-compare).
        // Đếm SONG SONG tối đa 4 bảng (NOLOCK nên không block prod; nhanh hơn nhiều so với tuần tự).
        var countOpts = _lastLoadOptions;
        using var sem = new System.Threading.SemaphoreSlim(4);
        var snapshot = _tables.ToList();
        int done = 0;
        var tasks = snapshot.Select(async vm =>
        {
            await sem.WaitAsync();
            try
            {
                if (gen != _compareGen) return;
                DataTableSummary s;
                try { s = await Task.Run(() => svc.CompareCounts(source, target, vm.TableKey, countOpts)); }
                catch (System.Exception ex) { s = new DataTableSummary { TableKey = vm.TableKey, Failed = true, Error = ex.Message }; }

                if (gen != _compareGen) return;
                // Continuation chạy trên UI thread (đã capture context) → cập nhật VM an toàn.
                vm.SetSummary(s);
                done++;
                StatusText.Text = $"Đang đếm... {done}/{snapshot.Count} bảng";
                UpdateSelectedCount();
            }
            finally { sem.Release(); }
        }).ToList();

        await Task.WhenAll(tasks);

        if (gen != _compareGen) return;
        int withDiff = _tables.Count(t => t.HasDifferences);
        // KHÔNG auto-select bảng nào — auto-load bảng to (vd Address 30k dòng) làm app "quay" mãi.
        // Để user tự chọn bảng cần xem → mới full-compare bảng đó.
        StatusText.Text = $"Xong. {_tables.Count} bảng, {withDiff} khác biệt. Chọn 1 bảng để xem chi tiết.";
        UpdateSelectedCount();
    }

    private static DataTableSummary SummaryFromResult(DataComparisonResult r) => new()
    {
        TableKey = r.TableKey,
        OnlyInSource = r.CountOnlyInSource,
        Different = r.CountDifferent,
        OnlyInTarget = r.CountOnlyInTarget,
        Identical = r.CountIdentical,
        KeyKind = r.KeyKind,
        KeyColumns = r.KeyColumns,
    };

    // ====================== Chọn bảng → lazy full-compare rows ======================

    private async void TablesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TablesGrid.SelectedItem is TableSummaryVm vm)
            await EnsureLoadedAndShow(vm);
    }

    private async Task EnsureLoadedAndShow(TableSummaryVm vm)
    {
        if (!vm.Loaded)
        {
            DbConnectionInfo source = CompareSource();
            DbConnectionInfo target = CompareTarget();
            if (!Validate(source, "Source") || !Validate(target, "Target")) return;

            SetBusy(true, $"Comparing {vm.TableKey} ...");
            try
            {
                var opt = _lastLoadOptions;
                var res = await Task.Run(() => new DataCompareService().Compare(source, target, vm.TableKey, opt));
                vm.SetResult(res);
                BuildRows(vm, res);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message, "Compare failed", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "Failed.";
                SetBusy(false, null);
                return;
            }
            finally { SetBusy(false, null); }
        }
        LoadTableRows(vm);
    }

    /// <summary>Dựng DataRowVm cho rows của bảng (gọi sau khi đã có Result).</summary>
    private void BuildRows(TableSummaryVm vm, DataComparisonResult res)
    {
        foreach (var r in vm.Rows) r.PropertyChanged -= RowVm_PropertyChanged; // hygiene
        vm.Rows.Clear();
        foreach (var row in res.Rows)
        {
            var rvm = new DataRowVm(row, res.AllColumns);
            rvm.PropertyChanged += RowVm_PropertyChanged;
            vm.Rows.Add(rvm);
        }
    }

    /// <summary>Nạp rows của 1 bảng (đã loaded) vào DiffGrid.</summary>
    private void LoadTableRows(TableSummaryVm vm)
    {
        if (vm.Result == null) return;
        _current = vm;

        RebuildValueColumns(vm.Result.AllColumns);

        _rowsView = CollectionViewSource.GetDefaultView(vm.Rows);
        _rowsView.Filter = RowFilter;
        // KHÔNG group rows: group "Only in Source" có thể chứa hàng chục nghìn dòng → phá vỡ
        // row virtualization (WPF render hết). List phẳng + lọc bằng filter tabs (giống Redgate) → nhanh.
        _rowsView.GroupDescriptions.Clear();
        DiffGrid.ItemsSource = _rowsView;

        _groupChecks.Clear();
        _groupCounts.Clear();
        _rowsView.Refresh();

        SidePanelTitle.Text = $"{vm.TableKey}    ({vm.Result.Rows.Count} row(s) · {vm.KeyKindText})";
        UpdateSelectedCount();
    }

    // ====================== ROW FILTER ======================

    private bool RowFilter(object obj)
    {
        if (obj is not DataRowVm vm) return false;
        return (FilterCombo.SelectedIndex) switch
        {
            1 => vm.Status == DiffStatus.Different,
            2 => vm.Status == DiffStatus.OnlyInSource,
            3 => vm.Status == DiffStatus.OnlyInTarget,
            4 => vm.Status == DiffStatus.Identical,
            _ => true,
        };
    }

    private void FilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        => _rowsView?.Refresh();

    // ----- Tìm kiếm bảng (lọc danh sách bảng ở grid trên theo tên) -----
    private string _tableSearch = "";

    private bool TableSearchFilter(object o)
        => o is TableSummaryVm t &&
           (_tableSearch.Length == 0 || t.TableKey.IndexOf(_tableSearch, System.StringComparison.OrdinalIgnoreCase) >= 0);

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _tableSearch = SearchBox.Text?.Trim() ?? "";
        _tablesView?.Refresh();
    }

    private void FilterTab_Checked(object sender, RoutedEventArgs e)
    {
        if (FilterCombo == null) return;
        if (sender is RadioButton rb && int.TryParse(rb.Tag?.ToString(), out int idx))
            FilterCombo.SelectedIndex = idx;
    }

    private void DiffGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Row được bôi sáng bằng RowStyle IsSelected; không cần xử lý thêm.
    }

    private static string StatusTextOf(DiffStatus status) => status switch
    {
        DiffStatus.OnlyInSource => "Only in Source",
        DiffStatus.OnlyInTarget => "Only in Target",
        DiffStatus.Different => "Different",
        _ => "Identical",
    };

    // ====================== Dynamic value columns (S)/(T) ======================

    private const int FixedColumnCount = 1; // chỉ cột Include là cố định (đã bỏ Status)

    // Hồng rõ cho ô khác biệt (đậm hơn #FCE3EC cũ để dễ thấy).
    private static readonly Brush DiffPinkBrush = MakeFrozen(0xF6, 0xAE, 0xCB);
    private static Brush MakeFrozen(byte r, byte g, byte b)
    {
        var br = new SolidColorBrush(Color.FromRgb(r, g, b));
        br.Freeze();
        return br;
    }

    /// <summary>
    /// Sinh cột giá trị động: mỗi cột nguồn → "&lt;col&gt; (S)" + "&lt;col&gt; (T)".
    /// Ô bình thường TRẮNG; ô khác biệt → HỒNG rõ + đậm. Cam dành cho dòng select.
    /// </summary>
    private void RebuildValueColumns(System.Collections.Generic.IList<string> allColumns)
    {
        for (int i = DiffGrid.Columns.Count - 1; i >= FixedColumnCount; i--)
            DiffGrid.Columns.RemoveAt(i);

        if (allColumns == null) return;

        for (int i = 0; i < allColumns.Count; i++)
        {
            string col = allColumns[i];
            DiffGrid.Columns.Add(BuildValueColumn($"{col} (S)", $"Cells[{i}].Source", i));
            DiffGrid.Columns.Add(BuildValueColumn($"{col} (T)", $"Cells[{i}].Target", i));
        }
    }

    private static DataGridTemplateColumn BuildValueColumn(string header, string cellPath, int idx)
    {
        var factory = new FrameworkElementFactory(typeof(TextBlock));
        factory.SetBinding(TextBlock.TextProperty, new Binding(cellPath));
        factory.SetValue(TextBlock.PaddingProperty, new Thickness(4, 0, 4, 0));
        factory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
        factory.SetBinding(TextBlock.ToolTipProperty, new Binding(cellPath));

        // Ô khác biệt (Cells[idx].Different=true) → nền hồng rõ + chữ đậm.
        var style = new Style(typeof(TextBlock));
        var trig = new DataTrigger { Binding = new Binding($"Cells[{idx}].Different"), Value = true };
        trig.Setters.Add(new Setter(TextBlock.BackgroundProperty, DiffPinkBrush));
        trig.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.SemiBold));
        style.Triggers.Add(trig);
        factory.SetValue(FrameworkElement.StyleProperty, style);

        return new DataGridTemplateColumn
        {
            Header = header,
            Width = 130,
            IsReadOnly = true,
            CellTemplate = new DataTemplate { VisualTree = factory },
        };
    }

    /// <summary>Tính width cột "Table" để lấp đầy (DataGrid có grouping làm cột Width="*" bị co).</summary>
    private void TablesGrid_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (TableNameCol == null) return;
        double total = TablesGrid.ActualWidth;
        if (total <= 0) return;
        const double others = 40 + 70 + 95 + 80 + 95 + 80; // Include+Type+SourceOnly+Different+TargetOnly+Identical
        double w = total - others - 26; // chừa biên + scrollbar dọc
        TableNameCol.Width = new DataGridLength(w < 160 ? 160 : w);
    }

    // ====================== Deploy / Generate ======================

    /// <summary>Sinh script gộp cho mọi (Result, rows) đã chọn.</summary>
    private static string BuildCombinedSyncScript(
        DbConnectionInfo target, List<(DataComparisonResult Result, List<DataDiffRow> Rows)> picked)
    {
        var svc = new DataCompareService();
        var sb = new StringBuilder();
        foreach (var x in picked)
        {
            sb.AppendLine($"-- ===== {x.Result.TableKey}  ({x.Rows.Count} row(s)) =====");
            sb.AppendLine(svc.GenerateSyncScript(target, x.Result, x.Rows));
            sb.AppendLine();
        }
        return sb.ToString();
    }

    /// <summary>
    /// Gom rows để deploy theo các bảng Include + có khác biệt:
    ///  - bảng đã loaded → các row Include của nó (snapshot trên UI thread);
    ///  - bảng chưa loaded → full-compare nền, lấy tất cả row khác biệt.
    /// </summary>
    private async Task<List<(DataComparisonResult Result, List<DataDiffRow> Rows)>> CollectForDeployAsync(
        DbConnectionInfo source, DbConnectionInfo target)
    {
        var includedDiff = _tables.Where(t => t.Include && t.HasDifferences).ToList();

        var loaded = includedDiff.Where(t => t.Loaded)
            .Select(t => (t.Result!, Rows: t.Rows.Where(r => r.Include).Select(r => r.Row).ToList()))
            .Where(x => x.Rows.Count > 0)
            .ToList();

        var toLoad = includedDiff.Where(t => !t.Loaded).Select(t => t.TableKey).ToList();
        var opt = _lastLoadOptions;

        var extra = await Task.Run(() =>
        {
            var svc = new DataCompareService();
            var list = new List<(DataComparisonResult, List<DataDiffRow>)>();
            foreach (var tk in toLoad)
            {
                try
                {
                    var r = svc.Compare(source, target, tk, opt);
                    if (r.Rows.Count > 0) list.Add((r, r.Rows.ToList()));
                }
                catch { /* bỏ bảng lỗi */ }
            }
            return list;
        });

        return loaded.Concat(extra).ToList();
    }

    private async void DeployButton_Click(object sender, RoutedEventArgs e)
    {
        if (_tables.Count == 0)
        {
            MessageBox.Show("Run Compare first.", "No comparison", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        DbConnectionInfo source = CompareSource();
        DbConnectionInfo target = CompareTarget();
        if (!Validate(target, "Target")) return;

        if (!_tables.Any(t => t.Include && t.HasDifferences))
        {
            MessageBox.Show("Chưa chọn bảng nào (tick Include ở bảng có khác biệt).",
                "Nothing to deploy", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        SetBusy(true, "Preparing deployment...");
        string script;
        List<(DataComparisonResult Result, List<DataDiffRow> Rows)> picked;
        try
        {
            picked = await CollectForDeployAsync(source, target);
            if (picked.Count == 0)
            {
                SetBusy(false, null);
                MessageBox.Show("No rows selected (Include).", "Nothing to deploy",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            script = await Task.Run(() => BuildCombinedSyncScript(target, picked));
            ScriptBox.Text = script;
        }
        catch (System.Exception ex)
        {
            MessageBox.Show(ex.Message, "Generate failed", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText.Text = "Failed.";
            SetBusy(false, null);
            return;
        }
        finally { SetBusy(false, null); }

        int rowCount = picked.Sum(x => x.Rows.Count);
        string summary = $"{rowCount} row(s) across {picked.Count} table(s)";
        var objects = picked
            .SelectMany(x => x.Rows.Select(r => $"{x.Result.TableKey} · {StatusTextOf(r.Status)}: {r.Key}"))
            .Take(1000).ToList();

        System.Func<string, string> makeBackup = suffix =>
        {
            var svc = new DataCompareService();
            var sb = new StringBuilder();
            foreach (var x in picked)
                sb.AppendLine(svc.GenerateBackupScript(x.Result.TableKey, suffix));
            return sb.ToString();
        };

        var wizard = new DeploymentWizard(
            target, script, warnings: null, summary, objects, makeBackupScript: makeBackup)
        {
            Owner = Window.GetWindow(this),
        };
        wizard.ShowDialog();

        if (wizard.Deployed) StatusText.Text = "Deployed via wizard.";
        if (wizard.ShouldRecompare) CompareButton_Click(sender, e);
    }

    private async void GenerateButton_Click(object sender, RoutedEventArgs e)
    {
        if (_tables.Count == 0)
        {
            MessageBox.Show("Run Compare first.", "No comparison", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        DbConnectionInfo source = CompareSource();
        DbConnectionInfo target = CompareTarget();
        if (!Validate(target, "Target")) return;

        SetBusy(true, "Generating sync script...");
        try
        {
            var picked = await CollectForDeployAsync(source, target);
            if (picked.Count == 0)
            {
                MessageBox.Show("No rows selected (Include).", "Nothing to sync", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            string script = await Task.Run(() => BuildCombinedSyncScript(target, picked));
            ScriptBox.Text = script;
            StatusText.Text = $"Script generated ({picked.Sum(x => x.Rows.Count)} row(s), {picked.Count} table(s)).";
        }
        catch (System.Exception ex)
        {
            MessageBox.Show(ex.Message, "Generate failed", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText.Text = "Failed.";
        }
        finally { SetBusy(false, null); }
    }

    // ====================== Selection: row / table / group ======================

    private void RowVm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(DataRowVm.Include)) return;
        UpdateSelectedCount();
        RefreshGroupStates();
    }

    /// <summary>Banner = số bảng Include / tổng bảng khác biệt; master rows = bảng đang chọn; master tables = mọi bảng.</summary>
    private void UpdateSelectedCount()
    {
        int diffTables = _tables.Count(t => t.HasDifferences);
        int incl = _tables.Count(t => t.Include && t.HasDifferences);
        SelectedCountText.Text = $"{incl} of {diffTables} tables";

        // Master rows-grid = tristate theo BẢNG ĐANG CHỌN (chỉ khi đã loaded).
        if (HeaderCheckBox != null)
        {
            if (_current?.Loaded == true)
            {
                int ct = _current.Rows.Count;
                int cs = _current.Rows.Count(r => r.Include);
                HeaderCheckBox.IsChecked = (ct == 0 || cs == 0) ? false : (cs == ct ? true : (bool?)null);
            }
            else HeaderCheckBox.IsChecked = false;
        }

        // Master tables-grid = tristate theo Include trong các bảng khác biệt.
        if (TablesHeaderCheck != null)
            TablesHeaderCheck.IsChecked = (diffTables == 0 || incl == 0) ? false : (incl == diffTables ? true : (bool?)null);

        RefreshGroupStates();
    }

    private void HeaderCheckBox_Click(object sender, RoutedEventArgs e)
    {
        if (_current?.Loaded != true) return;
        bool toAll = HeaderCheckBox.IsChecked == true;
        foreach (var r in _current.Rows) r.Include = toAll;
        UpdateSelectedCount();
    }

    /// <summary>Master tables-grid → tick/bỏ Include cho mọi bảng có khác biệt.</summary>
    private void TablesHeaderCheck_Click(object sender, RoutedEventArgs e)
    {
        bool toAll = TablesHeaderCheck.IsChecked == true;
        foreach (var t in _tables.Where(t => t.HasDifferences)) t.Include = toAll;
        UpdateSelectedCount();
    }

    /// <summary>Tick Include 1 bảng (binding TwoWay đã set) → cập nhật đếm.</summary>
    private void TableInclude_Click(object sender, RoutedEventArgs e) => UpdateSelectedCount();

    // ----- Group header (rows-grid): tristate checkbox + đếm sel/tổng -----
    private readonly List<CheckBox> _groupChecks = new();
    private readonly List<TextBlock> _groupCounts = new();

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

    private void RefreshGroupStates()
    {
        _groupChecks.RemoveAll(cb => !cb.IsLoaded);
        _groupCounts.RemoveAll(tb => !tb.IsLoaded);

        foreach (var cb in _groupChecks)
        {
            if (cb.DataContext is not CollectionViewGroup g) continue;
            int t = g.Items.OfType<DataRowVm>().Count();
            int s = g.Items.OfType<DataRowVm>().Count(v => v.Include);
            cb.IsChecked = (t == 0 || s == 0) ? false : (s == t ? true : (bool?)null);
        }
        foreach (var tb in _groupCounts)
        {
            if (tb.DataContext is not CollectionViewGroup g) continue;
            int t = g.Items.OfType<DataRowVm>().Count();
            int s = g.Items.OfType<DataRowVm>().Count(v => v.Include);
            tb.Text = $"{s}/{t}";
        }
    }

    private void GroupCheck_Click(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox cb && cb.DataContext is CollectionViewGroup grp)
        {
            bool val = cb.IsChecked == true;
            foreach (var it in grp.Items)
                if (it is DataRowVm vm) vm.Include = val;
            UpdateSelectedCount();
        }
    }

    private void SelectAllButton_Click(object sender, RoutedEventArgs e)
    {
        if (_current?.Loaded != true) return;
        foreach (var row in _current.Rows) row.Include = true;
        UpdateSelectedCount();
    }

    private void SelectNoneButton_Click(object sender, RoutedEventArgs e)
    {
        if (_current?.Loaded != true) return;
        foreach (var row in _current.Rows) row.Include = false;
        UpdateSelectedCount();
    }

    // ====================== Action bar / banner ======================

    private void OptionsButton_Click(object sender, RoutedEventArgs e)
        => OptionsPopup.IsOpen = !OptionsPopup.IsOpen;

    private void Banner_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount != 2) return;
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

    /// <summary>Click bất kỳ chỗ nào trên header nhóm → đóng/mở nhóm (trừ checkbox/toggle).</summary>
    private void GroupHeader_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is System.Windows.DependencyObject d &&
            FindAncestor<System.Windows.Controls.Primitives.ToggleButton>(d) != null)
            return; // click trúng checkbox/toggle → để tự xử lý
        if (sender is System.Windows.DependencyObject host)
        {
            var toggle = FindDescendant<System.Windows.Controls.Primitives.ToggleButton>(host);
            if (toggle != null) toggle.IsChecked = !(toggle.IsChecked ?? false);
        }
    }

    private static T? FindDescendant<T>(System.Windows.DependencyObject root) where T : class
    {
        int n = System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < n; i++)
        {
            var c = System.Windows.Media.VisualTreeHelper.GetChild(root, i);
            if (c is T t) return t;
            var f = FindDescendant<T>(c);
            if (f != null) return f;
        }
        return null;
    }

    private void SwapButton_Click(object sender, RoutedEventArgs e)
    {
        // KHÔNG đổi vị trí server — chỉ đảo chiều (vai trò + chevron + chiều deploy).
        _reversed = !_reversed;
        UpdateBanner();
        StatusText.Text = _reversed ? "Đảo chiều: deploy ⬅ (phải → trái)." : "Chiều deploy: ➡ (trái → phải).";
        // BẮT BUỘC re-compare theo chiều mới (CompareButton tự clear _tables nên không deploy nhầm kết quả cũ).
        if (_tables.Count > 0) CompareButton_Click(sender, e);
    }

    // ====================== Load tables (Options combo) ======================

    private async void LoadTablesButton_Click(object sender, RoutedEventArgs e)
    {
        DbConnectionInfo source = CompareSource();
        if (!Validate(source, "Source")) return;

        SetBusy(true, "Loading tables...");
        try
        {
            List<string> tables = await Task.Run(() => new DataCompareService().GetComparableTables(source));
            TableCombo.ItemsSource = tables;
            StatusText.Text = $"{tables.Count} table(s) loaded. (Bỏ trống combo = so cả DB)";
        }
        catch (System.Exception ex)
        {
            MessageBox.Show(ex.Message, "Load tables failed", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText.Text = "Failed.";
        }
        finally { SetBusy(false, null); }
    }

    // ====================== Execute / Copy / Save script ======================

    private async void ExecuteButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ScriptBox.Text))
        {
            MessageBox.Show("No script to execute. Generate a sync script first.",
                "Nothing to execute", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        DbConnectionInfo target = CompareTarget();
        if (!Validate(target, "Target")) return;

        var confirm = MessageBox.Show(
            $"This will MODIFY the Target database:\n\n    {target}\n\n" +
            "The script runs inside a single transaction (rollback on error).\n" +
            "Have you reviewed the script?\n\nProceed?",
            "Confirm execute on Target",
            MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
        if (confirm != MessageBoxResult.Yes) return;

        string script = ScriptBox.Text;

        SetBusy(true, "Executing on target...");
        try
        {
            ScriptExecutionResult result = await Task.Run(() => new SqlScriptExecutor().Execute(target, script));
            MessageBox.Show(
                result.Message,
                result.Success ? "Execution succeeded" : "Execution failed",
                MessageBoxButton.OK,
                result.Success ? MessageBoxImage.Information : MessageBoxImage.Error);
            StatusText.Text = result.Success ? $"Executed. {result.BatchesExecuted} batch(es)." : "Execution failed.";
        }
        catch (System.Exception ex)
        {
            MessageBox.Show(ex.Message, "Execute failed", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText.Text = "Failed.";
        }
        finally { SetBusy(false, null); }
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
            FileName = "data_sync.sql",
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

    // ====================== View details (1 row) ======================

    private void DiffGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        // Double-click KHÔNG mở View Details nữa (dùng nút 🔍 View Details nếu cần).
    }

    private void ViewDetailsButton_Click(object sender, RoutedEventArgs e) => OpenDetails();

    private void OpenDetails()
    {
        if (DiffGrid.SelectedItem is not DataRowVm vm)
        {
            MessageBox.Show("Select a row first.", "No selection", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (_result == null)
        {
            MessageBox.Show("Run Compare first.", "No comparison", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        DiffViewerWindow.ShowDataRow(vm.Row, _result.AllColumns, Window.GetWindow(this));
    }

    // ====================== Project save/load ======================

    private void SaveProjectButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dlg = new SaveFileDialog
            {
                Filter = "Data Compare project (*.sqldcmp)|*.sqldcmp|JSON (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = ".sqldcmp",
                FileName = "comparison.sqldcmp",
            };
            if (dlg.ShowDialog() != true) return;

            var project = new ComparisonProject
            {
                Kind = "Data",
                Source = CompareSource(),
                Target = CompareTarget(),
                SelectedTable = TableCombo.SelectedItem as string,
            };
            ComparisonProjectStore.Save(dlg.FileName, project);
            RecentProjectStore.Add(dlg.FileName, "Data");
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
                Filter = "Data Compare project (*.sqldcmp)|*.sqldcmp|JSON (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = ".sqldcmp",
            };
            if (dlg.ShowDialog() != true) return;

            ComparisonProject project = ComparisonProjectStore.Load(dlg.FileName);
            if (!string.Equals(project.Kind, "Data", System.StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(
                    $"This project is of kind '{project.Kind}', not 'Data'. Connections will still be loaded.",
                    "Kind mismatch", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            SourcePanel.SetConnectionInfo(project.Source);
            TargetPanel.SetConnectionInfo(project.Target);
            _reversed = false;
            UpdateBanner();

            if (!string.IsNullOrWhiteSpace(project.SelectedTable))
                SetComboTable(project.SelectedTable!);

            RecentProjectStore.Add(dlg.FileName, "Data");
            ProjectTitleChanged?.Invoke(System.IO.Path.GetFileNameWithoutExtension(dlg.FileName));
            StatusText.Text = $"Project loaded: {dlg.FileName}";
        }
        catch (System.Exception ex)
        {
            MessageBox.Show(ex.Message, "Load project failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SetComboTable(string table)
    {
        var items = (TableCombo.ItemsSource as System.Collections.Generic.IEnumerable<string>)?.ToList()
                    ?? new System.Collections.Generic.List<string>();
        if (!items.Contains(table))
        {
            items.Add(table);
            TableCombo.ItemsSource = items;
        }
        TableCombo.SelectedItem = table;
    }

    // ====================== Options + report ======================

    private DataCompareOptions BuildCompareOptions(out string? error)
    {
        error = null;

        string? where = string.IsNullOrWhiteSpace(WhereBox.Text) ? null : WhereBox.Text.Trim();

        int? topN = null;
        if (!string.IsNullOrWhiteSpace(TopNBox.Text))
        {
            if (!int.TryParse(TopNBox.Text.Trim(), out int n) || n <= 0)
            {
                error = "Top-N phải là số nguyên dương (hoặc để trống = tất cả).";
                return new DataCompareOptions();
            }
            topN = n;
        }

        List<string>? customKeys = null;
        if (!string.IsNullOrWhiteSpace(CustomKeyBox.Text))
        {
            var keys = CustomKeyBox.Text
                .Split(';', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries)
                .ToList();
            if (keys.Count > 0) customKeys = keys;
        }

        return new DataCompareOptions
        {
            WhereClause = where,
            TopN = topN,
            CustomKeyColumns = customKeys,
        };
    }

    private void ExportReportButton_Click(object sender, RoutedEventArgs e)
    {
        if (_result == null)
        {
            MessageBox.Show("Chọn 1 bảng (đã Compare) để export.", "No comparison",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dlg = new SaveFileDialog
        {
            Filter = "HTML report (*.html)|*.html|CSV report (*.csv)|*.csv",
            DefaultExt = ".html",
            FileName = "data_comparison.html",
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            string ext = System.IO.Path.GetExtension(dlg.FileName).ToLowerInvariant();
            string content = ext == ".csv" ? DataReport.ToCsv(_result) : DataReport.ToHtml(_result);
            System.IO.File.WriteAllText(dlg.FileName, content);
            StatusText.Text = $"Report saved: {dlg.FileName}";
        }
        catch (System.Exception ex)
        {
            MessageBox.Show(ex.Message, "Export report failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

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

    // ====================== SAVED CONNECTIONS / session ======================

    public DbConnectionInfo GetSourceConnection() => CompareSource();
    public DbConnectionInfo GetTargetConnection() => CompareTarget();
    public string? GetSelectedTable() => TableCombo.SelectedItem as string;
    public void RunCompare() => CompareButton_Click(this, new RoutedEventArgs());

    /// <summary>Ctrl+F → focus ô tìm bảng (lọc danh sách bảng).</summary>
    public void FocusFind() { SearchBox.Focus(); SearchBox.SelectAll(); }

    public void SetSourceConnection(DbConnectionInfo conn)
    {
        if (conn == null) return;
        SourcePanel.SetConnectionInfo(conn);
        UpdateBanner();
        StatusText.Text = "Source applied from saved connection.";
    }

    public void SetTargetConnection(DbConnectionInfo conn)
    {
        if (conn == null) return;
        TargetPanel.SetConnectionInfo(conn);
        UpdateBanner();
        StatusText.Text = "Target applied from saved connection.";
    }

    public void ApplyProject(ComparisonProject p)
    {
        if (p == null) return;
        if (p.Source != null) SetSourceConnection(p.Source);
        if (p.Target != null) SetTargetConnection(p.Target);
        if (!string.IsNullOrWhiteSpace(p.SelectedTable)) SetComboTable(p.SelectedTable!);
        if (!string.IsNullOrWhiteSpace(p.WhereClause)) WhereBox.Text = p.WhereClause;
        _reversed = false;
        UpdateBanner();
        StatusText.Text = "Project applied.";
    }

    private void SetBusy(bool busy, string? status)
    {
        LoadTablesButton.IsEnabled = !busy;
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
        ExportReportButton.IsEnabled = !busy;
        BusyBar.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
        BusyText.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
        Mouse.OverrideCursor = busy ? Cursors.Wait : null;
        if (status != null) StatusText.Text = status;
    }

    // ====================== VM danh sách bảng (lazy) ======================

    /// <summary>
    /// VM 1 bảng: ban đầu chỉ có TÊN (đang đếm). SetSummary() khi có count. SetResult()+BuildRows khi
    /// user chọn (full compare). Include = tick để đưa vào deploy.
    /// </summary>
    public sealed class TableSummaryVm : INotifyPropertyChanged
    {
        public string TableKey { get; }
        public ObservableCollection<DataRowVm> Rows { get; } = new();

        private DataTableSummary? _summary;
        public DataComparisonResult? Result { get; private set; }
        public bool Loaded => Result != null;

        public TableSummaryVm(string tableKey) => TableKey = tableKey;

        public void SetSummary(DataTableSummary s)
        {
            _summary = s;
            _include = s.HasDifferences; // mặc định tick bảng có khác biệt
            RaiseAll();
        }

        public void SetResult(DataComparisonResult r)
        {
            Result = r;
            OnPropertyChanged(nameof(Loaded));
        }

        public bool HasSummary => _summary != null;
        public bool Failed => _summary?.Failed ?? false;

        public int CountOnlyInSource => _summary?.OnlyInSource ?? 0;
        public int CountDifferent => _summary?.Different ?? 0;
        public int CountOnlyInTarget => _summary?.OnlyInTarget ?? 0;
        public int CountIdentical => _summary?.Identical ?? 0;
        public bool HasDifferences => _summary != null && _summary.HasDifferences;

        public string TypeText =>
            _summary == null ? "Counting…" : (Failed ? "Error" : (_summary.HasDifferences ? "Different" : "Identical"));

        /// <summary>Thứ tự nhóm: Counting(0) → Different(1) → Error(2) → Identical(3, dưới cùng).</summary>
        public int TypeOrder => _summary == null ? 0 : (Failed ? 2 : (_summary.HasDifferences ? 1 : 3));

        /// <summary>Identical (bằng nhau) → KHÔNG cho tick (không có gì để deploy).</summary>
        public System.Windows.Visibility IncludeCheckVisibility =>
            HasDifferences ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

        // "…" khi chưa đếm xong; 0 → để trống cho đỡ rối.
        public string SourceOnlyText => _summary == null ? "…" : (CountOnlyInSource > 0 ? CountOnlyInSource.ToString() : "");
        public string DifferentText => _summary == null ? "…" : (CountDifferent > 0 ? CountDifferent.ToString() : "");
        public string TargetOnlyText => _summary == null ? "…" : (CountOnlyInTarget > 0 ? CountOnlyInTarget.ToString() : "");
        public string IdenticalText => _summary == null ? "…" : (CountIdentical > 0 ? CountIdentical.ToString() : "");

        public string KeyKindText => _summary == null
            ? "(đang đếm)"
            : _summary.KeyKind switch
            {
                DataKeyKind.PrimaryKey => $"Key: PK/custom ({string.Join(",", _summary.KeyColumns)})",
                DataKeyKind.UniqueIndex => $"Key: Unique ({string.Join(",", _summary.KeyColumns)})",
                _ => "Key: all columns (chỉ Insert/Delete)",
            };

        private bool _include;
        public bool Include
        {
            get => _include;
            set { if (_include != value) { _include = value; OnPropertyChanged(nameof(Include)); } }
        }

        private void RaiseAll()
        {
            OnPropertyChanged(nameof(TypeText));
            OnPropertyChanged(nameof(TypeOrder));
            OnPropertyChanged(nameof(IncludeCheckVisibility));
            OnPropertyChanged(nameof(SourceOnlyText));
            OnPropertyChanged(nameof(DifferentText));
            OnPropertyChanged(nameof(TargetOnlyText));
            OnPropertyChanged(nameof(IdenticalText));
            OnPropertyChanged(nameof(KeyKindText));
            OnPropertyChanged(nameof(HasDifferences));
            OnPropertyChanged(nameof(Include));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
