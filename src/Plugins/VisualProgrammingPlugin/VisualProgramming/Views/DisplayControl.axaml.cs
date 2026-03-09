using Avalonia.Controls;
using VisualProgramming.ViewModels;

namespace VisualProgramming.Views
{
    public partial class DisplayControl : UserControl
    {
        public DisplayControl()
        {
            InitializeComponent();
            DataContext = new DisplayControlViewModel();
        }
    }
}