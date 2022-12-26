using System;
using System.Globalization;
using System.Text;
using MySql.Data.MySqlClient;
using TrainStats.Service.Models;

namespace TrainStats.Service.Services;

public class DatabaseService
{
    static MySqlConnection GetDbConnection()
    {
        var settings = Settings.Load();
        var cnn = new MySqlConnection(settings.DbConnectionString);
        cnn.Open();

        return cnn;
    }

    public static int AddOrUpdate(string stationId, List<TrainData> trains)
    {
        using var cnn = GetDbConnection();

        foreach (var train in trains)
        {
            var sql =
                "INSERT INTO train_stats (id, station_id, train_id, origin_station_id, destination_station_id, schedule_time, is_cancelled, estimated_time_departure, delay_time, train_arrived, train_departed, delay, track_current, track_original) " +
                "VALUES (?id, ?station_id, ?train_id, ?origin_station_id, ?destination_station_id, ?schedule_time, ?is_cancelled, ?estimated_time_departure, ?delay_time, ?train_arrived, ?train_departed, ?delay, ?track_current, ?track_original) " +
                "ON DUPLICATE KEY UPDATE station_id=?station_id, train_id=?train_id, origin_station_id=?origin_station_id, destination_station_id=?destination_station_id, schedule_time=?schedule_time, is_cancelled=?is_cancelled, " +
                "estimated_time_departure=?estimated_time_departure, delay_time=?delay_time, train_arrived=?train_arrived, train_departed=?train_departed, delay=?delay, track_current=?track_current, track_original=?track_original";

            using var cmd = cnn.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("?id", train.Id);
            cmd.Parameters.AddWithValue("?station_id", stationId);
            cmd.Parameters.AddWithValue("?train_id", train.TrainId);
            cmd.Parameters.AddWithValue("?origin_station_id", train.OriginStationId);
            cmd.Parameters.AddWithValue("?destination_station_id", train.DestinationStationId);
            cmd.Parameters.AddWithValue("?schedule_time", train.ScheduleTime);
            cmd.Parameters.AddWithValue("?is_cancelled", train.IsCancelled);
            cmd.Parameters.AddWithValue("?estimated_time_departure", train.EstimatedTimeDeparture);
            cmd.Parameters.AddWithValue("?delay_time", train.DelayTime);
            cmd.Parameters.AddWithValue("?train_arrived", train.TrainArrived);
            cmd.Parameters.AddWithValue("?train_departed", train.TrainDeparted);
            cmd.Parameters.AddWithValue("?delay", train.Delay);
            cmd.Parameters.AddWithValue("?track_current", train.TrackCurrent);
            cmd.Parameters.AddWithValue("?track_original", train.TrackOriginal);
            cmd.ExecuteNonQuery();
        }

        cnn.Close();
        return trains.Count();
    }

    public static string QueryDelays(string stationId)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Forsinkede tog seneste 30 dage:");
        sb.AppendLine();

        var sql =
@"SELECT
    destination_station_id,
    COUNT(*) as count_delayed,
    (SELECT COUNT(*) FROM train_stats WHERE station_id = ?station_id AND schedule_time > DATE_ADD(NOW(), INTERVAL -30 DAY) AND schedule_time < NOW()) as count_total,
    IFNULL(AVG(TIME_TO_SEC(delay)), 0) as average_delay_seconds,
    IFNULL(MAX(delay), '00:00:00') as max_delay
FROM train_stats
WHERE station_id = ?station_id AND schedule_time > DATE_ADD(NOW(), INTERVAL -30 DAY) AND schedule_time < NOW() AND delay > 0
GROUP BY destination_station_id";

        using var cnn = GetDbConnection();
        using var cmd = cnn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("?station_id", stationId);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var destination = reader.GetString("destination_station_id");
            var delayed = reader.GetInt64("count_delayed");
            var total = reader.GetInt64("count_total");
            var avg = reader.GetDouble("average_delay_seconds");
            var max = DateTime.ParseExact(
                reader.GetString("max_delay"),
                "HH:mm:ss",
                CultureInfo.InvariantCulture);

            if (total != 0)
            {
                var percent = (delayed * 100.0 / total);
                sb.AppendLine(
                    $"Retning {destination}: {delayed,3:N0} / {total,4:N0} = {percent,5:N2}%, gns. {TimeSpan.FromSeconds(avg),5:m\\:ss}, max. {max,5:m\\:ss} minutter");
            }
        }

        reader.Close();
        cnn.Close();

        return sb.ToString();
    }

    public static string QueryTracks(string stationId)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Sporfordeling seneste 30 dage:");
        sb.AppendLine();

        var sql =
@"SELECT
	track_current,
    destination_station_id,
    COUNT(*) as count_trains
FROM train_stats
WHERE station_id = ?station_id AND (schedule_time > DATE_ADD(NOW(), INTERVAL -30 DAY)) AND schedule_time < NOW()
GROUP BY track_current, destination_station_id
ORDER BY track_current, destination_station_id";

        using var cnn = GetDbConnection();
        using var cmd = cnn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("?station_id", stationId);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var track = reader.GetInt64("track_current");
            var destination = reader.GetString("destination_station_id");
            var count = reader.GetInt64("count_trains");

            sb.AppendLine(
                $"Spor {track}, retning {destination}: {count,3:N0}");
        }

        reader.Close();
        cnn.Close();

        return sb.ToString();
    }

    public static string Install()
    {
        var sql = @"CREATE TABLE train_stats (
  id VARCHAR(20) NOT NULL,
  station_id VARCHAR(5) NOT NULL,
  train_id INT NOT NULL,
  origin_station_id VARCHAR(5) NOT NULL,
  destination_station_id VARCHAR(5) NOT NULL,
  schedule_time DATETIME NOT NULL,
  is_cancelled TINYINT NOT NULL,
  estimated_time_departure DATETIME NULL,
  delay_time DATETIME NULL,
  train_arrived DATETIME NULL,
  train_departed DATETIME NULL,
  delay TIME NULL,
  track_current INT NOT NULL,
  track_original INT NULL,
  PRIMARY KEY (id),
  UNIQUE INDEX id_UNIQUE (id ASC),
  INDEX idx_station_time (station_id ASC, schedule_time ASC) VISIBLE);";

        using var cnn = GetDbConnection();
        var cmd = cnn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
        cnn.Close();

        return "Database configured!";
    }
}
