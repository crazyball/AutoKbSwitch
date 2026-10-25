using System.Diagnostics;
using Microsoft.Win32;

namespace KbSwitch;

internal sealed record LayoutItem(string? Key, string Label)
{
    public override string ToString() => Label;
}

internal sealed class MainForm : Form
{
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string RunName = "KbSwitch";

    readonly AppConfig cfg;
    readonly Switcher switcher;
    readonly NotifyIcon tray;
    readonly ToolStripMenuItem miEnabled;

    readonly ListView lvProc = MakeList(("Process", 180), ("Window", 240));
    readonly ListView lvRules = MakeList(("Process", 180), ("Layout", 260));
    readonly TextBox txtFilter = new() { PlaceholderText = "Filter…", Width = 200 };
    readonly ComboBox cbLayout = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 260 };
    readonly CheckBox chkEnabled = new() { Text = "Enabled", AutoSize = true, Margin = new Padding(8, 6, 3, 3) };

    // Settings menu
    readonly ToolStripMenuItem miStartup = new("Start with &Windows")
    {
        CheckOnClick = true,
        ToolTipText = "Launch KbSwitch hidden in the tray when you sign in to Windows."
    };
    readonly ToolStripMenuItem miStartMinimized = new("Start &minimized in the tray")
    {
        CheckOnClick = true,
        ToolTipText = "Start KbSwitch hidden in the tray instead of showing its window.\n" +
                      "(\"Start with Windows\" always starts hidden, whatever this setting.)"
    };
    readonly ToolStripMenuItem miCloseToTray = new("&Keep running in the tray when the window is closed")
    {
        CheckOnClick = true,
        ToolTipText = "Ticked: the close button hides the window and KbSwitch keeps switching layouts.\n" +
                      "Unticked: the close button quits KbSwitch, which stops the automatic switching."
    };
    readonly ToolStripMenuItem miDefault = new("&Default layout (everything else)")
    {
        ToolTipText = "Layout applied to every program that has no rule."
    };

    readonly CheckBox chkShowAll = new() { Text = "Include Windows processes", AutoSize = true, Margin = new Padding(16, 5, 3, 3) };
    readonly ToolStripStatusLabel status = new() { Text = "Ready." };

    static readonly string WinDir =
        Environment.GetFolderPath(Environment.SpecialFolder.Windows).TrimEnd('\\') + "\\";

    List<(string Exe, string Title)> procs = new();
    bool hideOnStart;
    bool quitting;
    bool procsLoaded;

    public MainForm(AppConfig cfg, bool startHidden)
    {
        this.cfg = cfg;
        hideOnStart = startHidden;

        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = $"KbSwitch {AppInfo.Version}";
        Icon = AppIcon.Create();
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(960, 580);
        MinimumSize = new Size(860, 440);

        // ---------- UI ----------
        var btnRefresh = new Button { Text = "Refresh", AutoSize = true };
        var btnBrowse = new Button { Text = "Add an .exe…", AutoSize = true };
        var btnAssoc = new Button { Text = "Assign →", AutoSize = true };
        var btnRemove = new Button { Text = "Remove", AutoSize = true };

        var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 4, Padding = new Padding(8) };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        grid.Controls.Add(Flow(Title("Open windows"), chkShowAll), 0, 0);
        grid.Controls.Add(Title("Rules (program → layout)"), 1, 0);
        grid.Controls.Add(Flow(txtFilter, btnRefresh, btnBrowse), 0, 1);
        grid.Controls.Add(Hint("Double-click a program to assign it the selected layout"), 1, 1);
        grid.Controls.Add(lvProc, 0, 2);
        grid.Controls.Add(lvRules, 1, 2);
        grid.Controls.Add(Flow(Lbl("Layout:"), cbLayout, btnAssoc), 0, 3);
        grid.Controls.Add(Flow(btnRemove), 1, 3);

        var bottom = Flow(chkEnabled);
        bottom.Dock = DockStyle.Bottom;
        bottom.Padding = new Padding(8, 0, 8, 6);

        var strip = new StatusStrip();
        strip.Items.Add(status);

        // ---------- Menu bar ----------
        var menu = new MenuStrip();
        var mFile = new ToolStripMenuItem("&File");
        mFile.DropDownItems.Add(new ToolStripMenuItem("&Quit", null, (_, _) => Quit()) { ShortcutKeys = Keys.Control | Keys.Q });
        var mSettings = new ToolStripMenuItem("&Settings");
        mSettings.DropDownItems.Add(miStartup);
        mSettings.DropDownItems.Add(miStartMinimized);
        mSettings.DropDownItems.Add(miCloseToTray);
        mSettings.DropDownItems.Add(new ToolStripSeparator());
        mSettings.DropDownItems.Add(miDefault);
        var mHelp = new ToolStripMenuItem("&Help");
        mHelp.DropDownItems.Add(new ToolStripMenuItem("&About KbSwitch…", null, (_, _) => ShowAbout()));
        menu.ShowItemToolTips = true;
        menu.Items.Add(mFile);
        menu.Items.Add(mSettings);
        menu.Items.Add(mHelp);

        Controls.Add(grid);
        Controls.Add(bottom);
        Controls.Add(strip);
        Controls.Add(menu);
        MainMenuStrip = menu;

        // ---------- Tray icon ----------
        miEnabled = new ToolStripMenuItem("Enabled") { CheckOnClick = true };
        var trayMenu = new ContextMenuStrip();
        trayMenu.Items.Add("Open", null, (_, _) => ShowMain());
        trayMenu.Items.Add(miEnabled);
        trayMenu.Items.Add("About KbSwitch…", null, (_, _) => ShowAbout());
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add("Quit", null, (_, _) => Quit());

        tray = new NotifyIcon { Icon = AppIcon.Create(SystemInformation.SmallIconSize), Text = "KbSwitch", Visible = true, ContextMenuStrip = trayMenu };
        tray.DoubleClick += (_, _) => ShowMain();

        // ---------- Engine ----------
        switcher = new Switcher(cfg);
        switcher.Switched += (exe, label) =>
        {
            status.Text = $"{exe} → {label}";
            string t = $"KbSwitch: {label}";
            tray.Text = t.Length > 63 ? t[..63] : t;
        };

        // ---------- Initial data ----------
        chkShowAll.Checked = cfg.ShowSystemProcesses;
        LoadLayouts();
        LoadRules();
        // LoadProcesses() is NOT called here: it opens a handle on every process (games included).
        // It runs when the window is shown (OnShown / ShowMain), never during a hidden start (--tray).
        chkEnabled.Checked = cfg.Enabled;
        miEnabled.Checked = cfg.Enabled;
        miCloseToTray.Checked = cfg.CloseToTray;
        miStartMinimized.Checked = cfg.StartMinimized;
        miStartup.Checked = StartupEnabled;
        if (miStartup.Checked) SetStartup(true);   // rewrites the path in case the exe was moved

        // ---------- Events ----------
        txtFilter.TextChanged += (_, _) => FillProcesses();
        chkShowAll.CheckedChanged += (_, _) =>
        {
            cfg.ShowSystemProcesses = chkShowAll.Checked;
            cfg.Save();
            LoadProcesses();
        };
        btnRefresh.Click += (_, _) => { LoadLayouts(); LoadRules(); LoadProcesses(); };
        btnBrowse.Click += (_, _) =>
        {
            using var dlg = new OpenFileDialog { Filter = "Programs (*.exe)|*.exe", Title = "Choose the program's executable" };
            if (dlg.ShowDialog(this) == DialogResult.OK) Associate(Path.GetFileName(dlg.FileName));
        };
        btnAssoc.Click += (_, _) => AssociateSelected();
        lvProc.DoubleClick += (_, _) => AssociateSelected();
        btnRemove.Click += (_, _) => RemoveSelected();
        lvRules.KeyDown += (_, e) => { if (e.KeyCode == Keys.Delete) RemoveSelected(); };
        lvRules.SelectedIndexChanged += (_, _) =>
        {
            if (lvRules.SelectedItems.Count == 0 || lvRules.SelectedItems[0].Tag is not AppRule r) return;
            var it = cbLayout.Items.Cast<LayoutItem>().FirstOrDefault(i => i.Key == r.Layout);
            if (it != null) cbLayout.SelectedItem = it;
        };
        chkEnabled.CheckedChanged += (_, _) =>
        {
            miEnabled.Checked = chkEnabled.Checked;
            cfg.Enabled = chkEnabled.Checked;
            cfg.Save();
            if (cfg.Enabled) switcher.ApplyNow();
        };
        miEnabled.CheckedChanged += (_, _) => chkEnabled.Checked = miEnabled.Checked;
        miStartup.CheckedChanged += (_, _) => SetStartup(miStartup.Checked);
        miStartMinimized.CheckedChanged += (_, _) =>
        {
            cfg.StartMinimized = miStartMinimized.Checked;
            cfg.Save();
        };
        miCloseToTray.CheckedChanged += (_, _) =>
        {
            cfg.CloseToTray = miCloseToTray.Checked;
            cfg.Save();
        };
    }

    // ================= Data =================

    void LoadLayouts()
    {
        string? prev = (cbLayout.SelectedItem as LayoutItem)?.Key;

        cbLayout.Items.Clear();
        miDefault.DropDownItems.Clear();
        AddDefaultItem(new LayoutItem(null, "Windows default"));
        foreach (var l in Layouts.All())
        {
            var it = new LayoutItem(Layouts.Key(l), Layouts.Label(l));
            cbLayout.Items.Add(it);
            AddDefaultItem(it);
        }

        // Pre-selection: the previous choice, otherwise the first layout that is not the system default (e.g. your QWERTY)
        string sysDef = Layouts.Key(InputLanguage.DefaultInputLanguage);
        var items = cbLayout.Items.Cast<LayoutItem>().ToList();
        var pick = items.FirstOrDefault(i => i.Key == prev)
                   ?? items.FirstOrDefault(i => i.Key != sysDef)
                   ?? items.FirstOrDefault();
        if (pick != null) cbLayout.SelectedItem = pick;

        UpdateDefaultChecks();
    }

    void AddDefaultItem(LayoutItem it)
    {
        var mi = new ToolStripMenuItem(it.Label.Replace("&", "&&")) { Tag = it };
        mi.Click += (_, _) =>
        {
            cfg.DefaultLayout = it.Key;
            cfg.Save();
            UpdateDefaultChecks();
        };
        miDefault.DropDownItems.Add(mi);
    }

    /// <summary>Radio-style check mark; an unknown saved layout falls back to "Windows default" (the first item).</summary>
    void UpdateDefaultChecks()
    {
        var items = miDefault.DropDownItems.Cast<ToolStripMenuItem>().ToList();
        var target = items.FirstOrDefault(m => ((LayoutItem)m.Tag!).Key == cfg.DefaultLayout) ?? items[0];
        foreach (var m in items) m.Checked = m == target;
    }

    void LoadProcesses()
    {
        procsLoaded = true;
        bool showAll = cfg.ShowSystemProcesses;
        int myPid = Environment.ProcessId;
        var list = new List<(string Exe, string Title)>();
        foreach (var p in Process.GetProcesses())
        {
            using (p)
            {
                try
                {
                    IntPtr hwnd = p.MainWindowHandle;
                    if (hwnd == IntPtr.Zero || string.IsNullOrWhiteSpace(p.MainWindowTitle)) continue;
                    if (!showAll && !IsUserApp(p.Id, hwnd, myPid)) continue;
                    list.Add((p.ProcessName + ".exe", p.MainWindowTitle));
                }
                catch { /* protected process or already closed */ }
            }
        }

        procs = list.GroupBy(x => x.Exe, StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.First())
                    .OrderBy(x => x.Exe, StringComparer.OrdinalIgnoreCase)
                    .ToList();
        FillProcesses();
    }

    /// <summary>True for an app launched by the user: visible, not cloaked, not under C:\Windows, not KbSwitch.</summary>
    static bool IsUserApp(int pid, IntPtr hwnd, int myPid)
    {
        if (pid == myPid) return false;
        if (!Native.IsWindowVisible(hwnd) || Native.IsCloaked(hwnd)) return false;

        // Path not found (heavily protected process): when in doubt keep it, it may be a game with anti-cheat
        string? path = Native.ProcessPath(pid);
        return path == null || !path.StartsWith(WinDir, StringComparison.OrdinalIgnoreCase);
    }

    void FillProcesses()
    {
        string f = txtFilter.Text.Trim();
        lvProc.BeginUpdate();
        lvProc.Items.Clear();
        foreach (var (exe, title) in procs)
        {
            if (f.Length > 0
                && !exe.Contains(f, StringComparison.OrdinalIgnoreCase)
                && !title.Contains(f, StringComparison.OrdinalIgnoreCase)) continue;
            lvProc.Items.Add(new ListViewItem(new[] { exe, title }));
        }
        lvProc.EndUpdate();
    }

    void LoadRules()
    {
        lvRules.BeginUpdate();
        lvRules.Items.Clear();
        foreach (var r in cfg.Rules.OrderBy(r => r.Process, StringComparer.OrdinalIgnoreCase))
            lvRules.Items.Add(new ListViewItem(new[] { r.Process, Layouts.LabelOf(r.Layout) }) { Tag = r });
        lvRules.EndUpdate();
    }

    void AssociateSelected()
    {
        if (lvProc.SelectedItems.Count == 0)
        {
            MessageBox.Show(this, "Select a program in the left list first.", "KbSwitch");
            return;
        }
        Associate(lvProc.SelectedItems[0].Text);
    }

    void Associate(string exe)
    {
        if (cbLayout.SelectedItem is not LayoutItem { Key: { } key }) return;

        var r = cfg.Rules.FirstOrDefault(x => x.Process.Equals(exe, StringComparison.OrdinalIgnoreCase));
        if (r == null)
        {
            r = new AppRule { Process = exe };
            cfg.Rules.Add(r);
        }
        r.Layout = key;
        cfg.Save();
        LoadRules();
        status.Text = $"{exe} assigned to {Layouts.LabelOf(key)}";
    }

    void RemoveSelected()
    {
        if (lvRules.SelectedItems.Count == 0 || lvRules.SelectedItems[0].Tag is not AppRule r) return;
        cfg.Rules.Remove(r);
        cfg.Save();
        LoadRules();
    }

    // ================= Start with Windows =================

    static bool StartupEnabled
    {
        get
        {
            using var k = Registry.CurrentUser.OpenSubKey(RunKey);
            return k?.GetValue(RunName) != null;
        }
    }

    static void SetStartup(bool on)
    {
        using var k = Registry.CurrentUser.CreateSubKey(RunKey);
        if (on) k.SetValue(RunName, $"\"{Environment.ProcessPath}\" --tray");
        else k.DeleteValue(RunName, false);
    }

    // ================= Window / tray =================

    protected override void SetVisibleCore(bool value)
    {
        if (hideOnStart)   // started with --tray: stay hidden in the notification area
        {
            hideOnStart = false;
            if (!IsHandleCreated) CreateHandle();
            value = false;
        }
        base.SetVisibleCore(value);
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        if (!procsLoaded) LoadProcesses();   // normal start (no --tray); otherwise ShowMain already loads the list
    }

    void ShowMain()
    {
        LoadProcesses();
        Show();
        if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
        Activate();
    }

    void ShowAbout()
    {
        using var dlg = new AboutForm();
        if (Visible) dlg.ShowDialog(this);
        else dlg.ShowDialog();   // window hidden in the tray: no owner, and no process list loaded
    }

    void Quit()
    {
        quitting = true;
        Close();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!quitting && cfg.CloseToTray && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;   // the close button hides the window instead of quitting
            Hide();
            if (!cfg.TrayHintShown)
            {
                tray.ShowBalloonTip(4000, "KbSwitch", "KbSwitch keeps running in the notification area. Right-click its icon and choose Quit to exit.", ToolTipIcon.Info);
                cfg.TrayHintShown = true;
                cfg.Save();
            }
            return;
        }
        tray.Visible = false;
        switcher.Dispose();
        base.OnFormClosing(e);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        tray.Dispose();
        base.OnFormClosed(e);
    }

    // ================= UI helpers =================

    static ListView MakeList(params (string Text, int Width)[] cols)
    {
        var lv = new ListView
        {
            View = View.Details, FullRowSelect = true, Dock = DockStyle.Fill,
            HideSelection = false, MultiSelect = false, GridLines = true
        };
        foreach (var (t, w) in cols) lv.Columns.Add(t, w);
        return lv;
    }

    static FlowLayoutPanel Flow(params Control[] cs)
    {
        var f = new FlowLayoutPanel
        {
            AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false, Dock = DockStyle.Fill, Margin = new Padding(0)
        };
        f.Controls.AddRange(cs);
        return f;
    }

    static Label Lbl(string t) => new() { Text = t, AutoSize = true, Margin = new Padding(3, 7, 3, 3) };

    static Label Hint(string t) => new()
    {
        Text = t, AutoSize = true, ForeColor = SystemColors.GrayText, Margin = new Padding(3, 7, 3, 3)
    };

    static Label Title(string t) => new()
    {
        Text = t, AutoSize = true, Font = new Font("Segoe UI", 10f, FontStyle.Bold), Margin = new Padding(3, 3, 3, 6)
    };
}
