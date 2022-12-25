using TrainStats.Service.Models;

namespace TrainStats.Service.Services;

public class DatabaseService
{
    public static async Task AddOrUpdate(string stationId, List<TrainData> trains)
    {
        // var db = GetDbConnection();

        // foreach (var train in trains)
        // {
        //     db.AddOrUpdate(stationId, train);
        // }
    }
}
