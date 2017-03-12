using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Windows_VM_Benchmark
{
    interface IBenchmark
    {
        List<Task> StartBenchmark(CancellationToken token);

        void Cleanup();

        string Name { get; set; }
    }
}
