using Microsoft.AspNetCore.SignalR;
using ModernBetaPositioningSystem.Hubs;
using ModernBetaPositioningSystem.Services;
using System.Diagnostics;
using Route = ModernBetaPositioningSystem.Models.Route;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();

// 1. SignalR i CORS
builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSvelte", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:4173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

var routes = app.Configuration.GetSection("Routes").Get<List<Route>>() ?? new();
var url = app.Configuration.GetValue<string>("ApiUrl") ?? string.Empty;
var apiKey = app.Configuration.GetValue<string>("ApiKey") ?? string.Empty;

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowSvelte");
app.UseAuthorization();

app.MapControllers();
app.MapHub<RouteHub>("/routeHub");

var positionService = new PositionService(url, apiKey);
var routeService = new RouteService();

var hubContext = app.Services.GetRequiredService<IHubContext<RouteHub>>();

positionService.OnPlayerPositionUpdated += async (sender, e) =>
{
    var closestFeature = routeService.GetClosestFeature(e.PlayerPosition.ActualPosition);
    routeService.DetectActiveRoutes(e.PlayerPosition, closestFeature);
    await hubContext.Clients.Group(e.PlayerPosition.Username).SendAsync("OnPositionUpdate", e);
};

routeService.OnApproach += async (sender, e) =>
{
    var data = e;
    data.PlayerRoute.Route.Checkpoints = data.PlayerRoute.Route.Checkpoints.Where(c => c.IsInvisible != true).ToList();
    if (e.IsInsideApproachingFeature)
    {
        Debug.WriteLine($"{e.PlayerRoute.Username}> [THIS IS] {data.ApproachingFeature.Name}");
        await hubContext.Clients.Group(e.PlayerRoute.Username).SendAsync("OnStation", data);
    }
};
routeService.OnLeave += async (sender, e) =>
{
    var data = e;
    data.PlayerRoute.Route.Checkpoints = data.PlayerRoute.Route.Checkpoints.Where(c => c.IsInvisible != true).ToList();
    if (e.PlayerRoute.HeadingTo != null)
    {
        Debug.WriteLine($"{e.PlayerRoute.Username}> [NEXT STATION IS] {data.PlayerRoute.HeadingTo?.Name}");

        await hubContext.Clients.Group(e.PlayerRoute.Username).SendAsync("OnNextStation", data);
    }
};

routeService.OnPlayerRouteAdded += async (sender, e) =>
{
    var data = e;
    data.PlayerRoute.Route.Checkpoints = data.PlayerRoute.Route.Checkpoints.Where(c => c.IsInvisible != true).ToList();
    Debug.WriteLine($"[PLAYER ROUTE ADDED] {e.PlayerRoute.Username} added to route: {data.PlayerRoute.Route.Name}");
    await hubContext.Clients.Group(e.PlayerRoute.Username).SendAsync("OnRouteJoin", data);
};

routeService.OnPlayerRouteDisposed += async (sender, e) =>
{
    var data = e;
    data.PlayerRoute.Route.Checkpoints = data.PlayerRoute.Route.Checkpoints.Where(c => c.IsInvisible != true).ToList();
    Debug.WriteLine($"[PLAYER ROUTE DISPOSED] {e.PlayerRoute.Username} removed from route: {data.PlayerRoute.Route.Name}");
    await hubContext.Clients.Group(e.PlayerRoute.Username).SendAsync("OnRouteLeave", data);
};

foreach (var route in routes)
    routeService.AddRoute(route);

_ = positionService.StartTrackingLoop(app.Lifetime.ApplicationStopping);

app.Run();