using ZeroAllocation.Core.Pools;

namespace ZeroAllocation.Benchmark.ObjectPools;

public sealed class WordProcessor : IPoolable
{
    private byte[]? _entropy;

    private Random? _random = new Random();

    public WordProcessor()
    {
        _entropy = new byte[16384];
        _random.NextBytes(_entropy);
    }

    public string ProcessString(string input)
    {
        Span<char> span = stackalloc char[input.Length];
        for (var i = 0; i < span.Length; i++)
        {
            var entropyIndex = _random.Next(0, _entropy.Length);
            var entropyByte = _entropy[entropyIndex];
            if (entropyByte % 2 == 0)
            {
                if (i % 2 == 0)
                {
                    span[i] = char.ToUpperInvariant(input[i]);
                    continue;
                }

                span[i] = char.ToLowerInvariant(input[i]);
            }
            else
            {
                span[i] = input[i];
            }
        }

        return span.ToString();
    }
}