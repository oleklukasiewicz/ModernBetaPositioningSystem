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
var trackingService = new ModernBetaPositioningSystem.Services.TrackingService(url, apiKey);
trackingService.RegisterRoutes(routes);
_ = trackingService.StartTrackingLoop(1, app.Lifetime.ApplicationStopping);
app.Run();