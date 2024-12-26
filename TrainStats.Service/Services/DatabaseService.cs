using System.Globalization;
using System.Text;
using MySql.Data.MySqlClient;
using TrainStats.Service.Models;

namespace TrainStats.Service.Services;

public static class DatabaseService
{
    private static MySqlConnection GetDbConnection()
    {
        var settings = Settings.Load();
        var cnn = new MySqlConnection(settings.DbConnectionString);
        cnn.Open();

        return cnn;
    }

    public static int AddOrUpdate(List<TrainData> trains)
    {
        using var cnn = GetDbConnection();

        foreach (var train in trains)
        {
            const string sql = "INSERT INTO train_stats (id, station_id, train_id, origin_station_id, destination_station_id, schedule_time, is_cancelled, estimated_time_departure, delay_time, train_arrived, train_departed, delay, track_current, track_original) " +
                               "VALUES (?id, ?station_id, ?train_id, ?origin_station_id, ?destination_station_id, ?schedule_time, ?is_cancelled, ?estimated_time_departure, ?delay_time, ?train_arrived, ?train_departed, ?delay, ?track_current, ?track_original) " +
                               "ON DUPLICATE KEY UPDATE station_id=?station_id, train_id=?train_id, origin_station_id=?origin_station_id, destination_station_id=?destination_station_id, schedule_time=?schedule_time, is_cancelled=?is_cancelled, " +
                               "estimated_time_departure=?estimated_time_departure, delay_time=?delay_time, train_arrived=?train_arrived, train_departed=?train_departed, delay=?delay, track_current=?track_current, track_original=?track_original";

            using var cmd = cnn.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("?id", train.Id);
            cmd.Parameters.AddWithValue("?station_id", train.StationId);
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
        return trains.Count;
    }

    public static string QueryDelays(string stationId)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Forsinkede tog seneste 30 dage:");
        sb.AppendLine();

        // only include delays above 2 mins ("delay >= 200")
        const string sql = """
                           SELECT
                               destination_station_id as dest,
                               COUNT(*) as count_delayed,
                               (SELECT COUNT(*) FROM train_stats WHERE station_id = ?station_id AND schedule_time > DATE_ADD(NOW(), INTERVAL -30 DAY) AND schedule_time < NOW() AND destination_station_id = dest AND NOT is_cancelled) as count_dest_total,
                               (SELECT COUNT(*) FROM train_stats WHERE station_id = ?station_id AND schedule_time > DATE_ADD(NOW(), INTERVAL -30 DAY) AND schedule_time < NOW() AND NOT is_cancelled) as count_total,
                               IFNULL(AVG(TIME_TO_SEC(delay)), 0) as average_delay_seconds,
                               IFNULL(MAX(delay), '00:00:00') as max_delay
                           FROM train_stats
                           WHERE station_id = ?station_id AND schedule_time > DATE_ADD(NOW(), INTERVAL -30 DAY) AND schedule_time < NOW() AND delay >= 200 AND NOT is_cancelled
                           GROUP BY destination_station_id
                           ORDER BY destination_station_id
                           """;

        using var cnn = GetDbConnection();
        using var cmd = cnn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("?station_id", stationId);

        var total = 0d;
        var sumDelayed = 0d;

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var destination = reader.GetString("dest");
            var delayed = reader.GetInt64("count_delayed");
            sumDelayed += delayed;
            var totalForDestination = reader.GetInt64("count_dest_total");
            var avg = reader.GetDouble("average_delay_seconds");
            var max = DateTime.ParseExact(
                reader.GetString("max_delay"),
                "HH:mm:ss",
                CultureInfo.InvariantCulture);
            total = reader.GetInt64("count_total");

            if (totalForDestination != 0)
            {
                var percent = delayed * 100.0 / totalForDestination;
                sb.AppendLine(
                    $@"Retning {destination,-3}: {delayed,3:N0} / {totalForDestination,4:N0} = {percent,5:N2}%, gns. {TimeSpan.FromSeconds(avg),5:m\:ss}, max. {max,5:m\:ss} minutter");
            }
        }

        reader.Close();
        cnn.Close();

        sb.AppendLine();
        sb.AppendLine(
            $"I alt      : {sumDelayed,3:N0} / {total,4:N0} = {sumDelayed * 100.0 / total,5:N2}%");

        return sb.ToString();
    }

    public static string QueryCancellations(string stationId)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Aflyste tog seneste 30 dage:");
        sb.AppendLine();

        const string sql = """
                           SELECT
                               destination_station_id as dest,
                               COUNT(*) as count_cancelled,
                               (SELECT COUNT(*) FROM train_stats WHERE station_id = ?station_id AND schedule_time > DATE_ADD(NOW(), INTERVAL -30 DAY) AND schedule_time < NOW() AND destination_station_id = dest) as count_dest_total,
                               (SELECT COUNT(*) FROM train_stats WHERE station_id = ?station_id AND schedule_time > DATE_ADD(NOW(), INTERVAL -30 DAY) AND schedule_time < NOW()) as count_total
                           FROM train_stats
                           WHERE station_id = ?station_id AND schedule_time > DATE_ADD(NOW(), INTERVAL -30 DAY) AND schedule_time < NOW() AND is_cancelled
                           GROUP BY destination_station_id
                           ORDER BY destination_station_id
                           """;

        using var cnn = GetDbConnection();
        using var cmd = cnn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("?station_id", stationId);

        var total = 0d;
        var sumCancelled = 0d;

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var destination = reader.GetString("dest");
            var cancelled = reader.GetInt64("count_cancelled");
            sumCancelled += cancelled;
            var totalForDestination = reader.GetInt64("count_dest_total");
            total = reader.GetInt64("count_total");

            if (totalForDestination != 0)
            {
                var percent = cancelled * 100.0 / totalForDestination;
                sb.AppendLine(
                    $"Retning {destination,-3}: {cancelled,3:N0} / {totalForDestination,4:N0} = {percent,5:N2}%");
            }
        }

        reader.Close();
        cnn.Close();

        sb.AppendLine();
        sb.AppendLine(
            $"I alt      : {sumCancelled,3:N0} / {total,4:N0} = {sumCancelled * 100.0 / total,5:N2}%");

        return sb.ToString();
    }

    public static string QueryTracks(string stationId)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Sporfordeling seneste 30 dage:");
        sb.AppendLine();

        const string sql = """
                           SELECT
                           	track_current,
                               COUNT(*) as count_trains,
                               (SELECT COUNT(*) FROM train_stats WHERE station_id = ?station_id AND schedule_time > DATE_ADD(NOW(), INTERVAL -30 DAY) AND schedule_time < NOW() AND NOT is_cancelled) as count_total
                           FROM train_stats
                           WHERE station_id = ?station_id AND (schedule_time > DATE_ADD(NOW(), INTERVAL -30 DAY)) AND schedule_time < NOW() AND NOT is_cancelled
                           GROUP BY track_current
                           ORDER BY track_current
                           """;

        using var cnn = GetDbConnection();
        using var cmd = cnn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("?station_id", stationId);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var track = reader.GetInt64("track_current");
            var count = reader.GetInt64("count_trains");
            var total = reader.GetInt64("count_total");
            var percent = count * 100.0 / total;

            sb.AppendLine(
                $"Spor {track}: {count,4:N0} ({percent,4:N1}%)");
        }

        reader.Close();
        cnn.Close();

        return sb.ToString();
    }

    public static string QueryTracksDetailed(string stationId)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Sporfordeling seneste 30 dage (detaljeret):");
        sb.AppendLine();

        const string sql = """
                           SELECT
                           	track_current,
                               destination_station_id,
                               COUNT(*) as count_trains
                           FROM train_stats
                           WHERE station_id = ?station_id AND (schedule_time > DATE_ADD(NOW(), INTERVAL -30 DAY)) AND schedule_time < NOW() AND NOT is_cancelled
                           GROUP BY track_current, destination_station_id
                           ORDER BY track_current, destination_station_id
                           """;

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
                $"Spor {track}, retning {destination,-3}: {count,3:N0}");
        }

        reader.Close();
        cnn.Close();

        return sb.ToString();
    }

    public static string Install()
    {
        const string sql = """
                           CREATE TABLE train_stats (
                             id VARCHAR(25) NOT NULL,
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
                             INDEX idx_station_time (station_id ASC, schedule_time ASC) VISIBLE);
                           """;

        using var cnn = GetDbConnection();
        var cmd = cnn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
        cnn.Close();

        return "Database configured!";
    }
}
