namespace Generation.Core;

public class CombinationGeneratorEfficient
{
    private static readonly Random Random = new(42);

    private readonly int[] _numbers;
    private readonly int[] _result;

    private readonly int _combinationLength;
    
    public CombinationGeneratorEfficient(int combinationLength)
    {
        if(combinationLength < 10)
            throw new ArgumentException("Combination length must be greater than 10.");

        _combinationLength = combinationLength;
        _result = new int[combinationLength];
        _numbers = Enumerable.Range(1, combinationLength).ToArray();
    }
    
    public bool IsResultReady { get; private set; }

    public int[] Result => IsResultReady
        ? _result
        : throw new InvalidOperationException("Result is not ready");

    public void MoveNext()
    {
        var tail = _combinationLength - 1;
        for (var i = 0; i < _combinationLength; i++)
        {
            var peekIndex = Random.Next(0, tail);
            _result[i] = _numbers[peekIndex];
            // Swap with deconstruct
            (_numbers[peekIndex], _numbers[tail]) = (_numbers[tail], _numbers[peekIndex]);
            tail--;
        }

        IsResultReady = true;
    }
}