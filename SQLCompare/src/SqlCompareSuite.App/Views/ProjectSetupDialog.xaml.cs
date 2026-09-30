using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using SqlCompareSuite.Core.Execution;
using SqlCompareSuite.Core.Models;
using SqlCompareSuite.Core.Project;

namespace SqlCompareSuite.App.Views;

/// <summary>
/// Dialog modal cấu hình kết nối Source/Target (kiểu Redgate "Data sources").
/// Tái dùng logic SqlServerBrowser (Test + ListDatabases). Trả về connection khi OK.
/// </summary>
public partial class ProjectSetupDialog : Window
{
    /// <summary>Kết quả khi user bấm OK (null nếu Cancel).</summary>
    public DbConnectionInfo? ResultSource { get; private set; }
    public DbConnectionInfo? ResultTarget { get; private set; }

    /// <summary>Loại project khi Save: "Schema" | "Data" (caller set trước khi mở).</summary>
    public string ProjectKind { get; set; } = "Schema";

    /// <summary>Đường dẫn file project hiện tại (nếu đã Save/Open) — Save sẽ ghi đè đây.</summary>
    public string? ProjectPath { get; set; }

    /// <summary>
    /// Bảng được chọn để compare (tab Tables &amp; Views). null = chưa chọn → so TẤT CẢ bảng.
    /// Caller đọc sau khi OK để giới hạn compare.
    /// </summary>
    public List<string>? SelectedTables { get; private set; }

    private readonly ObservableCollection<TablePick> _tablePicks = new();
    private List<string>? _initialSelected;

    public ProjectSetupDialog(DbConnectionInfo? source, DbConnectionInfo? target,
        IEnumerable<string>? selectedTables = null)
    {
        InitializeComponent();
        PopulateRecentServers();
        if (source != null) LoadInto(source, isSource: true);
        if (target != null) LoadInto(target, isSource: false);

        _initialSelected = selectedTables?.ToList();
        SelectedTables = _initialSelected;
        TablePickList.ItemsSource = _tablePicks;
    }

    /// <summary>Đổ danh sách server đã dùng gần đây vào 2 dropdown Server.</summary>
    private void PopulateRecentServers()
    {
        var recent = RecentServerStore.Load();
        SrcServerBox.ItemsSource = new List<string>(recent);
        TgtServerBox.ItemsSource = new List<string>(recent);
    }

    /// <summary>Ghi nhớ server vào "Recently used".</summary>
    private static void RememberServer(string? server) => RecentServerStore.Add(server);

    // ====================== LOAD/READ field ======================

    /// <summary>Đổ DbConnectionInfo vào field 1 bên.</summary>
    private void LoadInto(DbConnectionInfo conn, bool isSource)
    {
        if (isSource)
        {
            SrcServerBox.Text = conn.Server;
            SrcDatabaseBox.Text = conn.Database;
            if (conn.IntegratedSecurity) SrcWindowsAuthRadio.IsChecked = true;
            else SrcSqlAuthRadio.IsChecked = true;
            SrcUserBox.Text = conn.UserId;
            SrcPasswordBox.Password = conn.Password;
            bool sql = !conn.IntegratedSecurity;
            SrcUserBox.IsEnabled = sql;
            SrcPasswordBox.IsEnabled = sql;
            SrcSavePasswordChk.IsEnabled = sql;
        }
        else
        {
            TgtServerBox.Text = conn.Server;
            TgtDatabaseBox.Text = conn.Database;
            if (conn.IntegratedSecurity) TgtWindowsAuthRadio.IsChecked = true;
            else TgtSqlAuthRadio.IsChecked = true;
            TgtUserBox.Text = conn.UserId;
            TgtPasswordBox.Password = conn.Password;
            bool sql = !conn.IntegratedSecurity;
            TgtUserBox.IsEnabled = sql;
            TgtPasswordBox.IsEnabled = sql;
            TgtSavePasswordChk.IsEnabled = sql;
        }
    }

    /// <summary>Đọc DbConnectionInfo từ field 1 bên.</summary>
    private DbConnectionInfo ReadFrom(bool isSource)
    {
        if (isSource)
        {
            bool win = SrcWindowsAuthRadio.IsChecked == true;
            return new DbConnectionInfo
            {
                Server = (SrcServerBox.Text ?? "").Trim(),
                Database = (SrcDatabaseBox.Text ?? "").Trim(),
                IntegratedSecurity = win,
                UserId = win ? "" : SrcUserBox.Text.Trim(),
                Password = win ? "" : SrcPasswordBox.Password,
            };
        }
        else
        {
            bool win = TgtWindowsAuthRadio.IsChecked == true;
            return new DbConnectionInfo
            {
                Server = (TgtServerBox.Text ?? "").Trim(),
                Database = (TgtDatabaseBox.Text ?? "").Trim(),
                IntegratedSecurity = win,
                UserId = win ? "" : TgtUserBox.Text.Trim(),
                Password = win ? "" : TgtPasswordBox.Password,
            };
        }
    }

    // ====================== Auth radio toggle ======================

    private void SrcAuthRadio_Changed(object sender, RoutedEventArgs e)
    {
        if (SrcUserBox == null || SrcPasswordBox == null) return;
        bool sql = SrcSqlAuthRadio.IsChecked == true;
        SrcUserBox.IsEnabled = sql;
        SrcPasswordBox.IsEnabled = sql;
        SrcSavePasswordChk.IsEnabled = sql;
    }

    private void TgtAuthRadio_Changed(object sender, RoutedEventArgs e)
    {
        if (TgtUserBox == null || TgtPasswordBox == null) return;
        bool sql = TgtSqlAuthRadio.IsChecked == true;
        TgtUserBox.IsEnabled = sql;
        TgtPasswordBox.IsEnabled = sql;
        TgtSavePasswordChk.IsEnabled = sql;
    }

    // ====================== Refresh databases ======================

    private async void SrcRefreshButton_Click(object sender, RoutedEventArgs e)
        => await RefreshDatabases(isSource: true);

    private async void TgtRefreshButton_Click(object sender, RoutedEventArgs e)
        => await RefreshDatabases(isSource: false);

    private async Task RefreshDatabases(bool isSource)
    {
        DbConnectionInfo conn = ReadFrom(isSource);
        if (string.IsNullOrWhiteSpace(conn.Server))
        {
            MessageBox.Show("Server is required.", "Missing input",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ComboBox box = isSource ? SrcDatabaseBox : TgtDatabaseBox;
        SetBusy(true);
        try
        {
            List<string> dbs = await Task.Run(() => new SqlServerBrowser().ListDatabases(conn));
            string? current = box.Text;
            box.ItemsSource = dbs;
            if (!string.IsNullOrWhiteSpace(current)) box.Text = current;
            else if (dbs.Count > 0) box.SelectedIndex = 0;
            RememberServer(conn.Server); // kết nối được → nhớ server
        }
        catch (System.Exception ex)
        {
            MessageBox.Show(ex.Message, "Load databases failed",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    // ====================== Test ======================

    private async void SrcTestButton_Click(object sender, RoutedEventArgs e)
        => await TestConnection(isSource: true);

    private async void TgtTestButton_Click(object sender, RoutedEventArgs e)
        => await TestConnection(isSource: false);

    private async Task TestConnection(bool isSource)
    {
        DbConnectionInfo conn = ReadFrom(isSource);
        if (string.IsNullOrWhiteSpace(conn.Server))
        {
            MessageBox.Show("Server is required.", "Missing input",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        SetBusy(true);
        try
        {
            (bool ok, string message) = await Task.Run(() => new SqlServerBrowser().TestConnection(conn));
            if (ok) RememberServer(conn.Server); // test OK → nhớ server
            MessageBox.Show(message,
                ok ? "Connection OK" : "Connection failed",
                MessageBoxButton.OK,
                ok ? MessageBoxImage.Information : MessageBoxImage.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    // ====================== OK / Cancel ======================

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        ResultSource = ReadFrom(isSource: true);
        ResultTarget = ReadFrom(isSource: false);
        RememberServer(ResultSource.Server);
        RememberServer(ResultTarget.Server);
        // Bảng đã load + tick → giới hạn compare; chưa load → giữ lựa chọn cũ (null = all).
        SelectedTables = _tablePicks.Count > 0
            ? _tablePicks.Where(p => p.Include).Select(p => p.Name).ToList()
            : _initialSelected;
        DialogResult = true;
        Close();
    }

    // ====================== Tab Tables & Views ======================

    /// <summary>Load danh sách bảng chung 2 DB vào checklist (cần Server + Database 2 bên).</summary>
    private async void LoadTablesTab_Click(object sender, RoutedEventArgs e)
    {
        var src = ReadFrom(isSource: true);
        var tgt = ReadFrom(isSource: false);
        if (string.IsNullOrWhiteSpace(src.Server) || string.IsNullOrWhiteSpace(src.Database) ||
            string.IsNullOrWhiteSpace(tgt.Server) || string.IsNullOrWhiteSpace(tgt.Database))
        {
            MessageBox.Show("Cần nhập Server + Database cho cả Source và Target (tab Data sources) trước.",
                "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Giữ lại lựa chọn trước (nếu có) để restore tick theo tên.
        bool restore = _initialSelected != null || _tablePicks.Count > 0;
        var prev = new HashSet<string>(
            _initialSelected ?? _tablePicks.Where(p => p.Include).Select(p => p.Name),
            System.StringComparer.OrdinalIgnoreCase);

        SetBusy(true);
        try
        {
            List<string> common = await Task.Run(() =>
                new SqlCompareSuite.Core.Data.DataCompareService().GetCommonTables(src, tgt));

            _tablePicks.Clear();
            foreach (var t in common)
                _tablePicks.Add(new TablePick { Name = t, Include = !restore || prev.Contains(t) });

            _initialSelected = null; // đã chuyển sang checklist, không restore lại nữa
            TablesHint.Text = $"{common.Count} bảng chung. Đã chọn {_tablePicks.Count(p => p.Include)}.";
        }
        catch (System.Exception ex)
        {
            MessageBox.Show(ex.Message, "Load tables failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void TablesSelectAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var p in _tablePicks) p.Include = true;
        UpdateTablesHint();
    }

    private void TablesSelectNone_Click(object sender, RoutedEventArgs e)
    {
        foreach (var p in _tablePicks) p.Include = false;
        UpdateTablesHint();
    }

    private void UpdateTablesHint()
    {
        if (_tablePicks.Count > 0)
            TablesHint.Text = $"{_tablePicks.Count} bảng chung. Đã chọn {_tablePicks.Count(p => p.Include)}.";
    }

    /// <summary>1 dòng bảng trong checklist (tick để đưa vào compare).</summary>
    private sealed class TablePick : INotifyPropertyChanged
    {
        public string Name { get; set; } = "";
        private bool _include = true;
        public bool Include
        {
            get => _include;
            set { if (_include != value) { _include = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Include))); } }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    // ====================== Save / Save as / Swap ======================

    /// <summary>Tạo ComparisonProject từ field hiện tại (tôn trọng "Save password").</summary>
    private ComparisonProject BuildProject()
    {
        var src = ReadFrom(isSource: true);
        var tgt = ReadFrom(isSource: false);
        // Không tick "Save password" → không lưu mật khẩu.
        if (SrcSavePasswordChk.IsChecked != true) src.Password = "";
        if (TgtSavePasswordChk.IsChecked != true) tgt.Password = "";
        return new ComparisonProject { Kind = ProjectKind, Source = src, Target = tgt };
    }

    /// <summary>Save: ghi đè file hiện tại nếu có, ngược lại như Save as.</summary>
    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(ProjectPath)) { SaveAsButton_Click(sender, e); return; }
        try
        {
            ComparisonProjectStore.Save(ProjectPath!, BuildProject());
            RecentProjectStore.Add(ProjectPath!, ProjectKind); // → hiện vào panel Saved Projects
            MessageBox.Show($"Đã lưu: {ProjectPath}", "Save project",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (System.Exception ex)
        {
            MessageBox.Show(ex.Message, "Save failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>Save as: chọn file mới rồi lưu.</summary>
    private void SaveAsButton_Click(object sender, RoutedEventArgs e)
    {
        bool isData = string.Equals(ProjectKind, "Data", System.StringComparison.OrdinalIgnoreCase);
        string ext = isData ? "sqldcmp" : "sqlcmp";
        var dlg = new SaveFileDialog
        {
            Title = "Save project",
            Filter = isData
                ? "Data Compare project (*.sqldcmp)|*.sqldcmp|JSON (*.json)|*.json"
                : "Schema Compare project (*.sqlcmp)|*.sqlcmp|JSON (*.json)|*.json",
            DefaultExt = ext,
            FileName = $"{ReadFrom(true).Database}_vs_{ReadFrom(false).Database}.{ext}",
        };
        if (dlg.ShowDialog(this) != true) return;
        try
        {
            ComparisonProjectStore.Save(dlg.FileName, BuildProject());
            ProjectPath = dlg.FileName;
            RecentProjectStore.Add(dlg.FileName, ProjectKind); // → hiện vào panel Saved Projects
            MessageBox.Show($"Đã lưu: {dlg.FileName}", "Save project",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (System.Exception ex)
        {
            MessageBox.Show(ex.Message, "Save failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>Hoán đổi toàn bộ field Source ⇄ Target.</summary>
    private void SwapButton_Click(object sender, RoutedEventArgs e)
    {
        var src = ReadFrom(isSource: true);
        var tgt = ReadFrom(isSource: false);
        LoadInto(tgt, isSource: true);
        LoadInto(src, isSource: false);
    }

    private void SetBusy(bool busy)
    {
        SrcTestButton.IsEnabled = !busy;
        TgtTestButton.IsEnabled = !busy;
        SrcRefreshButton.IsEnabled = !busy;
        TgtRefreshButton.IsEnabled = !busy;
        OkButton.IsEnabled = !busy;
        SaveButton.IsEnabled = !busy;
        SaveAsButton.IsEnabled = !busy;
        SwapButton.IsEnabled = !busy;
        Mouse.OverrideCursor = busy ? Cursors.Wait : null;
    }
}
