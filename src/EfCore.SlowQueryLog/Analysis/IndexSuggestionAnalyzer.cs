using System.Text.RegularExpressions;

namespace EfCore.SlowQueryLog.Analysis;

/// <summary>
/// A deliberately simple, provider-agnostic SQL heuristic. It does not build a real
/// parse tree - it scans WHERE, JOIN ... ON and ORDER BY fragments and proposes an
/// index per referenced table/column set. The output is a hint, not a guarantee.
/// </summary>
public sealed partial class IndexSuggestionAnalyzer
{
    // matches an identifier optionally qualified with an alias/table: [t].[Col] or t.Col or Col.
    // The lookbehind rejects parameter markers (@p0, :p0, $1) and the leading [A-Za-z_]
    // rejects bare numeric literals (WHERE 1 = 1, ORDER BY 1).
    private const string Ident = @"(?<![@:$\w])(?:\[?(?<tbl>[A-Za-z_]\w*)\]?\.)?\[?(?<col>[A-Za-z_]\w*)\]?";

    [GeneratedRegex(@"\bWHERE\b(?<body>.*?)(?:\bGROUP\s+BY\b|\bORDER\s+BY\b|\bHAVING\b|$)",
        RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex WhereClause();

    [GeneratedRegex(@"\bJOIN\b\s+\[?(?<jtbl>\w+)\]?(?:\s+AS)?\s+(?:\[?(?<alias>\w+)\]?)?\s*\bON\b(?<body>.*?)(?=\bJOIN\b|\bWHERE\b|\bGROUP\b|\bORDER\b|$)",
        RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex JoinClause();

    [GeneratedRegex(@"\bORDER\s+BY\b(?<body>.*?)(?:\bLIMIT\b|\bOFFSET\b|$)",
        RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex OrderByClause();

    [GeneratedRegex(@"(?<![@:$\w])(?:\[?(?<tbl>[A-Za-z_]\w*)\]?\.)?\[?(?<col>[A-Za-z_]\w*)\]?\s*(?:=|>|<|>=|<=|<>|\bLIKE\b|\bIN\b|\bIS\b)",
        RegexOptions.IgnoreCase)]
    private static partial Regex Predicate();

    /// <summary>
    /// Produces a set of index suggestions for the supplied SQL. Returns an empty list
    /// when nothing indexable is found.
    /// </summary>
    public IReadOnlyList<IndexSuggestion> Analyze(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return Array.Empty<IndexSuggestion>();

        var aliasMap = BuildAliasMap(sql);
        // table -> ordered distinct columns
        var byTable = new Dictionary<string, List<(string col, string reason)>>(StringComparer.OrdinalIgnoreCase);
        // table -> include columns (for ORDER BY columns)
        var includeColumns = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        void Add(string? tbl, string col, string reason)
        {
            var table = Resolve(tbl, aliasMap);
            if (table is null || string.IsNullOrEmpty(col))
                return;
            if (!byTable.TryGetValue(table, out var list))
                byTable[table] = list = new();
            if (!list.Any(x => string.Equals(x.col, col, StringComparison.OrdinalIgnoreCase)))
                list.Add((col, reason));
        }

        void AddInclude(string? tbl, string col)
        {
            var table = Resolve(tbl, aliasMap);
            if (table is null || string.IsNullOrEmpty(col))
                return;
            if (!includeColumns.TryGetValue(table, out var list))
                includeColumns[table] = list = new();
            if (!list.Any(x => string.Equals(x, col, StringComparison.OrdinalIgnoreCase)))
                list.Add(col);
        }

        foreach (Match w in WhereClause().Matches(sql))
            foreach (Match p in Predicate().Matches(w.Groups["body"].Value))
                Add(p.Groups["tbl"].Value, p.Groups["col"].Value, "filtered in WHERE");

        foreach (Match j in JoinClause().Matches(sql))
        {
            foreach (Match p in Predicate().Matches(j.Groups["body"].Value))
                Add(p.Groups["tbl"].Value, p.Groups["col"].Value, "join key");
        }

        foreach (Match o in OrderByClause().Matches(sql))
        {
            foreach (var term in o.Groups["body"].Value.Split(','))
            {
                var m = Regex.Match(term.Trim(), Ident, RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    Add(m.Groups["tbl"].Value, m.Groups["col"].Value, "sort column");
                    AddInclude(m.Groups["tbl"].Value, m.Groups["col"].Value);
                }
            }
        }

        var result = new List<IndexSuggestion>();
        foreach (var (table, cols) in byTable)
        {
            var columns = cols.Select(c => c.col).ToArray();
            var includeCols = includeColumns.TryGetValue(table, out var incList) ? incList : null;
            var reason = string.Join("; ", cols.Select(c => $"{c.col} ({c.reason})").Distinct());
            result.Add(new IndexSuggestion(table, columns, reason, includeCols));
        }
        return result;
    }

    private static string? Resolve(string? tblOrAlias, IReadOnlyDictionary<string, string> aliasMap)
    {
        if (string.IsNullOrEmpty(tblOrAlias))
            return aliasMap.Count == 1 ? aliasMap.Values.First() : null;
        return aliasMap.TryGetValue(tblOrAlias, out var real) ? real : tblOrAlias;
    }

    private static Dictionary<string, string> BuildAliasMap(string sql)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        // FROM [Orders] AS [o]  /  JOIN [Lines] AS [l]
        foreach (Match m in Regex.Matches(sql,
            @"\b(?:FROM|JOIN)\b\s+\[?(?<tbl>\w+)\]?(?:\s+AS)?\s+\[?(?<alias>\w+)\]?",
            RegexOptions.IgnoreCase))
        {
            var tbl = m.Groups["tbl"].Value;
            var alias = m.Groups["alias"].Value;
            if (!string.IsNullOrEmpty(alias) &&
                !alias.Equals("ON", StringComparison.OrdinalIgnoreCase) &&
                !alias.Equals("WHERE", StringComparison.OrdinalIgnoreCase))
            {
                map[alias] = tbl;
            }
            map[tbl] = tbl;
        }
        return map;
    }

[GeneratedRegex(Ident)]
private static partial Regex Identifier();
}
