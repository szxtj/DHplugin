using System.ComponentModel;

namespace VisualProgramming.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private string _title = "VisualProgramming Application";
    
    public string Title
    {
        get => _title;
        set => SetField(ref _title, value);
    }
    
    public string Greeting => "Welcome to Avalonia!";
}