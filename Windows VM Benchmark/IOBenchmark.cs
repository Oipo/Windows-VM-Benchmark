using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Windows_VM_Benchmark
{
    public class IoBenchmark : IBenchmark
    {
        const string FILE_READ_WRITE_TASK_PATH = "FileReadWriteTask.txt";
        const int FILE_READ_WRITE_BLOCK_SIZE = 8192;
        const int MAX_ITERATIONS = 10000;

        private static Logger logger = LogManager.GetCurrentClassLogger();

        public string Name { get; set; } = nameof(IoBenchmark);

        private Stopwatch fileReadWriteWatch;
        private byte[] fileReadWriteBlockInput;
        private byte[] fileReadWriteBlockOutput;

        public List<Task> StartBenchmark(CancellationToken token)
        {
            List<Task> tasks = new List<Task>();
            File.Create(FILE_READ_WRITE_TASK_PATH).Dispose();

            fileReadWriteWatch = new Stopwatch();
            fileReadWriteBlockInput = new byte[FILE_READ_WRITE_BLOCK_SIZE];
            fileReadWriteBlockOutput = new byte[FILE_READ_WRITE_BLOCK_SIZE];

            tasks.Add(CreateFileReadWriteTask(token));

            return tasks;
        }

        public void Cleanup()
        {
            if (File.Exists(FILE_READ_WRITE_TASK_PATH))
            {
                File.Delete(FILE_READ_WRITE_TASK_PATH);
            }

            fileReadWriteWatch = null;
            fileReadWriteBlockInput = null;
            fileReadWriteBlockOutput = null;
        }

        private Task CreateFileReadWriteTask(CancellationToken token)
        {
            logger.Info($"Starting task {nameof(CreateFileReadWriteTask)}");
            Random random = new Random();
            random.NextBytes(fileReadWriteBlockInput);

            return Task.Factory.StartNew(() =>
            {
                
                while (true)
                {
                    if (token.IsCancellationRequested)
                    {
                        logger.Info($"Stopping task {nameof(CreateFileReadWriteTask)}");
                        return;
                    }

                    int bytesWritten = 0;
                    int bytesRead = 0;

                    fileReadWriteWatch.Start();
                    using (var writer = new BinaryWriter(File.Open(FILE_READ_WRITE_TASK_PATH, FileMode.Open)))
                    {
                        for (int i = 0; i < MAX_ITERATIONS; i++)
                        {
                            writer.Write(fileReadWriteBlockInput, 0, fileReadWriteBlockInput.Length);
                            bytesWritten += fileReadWriteBlockInput.Length;
                        }
                        writer.Flush();
                    }
                    using (var fs = new BinaryReader(File.Open(FILE_READ_WRITE_TASK_PATH, FileMode.Open)))
                    {
                        for (int i = 0; i < MAX_ITERATIONS; i++)
                        {
                            int iterationRead = 0;
                            while (iterationRead != fileReadWriteBlockOutput.Length)
                            {
                                var n = fs.Read(fileReadWriteBlockOutput, iterationRead, fileReadWriteBlockOutput.Length - iterationRead);
                                bytesRead += n;
                                iterationRead += n;
                                if (n == 0)
                                {
                                    break;
                                }
                            }

                            /*for(int j = 0; j < fileReadWriteBlockOutput.Length; j++)
                            {
                                if(fileReadWriteBlockInput[j] != fileReadWriteBlockOutput[j])
                                {
                                    throw new InvalidOperationException("Verification failure");
                                }
                            }*/
                        }
                    }

                    fileReadWriteWatch.Stop();
                    float mbytesPerSec = (bytesWritten + bytesRead) / 1024 / 1024 / ((float)fileReadWriteWatch.ElapsedMilliseconds / 1000);
                    logger.Info($"{nameof(CreateFileReadWriteTask)} {fileReadWriteWatch.ElapsedMilliseconds} ms - {mbytesPerSec:#,###} MB/s - {bytesWritten} bytes written - {bytesRead} - bytesRead");
                    fileReadWriteWatch.Reset();
                }
            });
        }
    }
}
