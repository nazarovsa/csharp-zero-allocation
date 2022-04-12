# Generation.Benchmark

|                              Method |           Mean |       Error |      StdDev |     Gen 0 |   Allocated |
|------------------------------------ |---------------:|------------:|------------:|----------:|------------:|
|                 GenerateCombination |       794.5 ns |    13.49 ns |    22.53 ns |    0.2594 |       552 B |
|        GenerateCombinationEfficient |       188.4 ns |     0.28 ns |     0.25 ns |         - |           - |
|          GenerateCombinations_10000 | 8,648,637.5 ns | 6,084.53 ns | 5,080.86 ns | 2625.0000 | 5,520,013 B |
| GenerateCombinationsEfficient_10000 | 1,668,015.5 ns | 1,749.16 ns | 1,550.58 ns |         - |         3 B |
