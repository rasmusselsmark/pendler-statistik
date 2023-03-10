using TrainStats.Service.Services;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// todo: remove when out of development
app.UseDeveloperExceptionPage();
app.MapGet("/error", () => { throw new System.Exception("Error"); });

app.MapGet("/", () => "Se https://github.com/rasmusselsmark/pendler-statistik for mere information");
app.MapGet("/about", () => AboutService.About());
app.MapGet("/install", () => DatabaseService.Install());

app.MapGet(
    "/trains/{stationId:alpha}",
    async (string stationId) =>
    {
        return await TrainService.GetTrainsAsync(stationId);
    });

app.MapGet(
    "/fetch/{stationId:alpha}",
    async (string stationId) =>
    {
        return await TrainService.FetchAndStoreAsync(stationId);
    });

app.MapGet(
    "/query/delays/{stationId:alpha}",
    (string stationId) =>
    {
        return DatabaseService.QueryDelays(stationId);
    });

app.MapGet(
    "/query/cancellations/{stationId:alpha}",
    (string stationId) =>
    {
        return DatabaseService.QueryCancellations(stationId);
    });

app.MapGet(
    "/query/tracks/{stationId:alpha}",
    (string stationId) =>
    {
        return DatabaseService.QueryTracks(stationId);
    });

app.MapGet(
    "/query/tracks/detailed/{stationId:alpha}",
    (string stationId) =>
    {
        return DatabaseService.QueryTracksDetailed(stationId);
    });

app.Run();
