using System;
using System.IO;
using System.Text.Json;

namespace TrainStats;

public class Settings
{
    public string DbConnectionString { get; init; }

    public static Settings Load()
    {
        return JsonSerializer.Deserialize<Settings>(File.ReadAllText("settings.json"));
    }
}
