using System;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using CA210WhiteBalance.Core.Algorithm;
using CA210WhiteBalance.Core.CA210;
using CA210WhiteBalance.Core.Models;
using CA210WhiteBalance.Core.SerialPort;
using CA210WhiteBalance.Services;
using Microsoft.Extensions.Logging;

namespace CA210WhiteBalance.UI.ViewModels
{
    /// <summary>
    /// 主窗口ViewModel
    /// </summary>
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly ICA210Service _ca210Service;
        private readonly ISerialPortService _serialPortService;
        private readonly IWhiteBalanceAlgorithm _algorithm;
        private readonly IReportService _reportService;
        private readonly ILogger<MainWindowViewModel> _logger;

        private readonly DispatcherTimer _measureTimer;
        private readonly DispatcherTimer _chartUpdateTimer;

        // 目标配置
        private float _targetX = 0.3130f;
        private float _targetY = 0.3290f;
        private float _tolerance = 0.005f;

        // 测量数据
        private CA210Data _currentData;
        private readonly ObservableCollection<CA210Data> _measureHistory;

        // 调试状态
        private CancellationTokenSource _debugCts;
        private DebugResult _lastDebugResult;

        // 图表模型
        private OxyPlot.PlotModel _chartModel;

        public MainWindowViewModel(
            ICA210Service ca210Service,
            ISerialPortService serialPortService,
            IWhiteBalanceAlgorithm algorithm,
            IReportService reportService,
            ILogger<MainWindowViewModel> logger)
        {
            _ca210Service = ca210Service;
            _serialPortService = serialPortService;
            _algorithm = algorithm;
            _reportService = reportService;
            _logger = logger;

            _measureHistory = new ObservableCollection<CA210Data>();

            // 初始化命令
            InitializeCommands();

            // 初始化图表
            InitializeChart();

            // 订阅事件
            _ca210Service.ConnectionChanged += OnCA210ConnectionChanged;
            _ca210Service.MeasurementComplete += OnMeasurementComplete;
            _serialPortService.ConnectionChanged += OnSerialConnectionChanged;
            _algorithm.ProgressUpdate += OnDebugProgressUpdate;
            _algorithm.Complete += OnDebugComplete;

            // 初始化定时器
            _chartUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(200)
            };
            _chartUpdateTimer.Tick += (s, e) => UpdateChart();

            // 加载配置
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

        public string[] AvailablePorts => SerialPortService.GetAvailablePorts();

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
        public string LvDisplay => _currentData?.Lv.ToString("F2") ?? "--";
        public string SxDisplay => _currentData?.Sx.ToString("F4") ?? "--";
        public string SyDisplay => _currentData?.Sy.ToString("F4") ?? "--";
        public string TDisplay => _currentData?.T.ToString("F0") + " K" ?? "--";
        public string MeasureTime => _currentData?.Timestamp.ToString("HH:mm:ss") ?? "--";

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

        public string CurrentX => _currentData?.Sx.ToString("F4") ?? "--";
        public string CurrentY => _currentData?.Sy.ToString("F4") ?? "--";

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
        public string ResultStatus => _debugComplete ? (_lastDebugResult?.Success == true ? "调试成功!" : "调试失败") : "";
        public Brush ResultColor => _lastDebugResult?.Success == true ? new SolidColorBrush(Color.FromRgb(76, 175, 80)) : new SolidColorBrush(Color.FromRgb(244, 67, 54));
        public Brush ResultBorderColor => _debugComplete ? (_lastDebugResult?.Success == true ? new SolidColorBrush(Color.FromRgb(76, 175, 80)) : new SolidColorBrush(Color.FromRgb(244, 67, 54))) : new SolidColorBrush(Color.FromRgb(224, 224, 224));
        public string ResultMessage => _lastDebugResult?.ToString() ?? "";
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
            bool success = await _ca210Service.ConnectAsync();
            if (success)
            {
                AddLog($"CA-210已连接, 通道: {_ca210Service.ChannelInfo}");
            }
            else
            {
                AddLog("CA-210连接失败");
            }
        }

        private void DisconnectCA210()
        {
            _ca210Service.Disconnect();
            AddLog("CA-210已断开");
        }

        private async Task CalibrateZero()
        {
            AddLog("开始零校准...");
            bool success = await _ca210Service.CalibrateZeroAsync();
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
            await _ca210Service.MeasureAsync();
        }

        private void OpenSerial()
        {
            if (string.IsNullOrEmpty(SelectedPort))
            {
                AddLog("请先选择COM端口");
                return;
            }

            AddLog($"正在打开串口: {SelectedPort}");
            bool success = _serialPortService.Open(SelectedPort, 9600);
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
            _serialPortService.Close();
            AddLog("串口已关闭");
        }

        private async Task StartContinuous()
        {
            _measureTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _measureTimer.Tick += async (s, e) => await _ca210Service.MeasureAsync();
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

            var config = new TargetConfig
            {
                TargetX = _targetX,
                TargetY = _targetY,
                Tolerance = _tolerance,
                MaxIterations = _debugMaxIteration
            };

            AddLog($"开始调试: {config}");

            await _algorithm.AutoDebugAsync(config, _debugCts.Token);
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
                    bool success = _reportService.ExportDebugResultToExcel(saveFileDialog.FileName, _lastDebugResult);
                    if (success)
                    {
                        AddLog($"报告已导出: {saveFileDialog.FileName}");
                    }
                    else
                    {
                        AddLog("报告导出失败");
                    }
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

        private void OnCA210ConnectionChanged(object sender, ConnectionStatus status)
        {
            CA210Connected = (status == ConnectionStatus.Connected);
            CA210ChannelInfo = _ca210Service.ChannelInfo ?? (CA210Connected ? "已连接" : "未连接");
        }

        private void OnMeasurementComplete(object sender, CA210Data data)
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
        }

        private void OnSerialConnectionChanged(object sender, bool connected)
        {
            SerialConnected = connected;
        }

        private void OnDebugProgressUpdate(object sender, DebugProgress progress)
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
        }

        private void OnDebugComplete(object sender, DebugResult result)
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
        }

        #endregion

        #region Helper Methods

        private void UpdateDelta()
        {
            if (_currentData != null)
            {
                _deltaX = _currentData.DeltaX(_targetX);
                _deltaY = _currentData.DeltaY(_targetY);
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

            foreach (var data in _measureHistory)
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
            _ca210Service?.Disconnect();
            _serialPortService?.Close();
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
