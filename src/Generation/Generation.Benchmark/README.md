# Generation.Benchmark

|                        Method |  Count |            Mean |         Error |        StdDev |      Gen 0 |    Allocated |
|------------------------------ |------- |----------------:|--------------:|--------------:|-----------:|-------------:|
|          GenerateCombinations |      1 |        558.5 ns |       1.96 ns |       1.83 ns |     0.2251 |        472 B |
| GenerateCombinationsEfficient |      1 |        141.1 ns |       0.25 ns |       0.24 ns |          - |            - |
|          GenerateCombinations |  10000 |  5,357,173.9 ns |  45,280.68 ns |  40,140.15 ns |  2250.0000 |  4,720,009 B |
| GenerateCombinationsEfficient |  10000 |  1,412,395.9 ns |   2,739.52 ns |   2,287.63 ns |          - |          2 B |
|          GenerateCombinations | 100000 | 62,100,165.6 ns | 204,302.19 ns | 191,104.39 ns | 22500.0000 | 47,201,427 B |
| GenerateCombinationsEfficient | 100000 | 16,507,169.5 ns |  27,985.89 ns |  26,178.02 ns |          - |         36 B |
