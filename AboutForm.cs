using System.Diagnostics;
using System.Reflection;

namespace KbSwitch;

/// <summary>Version / copyright / project URL, read from the assembly metadata (see KbSwitch.csproj).</summary>
internal static class AppInfo
{
    public const string ProjectUrl = "https://github.com/crazyball/KbSwitch";

    static readonly Assembly Asm = typeof(AppInfo).Assembly;

    public static string Version =>
        Asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "?";

    public static string Copyright =>
        Asm.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright ?? "";
}

internal sealed class AboutForm : Form
{
    public AboutForm()
    {
        Text = "About KbSwitch";
        Icon = AppIcon.Create();
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(440, 290);   // fixed size (scaled by DPI): wrapped labels do not drive AutoSize reliably
        Padding = new Padding(16);

        var link = new LinkLabel { Text = AppInfo.ProjectUrl, AutoSize = true };
        link.LinkClicked += (_, _) =>
        {
            try { Process.Start(new ProcessStartInfo(AppInfo.ProjectUrl) { UseShellExecute = true }); }
            catch { /* no default browser: nothing to do */ }
        };

        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, AutoSize = true, Anchor = AnchorStyles.Right };
        AcceptButton = ok;
        CancelButton = ok;

        var panel = new TableLayoutPanel
        {
            AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1, Dock = DockStyle.Fill
        };
        panel.Controls.Add(new Label
        {
            Text = "KbSwitch", AutoSize = true, Font = new Font("Segoe UI", 16f, FontStyle.Bold)
        });
        panel.Controls.Add(Line($"Version {AppInfo.Version}"));
        panel.Controls.Add(Line("Automatically switches the keyboard layout depending on the foreground application.", top: 10));
        panel.Controls.Add(Line(AppInfo.Copyright, top: 10));
        panel.Controls.Add(Line("Released under the MIT license."));
        panel.Controls.Add(Line("KbSwitch does not collect or send any data.", top: 10));
        link.Margin = new Padding(3, 10, 3, 3);
        panel.Controls.Add(link);
        ok.Margin = new Padding(3, 14, 3, 3);
        panel.Controls.Add(ok);
        Controls.Add(panel);
    }

    static Label Line(string text, int top = 3) => new()
    {
        Text = text, AutoSize = true, MaximumSize = new Size(400, 0), Margin = new Padding(3, top, 3, 3)
    };
}
