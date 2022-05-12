using Microsoft.Extensions.ObjectPool;

namespace ObjectPools.Benchmark;

public class ExampleObjectPooledPolicy : DefaultPooledObjectPolicy<ExampleObject>
{
    public override ExampleObject Create() => new ExampleObject();
}