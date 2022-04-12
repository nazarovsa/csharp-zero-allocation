using BenchmarkDotNet.Attributes;
using ZeroAllocation.Core.Pools;

namespace ZeroAllocation.Benchmark.ObjectPools;

[MemoryDiagnoser]
public class ObjectPoolBenchmark
{
    private static List<string> _albums = new List<string>
    {
        "Are you experienced?",
        "And Justice For All",
        "Mass V",
        "The Wall",
        "Fortitude",
        "L'Enfant Sauvage",
        "Metallica",
        "The Number of the Beast",
    };

    [Benchmark]
    public void ProcessTicketsObjectPool()
    {
        var objectPool = new ObjectPool<WordProcessor>(16);

        var modifiedStringCount = 0;
        var operationsAmount = 5;
        Parallel.For(0L, operationsAmount, (i, _) =>
        {
            var index = (int)i % _albums.Count;
            var str = _albums[index];
            var processor = objectPool.Get();

            var result = processor.ProcessString(str);

            if (result.Equals(str, StringComparison.Ordinal))
            {
                Interlocked.Increment(ref modifiedStringCount);
            }

            objectPool.Return(processor);
        });
    }

    [Benchmark]
    public void ProcessTicketsCreateInstances()
    {
        var modifiedStringCount = 0;
        var operationsAmount = 5;
        Parallel.For(0L, operationsAmount, (i, _) =>
        {
            var processor = new WordProcessor();
            var index = (int)i % _albums.Count;
            var str = _albums[index];

            var result = processor.ProcessString(str);

            if (result.Equals(str, StringComparison.Ordinal))
            {
                Interlocked.Increment(ref modifiedStringCount);
            }
        });
    }
}