using System.Text;
using ZeroAllocation.Core;

namespace QueryBuilders.Benchmark.QueryBuilders;

public static class QueryBuilder
{
    public static string GetSelectQueryConcat(string fieldName)
    {
        var result = $"SELECT ${fieldName} FROM DUMMY ";

        // Simulate params
        if (true)
        {
            result += "WHERE ID = @ID ";
            result += "AND CreationDate > @CreationDate";
        }
        result += "ORDER BY CreationDate DESC";

        return result;
    }
    
    public static string GetSelectQuerySb(string fieldName)
    {
        var sb = new StringBuilder($"SELECT ${fieldName} FROM DUMMY ");

        // Simulate params
        if (true)
        {
            sb.Append("WHERE ID = @ID ");
            sb.Append("AND CreationDate > @CreationDate");
        }

        sb.Append("ORDER BY CreationDate DESC");

        return sb.ToString();
    }

    public static string GetSelectQueryVsb(string fieldName)
    {
        var vsb = new ValueStringBuilder();

        vsb.Append($"SELECT ${fieldName} FROM DUMMY ");
        
        // Simulate params
        if (true)
        {
            vsb.Append("WHERE ID = @ID ");
            vsb.Append("AND CreationDate > @CreationDate");
        }
        
        vsb.Append("ORDER BY CreationDate DESC");

        return vsb.ToString();
    }
}