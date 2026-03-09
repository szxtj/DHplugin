using Avalonia.Controls;
using VisualProgramming.ViewModels;

namespace VisualProgramming.Views
{
    public partial class DisplayControl2 : UserControl
    {
        public DisplayControl2()
        {
            InitializeComponent();
            DataContext = new DisplayControl2ViewModel();
        }
    }
}