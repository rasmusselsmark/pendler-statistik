using System;
using System.Globalization;
using Newtonsoft.Json.Linq;

namespace TrainStats.Service.Models;

public class TrainData
{
    /// <summary>
    /// Unique id based on date and train id
    /// </summary>
    public string Id { get; set; }

    public int TrainId { get; set; }
    public DateTime ScheduleTime { get; set; }
    public string OriginStationId { get; set; }
    public string DestinationStationId { get; set; }
    public bool IsCancelled { get; set; }
    public DateTime? EstimatedTimeDeparture { get; set; }
    public DateTime? DelayTime { get; set; }
    public TimeSpan Delay { get; set; }
    public int TrackCurrent { get; set; }
    public int? TrackOriginal { get; set; }
    public DateTime? TrainArrived { get; set; }
    public DateTime? TrainDeparted { get; set; }

    public TrainData(JToken jsonData)
    {
        TrainId = jsonData["TrainId"]!.Value<int>()!;
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

        TrackCurrent = jsonData["TrackCurrent"]!.Value<int>()!;
        if (!string.IsNullOrEmpty(jsonData["TrackOriginal"]!.Value<string>()))
        {
            TrackOriginal = jsonData["TrackOriginal"]!.Value<int>()!;
        }

        TrainArrived = ParseJsonDateTime(jsonData["TrainArrived"]);
        TrainDeparted = ParseJsonDateTime(jsonData["TrainDeparted"]);

        // calculate unique database id, so we keep one row per train/day
        Id = $"{ScheduleTime:yyyyMMdd}-{TrainId}";
    }

    DateTime? ParseJsonDateTime(JToken? token)
    {
        if (token == null)
        {
            return null;
        }

        var s = token.Value<string>();
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
