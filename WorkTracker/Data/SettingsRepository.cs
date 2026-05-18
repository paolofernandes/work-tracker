using Microsoft.Data.Sqlite;
using WorkTracker.Models;

namespace WorkTracker.Data;

static class SettingsRepository
{
    static SqliteConnection Open() =>
        new SqliteConnection($"Data Source={DatabaseInitializer.DbPath}");

    public static AppSettings GetAll()
    {
        var map = new Dictionary<string, string>();
        using var conn = Open();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT key, value FROM app_settings";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            map[reader.GetString(0)] = reader.GetString(1);

        var defaults = AppSettings.Defaults;
        return new AppSettings
        {
            Prompt1Start    = ParseTime(map, "prompt_window1_start", defaults.Prompt1Start),
            Prompt1End      = ParseTime(map, "prompt_window1_end",   defaults.Prompt1End),
            Prompt2Start    = ParseTime(map, "prompt_window2_start", defaults.Prompt2Start),
            Prompt2End      = ParseTime(map, "prompt_window2_end",   defaults.Prompt2End),
            ArtiaActivityId = map.GetValueOrDefault("artia_activity_id", defaults.ArtiaActivityId),
            UserEmail       = map.GetValueOrDefault("user_email",        defaults.UserEmail),
        };
    }

    public static void SaveAll(AppSettings s)
    {
        var rows = new Dictionary<string, string>
        {
            ["prompt_window1_start"] = s.Prompt1Start.ToString("HH:mm"),
            ["prompt_window1_end"]   = s.Prompt1End.ToString("HH:mm"),
            ["prompt_window2_start"] = s.Prompt2Start.ToString("HH:mm"),
            ["prompt_window2_end"]   = s.Prompt2End.ToString("HH:mm"),
            ["artia_activity_id"]    = s.ArtiaActivityId,
            ["user_email"]           = s.UserEmail,
        };

        using var conn = Open();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT OR REPLACE INTO app_settings (key, value) VALUES ($key, $value)";
        cmd.Parameters.Add("$key", SqliteType.Text);
        cmd.Parameters.Add("$value", SqliteType.Text);

        foreach (var (key, value) in rows)
        {
            cmd.Parameters["$key"].Value = key;
            cmd.Parameters["$value"].Value = value;
            cmd.ExecuteNonQuery();
        }
    }

    static TimeOnly ParseTime(Dictionary<string, string> map, string key, TimeOnly fallback) =>
        map.TryGetValue(key, out var val) && TimeOnly.TryParse(val, out var t) ? t : fallback;
}
