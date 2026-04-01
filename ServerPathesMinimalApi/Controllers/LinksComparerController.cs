using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using ServerPathsMinimalApi.Models;

[ApiController]
[Route("api/[controller]")]
public class LinksComparerController : ControllerBase
{
    private readonly ILinksComparerService _linksComparer;
    private readonly string _expectedApiKey;

    public LinksComparerController(ILinksComparerService linksComparer, IOptions<FileServiceOptions> options)
    {
        _linksComparer = linksComparer;
        _expectedApiKey = options.Value.ApiKey;
    }

    [HttpPost("compare-list")]
    public IActionResult CompareLinks([FromHeader(Name = "x-api-key")] string apiKey, [FromBody] LinksComparisonRequest request)
    {
        if (apiKey != _expectedApiKey) return Unauthorized();

        var response = _linksComparer.GetInvalidLinks(request);
        if (response is null) return StatusCode(500, "Internal Server Error");

        return Ok(response);
    }
}