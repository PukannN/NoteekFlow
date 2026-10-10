
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using NoteekFlow.Models;

namespace NoteekFlow.ViewModels;

public partial class FileContentViewModel : ObservableObject
{
    [ObservableProperty] 
    private FileItem? fileItem;
}

public partial class DefaultWorkspaceViewModel : FileContentViewModel
{

}

public partial class ImageVisualizerViewModel : FileContentViewModel
{
    [ObservableProperty] 
    private Bitmap? _imageSource;
    
    [ObservableProperty]
    private string? _fileSource;

    public async Task LoadImageAsync(string filePath)
    {
        try
        {
            byte[] fileBytes = await Task.Run(() => File.ReadAllBytes(filePath));

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ImageSource?.Dispose();

                using (var memoryStream = new MemoryStream(fileBytes))
                {
                    ImageSource = new Bitmap(memoryStream);
                }
            });
        }
            catch (Exception ex)
        {
            Debug.Print($"Error loading image: {ex.Message}");
            ImageSource = null;
        }
        
    }
}