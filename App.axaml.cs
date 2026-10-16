using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using NoteekFlow.ViewModels;
using NoteekFlow.Views;
using NoteekFlow.Models;
using NoteekFlow.Resources;

namespace NoteekFlow;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            try
            {
                LanguageService.Apply(new UserPreferencesStore().Load().Language);
            }
            catch (Exception ex) when (UserPreferencesStore.IsRecoverableLoadError(ex))
            {
                LanguageService.Apply("en");
            }

            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}