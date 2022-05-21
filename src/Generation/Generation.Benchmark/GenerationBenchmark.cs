using BenchmarkDotNet.Attributes;
using Generation.Core;

namespace Generation.Benchmark;

[MemoryDiagnoser]
public class GenerationBenchmark
{
    private static readonly CombinationGenerator Generator = new(10);
    private static readonly CombinationGeneratorEfficient GeneratorEfficient = new(10);

    [Params(1, 10_000, 100_000)]
    public int Count;
    
    [Benchmark]
    public void GenerateCombinations()
    {
        for (var i = 0; i < Count; i++)
        {
            Generator.Generate();
        }
    }

    [Benchmark]
    public void GenerateCombinationsEfficient()
    {
        for (var i = 0; i < Count; i++)
        {
            GeneratorEfficient.MoveNext();
        }
    }
}