using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using SqlCompareSuite.App.Views;
using SqlCompareSuite.Core.Models;
using SqlCompareSuite.Core.Project;

namespace SqlCompareSuite.App;

public partial class MainWindow : Window
{
    private int _schemaCounter;
    private int _dataCounter;

    // VM hiển thị 1 saved connection trong ListBox (giữ connection gốc để Apply).
    private sealed class SavedConnVm
    {
        public string Name { get; set; } = "";
        public string SubText { get; set; } = "";
        public DbConnectionInfo Connection { get; set; } = new();
    }

    private readonly ObservableCollection<SavedConnVm> _savedConns = new();

    // VM hiển thị 1 saved project trong ListBox (giữ Path + Kind để mở/xoá).
    private sealed class SavedProjectVm
    {
        public string Name { get; set; } = "";
        public string Kind { get; set; } = "Schema";
        public string Path { get; set; } = "";

        /// <summary>Group chứa project ("" = chưa nhóm).</summary>
        public string Group { get; set; } = "";
        /// <summary>Dòng giả lập của group rỗng (chỉ để hiện header + drop được).</summary>
        public bool IsPlaceholder { get; set; }
        /// <summary>Khoá nhóm hiển thị (rỗng → "(Chưa nhóm)").</summary>
        public string GroupDisplay => string.IsNullOrWhiteSpace(Group) ? "(Chưa nhóm)" : Group;

        /// <summary>Màu badge theo loại — KHỚP màu chip tab: Schema = rose, Data = lavender.</summary>
        public System.Windows.Media.Brush KindBrush =>
            string.Equals(Kind, "Schema", System.StringComparison.OrdinalIgnoreCase)
                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xE9, 0x7A, 0xA6))
                : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xB4, 0x93, 0xD6));
    }

    private readonly ObservableCollection<SavedProjectVm> _savedProjects = new();
    private readonly ObservableCollection<PinnedTabVm> _pinnedTabs = new();   // tab đã ghim (dòng trên)
    private double _lastPanelWidth = 220; // nhớ độ rộng panel để khôi phục khi mở lại

    /// <summary>VM 1 tab ghim trên dải ghim. IsSelected → tô như tab đang chọn.</summary>
    private sealed class PinnedTabVm : System.ComponentModel.INotifyPropertyChanged
    {
        public TabItem Tab { get; set; } = null!;
        public string Title { get; set; } = "";
        public string Kind { get; set; } = "Schema";
        public System.Windows.Media.Brush KindBrush =>
            string.Equals(Kind, "Schema", System.StringComparison.OrdinalIgnoreCase)
                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xE9, 0x7A, 0xA6))
                : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xB4, 0x93, 0xD6));
        // Icon ⚙/▦ — GIỐNG HỆT chip icon của tab thường (BuildHeader)
        public string KindIcon =>
            string.Equals(Kind, "Schema", System.StringComparison.OrdinalIgnoreCase) ? "⚙" : "▦";

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { if (_isSelected != value) { _isSelected = value; PropertyChanged?.Invoke(this, new(nameof(IsSelected))); } }
        }
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    }

    /// <summary>Phiên bản app — hiện trên title bar + About.</summary>
    private const string AppVersion = "1.8.7";

    public MainWindow()
    {
        InitializeComponent();
        VersionText.Text = $"v{AppVersion}";

        SavedConnList.ItemsSource = _savedConns;
        ReloadSavedConnections();

        // Nhóm Saved Projects theo Group (group "(Chưa nhóm)" cho project chưa gán).
        var projView = System.Windows.Data.CollectionViewSource.GetDefaultView(_savedProjects);
        projView.GroupDescriptions.Add(new System.Windows.Data.PropertyGroupDescription(nameof(SavedProjectVm.GroupDisplay)));
        projView.SortDescriptions.Add(new System.ComponentModel.SortDescription(nameof(SavedProjectVm.GroupDisplay), System.ComponentModel.ListSortDirection.Ascending));
        projView.SortDescriptions.Add(new System.ComponentModel.SortDescription(nameof(SavedProjectVm.Name), System.ComponentModel.ListSortDirection.Ascending));
        SavedProjectList.ItemsSource = projView;
        PinnedTabsStrip.ItemsSource = _pinnedTabs;
        Tabs.SelectionChanged += Tabs_SelectionChanged;   // đồng bộ highlight dải ghim
        ReloadSavedProjects();
        // Tự refresh khi có project mới được Save/Load/Remove ở bất kỳ đâu.
        RecentProjectStore.Changed += OnRecentProjectsChanged;

        RestoreSession();   // khôi phục tab phiên trước; trống → 1 tab Schema mặc định

        Closing += MainWindow_Closing;   // lưu session khi đóng
        Closed += (_, _) => RecentProjectStore.Changed -= OnRecentProjectsChanged;
        PreviewKeyDown += MainWindow_PreviewKeyDown;   // F5 = compare tab đang active
    }

    /// <summary>F5 → compare; Ctrl+F → focus ô tìm của tab đang active.</summary>
    private void MainWindow_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        // Ctrl+F → focus ô tìm
        if (e.Key == System.Windows.Input.Key.F &&
            (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) != 0)
        {
            switch (ActiveView())
            {
                case SchemaCompareView s: s.FocusFind(); e.Handled = true; break;
                case DataCompareView d: d.FocusFind(); e.Handled = true; break;
            }
            return;
        }

        if (e.Key != System.Windows.Input.Key.F5) return;
        switch (ActiveView())
        {
            case SchemaCompareView s: s.RunCompare(); e.Handled = true; break;
            case DataCompareView d: d.RunCompare(); e.Handled = true; break;
        }
    }

    /// <summary>Activity bar: nút 🔄 → compare tab đang active.</summary>
    private void ActivityRefresh_Click(object sender, RoutedEventArgs e)
    {
        switch (ActiveView())
        {
            case SchemaCompareView s: s.RunCompare(); break;
            case DataCompareView d: d.RunCompare(); break;
        }
    }

    /// <summary>Khôi phục các tab đang mở ở phiên trước (session). Trống → 1 tab Schema mặc định.</summary>
    private void RestoreSession()
    {
        var tabs = SessionStore.Load();
        if (tabs.Count == 0)
        {
            AddTab("Schema", new SchemaCompareView());
            return;
        }
        foreach (var t in tabs)
        {
            var project = new ComparisonProject
            {
                Kind = t.Kind, Source = t.Source, Target = t.Target, SelectedTable = t.SelectedTable,
            };
            string? title = string.IsNullOrWhiteSpace(t.Title) ? null : t.Title;
            TabItem tab;
            if (string.Equals(t.Kind, "Data", System.StringComparison.OrdinalIgnoreCase))
            {
                var v = new DataCompareView();
                tab = AddTab("Data", v, title);
                v.ApplyProject(project);
            }
            else
            {
                var v = new SchemaCompareView();
                tab = AddTab("Schema", v, title);
                v.ApplyProject(project);
            }
            if (t.Pinned) PinTab(tab);
        }
    }

    /// <summary>Lưu danh sách tab đang mở khi đóng app.</summary>
    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        var tabs = new System.Collections.Generic.List<SessionTab>();
        foreach (var item in Tabs.Items)
        {
            if (item is not TabItem ti) continue;
            string title = GetTabTitle(ti);
            if (ti.Content is DataCompareView dv)
                tabs.Add(new SessionTab
                {
                    Kind = "Data", Source = dv.GetSourceConnection(), Target = dv.GetTargetConnection(),
                    SelectedTable = dv.GetSelectedTable(), Title = title, Pinned = IsPinned(ti),
                });
            else if (ti.Content is SchemaCompareView sv)
                tabs.Add(new SessionTab
                {
                    Kind = "Schema", Source = sv.GetSourceConnection(), Target = sv.GetTargetConnection(),
                    Title = title, Pinned = IsPinned(ti),
                });
        }
        SessionStore.Save(tabs);
    }

    /// <summary>Lấy text tiêu đề của tab.</summary>
    private string GetTabTitle(TabItem tab)
    {
        if (tab.Header is StackPanel sp)
            foreach (var c in sp.Children)
                if (c is TextBlock tb) return tb.Text;
        return "";
    }

    // ====================== Danh sách tab (nút ☰) ======================

    /// <summary>Nút ☰ → mở popup liệt kê các tab đang mở (chip màu + tên + dấu tab hiện tại).</summary>
    private void TabListButton_Click(object sender, RoutedEventArgs e)
    {
        var items = new System.Collections.Generic.List<TabListItem>();
        foreach (var obj in Tabs.Items)
        {
            if (obj is not TabItem ti) continue;
            items.Add(new TabListItem
            {
                Title = GetTabTitle(ti),
                Kind = ti.Content is DataCompareView ? "Data" : "Schema",
                Tab = ti,
                IsCurrent = ReferenceEquals(ti, Tabs.SelectedItem),
            });
        }
        TabListBox.ItemsSource = items;
        TabListPopup.IsOpen = true;
    }

    /// <summary>Chọn 1 tab trong popup → active tab đó.</summary>
    private void TabListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TabListBox.SelectedItem is TabListItem it && it.Tab != null)
        {
            Tabs.SelectedItem = it.Tab;
            it.Tab.BringIntoView();   // cuộn dải tab tới tab vừa chọn
            TabListPopup.IsOpen = false;
            TabListBox.SelectedItem = null;
        }
    }

    /// <summary>Lăn chuột trên dải tab → cuộn NGANG.</summary>
    private void HeaderScroll_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
        if (sender is ScrollViewer sv)
        {
            sv.ScrollToHorizontalOffset(sv.HorizontalOffset - e.Delta);
            e.Handled = true;
        }
    }

    // ====================== Pin tab (ghim lên dòng trên) ======================

    private static bool IsPinned(TabItem tab) => (tab.Tag as string) == "pinned";

    private void TogglePin(TabItem tab)
    {
        if (IsPinned(tab)) UnpinTab(tab); else PinTab(tab);
    }

    private void PinTab(TabItem tab)
    {
        if (IsPinned(tab)) return;
        tab.Tag = "pinned";
        _pinnedTabs.Add(new PinnedTabVm
        {
            Tab = tab,
            Title = GetTabTitle(tab),
            Kind = tab.Content is DataCompareView ? "Data" : "Schema",
        });
        RefreshPinnedStrip();
        Tabs.SelectedItem = tab; // giữ chọn → content vẫn hiện
    }

    private void UnpinTab(TabItem tab)
    {
        tab.Tag = null;
        var vm = _pinnedTabs.FirstOrDefault(p => ReferenceEquals(p.Tab, tab));
        if (vm != null) _pinnedTabs.Remove(vm);
        RefreshPinnedStrip();
        Tabs.SelectedItem = tab;
        tab.BringIntoView();
    }

    private void RefreshPinnedStrip()
    {
        PinnedTabsStrip.Visibility = _pinnedTabs.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        UpdatePinnedSelection();
    }

    /// <summary>Đồng bộ highlight tab ghim với tab đang active.</summary>
    private void UpdatePinnedSelection()
    {
        foreach (var vm in _pinnedTabs)
            vm.IsSelected = ReferenceEquals(vm.Tab, Tabs.SelectedItem);
    }

    private void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.Source is not TabControl) return;   // bỏ qua selection bubble từ control con
        UpdatePinnedSelection();
    }

    /// <summary>Click 1 tab ghim → active tab đó.</summary>
    private void PinnedTab_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is PinnedTabVm vm)
            Tabs.SelectedItem = vm.Tab;
    }

    /// <summary>📌 trên tab ghim → bỏ ghim (tab quay về dải dưới).</summary>
    private void PinnedTabUnpin_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true; // không bubble lên PinnedTab_Click
        if (sender is FrameworkElement fe && fe.Tag is PinnedTabVm vm)
            UnpinTab(vm.Tab);
    }

    /// <summary>✕ trên tab ghim → đóng tab.</summary>
    private void PinnedTabClose_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true; // không cho bubble lên PinnedTab_Click (chọn tab vừa đóng)
        if (sender is FrameworkElement fe && fe.Tag is PinnedTabVm vm)
            CloseTab(vm.Tab);
    }

    /// <summary>VM 1 dòng trong popup danh sách tab.</summary>
    private sealed class TabListItem
    {
        public string Title { get; set; } = "";
        public string Kind { get; set; } = "Schema";
        public TabItem? Tab { get; set; }
        public bool IsCurrent { get; set; }

        public string Marker => IsCurrent ? "●" : "";
        public System.Windows.FontWeight Weight => IsCurrent ? System.Windows.FontWeights.Bold : System.Windows.FontWeights.Normal;
        public System.Windows.Media.Brush KindBrush =>
            string.Equals(Kind, "Schema", System.StringComparison.OrdinalIgnoreCase)
                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xE9, 0x7A, 0xA6))
                : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xB4, 0x93, 0xD6));
    }

    // ====================== SAVED PROJECTS PANEL ======================

    /// <summary>Marshal về UI thread rồi reload danh sách project.</summary>
    private void OnRecentProjectsChanged()
    {
        if (Dispatcher.CheckAccess()) ReloadSavedProjects();
        else Dispatcher.Invoke(ReloadSavedProjects);
    }

    /// <summary>Đọc lại danh sách recent projects từ store và bind vào ListBox.</summary>
    private void ReloadSavedProjects()
    {
        _savedProjects.Clear();
        var projects = RecentProjectStore.Load();
        foreach (var rp in projects)
        {
            _savedProjects.Add(new SavedProjectVm
            {
                Name = string.IsNullOrWhiteSpace(rp.Name) ? System.IO.Path.GetFileNameWithoutExtension(rp.Path) : rp.Name,
                Kind = rp.Kind,
                Path = rp.Path,
                Group = rp.Group,
            });
        }

        // Group rỗng (đã tạo nhưng chưa có project) → thêm placeholder để header hiện + drop được.
        var used = new System.Collections.Generic.HashSet<string>(
            projects.Where(p => !string.IsNullOrWhiteSpace(p.Group)).Select(p => p.Group),
            System.StringComparer.OrdinalIgnoreCase);
        foreach (var g in RecentProjectStore.LoadGroups())
            if (!used.Contains(g))
                _savedProjects.Add(new SavedProjectVm { Group = g, IsPlaceholder = true, Kind = "Schema" });
    }

    // ====================== Group Saved Projects (drag-drop) ======================

    private System.Windows.Point _projDragStart;
    private SavedProjectVm? _projDragVm;

    private void SavedProject_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        _projDragStart = e.GetPosition(null);
        _projDragVm = (e.OriginalSource as FrameworkElement)?.DataContext as SavedProjectVm;
    }

    private void SavedProject_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (e.LeftButton != System.Windows.Input.MouseButtonState.Pressed) return;
        if (_projDragVm == null || _projDragVm.IsPlaceholder) return;
        var pos = e.GetPosition(null);
        if (System.Math.Abs(pos.X - _projDragStart.X) < SystemParameters.MinimumHorizontalDragDistance &&
            System.Math.Abs(pos.Y - _projDragStart.Y) < SystemParameters.MinimumVerticalDragDistance) return;

        var vm = _projDragVm;
        _projDragVm = null;
        DragDrop.DoDragDrop(SavedProjectList, vm, DragDropEffects.Move);
    }

    private void GroupHeader_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(typeof(SavedProjectVm)) ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private void GroupHeader_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(typeof(SavedProjectVm)) is not SavedProjectVm vm) return;
        if (sender is not FrameworkElement fe || fe.Tag is not string gname) return;
        // "(Chưa nhóm)" → bỏ nhóm (group rỗng).
        string group = string.Equals(gname, "(Chưa nhóm)", System.StringComparison.Ordinal) ? "" : gname;
        RecentProjectStore.SetGroup(vm.Path, group); // raise Changed → ReloadSavedProjects
        e.Handled = true;
    }

    private void NewGroupButton_Click(object sender, RoutedEventArgs e)
    {
        string? name = PromptText(this, "New group", "Tên group:", "");
        if (!string.IsNullOrWhiteSpace(name)) RecentProjectStore.AddGroup(name.Trim());
    }

    private void RemoveGroupButton_Click(object sender, RoutedEventArgs e)
    {
        if (SavedProjectList.SelectedItem is not SavedProjectVm vm || string.IsNullOrWhiteSpace(vm.Group))
        {
            MessageBox.Show("Chọn 1 project (hoặc dòng trống) thuộc group cần xoá.", "Remove group",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (MessageBox.Show($"Xoá group \"{vm.Group}\"? Project trong group sẽ về 'Chưa nhóm'.",
            "Remove group", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes)
            RecentProjectStore.RemoveGroup(vm.Group);
    }

    /// <summary>Hộp nhập text đơn giản (OK/Cancel). Trả null nếu Cancel.</summary>
    private static string? PromptText(Window owner, string title, string prompt, string def)
    {
        var win = new Window
        {
            Title = title, Width = 340, Height = 150, Owner = owner,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize, WindowStyle = WindowStyle.ToolWindow, ShowInTaskbar = false,
        };
        var grid = new Grid { Margin = new Thickness(12) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var lbl = new TextBlock { Text = prompt, Margin = new Thickness(0, 0, 0, 6) };
        Grid.SetRow(lbl, 0); grid.Children.Add(lbl);
        var tb = new TextBox { Text = def, VerticalContentAlignment = VerticalAlignment.Center };
        Grid.SetRow(tb, 1); grid.Children.Add(tb);

        var panel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        Grid.SetRow(panel, 3);
        string? result = null;
        var ok = new Button { Content = "OK", Width = 72, Margin = new Thickness(0, 0, 6, 0), IsDefault = true };
        var cancel = new Button { Content = "Cancel", Width = 72, IsCancel = true };
        ok.Click += (_, _) => { result = tb.Text; win.DialogResult = true; };
        panel.Children.Add(ok); panel.Children.Add(cancel);
        grid.Children.Add(panel);

        win.Content = grid;
        tb.Loaded += (_, _) => { tb.SelectAll(); tb.Focus(); };
        return win.ShowDialog() == true ? result : null;
    }

    private void SavedProjectList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        => OpenSelectedProject();

    private void OpenProjectButton_Click(object sender, RoutedEventArgs e) => OpenSelectedProject();

    /// <summary>Thêm 1 file project .sqlcmp/.sqldcmp đã có sẵn vào danh sách recent.</summary>
    private void AddProjectButton_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Add existing project",
            Filter = "Compare projects (*.sqlcmp;*.sqldcmp;*.json)|*.sqlcmp;*.sqldcmp;*.json|All files (*.*)|*.*",
            Multiselect = true,
        };
        if (dlg.ShowDialog(this) != true) return;

        foreach (string path in dlg.FileNames)
        {
            // Suy ra Kind theo đuôi (.sqldcmp = Data), mặc định Schema.
            string kind = path.EndsWith(".sqldcmp", System.StringComparison.OrdinalIgnoreCase) ? "Data" : "Schema";
            RecentProjectStore.Add(path, kind); // raise Changed → list tự refresh
        }
    }

    /// <summary>Mở lại project đang chọn trong tab mới (theo Kind).</summary>
    private void OpenSelectedProject()
    {
        if (SavedProjectList.SelectedItem is not SavedProjectVm vm || vm.IsPlaceholder)
        {
            MessageBox.Show("Select a saved project first.", "Open project",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        OpenProject(vm.Path, vm.Kind);
    }

    /// <summary>
    /// Load file project → tạo view đúng loại trong tab mới → áp connection + bảng.
    /// File không tồn tại → báo + hỏi gỡ khỏi danh sách.
    /// </summary>
    private void OpenProject(string path, string kind)
    {
        if (string.IsNullOrWhiteSpace(path)) return;

        if (!System.IO.File.Exists(path))
        {
            var ans = MessageBox.Show(
                $"Project file no longer exists:\n\n{path}\n\nRemove it from the list?",
                "File not found", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.Yes);
            if (ans == MessageBoxResult.Yes)
                RecentProjectStore.Remove(path); // raise Changed → list tự refresh
            return;
        }

        try
        {
            ComparisonProject project = ComparisonProjectStore.Load(path);
            bool isData = string.Equals(project.Kind, "Data", System.StringComparison.OrdinalIgnoreCase)
                          || string.Equals(kind, "Data", System.StringComparison.OrdinalIgnoreCase);

            string projName = System.IO.Path.GetFileNameWithoutExtension(path);
            if (isData)
            {
                var view = new DataCompareView();
                AddTab("Data", view, projName);
                view.ApplyProject(project);
            }
            else
            {
                var view = new SchemaCompareView();
                AddTab("Schema", view, projName);
                view.ApplyProject(project);
            }
            // Đưa project vừa mở lên đầu danh sách.
            RecentProjectStore.Add(path, isData ? "Data" : "Schema");
        }
        catch (System.Exception ex)
        {
            MessageBox.Show(ex.Message, "Open project failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>Gỡ project đang chọn khỏi danh sách recent (không xoá file trên đĩa).</summary>
    private void RemoveProjectButton_Click(object sender, RoutedEventArgs e)
    {
        if (SavedProjectList.SelectedItem is not SavedProjectVm vm || vm.IsPlaceholder)
        {
            MessageBox.Show("Select a saved project first.", "Remove project",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var ans = MessageBox.Show(
            $"Remove \"{vm.Name}\" from recent projects?\n(The project file is NOT deleted.)",
            "Remove", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
        if (ans != MessageBoxResult.Yes) return;

        try { RecentProjectStore.Remove(vm.Path); } // raise Changed → list tự refresh
        catch (System.Exception ex)
        {
            MessageBox.Show(ex.Message, "Remove failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ====================== SAVED CONNECTIONS PANEL ======================

    /// <summary>Đọc lại danh sách saved connections từ store và bind vào ListBox.</summary>
    private void ReloadSavedConnections()
    {
        _savedConns.Clear();
        foreach (var sc in SavedConnectionStore.Load())
        {
            _savedConns.Add(new SavedConnVm
            {
                Name = sc.Name,
                SubText = $"{sc.Connection.Server} / {sc.Connection.Database}",
                Connection = sc.Connection,
            });
        }
    }

    /// <summary>Toggle đóng/mở panel trái (Width 220 ↔ 0).</summary>
    private void TogglePanelButton_Click(object sender, RoutedEventArgs e)
    {
        bool isOpen = LeftPanelColumn.Width.Value > 0;
        if (isOpen)
        {
            // Thu gọn: panel + splitter ẩn, nút mở hiện ở footer.
            _lastPanelWidth = LeftPanelColumn.ActualWidth > 0 ? LeftPanelColumn.ActualWidth : _lastPanelWidth;
            LeftPanelColumn.Width = new GridLength(0);
            LeftPanel.Visibility = Visibility.Collapsed;
            LeftSplitter.Visibility = Visibility.Collapsed;
            ExpandPanelButton.Visibility = Visibility.Visible;
            PanelActivityButton.Tag = null;        // panel đóng → bỏ highlight nút 📁
        }
        else
        {
            // Mở lại: khôi phục độ rộng, hiện panel + splitter, ẩn nút mở ở footer.
            LeftPanelColumn.Width = new GridLength(_lastPanelWidth <= 0 ? 220 : _lastPanelWidth);
            LeftPanel.Visibility = Visibility.Visible;
            LeftSplitter.Visibility = Visibility.Visible;
            ExpandPanelButton.Visibility = Visibility.Collapsed;
            PanelActivityButton.Tag = "active";    // panel mở → highlight nút 📁
        }
    }

    /// <summary>View đang active trong tab (Schema/Data) — null nếu không có.</summary>
    private UIElement? ActiveView() => (Tabs.SelectedItem as TabItem)?.Content as UIElement;

    /// <summary>Lưu connection của tab đang active (cả Source + Target → 2 entry đặt tên).</summary>
    private void SaveCurrentConnButton_Click(object sender, RoutedEventArgs e)
    {
        DbConnectionInfo? src = null, tgt = null;
        switch (ActiveView())
        {
            case SchemaCompareView s: src = s.GetSourceConnection(); tgt = s.GetTargetConnection(); break;
            case DataCompareView d: src = d.GetSourceConnection(); tgt = d.GetTargetConnection(); break;
        }
        if (src == null || tgt == null)
        {
            MessageBox.Show("No active compare tab.", "Save connection",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        int saved = 0;
        saved += SavePromptOne("Source", src);
        saved += SavePromptOne("Target", tgt);
        if (saved > 0) ReloadSavedConnections();
    }

    /// <summary>Hỏi tên rồi lưu 1 connection (bỏ qua nếu server/db trống hoặc user huỷ).</summary>
    private int SavePromptOne(string side, DbConnectionInfo conn)
    {
        if (string.IsNullOrWhiteSpace(conn.Server) && string.IsNullOrWhiteSpace(conn.Database))
            return 0; // không có gì để lưu

        string suggested = $"{conn.Server}/{conn.Database}";
        string? name = PromptForName($"Name for {side} connection:", suggested);
        if (string.IsNullOrWhiteSpace(name)) return 0;

        try
        {
            SavedConnectionStore.Add(new SavedConnection { Name = name.Trim(), Connection = conn });
            return 1;
        }
        catch (System.Exception ex)
        {
            MessageBox.Show(ex.Message, "Save connection failed", MessageBoxButton.OK, MessageBoxImage.Error);
            return 0;
        }
    }

    /// <summary>Hộp thoại nhập tên đơn giản (WPF, không phụ thuộc WinForms). Cancel → null.</summary>
    private string? PromptForName(string message, string defaultValue)
    {
        var dlg = new Window
        {
            Title = "Save Connection",
            Width = 380,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            ResizeMode = ResizeMode.NoResize,
            WindowStyle = WindowStyle.ToolWindow,
        };
        var grid = new Grid { Margin = new Thickness(12) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var label = new TextBlock { Text = message, Margin = new Thickness(0, 0, 0, 6) };
        Grid.SetRow(label, 0);
        grid.Children.Add(label);

        var box = new TextBox { Text = defaultValue, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetRow(box, 1);
        grid.Children.Add(box);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 8, 0, 0),
        };
        var ok = new Button { Content = "OK", IsDefault = true, MinWidth = 70, Margin = new Thickness(0, 0, 6, 0) };
        var cancel = new Button { Content = "Cancel", IsCancel = true, MinWidth = 70 };
        bool confirmed = false;
        ok.Click += (_, _) => { confirmed = true; dlg.DialogResult = true; };
        buttons.Children.Add(ok);
        buttons.Children.Add(cancel);
        Grid.SetRow(buttons, 2);
        grid.Children.Add(buttons);

        dlg.Content = grid;
        box.Focus();
        box.SelectAll();
        dlg.ShowDialog();
        return confirmed ? box.Text : null;
    }

    /// <summary>Connection đang chọn trong ListBox (null nếu chưa chọn).</summary>
    private DbConnectionInfo? SelectedConn() => (SavedConnList.SelectedItem as SavedConnVm)?.Connection;

    private void ApplySourceButton_Click(object sender, RoutedEventArgs e) => ApplyToSide(toSource: true);

    private void ApplyTargetButton_Click(object sender, RoutedEventArgs e) => ApplyToSide(toSource: false);

    private void SavedConnList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        => ApplyToSide(toSource: true); // double-click = Apply to Source

    /// <summary>Áp connection đang chọn vào Source/Target của tab active.</summary>
    private void ApplyToSide(bool toSource)
    {
        DbConnectionInfo? conn = SelectedConn();
        if (conn == null)
        {
            MessageBox.Show("Select a saved connection first.", "Apply connection",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        // Clone để không chia sẻ instance giữa các tab.
        var copy = CloneConn(conn);
        switch (ActiveView())
        {
            case SchemaCompareView s:
                if (toSource) s.SetSourceConnection(copy); else s.SetTargetConnection(copy);
                break;
            case DataCompareView d:
                if (toSource) d.SetSourceConnection(copy); else d.SetTargetConnection(copy);
                break;
            default:
                MessageBox.Show("No active compare tab.", "Apply connection",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                break;
        }
    }

    private static DbConnectionInfo CloneConn(DbConnectionInfo c) => new()
    {
        Server = c.Server,
        Database = c.Database,
        IntegratedSecurity = c.IntegratedSecurity,
        UserId = c.UserId,
        Password = c.Password,
        TrustServerCertificate = c.TrustServerCertificate,
        ConnectTimeoutSeconds = c.ConnectTimeoutSeconds,
    };

    /// <summary>Xoá saved connection đang chọn (có confirm).</summary>
    private void RemoveConnButton_Click(object sender, RoutedEventArgs e)
    {
        if (SavedConnList.SelectedItem is not SavedConnVm vm)
        {
            MessageBox.Show("Select a saved connection first.", "Remove connection",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var ans = MessageBox.Show($"Remove saved connection \"{vm.Name}\"?", "Remove",
            MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
        if (ans != MessageBoxResult.Yes) return;

        try
        {
            SavedConnectionStore.Remove(vm.Name);
            ReloadSavedConnections();
        }
        catch (System.Exception ex)
        {
            MessageBox.Show(ex.Message, "Remove failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void NewSchemaButton_Click(object sender, RoutedEventArgs e)
        => AddTab("Schema", new SchemaCompareView());

    private void NewDataButton_Click(object sender, RoutedEventArgs e)
        => AddTab("Data", new DataCompareView());

    private void CloseTabButton_Click(object sender, RoutedEventArgs e)
    {
        if (Tabs.SelectedItem is TabItem ti) CloseTab(ti);
    }

    /// <summary>Menu File → Exit.</summary>
    private void ExitMenu_Click(object sender, RoutedEventArgs e) => Close();

    /// <summary>Menu Help → About.</summary>
    private void AboutMenu_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            $"SQL Compare Suite  v{AppVersion}\nSchema & data comparison tool for SQL Server.\n\n.NET 8 WPF",
            "About", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    /// <summary>Thêm 1 tab mới (mỗi phiên so sánh độc lập), header có nút × để đóng.</summary>
    private TabItem AddTab(string kind, UIElement content, string? title = null)
    {
        var index = kind == "Schema" ? ++_schemaCounter : ++_dataCounter;
        var tab = new TabItem { Content = content };
        tab.Header = BuildHeader(kind, title ?? $"{kind} {index}", tab);
        // Middle-click (nút cuộn chuột) lên tab → đóng tab đó.
        tab.PreviewMouseDown += (_, e) =>
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Middle)
            {
                CloseTab(tab);
                e.Handled = true;
            }
        };
        Tabs.Items.Add(tab);
        Tabs.SelectedItem = tab;
        // Cuộn dải tab tới tab mới (defer để container đã layout).
        Dispatcher.BeginInvoke(new System.Action(() => tab.BringIntoView()),
            System.Windows.Threading.DispatcherPriority.Loaded);

        // Đổi tên tab khi view Save/Load project (hiện tên file project).
        if (content is SchemaCompareView sv) sv.ProjectTitleChanged += t => SetTabTitle(tab, t);
        else if (content is DataCompareView dv) dv.ProjectTitleChanged += t => SetTabTitle(tab, t);
        return tab;
    }

    /// <summary>Đổi text tiêu đề của tab (TextBlock title trong header StackPanel).</summary>
    private void SetTabTitle(TabItem tab, string title)
    {
        if (tab.Header is StackPanel sp)
            foreach (var c in sp.Children)
                if (c is TextBlock tb) { tb.Text = title; break; }

        // Nếu tab đang ghim → cập nhật tên trên dải ghim.
        var pv = _pinnedTabs.FirstOrDefault(p => ReferenceEquals(p.Tab, tab));
        if (pv != null)
        {
            int idx = _pinnedTabs.IndexOf(pv);
            _pinnedTabs[idx] = new PinnedTabVm { Tab = tab, Title = title, Kind = pv.Kind };
        }
    }

    /// <summary>Header tab = chip màu (icon) theo loại + tiêu đề + nút × đóng.</summary>
    private object BuildHeader(string kind, string title, TabItem tab)
    {
        // Schema = xanh dương ⚙ ; Data = xanh lá ▦ — để phân biệt 2 loại tab ngay từ cái nhìn
        var isSchema = kind == "Schema";
        var accent = isSchema
            ? System.Windows.Media.Color.FromRgb(0xE9, 0x7A, 0xA6)   // rose macaron
            : System.Windows.Media.Color.FromRgb(0xB4, 0x93, 0xD6);  // lavender macaron
        var accentBrush = new System.Windows.Media.SolidColorBrush(accent);

        var panel = new StackPanel { Orientation = Orientation.Horizontal };

        // Chip icon tròn màu theo loại
        panel.Children.Add(new Border
        {
            Background = accentBrush,
            CornerRadius = new CornerRadius(3),
            Width = 18,
            Height = 18,
            Margin = new Thickness(0, 0, 6, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = isSchema ? "⚙" : "▦",
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 11,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
        });

        panel.Children.Add(new TextBlock
        {
            Text = title,
            FontWeight = FontWeights.SemiBold,
            Foreground = accentBrush,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 3, 0),
        });
        var close = new Button
        {
            Content = "✕",
            FontSize = 8,
            Width = 13,
            Height = 13,
            MinWidth = 0,
            Padding = new Thickness(0),
            Margin = new Thickness(0, 0, -2, 0),
            BorderThickness = new Thickness(0),
            Background = System.Windows.Media.Brushes.Transparent,
            Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0x99, 0x99, 0x99)),
            VerticalAlignment = VerticalAlignment.Center,
            Cursor = System.Windows.Input.Cursors.Hand,
            ToolTip = "Đóng tab",
        };
        close.Click += (_, _) => CloseTab(tab);
        panel.Children.Add(close);

        // Right-click menu: Pin/Unpin + Close
        var menu = new ContextMenu();
        var pinItem = new MenuItem();
        pinItem.Click += (_, _) => TogglePin(tab);
        var closeMi = new MenuItem { Header = "✕ Close tab" };
        closeMi.Click += (_, _) => CloseTab(tab);
        menu.Items.Add(pinItem);
        menu.Items.Add(closeMi);
        menu.Opened += (_, _) => pinItem.Header = IsPinned(tab) ? "📌 Unpin tab" : "📌 Pin tab";
        panel.ContextMenu = menu;
        return panel;
    }

    private void CloseTab(TabItem tab)
    {
        // Lấy tiêu đề tab để hiện trong confirm
        var title = "tab này";
        if (tab.Header is System.Windows.Controls.StackPanel sp &&
            sp.Children.Count > 0 && sp.Children[0] is System.Windows.Controls.TextBlock tb)
            title = $"\"{tb.Text}\"";

        var answer = MessageBox.Show(
            $"Đóng {title}? Mọi kết quả so sánh trong tab sẽ mất.",
            "Đóng tab", MessageBoxButton.YesNo, MessageBoxImage.Question,
            MessageBoxResult.No);

        if (answer == MessageBoxResult.Yes)
        {
            Tabs.Items.Remove(tab);
            var pv = _pinnedTabs.FirstOrDefault(p => ReferenceEquals(p.Tab, tab));
            if (pv != null) { _pinnedTabs.Remove(pv); RefreshPinnedStrip(); }
            // Nếu không còn tab nào được chọn nhưng vẫn còn tab → chọn tab đầu.
            if (Tabs.SelectedItem == null && Tabs.Items.Count > 0)
                Tabs.SelectedIndex = 0;
        }
    }
}
