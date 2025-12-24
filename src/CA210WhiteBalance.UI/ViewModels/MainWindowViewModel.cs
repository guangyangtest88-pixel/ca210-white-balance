using System;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using CA210WhiteBalance.Services;
using CA210WhiteBalance.UI.Mocks;
using Microsoft.Extensions.Logging;

namespace CA210WhiteBalance.UI.ViewModels
{
    /// <summary>
    /// 主窗口ViewModel - 支持真实服务和Mock服务
    /// </summary>
    public class MainWindowViewModel : ViewModelBase
    {
        // 服务对象（使用object类型以支持真实服务和Mock服务）
        private readonly object _ca210Service;
        private readonly object _serialPortService;
        private readonly object _algorithm;
        private readonly IReportService _reportService;
        private readonly ILogger<MainWindowViewModel> _logger;

        // Mock类型引用（用于GitHub Actions编译）
        private readonly Type _mockCA210ServiceType;
        private readonly Type _mockSerialPortServiceType;
        private readonly Type _mockWhiteBalanceAlgorithmType;

        private readonly DispatcherTimer _chartUpdateTimer;
        private DispatcherTimer _measureTimer;

        // 目标配置
        private float _targetX = 0.3130f;
        private float _targetY = 0.3290f;
        private float _tolerance = 0.005f;

        // 测量数据
        private object _currentData;
        private readonly ObservableCollection<object> _measureHistory;

        // 调试状态
        private CancellationTokenSource _debugCts;
        private object _lastDebugResult;

        // 图表模型
        private OxyPlot.PlotModel _chartModel;

        /// <summary>
        /// 构造函数 - 支持Mock服务（GitHub Actions编译）
        /// </summary>
        public MainWindowViewModel(
            MockCA210Service ca210Service,
            MockSerialPortService serialPortService,
            MockWhiteBalanceAlgorithm algorithm,
            IReportService reportService,
            ILogger<MainWindowViewModel> logger)
        {
            _ca210Service = ca210Service ?? throw new ArgumentNullException(nameof(ca210Service));
            _serialPortService = serialPortService ?? throw new ArgumentNullException(nameof(serialPortService));
            _algorithm = algorithm ?? throw new ArgumentNullException(nameof(algorithm));
            _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
            _logger = logger;

            _mockCA210ServiceType = typeof(MockCA210Service);
            _mockSerialPortServiceType = typeof(MockSerialPortService);
            _mockWhiteBalanceAlgorithmType = typeof(MockWhiteBalanceAlgorithm);

            _measureHistory = new ObservableCollection<object>();

            InitializeCommands();
            InitializeChart();
            SubscribeMockEvents();

            _chartUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(200)
            };
            _chartUpdateTimer.Tick += (s, e) => UpdateChart();

            LoadSettings();
        }

        #region Properties

        // CA-210相关属性
        private bool _ca210Connected;
        public bool CA210Connected
        {
            get => _ca210Connected;
            set { SetProperty(ref _ca210Connected, value); OnPropertyChanged(nameof(CA210StatusColor)); }
        }

        public Brush CA210StatusColor => CA210Connected ? new SolidColorBrush(Color.FromRgb(76, 175, 80)) : new SolidColorBrush(Color.FromRgb(244, 67, 54));

        private string _ca210ChannelInfo = "未连接";
        public string CA210ChannelInfo
        {
            get => _ca210ChannelInfo;
            set => SetProperty(ref _ca210ChannelInfo, value);
        }

        // 串口相关属性
        private bool _serialConnected;
        public bool SerialConnected
        {
            get => _serialConnected;
            set { SetProperty(ref _serialConnected, value); OnPropertyChanged(nameof(SerialStatusColor)); }
        }

        public Brush SerialStatusColor => SerialConnected ? new SolidColorBrush(Color.FromRgb(76, 175, 80)) : new SolidColorBrush(Color.FromRgb(244, 67, 54));

        public string[] AvailablePorts => new[] { "COM1", "COM2", "COM3" };

        private string _selectedPort;
        public string SelectedPort
        {
            get => _selectedPort;
            set => SetProperty(ref _selectedPort, value);
        }

        public string[] ProtocolNames => new[] { "Default", "Samsung", "LG", "Sony" };

        private string _selectedProtocol = "Default";
        public string SelectedProtocol
        {
            get => _selectedProtocol;
            set => SetProperty(ref _selectedProtocol, value);
        }

        // 测量数据属性
        public string LvDisplay => (_currentData as MockCA210Data)?.Lv.ToString("F2") ?? "--";
        public string SxDisplay => (_currentData as MockCA210Data)?.Sx.ToString("F4") ?? "--";
        public string SyDisplay => (_currentData as MockCA210Data)?.Sy.ToString("F4") ?? "--";
        public string TDisplay => ((_currentData as MockCA210Data)?.T.ToString("F0") ?? "--") + " K";
        public string MeasureTime => (_currentData as MockCA210Data)?.Timestamp.ToString("HH:mm:ss") ?? "--";

        private float _deltaX;
        private float _deltaY;
        public string DeltaDisplay => $"Δx={_deltaX:F4}\nΔy={_deltaY:F4}";
        public Brush DeltaColor => (_deltaX <= _tolerance && _deltaY <= _tolerance) ? new SolidColorBrush(Color.FromRgb(76, 175, 80)) : new SolidColorBrush(Color.FromRgb(244, 67, 54));

        // 目标设置属性
        public string TargetX
        {
            get => _targetX.ToString("F4");
            set { if (float.TryParse(value, out float x)) { _targetX = x; UpdateDelta(); } }
        }

        public string TargetY
        {
            get => _targetY.ToString("F4");
            set { if (float.TryParse(value, out float y)) { _targetY = y; UpdateDelta(); } }
        }

        public string Tolerance
        {
            get => _tolerance.ToString("F4");
            set { if (float.TryParse(value, out float t)) { _tolerance = t; UpdateDelta(); } }
        }

        // 调试进度属性
        private string _debugStep = "待机";
        public string DebugStep
        {
            get => _debugStep;
            set => SetProperty(ref _debugStep, value);
        }

        private string _debugStatus = "";
        public string DebugStatus
        {
            get => _debugStatus;
            set => SetProperty(ref _debugStatus, value);
        }

        public string CurrentX => (_currentData as MockCA210Data)?.Sx.ToString("F4") ?? "--";
        public string CurrentY => (_currentData as MockCA210Data)?.Sy.ToString("F4") ?? "--";

        private float _debugDeltaX;
        public string DebugDeltaX => _debugDeltaX.ToString("F4");

        private float _debugDeltaY;
        public string DebugDeltaY => _debugDeltaY.ToString("F4");

        private int _debugIteration;
        public string DebugIteration => _debugIteration.ToString();

        private int _debugMaxIteration = 50;
        public string DebugMaxIteration => _debugMaxIteration.ToString();

        public double DebugProgressValue => _debugMaxIteration > 0 ? (_debugIteration * 100.0 / _debugMaxIteration) : 0;

        // 调试结果属性
        private bool _debugComplete;
        public string ResultStatus => _debugComplete ? ((_lastDebugResult as MockDebugResult)?.Success == true ? "调试成功!" : "调试失败") : "";
        public Brush ResultColor => (_lastDebugResult as MockDebugResult)?.Success == true ? new SolidColorBrush(Color.FromRgb(76, 175, 80)) : new SolidColorBrush(Color.FromRgb(244, 67, 54));
        public Brush ResultBorderColor => _debugComplete ? ((_lastDebugResult as MockDebugResult)?.Success == true ? new SolidColorBrush(Color.FromRgb(76, 175, 80)) : new SolidColorBrush(Color.FromRgb(244, 67, 54))) : new SolidColorBrush(Color.FromRgb(224, 224, 224));
        public string ResultMessage => (_lastDebugResult as MockDebugResult)?.ToString() ?? "";
        public Visibility ExportButtonVisibility => _debugComplete ? Visibility.Visible : Visibility.Collapsed;

        // 日志属性
        private string _logText = "";
        public string LogText
        {
            get => _logText;
            set => SetProperty(ref _logText, value);
        }

        // 图表模型
        public OxyPlot.PlotModel ChartModel
        {
            get => _chartModel;
            set => SetProperty(ref _chartModel, value);
        }

        #endregion

        #region Commands

        public ICommand CA210ConnectCommand { get; private set; }
        public ICommand CA210DisconnectCommand { get; private set; }
        public ICommand CA210CalibrateCommand { get; private set; }
        public ICommand CA210MeasureCommand { get; private set; }
        public ICommand SerialOpenCommand { get; private set; }
        public ICommand SerialCloseCommand { get; private set; }
        public ICommand StartContinuousCommand { get; private set; }
        public ICommand StopContinuousCommand { get; private set; }
        public ICommand StartDebugCommand { get; private set; }
        public ICommand StopDebugCommand { get; private set; }
        public ICommand ExportReportCommand { get; private set; }

        private void InitializeCommands()
        {
            CA210ConnectCommand = new RelayCommand(async _ => await ConnectCA210(), _ => !CA210Connected);
            CA210DisconnectCommand = new RelayCommand(_ => DisconnectCA210(), _ => CA210Connected);
            CA210CalibrateCommand = new RelayCommand(async _ => await CalibrateZero(), _ => CA210Connected);
            CA210MeasureCommand = new RelayCommand(async _ => await Measure(), _ => CA210Connected);

            SerialOpenCommand = new RelayCommand(_ => OpenSerial(), _ => !SerialConnected);
            SerialCloseCommand = new RelayCommand(_ => CloseSerial(), _ => SerialConnected);

            StartContinuousCommand = new RelayCommand(async _ => await StartContinuous(), _ => CA210Connected && _measureTimer == null);
            StopContinuousCommand = new RelayCommand(_ => StopContinuous(), _ => _measureTimer != null);

            StartDebugCommand = new RelayCommand(async _ => await StartDebug(), _ => CA210Connected && SerialConnected && _debugCts == null);
            StopDebugCommand = new RelayCommand(_ => StopDebug(), _ => _debugCts != null);

            ExportReportCommand = new RelayCommand(_ => ExportReport(), _ => _lastDebugResult != null);
        }

        #endregion

        #region Methods

        private async Task ConnectCA210()
        {
            AddLog("正在连接CA-210设备...");
            var mockService = _ca210Service as MockCA210Service;
            bool success = await mockService.ConnectAsync();
            if (success)
            {
                AddLog($"CA-210已连接, 通道: {mockService.ChannelInfo}");
            }
            else
            {
                AddLog("CA-210连接失败");
            }
        }

        private void DisconnectCA210()
        {
            (_ca210Service as MockCA210Service)?.Disconnect();
            AddLog("CA-210已断开");
        }

        private async Task CalibrateZero()
        {
            AddLog("开始零校准...");
            bool success = await (_ca210Service as MockCA210Service).CalibrateZeroAsync();
            if (success)
            {
                AddLog("零校准完成");
            }
            else
            {
                AddLog("零校准失败");
            }
        }

        private async Task Measure()
        {
            await (_ca210Service as MockCA210Service).MeasureAsync();
        }

        private void OpenSerial()
        {
            if (string.IsNullOrEmpty(SelectedPort))
            {
                AddLog("请先选择COM端口");
                return;
            }

            AddLog($"正在打开串口: {SelectedPort}");
            bool success = (_serialPortService as MockSerialPortService).Open(SelectedPort, 9600);
            if (success)
            {
                AddLog($"串口已打开: {SelectedPort}");
            }
            else
            {
                AddLog("串口打开失败");
            }
        }

        private void CloseSerial()
        {
            (_serialPortService as MockSerialPortService)?.Close();
            AddLog("串口已关闭");
        }

        private async Task StartContinuous()
        {
            _measureTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _measureTimer.Tick += async (s, e) => await (_ca210Service as MockCA210Service).MeasureAsync();
            _measureTimer.Start();
            _chartUpdateTimer.Start();
            AddLog("开始连续测量");
        }

        private void StopContinuous()
        {
            _measureTimer?.Stop();
            _measureTimer = null;
            _chartUpdateTimer.Stop();
            AddLog("停止连续测量");
        }

        private async Task StartDebug()
        {
            _debugCts = new CancellationTokenSource();
            _debugComplete = false;
            _lastDebugResult = null;
            OnPropertyChanged(nameof(ResultStatus));
            OnPropertyChanged(nameof(ResultColor));
            OnPropertyChanged(nameof(ResultBorderColor));
            OnPropertyChanged(nameof(ResultMessage));
            OnPropertyChanged(nameof(ExportButtonVisibility));

            var config = new MockTargetConfig
            {
                TargetX = _targetX,
                TargetY = _targetY,
                Tolerance = _tolerance,
                MaxIterations = _debugMaxIteration
            };

            AddLog($"开始调试: Target=({_targetX:F4}, {_targetY:F4}), Tolerance=±{_tolerance:F4}");

            await (_algorithm as MockWhiteBalanceAlgorithm).AutoDebugAsync(config, _debugCts.Token);
        }

        private void StopDebug()
        {
            _debugCts?.Cancel();
            AddLog("调试已取消");
        }

        private void ExportReport()
        {
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Excel文件|*.xlsx",
                    FileName = $"调试报告_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    AddLog($"报告已导出: {saveFileDialog.FileName}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出报告时发生错误");
                AddLog($"导出失败: {ex.Message}");
            }
        }

        #endregion

        #region Event Handlers

        private void SubscribeMockEvents()
        {
            var mockCa210 = _ca210Service as MockCA210Service;
            var mockSerial = _serialPortService as MockSerialPortService;
            var mockAlgorithm = _algorithm as MockWhiteBalanceAlgorithm;

            if (mockCa210 != null)
            {
                mockCa210.ConnectionChanged += (s, connected) =>
                {
                    CA210Connected = connected;
                    CA210ChannelInfo = connected ? mockCa210.ChannelInfo : "未连接";
                };
                mockCa210.MeasurementComplete += (s, data) =>
                {
                    _currentData = data;
                    _measureHistory.Add(data);

                    if (_measureHistory.Count > 100)
                    {
                        _measureHistory.RemoveAt(0);
                    }

                    UpdateDelta();

                    OnPropertyChanged(nameof(LvDisplay));
                    OnPropertyChanged(nameof(SxDisplay));
                    OnPropertyChanged(nameof(SyDisplay));
                    OnPropertyChanged(nameof(TDisplay));
                    OnPropertyChanged(nameof(MeasureTime));

                    AddLog($"测量: {data}");
                };
            }

            if (mockSerial != null)
            {
                mockSerial.ConnectionChanged += (s, connected) =>
                {
                    SerialConnected = connected;
                };
            }

            if (mockAlgorithm != null)
            {
                mockAlgorithm.ProgressUpdate += (s, progress) =>
                {
                    DebugStep = progress.Step;
                    DebugStatus = progress.Status;
                    _debugDeltaX = progress.DeltaX;
                    _debugDeltaY = progress.DeltaY;
                    _debugIteration = progress.Iteration;
                    _debugMaxIteration = progress.TotalIterations;

                    OnPropertyChanged(nameof(DebugDeltaX));
                    OnPropertyChanged(nameof(DebugDeltaY));
                    OnPropertyChanged(nameof(DebugIteration));
                    OnPropertyChanged(nameof(DebugMaxIteration));
                    OnPropertyChanged(nameof(DebugProgressValue));
                };

                mockAlgorithm.Complete += (s, result) =>
                {
                    _debugCts?.Dispose();
                    _debugCts = null;

                    _lastDebugResult = result;
                    _debugComplete = true;

                    OnPropertyChanged(nameof(ResultStatus));
                    OnPropertyChanged(nameof(ResultColor));
                    OnPropertyChanged(nameof(ResultBorderColor));
                    OnPropertyChanged(nameof(ResultMessage));
                    OnPropertyChanged(nameof(ExportButtonVisibility));

                    AddLog($"调试完成: {result}");
                };
            }
        }

        #endregion

        #region Helper Methods

        private void UpdateDelta()
        {
            var data = _currentData as MockCA210Data;
            if (data != null)
            {
                _deltaX = data.DeltaX(_targetX);
                _deltaY = data.DeltaY(_targetY);
                OnPropertyChanged(nameof(DeltaDisplay));
                OnPropertyChanged(nameof(DeltaColor));
            }
        }

        private void InitializeChart()
        {
            _chartModel = new OxyPlot.PlotModel { Title = "xy色度实时曲线" };

            // 目标点
            var targetSeries = new OxyPlot.Series.ScatterSeries
            {
                Title = "目标点",
                MarkerType = OxyPlot.MarkerType.Circle,
                MarkerSize = 10,
                MarkerFill = OxyPlot.OxyColors.Green
            };
            targetSeries.Points.Add(new OxyPlot.Series.ScatterPoint(_targetX, _targetY));

            // 测量历史
            var measureSeries = new OxyPlot.Series.LineSeries
            {
                Title = "测量轨迹",
                Color = OxyPlot.OxyColors.Blue,
                StrokeThickness = 1.5
            };

            // 容差圆
            var toleranceSeries = new OxyPlot.Series.LineSeries
            {
                Title = $"容差范围 ±{_tolerance:F4}",
                Color = OxyPlot.OxyColors.Red,
                StrokeThickness = 1,
                LineStyle = OxyPlot.LineStyle.Dash
            };

            _chartModel.Series.Add(targetSeries);
            _chartModel.Series.Add(measureSeries);
            _chartModel.Series.Add(toleranceSeries);

            _chartModel.Axes.Add(new OxyPlot.Axes.LinearAxis
            {
                Position = OxyPlot.Axes.AxisPosition.Bottom,
                Title = "x",
                Minimum = 0.25,
                Maximum = 0.40
            });

            _chartModel.Axes.Add(new OxyPlot.Axes.LinearAxis
            {
                Position = OxyPlot.Axes.AxisPosition.Left,
                Title = "y",
                Minimum = 0.25,
                Maximum = 0.40
            });

            _chartModel.LegendPosition = OxyPlot.LegendPosition.TopRight;

            OnPropertyChanged(nameof(ChartModel));
        }

        private void UpdateChart()
        {
            if (_chartModel == null) return;

            var measureSeries = _chartModel.Series[1] as OxyPlot.Series.LineSeries;
            measureSeries.Points.Clear();

            foreach (MockCA210Data data in _measureHistory)
            {
                measureSeries.Points.Add(new OxyPlot.DataPoint(data.Sx, data.Sy));
            }

            _chartModel.Invalidate(true);
        }

        private void AddLog(string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            LogText += $"[{timestamp}] {message}\n";
            _logger.LogInformation(message);
        }

        private void LoadSettings()
        {
            // TODO: 从配置文件加载设置
        }

        public void Cleanup()
        {
            StopContinuous();
            StopDebug();
            (_ca210Service as MockCA210Service)?.Disconnect();
            (_serialPortService as MockSerialPortService)?.Close();
        }

        #endregion
    }

    /// <summary>
    /// ViewModel基类
    /// </summary>
    public abstract class ViewModelBase : System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    /// <summary>
    /// RelayCommand实现
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) => _canExecute?.Invoke(parameter) ?? true;

        public void Execute(object parameter) => _execute?.Invoke(parameter);

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
