using System.Runtime.InteropServices;

namespace Generation.Core;

public class CombinationGenerator
{
    private const int CombinationLength = 10;

    private static readonly Random Random = new(42);

    private static readonly int[] Numbers = Enumerable.Range(1, CombinationLength).ToArray();
    private readonly int[] _result = new int[CombinationLength];

    public bool IsResultReady { get; private set; }

    public int[] Result => IsResultReady
        ? _result
        : throw new InvalidOperationException("Result is not ready");

    public void MoveNext()
    {
        var enumerable = Enumerable.Range(1, CombinationLength)
            .OrderBy(x => Random.Next());

        foreach (var (index, number) in enumerable.Select((x, i) => (i, x)))
        {
            _result[index] = number;
        }

        IsResultReady = true;
    }

    public void MoveNextEfficient()
    {
        var tail = CombinationLength - 1;
        for (var i = 0; i < CombinationLength; i++)
        {
            var peekIndex = Random.Next(0, tail);
            _result[i] = Numbers[peekIndex];
            // Swap with deconstruct
            (Numbers[i], Numbers[tail]) = (Numbers[tail], Numbers[i]);
            tail--;
        }

        IsResultReady = true;
    }
}