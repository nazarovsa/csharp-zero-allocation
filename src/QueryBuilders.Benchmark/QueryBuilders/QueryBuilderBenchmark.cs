using BenchmarkDotNet.Attributes;

namespace QueryBuilders.Benchmark.QueryBuilders;

[MemoryDiagnoser(false)]
public class QueryBuilderBenchmark
{
    private const string _fieldName = "username";

    [Params(16, 64, 101, 256)] public int StringBuilderBufferSize;

    [Benchmark]
    public string GetSelectQueryConcat()
    {
        return QueryBuilder.GetSelectQueryConcat(_fieldName);
    }

    [Benchmark]
    public string GetSelectQuerySb()
    {
        return QueryBuilder.GetSelectQuerySb(_fieldName, StringBuilderBufferSize);
    }

    [Benchmark]
    public string GetSelectQueryVsb()
    {
        return QueryBuilder.GetSelectQueryVsb(_fieldName);
    }
}