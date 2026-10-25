using System.Text.Json;

namespace KbSwitch;

public sealed class AppRule
{
    public string Process { get; set; } = "";   // e.g. "cs2.exe"
    public string Layout { get; set; } = "";    // layout key (see Layouts.Key)
}

public sealed class AppConfig
{
    public bool Enabled { get; set; } = true;
    public string? DefaultLayout { get; set; }  // null = Windows default layout
    public bool TrayHintShown { get; set; }
    public bool ShowSystemProcesses { get; set; }
    public bool StartMinimized { get; set; }        // true = always start hidden in the tray (same as the --tray argument)
    public bool CloseToTray { get; set; } = true;   // false = closing the window really quits the app
    public List<AppRule> Rules { get; set; } = new();

    // KBSWITCH_CONFIG = full path of an alternative config.json (testing: avoids touching %APPDATA%)
    static readonly string FilePath =
        Environment.GetEnvironmentVariable("KBSWITCH_CONFIG") is { Length: > 0 } custom
            ? Path.GetFullPath(custom)
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "KbSwitch", "config.json");
    static readonly string Dir = Path.GetDirectoryName(FilePath)!;
    static readonly JsonSerializerOptions Opts = new() { WriteIndented = true };

    public static AppConfig Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(FilePath)) ?? new AppConfig();
        }
        catch { /* corrupt config: start from scratch */ }
        return new AppConfig();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Dir);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, Opts));
        }
        catch { }
    }
}
