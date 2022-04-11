using System.Runtime.CompilerServices;

namespace Generation.Core;

public class TicketClass
{
    public TicketClass(int[] combination)
    {
        Combination = combination;
    }

    public int[] Combination { get; }

    public override string ToString()
    {
        return string.Join(" ", Combination);
    }
}