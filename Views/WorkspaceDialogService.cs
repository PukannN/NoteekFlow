using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using NoteekFlow.Resources;

namespace NoteekFlow.Views;

public static class WorkspaceDialogService
{
    public static async Task<string?> PromptForTextAsync(
        string title,
        string? initialText,
        string? prompt = null)
    {
        var input = new TextBox
        {
            Text = initialText ?? string.Empty,
            MinWidth = 340,
            Margin = new Thickness(0, 8, 0, 14)
        };
        var dialog = new Window
        {
            Title = title,
            Width = 420,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };
        var cancelButton = new Button { Content = Strings.Cancel, MinWidth = 90 };
        var confirmButton = new Button { Content = Strings.Confirm, MinWidth = 90 };
        cancelButton.Click += (_, _) => dialog.Close(null);
        confirmButton.Click += (_, _) => dialog.Close(input.Text);
        dialog.Content = new StackPanel
        {
            Margin = new Thickness(18),
            Spacing = 8,
            Children =
            {
                new TextBlock { Text = prompt ?? Strings.EnterName },
                input,
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 8,
                    Children = { cancelButton, confirmButton }
                }
            }
        };

        var result = await dialog.ShowDialog<string?>(GetOwner());
        return result;
    }

    public static async Task<bool> ConfirmAsync(string message)
    {
        var dialog = new Window
        {
            Title = Strings.Delete,
            Width = 420,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };
        var confirmButton = new Button { Content = Strings.Confirm, MinWidth = 90 };
        var cancelButton = new Button { Content = Strings.Cancel, MinWidth = 90 };
        confirmButton.Click += (_, _) => dialog.Close(true);
        cancelButton.Click += (_, _) => dialog.Close(false);

        dialog.Content = new StackPanel
        {
            Margin = new Thickness(18),
            Spacing = 14,
            Children =
            {
                new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 8,
                    Children = { cancelButton, confirmButton }
                }
            }
        };

        return await dialog.ShowDialog<bool>(GetOwner());
    }

    private static Window GetOwner() =>
        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow
        ?? throw new System.InvalidOperationException("The main window is not available for a dialog.");
}
