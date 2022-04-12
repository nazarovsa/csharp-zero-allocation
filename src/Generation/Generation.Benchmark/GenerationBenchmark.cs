using BenchmarkDotNet.Attributes;
using Generation.Core;

namespace Generation.Benchmark;

[MemoryDiagnoser]
public class GenerationBenchmark
{
    private static readonly CombinationGenerator Generator = new CombinationGenerator();
    
    [Benchmark]
    public void GenerateCombination()
    {
        Generator.MoveNext();
    }

    [Benchmark]
    public void GenerateCombinationEfficient()
    {
        Generator.MoveNextEfficient();
    }

    [Benchmark]
    public void GenerateCombinations_10000()
    {
        for (var i = 0; i < 10000; i++)
        {
            Generator.MoveNext();
        }
    }

    [Benchmark]
    public void GenerateCombinationsEfficient_10000()
    {
        for (var i = 0; i < 10000; i++)
        {
            Generator.MoveNextEfficient();
        }
    }
}