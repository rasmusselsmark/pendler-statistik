using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json.Linq;
using TrainStats.Service.Models;

namespace TrainStats.Service.Services;

public class TrainService
{
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
                break;
            }
        } while (!result.EndOfMessage); // check end of message mark

        var json = StreamToString(memStream);
        var jsonData = JObject.Parse(json);

        var trains = new List<TrainData>();
        foreach (var train in jsonData["data"]!["Trains"]!)
        {
            trains.Add(new TrainStats.Service.Models.TrainData(train));
        }

        return trains;
    }

    /// <summary>
    /// Fetches current train data and stores to database, based on unique train id.
    /// </summary>
    /// <param name="stationId">Station id, typically 2 characters. E.g. 'HH'.</param>
    public static async Task<int> FetchAndStoreAsync(string stationId)
    {
        var trains = await GetTrainsAsync(stationId);
        return DatabaseService.AddOrUpdate(stationId, trains);
    }

    static string StreamToString(Stream stream)
    {
        stream.Position = 0;
        using StreamReader reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
