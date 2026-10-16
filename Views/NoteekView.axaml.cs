using Avalonia.Controls;
using Avalonia;
using NoteekFlow.Resources;
using NoteekFlow.ViewModels;

namespace NoteekFlow.Views;

public partial class NoteekView : UserControl
{
    public NoteekView()
    {
        InitializeComponent();
        InkCanvas.TextRequested += OnTextRequested;
    }

    private async void OnTextRequested(Point position)
    {
        var text = await WorkspaceDialogService.PromptForTextAsync(
            Strings.TextTool,
            null,
            Strings.EnterCanvasText);
        if (text is not null && DataContext is NoteekViewModel viewModel)
        {
            viewModel.AddText(position, text);
        }
    }
}
