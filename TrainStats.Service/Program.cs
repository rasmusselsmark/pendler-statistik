using TrainStats.Service.Services;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => AboutService.About());
app.MapGet("/trains", async () => await TrainService.GetTrainsAsync("HH"));
app.MapGet("/fetch", async () => await TrainService.FetchAndStoreAsync("HH"));

app.Run();
