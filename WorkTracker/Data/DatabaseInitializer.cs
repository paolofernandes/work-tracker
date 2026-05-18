using Microsoft.Data.Sqlite;
using WorkTracker.Models;

namespace WorkTracker.Data;

static class DatabaseInitializer
{
    public static string DbPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "WorkTracker", "worktracker.db");

    public static void Initialize()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DbPath)!);

        using var conn = new SqliteConnection($"Data Source={DbPath}");
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS work_entries (
                id          INTEGER PRIMARY KEY AUTOINCREMENT,
                date        TEXT NOT NULL,
                start_time  TEXT NOT NULL,
                end_time    TEXT,
                total_hours REAL,
                description TEXT,
                created_at  TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS app_settings (
                key   TEXT PRIMARY KEY,
                value TEXT NOT NULL
            );
            """;
        cmd.ExecuteNonQuery();

        SeedDefaultSettings(conn);
    }

    static void SeedDefaultSettings(SqliteConnection conn)
    {
        var defaults = AppSettings.Defaults;
        var rows = new Dictionary<string, string>
        {
            ["prompt_window1_start"] = defaults.Prompt1Start.ToString("HH:mm"),
            ["prompt_window1_end"]   = defaults.Prompt1End.ToString("HH:mm"),
            ["prompt_window2_start"] = defaults.Prompt2Start.ToString("HH:mm"),
            ["prompt_window2_end"]   = defaults.Prompt2End.ToString("HH:mm"),
            ["artia_activity_id"]    = defaults.ArtiaActivityId,
            ["user_email"]           = defaults.UserEmail,
        };

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT OR IGNORE INTO app_settings (key, value) VALUES ($key, $value)";
        cmd.Parameters.Add("$key", SqliteType.Text);
        cmd.Parameters.Add("$value", SqliteType.Text);

        foreach (var (key, value) in rows)
        {
            cmd.Parameters["$key"].Value = key;
            cmd.Parameters["$value"].Value = value;
            cmd.ExecuteNonQuery();
        }
    }
}
