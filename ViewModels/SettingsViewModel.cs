using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NoteekFlow.Models;
using NoteekFlow.Resources;

namespace NoteekFlow.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly UserPreferencesStore _store;
    private readonly UserPreferences _preferences;

    [ObservableProperty]
    private string _theme = "System";

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string _accentColor = "Blue";

    [ObservableProperty]
    private double _explorerWidth = 280;

    public event Action<double>? ExplorerWidthChanged;
    public event Action? LanguageChanged;

    public SettingsViewModel(UserPreferencesStore store, UserPreferences preferences)
    {
        _store = store;
        _preferences = preferences;
        Theme = preferences.Theme is "Dark" or "Light" ? preferences.Theme : "System";
        _preferences.Theme = Theme;
        AccentColor = preferences.AccentColor is "Blue" or "Green" or "Purple"
            ? preferences.AccentColor
            : "Blue";
        _preferences.AccentColor = AccentColor;
        ExplorerWidth = Math.Clamp(preferences.ExplorerWidth, 220, 600);
        _preferences.ExplorerWidth = ExplorerWidth;
        Language = LanguageService.Normalize(preferences.Language);
        _preferences.Language = Language;
        LanguageService.Apply(Language);
    }

    [ObservableProperty]
    private string _language = "en";

    [RelayCommand]
    private void UseEnglish() => SetLanguage("en");

    [RelayCommand]
    private void UseCzech() => SetLanguage("cs");

    [RelayCommand]
    private void UseSystemTheme() => SetTheme("System");

    [RelayCommand]
    private void UseDarkTheme() => SetTheme("Dark");

    [RelayCommand]
    private void UseLightTheme() => SetTheme("Light");

    [RelayCommand]
    private void UseBlueAccent() => SetAccentColor("Blue");

    [RelayCommand]
    private void UseGreenAccent() => SetAccentColor("Green");

    [RelayCommand]
    private void UsePurpleAccent() => SetAccentColor("Purple");

    [RelayCommand]
    private void SetCompactExplorer() => SetExplorerWidth(220);

    [RelayCommand]
    private void SetStandardExplorer() => SetExplorerWidth(280);

    [RelayCommand]
    private void SetWideExplorer() => SetExplorerWidth(360);

    public void SetLastWorkspace(string path)
    {
        _preferences.LastWorkspacePath = path;
        SavePreferences();
    }

    private void SetTheme(string theme)
    {
        Theme = theme;
        _preferences.Theme = theme;
        SavePreferences();
        ApplyTheme();
    }

    public void ApplyTheme()
    {
        var variant = Theme switch
        {
            "Dark" => ThemeVariant.Dark,
            "Light" => ThemeVariant.Light,
            _ => ThemeVariant.Default
        };

        if (Application.Current is not null)
        {
            Application.Current.RequestedThemeVariant = variant;
            ApplyPalette(Application.Current, Theme == "Light" ? LightPalette : DarkPalette);
        }
    }

    private static readonly IReadOnlyDictionary<string, string> DarkPalette = new Dictionary<string, string>
    {
        ["AppBackground"] = "#181A1F",
        ["PanelBackground"] = "#202328",
        ["PanelSecondary"] = "#1E2127",
        ["PanelRaised"] = "#23262D",
        ["PanelBorder"] = "#34383F",
        ["ButtonBackground"] = "#292D34",
        ["ButtonHover"] = "#363B44",
        ["TextPrimary"] = "#D9DEE7",
        ["TextSecondary"] = "#8F98A6",
        ["ErrorForeground"] = "#E59A8A",
        ["AccentBackground"] = "#315B87",
        ["AccentBorder"] = "#4778A8",
        ["CanvasBackground"] = "#FBFAF7",
        ["CanvasInk"] = "#20242B",
        ["MarkdownCodeBackground"] = "#22262D",
        ["MarkdownLink"] = "#77AADD",
        ["MarkdownQuote"] = "#527BA5",
        ["DirtyIndicator"] = "#D6A85F",
        ["ThumbnailBackground"] = "#30343B",
        ["ThumbnailForeground"] = "#A9B7C6"
    };

    private static readonly IReadOnlyDictionary<string, string> LightPalette = new Dictionary<string, string>
    {
        ["AppBackground"] = "#F5F6F8",
        ["PanelBackground"] = "#FFFFFF",
        ["PanelSecondary"] = "#ECEEF2",
        ["PanelRaised"] = "#E5E8ED",
        ["PanelBorder"] = "#D1D5DC",
        ["ButtonBackground"] = "#E3E6EB",
        ["ButtonHover"] = "#D5D9E0",
        ["TextPrimary"] = "#222831",
        ["TextSecondary"] = "#667085",
        ["ErrorForeground"] = "#B42318",
        ["AccentBackground"] = "#DCEBFA",
        ["AccentBorder"] = "#8DB3D9",
        ["CanvasBackground"] = "#FFFFFF",
        ["CanvasInk"] = "#20242B",
        ["MarkdownCodeBackground"] = "#ECEEF2",
        ["MarkdownLink"] = "#175DA8",
        ["MarkdownQuote"] = "#527BA5",
        ["DirtyIndicator"] = "#A15C00",
        ["ThumbnailBackground"] = "#DCE1E8",
        ["ThumbnailForeground"] = "#344054"
    };

    private void ApplyPalette(Application application, IReadOnlyDictionary<string, string> palette)
    {
        foreach (var (key, color) in palette)
        {
            application.Resources[key] = new SolidColorBrush(Color.Parse(color));
        }

        var accent = AccentColor switch
        {
            "Green" => ("#26734D", "#398D63"),
            "Purple" => ("#6941A5", "#825BC1"),
            _ => ("#315B87", "#4778A8")
        };
        application.Resources["AccentBackground"] = new SolidColorBrush(Color.Parse(accent.Item1));
        application.Resources["AccentBorder"] = new SolidColorBrush(Color.Parse(accent.Item2));
    }

    private void SetAccentColor(string color)
    {
        AccentColor = color;
        _preferences.AccentColor = color;
        SavePreferences();
        ApplyTheme();
    }

    private void SetExplorerWidth(double width)
    {
        ExplorerWidth = width;
        _preferences.ExplorerWidth = width;
        SavePreferences();
        ExplorerWidthChanged?.Invoke(width);
    }

    private void SetLanguage(string language)
    {
        language = LanguageService.Normalize(language);
        if (Language == language)
        {
            return;
        }

        Language = language;
        _preferences.Language = language;
        LanguageService.Apply(language);
        SavePreferences();
        LanguageChanged?.Invoke();
    }

    private void SavePreferences()
    {
        try
        {
            _store.Save(_preferences);
            ErrorMessage = null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException)
        {
            ErrorMessage = Strings.Format("SaveSettingsError", ex.Message);
        }
    }
}
