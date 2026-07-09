using System.Diagnostics;
using System.Windows;

namespace RobloxMultiInstance;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => ShowStatus();
    }

    private void ShowStatus()
    {
        bool active = (Application.Current as App)?.Active == true;

        if (active)
        {
            StatusText.Text = "On";
            HintText.Text = "Keep this open, then launch Roblox.";
            Buttons.Visibility = Visibility.Collapsed;
        }
        else
        {
            StatusText.Text = "Off";
            HintText.Text = "Close Roblox first, then click Retry.";
            Buttons.Visibility = Visibility.Visible;
        }
    }

    private void Retry_Click(object sender, RoutedEventArgs e)
    {
        (Application.Current as App)?.TryAcquire();
        ShowStatus();
    }

    private void ForceClose_Click(object sender, RoutedEventArgs e)
    {
        foreach (var name in new[] { "RobloxPlayerBeta", "RobloxPlayerLauncher", "Windows10Universal" })
            foreach (var p in Process.GetProcessesByName(name))
                try { p.Kill(); } catch { }

        Dispatcher.InvokeAsync(async () =>
        {
            await System.Threading.Tasks.Task.Delay(700);
            (Application.Current as App)?.TryAcquire();
            ShowStatus();
        });
    }
}
