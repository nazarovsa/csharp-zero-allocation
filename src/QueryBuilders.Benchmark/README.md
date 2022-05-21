# QueryBuilders.Benchmark

|               Method | StringBuilderBufferSize |      Mean |    Error |   StdDev |    Median | Allocated |
|--------------------- |-------------------------|----------:|---------:|---------:|----------:|----------:|
| GetSelectQueryConcat | -                       |  79.42 ns | 0.851 ns | 0.796 ns |  79.40 ns |     592 B |
|     GetSelectQuerySb | 16                      | 196.22 ns | 1.090 ns | 1.020 ns | 196.78 ns |     768 B |
|     GetSelectQuerySb | 64                      | 136.62 ns | 0.585 ns | 0.547 ns | 136.46 ns |     624 B |
|     GetSelectQuerySb | 101                     | 105.06 ns | 0.286 ns | 0.268 ns | 105.09 ns |     504 B |
|     GetSelectQuerySb | 256                     | 111.55 ns | 0.317 ns | 0.296 ns | 111.48 ns |     808 B |
|    GetSelectQueryVsb | -                       | 155.25 ns | 0.164 ns | 0.137 ns | 155.25 ns |     224 B |

When using `StringBuider` it's necessary to choose right buffer size,
because if string length is larger than buffer `StringBuilder` will create a new instance of string builder.
Check source code for details.

A result string has length 101 and allocates 224 bytes of memory.
