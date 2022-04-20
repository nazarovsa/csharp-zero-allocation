# GuidTransformer.Benchmark

|                    Method |      Mean |    Error |   StdDev | Allocated |
|-------------------------- |----------:|---------:|---------:|----------:|
|          ToGuidFromString | 109.34 ns | 1.392 ns | 1.234 ns |     184 B |
| ToGuidFromStringEfficient |  50.80 ns | 0.214 ns | 0.189 ns |         - |
|          ToStringFromGuid | 121.99 ns | 0.480 ns | 0.449 ns |     256 B |
| ToStringFromGuidEfficient |  40.07 ns | 0.376 ns | 0.370 ns |      72 B |
