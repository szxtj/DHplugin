using Avalonia.Controls;
using VisualProgramming.ViewModels;

namespace VisualProgramming.Views
{
    public partial class NewOxyPlotSinWaveControl : UserControl
    {
        public NewOxyPlotSinWaveControl()
        {
            InitializeComponent();
            DataContext = new NewOxyPlotSinWaveViewModel();
        }
    }
}