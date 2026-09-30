using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SqlCompareSuite.Core.Execution;
using SqlCompareSuite.Core.Models;

namespace SqlCompareSuite.App.Views;

public partial class ConnectionPanel : UserControl
{
    public ConnectionPanel()
    {
        InitializeComponent();
    }

    /// <summary>Header label ("Source" / "Target").</summary>
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(ConnectionPanel),
            new PropertyMetadata("Source", OnTitleChanged));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ConnectionPanel panel)
            panel.HeaderText.Text = (string)e.NewValue;
    }

    private void AuthRadio_Changed(object sender, RoutedEventArgs e)
    {
        // UserBox/PasswordBox may be null during XAML init order.
        if (UserBox == null || PasswordBox == null)
            return;

        bool sqlAuth = SqlAuthRadio.IsChecked == true;
        UserBox.IsEnabled = sqlAuth;
        PasswordBox.IsEnabled = sqlAuth;
    }

    /// <summary>Build a DbConnectionInfo from the current input.</summary>
    public DbConnectionInfo GetConnectionInfo()
    {
        bool windowsAuth = WindowsAuthRadio.IsChecked == true;
        return new DbConnectionInfo
        {
            Server = ServerBox.Text.Trim(),
            Database = (DatabaseBox.Text ?? "").Trim(),
            IntegratedSecurity = windowsAuth,
            UserId = windowsAuth ? "" : UserBox.Text.Trim(),
            Password = windowsAuth ? "" : PasswordBox.Password,
        };
    }

    /// <summary>Set the panel fields from a DbConnectionInfo (used by Swap Source ⇄ Target).</summary>
    public void SetConnectionInfo(DbConnectionInfo conn)
    {
        if (conn == null) return;

        ServerBox.Text = conn.Server;
        DatabaseBox.Text = conn.Database;

        if (conn.IntegratedSecurity)
            WindowsAuthRadio.IsChecked = true;
        else
            SqlAuthRadio.IsChecked = true;

        UserBox.Text = conn.UserId;
        PasswordBox.Password = conn.Password;

        // Explicitly sync enabled state — Checked event won't fire if the radio
        // was already selected (e.g. swapping two SQL-auth connections).
        bool sqlAuth = !conn.IntegratedSecurity;
        UserBox.IsEnabled = sqlAuth;
        PasswordBox.IsEnabled = sqlAuth;
    }

    private async void TestButton_Click(object sender, RoutedEventArgs e)
    {
        DbConnectionInfo conn = GetConnectionInfo();
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
            MessageBox.Show(
                message,
                ok ? "Connection OK" : "Connection failed",
                MessageBoxButton.OK,
                ok ? MessageBoxImage.Information : MessageBoxImage.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void LoadDbButton_Click(object sender, RoutedEventArgs e)
    {
        DbConnectionInfo conn = GetConnectionInfo();
        if (string.IsNullOrWhiteSpace(conn.Server))
        {
            MessageBox.Show("Server is required.", "Missing input",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        SetBusy(true);
        try
        {
            List<string> dbs = await Task.Run(() => new SqlServerBrowser().ListDatabases(conn));

            string? current = DatabaseBox.Text;
            DatabaseBox.ItemsSource = dbs;
            // Preserve typed value (editable combo) if it was set.
            if (!string.IsNullOrWhiteSpace(current))
                DatabaseBox.Text = current;
            else if (dbs.Count > 0)
                DatabaseBox.SelectedIndex = 0;
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

    private void SetBusy(bool busy)
    {
        TestButton.IsEnabled = !busy;
        LoadDbButton.IsEnabled = !busy;
        Mouse.OverrideCursor = busy ? Cursors.Wait : null;
    }
}
