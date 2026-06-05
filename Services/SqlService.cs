using System.Data;
using Microsoft.Data.SqlClient;
using ReportEditor.Models;

namespace ReportEditor.Services;

/// <summary>
/// Executes a dataset's CommandText against a user-provided connection string
/// and returns a DataTable. Also keeps a per-dataset cache for preview.
/// </summary>
public sealed class SqlService
{
    public string? ConnectionString { get; set; }

    private readonly Dictionary<string, DataTable> _cache = new();

    public DataTable? Preview(string datasetName)
        => _cache.TryGetValue(datasetName, out var dt) ? dt : null;

    public void SetPreview(string datasetName, DataTable dt) => _cache[datasetName] = dt;

    public void Clear(string datasetName) => _cache.Remove(datasetName);

    public async Task<DataTable> ExecuteAsync(
        RdlDataSet ds,
        IEnumerable<RdlReportParameter> parameters,
        IReadOnlyDictionary<string, string?> paramValues,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
            throw new InvalidOperationException(
                "No connection string. Set one in the Data panel before running a query.");

        await using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync(ct);

        await using var cmd = new SqlCommand(ds.CommandText, conn);
        // Auto-detect stored proc vs ad-hoc.
        var ct2 = ds.CommandType;
        if (string.Equals(ct2, "StoredProcedure", StringComparison.OrdinalIgnoreCase))
            cmd.CommandType = CommandType.StoredProcedure;
        else if (ds.CommandText.TrimStart().StartsWith("EXEC", StringComparison.OrdinalIgnoreCase))
        {
            // leave as Text — already an EXEC statement.
        }

        cmd.CommandTimeout = 60;

        foreach (var qp in ds.QueryParameters)
        {
            // Value like "=Parameters!Site.Value" → look up Site in paramValues
            var raw = qp.Value ?? "";
            string? val = null;
            if (raw.StartsWith("=Parameters!"))
            {
                var rest = raw.Substring("=Parameters!".Length);
                var dot = rest.IndexOf('.');
                var paramName = dot >= 0 ? rest.Substring(0, dot) : rest;
                paramValues.TryGetValue(paramName, out val);
            }
            else if (raw.StartsWith("="))
            {
                val = raw.Substring(1); // best-effort literal
            }
            else
            {
                val = raw;
            }
            cmd.Parameters.AddWithValue(qp.Name, (object?)val ?? DBNull.Value);
        }

        var dt = new DataTable();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        dt.Load(reader);
        _cache[ds.Name] = dt;
        return dt;
    }

    public DataTable LoadFromCsv(string datasetName, string csv)
    {
        var dt = new DataTable();
        using var reader = new StringReader(csv);
        var header = reader.ReadLine();
        if (header == null) return dt;
        foreach (var col in SplitCsv(header)) dt.Columns.Add(col);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            var parts = SplitCsv(line);
            var row = dt.NewRow();
            for (int i = 0; i < Math.Min(parts.Count, dt.Columns.Count); i++)
                row[i] = parts[i];
            dt.Rows.Add(row);
        }
        _cache[datasetName] = dt;
        return dt;
    }

    private static List<string> SplitCsv(string line)
    {
        var result = new List<string>();
        var cur = new System.Text.StringBuilder();
        bool inQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inQuotes)
            {
                if (c == '"' && i + 1 < line.Length && line[i + 1] == '"') { cur.Append('"'); i++; }
                else if (c == '"') inQuotes = false;
                else cur.Append(c);
            }
            else
            {
                if (c == ',') { result.Add(cur.ToString()); cur.Clear(); }
                else if (c == '"' && cur.Length == 0) inQuotes = true;
                else cur.Append(c);
            }
        }
        result.Add(cur.ToString());
        return result;
    }
}
