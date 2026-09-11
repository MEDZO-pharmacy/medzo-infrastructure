using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);
// Console logging works in containers and without Windows Event Log permissions.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
builder.Services.AddHealthChecks();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    if (origins.Length > 0)
        policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
}));

var app = builder.Build();
app.UseCors();
app.Use(async (context, next) =>
{
    var started = Stopwatch.GetTimestamp();
    await next();
    // Never log cookies, tokens, query strings, or request bodies.
    app.Logger.LogInformation("{Method} {Path} returned {StatusCode} in {ElapsedMs}ms",
        context.Request.Method, context.Request.Path, context.Response.StatusCode,
        Stopwatch.GetElapsedTime(started).TotalMilliseconds);
});
// Liveness only: does not claim the downstream API or database is healthy.
app.MapHealthChecks("/health");
// Authentication and authorization remain enforced by the destination service.
app.MapReverseProxy();
app.Run();

public partial class Program;
