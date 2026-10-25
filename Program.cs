namespace KbSwitch;

internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        using var mutex = new Mutex(true, "KbSwitch_SingleInstance_7f3a", out bool first);
        if (!first)
        {
            MessageBox.Show("KbSwitch is already running (see the icon in the notification area).", "KbSwitch");
            return;
        }

        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        var cfg = AppConfig.Load();
        bool startInTray = cfg.StartMinimized || args.Any(a => a.Equals("--tray", StringComparison.OrdinalIgnoreCase));
        Application.Run(new MainForm(cfg, startInTray));
    }
}
