using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.ObjectPool;

namespace ObjectPools.Benchmark;

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
    public void ProcessExampleMicrosoftObjectPool()
    {
        var pool = new DefaultObjectPool<ExampleObject>(new ExampleObjectPooledPolicy(),4);

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