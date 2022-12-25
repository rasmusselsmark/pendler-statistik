using System;
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
                "INSERT INTO train_stats (id, station_id, train_id, origin_station_id, destination_station_id, schedule_time, is_cancelled, estimated_time_departure, delay_time, delay) " +
                "VALUES (?id, ?station_id, ?train_id, ?origin_station_id, ?destination_station_id, ?schedule_time, ?is_cancelled, ?estimated_time_departure, ?delay_time, ?delay) " +
                "ON DUPLICATE KEY UPDATE station_id=?station_id, train_id=?train_id, origin_station_id=?origin_station_id, destination_station_id=?destination_station_id, schedule_time=?schedule_time, is_cancelled=?is_cancelled, estimated_time_departure=?estimated_time_departure, delay_time=?delay_time, delay=?delay";

            var cmd = cnn.CreateCommand();
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
            cmd.Parameters.AddWithValue("?delay", train.Delay);
            cmd.ExecuteNonQuery();
        }

        cnn.Close();
        return trains.Count();
    }

    public static List<TrainData> Query(string stationId)
    {
        // TODO: query for statistics for station, e.g. for last month
        return null;
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
  delay TIME NULL,
  PRIMARY KEY (id),
  UNIQUE INDEX id_UNIQUE (id ASC),
  INDEX schedule_time (schedule_time ASC));";

        using var cnn = GetDbConnection();
        var cmd = cnn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
        cnn.Close();

        return "Database configured!";
    }
}
