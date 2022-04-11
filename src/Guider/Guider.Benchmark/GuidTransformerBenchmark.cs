using BenchmarkDotNet.Attributes;
using Guider.Core;

namespace Guider.Benchmark;

[MemoryDiagnoser(false)]
public class GuidTransformerBenchmark
{
    private static readonly Guid GuidForTests = Guid.Parse("d1ee31ab-3b3e-47f1-97c2-b5bfa1e99664");
    private const string StringForTests = "qzHu0T478UeXwrW-oemWZA";
    
    [Benchmark]
    public Guid ToGuidFromString()
    {
        return GuidTransformer.ToGuidFromString(StringForTests);
    }
    
    [Benchmark]
    public Guid ToGuidFromStringEfficient()
    {
        return EfficientGuidTransformer.ToGuidFromString(StringForTests);
    }
    
    [Benchmark]
    public string ToStringFromGuid()
    {
        return GuidTransformer.ToStringFromGuid(GuidForTests);
    }

    [Benchmark]
    public string ToStringFromGuidEfficient()
    {
        return EfficientGuidTransformer.ToStringFromGuid(GuidForTests);
    }
}