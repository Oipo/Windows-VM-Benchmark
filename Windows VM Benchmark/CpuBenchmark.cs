using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Numerics;

namespace Windows_VM_Benchmark
{
    public class CpuBenchmark : IBenchmark
    {
        const int INTEGER_MATH_SIZE = 4096;
        const int SIMD_MATH_SIZE = 4096;
        const int MAX_ITERATIONS = 20000;

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private int[] integerMathIntsInput;
        private int[] integerMathIntsOutput;

        private Vector4[] simdMathInput;
        private Vector4[] simdMathOutput;

        private Stopwatch integerMathWatch;
        private Stopwatch simdMathWatch;

        public string Name { get; set; } = nameof(CpuBenchmark);
        private readonly int id;

        public CpuBenchmark(int id)
        {
            this.id = id;
        }

        public List<Task> StartBenchmark(CancellationToken token)
        {
            List<Task> tasks = new List<Task>();

            if(SIMD_MATH_SIZE % 2 != 0)
            {
                throw new InvalidOperationException($"SIMD_MATH_SIZE {SIMD_MATH_SIZE} has to be divisible by 2");
            }

            integerMathIntsInput = new int[INTEGER_MATH_SIZE];
            integerMathIntsOutput = new int[INTEGER_MATH_SIZE];

            simdMathInput = new Vector4[SIMD_MATH_SIZE];
            simdMathOutput = new Vector4[SIMD_MATH_SIZE/2];

            integerMathWatch = new Stopwatch();
            simdMathWatch = new Stopwatch();

            tasks.Add(CreateIntegerMathTask(token));
            tasks.Add(CreateSimdMathTask(token));

            return tasks;
        }

        public void Cleanup()
        {
            integerMathIntsInput = null;
            integerMathIntsOutput = null;

            simdMathInput = null;
            simdMathOutput = null;

            integerMathWatch = null;
            simdMathWatch = null;
        }

        private Task CreateIntegerMathTask(CancellationToken token)
        {
            logger.Info($"Starting task {nameof(CreateIntegerMathTask)} - {id}");
            Random random = new Random();
            for (int i = 0; i < INTEGER_MATH_SIZE; i++)
            {
                integerMathIntsInput[i] = random.Next();
            }

            return Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    if (token.IsCancellationRequested)
                    {
                        logger.Info($"Stopping task {nameof(CreateIntegerMathTask)} - {id}");
                        return;
                    }

                    integerMathWatch.Start();
                    for (int j = 0; j < MAX_ITERATIONS; j++)
                    {
                        for (int i = 0; i < INTEGER_MATH_SIZE; i++)
                        {
                            integerMathIntsOutput[i] = integerMathIntsInput[i] * 4 + j;
                        }
                    }
                    integerMathWatch.Stop();
                    logger.Info($"{nameof(CreateIntegerMathTask)} - {id} - {integerMathWatch.ElapsedMilliseconds}");
                    integerMathWatch.Reset();
                }
            });
        }

        private Task CreateSimdMathTask(CancellationToken token)
        {
            logger.Info($"Starting task {nameof(CreateSimdMathTask)} - {id}");
            Random random = new Random();
            for (int i = 0; i < SIMD_MATH_SIZE; i++)
            {
                simdMathInput[i] = new Vector4(GenerateNewFloat(random), GenerateNewFloat(random), GenerateNewFloat(random), GenerateNewFloat(random));
            }

            return Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    if (token.IsCancellationRequested)
                    {
                        logger.Info($"Stopping task {nameof(CreateSimdMathTask)} - {id}");
                        return;
                    }

                    simdMathWatch.Start();
                    for (int j = 0; j < MAX_ITERATIONS; j++)
                    {
                        for (int i = 0; i < SIMD_MATH_SIZE / 2; i += 4)
                        {
                            simdMathOutput[i] = Vector4.Add(simdMathInput[i], simdMathInput[i + 1]);
                        }
                    }
                    simdMathWatch.Stop();
                    logger.Info($"{nameof(CreateSimdMathTask)} - {id} - {simdMathWatch.ElapsedMilliseconds}");
                    simdMathWatch.Reset();
                }
            });
        }

        private float GenerateNewFloat(Random random)
        {
            float result = (float)random.NextDouble();

            if (float.IsPositiveInfinity(result))
            {
                result = float.MaxValue;
            }
            else if (float.IsNegativeInfinity(result))
            {
                result = float.MinValue;
            }

            return result;
        }
    }
}
