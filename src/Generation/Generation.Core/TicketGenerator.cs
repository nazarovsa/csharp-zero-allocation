namespace Generation.Core;

public static class TicketGenerator
{
    private const int CombinationLength = 10;

    private static readonly Random Random = new(42);

    private static int[] _numbers = Enumerable.Range(1, CombinationLength).ToArray();

    public static TicketClass Generate()
    {
        var result = Enumerable.Range(1, CombinationLength)
            .OrderBy(x => Random.Next())
            .ToArray();

        return new TicketClass(result);
    }

    public static TicketStruct GenerateEfficient()
    {
        Span<int> result = stackalloc int[CombinationLength];

        var tail = CombinationLength - 1;
        for (var i = 0; i < 10; i++)
        {
            var peekIndex = Random.Next(0, tail);
            result[i] = _numbers[peekIndex];
            // Swap with deconstruct
            (_numbers[i], _numbers[tail]) = (_numbers[tail], _numbers[i]);
            tail--;
        }

        return new TicketStruct(result.ToArray());
    }
    
    /*public static TicketStruct GenerateEfficient()
    {
        Span<int> result = stackalloc int[CombinationLength];
        Span<int> numbers = stackalloc int[CombinationLength];

        for (var i = 0; i < 10; i++)
        {
            numbers[i] = i + 1;
        }

        var tail = CombinationLength - 1;
        for (var i = 0; i < 10; i++)
        {
            var peekIndex = Random.Next(0, tail);
            result[i] = numbers[peekIndex];
            // Swap with deconstruct
            (numbers[i], numbers[tail]) = (numbers[tail], numbers[i]);
            tail--;
        }

        return new TicketStruct(result.ToArray());
    }*/
}