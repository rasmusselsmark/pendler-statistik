var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => $@"pendler-statistik

ASP.NET version {System.Environment.Version}
{System.DateTime.Now}");

app.Run();
