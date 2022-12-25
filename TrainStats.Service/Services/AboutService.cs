namespace TrainStats.Service.Services;

public class AboutService
{
    public static string About()
    {
        return $@"pendler-statistik

ASP.NET version {System.Environment.Version}
Current time: {System.DateTime.Now}
Next train: {TrainService.GetNextTrainTime("HH")}";
    }
}
