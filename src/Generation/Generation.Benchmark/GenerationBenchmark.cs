using BenchmarkDotNet.Attributes;
using Generation.Core;

namespace Generation.Benchmark;

[MemoryDiagnoser]
public class GenerationBenchmark
{
    private static readonly CombinationGenerator Generator = new(10);
    private static readonly CombinationGeneratorEfficient GeneratorEfficient = new(10);
    
    [Benchmark]
    public void GenerateCombination()
    {
        Generator.Generate();
    }

    [Benchmark]
    public void GenerateCombinationEfficient()
    {
        GeneratorEfficient.MoveNext();
    }

    [Benchmark]
    public void GenerateCombinations_10000()
    {
        for (var i = 0; i < 10000; i++)
        {
            Generator.Generate();
        }
    }

    [Benchmark]
    public void GenerateCombinationsEfficient_10000()
    {
        for (var i = 0; i < 10000; i++)
        {
            GeneratorEfficient.MoveNext();
        }
    }
}