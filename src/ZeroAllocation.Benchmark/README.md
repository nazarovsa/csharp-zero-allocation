# ZeroAllocation.Benchmark

## ObjectPoolBenchmark

|                            Method |      Mean |    Error |   StdDev |     Gen 0 |     Gen 1 |     Gen 2 | Allocated |
|---------------------------------- |----------:|---------:|---------:|----------:|----------:|----------:|----------:|
|                    ProcessExample | 117.42 ms | 2.329 ms | 4.964 ms | 5250.0000 | 5250.0000 | 5250.0000 |    382 MB |
| ProcessExampleMicrosoftObjectPool |  14.32 ms | 0.377 ms | 1.105 ms |  765.6250 |  750.0000 |  671.8750 |     42 MB |

# QueryBuilderBenchmark

|               Method |      Mean |    Error |   StdDev | Allocated |
|--------------------- |----------:|---------:|---------:|----------:|
| GetSelectQueryConcat |  53.06 ns | 0.088 ns | 0.082 ns |     352 B |
|     GetSelectQuerySb | 159.37 ns | 0.276 ns | 0.245 ns |     680 B |
|    GetSelectQueryVsb | 147.65 ns | 2.922 ns | 3.901 ns |     304 B |
