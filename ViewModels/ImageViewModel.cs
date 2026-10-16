using System;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using NoteekFlow.Models;
using NoteekFlow.Resources;

namespace NoteekFlow.ViewModels;

public partial class ImageViewModel : ZoomableFileContentViewModel
{
    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private Bitmap? _imageSource;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private string? _errorMessage;

    public ImageViewModel(FileItem fileItem) : base(fileItem) { }

    public double ImageWidth => ImageSource?.Size.Width * Zoom ?? 0;
    public double ImageHeight => ImageSource?.Size.Height * Zoom ?? 0;

    partial void OnImageSourceChanged(Bitmap? value)
    {
        OnPropertyChanged(nameof(ImageWidth));
        OnPropertyChanged(nameof(ImageHeight));
    }

    protected override void OnZoomLevelChanged()
    {
        OnPropertyChanged(nameof(ImageWidth));
        OnPropertyChanged(nameof(ImageHeight));
    }

    public async Task LoadImageAsync(string filePath)
    {
        try
        {
            var image = await Task.Run(() => new Bitmap(filePath));
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ImageSource?.Dispose();
                ImageSource = image;
                ErrorMessage = null;
            });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException or ArgumentException)
        {
            Debug.WriteLine($"Unable to load image '{filePath}': {ex.Message}");
            ErrorMessage = Strings.Format("ImageLoadError", ex.Message);
        }
    }

    public void DisposeImage()
    {
        ImageSource?.Dispose();
        ImageSource = null;
    }
}
