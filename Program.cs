using Microsoft.AspNetCore.SignalR;
using ModernBetaPositioningSystem.Hubs;
using ModernBetaPositioningSystem.Models;
using ModernBetaPositioningSystem.Services;
using System.Diagnostics;
using Route = ModernBetaPositioningSystem.Models.Route;

var builder = WebApplication.CreateBuilder(args);
ConfigureServices(builder);

var url = builder.Configuration.GetValue<string>("ApiUrl") ?? string.Empty;
var apiKey = builder.Configuration.GetValue<string>("ApiKey") ?? string.Empty;
var positionService = new PositionService(url, apiKey);
builder.Services.AddSingleton(positionService);

var app = builder.Build();
ConfigureApplication(app);

var routes = app.Configuration.GetSection("Routes").Get<List<Route>>() ?? new();
var routeService = new RouteService();
var hubContext = app.Services.GetRequiredService<IHubContext<RouteHub>>();

RouteEventRegistrations.Register(positionService, routeService, hubContext);
routeService.AddRoute(routes);

_ = positionService.StartTrackingLoop(app.Lifetime.ApplicationStopping);

app.Run();

static void ConfigureServices(WebApplicationBuilder builder)
{
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddOpenApi();
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
}

static void ConfigureApplication(WebApplication app)
{
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
}

static class RouteEventRegistrations
{
    public static void Register(PositionService positionService, RouteService routeService, IHubContext<RouteHub> hubContext)
    {
        positionService.OnPlayerPositionUpdated += async (_, e) =>
        {
            var closestFeature = routeService.GetClosestFeature(e.PlayerPosition.ActualPosition);
            routeService.DetectActiveRoutes(e.PlayerPosition, closestFeature);
            await hubContext.Clients.Group(e.PlayerPosition.Username).SendAsync("OnPositionUpdate", e);
        };

        routeService.OnApproach += async (_, data) =>
        {
            if (!data.IsInsideApproachingFeature)
                return;

            Debug.WriteLine($"{data.PlayerRoute.Username}> [THIS IS] {data.ApproachingFeature.Name}");
            await hubContext.Clients.Group(data.PlayerRoute.Username).SendAsync("OnStation", data);
        };

        routeService.OnLeave += async (_, data) =>
        {
            if (data.PlayerRoute.HeadingTo == null)
                return;

            Debug.WriteLine($"{data.PlayerRoute.Username}> [NEXT STATION IS] {data.PlayerRoute.HeadingTo.Name}");
            await hubContext.Clients.Group(data.PlayerRoute.Username).SendAsync("OnNextStation", data);
        };

        routeService.OnPlayerRouteAdded += async (_, data) =>
        {
            Debug.WriteLine($"[PLAYER ROUTE ADDED] {data.PlayerRoute.Username} added to route: {data.PlayerRoute.Route.Name}");
            await hubContext.Clients.Group(data.PlayerRoute.Username).SendAsync("OnRouteJoin", data);
        };

        routeService.OnPlayerRouteDisposed += async (_, data) =>
        {
            Debug.WriteLine($"[PLAYER ROUTE DISPOSED] {data.PlayerRoute.Username} removed from route: {data.PlayerRoute.Route.Name}");
            await hubContext.Clients.Group(data.PlayerRoute.Username).SendAsync("OnRouteLeave", data);
        };
    }
}