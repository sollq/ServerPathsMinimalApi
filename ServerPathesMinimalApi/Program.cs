using ServerPathsMinimalApi.Models;
using ServerPathsMinimalApi.Services;
using ServerPathsMinimalApi.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(5001, listenOptions => listenOptions.UseHttps()));

builder.Services.Configure<FileServiceOptions>(
    builder.Configuration.GetSection("FileService"));

builder.Services.AddLogging();
builder.Services.AddSingleton<IFileProviderService, FileProviderService>();
builder.Services.AddHostedService(sp => (FileProviderService)sp.GetRequiredService<IFileProviderService>());
builder.Services.AddScoped<ILinksComparerService, LinksComparerService>();

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

var app = builder.Build();
app.UseResponseCompression();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

app.Run();
