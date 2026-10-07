namespace AnuradhapuraAI.Infrastructure.Persistence.Configuration;

internal static class CheckConstraintSql
{
    public static string In(string columnName, IEnumerable<string> allowedValues)
    {
        var values = string.Join(", ", allowedValues.Select(value => $"N'{value.Replace("'", "''")}'"));

        return $"[{columnName}] IN ({values})";
    }
}
