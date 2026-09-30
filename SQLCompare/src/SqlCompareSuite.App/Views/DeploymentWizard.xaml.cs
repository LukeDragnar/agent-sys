using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using SqlCompareSuite.Core.Execution;
using SqlCompareSuite.Core.Models;

namespace SqlCompareSuite.App.Views;

/// <summary>
/// Deployment Wizard 3 bước (kiểu Redgate SQL Compare):
///   ① Deployment method  ② Dependencies  ③ Deploy.
/// View truyền sẵn: script, warnings, summary, target connection, danh sách object/row,
/// và optional makeBackupScript (chỉ Data Compare có; null → ẩn/disable backup).
///
/// Kết quả trả về view qua property: <see cref="Deployed"/> và <see cref="ShouldRecompare"/>.
/// </summary>
public partial class DeploymentWizard : Window
{
    private readonly string _script;
    private readonly List<string> _warnings;
    private readonly string _summary;
    private readonly DbConnectionInfo _target = new();
    private readonly IReadOnlyList<string> _objects;
    private readonly Func<string, string>? _makeBackupScript; // suffix → backup SQL (null nếu schema)

    private int _step = 1; // 1..3

    /// <summary>True nếu đã chạy deploy thành công lên target.</summary>
    public bool Deployed { get; private set; }

    /// <summary>True nếu user chọn "Recompare after deployment" và deploy đã chạy.</summary>
    public bool ShouldRecompare { get; private set; }

    /// <param name="target">Target connection sẽ chạy script.</param>
    /// <param name="script">Sync script đã sinh sẵn từ view.</param>
    /// <param name="warnings">Cảnh báo kèm theo (có thể null).</param>
    /// <param name="summary">Mô tả ngắn (counts) hiển thị tab Summary.</param>
    /// <param name="objects">Danh sách object/row sẽ deploy (cho Dependencies + Summary).</param>
    /// <param name="makeBackupScript">suffix → backup SQL; null nếu không hỗ trợ (Schema).</param>
    public DeploymentWizard(
        DbConnectionInfo target,
        string script,
        List<string>? warnings,
        string summary,
        IReadOnlyList<string>? objects = null,
        Func<string, string>? makeBackupScript = null)
    {
        InitializeComponent();

        _target = target ?? new DbConnectionInfo();
        _script = script ?? "";
        _warnings = warnings ?? new List<string>();
        _summary = summary ?? "";
        _objects = objects ?? Array.Empty<string>();
        _makeBackupScript = makeBackupScript;

        HeaderSourceText.Text = "Selected changes";
        HeaderTargetText.Text = _target?.ToString() ?? "";

        // Backup chỉ hỗ trợ Data Compare (makeBackupScript != null).
        if (_makeBackupScript == null)
        {
            BackupChk.IsEnabled = false;
            BackupChk.ToolTip = "Chỉ hỗ trợ Data Compare.";
        }

        BuildDependencies();
        BuildSummary();
        BuildWarnings();
        RenderScript();

        GoToStep(1);
    }

    // ===================== STEPPER / NAVIGATION =====================

    private void GoToStep(int step)
    {
        _step = step;

        Page1.Visibility = step == 1 ? Visibility.Visible : Visibility.Collapsed;
        Page2.Visibility = step == 2 ? Visibility.Visible : Visibility.Collapsed;
        Page3.Visibility = step == 3 ? Visibility.Visible : Visibility.Collapsed;

        // Tô chip active (xanh).
        SetChip(Step1Chip, step == 1);
        SetChip(Step2Chip, step == 2);
        SetChip(Step3Chip, step == 3);

        BackButton.IsEnabled = step > 1;

        // Bước 3 → hiện nút primary (Deploy / Save & Close), ẩn Next.
        if (step == 3)
        {
            NextButton.Visibility = Visibility.Collapsed;
            PrimaryActionButton.Visibility = Visibility.Visible;
            bool deployMode = MethodToolRadio.IsChecked == true;
            PrimaryActionButton.Content = deployMode ? "Deploy" : "Save & Close";
            PrimaryActionButton.Background = deployMode
                ? new SolidColorBrush(Color.FromRgb(0x43, 0xA0, 0x47))
                : (Brush)FindResource("RgBlue");
        }
        else
        {
            NextButton.Visibility = Visibility.Visible;
            PrimaryActionButton.Visibility = Visibility.Collapsed;
        }
    }

    private void SetChip(Border chip, bool active)
    {
        chip.Background = active ? (Brush)FindResource("RgBlue") : Brushes.Transparent;
        // Cập nhật màu chữ trong chip cho dễ đọc.
        if (chip.Child is StackPanel sp)
        {
            foreach (var ch in sp.Children)
                if (ch is TextBlock tb)
                    tb.Foreground = active
                        ? Brushes.White
                        : new SolidColorBrush(Color.FromRgb(0xAA, 0xB2, 0xBA));
        }
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        // 3 → 1 (bỏ qua bước 2 khi back nhanh từ Deploy), 2 → 1.
        if (_step == 3) GoToStep(1);
        else if (_step == 2) GoToStep(1);
    }

    private void NextButton_Click(object sender, RoutedEventArgs e)
    {
        // 1 → 3 trực tiếp (bước 2 chỉ là thông tin, có thể xem qua Back nếu muốn).
        // Vẫn cho phép xem Dependencies: 1 → 2 → 3.
        if (_step == 1) GoToStep(2);
        else if (_step == 2) GoToStep(3);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = Deployed;
        Close();
    }

    // ===================== BUILD CONTENT =====================

    private void BuildDependencies()
    {
        DependenciesList.Items.Clear();
        if (_objects.Count == 0)
        {
            DependenciesHint.Text = "No dependencies.";
            return;
        }
        DependenciesHint.Text = $"{_objects.Count} object(s) selected for deployment:";
        foreach (var o in _objects) DependenciesList.Items.Add(o);
    }

    private void BuildSummary()
    {
        SummaryHeader.Text = string.IsNullOrWhiteSpace(_summary)
            ? $"{_objects.Count} object(s) to deploy."
            : _summary;
        SummaryList.Items.Clear();
        foreach (var o in _objects) SummaryList.Items.Add(o);
        if (_objects.Count == 0) SummaryList.Items.Add("(không có danh sách object chi tiết)");
    }

    private void BuildWarnings()
    {
        WarningsList.Items.Clear();
        if (_warnings.Count == 0)
        {
            WarningsHeader.Text = "No warnings";
            WarningsList.Items.Add("(không có cảnh báo)");
            return;
        }
        WarningsHeader.Text = $"⚠ {_warnings.Count} warning(s):";
        foreach (var w in _warnings) WarningsList.Items.Add(w);
    }

    /// <summary>Render script vào RichTextBox + syntax highlight. Script đã có header (wrapper) → KHÔNG thêm header nữa.</summary>
    private void RenderScript()
    {
        SqlDiffRenderer.HighlightInto(ScriptViewer, _script);
    }

    /// <summary>Lấy text thuần script (header + body) để Save/Copy.</summary>
    private string GetPlainScript()
    {
        var range = new TextRange(ScriptViewer.Document.ContentStart, ScriptViewer.Document.ContentEnd);
        return range.Text;
    }

    // ===================== STEP 1 controls =====================

    private void SaveCopyChk_Toggled(object sender, RoutedEventArgs e)
    {
        if (SaveCopyPanel != null)
            SaveCopyPanel.IsEnabled = SaveCopyChk.IsChecked == true;
    }

    private void BrowsePathButton_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog
        {
            Filter = "SQL script (*.sql)|*.sql|All files (*.*)|*.*",
            DefaultExt = ".sql",
            FileName = string.IsNullOrWhiteSpace(SaveCopyNameBox.Text) ? "deployment.sql" : SaveCopyNameBox.Text,
        };
        if (dlg.ShowDialog(this) == true)
        {
            SaveCopyPathBox.Text = dlg.FileName;
            SaveCopyNameBox.Text = System.IO.Path.GetFileName(dlg.FileName);
        }
    }

    // ===================== STEP 3 toolbar =====================

    private void SaveScriptButton_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog
        {
            Filter = "SQL script (*.sql)|*.sql|All files (*.*)|*.*",
            DefaultExt = ".sql",
            FileName = "deployment.sql",
        };
        if (dlg.ShowDialog(this) != true) return;
        try
        {
            System.IO.File.WriteAllText(dlg.FileName, GetPlainScript());
            MessageBox.Show($"Saved: {dlg.FileName}", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Save failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CopyScriptButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Clipboard.SetText(GetPlainScript());
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Copy failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void FindBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) FindNext();
    }

    private void FindButton_Click(object sender, RoutedEventArgs e) => FindNext();

    // Tìm chuỗi đơn giản trong RichTextBox, select kết quả tiếp theo (từ cuối lần chọn trước).
    private void FindNext()
    {
        string needle = FindBox.Text;
        if (string.IsNullOrEmpty(needle)) return;

        TextPointer start = ScriptViewer.Selection.IsEmpty
            ? ScriptViewer.Document.ContentStart
            : ScriptViewer.Selection.End;

        TextRange? found = FindFrom(start, needle);
        // Không thấy → quay lại từ đầu (wrap).
        found ??= FindFrom(ScriptViewer.Document.ContentStart, needle);

        if (found != null)
        {
            ScriptViewer.Selection.Select(found.Start, found.End);
            ScriptViewer.Focus();
        }
        else
        {
            MessageBox.Show($"Không tìm thấy: {needle}", "Find",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private static TextRange? FindFrom(TextPointer start, string needle)
    {
        TextPointer? ptr = start;
        while (ptr != null)
        {
            if (ptr.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
            {
                string text = ptr.GetTextInRun(LogicalDirection.Forward);
                int idx = text.IndexOf(needle, StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    TextPointer s = ptr.GetPositionAtOffset(idx);
                    TextPointer? e = s?.GetPositionAtOffset(needle.Length);
                    if (s != null && e != null) return new TextRange(s, e);
                }
            }
            ptr = ptr.GetNextContextPosition(LogicalDirection.Forward);
        }
        return null;
    }

    // ===================== PRIMARY ACTION (Deploy / Save & Close) =====================

    private async void PrimaryActionButton_Click(object sender, RoutedEventArgs e)
    {
        bool deployMode = MethodToolRadio.IsChecked == true;

        // ----- Method = Create script → chỉ lưu file rồi đóng -----
        if (!deployMode)
        {
            if (!SaveScriptCopyIfRequested(force: true)) return;
            DialogResult = false; // không deploy
            Close();
            return;
        }

        // ----- Method = Deploy → confirm rồi chạy -----
        var confirm = MessageBox.Show(
            $"This will MODIFY the Target database:\n\n    {_target}\n\n" +
            "Have you reviewed the deployment script?\n\nProceed?",
            "Confirm deploy on Target",
            MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
        if (confirm != MessageBoxResult.Yes) return;

        // Lưu copy nếu user bật (không bắt buộc với deploy mode).
        if (SaveCopyChk.IsChecked == true && !SaveScriptCopyIfRequested(force: false)) return;

        // Dựng script chạy: prepend backup nếu bật + hỗ trợ.
        string runScript = _script;
        string? backupInfo = null;
        if (BackupChk.IsChecked == true && _makeBackupScript != null)
        {
            string suffix = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string backup = _makeBackupScript(suffix);
            runScript = backup + Environment.NewLine + runScript;
            backupInfo = $"backup suffix: {suffix}";
        }

        DbConnectionInfo target = _target;
        SetBusy(true, "Deploying to target...");
        try
        {
            ScriptExecutionResult result = await Task.Run(() =>
                new SqlScriptExecutor().Execute(target, runScript));

            string msg = result.Message;
            if (result.Success && backupInfo != null) msg += $"\n\n{backupInfo}";

            MessageBox.Show(
                msg,
                result.Success ? "Deployment succeeded" : "Deployment failed",
                MessageBoxButton.OK,
                result.Success ? MessageBoxImage.Information : MessageBoxImage.Error);

            if (result.Success)
            {
                Deployed = true;
                ShouldRecompare = RecompareChk.IsChecked == true;
                DialogResult = true;
                Close();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Deploy failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SetBusy(false, null);
        }
    }

    /// <summary>
    /// Ghi file copy nếu "Save a copy" bật. force=true → bắt buộc (Create-script mode).
    /// Trả false nếu thiếu path khi bắt buộc (đã báo lỗi).
    /// </summary>
    private bool SaveScriptCopyIfRequested(bool force)
    {
        bool wants = SaveCopyChk.IsChecked == true || force;
        if (!wants) return true;

        string path = SaveCopyPathBox.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(path))
        {
            // Chưa có path → mở SaveFileDialog ngay.
            var dlg = new SaveFileDialog
            {
                Filter = "SQL script (*.sql)|*.sql|All files (*.*)|*.*",
                DefaultExt = ".sql",
                FileName = string.IsNullOrWhiteSpace(SaveCopyNameBox.Text) ? "deployment.sql" : SaveCopyNameBox.Text,
            };
            if (dlg.ShowDialog(this) != true)
            {
                if (force)
                {
                    MessageBox.Show("Bạn cần chọn nơi lưu script.", "Cần đường dẫn",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
                return true; // không bắt buộc → bỏ qua
            }
            path = dlg.FileName;
            SaveCopyPathBox.Text = path;
        }

        try
        {
            System.IO.File.WriteAllText(path, GetPlainScript());
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Save copy failed", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    private void SetBusy(bool busy, string? status)
    {
        BusyOverlay.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
        if (status != null) BusyText.Text = status;
        PrimaryActionButton.IsEnabled = !busy;
        BackButton.IsEnabled = !busy && _step > 1;
        CloseButton.IsEnabled = !busy;
        Mouse.OverrideCursor = busy ? Cursors.Wait : null;
    }
}
