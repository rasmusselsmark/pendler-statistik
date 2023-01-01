using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json.Linq;
using TrainStats.Service.Models;

namespace TrainStats.Service.Services;

public class TrainService
{
    public static Dictionary<string, DateTime> NextTrainTimes = new Dictionary<string, DateTime>();

    public static async Task<List<TrainData>> GetTrainsAsync(string stationId)
    {
        using var ws = new ClientWebSocket();

        await ws.ConnectAsync(new Uri($"ws://labapiprod.dinstation.dk/api/ws/departure/{stationId}/"), CancellationToken.None);

        var data = new byte[4096];
        var buffer = new ArraySegment<byte>(data);

        using var memStream = new MemoryStream();
        WebSocketReceiveResult? result;
        do
        {
            result = await ws.ReceiveAsync(buffer, CancellationToken.None).ConfigureAwait(false);
            if (result.Count > 0)
            {
                memStream.Write(buffer.Array!, buffer.Offset, result.Count);
            }
            else
            {
                throw new Exception($"WS received zero: {result.Count}, {ws.State}");
            }
        } while (!result.EndOfMessage); // check end of message mark

        var json = StreamToString(memStream);
        var jsonData = JObject.Parse(json);

        var trains = new List<TrainData>();
        foreach (var train in jsonData["data"]!["Trains"]!)
        {
            trains.Add(new TrainStats.Service.Models.TrainData(train, stationId));
        }

        return trains;
    }

    /// <summary>
    /// Fetches current train data and stores to database, based on unique train id.
    /// Store departure time of next train so we can bail out early, if we're
    /// </summary>
    /// <param name="stationId">Station id, typically 2 characters. E.g. 'HH'.</param>
    public static async Task<int> FetchAndStoreAsync(string stationId)
    {
        // no need to query/store in database if we already have queried data for next train
        if (!NextTrainTimes.TryGetValue(stationId, out var nextTrain)
            || System.DateTime.Now > nextTrain.Add(TimeSpan.FromMinutes(-1)))
        {
            var trains = await GetTrainsAsync(stationId);

            // departed trains still shows up a few mins after departure, but no need to query those again
            // we include cancelled trains, as there could be replacement trains
            NextTrainTimes[stationId] = trains.First(t => t.TrainDeparted == null).ScheduleTime;

            return DatabaseService.AddOrUpdate(trains);
        }

        return -1; // no need to query data
    }

    static string StreamToString(Stream stream)
    {
        stream.Position = 0;
        using StreamReader reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
