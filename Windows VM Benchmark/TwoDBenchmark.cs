using NLog;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Windows_VM_Benchmark
{
    class TwoDBenchmark : IBenchmark
    {
        const int RECTANGLE_SIZE = 100;

        private Stopwatch renderRectangleWatch;
        private Canvas canvas;
        private Rectangle rectangle;
        private readonly double canvasWidth;
        private readonly double canvasHeight;

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public string Name { get; set; } = nameof(TwoDBenchmark);

        public TwoDBenchmark(Canvas canvas)
        {
            this.canvas = canvas;
            canvasWidth = canvas.Width;
            canvasHeight = canvas.Height;
        }

        public List<Task> StartBenchmark(CancellationToken token)
        {
            List<Task> tasks = new List<Task>();

            renderRectangleWatch = new Stopwatch();
            rectangle = new Rectangle()
            {
                Width = RECTANGLE_SIZE,
                Height = RECTANGLE_SIZE,
                Fill = new SolidColorBrush(Colors.AliceBlue),
                Stroke = new SolidColorBrush(Colors.Black)
            };
            canvas.Children.Add(rectangle);

            tasks.Add(CreateRenderRectangleTask(token));

            return tasks;
        }

        public void Cleanup()
        {
            renderRectangleWatch = null;
            rectangle = null;
            canvas.Children.Clear();
            canvas = null;
        }
        
        private Task CreateRenderRectangleTask(CancellationToken token)
        {
            logger.Info($"Starting task {nameof(CreateRenderRectangleTask)}");

            return Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    if (token.IsCancellationRequested)
                    {
                        logger.Info($"Stopping task {nameof(CreateRenderRectangleTask)}");
                        return;
                    }

                    renderRectangleWatch.Start();
                    for (int x = 0; x < canvasWidth - RECTANGLE_SIZE; x++)
                    {
                        for (int y = 0; y < canvasHeight - RECTANGLE_SIZE; y++)
                        {
                            if(token.IsCancellationRequested)
                            {
                                break;
                            }

                            canvas.Dispatcher.Invoke(() =>
                            {
                                Canvas.SetLeft(rectangle, x);
                                Canvas.SetTop(rectangle, y);
                                canvas.InvalidateVisual();
                            }, DispatcherPriority.Normal);
                        }
                    }
                    renderRectangleWatch.Stop();
                    logger.Info($"{nameof(CreateRenderRectangleTask)} {renderRectangleWatch.ElapsedMilliseconds}");
                    renderRectangleWatch.Reset();
                }
            });
        }
    }
}
