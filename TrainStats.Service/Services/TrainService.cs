using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json.Linq;
using TrainStats.Service.Models;

namespace TrainStats.Service.Services;

public static class TrainService
{
    public static readonly Dictionary<string, DateTime> NextTrainTimes = new();

    public static async Task<List<TrainData>> GetTrainsAsync(string stationId)
    {
        using var ws = new ClientWebSocket();

        await ws.ConnectAsync(new Uri($"wss://api.mittog.dk/api/ws/departure/{stationId}/dinstation/"), CancellationToken.None);

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

        return jsonData["data"]!["Trains"]!.Select(train => new TrainData(train, stationId)).ToList();
    }

    /// <summary>
    /// Fetches current train data and stores to database, based on unique train id.
    /// Store departure time of next train so we can bail out early, if we're
    /// </summary>
    /// <param name="stationId">Station id, typically 2 characters. E.g. 'HH'.</param>
    public static async Task<int> FetchAndStoreAsync(string stationId)
    {
        // no need to query/store in database if we already have queried data for next train
        if (NextTrainTimes.TryGetValue(stationId, out var nextTrain)
            && DateTime.Now <= nextTrain.Add(TimeSpan.FromMinutes(-1)))
        {
            return -1; // no need to query data
        }

        var trains = await GetTrainsAsync(stationId);

        // departed trains still shows up a few mins after departure, but no need to query those again
        // we include cancelled trains, as there could be replacement trains
        NextTrainTimes[stationId] = trains.First(t => t.TrainDeparted == null).ScheduleTime;

        return DatabaseService.AddOrUpdate(trains);
    }

    private static string StreamToString(Stream stream)
    {
        stream.Position = 0;
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
