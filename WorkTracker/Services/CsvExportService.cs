using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using WorkTracker.Models;

namespace WorkTracker.Services;

static class CsvExportService
{
    public static void Export(IEnumerable<WorkEntry> entries, AppSettings settings, string filePath)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
        };

        using var writer = new StreamWriter(filePath, append: false, System.Text.Encoding.UTF8);
        using var csv = new CsvWriter(writer, config);

        // Write Portuguese headers manually
        csv.WriteField("Data");
        csv.WriteField("Hora de início");
        csv.WriteField("Esforço");
        csv.WriteField("Observação");
        csv.WriteField("ID da Atividade");
        csv.WriteField("E-mail");
        csv.NextRecord();

        foreach (var e in entries)
        {
            csv.WriteField(e.Date.ToString("dd/MM/yyyy"));
            csv.WriteField(e.StartTime.ToString("HH:mm"));
            csv.WriteField((e.TotalHours ?? 0).ToString("0.##", CultureInfo.InvariantCulture));
            csv.WriteField(e.Description ?? string.Empty);
            csv.WriteField(settings.ArtiaActivityId);
            csv.WriteField(settings.UserEmail);
            csv.NextRecord();
        }
    }
}
