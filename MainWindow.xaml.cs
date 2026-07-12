using System.Windows;

namespace TinyAcc;

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
        bool gameRunning = App.IsGameRunning();

        if (active && !gameRunning)
        {
            StatusText.Text = "Multi-instance: ON";
            HintText.Text = "Keep this window open, then launch the game as many times as you want.";
            Buttons.Visibility = Visibility.Collapsed;
        }
        else if (gameRunning)
        {
            // Even with the mutex held, a client that was already open owns the
            // real singleton event — multi-instance needs a clean start.
            StatusText.Text = "Warning — Roblox is already open";
            HintText.Text = "Multi-instance only works when TinyAcc starts first. " +
                            "Close ALL Roblox windows, then click Retry (or use Close Roblox).";
            Buttons.Visibility = Visibility.Visible;
        }
        else
        {
            StatusText.Text = "Multi-instance: OFF";
            HintText.Text = "Something else owns the game's startup lock. Close Roblox, then click Retry.";
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
        App.CloseGamesInSession();   // only this session's Roblox — never another desktop's

        Dispatcher.InvokeAsync(async () =>
        {
            await System.Threading.Tasks.Task.Delay(700);
            (Application.Current as App)?.TryAcquire();
            ShowStatus();
        });
    }
}
