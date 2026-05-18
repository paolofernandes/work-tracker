using Microsoft.Data.Sqlite;
using WorkTracker.Models;

namespace WorkTracker.Data;

static class WorkEntryRepository
{
    static SqliteConnection Open() =>
        new SqliteConnection($"Data Source={DatabaseInitializer.DbPath}");

    public static int CreatePendingEntry(DateOnly date, TimeOnly startTime)
    {
        using var conn = Open();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO work_entries (date, start_time, created_at)
            VALUES ($date, $start, $created)
            RETURNING id
            """;
        cmd.Parameters.AddWithValue("$date", date.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("$start", startTime.ToString("HH:mm"));
        cmd.Parameters.AddWithValue("$created", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public static void CloseEntry(int id, TimeOnly endTime, string? description)
    {
        using var conn = Open();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE work_entries
            SET end_time    = $end,
                total_hours = $hours,
                description = $desc
            WHERE id = $id
            """;
        cmd.Parameters.AddWithValue("$id", id);
        cmd.Parameters.AddWithValue("$end", endTime.ToString("HH:mm"));
        cmd.Parameters.AddWithValue("$desc", (object?)description ?? DBNull.Value);

        // Total hours is fetched from start_time for accuracy
        using var getCmd = conn.CreateCommand();
        getCmd.CommandText = "SELECT start_time FROM work_entries WHERE id = $id";
        getCmd.Parameters.AddWithValue("$id", id);
        var startStr = getCmd.ExecuteScalar() as string;
        double hours = 0;
        if (startStr is not null && TimeOnly.TryParse(startStr, out var start))
            hours = (endTime - start).TotalHours;

        cmd.Parameters.AddWithValue("$hours", Math.Round(hours, 2));
        cmd.ExecuteNonQuery();
    }

    public static WorkEntry? GetOpenSession()
    {
        using var conn = Open();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM work_entries WHERE end_time IS NULL LIMIT 1";
        using var reader = cmd.ExecuteReader();
        return reader.Read() ? MapRow(reader) : null;
    }

    public static void AddEntry(WorkEntry entry)
    {
        using var conn = Open();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO work_entries (date, start_time, end_time, total_hours, description, created_at)
            VALUES ($date, $start, $end, $hours, $desc, $created)
            """;
        cmd.Parameters.AddWithValue("$date", entry.Date.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("$start", entry.StartTime.ToString("HH:mm"));
        cmd.Parameters.AddWithValue("$end", entry.EndTime?.ToString("HH:mm") ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$hours", (object?)entry.TotalHours ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$desc", (object?)entry.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$created", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        cmd.ExecuteNonQuery();
    }

    public static List<WorkEntry> GetEntriesByRange(DateOnly from, DateOnly to)
    {
        using var conn = Open();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT * FROM work_entries
            WHERE date BETWEEN $from AND $to
            ORDER BY date, start_time
            """;
        cmd.Parameters.AddWithValue("$from", from.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("$to", to.ToString("yyyy-MM-dd"));
        using var reader = cmd.ExecuteReader();
        var list = new List<WorkEntry>();
        while (reader.Read()) list.Add(MapRow(reader));
        return list;
    }

    public static void UpdateEntry(WorkEntry entry)
    {
        using var conn = Open();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE work_entries
            SET date        = $date,
                start_time  = $start,
                end_time    = $end,
                total_hours = $hours,
                description = $desc
            WHERE id = $id
            """;
        cmd.Parameters.AddWithValue("$id", entry.Id);
        cmd.Parameters.AddWithValue("$date", entry.Date.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("$start", entry.StartTime.ToString("HH:mm"));
        cmd.Parameters.AddWithValue("$end", entry.EndTime?.ToString("HH:mm") ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$hours", (object?)entry.TotalHours ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$desc", (object?)entry.Description ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public static void DeleteEntry(int id)
    {
        using var conn = Open();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM work_entries WHERE id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    static WorkEntry MapRow(SqliteDataReader r) => new()
    {
        Id          = r.GetInt32(r.GetOrdinal("id")),
        Date        = DateOnly.Parse(r.GetString(r.GetOrdinal("date"))),
        StartTime   = TimeOnly.Parse(r.GetString(r.GetOrdinal("start_time"))),
        EndTime     = r.IsDBNull(r.GetOrdinal("end_time")) ? null
                      : TimeOnly.Parse(r.GetString(r.GetOrdinal("end_time"))),
        TotalHours  = r.IsDBNull(r.GetOrdinal("total_hours")) ? null
                      : r.GetDouble(r.GetOrdinal("total_hours")),
        Description = r.IsDBNull(r.GetOrdinal("description")) ? null
                      : r.GetString(r.GetOrdinal("description")),
        CreatedAt   = DateTime.Parse(r.GetString(r.GetOrdinal("created_at"))),
    };
}
