using BenchmarkDotNet.Attributes;
using ZeroAllocation.Core.Pools;

namespace ZeroAllocation.Benchmark.ObjectPools;

[MemoryDiagnoser]
public class ObjectPoolBenchmark
{
    private static long _count = 100;

    [Benchmark]
    public void ProcessExample()
    {
        // Create a high demand for ExampleObject instance.
        Parallel.For(0, _count, (i, loopState) =>
        {
            var example = new ExampleObject();
            Console.CursorLeft = 0;
            // This is the bottleneck in our application. All threads in this loop
            // must serialize their access to the static Console class.
            Console.WriteLine($"{example.GetValue(i):####.####}");
        });
    }

    [Benchmark]
    public void ProcessExampleObjectPool()
    {
        var pool = new ObjectPool<ExampleObject>(16);

        // Create a high demand for ExampleObject instance.
        Parallel.For(0, _count, (i, loopState) =>
        {
            var example = pool.Get();
            try
            {
                Console.CursorLeft = 0;
                // This is the bottleneck in our application. All threads in this loop
                // must serialize their access to the static Console class.
                Console.WriteLine($"{example.GetValue(i):####.####}");
            }
            finally
            {
                pool.Return(example);
            }
        });
    }
}