using BenchmarkDotNet.Attributes;
using Generation.Core;

namespace Generation.Benchmark;

[MemoryDiagnoser]
public class GenerationBenchmark
{
    [Benchmark]
    public TicketClass GenerateTicket()
    {
        return TicketGenerator.Generate();
    }

    [Benchmark]
    public TicketStruct GenerateTicketEfficient()
    {
        return TicketGenerator.GenerateEfficient();
    }

    [Benchmark]
    public List<TicketClass> GenerateTickets_10000()
    {
        var list = new List<TicketClass>();
        for (var i = 0; i < 10000; i++)
        {
            list.Add(TicketGenerator.Generate());
        }

        return list;
    }

    [Benchmark]
    public List<TicketStruct> GenerateTicketEfficient_10000()
    {
        var list = new List<TicketStruct>();
        for (var i = 0; i < 10000; i++)
        {
            list.Add(TicketGenerator.GenerateEfficient());
        }

        return list;
    }
}