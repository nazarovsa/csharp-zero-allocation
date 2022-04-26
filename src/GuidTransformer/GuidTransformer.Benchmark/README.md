# GuidTransformer.Benchmark

|                    Method |      Mean |    Error |   StdDev | Allocated |
|-------------------------- |----------:|---------:|---------:|----------:|
|          ToGuidFromString | 101.63 ns | 0.244 ns | 0.190 ns |     184 B |
| ToGuidFromStringEfficient |  48.69 ns | 1.087 ns | 0.908 ns |         - |
|          ToStringFromGuid | 117.85 ns | 1.724 ns | 1.528 ns |     256 B |
| ToStringFromGuidEfficient |  40.45 ns | 0.195 ns | 0.152 ns |      72 B |
