using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Security;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NoteekFlow.Models;
using NoteekFlow.Resources;
using PDFtoImage;
using SkiaSharp;

namespace NoteekFlow.ViewModels;

public partial class PdfViewModel : ZoomableFileContentViewModel
{
    [ObservableProperty]
    private ObservableCollection<Bitmap> _pages = new();

    [ObservableProperty]
    private int _currentPageIndex;

    [ObservableProperty]
    private string? _errorMessage;

    public string? FileSource => FileItem?.FullPath;
    public Bitmap? CurrentPage => CurrentPageIndex >= 0 && CurrentPageIndex < Pages.Count
        ? Pages[CurrentPageIndex]
        : null;
    public double CurrentPageWidth => CurrentPage?.Size.Width * Zoom ?? 0;
    public double CurrentPageHeight => CurrentPage?.Size.Height * Zoom ?? 0;
    public bool CanGoToPreviousPage => CurrentPageIndex > 0;
    public bool CanGoToNextPage => CurrentPageIndex < Pages.Count - 1;
    public string PageInfo => Pages.Count == 0
        ? string.Empty
        : Strings.Format("PageCounter", CurrentPageIndex + 1, Pages.Count);

    public PdfViewModel(FileItem fileItem) : base(fileItem) { }

    protected override void OnZoomLevelChanged()
    {
        OnPropertyChanged(nameof(CurrentPageWidth));
        OnPropertyChanged(nameof(CurrentPageHeight));
    }

    partial void OnCurrentPageIndexChanged(int value)
    {
        OnPropertyChanged(nameof(CurrentPage));
        OnPropertyChanged(nameof(CurrentPageWidth));
        OnPropertyChanged(nameof(CurrentPageHeight));
        OnPropertyChanged(nameof(PageInfo));
        OnPropertyChanged(nameof(CanGoToPreviousPage));
        OnPropertyChanged(nameof(CanGoToNextPage));
    }
    partial void OnPagesChanged(ObservableCollection<Bitmap> value)
    {
        CurrentPageIndex = 0;
        OnPropertyChanged(nameof(CurrentPage));
        OnPropertyChanged(nameof(CurrentPageWidth));
        OnPropertyChanged(nameof(CurrentPageHeight));
        OnPropertyChanged(nameof(PageInfo));
        OnPropertyChanged(nameof(CanGoToPreviousPage));
        OnPropertyChanged(nameof(CanGoToNextPage));
    }

    public async Task LoadAsync()
    {
        if (FileItem is null)
        {
            return;
        }

        try
        {
            var pageImages = await Task.Run(() => RenderPages(FileItem.FullPath));
            DisposePages();
            foreach (var bytes in pageImages)
            {
                using var stream = new MemoryStream(bytes);
                Pages.Add(new Bitmap(stream));
            }
            ErrorMessage = null;
            OnPropertyChanged(nameof(CurrentPage));
            OnPropertyChanged(nameof(PageInfo));
            OnPropertyChanged(nameof(CanGoToPreviousPage));
            OnPropertyChanged(nameof(CanGoToNextPage));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException
            or InvalidDataException or ArgumentException or InvalidOperationException or DllNotFoundException
            or PlatformNotSupportedException)
        {
            ErrorMessage = ex is PlatformNotSupportedException
                ? Strings.PdfPlatformUnsupported
                : Strings.Format("PdfRenderError", ex.Message);
        }
    }

    private static IReadOnlyList<byte[]> RenderPages(string filePath)
    {
        if (OperatingSystem.IsWindows()
            || OperatingSystem.IsLinux()
            || OperatingSystem.IsMacOS()
            || OperatingSystem.IsAndroidVersionAtLeast(31)
            || OperatingSystem.IsBrowser()
            || OperatingSystem.IsIOSVersionAtLeast(13, 6)
            || OperatingSystem.IsMacCatalystVersionAtLeast(13, 5))
        {
            using var stream = File.OpenRead(filePath);
            var pages = new List<byte[]>();
            foreach (var page in Conversion.ToImages(stream))
            {
                using (page)
                using (var encoded = page.Encode(SKEncodedImageFormat.Png, 100))
                {
                    pages.Add(encoded.ToArray());
                }
            }

            return pages;
        }

        throw new PlatformNotSupportedException();
    }

    public void DisposePages()
    {
        foreach (var page in Pages)
        {
            page.Dispose();
        }
        Pages.Clear();
    }

    [RelayCommand]
    private void PreviousPage()
    {
        if (CurrentPageIndex > 0)
        {
            CurrentPageIndex--;
            OnPropertyChanged(nameof(PageInfo));
        }
    }

    [RelayCommand]
    private void NextPage()
    {
        if (CurrentPageIndex < Pages.Count - 1)
        {
            CurrentPageIndex++;
            OnPropertyChanged(nameof(PageInfo));
        }
    }
}
