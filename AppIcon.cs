namespace AutoKbSwitch;

/// <summary>The app icon, loaded from AutoKbSwitch.ico (embedded in the exe, see AutoKbSwitch.csproj).</summary>
internal static class AppIcon
{
    /// <summary>The icon at the default size, or at the closest size stored in the .ico file.</summary>
    public static Icon Create(Size? size = null)
    {
        using var s = typeof(AppIcon).Assembly.GetManifestResourceStream("AutoKbSwitch.ico")
                      ?? throw new InvalidOperationException("AutoKbSwitch.ico is not embedded in the assembly.");
        return size is { } sz ? new Icon(s, sz) : new Icon(s);
    }
}
