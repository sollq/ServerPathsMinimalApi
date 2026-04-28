using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ServerPathsMinimalApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();
builder.Services.AddOpenApi();

builder.WebHost.ConfigureKestrel(options =>
    options.ListenAnyIP(5001, listenOptions => listenOptions.UseHttps()));

builder.Services.Configure<FileServiceOptions>(
    builder.Configuration.GetSection("FileService"));

builder.Services.AddLogging();
builder.Services.AddResponseCompression(options => { options.EnableForHttps = true; });

builder.Services.AddHttpClient("ScannerClient", (sp, client) =>
{
    var opts = sp.GetRequiredService<IOptions<FileServiceOptions>>().Value;
    client.BaseAddress = new Uri(opts.ScannerUrl);
    client.DefaultRequestHeaders.Add("X-API-Key", opts.ExternalApiKey);
});
builder.Services.AddScoped<ILinksComparerService, LinksComparerService>();

var app = builder.Build();

app.UseResponseCompression();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPost("/api/LinksComparer/compare-list", async (
    [FromHeader(Name = "x-api-key")] string? apiKey,
    [FromBody] LinksComparisonRequest request,
    ILinksComparerService linksComparer,
    IOptions<FileServiceOptions> options,
    CancellationToken ct) =>
{
    if (string.IsNullOrEmpty(apiKey) || apiKey != options.Value.ApiKey)
        return Results.Unauthorized();

    if (request == null || request.LinksInBd == null)
        return Results.BadRequest("Body is missing or invalid JSON");


    var invalidLinks = await linksComparer.GetInvalidLinks(request, ct);

    var response = new LinksComparisonResponse(invalidLinks);

    return Results.Ok(response);
});

app.Run();