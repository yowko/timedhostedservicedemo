using TimedHostedServiceDemo;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<UpdateCacheService>();
builder.Services.AddHostedService<TimedHostedService>();
var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();