using BenchmarkDotNet.Attributes;

namespace QueryBuilders.Benchmark.QueryBuilders;

[MemoryDiagnoser(false)]
public class QueryBuilderBenchmark
{
    private const string _fieldName = "username";

    [Benchmark]
    public string GetSelectQueryConcat()
    {
        return QueryBuilder.GetSelectQueryConcat(_fieldName);
    }

    [Benchmark]
    public string GetSelectQuerySb()
    {
        return QueryBuilder.GetSelectQuerySb(_fieldName);
    }

    [Benchmark]
    public string GetSelectQueryVsb()
    {
        return QueryBuilder.GetSelectQueryVsb(_fieldName);
    }
}