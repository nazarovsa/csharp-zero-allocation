# QueryBuilders.Benchmark

|               Method |      Mean |    Error |   StdDev | Allocated |
|--------------------- |----------:|---------:|---------:|----------:|
| GetSelectQueryConcat |  76.33 ns | 0.577 ns | 0.512 ns |     592 B |
|     GetSelectQuerySb | 104.56 ns | 2.198 ns | 3.356 ns |     552 B |
|    GetSelectQueryVsb | 158.39 ns | 1.659 ns | 1.471 ns |     224 B |

A result string allocates 224 bytes of memory.
