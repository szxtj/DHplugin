using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using System.Threading.Tasks;

namespace VisualProgramming.Views
{
    public class AlgorithmCapableTcpRealtimeWaveformControl : UserControl
    {
        private sealed class ChannelDescriptor
        {
            public ChannelDescriptor(string key, string display)
            {
                Key = key;
                Display = display;
            }

            public string Key { get; }
            public string Display { get; }
            public override string ToString() => Display;
        }

        private readonly ObservableCollection<ChannelDescriptor> _channels = new();
        private AlgorithmCapableTcpRealtimeWaveformCanvas _canvas = null!;
        private TextBox _ipBox = null!;
        private TextBox _portBox = null!;
        private ComboBox _channelCombo = null!;
        private TextBlock _statusText = null!;
        private TextBlock _lastPacketText = null!;
        private TextBlock _windowInfoText = null!;
        private Button _connectButton = null!;
        private Button _disconnectButton = null!;
        private ComboBox _algorithmCombo = null!;
        private Button _applyAlgorithmButton = null!;
        private Button _resetAlgorithmButton = null!;
        private Button _configureAlgorithmButton = null!;
        private Window? _parameterWindow;

        private TcpRealtimeIngestor? _ingestor;
        private string? _selectedChannelKey;
        private string? _selectedAlgorithm;

        public AlgorithmCapableTcpRealtimeWaveformControl()
        {
            Content = BuildLayout();

            _connectButton.Click += async (_, _) => await ConnectAsync();
            _disconnectButton.Click += async (_, _) => await DisconnectAsync();
            _channelCombo.SelectionChanged += (_, _) =>
            {
                if (_channelCombo.SelectedItem is ChannelDescriptor descriptor)
                {
                    _selectedChannelKey = descriptor.Key;
                }
                else
                {
                    _selectedChannelKey = null;
                }
                _canvas.Reset();
            };

            _applyAlgorithmButton.Click += (_, _) => ApplyAlgorithm();
            _resetAlgorithmButton.Click += (_, _) => ResetAlgorithm();
            _configureAlgorithmButton.Click += (_, _) => ShowAlgorithmParameters();
            _algorithmCombo.SelectionChanged += (_, _) =>
            {
                if (_algorithmCombo.SelectedItem is string algorithm)
                {
                    _selectedAlgorithm = algorithm;
                }
            };
        }

        private Control BuildLayout()
        {
            _ipBox = new TextBox { Width = 140, Text = "127.0.0.1" };
            _portBox = new TextBox { Width = 80, Text = "4008" };
            _channelCombo = new ComboBox
            {
                Width = 210,
                PlaceholderText = "等待数据..."
            };
            _channelCombo.ItemsSource = _channels;

            _algorithmCombo = new ComboBox
            {
                Width = 150,
                PlaceholderText = "选择算法..."
            };

            var processor = new VisualProgramming.Services.RealtimeWaveformProcessor();
            var algorithms = processor.GetAvailableAlgorithms();
            foreach (var algorithm in algorithms)
            {
                _algorithmCombo.Items.Add(algorithm);
            }

            _statusText = new TextBlock
            {
                Text = "未连接",
                Foreground = Brushes.OrangeRed,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                FontWeight = FontWeight.Bold
            };
            _connectButton = new Button
            {
                Content = "连接",
                Padding = new Thickness(16, 5),
                Background = new SolidColorBrush(Color.Parse("#28A745")),
                Foreground = Brushes.White
            };
            _disconnectButton = new Button
            {
                Content = "断开",
                Padding = new Thickness(16, 5),
                Background = new SolidColorBrush(Color.Parse("#AA3333")),
                Foreground = Brushes.White,
                IsEnabled = false
            };
            _lastPacketText = new TextBlock
            {
                Text = "暂无数据",
                Foreground = Brushes.LightGreen,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            _windowInfoText = new TextBlock
            {
                Text = "当前窗口：0ms - 4000ms",
                Foreground = Brushes.LightGray,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            _applyAlgorithmButton = new Button
            {
                Content = "应用算法",
                Padding = new Thickness(16, 5),
                Background = new SolidColorBrush(Color.Parse("#17A2B8")),
                Foreground = Brushes.White
            };

            _resetAlgorithmButton = new Button
            {
                Content = "重置算法",
                Padding = new Thickness(16, 5),
                Background = new SolidColorBrush(Color.Parse("#6C757D")),
                Foreground = Brushes.White
            };

            _configureAlgorithmButton = new Button
            {
                Content = "配置参数",
                Padding = new Thickness(16, 5),
                Background = new SolidColorBrush(Color.Parse("#FFC107")),
                Foreground = Brushes.Black
            };

            var topRow = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Spacing = 8,
                Children =
                {
                    new TextBlock{ Text = "TCP服务器:", Foreground = Brushes.White, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center },
                    _ipBox,
                    new TextBlock{ Text = "端口:", Foreground = Brushes.White, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center },
                    _portBox,
                    _connectButton,
                    _disconnectButton,
                    _statusText
                }
            };

            var secondRow = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Spacing = 10,
                Children =
                {
                    new TextBlock{ Text = "通道:", Foreground = Brushes.White, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center },
                    _channelCombo,
                    new TextBlock{ Text = "算法:", Foreground = Brushes.White, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center },
                    _algorithmCombo,
                    _applyAlgorithmButton,
                    _resetAlgorithmButton,
                    _configureAlgorithmButton,
                    _lastPacketText,
                    _windowInfoText
                }
            };

            _canvas = new AlgorithmCapableTcpRealtimeWaveformCanvas();
            _canvas.WindowRangeChanged += (start, end) =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    _windowInfoText.Text = $"当前窗口：{start:0}ms - {end:0}ms";
                });
            };
            _canvas.Reset();

            var grid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition(GridLength.Auto),
                    new RowDefinition(GridLength.Auto),
                    new RowDefinition(GridLength.Star),
                },
                RowSpacing = 12,
            };
            grid.Children.Add(topRow);
            Grid.SetRow(secondRow, 1);
            grid.Children.Add(secondRow);
            Grid.SetRow(_canvas, 2);
            grid.Children.Add(_canvas);

            return new Border
            {
                Background = new SolidColorBrush(Color.Parse("#1C1F26")),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16),
                Child = grid,
                Height = 420,
                MinWidth = 900
            };
        }

        private void ApplyAlgorithm()
        {
            if (!string.IsNullOrEmpty(_selectedAlgorithm))
            {
                _canvas.SetAlgorithm(_selectedAlgorithm, true);
                _canvas.ReapplyCurrentAlgorithm();
            }
        }

        public void ApplyAlgorithmByName(string algorithmName)
        {
            _selectedAlgorithm = algorithmName;
            _algorithmCombo.SelectedItem = algorithmName;
            ApplyAlgorithm();
            _canvas?.SetAlgorithm(_selectedAlgorithm, true);
        }

        public void ResetAlgorithmByName()
        {
            ResetAlgorithm();
        }

        private void ResetAlgorithm()
        {
            _canvas.SetAlgorithm(string.Empty, false);
            _algorithmCombo.SelectedItem = null;
            System.Diagnostics.Debug.WriteLine("算法已重置");
        }

        private void ShowAlgorithmParameters()
        {
            if (string.IsNullOrEmpty(_selectedAlgorithm))
            {
                return;
            }

            if (_parameterWindow != null)
            {
                _parameterWindow.Close();
            }

            var window = new Window
            {
                Title = $"配置算法参数: {_selectedAlgorithm}",
                Width = 450,
                Height = 550,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Background = new SolidColorBrush(Color.Parse("#1C1F26"))
            };

            var parameterControl = new Controls.WaveformAlgorithmParameterControl();
            parameterControl.SetAlgorithm(_selectedAlgorithm);
            
            parameterControl.ParametersApplied += () =>
            {
                var parameters = parameterControl.GetAppliedParameters();
                _canvas.SetAlgorithmParameters(parameters);
                _canvas.SetAlgorithm(_selectedAlgorithm, true);
            };

            window.Content = parameterControl;
            window.Show();
            _parameterWindow = window;

            window.Closed += (_, _) => _parameterWindow = null;
        }

        private async Task ConnectAsync()
        {
            if (_ingestor != null)
            {
                await DisconnectAsync();
            }

            if (!int.TryParse(_portBox.Text, out int port))
            {
                UpdateStatus("端口无效", false);
                return;
            }

            string host = string.IsNullOrWhiteSpace(_ipBox.Text) ? "127.0.0.1" : _ipBox.Text.Trim();

            _ingestor = new TcpRealtimeIngestor(host, port);
            _ingestor.SamplesReceived += OnSamplesReceived;
            _ingestor.ConnectionStatusChanged += OnConnectionStatusChanged;
            await _ingestor.StartAsync(default);
        }

        private void UpdateStatus(string message, bool connected)
        {
            _statusText.Text = message;
            _statusText.Foreground = connected ? Brushes.LimeGreen : Brushes.OrangeRed;
            if (!connected)
            {
                _connectButton.IsEnabled = true;
                _disconnectButton.IsEnabled = false;
            }
        }

        private void OnSamplesReceived(object? sender, TcpSamplesEventArgs e)
        {
            Dispatcher.UIThread.Post(() =>
            {
                var descriptor = _channels.FirstOrDefault(c => c.Key == e.ChannelKey);
                if (descriptor == null)
                {
                    descriptor = new ChannelDescriptor(e.ChannelKey, e.DisplayName);
                    _channels.Add(descriptor);
                }

                if (_selectedChannelKey == null)
                {
                    _selectedChannelKey = descriptor.Key;
                    _channelCombo.SelectedItem = descriptor;
                }

                if (_selectedChannelKey == e.ChannelKey)
                {
                    _channelCombo.SelectedItem = descriptor;
                    _canvas.AppendSamples(e.Samples, e.SampleIntervalMs);
                    _lastPacketText.Text = $"最近包：{e.Timestamp:HH:mm:ss}";
                }
            });
        }

        private void OnConnectionStatusChanged(object? sender, TcpConnectionStatusEventArgs e)
        {
            Dispatcher.UIThread.Post(() =>
            {
                UpdateStatus(e.Message, e.IsConnected);
                _connectButton.IsEnabled = !e.IsConnected;
                _disconnectButton.IsEnabled = e.IsConnected;
            });
        }

        private async Task DisconnectAsync()
        {
            if (_ingestor != null)
            {
                _ingestor.SamplesReceived -= OnSamplesReceived;
                _ingestor.ConnectionStatusChanged -= OnConnectionStatusChanged;
                await _ingestor.DisposeAsync();
                _ingestor = null;
            }

            _channels.Clear();
            _canvas.Reset();
            _selectedChannelKey = null;
            _channelCombo.SelectedItem = null;
            _lastPacketText.Text = "暂无数据";
            _windowInfoText.Text = "当前窗口：0ms - 2000ms";
            UpdateStatus("未连接", false);
        }

        protected override async void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            await DisconnectAsync();
            base.OnDetachedFromVisualTree(e);
        }
    }
}
