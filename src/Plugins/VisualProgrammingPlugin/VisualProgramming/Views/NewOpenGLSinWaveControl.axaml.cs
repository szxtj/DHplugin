using Avalonia.Controls;
using Avalonia.Interactivity;
using VisualProgramming.ViewModels;

namespace VisualProgramming.Views
{
    public partial class NewOpenGLSinWaveControl : UserControl
    {
        public NewOpenGLSinWaveControl()
        {
            InitializeComponent();
            var viewModel = new NewOpenGLSinWaveViewModel();
            DataContext = viewModel;
            
            // 获取控件并设置属性绑定
            var openGLWaveView = this.FindControl<OpenGLSinWaveControl>("OpenGLWaveView");
            if (openGLWaveView != null)
            {
                // 设置绑定以连接控件和ViewModel属性
                openGLWaveView.Bind(OpenGLSinWaveControl.AmplitudeProperty, 
                    new Avalonia.Data.Binding("Amplitude"));
                openGLWaveView.Bind(OpenGLSinWaveControl.FrequencyProperty, 
                    new Avalonia.Data.Binding("Frequency"));
                openGLWaveView.Bind(OpenGLSinWaveControl.SpeedProperty, 
                    new Avalonia.Data.Binding("Speed"));
                openGLWaveView.Bind(OpenGLSinWaveControl.SelectedColorIndexProperty, 
                    new Avalonia.Data.Binding("SelectedColorIndex"));
                openGLWaveView.Bind(OpenGLSinWaveControl.PhaseProperty, 
                    new Avalonia.Data.Binding("Phase"));
            }
        }
    }
}