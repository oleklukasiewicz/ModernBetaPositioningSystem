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

positionService.OnPlayerPositionUpdated += (sender, e) =>
{
    var closestFeature = routeService.GetClosestFeature(e.PlayerPosition.ActualPosition);
    routeService.DetectActiveRoutes(e.PlayerPosition, closestFeature);
};

// Zdarzenie: Wjazd na stację
routeService.OnApproach += async (sender, e) =>
{
    if (e.IsInsideApproachingFeature)
    {
        Debug.WriteLine($"{e.PlayerRoute.Username}> [THIS IS] {e.ApproachingFeature.Name}");

        // Wysyłamy wiadomość TYLKO do osób śledzących tego użytkownika
        await hubContext.Clients.Group(e.PlayerRoute.Username).SendAsync("OnStationArrived", new
        {
            Username = e.PlayerRoute.Username,
            StationName = e.ApproachingFeature.Name,
            AudioFile = e.ApproachingFeature.Name.ToLower().Replace(" ", "_") + ".mp3"
        });
    }
};

// Zdarzenie: Zapowiedź następnej stacji
routeService.OnLeave += async (sender, e) =>
{
    if (e.PlayerRoute.HeadingTo != null)
    {
        Debug.WriteLine($"{e.PlayerRoute.Username}> [NEXT STATION IS] {e.PlayerRoute.HeadingTo?.Name}");

        await hubContext.Clients.Group(e.PlayerRoute.Username).SendAsync("OnNextStationAnnounced", new
        {
            Username = e.PlayerRoute.Username,
            NextStationName = e.PlayerRoute.HeadingTo?.Name,
            AudioFile = e.PlayerRoute.HeadingTo?.Name.ToLower().Replace(" ", "_") + ".mp3"
        });
    }
};

routeService.OnPlayerRouteAdded += async (sender, e) =>
{
    Debug.WriteLine($"[PLAYER ROUTE ADDED] {e.PlayerRoute.Username} added to route: {e.PlayerRoute.Route.Name}");
    await hubContext.Clients.Group(e.PlayerRoute.Username).SendAsync("OnRouteAdded", new { Username = e.PlayerRoute.Username, RouteName = e.PlayerRoute.Route.Name });
};

routeService.OnPlayerRouteDisposed += async (sender, e) =>
{
    Debug.WriteLine($"[PLAYER ROUTE DISPOSED] {e.PlayerRoute.Username} removed from route: {e.PlayerRoute.Route.Name}");
    await hubContext.Clients.Group(e.PlayerRoute.Username).SendAsync("OnRouteDisposed", new { Username = e.PlayerRoute.Username, RouteName = e.PlayerRoute.Route.Name });
};

foreach (var route in routes)
{
    routeService.AddRoute(route);
}

_ = positionService.StartTrackingLoop(app.Lifetime.ApplicationStopping);

app.Run();