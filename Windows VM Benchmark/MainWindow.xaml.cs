using NLog;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Windows_VM_Benchmark
{
    public partial class MainWindow : Window
    {
        private CancellationTokenSource tokenSource;
        private List<Task> benchmarkTasks;
        private List<IBenchmark> benchmarks;
        private bool isRunning;

        private static Logger logger = LogManager.GetCurrentClassLogger();

        public MainWindow()
        {
            InitializeComponent();
            
            benchmarkTasks = new List<Task>();
            benchmarks = new List<IBenchmark>();
            isRunning = false;
            startButton.IsEnabled = true;
            stopButton.IsEnabled = false;
        }

        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(isRunning)
            {
                await StopBenchmark();
            }
        }

        private void startButton_Click(object sender, RoutedEventArgs e)
        {
            if(isRunning)
            {
                logger.Warn("Benchmark already running");
                return;
            }

            isRunning = true;

            tokenSource = new CancellationTokenSource();

            if (checkBoxCPU.IsChecked.HasValue && checkBoxCPU.IsChecked.Value)
            {
                benchmarks.Add(new CpuBenchmark());
            }

            if(checkBoxIO.IsChecked.HasValue && checkBoxIO.IsChecked.Value)
            {
                benchmarks.Add(new IoBenchmark());
            }

            if(checkBox2D.IsChecked.HasValue && checkBox2D.IsChecked.Value)
            {
                benchmarks.Add(new TwoDBenchmark(twoDCanvas));
            }

            logger.Info($"Starting benchmarks: {string.Join(",", benchmarks.Select(benchmark => benchmark.Name))}");

            foreach (var benchmark in benchmarks)
            {
                benchmarkTasks.AddRange(benchmark.StartBenchmark(tokenSource.Token));
            }

            startButton.IsEnabled = false;
            stopButton.IsEnabled = true;

            logger.Info("Started benchmarks");
        }

        private async void stopButton_Click(object sender, RoutedEventArgs e)
        {
            await StopBenchmark();
        }

        private async Task StopBenchmark()
        {
            if (!isRunning)
            {
                logger.Warn("Benchmark not running");
                return;
            }

            logger.Info("Stopping benchmarks");

            tokenSource.Cancel();

            await this.Dispatcher.InvokeAsync(() =>
            {
                Task.WaitAll(benchmarkTasks.ToArray());
            });

            isRunning = false;
            startButton.IsEnabled = true;
            stopButton.IsEnabled = false;

            foreach (var benchmark in benchmarks)
            {
                benchmark.Cleanup();
            }

            benchmarkTasks.Clear();
            benchmarks.Clear();

            logger.Info($"Stopped benchmarks: {string.Join(",", benchmarks.Select(benchmark => benchmark.Name))}");
        }
    }
}
