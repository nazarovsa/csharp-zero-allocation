# Generation.Benchmark

|                              Method |           Mean |         Error |        StdDev |         Median |     Gen 0 |   Allocated |
|------------------------------------ |---------------:|--------------:|--------------:|---------------:|----------:|------------:|
|                 GenerateCombination |       766.7 ns |       3.06 ns |       2.87 ns |       766.7 ns |    0.2251 |       472 B |
|        GenerateCombinationEfficient |       151.3 ns |       0.17 ns |       0.15 ns |       151.4 ns |         - |           - |
|          GenerateCombinations_10000 | 5,475,323.5 ns | 109,112.95 ns | 281,655.38 ns | 5,317,212.1 ns | 2250.0000 | 4,720,009 B |
| GenerateCombinationsEfficient_10000 | 1,438,827.7 ns |   5,780.14 ns |   5,123.94 ns | 1,437,592.4 ns |         - |         1 B |
