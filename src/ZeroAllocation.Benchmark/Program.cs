using BenchmarkDotNet.Running;
using ZeroAllocation.Benchmark.QueryBuilders;

BenchmarkRunner.Run<QueryBuilderBenchmark>();
// BenchmarkRunner.Run<ObjectPoolBenchmark>();