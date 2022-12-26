using System.Text;

namespace TrainStats.Service.Services;

public class AboutService
{
    public static string About()
    {
        var sb = new StringBuilder();
        sb.AppendLine("pendler-statistik");
        sb.AppendLine();
        sb.AppendLine($"ASP.NET version {System.Environment.Version}");
        sb.AppendLine($"Current time: {System.DateTime.Now}");
        sb.AppendLine();
        sb.AppendLine("Next train(s):");

        foreach (var key in TrainService.NextTrainTimes.Keys)
        {
            sb.AppendLine($"{key}: {TrainService.NextTrainTimes[key]}");
        }

        return sb.ToString();
    }
}
