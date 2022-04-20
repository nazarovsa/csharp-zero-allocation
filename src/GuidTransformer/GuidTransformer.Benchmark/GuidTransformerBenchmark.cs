using BenchmarkDotNet.Attributes;

namespace GuidTransformer.Benchmark;

[MemoryDiagnoser(false)]
public class GuidTransformerBenchmark
{
    private static readonly Guid GuidForTests = Guid.Parse("d1ee31ab-3b3e-47f1-97c2-b5bfa1e99664");
    private const string StringForTests = "qzHu0T478UeXwrW-oemWZA";
    
    [Benchmark]
    public Guid ToGuidFromString()
    {
        return Core.GuidTransformer.ToGuidFromString(StringForTests);
    }
    
    [Benchmark]
    public Guid ToGuidFromStringEfficient()
    {
        return Core.GuidTransformer.ToGuidFromStringEfficient(StringForTests);
    }
    
    [Benchmark]
    public string ToStringFromGuid()
    {
        return Core.GuidTransformer.ToStringFromGuid(GuidForTests);
    }

    [Benchmark]
    public string ToStringFromGuidEfficient()
    {
        return Core.GuidTransformer.ToStringFromGuidEfficient(GuidForTests);
    }
}