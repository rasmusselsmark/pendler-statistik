using TrainStats.Service.Services;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => AboutService.About());
app.MapGet("/install", () => DatabaseService.Install());
app.MapGet("/trains", async () => await TrainService.GetTrainsAsync("HH"));
app.MapGet("/fetch", async () => await TrainService.FetchAndStoreAsync("HH"));
app.MapGet("/query", () => DatabaseService.Query("HH"));

app.Run();
