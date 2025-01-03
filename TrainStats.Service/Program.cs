using TrainStats.Service.Services;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// ONLY ENABLE IN DEVELOPMENT
// app.UseDeveloperExceptionPage();
// app.MapGet("/error", () => { throw new Exception("Error"); });

const string gitRepo = "https://github.com/rasmusselsmark/pendler-statistik";
app.MapGet("/", () => Results.Content($"""
<html>
    <body>
        <h2>Pendler Statistik</h2>
        <p>Se <a href='{gitRepo}'>{gitRepo}</a> for mere information</p>
    </body>
</html>
""", "text/html"));

app.MapGet("/about", AboutService.About);
app.MapGet("/install", DatabaseService.Install);

app.MapGet(
    "/trains/{stationId:alpha}",
    async (string stationId) => await TrainService.GetTrainsAsync(stationId));

app.MapGet(
    "/fetch/{stationId:alpha}",
    async (string stationId) => await TrainService.FetchAndStoreAsync(stationId));

app.MapGet(
    "/query/delays/{stationId:alpha}",
    DatabaseService.QueryDelays);

app.MapGet(
    "/query/cancellations/{stationId:alpha}",
    DatabaseService.QueryCancellations);

app.MapGet(
    "/query/tracks/{stationId:alpha}",
    DatabaseService.QueryTracks);

app.MapGet(
    "/query/tracks/detailed/{stationId:alpha}",
    DatabaseService.QueryTracksDetailed);

app.Run();
