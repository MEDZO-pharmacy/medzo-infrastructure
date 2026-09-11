using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Xunit;

public sealed class GatewayTests : IAsyncLifetime
{
    private WebApplication backend = null!;
    private WebApplicationFactory<Program> gateway = null!;
    private HttpClient client = null!;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        backend = builder.Build();
        backend.Map("/{**path}", async context =>
        {
            if (context.Request.Path == "/api/auth/session")
            {
                context.Response.StatusCode = 401;
                return;
            }
            if (context.Request.Path == "/api/auth/login")
                context.Response.Headers.Append("Set-Cookie", "medzo.refresh=test; path=/api/auth; httponly; samesite=strict");
            if (context.Request.Path == "/api/auth/revoke")
                context.Response.Headers.Append("Set-Cookie", "medzo.refresh=; path=/api/auth; max-age=0; httponly");
            using var reader = new StreamReader(context.Request.Body);
            await context.Response.WriteAsJsonAsync(new Echo(
                context.Request.Path + context.Request.QueryString,
                context.Request.Method,
                context.Request.Headers.Authorization.ToString(),
                context.Request.Headers.Cookie.ToString(),
                await reader.ReadToEndAsync()));
        });
        await backend.StartAsync();
        gateway = new WebApplicationFactory<Program>().WithWebHostBuilder(host =>
            host.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ReverseProxy:Clusters:auth:Destinations:primary:Address"] = backend.Urls.Single(),
                    ["Cors:AllowedOrigins:0"] = "https://frontend.example"
                })));
        client = gateway.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
    }

    [Theory]
    [InlineData("/api/auth/login")]
    [InlineData("/api/users")]
    [InlineData("/api/users/123/managed")]
    [InlineData("/api/dashboard/admin")]
    [InlineData("/api/reviews")]
    [InlineData("/api/contact")]
    public async Task ForwardsPathQueryBodyAndCredentials(string path)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path + "?page=2");
        request.Headers.Add("Authorization", "Bearer test-token");
        request.Headers.Add("Cookie", "medzo.refresh=test-refresh");
        request.Content = new StringContent("payload");
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var echo = await response.Content.ReadFromJsonAsync<Echo>();
        Assert.Equal(new Echo(path + "?page=2", "POST", "Bearer test-token", "medzo.refresh=test-refresh", "payload"), echo);
    }

    [Theory]
    [InlineData("/api/auth/login", "medzo.refresh=test")]
    [InlineData("/api/auth/revoke", "max-age=0")]
    public async Task PreservesRefreshCookieResponses(string path, string expected)
    {
        var response = await client.PostAsync(path, null);
        Assert.Contains(expected, string.Join(";", response.Headers.GetValues("Set-Cookie")));
        Assert.Contains("path=/api/auth", string.Join(";", response.Headers.GetValues("Set-Cookie")));
    }

    [Fact]
    public async Task PreservesBackendAuthorizationFailure()
        => Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/session")).StatusCode);

    [Theory]
    [InlineData("/api/inventory")]
    [InlineData("/api/products")]
    [InlineData("/api/products/1")]
    [InlineData("/")]
    [InlineData("/index.html")]
    [InlineData("/swagger")]
    [InlineData("/api/users-other")]
    public async Task DoesNotExposeUnconfiguredRoutes(string path)
        => Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(path)).StatusCode);

    [Theory]
    [InlineData("https://frontend.example", true)]
    [InlineData("https://untrusted.example", false)]
    public async Task CorsAllowsOnlyConfiguredOrigin(string origin, bool allowed)
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/auth/login");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        var response = await client.SendAsync(request);
        Assert.Equal(allowed, response.Headers.Contains("Access-Control-Allow-Origin"));
        if (allowed)
        {
            Assert.Equal(origin, response.Headers.GetValues("Access-Control-Allow-Origin").Single());
            Assert.Equal("true", response.Headers.GetValues("Access-Control-Allow-Credentials").Single());
        }
    }

    [Fact]
    public async Task BackendFailureReturns502WhileGatewayRemainsAlive()
    {
        await backend.StopAsync();
        Assert.Equal(HttpStatusCode.BadGateway, (await client.GetAsync("/api/reviews")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
    }

    public async Task DisposeAsync()
    {
        client.Dispose();
        await gateway.DisposeAsync();
        await backend.DisposeAsync();
    }

    public sealed record Echo(string Path, string Method, string Authorization, string Cookie, string Body);
}
