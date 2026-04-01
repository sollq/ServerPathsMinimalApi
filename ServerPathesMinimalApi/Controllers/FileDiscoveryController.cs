using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ServerPathsMinimalApi.Models;
using ServerPathsMinimalApi.Services.Interfaces;

[ApiController]
[Route("api/[controller]")]
public class FileDiscoveryController : ControllerBase
{
    private readonly IFileProviderService _fileProvider;
    private readonly string _expectedApiKey;

    public FileDiscoveryController(IFileProviderService fileProvider, IOptions<FileServiceOptions> options)
    {
        _fileProvider = fileProvider;
        _expectedApiKey = options.Value.ApiKey;
    }

    [HttpGet("list")]
    public IActionResult GetFiles([FromHeader(Name = "x-api-key")] string apiKey)
    {
        if (apiKey != _expectedApiKey) return Unauthorized();
        return Ok(_fileProvider.CachedFiles);
    }
}