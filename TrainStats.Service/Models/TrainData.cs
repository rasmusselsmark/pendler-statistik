using System.Globalization;
using Newtonsoft.Json.Linq;

namespace TrainStats.Service.Models;

public class TrainData
{
    /// <summary>
    /// Unique id based on date and train id
    /// </summary>
    public string Id { get; }

    public int TrainId { get; }
    public string StationId { get; }
    public DateTime ScheduleTime { get; }
    public string OriginStationId { get; }
    public string DestinationStationId { get; }
    public bool IsCancelled { get; }
    public DateTime? EstimatedTimeDeparture { get; }
    public DateTime? DelayTime { get; }
    public TimeSpan Delay { get; }
    public int TrackCurrent { get; }
    public int? TrackOriginal { get; }
    public DateTime? TrainArrived { get; }
    public DateTime? TrainDeparted { get; }

    public TrainData(JToken jsonData, string stationId)
    {
        TrainId = jsonData["TrainId"]!.Value<int>();
        StationId = stationId;
        ScheduleTime = DateTime.ParseExact(
            jsonData["ScheduleTime"]!.Value<string>()!,
            "dd-MM-yyyy HH:mm:ss",
            CultureInfo.InvariantCulture);
        OriginStationId = jsonData["Routes"]!.First()["OriginStationId"]!.Value<string>()!;
        DestinationStationId = jsonData["Routes"]!.First()["DestinationStationId"]!.Value<string>()!;
        IsCancelled = jsonData["IsCancelled"]!.Value<bool>();
        EstimatedTimeDeparture = ParseJsonDateTime(jsonData["EstimatedTimeDeparture"]);

        if (EstimatedTimeDeparture == null)
        {
            DelayTime = null;
            Delay = TimeSpan.Zero;
        }
        else
        {
            DelayTime = ParseJsonDateTime(jsonData["DelayTime"]);
            Delay = DelayTime == null ? TimeSpan.Zero : DelayTime.Value - ScheduleTime;
        }

        TrackCurrent = jsonData["TrackCurrent"]!.Value<int>();
        if (!string.IsNullOrEmpty(jsonData["TrackOriginal"]!.Value<string>()))
        {
            TrackOriginal = jsonData["TrackOriginal"]!.Value<int>();
        }

        TrainArrived = ParseJsonDateTime(jsonData["TrainArrived"]);
        TrainDeparted = ParseJsonDateTime(jsonData["TrainDeparted"]);

        // calculate unique database id, so we keep one row per train/day
        Id = $"{StationId}-{ScheduleTime:yyyyMMdd}-{TrainId}";
    }

    private static DateTime? ParseJsonDateTime(JToken? token)
    {
        var s = token?.Value<string>();
        if (s == null)
        {
            return null;
        }

        var value = DateTime.ParseExact(s, "dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture);
        if (value.Year == 1)
        {
            return null;
        }

        return value;
    }
}
