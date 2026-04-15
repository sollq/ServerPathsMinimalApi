using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ServerPathsMinimalApi.Models;
using ServerPathsMinimalApi.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();
builder.Services.AddOpenApi();

builder.WebHost.ConfigureKestrel(options =>
    options.ListenAnyIP(5001, listenOptions => listenOptions.UseHttps()));

builder.Services.Configure<FileServiceOptions>(
    builder.Configuration.GetSection("FileService"));

builder.Services.AddLogging();
builder.Services.AddResponseCompression(options => { options.EnableForHttps = true; });
builder.Services.AddSingleton<FileCacheBackgroundService>();
builder.Services.AddSingleton<IFileProviderBgService>(sp => sp.GetRequiredService<FileCacheBackgroundService>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<FileCacheBackgroundService>());
builder.Services.AddSingleton<ILinksComparerService, LinksComparerService>();

var app = builder.Build();

app.UseResponseCompression();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPost("/api/LinksComparer/compare-list", (
    [FromHeader(Name = "x-api-key")] string? apiKey,
    [FromBody] LinksComparisonRequest request,
    ILinksComparerService linksComparer,
    IOptions<FileServiceOptions> options) =>
{
    if (string.IsNullOrEmpty(apiKey) || apiKey != options.Value.ApiKey)
        return Results.Unauthorized();

    var response = linksComparer.GetInvalidLinks(request);

    return response is not null
        ? Results.Ok(response)
        : Results.StatusCode(500);
});

app.Run();