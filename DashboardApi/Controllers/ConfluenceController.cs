using DashboardApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DashboardApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ConfluenceController : ControllerBase
{
    private readonly ConfluenceService _confluenceService;

    public ConfluenceController(ConfluenceService confluenceService)
    {
        _confluenceService = confluenceService;
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string? q, [FromQuery] int limit = 10)
    {
        var query = q?.Trim();
        if (string.IsNullOrEmpty(query))
            return Ok(new { results = Array.Empty<object>() });

        var results = await _confluenceService.SearchAsync(query, Math.Clamp(limit, 1, 50));
        return Ok(new { results });
    }
}
