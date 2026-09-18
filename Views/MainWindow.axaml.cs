using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Diagnostics;
using System.Diagnostics;
using NoteekFlow.ViewModels;

namespace NoteekFlow.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

    }

    protected override void OnOpened(System.EventArgs e)
    {
        base.OnOpened(e);

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel != null)
        {
            DataContext = new MainViewModel(topLevel.StorageProvider);
        }

    }



}