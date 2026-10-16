using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia;
using Avalonia.VisualTree;
using Avalonia.Threading;
using Avalonia.Controls.ApplicationLifetimes;
using System.Linq;
using NoteekFlow.Models;
using NoteekFlow.ViewModels;


namespace NoteekFlow.Views;

public partial class MainWindow : Window
{
    private bool _isSavingBeforeClose;
    private bool _allowClose;

    public MainWindow()
    {
        InitializeComponent();
        Closing += OnWindowClosing;
    }

    public MainWindow(MainViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    protected override void OnOpened(System.EventArgs e)
    {
        base.OnOpened(e);
        if (DataContext is not MainViewModel viewModel)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel is not null)
            {
                viewModel = new MainViewModel(topLevel.StorageProvider);
                DataContext = viewModel;
            }
        }

        if (DataContext is MainViewModel mainViewModel)
        {
            mainViewModel.Settings.LanguageChanged += OnLanguageChanged;
        }
    }

    protected override void OnClosed(System.EventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.Settings.LanguageChanged -= OnLanguageChanged;
        }

        base.OnClosed(e);
    }

    private async void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_allowClose || DataContext is not MainViewModel viewModel)
        {
            return;
        }

        e.Cancel = true;
        if (_isSavingBeforeClose)
        {
            return;
        }

        _isSavingBeforeClose = true;
        try
        {
            if (await viewModel.SaveDirtyNoteekFilesAsync())
            {
                _allowClose = true;
                Close();
            }
        }
        finally
        {
            _isSavingBeforeClose = false;
        }
    }

    private void OnLanguageChanged()
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            {
                return;
            }

            var localizedWindow = new MainWindow(viewModel);
            desktop.MainWindow = localizedWindow;
            localizedWindow.Show();
            Close();
        });
    }

    private void OnFileExplorerPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not TreeView treeView
            || !e.GetCurrentPoint(treeView).Properties.IsRightButtonPressed
            || DataContext is not MainViewModel viewModel)
        {
            return;
        }

        var source = e.Source as Control;
        var itemContainer = source as TreeViewItem
            ?? source?.GetVisualAncestors().OfType<TreeViewItem>().FirstOrDefault();
        viewModel.FileExplorer.SelectContextMenuItem(itemContainer?.DataContext as FileSystemItem);
    }

    private void OnFileExplorerContextMenuOpened(object? sender, RoutedEventArgs e)
    {
        if (sender is ContextMenu contextMenu)
        {
            contextMenu.DataContext = DataContext;
        }
    }

}