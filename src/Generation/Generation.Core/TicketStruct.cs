namespace Generation.Core;

public readonly struct TicketStruct
{
    public TicketStruct(int[] combination)
    {
        Combination = combination;
    }

    public int[] Combination { get; }

    public override string ToString()
    {
        return string.Join(" ", Combination);
    }
}