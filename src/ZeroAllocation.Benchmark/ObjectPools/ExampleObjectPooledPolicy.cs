using Microsoft.Extensions.ObjectPool;

namespace ZeroAllocation.Benchmark.ObjectPools;

public class ExampleObjectPooledPolicy : DefaultPooledObjectPolicy<ExampleObject>
{
    public override ExampleObject Create() => new ExampleObject();
}