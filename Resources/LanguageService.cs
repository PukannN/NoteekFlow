using System;
using System.Globalization;

namespace NoteekFlow.Resources;

public static class LanguageService
{
    public static string Normalize(string? language) =>
        string.Equals(language, "cs", StringComparison.OrdinalIgnoreCase) ? "cs" : "en";

    public static void Apply(string? language)
    {
        var culture = CultureInfo.GetCultureInfo(Normalize(language));
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }
}
