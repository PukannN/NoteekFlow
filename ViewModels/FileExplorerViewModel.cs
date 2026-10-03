using System.Collections.ObjectModel;
using System.Diagnostics;
using NoteekFlow.Models;
using NoteekFlow.ViewModels;

public class FileExplorerViewModel : ViewModelBase
{
    public ObservableCollection<FileItem> Files { get;} = new();

    private FileItem? _selectedFile;
    public FileItem? SelectedFile
    {
        get => _selectedFile;
        set
        {
            if(SetProperty(ref _selectedFile, value) && value != null)
            {
                OnFileSelected(value);
            }
        }
    }

    private void OnFileSelected(FileItem file)
    {
        if(file.Type == FileItemType.Markdown)
        {
            Debug.Print("Openning file in Markdown editor");
        }
    }

}