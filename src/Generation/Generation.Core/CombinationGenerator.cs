namespace Generation.Core;

public class CombinationGenerator
{
    private readonly int _combinationLength;

    private static readonly Random Random = new(42);

    public CombinationGenerator(int combinationLength)
    {
        if (combinationLength < 10)
            throw new ArgumentException("Combination length must be greater than 2");

        _combinationLength = combinationLength;
    }

    public int[] Generate()
    {
        return Enumerable.Range(1, _combinationLength)
            .OrderBy(x => Random.Next())
            .ToArray();
    }
}