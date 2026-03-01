using Avalonia.Controls;
using Avalonia.Interactivity;

namespace CPlusPlusEditor;

public partial class CppEditorView : UserControl
{
    public CppEditorView()
    {
        InitializeComponent();
    }
    
    private async void OnCompile(object? sender, RoutedEventArgs e)
    {
        if (DataContext is CppEditorViewModel vm)
        {
            await vm.CompileAsync();
        }
    }
    
    private async void OnRun(object? sender, RoutedEventArgs e)
    {
        if (DataContext is CppEditorViewModel vm)
        {
            await vm.RunAsync();
        }
    }
    
    private void OnClearOutput(object? sender, RoutedEventArgs e)
    {
        if (DataContext is CppEditorViewModel vm)
        {
            vm.ClearOutput();
        }
    }
}
