namespace AutoKbSwitch;

internal static class Layouts
{
    public static IEnumerable<InputLanguage> All() =>
        InputLanguage.InstalledInputLanguages.Cast<InputLanguage>();

    public static string Key(InputLanguage l) => $"{l.Culture.Name}|{l.LayoutName}";

    public static string Label(InputLanguage l) => $"{l.LayoutName}  ({l.Culture.DisplayName})";

    public static InputLanguage? Find(string? key) =>
        key == null ? null : All().FirstOrDefault(l => Key(l) == key);

    public static string LabelOf(string key) =>
        Find(key) is { } l ? Label(l) : $"{key} (not found)";
}
