using System.Text.Json;

namespace TrainStats.Service;

public class Settings
{
    public string? DbConnectionString { get; init; }

    public static Settings Load()
    {
        return JsonSerializer.Deserialize<Settings>(File.ReadAllText("settings.json"))!;
    }
}
