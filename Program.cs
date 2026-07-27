using ModernBetaPositioningSystem.Services;
using System.Diagnostics;
using Route = ModernBetaPositioningSystem.Models.Route;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();

var app = builder.Build();

var routes = app.Configuration.GetSection("Routes").Get<List<Route>>();
var url = app.Configuration.GetValue<string>("ApiUrl");
var apiKey = app.Configuration.GetValue<string>("ApiKey");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

//start tracking loop
var positionService = new ModernBetaPositioningSystem.Services.PositionService(url, apiKey);
var routeService = new ModernBetaPositioningSystem.Services.RouteService();

positionService.OnPlayerPositionUpdated += (sender, e) =>
{
    var closestFeature = routeService.GetClosestFeature(e.PlayerPosition.ActualPosition);
    routeService.DetectActiveRoutes(e.PlayerPosition, closestFeature);
};
routeService.OnApproach += (sender, e) =>
{
    if (e.IsInsideApproachingFeature)
        Debug.WriteLine($"{e.PlayerRoute.Username}> [THIS IS] {e.ApproachingFeature.Name}");
};

routeService.OnLeave += (sender, e) =>
{
    if (e.PlayerRoute.HeadingTo != null)
        Debug.WriteLine($"{e.PlayerRoute.Username}> [NEXT STATION IS] {e.PlayerRoute.HeadingTo?.Name}");
};

routeService.OnPlayerRouteAdded += (sender, e) =>
{
    Debug.WriteLine($"[PLAYER ROUTE ADDED] {e.PlayerRoute.Username} added to route: {e.PlayerRoute.Route.Name}");
};

routeService.OnPlayerRouteDisposed += (sender, e) =>
{
    Debug.WriteLine($"[PLAYER ROUTE DISPOSED] {e.PlayerRoute.Username} removed from route: {e.PlayerRoute.Route.Name}");
};
foreach (var route in routes)
{
    routeService.AddRoute(route);
}
_ = positionService.StartTrackingLoop(app.Lifetime.ApplicationStopping);
app.Run();